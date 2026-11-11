using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed partial class MainForm
    {
        private const int ProgramExportFull = 0;
        private const int ProgramExportIps = 1;
        private const int ProgramExportDomains = 2;
        private const int ProgramExportCustom = 3;

        private void ShowProgramExportMenu(Control anchor)
        {
            AppAggregate selected = _appsGrid != null ? _appsGrid.SelectedItem : null;
            int appId = selected != null ? selected.AppId : _appFilterId;
            string appPath = selected != null ? selected.AppPath : _appFilterName;
            if (appId <= 0)
            {
                SetStatus("Выберите программу для экспорта.");
                return;
            }

            ContextMenuStrip menu = Ui.CreateMenu();
            AddProgramExportItems(menu.Items, appId, appPath);
            menu.Show(anchor, new Point(0, anchor.Height));
        }

        private ToolStripMenuItem CreateProgramExportMenuItem(Func<int> appId, Func<string> appPath)
        {
            ToolStripMenuItem root = new ToolStripMenuItem("Экспорт IP и доменов");
            AddProgramExportItems(root.DropDownItems, appId, appPath);
            return root;
        }

        private void AddProgramExportItems(ToolStripItemCollection items, Func<int> appId, Func<string> appPath)
        {
            ToolStripMenuItem full = new ToolStripMenuItem("Полная таблица (IP, домен, трафик)…");
            ToolStripMenuItem ips = new ToolStripMenuItem("Только IP-адреса…");
            ToolStripMenuItem domains = new ToolStripMenuItem("Только домены…");
            ToolStripMenuItem custom = new ToolStripMenuItem("Свой формат (TXT/CSV, маска IP, поддомены)…");

            full.Click += delegate { ExportProgramData(appId(), appPath(), ProgramExportFull); };
            ips.Click += delegate { ExportProgramData(appId(), appPath(), ProgramExportIps); };
            domains.Click += delegate { ExportProgramData(appId(), appPath(), ProgramExportDomains); };
            custom.Click += delegate { ExportProgramData(appId(), appPath(), ProgramExportCustom); };

            items.Add(full);
            items.Add(ips);
            items.Add(domains);
            items.Add(new ToolStripSeparator());
            items.Add(custom);
        }

        private void AddProgramExportItems(ToolStripItemCollection items, int appId, string appPath)
        {
            AddProgramExportItems(items, delegate { return appId; }, delegate { return appPath; });
        }

        private void UpdateProgramExportButton()
        {
            if (_btnExportAppDomains == null) return;

            AppAggregate selected = _appsGrid != null ? _appsGrid.SelectedItem : null;
            bool hasTarget = (selected != null && selected.AppId > 0) || _appFilterId > 0;
            bool idle = !_activeOperation && !_openingDatabase;
            _btnExportAppDomains.Enabled = idle && hasTarget && _currentTab == 2;
        }

        private async void ExportProgramData(int appId, string appPath, int mode)
        {
            if (_db == null || appId <= 0 || _activeOperation || _openingDatabase) return;

            ProgramExportOptions customOptions = null;
            if (mode == ProgramExportCustom)
            {
                using (ProgramExportDialog customDialog = new ProgramExportDialog())
                {
                    if (customDialog.ShowDialog(this) != DialogResult.OK) return;
                    customOptions = customDialog.Options;
                }
            }

            string suffix;
            string title;
            switch (mode)
            {
                case ProgramExportIps:
                    suffix = "ips";
                    title = "Экспорт IP-адресов программы";
                    break;
                case ProgramExportDomains:
                    suffix = "domains";
                    title = "Экспорт доменов программы";
                    break;
                case ProgramExportCustom:
                    suffix = "custom";
                    title = "Свой формат вывода программы";
                    break;
                default:
                    suffix = "ip-domains";
                    title = "Экспорт IP и доменов программы";
                    break;
            }

            bool customTxt = customOptions != null && customOptions.TxtFormat;
            string extension = customTxt ? "txt" : "csv";
            string fileName = SafeFileNamePart(DisplayNames.FromPath(appPath)) + "-" + suffix + "-" +
                DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + "." + extension;

            string path;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = title;
                dialog.Filter = customTxt
                    ? "Текст (*.txt)|*.txt|Все файлы (*.*)|*.*"
                    : "CSV (разделитель ;)|*.csv|Все файлы (*.*)|*.*";
                dialog.DefaultExt = extension;
                dialog.AddExtension = true;
                dialog.FileName = fileName;

                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                path = dialog.FileName;
            }

            NetLimiterDatabase db = _db;
            long from = _fromMs;
            long to = _toMs;
            int proto = _protoFilter;
            CancellationTokenSource source = new CancellationTokenSource();
            _cts = source;
            CancellationToken token = source.Token;

            ShowBusy(mode == ProgramExportCustom ? "Свой формат вывода" : "Экспорт IP и доменов",
                "Чтение соединений программы…", true);

            try
            {
                List<DomainRow> rows = await Task.Run(delegate
                {
                    return db.LoadDomains(from, to, proto, appId, token);
                });

                if (_closing) return;
                token.ThrowIfCancellationRequested();

                if (rows.Count == 0)
                {
                    SetStatus("Нет данных для экспорта.");
                    MessageBox.Show(this,
                        "Для программы «" + DisplayNames.FromPath(appPath) + "» за выбранный период не найдено соединений." +
                        Environment.NewLine + Environment.NewLine +
                        "Увеличьте период или измените фильтр протокола.",
                        "Нет данных",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (mode == ProgramExportDomains)
                {
                    bool hasDomain = false;
                    foreach (DomainRow row in rows)
                    {
                        if (!string.IsNullOrEmpty(row.Domain))
                        {
                            hasDomain = true;
                            break;
                        }
                    }

                    if (!hasDomain)
                    {
                        SetStatus("Нет доменных имён для экспорта.");
                        MessageBox.Show(this,
                            "Для программы «" + DisplayNames.FromPath(appPath) + "» за выбранный период не найдено доменных имён." +
                            Environment.NewLine + Environment.NewLine +
                            "Попробуйте экспорт «Только IP-адреса» или увеличьте период.",
                            "Нет доменов",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }
                }

                string exportPath = path;
                int count = rows.Count;
                SetStatus("Запись файла…");

                if (mode == ProgramExportCustom)
                {
                    List<string> values = ProgramListExporter.Build(rows, customOptions);
                    if (values.Count == 0)
                    {
                        SetStatus("Нет данных для экспорта.");
                        MessageBox.Show(this,
                            "Для программы «" + DisplayNames.FromPath(appPath) + "» с выбранными параметрами " +
                            "за этот период не найдено значений." + Environment.NewLine + Environment.NewLine +
                            "Измените период, фильтр протокола или настройки формата.",
                            "Нет данных",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }

                    count = values.Count;
                    await Task.Run(delegate
                    {
                        ProgramListExporter.Write(values, exportPath, !customOptions.TxtFormat);
                    });
                }
                else if (mode == ProgramExportFull)
                {
                    await Task.Run(delegate
                    {
                        CsvExporter.ExportDomainRows(rows, exportPath);
                    });
                }
                else
                {
                    await Task.Run(delegate
                    {
                        count = CsvExporter.ExportDomainValues(rows, mode == ProgramExportDomains, exportPath);
                    });
                }

                SetStatus("Экспортировано: " + exportPath);

                string summary;
                if (mode == ProgramExportFull)
                    summary = "Строк в таблице: " + count.ToString("N0", CultureInfo.CurrentCulture);
                else if (mode == ProgramExportDomains)
                    summary = "Уникальных доменов: " + count.ToString("N0", CultureInfo.CurrentCulture);
                else if (mode == ProgramExportCustom)
                    summary = "Значений в файле: " + count.ToString("N0", CultureInfo.CurrentCulture);
                else
                    summary = "Уникальных IP-адресов: " + count.ToString("N0", CultureInfo.CurrentCulture);

                MessageBox.Show(this,
                    summary + "." + Environment.NewLine + exportPath,
                    "Готово",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                SetStatus("Экспорт отменён.");
            }
            catch (Exception ex)
            {
                Log.Error("Не удалось экспортировать IP и домены программы.", ex);
                SetStatus("Ошибка экспорта.");
                MessageBox.Show(this, ex.Message, "Ошибка экспорта", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (_cts == source) _cts = null;
                _activeOperation = false;
                HideBusy();
            }
        }

        private static string SafeFileNamePart(string value)
        {
            if (string.IsNullOrEmpty(value)) return "program";

            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                bool bad = c == ' ' || Array.IndexOf(invalid, c) >= 0;
                sb.Append(bad ? '_' : c);
            }

            string result = sb.ToString().Trim('_', '.', ' ');
            if (result.Length == 0) result = "program";
            if (result.Length > 48) result = result.Substring(0, 48);
            return result;
        }
    }
}
