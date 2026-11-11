using System;
using System.Threading;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                HandleFatal(e.Exception, "Необработанная ошибка интерфейса");
            };

            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                HandleFatal(e.ExceptionObject as Exception, "Необработанная ошибка приложения");
            };

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                HandleFatal(ex, "Не удалось запустить приложение");
            }
        }

        private static void HandleFatal(Exception ex, string title)
        {
            try
            {
                Log.Error(title + ".", ex);
            }
            catch
            {
            }

            try
            {
                string details = ex == null ? "Неизвестная ошибка." : ex.Message;
                MessageBox.Show(
                    details + Environment.NewLine + Environment.NewLine +
                    "Подробности записаны в журнал:" + Environment.NewLine + AppPaths.LogFile,
                    title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
            }
        }
    }
}
