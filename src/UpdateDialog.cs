using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed class UpdateDialog : Form
    {
        private readonly UpdateCheckResult _result;

        private UpdateDialog(UpdateCheckResult result)
        {
            _result = result ?? new UpdateCheckResult();

            string currentVersion = string.IsNullOrWhiteSpace(_result.CurrentVersion)
                ? UpdateChecker.CurrentVersion
                : _result.CurrentVersion;
            bool hasNotes = !string.IsNullOrWhiteSpace(_result.ReleaseNotes);

            Text = "Обновления";
            Font = Theme.FontUi;
            BackColor = Theme.Bg;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, hasNotes ? 580 : 390);

            GradientPanel header = new GradientPanel();
            header.Dock = DockStyle.Top;
            header.Height = 58;
            Controls.Add(header);

            Label title = Ui.Label("Проверка обновлений", Theme.FontSection, Color.White);
            title.SetBounds(18, 9, 660, 28);
            header.Controls.Add(title);

            Label subtitle = Ui.Label(
                "GitHub · phenomenonRT/NetLimiterParser",
                Theme.FontUiSmall,
                Color.FromArgb(203, 213, 225));
            subtitle.SetBounds(18, 34, 660, 18);
            header.Controls.Add(subtitle);

            Label current = Ui.Label(
                "Текущая версия: " + currentVersion,
                Theme.FontBold,
                Theme.Text);
            current.SetBounds(20, 72, 660, 22);
            Controls.Add(current);

            string status;
            Color statusColor;
            string details;
            BuildStatusText(currentVersion, out status, out statusColor, out details);

            Label statusLabel = Ui.Label(status, Theme.FontSection, statusColor);
            statusLabel.SetBounds(20, 100, 660, 30);
            Controls.Add(statusLabel);

            Label detailsLabel = Ui.Label(details, Theme.FontUi, Theme.MutedDark);
            detailsLabel.SetBounds(20, 136, 660, hasNotes ? 68 : 150);
            detailsLabel.TextAlign = ContentAlignment.TopLeft;
            Controls.Add(detailsLabel);

            if (hasNotes)
            {
                Label notesTitle = Ui.Label("Что нового", Theme.FontBold, Theme.Text);
                notesTitle.SetBounds(20, 208, 660, 22);
                Controls.Add(notesTitle);

                TextBox notes = new TextBox();
                notes.Multiline = true;
                notes.ReadOnly = true;
                notes.ScrollBars = ScrollBars.Vertical;
                notes.WordWrap = true;
                notes.Font = Theme.FontUi;
                notes.BackColor = Theme.Surface;
                notes.ForeColor = Theme.Text;
                notes.BorderStyle = BorderStyle.FixedSingle;
                notes.SetBounds(20, 234, 660, 260);
                notes.Text = LimitNotes(_result.ReleaseNotes);
                Controls.Add(notes);
            }

            RoundButton close = new RoundButton();
            close.Text = "Закрыть";
            close.Quiet = true;
            close.SetBounds(592, ClientSize.Height - 54, 88, 34);
            close.Click += delegate { Close(); };
            Controls.Add(close);

            RoundButton action = new RoundButton();
            action.Text = _result.Success && !_result.NoReleases && _result.HasUpdate
                ? "Скачать обновление"
                : "Открыть на GitHub";
            action.Primary = _result.Success && !_result.NoReleases && _result.HasUpdate;
            action.SetBounds(408, ClientSize.Height - 54, 172, 34);
            action.Click += delegate { OpenReleasePage(); };
            Controls.Add(action);

            AcceptButton = action;
            CancelButton = close;
            Shown += delegate { close.Focus(); };
        }

        public static void Show(IWin32Window owner, UpdateCheckResult result)
        {
            using (UpdateDialog dialog = new UpdateDialog(result))
                dialog.ShowDialog(owner);
        }

        private void BuildStatusText(string currentVersion, out string status, out Color color, out string details)
        {
            if (!_result.Success)
            {
                status = "Не удалось проверить обновления";
                color = Theme.Red;
                details = (_result.ErrorMessage ?? "Неизвестная ошибка.") + Environment.NewLine +
                    "Проверьте подключение к интернету или откройте страницу релизов вручную.";
                return;
            }

            if (_result.NoReleases)
            {
                status = "Опубликованных релизов пока нет";
                color = Theme.Amber;
                details = "В репозитории GitHub ещё нет стабильных релизов, поэтому проверять обновление не с чем." +
                    Environment.NewLine +
                    "Когда появится новый релиз, нажмите «Обновления» ещё раз. Текущая версия: " +
                    currentVersion + ".";
                return;
            }

            string releaseLine = BuildReleaseLine();
            if (_result.VersionComparisonFailed)
            {
                status = "Не удалось сравнить версии";
                color = Theme.Amber;
                details = releaseLine + Environment.NewLine +
                    "Номер версии в теге релиза не удалось разобрать автоматически. " +
                    "Откройте страницу релиза на GitHub и проверьте версию вручную.";
                return;
            }

            if (_result.HasUpdate)
            {
                status = "Доступна новая версия " + _result.LatestVersion;
                color = Theme.Accent;
                details = releaseLine + Environment.NewLine +
                    "Скачайте новый выпуск со страницы GitHub и замените текущий исполняемый файл.";
                return;
            }

            status = "У вас последняя версия";
            color = Theme.Green;
            details = releaseLine + Environment.NewLine +
                "Установленная версия не старше последнего стабильного релиза. Обновление не требуется.";
        }

        private string BuildReleaseLine()
        {
            string tag = string.IsNullOrWhiteSpace(_result.TagName)
                ? _result.LatestVersion
                : _result.TagName.Trim();

            StringBuilder sb = new StringBuilder();
            sb.Append("Последний релиз: ");
            sb.Append(string.IsNullOrWhiteSpace(tag) ? "—" : tag);

            string name = (_result.ReleaseName ?? "").Trim();
            if (name.Length > 0 && !string.Equals(name, tag, StringComparison.OrdinalIgnoreCase))
                sb.Append(" — ").Append(name);

            if (_result.HasPublishedDate)
            {
                sb.Append(" · опубликован ");
                sb.Append(_result.PublishedUtc.ToLocalTime().ToString(
                    "dd.MM.yyyy HH:mm",
                    CultureInfo.CurrentCulture));
            }

            return sb.ToString();
        }

        private void OpenReleasePage()
        {
            string url = _result.ReleaseUrl;
            if (string.IsNullOrWhiteSpace(url))
                url = UpdateChecker.ReleasesUrl;

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Error("Не удалось открыть страницу релиза.", ex);
                MessageBox.Show(
                    this,
                    "Не удалось открыть ссылку в браузере." + Environment.NewLine + Environment.NewLine +
                    url + Environment.NewLine + Environment.NewLine +
                    "Подробности записаны в журнал: " + AppPaths.LogFile,
                    "Обновления",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static string LimitNotes(string notes)
        {
            string text = (notes ?? "").Trim();
            if (text.Length > 30000)
            {
                text = text.Substring(0, 30000) + Environment.NewLine + Environment.NewLine +
                    "… Текст описания сокращён. Полная версия — на странице релиза.";
            }

            return text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", "\r\n");
        }
    }
}
