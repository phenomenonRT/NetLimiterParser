using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace NetLimiterParser
{
    internal static class Log
    {
        private static readonly object Sync = new object();

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Warn(string message)
        {
            Write("WARN", message, null);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", message, ex);
        }

        private static void Write(string level, string message, Exception ex)
        {
            try
            {
                lock (Sync)
                {
                    AppPaths.EnsureDataDir();

                    FileInfo fi = new FileInfo(AppPaths.LogFile);
                    if (fi.Exists && fi.Length > 2 * 1024 * 1024)
                    {
                        string old = AppPaths.LogFile + ".old";
                        try
                        {
                            if (File.Exists(old)) File.Delete(old);
                            File.Move(AppPaths.LogFile, old);
                        }
                        catch
                        {
                        }
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                    sb.Append(" [");
                    sb.Append(level);
                    sb.Append("] ");
                    sb.Append(message);
                    if (ex != null)
                    {
                        sb.AppendLine();
                        sb.Append(ex.ToString());
                    }
                    sb.AppendLine();

                    File.AppendAllText(AppPaths.LogFile, sb.ToString(), new UTF8Encoding(false));
                }
            }
            catch
            {
                // Logging must never crash the application.
            }
        }
    }
}
