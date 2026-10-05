using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace DesktopNotes
{
    public class NoteWindow : Window
    {
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int i);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h, int i, int v);

        static readonly FontFamily Icons = new("Segoe Fluent Icons, Segoe MDL2 Assets");
        static readonly ControlTemplate TbTemplate = (ControlTemplate)XamlReader.Parse(
            "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
            "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='TextBox'>" +
            "<Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' " +
            "BorderThickness='{TemplateBinding BorderThickness}' Padding='{TemplateBinding Padding}'>" +
            "<ScrollViewer x:Name='PART_ContentHost'/></Border></ControlTemplate>");

        readonly NoteData D;
        readonly bool cursor;
        readonly Border card = new();
        TextBox newBox;
        Border pinIcon;
        Brush fg, line;

        public NoteWindow(NoteData d, bool cursor)
        {
            D = d; this.cursor = cursor;
            WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
            ShowInTaskbar = false; ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight; Left = d.X; Top = d.Y;
            Language = XmlLanguage.GetLanguage("ru-RU");
            card.CornerRadius = new CornerRadius(14);
            card.Padding = new Thickness(16, 10, 16, 18);
            card.Margin = new Thickness(8);
            card.Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 2, Opacity = .4 };
            Content = card;

            // перетаскивание — только пока заметка не закреплена
            card.MouseLeftButtonDown += (s, e) =>
            {
                if (D.Pinned) return;
                try { DragMove(); } catch { }
                D.X = Left; D.Y = Top; Store.Save();
            };
            Store.SettingsChanged += Build;
            Closed += (s, e) => Store.SettingsChanged -= Build;
            Activated += (s, e) => { if (D.Pinned) ToBottom(); };
            Loaded += (s, e) =>
            {
                if (D.Pinned) ToBottom();
                if (this.cursor) { Activate(); newBox?.Focus(); }
            };
            Build();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var h = new WindowInteropHelper(this).Handle;
            SetWindowLong(h, -20, GetWindowLong(h, -20) | 0x80); // TOOLWINDOW: не показывать в Alt+Tab
            if (cursor)
            {   // новая заметка появляется там, где щёлкнули правой кнопкой
                var p = System.Windows.Forms.Cursor.Position;
                var dpi = VisualTreeHelper.GetDpi(this);
                Left = p.X / dpi.DpiScaleX; Top = p.Y / dpi.DpiScaleY;
                D.X = Left; D.Y = Top; Store.Save();
            }
        }

        static Brush Br(string c) => (Brush)new BrushConverter().ConvertFromString(c);

        public void Build()
        {
            var st = Store.Data.Settings;
            var th = Themes.Get(st.Theme);
            card.Background = Br(th.bg); fg = Br(th.fg); line = Br(th.line);
            FontFamily = new FontFamily(st.Font); FontSize = st.FontSize; Foreground = fg;

            var root = new StackPanel { MinWidth = 300, MaxWidth = 520 };
            var head = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            pinIcon = new Border { Background = Brushes.Transparent, Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(8, 0, 8, 0) };
            pinIcon.MouseLeftButtonDown += (s, e) => { e.Handled = true; TogglePin(); };
            var del = MkIcon("\uE711", DeleteNote, 14);
            del.HorizontalAlignment = HorizontalAlignment.Right; del.VerticalAlignment = VerticalAlignment.Top; del.Opacity = .6;
            head.Children.Add(pinIcon); head.Children.Add(del);
            root.Children.Add(head);
            newBox = null;
            root.Children.Add(D.Type == "list" ? ListBody() : ReminderBody());
            card.Child = root;
            ApplyPin();
        }

        TextBlock MkIcon(string glyph, Action click, double size)
        {
            var t = new TextBlock
            {
                Text = glyph, FontFamily = Icons, FontSize = size, Foreground = fg,
                Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center, Background = Brushes.Transparent
            };
            t.MouseLeftButtonDown += (s, e) => { e.Handled = true; click(); };
            return t;
        }

        TextBox Field(string text) => new TextBox
        {
            Text = text, Template = TbTemplate, Background = Brushes.Transparent, Foreground = fg, CaretBrush = fg,
            BorderBrush = line, BorderThickness = new Thickness(0, 0, 0, 2), Padding = new Thickness(2, 4, 10, 4),
            TextWrapping = TextWrapping.Wrap, VerticalContentAlignment = VerticalAlignment.Center
        };

        Grid Row(UIElement icon, UIElement content)
        {
            var g = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(content, 1);
            g.Children.Add(icon); g.Children.Add(content);
            return g;
        }

        // ---------- заметка-список ----------
        UIElement ListBody()
        {
            var p = new StackPanel();
            foreach (var it in D.Items.ToList())
            {
                var pen = MkIcon("\uE70F", () => { it.Done = !it.Done; Store.Save(); Build(); }, 16);
                var tb = Field(it.Text);
                tb.TextChanged += (s, e) => it.Text = tb.Text;
                tb.KeyDown += (s, e) => { if (e.Key == Key.Enter) newBox?.Focus(); };
                tb.LostKeyboardFocus += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(it.Text))
                    {
                        D.Items.Remove(it); Store.Save();
                        Dispatcher.BeginInvoke(new Action(Build));
                    }
                    else Store.Save();
                };
                var row = Row(pen, tb);
                if (it.Done) { tb.TextDecorations = TextDecorations.Strikethrough; row.Opacity = .4; }
                p.Children.Add(row);
            }
            newBox = Field("");
            newBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) AddItem(); };
            p.Children.Add(Row(MkIcon("\uE710", AddItem, 16), newBox));
            return p;
        }

        void AddItem()
        {
            if (newBox == null || string.IsNullOrWhiteSpace(newBox.Text)) { newBox?.Focus(); return; }
            D.Items.Add(new Item { Text = newBox.Text.Trim() });
            Store.Save();
            Build();
            Dispatcher.BeginInvoke(new Action(() => newBox?.Focus()), DispatcherPriority.Input);
        }

        // ---------- напоминание ----------
        UIElement ReminderBody()
        {
            var p = new StackPanel();
            var date = Field(Fmt.Date(D.When)); var time = Field(Fmt.Time(D.When));
            date.BorderThickness = new Thickness(0); time.BorderThickness = new Thickness(0);
            date.TextWrapping = TextWrapping.NoWrap; time.TextWrapping = TextWrapping.NoWrap;
            time.TextAlignment = TextAlignment.Right; time.Margin = new Thickness(24, 0, 0, 0);
            Mask.Attach(date, "00.00.0000");
            Mask.Attach(time, "00:00");

            var cal = new System.Windows.Controls.Calendar();
            var calIcon = MkIcon("\uE787", () => { }, 16);
            var pop = new Popup
            {
                StaysOpen = false, PlacementTarget = calIcon, Placement = PlacementMode.Bottom,
                Child = new Border { Background = Brushes.White, Padding = new Thickness(4), CornerRadius = new CornerRadius(6), Child = cal }
            };

            void Update()
            {
                if (DateTime.TryParseExact(date.Text, Fmt.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                    && TimeSpan.TryParseExact(time.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var ts))
                {
                    D.When = dt.Date + ts; D.Fired = false; Store.Save();
                }
                date.Text = Fmt.Date(D.When); time.Text = Fmt.Time(D.When);   // неверный ввод откатывается
            }
            date.LostKeyboardFocus += (s, e) => Update();
            time.LostKeyboardFocus += (s, e) => Update();
            calIcon.MouseLeftButtonDown += (s, e) => { cal.SelectedDate = D.When?.Date; pop.IsOpen = true; };
            cal.SelectedDatesChanged += (s, e) =>
            {
                if (cal.SelectedDate == null) return;
                date.Text = cal.SelectedDate.Value.ToString(Fmt.DateFormat, CultureInfo.InvariantCulture);
                pop.IsOpen = false; Update();
            };

            var g = new Grid();
            for (int i = 0; i < 4; i++) g.ColumnDefinitions.Add(new ColumnDefinition { Width = i == 2 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
            Grid.SetColumn(calIcon, 1); Grid.SetColumn(time, 3);
            g.Children.Add(date); g.Children.Add(calIcon); g.Children.Add(time);
            var b = new Border { BorderBrush = line, BorderThickness = new Thickness(0, 0, 0, 2), Child = g };
            p.Children.Add(Row(MkIcon("\uE70F", () => date.Focus(), 16), b));

            var desc = Field(D.Text);
            desc.AcceptsReturn = true; desc.MinHeight = 56; desc.VerticalContentAlignment = VerticalAlignment.Top;
            desc.TextChanged += (s, e) => D.Text = desc.Text;
            desc.LostKeyboardFocus += (s, e) => Store.Save();
            p.Children.Add(Row(MkIcon("\uE70F", () => desc.Focus(), 16), desc));
            return p;
        }

        // ---------- закрепление ----------
        // закреплена — «кнопка, вдавленная в поверхность»; откреплена — наклонная булавка
        const string LampPin = "M17,3 L29,3 L35,18 Q36,21 32,22 Q23,26 14,22 Q10,21 11,18 Z M23,24 L23,35 M15,35 L31,35";
        const string TiltPin = "M21,3 L34,16 L29,18 L26,25 L12,11 L19,8 Z M19,19 L5,33";

        FrameworkElement PinShape(bool pinned) => new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(pinned ? LampPin : TiltPin), Stroke = fg, StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
            Stretch = Stretch.Uniform, Width = 26, Height = 30
        };

        void TogglePin() { D.Pinned = !D.Pinned; Store.Save(); ApplyPin(); }

        // закреплена: стоит на рабочем столе, под всеми окнами, не двигается
        // откреплена: поверх окон, можно перетаскивать
        void ApplyPin()
        {
            pinIcon.Child = PinShape(D.Pinned);
            Topmost = !D.Pinned;
            if (D.Pinned && IsLoaded) ToBottom();
        }

        void ToBottom()
        {
            var h = new WindowInteropHelper(this).Handle;
            if (h != IntPtr.Zero) SetWindowPos(h, (IntPtr)1, 0, 0, 0, 0, 0x0013); // HWND_BOTTOM, NOMOVE|NOSIZE|NOACTIVATE
        }

        void DeleteNote()
        {
            if (MessageBox.Show("Удалить заметку?", "Desktop Notes", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Program.Delete(D);
        }
    }
}
