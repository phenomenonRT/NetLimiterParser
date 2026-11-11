using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace NetLimiterParser
{
    internal static class AppDefaults
    {
        public const string DbPath = @"G:\Program Files\NetLimiter\nlstats.db";

        public static readonly string[] FallbackDbPaths = new string[]
        {
            @"G:\Program Files\NetLimiter\nlstats.db",
            @"C:\ProgramData\Locktime\NetLimiter\5\nlstats.db",
            @"C:\Program Files\NetLimiter\nlstats.db",
            @"C:\Program Files (x86)\NetLimiter\nlstats.db"
        };

        public static string FindExistingDatabase()
        {
            string found;
            if (TryFindDatabase(out found)) return found;
            return DbPath;
        }

        /// <summary>
        /// Проверяет сохранённый путь, затем ищет базу автоматически.
        /// </summary>
        public static bool TryResolveDatabase(string preferred, out string path)
        {
            if (IsDatabaseFile(preferred))
            {
                path = preferred;
                return true;
            }
            return TryFindDatabase(out path);
        }

        /// <summary>
        /// Ищет nlstats.db в папке статистики NetLimiter, стандартных местах
        /// и на всех фиксированных дисках. Не выполняет полное сканирование диска.
        /// </summary>
        public static bool TryFindDatabase(out string path)
        {
            path = null;
            List<string> candidates = new List<string>();

            AddNetLimiterSettingsCandidates(candidates);

            foreach (string standard in FallbackDbPaths)
                AddUnique(candidates, standard);

            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;

                    string root = drive.RootDirectory.FullName;
                    AddUnique(candidates, Path.Combine(root, @"Program Files\NetLimiter\nlstats.db"));
                    AddUnique(candidates, Path.Combine(root, @"Program Files\Locktime Software\NetLimiter\nlstats.db"));
                    AddUnique(candidates, Path.Combine(root, @"Program Files (x86)\NetLimiter\nlstats.db"));
                    AddUnique(candidates, Path.Combine(root, @"Program Files (x86)\Locktime Software\NetLimiter\nlstats.db"));
                    AddUnique(candidates, Path.Combine(root, @"ProgramData\Locktime\NetLimiter\5\nlstats.db"));
                    AddUnique(candidates, Path.Combine(root, @"NetLimiter\nlstats.db"));
                }
            }
            catch
            {
            }

            foreach (string candidate in candidates)
            {
                if (IsDatabaseFile(candidate))
                {
                    path = candidate;
                    return true;
                }
            }

            return false;
        }

        private static void AddNetLimiterSettingsCandidates(List<string> candidates)
        {
            string common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string root = Path.Combine(common, @"Locktime\NetLimiter");

            AddSettingsCandidates(candidates, Path.Combine(root, "5"));

            try
            {
                if (!Directory.Exists(root)) return;
                foreach (string dir in Directory.GetDirectories(root))
                {
                    if (string.Equals(dir, Path.Combine(root, "5"), StringComparison.OrdinalIgnoreCase))
                        continue;
                    AddSettingsCandidates(candidates, dir);
                }
            }
            catch
            {
            }
        }

        private static void AddSettingsCandidates(List<string> candidates, string folder)
        {
            try
            {
                AddStatsFolder(candidates, Path.Combine(folder, "nl_settings.xml"));
                AddStatsFolder(candidates, Path.Combine(folder, "nl_settings.xml.lastworking"));
                AddUnique(candidates, Path.Combine(folder, "nlstats.db"));
                AddUnique(candidates, Path.Combine(folder, @"Stats\nlstats.db"));
            }
            catch
            {
            }
        }

        private static void AddStatsFolder(List<string> candidates, string settingsFile)
        {
            try
            {
                if (!File.Exists(settingsFile)) return;

                string text = File.ReadAllText(settingsFile);
                string folder = ExtractTagValue(text, "StatsFolderName");
                if (string.IsNullOrWhiteSpace(folder)) return;

                AddUnique(candidates, Path.Combine(folder, "nlstats.db"));
            }
            catch
            {
            }
        }

        private static string ExtractTagValue(string text, string tag)
        {
            string open = "<" + tag;
            int start = text.IndexOf(open, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return null;

            int openEnd = text.IndexOf('>', start);
            if (openEnd < 0) return null;
            if (openEnd > start && text[openEnd - 1] == '/') return null;

            string close = "</" + tag + ">";
            int end = text.IndexOf(close, openEnd + 1, StringComparison.OrdinalIgnoreCase);
            if (end < 0) return null;

            string value = text.Substring(openEnd + 1, end - openEnd - 1).Trim();
            if (value.Length == 0) return null;

            value = value.Replace("&amp;", "&");
            value = value.Replace("&lt;", "<");
            value = value.Replace("&gt;", ">");
            value = value.Replace("&quot;", "\"");
            value = value.Replace("&apos;", "'");
            return value;
        }

        private static void AddUnique(List<string> list, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            foreach (string existing in list)
            {
                if (string.Equals(existing, path, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            list.Add(path);
        }

        private static bool IsDatabaseFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            try
            {
                FileInfo info = new FileInfo(path);
                return info.Exists && info.Length > 4096;
            }
            catch
            {
                return false;
            }
        }
    }

    internal sealed class AppSettings
    {
        public string LastDbPath { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public bool Maximized { get; set; }
        public int PeriodMinutes { get; set; }
        public int LastTab { get; set; }

        public AppSettings()
        {
            LastDbPath = AppDefaults.FindExistingDatabase();
            WindowWidth = 1500;
            WindowHeight = 900;
            Maximized = false;
            PeriodMinutes = 1440;
            LastTab = 0;
        }
    }

    internal static class SettingsStore
    {
        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(AppPaths.SettingsFile))
                    return new AppSettings();

                string json = File.ReadAllText(AppPaths.SettingsFile, Encoding.UTF8);
                JavaScriptSerializer ser = new JavaScriptSerializer();
                AppSettings s = ser.Deserialize<AppSettings>(json);
                if (s == null) return new AppSettings();

                if (string.IsNullOrWhiteSpace(s.LastDbPath))
                    s.LastDbPath = AppDefaults.FindExistingDatabase();
                if (s.WindowWidth < 1100) s.WindowWidth = 1500;
                if (s.WindowHeight < 650) s.WindowHeight = 900;

                int[] allowed = new int[] { -1, 0, 15, 60, 360, 1440, 10080, 43200 };
                bool ok = false;
                foreach (int v in allowed)
                {
                    if (s.PeriodMinutes == v) { ok = true; break; }
                }
                if (!ok) s.PeriodMinutes = 1440;
                if (s.LastTab < 0 || s.LastTab > 4) s.LastTab = 0;

                return s;
            }
            catch (Exception ex)
            {
                Log.Error("Не удалось прочитать настройки.", ex);
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                AppPaths.EnsureDataDir();
                JavaScriptSerializer ser = new JavaScriptSerializer();
                string json = ser.Serialize(settings);
                File.WriteAllText(AppPaths.SettingsFile, json, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Log.Error("Не удалось сохранить настройки.", ex);
            }
        }
    }
}
