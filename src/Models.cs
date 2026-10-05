using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace DesktopNotes
{
    public class Item { public string Text { get; set; } = ""; public bool Done { get; set; } }

    public class NoteData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "list";          // list | reminder
        public bool Pinned { get; set; }
        public double X { get; set; } = 200;
        public double Y { get; set; } = 200;
        public List<Item> Items { get; set; } = new();
        public DateTime? When { get; set; }
        public string Text { get; set; } = "";
        public bool Fired { get; set; }
    }

    public class Settings
    {
        public string Theme { get; set; } = "Графит";
        public string Font { get; set; } = "Bahnschrift";
        public double FontSize { get; set; } = 14;
        public string Sound { get; set; } = "";
        public string DateFormat { get; set; } = "dd.MM.yyyy";   // или MM.dd.yyyy
        public string CustomBg { get; set; } = "#404040";
        public string CustomFg { get; set; } = "#FFFFFF";
    }

    public class Db
    {
        public List<NoteData> Notes { get; set; } = new();
        public Settings Settings { get; set; } = new();
    }

    public static class Themes
    {
        public static readonly Dictionary<string, (string bg, string fg, string line)> All = new()
        {
            ["Графит"] = ("#404040", "#FFFFFF", "#D9D9D9"),
            ["Светлая"] = ("#F4F1EA", "#222222", "#555555"),
            ["Синяя"] = ("#1F3A5F", "#FFFFFF", "#BBD0F0"),
            ["Зелёная"] = ("#2E4A3B", "#FFFFFF", "#BEE0CB"),
            ["Фиолетовая"] = ("#4A3566", "#FFFFFF", "#D6C4F0"),
            ["Жёлтый стикер"] = ("#FFE88A", "#3A3000", "#7A6A20"),
        };
        public const string Custom = "Своя";
        public static (string bg, string fg, string line) Get(string n)
        {
            if (n == Custom) { var s = Store.Data.Settings; return (s.CustomBg, s.CustomFg, s.CustomFg); }
            return All.TryGetValue(n ?? "", out var t) ? t : All["Графит"];
        }
    }

    public static class Fmt
    {
        public static string DateFormat => Store.Data.Settings.DateFormat;
        public static string Date(DateTime? d) => d?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? "00.00.0000";
        public static string Time(DateTime? d) => d?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "00:00";
    }

    public static class Store
    {
        static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopNotes");
        static readonly string File_ = Path.Combine(Dir, "data.json");
        public static Db Data = new();
        public static event Action SettingsChanged;

        /// <returns>true если это первый запуск</returns>
        public static bool Load()
        {
            bool isNew = !File.Exists(File_);
            try { if (!isNew) Data = JsonSerializer.Deserialize<Db>(File.ReadAllText(File_)) ?? new Db(); } catch { }
            return isNew;
        }
        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(File_, JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
        public static void RaiseSettings() => SettingsChanged?.Invoke();
    }
}
