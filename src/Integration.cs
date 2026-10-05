using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DesktopNotes
{
    /// <summary>Пункты меню рабочего стола и автозапуск (всё в HKCU, права администратора не нужны).</summary>
    public static class Integration
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string Shell = @"Software\Classes\DesktopBackground\Shell\";
        static string Exe => Environment.ProcessPath;
        const string ClassicRoot = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";
        [DllImport("shell32.dll")] static extern void SHChangeNotify(int evt, int flags, IntPtr a, IntPtr b);

        /// <summary>Классическое контекстное меню Windows 10 вместо сокращённого меню Windows 11.</summary>
        public static bool ClassicMenu
        {
            get { using var k = Registry.CurrentUser.OpenSubKey(ClassicRoot + @"\InprocServer32"); return k != null; }
            set
            {
                if (value) { using var k = Registry.CurrentUser.CreateSubKey(ClassicRoot + @"\InprocServer32"); k.SetValue("", ""); }
                else Registry.CurrentUser.DeleteSubKeyTree(ClassicRoot, false);
            }
        }

        public static void RestartExplorer()
        {
            var p = Process.Start(new ProcessStartInfo("taskkill", "/f /im explorer.exe") { CreateNoWindow = true, UseShellExecute = false });
            p?.WaitForExit();
            Process.Start("explorer.exe");
        }

        public static void Register()
        {
            Add("DesktopNotes_List", "Создать заметку", "--new list");
            Add("DesktopNotes_Reminder", "Создать напоминание", "--new reminder");
            SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
        }

        static void Add(string key, string title, string args)
        {
            using var k = Registry.CurrentUser.CreateSubKey(Shell + key);
            k.SetValue("", title);
            k.SetValue("Icon", $"\"{Exe}\"");
            k.SetValue("Position", "Top");
            using var c = k.CreateSubKey("command");
            c.SetValue("", $"\"{Exe}\" {args}");
        }

        public static void Remove()
        {
            Registry.CurrentUser.DeleteSubKeyTree(Shell + "DesktopNotes_List", false);
            Registry.CurrentUser.DeleteSubKeyTree(Shell + "DesktopNotes_Reminder", false);
            Autostart = false;
            ClassicMenu = false;
        }

        public static bool Autostart
        {
            get { using var k = Registry.CurrentUser.OpenSubKey(RunKey); return k?.GetValue("DesktopNotes") != null; }
            set
            {
                using var k = Registry.CurrentUser.CreateSubKey(RunKey);
                if (value) k.SetValue("DesktopNotes", $"\"{Exe}\"");
                else k.DeleteValue("DesktopNotes", false);
            }
        }
    }
}
