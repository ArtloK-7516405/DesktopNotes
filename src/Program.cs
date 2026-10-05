using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using WF = System.Windows.Forms;

namespace DesktopNotes
{
    public static class Program
    {
        static Mutex mutex;
        static WF.NotifyIcon tray;
        static Application app;
        static readonly Dictionary<string, NoteWindow> wins = new();
        static SettingsWindow settings;

        [STAThread]
        public static void Main(string[] args)
        {
            var cmd = string.Join(" ", args);
            if (cmd == "--uninstall") { Integration.Remove(); return; }

            mutex = new Mutex(true, "DesktopNotes_Single", out bool first);
            if (!first)
            {   // приложение уже запущено — передаём ему команду
                try
                {
                    using var c = new NamedPipeClientStream(".", "DesktopNotes_Pipe", PipeDirection.Out);
                    c.Connect(3000);
                    using var w = new StreamWriter(c) { AutoFlush = true };
                    w.WriteLine(cmd == "" ? "--show" : cmd);
                }
                catch { }
                return;
            }

            bool isNew = Store.Load();
            Integration.Register();
            if (isNew) Integration.Autostart = true;

            app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            foreach (var n in Store.Data.Notes.ToList()) Open(n, false);
            StartTray();
            StartPipe();
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            t.Tick += (s, e) => CheckAlarms();
            t.Start();
            app.Dispatcher.BeginInvoke(new Action(() => { Handle(cmd); CheckAlarms(); }));
            app.Run();
            tray.Visible = false;
        }

        static void Open(NoteData d, bool cursor)
        {
            var w = new NoteWindow(d, cursor);
            wins[d.Id] = w;
            w.Show();
        }

        public static void NewNote(string type)
        {
            var d = new NoteData { Type = type };
            if (type == "reminder")
            {
                var n = DateTime.Now.AddHours(1);
                d.When = new DateTime(n.Year, n.Month, n.Day, n.Hour, 0, 0);
            }
            Store.Data.Notes.Add(d);
            Store.Save();
            Open(d, true);
        }

        public static void Delete(NoteData d)
        {
            if (wins.TryGetValue(d.Id, out var w)) { wins.Remove(d.Id); w.Close(); }
            Store.Data.Notes.Remove(d);
            Store.Save();
        }

        public static void Refresh(NoteData d) { if (wins.TryGetValue(d.Id, out var w)) w.Build(); }

        static void Handle(string c)
        {
            if (c.StartsWith("--new")) NewNote(c.EndsWith("reminder") ? "reminder" : "list");
            else if (c == "--settings") ShowSettings();
            else if (c == "--show") foreach (var w in wins.Values) w.Show();
        }

        static void CheckAlarms()
        {
            var due = Store.Data.Notes.Where(n => n.Type == "reminder" && !n.Fired && n.When != null && n.When <= DateTime.Now).ToList();
            foreach (var d in due)
            {
                d.Fired = true;
                Store.Save();
                new AlarmWindow(d).Show();
            }
        }

        static void ShowSettings()
        {
            if (settings != null) { settings.Activate(); return; }
            settings = new SettingsWindow();
            settings.Closed += (s, e) => settings = null;
            settings.Show();
        }

        static void StartTray()
        {
            var m = new WF.ContextMenuStrip();
            m.Items.Add("Новая заметка", null, (s, e) => NewNote("list"));
            m.Items.Add("Новое напоминание", null, (s, e) => NewNote("reminder"));
            m.Items.Add("Показать все заметки", null, (s, e) => Handle("--show"));
            m.Items.Add("Настройки", null, (s, e) => ShowSettings());
            m.Items.Add(new WF.ToolStripSeparator());
            m.Items.Add("Выход", null, (s, e) => { tray.Visible = false; app.Shutdown(); });
            tray = new WF.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath),
                Text = "Desktop Notes",
                Visible = true,
                ContextMenuStrip = m
            };
            tray.DoubleClick += (s, e) => ShowSettings();
        }

        static void StartPipe()
        {
            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        using var s = new NamedPipeServerStream("DesktopNotes_Pipe", PipeDirection.In);
                        s.WaitForConnection();
                        using var r = new StreamReader(s);
                        var line = r.ReadLine();
                        if (line != null) app.Dispatcher.BeginInvoke(new Action(() => Handle(line)));
                    }
                    catch { Thread.Sleep(300); }
                }
            })
            { IsBackground = true }.Start();
        }
    }
}
