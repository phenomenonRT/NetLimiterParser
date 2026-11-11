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
        private async void LoadConnectionsData(bool append)
        {
            if (_db == null) return;

            CancelActiveOperation();
            CancellationTokenSource source = new CancellationTokenSource();
            _cts = source;
            int version = ++_loadVersion;
            _activeOperation = true;

            NetLimiterDatabase db = _db;
            bool useCursor = append && _connectionsHasCursor;
            long cursorTime = _cursorTime;
            long cursorRowId = _cursorRowId;
            long from = _fromMs;
            long to = _toMs;
            int proto = _protoFilter;
            int appId = _appFilterId;
            string search = GetSearchText();
            string category = _categoryFilter;
            CancellationToken token = source.Token;

            ShowBusy(
                append ? "Загрузка следующей страницы" : "Загрузка соединений",
                "Чтение таблицы Cnns…",
                true);

            try
            {
                ConnectionsLoadResult result = await Task.Run(delegate
                {
                    return LoadConnectionsCore(
                        db, from, to, proto, appId,
                        useCursor, cursorTime, cursorRowId,
                        search, category, append ? 0 : -1, token);
                });

                if (version != _loadVersion || _closing) return;

                if (!append) _connectionRows.Clear();
                _connectionRows.AddRange(result.Rows);

                _connectionsLoaded = true;
                _connectionsExhausted = !result.HasMore;
                _connectionsHasCursor = result.HasMore;
                _cursorTime = result.CursorTime;
                _cursorRowId = result.CursorRowId;
                if (!append && result.Total >= 0) _totalConnections = result.Total;

                _connectionsGrid.SetItems(_connectionRows);
                UpdateConnectionsInfo(result.ScanLimited);
                SetStatus("Соединения загружены: " + Theme.FormatCount(_connectionRows.Count) + ".");
            }
            catch (OperationCanceledException)
            {
                if (version == _loadVersion) SetStatus("Загрузка соединений отменена.");
            }
            catch (Exception ex)
            {
                if (version == _loadVersion)
                {
                    Log.Error("Не удалось загрузить соединения.", ex);
                    SetStatus("Ошибка загрузки соединений.");
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

        private ConnectionsLoadResult LoadConnectionsCore(
            NetLimiterDatabase db,
            long from,
            long to,
            int proto,
            int appId,
            bool hasCursor,
            long cursorTime,
            long cursorRowId,
            string search,
            string category,
            long knownTotal,
            CancellationToken token)
        {
            ConnectionsLoadResult result = new ConnectionsLoadResult();
            result.Total = knownTotal >= 0 ? knownTotal : db.CountConnections(from, to, proto, appId);

            bool filtered = !string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(category);
            const int pageSize = 1000;
            int maxPages = filtered ? 100 : 12;
            int targetRows = filtered ? 4000 : 1500;

            bool cursor = hasCursor;
            long ct = cursorTime;
            long cr = cursorRowId;
            int pages = 0;

            while (pages < maxPages)
            {
                token.ThrowIfCancellationRequested();

                ConnectionsPageRequest request = new ConnectionsPageRequest();
                request.FromMs = from;
                request.ToMs = to;
                request.Proto = proto;
                request.AppId = appId;
                request.Limit = pageSize;
                request.HasCursor = cursor;
                request.CursorTime = ct;
                request.CursorRowId = cr;

                ConnectionsPage page = db.LoadConnectionsPage(request);
                pages++;

                if (page.Rows.Count > 0)
                {
                    foreach (ConnectionRow row in page.Rows)
                    {
                        if (MatchesConnection(row, search, category))
                            result.Rows.Add(row);
                    }

                    ct = page.LastTimeMs;
                    cr = page.LastRowId;
                    cursor = true;
                }

                result.HasMore = page.HasMore;
                result.CursorTime = ct;
                result.CursorRowId = cr;

                if (!page.HasMore) break;
                if (result.Rows.Count >= targetRows) break;
            }

            result.ScanLimited = result.HasMore && pages >= maxPages && result.Rows.Count < targetRows;
            return result;
        }

        private static bool MatchesConnection(ConnectionRow row, string search, string category)
        {
            if (!string.IsNullOrEmpty(category) &&
                !string.Equals(row.Category, category, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrEmpty(search)) return true;

            if (TextContains(row.AppPath, search)) return true;
            if (TextContains(DisplayNames.FromPath(row.AppPath), search)) return true;
            if (TextContains(row.RemoteIp, search)) return true;
            if (TextContains(row.LocalIp, search)) return true;
            if (TextContains(row.Domain, search)) return true;
            if (TextContains(row.Category, search)) return true;
            if (TextContains(Protocols.Name(row.Proto), search)) return true;
            if (TextContains(row.RemotePort.ToString(CultureInfo.InvariantCulture), search)) return true;
            if (TextContains(row.LocalPort.ToString(CultureInfo.InvariantCulture), search)) return true;
            return false;
        }

        private void UpdateConnectionsInfo(bool scanLimited)
        {
            if (_lblConnInfo == null) return;

            string text = "Показано " + Theme.FormatCount(_connectionRows.Count);
            if (_totalConnections > 0)
                text += " из " + Theme.FormatCount(_totalConnections);
            text += " соединений";

            if (!string.IsNullOrEmpty(_categoryFilter))
                text += " · категория: " + _categoryFilter;
            if (_appFilterId > 0)
                text += " · программа: " + DisplayNames.FromPath(_appFilterName);

            string search = GetSearchText();
            if (search.Length > 0)
                text += " · поиск: «" + search + "»";

            if (scanLimited)
                text += " · нажмите «Загрузить ещё» для продолжения поиска";

            _lblConnInfo.Text = text;
        }

        private async void LoadTransfersData(bool force)
        {
            if (_db == null) return;

            if (_transfersLoaded && !force)
            {
                ApplyTransferFilters();
                return;
            }

            CancelActiveOperation();
            CancellationTokenSource source = new CancellationTokenSource();
            _cts = source;
            int version = ++_loadVersion;
            _activeOperation = true;

            NetLimiterDatabase db = _db;
            DateTime fromUtc = IpTools.FromWinMilliseconds(_fromMs);
            DateTime toUtc = IpTools.FromWinMilliseconds(_toMs);
            CancellationToken token = source.Token;

            _statusProgress.Visible = true;
            _transferOverlay.ShowOverlay("Разбор таблицы Transfers", "Подготовка…", true);
            SetStatus("Разбор таблицы Transfers…");

            Progress<TransferProgress> progress = new Progress<TransferProgress>(delegate(TransferProgress value)
            {
                if (version != _loadVersion) return;
                string detail = "Записей: " + Theme.FormatCount(value.Records) +
                    " · пакетов: " + Theme.FormatCount(value.BlobsDone) + " из " + Theme.FormatCount(value.BlobsTotal);
                _transferOverlay.SetProgress(value.Percent, detail);
                SetStatus("Разбор трафика: " + value.Percent.ToString(CultureInfo.InvariantCulture) + "%");
            });

            try
            {
                TransferSummary summary = await Task.Run(delegate
                {
                    return db.LoadTransfers(fromUtc, toUtc, 0, 0, token, progress);
                });

                if (version != _loadVersion || _closing) return;

                _transferSummary = summary;
                _transfersLoaded = true;
                ApplyTransferFilters();
                SetStatus("Трафик рассчитан: " + Theme.FormatCount(summary.Groups) + " групп.");
            }
            catch (OperationCanceledException)
            {
                if (version == _loadVersion) SetStatus("Разбор трафика отменён.");
            }
            catch (Exception ex)
            {
                if (version == _loadVersion)
                {
                    Log.Error("Не удалось разобрать таблицу Transfers.", ex);
                    SetStatus("Ошибка разбора трафика.");
                    MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (version == _loadVersion)
                {
                    _activeOperation = false;
                    _statusProgress.Visible = false;
                    _transferOverlay.HideOverlay();
                    UpdateButtons();
                }
            }
        }

        private void ApplyTransferFilters()
        {
            if (_transfersGrid == null) return;

            if (_transferSummary == null)
            {
                _transfersGrid.Clear();
                if (_lblTransfersInfo != null)
                    _lblTransfersInfo.Text = "Трафик не рассчитан";
                UpdateButtons();
                return;
            }

            string search = GetSearchText();
            List<TrafficRow> filtered = new List<TrafficRow>();
            long records = 0;
            long inBytes = 0;
            long outBytes = 0;

            foreach (TrafficRow row in _transferSummary.Rows)
            {
                if (_appFilterId > 0 && row.AppId != _appFilterId) continue;
                if (!string.IsNullOrEmpty(_categoryFilter) &&
                    !string.Equals(row.Category, _categoryFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (search.Length > 0)
                {
                    bool matched = TextContains(row.AppPath, search)
                        || TextContains(DisplayNames.FromPath(row.AppPath), search)
                        || TextContains(row.RemoteIp, search)
                        || TextContains(row.LocalIp, search)
                        || TextContains(row.Domain, search)
                        || TextContains(row.Category, search)
                        || TextContains(Protocols.Name(row.Proto), search)
                        || TextContains(row.RemotePort.ToString(CultureInfo.InvariantCulture), search);
                    if (!matched) continue;
                }

                filtered.Add(row);
                records += row.RecordCount;
                inBytes += row.InBytes;
                outBytes += row.OutBytes;
            }

            _transfersGrid.SetItems(filtered);
            UpdateTransfersInfo(filtered.Count, records, inBytes, outBytes);
            UpdateButtons();
        }

        private void UpdateTransfersInfo(int groups, long records, long inBytes, long outBytes)
        {
            if (_lblTransfersInfo == null || _transferSummary == null) return;

            string text = "Групп: " + Theme.FormatCount(groups);
            if (groups != _transferSummary.Rows.Count)
                text += " из " + Theme.FormatCount(_transferSummary.Rows.Count);
            text += " · записей: " + Theme.FormatCount(records);
            text += " · Вход: " + Theme.FormatBytes(inBytes);
            text += " · Исход: " + Theme.FormatBytes(outBytes);

            if (!string.IsNullOrEmpty(_categoryFilter))
                text += " · категория: " + _categoryFilter;
            if (_appFilterId > 0)
                text += " · программа: " + DisplayNames.FromPath(_appFilterName);

            _lblTransfersInfo.Text = text;
        }
    }
}
