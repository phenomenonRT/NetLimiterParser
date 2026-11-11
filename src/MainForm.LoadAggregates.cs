using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed partial class MainForm
    {
        private async void LoadAppsData()
        {
            if (_db == null) return;

            CancelActiveOperation();
            CancellationTokenSource source = new CancellationTokenSource();
            _cts = source;
            int version = ++_loadVersion;
            _activeOperation = true;

            NetLimiterDatabase db = _db;
            long from = _fromMs;
            long to = _toMs;
            int proto = _protoFilter;
            int appId = _appFilterId;
            CancellationToken token = source.Token;

            ShowBusy("Сводка по программам", "Агрегация таблицы Cnns…", true);

            try
            {
                List<AppAggregate> rows = await Task.Run(delegate
                {
                    return db.LoadAppSummary(from, to, proto, appId, token);
                });

                if (version != _loadVersion || _closing) return;

                _appRows = rows;
                _appsLoaded = true;
                ApplyAppFilters();
                SetStatus("Сводка по программам загружена: " + Theme.FormatCount(rows.Count) + ".");
            }
            catch (OperationCanceledException)
            {
                if (version == _loadVersion) SetStatus("Загрузка сводки отменена.");
            }
            catch (Exception ex)
            {
                if (version == _loadVersion)
                {
                    Log.Error("Не удалось загрузить сводку по программам.", ex);
                    SetStatus("Ошибка загрузки программ.");
                    MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        }

        private void ApplyAppFilters()
        {
            if (_appsGrid == null || _appRows == null) return;

            string search = GetSearchText();
            List<AppAggregate> filtered = new List<AppAggregate>();
            long connections = 0;
            long inBytes = 0;
            long outBytes = 0;
            DateTime last = DateTime.MinValue;

            foreach (AppAggregate row in _appRows)
            {
                if (!string.IsNullOrEmpty(_categoryFilter) &&
                    !string.Equals(row.Category, _categoryFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (search.Length > 0)
                {
                    bool matched = TextContains(row.AppPath, search)
                        || TextContains(DisplayNames.FromPath(row.AppPath), search)
                        || TextContains(row.Category, search);
                    if (!matched) continue;
                }

                filtered.Add(row);
                connections += row.Connections;
                inBytes += row.InBytes;
                outBytes += row.OutBytes;
                if (row.LastUtc > last) last = row.LastUtc;
            }

            _appsGrid.SetItems(filtered);
            UpdateAppsInfo(filtered.Count, connections, inBytes, outBytes);
            UpdateButtons();
        }

        private void UpdateAppsInfo(int count, long connections, long inBytes, long outBytes)
        {
            if (_lblAppsInfo == null) return;

            string text = "Программ: " + Theme.FormatCount(count);
            if (_appRows != null && count != _appRows.Count)
                text += " из " + Theme.FormatCount(_appRows.Count);
            text += " · соединений: " + Theme.FormatCount(connections);
            text += " · Вход: " + Theme.FormatBytes(inBytes);
            text += " · Исход: " + Theme.FormatBytes(outBytes);

            if (!string.IsNullOrEmpty(_categoryFilter))
                text += " · категория: " + _categoryFilter;
            if (_appFilterId > 0)
                text += " · программа: " + DisplayNames.FromPath(_appFilterName);

            _lblAppsInfo.Text = text;
        }

        private async void LoadDomainsData()
        {
            if (_db == null) return;

            CancelActiveOperation();
            CancellationTokenSource source = new CancellationTokenSource();
            _cts = source;
            int version = ++_loadVersion;
            _activeOperation = true;

            NetLimiterDatabase db = _db;
            long from = _fromMs;
            long to = _toMs;
            int proto = _protoFilter;
            int appId = _appFilterId;
            CancellationToken token = source.Token;

            ShowBusy("Сводка по IP-адресам", "Агрегация таблицы Cnns…", true);

            try
            {
                List<DomainRow> rows = await Task.Run(delegate
                {
                    return db.LoadDomains(from, to, proto, appId, token);
                });

                if (version != _loadVersion || _closing) return;

                _domainRows.Clear();
                _domainRows.AddRange(rows);
                _domainsLoaded = true;
                ApplyDomainFilters();
                SetStatus("Сводка по IP-адресам загружена: " + Theme.FormatCount(rows.Count) + ".");
            }
            catch (OperationCanceledException)
            {
                if (version == _loadVersion) SetStatus("Загрузка IP-адресов отменена.");
            }
            catch (Exception ex)
            {
                if (version == _loadVersion)
                {
                    Log.Error("Не удалось загрузить сводку по IP-адресам.", ex);
                    SetStatus("Ошибка загрузки IP-адресов.");
                    MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        }

        private void ApplyDomainFilters()
        {
            if (_domainsGrid == null) return;

            string search = GetSearchText();
            List<DomainRow> filtered = new List<DomainRow>();
            long connections = 0;
            long inBytes = 0;
            long outBytes = 0;

            foreach (DomainRow row in _domainRows)
            {
                if (!string.IsNullOrEmpty(_categoryFilter) &&
                    !string.Equals(row.Service, _categoryFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (search.Length > 0)
                {
                    bool matched = TextContains(row.Ip, search)
                        || TextContains(row.Domain, search)
                        || TextContains(row.Service, search)
                        || TextContains(row.Protocols, search);
                    if (!matched) continue;
                }

                filtered.Add(row);
                connections += row.Connections;
                inBytes += row.InBytes;
                outBytes += row.OutBytes;
            }

            _domainsGrid.SetItems(filtered);
            UpdateDomainsInfo(filtered.Count, connections, inBytes, outBytes);
            UpdateButtons();
        }

        private void UpdateDomainsInfo(int count, long connections, long inBytes, long outBytes)
        {
            if (_lblDomainsInfo == null) return;

            string text = "IP-адресов: " + Theme.FormatCount(count);
            if (count != _domainRows.Count)
                text += " из " + Theme.FormatCount(_domainRows.Count);
            text += " · соединений: " + Theme.FormatCount(connections);
            text += " · Вход: " + Theme.FormatBytes(inBytes);
            text += " · Исход: " + Theme.FormatBytes(outBytes);

            if (!string.IsNullOrEmpty(_categoryFilter))
                text += " · категория: " + _categoryFilter;
            if (_appFilterId > 0)
                text += " · программа: " + DisplayNames.FromPath(_appFilterName);

            _lblDomainsInfo.Text = text;
        }

        private void ApplyCurrentFilters()
        {
            if (_db == null)
            {
                UpdateButtons();
                return;
            }

            switch (_currentTab)
            {
                case 0:
                    LoadConnectionsData(false);
                    break;
                case 1:
                    if (_transfersLoaded) ApplyTransferFilters();
                    else LoadTransfersData(false);
                    break;
                case 2:
                    if (_appsLoaded) ApplyAppFilters();
                    else LoadAppsData();
                    break;
                case 3:
                    if (_domainsLoaded) ApplyDomainFilters();
                    else LoadDomainsData();
                    break;
            }

            UpdateButtons();
        }

        private void ProtocolFilterChanged()
        {
            if (_suppressFilters || _cmbProto == null) return;

            switch (_cmbProto.SelectedIndex)
            {
                case 1: _protoFilter = 6; break;
                case 2: _protoFilter = 17; break;
                case 3: _protoFilter = 1; break;
                case 4: _protoFilter = 58; break;
                default: _protoFilter = 0; break;
            }

            ReloadForServerFilterChange();
        }

        private void CategoryFilterChanged()
        {
            if (_suppressFilters || _cmbCategory == null) return;

            if (_cmbCategory.SelectedIndex <= 0)
                _categoryFilter = null;
            else
                _categoryFilter = _cmbCategory.SelectedItem as string;

            ApplyCurrentFilters();
        }

        private void ReloadForServerFilterChange()
        {
            switch (_currentTab)
            {
                case 0:
                    LoadConnectionsData(false);
                    break;
                case 1:
                    ApplyTransferFilters();
                    break;
                case 2:
                    _appsLoaded = false;
                    LoadAppsData();
                    break;
                case 3:
                    _domainsLoaded = false;
                    LoadDomainsData();
                    break;
            }
        }

        private void SetAppFilter(int appId, string appPath)
        {
            if (appId <= 0) return;

            _appFilterId = appId;
            _appFilterName = appPath ?? "";

            string name = DisplayNames.FromPath(appPath);
            if (name.Length > 34) name = name.Substring(0, 31) + "…";
            _chipAppFilter.Text = "Программа: " + name + "  ×";
            _chipAppFilter.Visible = true;
            _tips.SetToolTip(_chipAppFilter, appPath + Environment.NewLine + "Нажмите, чтобы снять фильтр");
            ReloadForServerFilterChange();
        }

        private void ClearAppFilter()
        {
            if (_appFilterId == 0 && !_chipAppFilter.Visible) return;

            _appFilterId = 0;
            _appFilterName = null;
            _chipAppFilter.Visible = false;
            _chipAppFilter.Text = "";
            ReloadForServerFilterChange();
        }

        private void ResetFilters()
        {
            _suppressFilters = true;
            try
            {
                _search.Text = "";
                _searchTimer.Stop();
                if (_cmbProto.Items.Count > 0) _cmbProto.SelectedIndex = 0;
                if (_cmbCategory.Items.Count > 0) _cmbCategory.SelectedIndex = 0;
            }
            finally
            {
                _suppressFilters = false;
            }

            _protoFilter = 0;
            _categoryFilter = null;
            _appFilterId = 0;
            _appFilterName = null;
            if (_chipAppFilter != null)
            {
                _chipAppFilter.Visible = false;
                _chipAppFilter.Text = "";
            }

            ApplyCurrentFilters();
            SetStatus("Фильтры сброшены.");
        }
    }
}
