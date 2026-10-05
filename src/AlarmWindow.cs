using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DesktopNotes
{
    /// <summary>Полноэкранное напоминание: затемнение + карточка по центру + сигнал.</summary>
    public class AlarmWindow : Window
    {
        static readonly FontFamily Icons = new("Segoe Fluent Icons, Segoe MDL2 Assets");
        readonly NoteData d;

        public AlarmWindow(NoteData d)
        {
            this.d = d;
            var st = Store.Data.Settings;
            var th = Themes.Get(st.Theme);
            var fg = (Brush)new BrushConverter().ConvertFromString(th.fg);
            var ln = (Brush)new BrushConverter().ConvertFromString(th.line);

            WindowStyle = WindowStyle.None; AllowsTransparency = true; ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromArgb(0xB3, 0, 0, 0));
            Topmost = true; ShowInTaskbar = false;
            Left = SystemParameters.VirtualScreenLeft; Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth; Height = SystemParameters.VirtualScreenHeight;
            FontFamily = new FontFamily(st.Font); Foreground = fg;

            const double cw = 820;
            var card = new Border
            {
                Width = cw, CornerRadius = new CornerRadius(24), Padding = new Thickness(36, 14, 36, 40),
                Background = (Brush)new BrushConverter().ConvertFromString(th.bg),
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness((SystemParameters.PrimaryScreenWidth - cw) / 2 - SystemParameters.VirtualScreenLeft,
                                       SystemParameters.PrimaryScreenHeight * 0.25 - SystemParameters.VirtualScreenTop, 0, 0)
            };

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(Glyph("\uE921", fg, Snooze, "Отложить на 5 минут"));
            buttons.Children.Add(Glyph("\uE8BB", fg, Close, "Закрыть"));

            var bell = new TextBlock { Text = "\uEA8F", FontFamily = Icons, FontSize = 64, Foreground = fg, HorizontalAlignment = HorizontalAlignment.Center };
            var date = new TextBlock { Text = Fmt.Date(d.When), FontSize = 32, Foreground = fg };
            var time = new TextBlock { Text = Fmt.Time(d.When), FontSize = 32, Foreground = fg, HorizontalAlignment = HorizontalAlignment.Right };
            var dt = new Grid { Margin = new Thickness(0, 26, 0, 26) };
            dt.Children.Add(date); dt.Children.Add(time);
            var text = new TextBlock
            {
                Text = d.Text, FontSize = 40, FontWeight = FontWeights.SemiBold, Foreground = fg,
                TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 26, 0, 0)
            };

            var p = new StackPanel();
            p.Children.Add(buttons); p.Children.Add(bell);
            p.Children.Add(Line(ln)); p.Children.Add(dt); p.Children.Add(Line(ln)); p.Children.Add(text);
            card.Child = p;
            Content = new Grid { Children = { card } };

            KeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
            Loaded += (s, e) => { Activate(); Focus(); AlarmSound.Start(); };
            Closed += (s, e) => AlarmSound.Stop();
        }

        static Rectangle Line(Brush b) => new() { Height = 5, RadiusX = 2.5, RadiusY = 2.5, Fill = b, Margin = new Thickness(0, 14, 0, 0) };

        TextBlock Glyph(string g, Brush fg, Action a, string tip)
        {
            var t = new TextBlock
            {
                Text = g, FontFamily = Icons, FontSize = 20, Foreground = fg, ToolTip = tip, Cursor = Cursors.Hand,
                Padding = new Thickness(10), Background = Brushes.Transparent
            };
            t.MouseLeftButtonDown += (s, e) => a();
            return t;
        }

        // «—» откладывает напоминание на 5 минут
        void Snooze()
        {
            d.When = DateTime.Now.AddMinutes(5); d.Fired = false; Store.Save();
            Program.Refresh(d);
            Close();
        }
    }
}
