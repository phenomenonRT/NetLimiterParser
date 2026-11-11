using System;
using System.IO;

namespace NetLimiterParser
{
    internal static class AppPaths
    {
        public static readonly string DataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NetLimiterParser");

        public static readonly string SettingsFile = Path.Combine(DataDir, "settings.json");
        public static readonly string CategoriesFile = Path.Combine(DataDir, "categories.json");
        public static readonly string LogFile = Path.Combine(DataDir, "app.log");

        public static string SnapshotFile
        {
            get { return Path.Combine(DataDir, "nlstats.snapshot.db"); }
        }

        public static void EnsureDataDir()
        {
            if (!Directory.Exists(DataDir))
                Directory.CreateDirectory(DataDir);
        }
    }
}
