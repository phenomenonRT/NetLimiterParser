using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed partial class MainForm
    {
        private sealed class DatabaseLoadResult
        {
            public NetLimiterDatabase Database;
            public DbStats Stats;
        }

        private sealed class ConnectionsLoadResult
        {
            public List<ConnectionRow> Rows;
            public long Total;
            public bool HasMore;
            public bool ScanLimited;
            public long CursorTime;
            public long CursorRowId;

            public ConnectionsLoadResult()
            {
                Rows = new List<ConnectionRow>();
            }
        }

        private void StartDatabaseInitialization()
        {
            string resolved;
            if (AppDefaults.TryResolveDatabase(_settings.LastDbPath, out resolved))
            {
                if (!string.Equals(resolved, _settings.LastDbPath, StringComparison.OrdinalIgnoreCase))
                {
                    _settings.LastDbPath = resolved;
                    SettingsStore.Save(_settings);
                    UpdateHeaderPaths();
                }

                BeginDatabaseInitialization(resolved);
                return;
            }

            SetStatus("База NetLimiter не найдена. Укажите файл nlstats.db.");
            ShowOpenDatabaseDialog();

            if (_db == null)
                SetStatus("База NetLimiter не найдена — нажмите «Открыть БД…» и выберите файл.");
        }

        private void BeginDatabaseInitialization(string path)
        {
            OpenDatabaseAsync(path, true);
        }

        private async void OpenDatabaseAsync(string path, bool savePath)
        {
            if (_openingDatabase)
            {
                SetStatus("База данных уже загружается…");
                return;
            }

            _openingDatabase = true;
            NetLimiterDatabase newDatabase = null;
            bool success = false;

            try
            {
                CancelActiveOperation();
                int version = ++_loadVersion;
                ShowBusy("Загрузка базы данных", "Открытие " + path, false);

                string dbPath = path;
                DatabaseLoadResult result = await Task.Run(delegate
                {
                    DatabaseLoadResult loaded = new DatabaseLoadResult();
                    loaded.Database = NetLimiterDatabase.Open(dbPath, _classifier, delegate(int percent, string message)
                    {
                        PostToUi(delegate
                        {
                            if (_overlay != null && _overlay.Visible)
                                _overlay.SetProgress(percent, message);
                            SetStatus(message);
                        });
                    });
                    loaded.Stats = loaded.Database.LoadStats();
                    return loaded;
                });

                if (version != _loadVersion || _closing)
                {
                    if (result != null && result.Database != null)
                        result.Database.Dispose();
                    return;
                }

                newDatabase = result.Database;
                NetLimiterDatabase old = _db;
                _db = newDatabase;
                _stats = result.Stats;
                newDatabase = null;
                if (old != null)
                {
                    try { old.Dispose(); }
                    catch { }
                }

                _settings.LastDbPath = dbPath;
                if (savePath) SettingsStore.Save(_settings);

                ResetLoadedFlags();
                UpdateHeaderPaths();
                UpdateDbStatsDisplay();
                SelectPeriodChip(_settings.PeriodMinutes, false);
                UpdatePeriodLabel();
                UpdateButtons();
                success = true;
                SetStatus("База данных загружена.");
            }
            catch (Exception ex)
            {
                if (newDatabase != null)
                {
                    try { newDatabase.Dispose(); }
                    catch { }
                }

                Log.Error("Не удалось открыть базу данных.", ex);
                SetStatus("Не удалось открыть базу данных.");
                MessageBox.Show(
                    this,
                    ex.Message + Environment.NewLine + Environment.NewLine +
                    "Можно выбрать другой файл кнопкой «Открыть БД…»." + Environment.NewLine +
                    "Подробности записаны в журнал: " + AppPaths.LogFile,
                    "Ошибка базы данных",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _openingDatabase = false;
                _activeOperation = false;
                HideBusy();
                UpdateButtons();
            }

            if (success && !_closing)
            {
                SelectTab(_settings.LastTab);
            }
        }

        private void PostToUi(MethodInvoker action)
        {
            if (action == null) return;
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void CancelActiveOperation()
        {
            CancellationTokenSource source = _cts;
            _cts = null;
            if (source == null) return;
            try { source.Cancel(); }
            catch (ObjectDisposedException) { }
        }

        private void ShowBusy(string title, string detail, bool allowCancel)
        {
            _activeOperation = true;
            if (_btnRefresh != null) _btnRefresh.Enabled = false;
            if (_btnOpenDb != null) _btnOpenDb.Enabled = false;
            if (_btnExport != null) _btnExport.Enabled = false;
            if (_btnLoadMore != null) _btnLoadMore.Enabled = false;
            if (_statusProgress != null) _statusProgress.Visible = true;
            if (_overlay != null) _overlay.ShowOverlay(title, detail, allowCancel);
            SetStatus(title);
        }

        private void HideBusy()
        {
            if (_statusProgress != null) _statusProgress.Visible = _activeOperation;
            if (_overlay != null && !_activeOperation) _overlay.HideOverlay();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            if (_btnExport == null) return;

            bool idle = !_activeOperation && !_openingDatabase;
            bool hasData = false;

            switch (_currentTab)
            {
                case 0: hasData = _connectionsGrid != null && _connectionsGrid.Count > 0; break;
                case 1: hasData = _transfersGrid != null && _transfersGrid.Count > 0; break;
                case 2: hasData = _appsGrid != null && _appsGrid.Count > 0; break;
                case 3: hasData = _domainsGrid != null && _domainsGrid.Count > 0; break;
                case 4: hasData = true; break;
            }

            _btnExport.Enabled = idle && hasData;
            _btnRefresh.Enabled = idle;
            _btnOpenDb.Enabled = !_openingDatabase;
            _btnResetFilters.Visible = _currentTab != 4;
            _btnResetFilters.Enabled = idle;
            _search.Visible = _currentTab != 4;
            _search.Enabled = _currentTab != 4;
            _cmbProto.Enabled = idle && _currentTab != 4;
            _cmbCategory.Enabled = idle && _currentTab != 4;
            _btnLoadMore.Enabled = idle && _connectionsLoaded && !_connectionsExhausted;
            _btnLoadMore.Visible = _currentTab == 0 && !_connectionsExhausted;
            UpdateProgramExportButton();
        }

        private void ResetLoadedFlags()
        {
            _connectionsLoaded = false;
            _connectionsExhausted = false;
            _connectionsHasCursor = false;
            _cursorTime = 0;
            _cursorRowId = 0;
            _transfersLoaded = false;
            _appsLoaded = false;
            _domainsLoaded = false;
        }

        private void SetPeriod(int minutes)
        {
            _settings.PeriodMinutes = minutes;
            SettingsStore.Save(_settings);
            SelectPeriodChip(minutes, true);
        }

        private void SelectPeriodChip(int minutes, bool reload)
        {
            int selected = -1;
            for (int i = 0; i < PeriodValues.Length; i++)
            {
                if (PeriodValues[i] == minutes)
                {
                    selected = i;
                    break;
                }
            }
            if (selected < 0) selected = 4;

            if (_periodChips != null)
            {
                for (int i = 0; i < _periodChips.Length; i++)
                    _periodChips[i].Checked = i == selected;
            }

            _settings.PeriodMinutes = PeriodValues[selected];
            UpdatePeriodRange(PeriodValues[selected]);

            if (!reload) return;

            ResetLoadedFlags();
            UpdatePeriodLabel();
            UpdateButtons();
            LoadCurrentTabData();
        }

        private void UpdatePeriodRange(int minutes)
        {
            DateTime end;
            if (_stats != null && _stats.DataMaxUtc > DateTime.MinValue)
                end = _stats.DataMaxUtc;
            else
                end = DateTime.UtcNow;

            DateTime start;
            if (minutes < 0)
            {
                if (_stats != null && _stats.DataMinUtc > DateTime.MinValue)
                    start = _stats.DataMinUtc;
                else
                    start = end.AddDays(-30);
            }
            else
            {
                start = end.AddMinutes(-minutes);
            }

            _fromMs = IpTools.ToWinMilliseconds(start);
            _toMs = IpTools.ToWinMilliseconds(end.AddSeconds(1));
        }

        private void UpdatePeriodLabel()
        {
            if (_lblPeriodInfo == null) return;
            if (_fromMs == 0 && _toMs == 0)
            {
                _lblPeriodInfo.Text = "";
                return;
            }

            DateTime from = IpTools.FromWinMilliseconds(_fromMs).ToLocalTime();
            DateTime to = IpTools.FromWinMilliseconds(_toMs).ToLocalTime();
            string text = "Период: " + from.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture) +
                " — " + to.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture);
            _lblPeriodInfo.Text = text;
            _tips.SetToolTip(_lblPeriodInfo, text);
        }

        private void UpdateDbStatsDisplay()
        {
            if (_lblDbStats == null) return;
            if (_stats == null)
            {
                _lblDbStats.Text = "База данных не загружена";
                return;
            }

            string line1 = Theme.FormatCount(_stats.ConnectionCount) + " соединений · " +
                Theme.FormatCount(_stats.TransferCount) + " записей трафика";
            string line2 = Theme.FormatCount(_stats.AppCount) + " программ · " + Theme.FormatBytes(_stats.FileSize);
            if (_stats.IsSnapshot) line2 += " · снимок";
            _lblDbStats.Text = line1 + Environment.NewLine + line2;
            _tips.SetToolTip(_lblDbStats,
                "Файл: " + _stats.EffectivePath + Environment.NewLine +
                "Диапазон данных: " + IpTools.FormatLocal(_stats.DataMinUtc) + " — " + IpTools.FormatLocal(_stats.DataMaxUtc));

            if (_lblStatusRight != null)
            {
                _lblStatusRight.Text = "SQLite " + NativeSqlite.Version() + " · " +
                    Theme.FormatCount(_stats.ConnectionCount) + " соединений";
            }
        }

        private void SelectTab(int index)
        {
            if (_tabs == null || index < 0 || index >= _tabs.Length) return;

            CancelActiveOperation();
            _loadVersion++;
            _activeOperation = false;
            if (_overlay != null) _overlay.HideOverlay();
            if (_transferOverlay != null) _transferOverlay.HideOverlay();
            if (_statusProgress != null) _statusProgress.Visible = false;

            _currentTab = index;
            for (int i = 0; i < _tabs.Length; i++)
                _tabs[i].Checked = i == index;

            if (_pageConnections != null) _pageConnections.Visible = index == 0;
            if (_pageTransfers != null) _pageTransfers.Visible = index == 1;
            if (_pageApps != null) _pageApps.Visible = index == 2;
            if (_pageDomains != null) _pageDomains.Visible = index == 3;
            if (_pageCategories != null) _pageCategories.Visible = index == 4;

            UpdateButtons();

            switch (index)
            {
                case 0:
                    if (_db != null && !_connectionsLoaded) LoadConnectionsData(false);
                    break;
                case 1:
                    if (_db != null && !_transfersLoaded) LoadTransfersData(false);
                    break;
                case 2:
                    if (_db != null && !_appsLoaded) LoadAppsData();
                    break;
                case 3:
                    if (_db != null && !_domainsLoaded) LoadDomainsData();
                    break;
                case 4:
                    RefreshCategoryEditor();
                    SetStatus("Категории: " + Theme.FormatCount(_categories.Count) + " · правила можно менять и сохранять.");
                    break;
            }
        }

        private void LoadCurrentTabData()
        {
            if (_db == null) return;
            switch (_currentTab)
            {
                case 0: LoadConnectionsData(false); break;
                case 1: LoadTransfersData(false); break;
                case 2: LoadAppsData(); break;
                case 3: LoadDomainsData(); break;
            }
        }

        private async void RefreshDataAsync()
        {
            if (_openingDatabase) return;
            if (_db == null)
            {
                StartDatabaseInitialization();
                return;
            }
            if (_activeOperation) return;

            NetLimiterDatabase db = _db;
            CancellationTokenSource source = new CancellationTokenSource();
            _cts = source;
            _activeOperation = true;
            int version = ++_loadVersion;
            bool success = false;

            ShowBusy("Обновление данных", "Чтение статистики базы…", true);

            try
            {
                DbStats stats = await Task.Run(delegate { return db.LoadStats(); });
                if (version != _loadVersion || _closing) return;

                _stats = stats;
                UpdateDbStatsDisplay();
                SelectPeriodChip(_settings.PeriodMinutes, false);
                UpdatePeriodLabel();
                ResetLoadedFlags();
                success = true;
                SetStatus("Статистика обновлена.");
            }
            catch (OperationCanceledException)
            {
                if (version == _loadVersion) SetStatus("Обновление отменено.");
            }
            catch (Exception ex)
            {
                Log.Error("Не удалось обновить статистику базы.", ex);
                if (version == _loadVersion)
                {
                    SetStatus("Ошибка обновления.");
                    MessageBox.Show(this, ex.Message, "Ошибка обновления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (version == _loadVersion)
                {
                    _activeOperation = false;
                    HideBusy();
                }
            }

            if (success && version == _loadVersion && !_closing)
                LoadCurrentTabData();
        }

        private void ShowOpenDatabaseDialog()
        {
            if (_openingDatabase || _activeOperation)
            {
                SetStatus("Дождитесь завершения текущей операции.");
                return;
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Выберите базу статистики NetLimiter";
                dialog.Filter = "База NetLimiter (*.db)|*.db|Все файлы (*.*)|*.*";
                dialog.CheckFileExists = true;
                dialog.Multiselect = false;

                string current = _settings.LastDbPath;
                try
                {
                    if (!string.IsNullOrWhiteSpace(current) && File.Exists(current))
                    {
                        dialog.InitialDirectory = Path.GetDirectoryName(current);
                        dialog.FileName = Path.GetFileName(current);
                    }
                }
                catch
                {
                }

                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                OpenDatabaseAsync(dialog.FileName, true);
            }
        }

        private string GetSearchText()
        {
            if (_search == null) return "";
            return (_search.Text ?? "").Trim();
        }

        private static bool TextContains(string text, string search)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
