using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using WF = System.Windows.Forms;

namespace DesktopNotes
{
    public class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            Title = "Desktop Notes — настройки";
            Width = 500; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var s = Store.Data.Settings;
            var p = new StackPanel { Margin = new Thickness(22) };
            void Changed() { Store.Save(); Store.RaiseSettings(); }
            TextBlock Label(string t) => new() { Text = t, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 14, 0, 4) };
            Button Btn(string t, Action act) { var b = new Button { Content = t, Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 0, 8, 0) }; b.Click += (x, y) => act(); return b; }

            // ---- тема
            p.Children.Add(Label("Тема заметок"));
            var themes = Themes.All.Keys.ToList(); themes.Add(Themes.Custom);
            var th = new ComboBox { ItemsSource = themes, SelectedItem = s.Theme };
            th.SelectionChanged += (a, b) => { if (th.SelectedItem != null) { s.Theme = (string)th.SelectedItem; Changed(); } };
            p.Children.Add(th);

            p.Children.Add(Label("Своя тема: цвет фона и цвет элементов (текст, булавки, карандаши)"));
            Button ColorBtn(Func<string> get, Action<string> set)
            {
                var b = new Button { Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(0, 0, 8, 0), MinWidth = 120 };
                void Paint()
                {
                    var c = (Color)ColorConverter.ConvertFromString(get());
                    b.Background = new SolidColorBrush(c);
                    b.Foreground = (c.R * 0.299 + c.G * 0.587 + c.B * 0.114) > 140 ? Brushes.Black : Brushes.White;
                    b.Content = get();
                }
                Paint();
                b.Click += (x, y) =>
                {
                    var cc = (Color)ColorConverter.ConvertFromString(get());
                    using var dlg = new WF.ColorDialog { FullOpen = true, Color = System.Drawing.Color.FromArgb(cc.R, cc.G, cc.B) };
                    if (dlg.ShowDialog() == WF.DialogResult.OK)
                    {
                        set($"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}");
                        s.Theme = Themes.Custom; th.SelectedItem = Themes.Custom;
                        Paint(); Changed();
                    }
                };
                return b;
            }
            var colors = new StackPanel { Orientation = Orientation.Horizontal };
            colors.Children.Add(new TextBlock { Text = "Фон:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            colors.Children.Add(ColorBtn(() => s.CustomBg, v => s.CustomBg = v));
            colors.Children.Add(new TextBlock { Text = "Элементы:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 6, 0) });
            colors.Children.Add(ColorBtn(() => s.CustomFg, v => s.CustomFg = v));
            p.Children.Add(colors);

            // ---- шрифт
            p.Children.Add(Label("Шрифт заметок"));
            var font = new ComboBox { ItemsSource = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(x => x).ToList(), SelectedItem = s.Font };
            font.SelectionChanged += (a, b) => { if (font.SelectedItem != null) { s.Font = (string)font.SelectedItem; Changed(); } };
            p.Children.Add(font);

            p.Children.Add(Label("Размер шрифта"));
            var size = new ComboBox { ItemsSource = new[] { 12, 14, 16, 18, 20, 24 }, SelectedItem = (int)s.FontSize };
            size.SelectionChanged += (a, b) => { if (size.SelectedItem != null) { s.FontSize = (int)size.SelectedItem; Changed(); } };
            p.Children.Add(size);

            // ---- формат даты
            p.Children.Add(Label("Формат даты в напоминаниях"));
            var fmts = new[] { "ДД.ММ.ГГГГ", "ММ.ДД.ГГГГ" };
            var df = new ComboBox { ItemsSource = fmts, SelectedIndex = s.DateFormat == "MM.dd.yyyy" ? 1 : 0 };
            df.SelectionChanged += (a, b) =>
            {
                var newFmt = df.SelectedIndex == 1 ? "MM.dd.yyyy" : "dd.MM.yyyy";
                if (newFmt == s.DateFormat) return;
                s.DateFormat = newFmt; Changed();
            };
            p.Children.Add(df);

            // ---- мелодия
            p.Children.Add(Label("Мелодия напоминания"));
            var path = new TextBlock { Text = string.IsNullOrEmpty(s.Sound) ? "Стандартный сигнал" : s.Sound, TextTrimming = TextTrimming.CharacterEllipsis };
            p.Children.Add(path);
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            row.Children.Add(Btn("Выбрать файл…", () =>
            {
                var dlg = new OpenFileDialog { Filter = "Аудио|*.mp3;*.wav;*.wma;*.m4a|Все файлы|*.*" };
                if (dlg.ShowDialog() == true) { s.Sound = dlg.FileName; path.Text = dlg.FileName; Changed(); }
            }));
            row.Children.Add(Btn("Стандартный", () => { s.Sound = ""; path.Text = "Стандартный сигнал"; Changed(); }));
            bool playing = false;
            Button test = null;
            test = Btn("▶ Проверить", () =>
            {
                playing = !playing;
                if (playing) AlarmSound.Start(); else AlarmSound.Stop();
                test.Content = playing ? "■ Стоп" : "▶ Проверить";
            });
            row.Children.Add(test);
            p.Children.Add(row);

            // ---- система
            var auto = new CheckBox { Content = "Запускать вместе с Windows", IsChecked = Integration.Autostart, Margin = new Thickness(0, 22, 0, 0) };
            auto.Checked += (a, b) => Integration.Autostart = true;
            auto.Unchecked += (a, b) => Integration.Autostart = false;
            p.Children.Add(auto);

            var classic = new CheckBox
            {
                Content = new TextBlock { Text = "Показывать «Создать заметку» сразу в меню рабочего стола (классическое меню Windows 10)", TextWrapping = TextWrapping.Wrap },
                IsChecked = Integration.ClassicMenu, Margin = new Thickness(0, 12, 0, 0)
            };
            void ClassicChanged(bool on)
            {
                Integration.ClassicMenu = on;
                if (MessageBox.Show("Чтобы изменение вступило в силу, нужно перезапустить Проводник " +
                    "(панель задач на секунду пропадёт). Перезапустить сейчас?", "Desktop Notes",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    Integration.RestartExplorer();
            }
            classic.Checked += (a, b) => ClassicChanged(true);
            classic.Unchecked += (a, b) => ClassicChanged(false);
            p.Children.Add(classic);
            p.Children.Add(new TextBlock
            {
                Text = "В Windows 11 без этой опции пункты находятся в «Показать дополнительные параметры» (или Shift + правый клик). " +
                       "Опция меняет меню во всей системе; отключить её можно здесь же.",
                TextWrapping = TextWrapping.Wrap, Opacity = .6, FontSize = 11, Margin = new Thickness(22, 4, 0, 0)
            });

            Content = new ScrollViewer { Content = p, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            MaxHeight = SystemParameters.WorkArea.Height - 40;
            Closed += (a, b) => AlarmSound.Stop();
        }
    }
}
