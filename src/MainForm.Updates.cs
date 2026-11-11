using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed partial class MainForm
    {
        private async void CheckForUpdatesAsync()
        {
            if (_checkingUpdates) return;

            _checkingUpdates = true;
            string previousText = _btnUpdates == null ? "Обновления" : _btnUpdates.Text;
            if (_btnUpdates != null)
            {
                _btnUpdates.Text = "Проверка…";
                _btnUpdates.Enabled = false;
            }

            SetStatus("Проверка обновлений на GitHub…");

            try
            {
                UpdateCheckResult result = await Task.Run(delegate { return UpdateChecker.Check(); });
                if (_closing || IsDisposed) return;

                if (!result.Success)
                {
                    SetStatus("Не удалось проверить обновления.");
                    Log.Warn("Проверка обновлений: " + result.ErrorMessage);
                }
                else if (result.NoReleases)
                {
                    SetStatus("Опубликованных релизов на GitHub пока нет.");
                }
                else if (result.VersionComparisonFailed)
                {
                    SetStatus("Последний релиз: " + result.LatestVersion + " — сравнение версий недоступно.");
                }
                else if (result.HasUpdate)
                {
                    SetStatus("Доступна новая версия " + result.LatestVersion + ".");
                }
                else
                {
                    SetStatus("У вас последняя версия " + result.CurrentVersion + ".");
                }

                UpdateDialog.Show(this, result);
            }
            catch (Exception ex)
            {
                Log.Error("Не удалось выполнить проверку обновлений.", ex);
                SetStatus("Не удалось проверить обновления.");
                MessageBox.Show(
                    this,
                    "Не удалось выполнить проверку обновлений." + Environment.NewLine + Environment.NewLine +
                    ex.Message + Environment.NewLine + Environment.NewLine +
                    "Подробности записаны в журнал: " + AppPaths.LogFile,
                    "Обновления",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                _checkingUpdates = false;
                if (_btnUpdates != null && !_btnUpdates.IsDisposed)
                {
                    _btnUpdates.Text = previousText;
                    _btnUpdates.Enabled = true;
                }
            }
        }
    }
}
