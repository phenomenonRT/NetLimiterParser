using System;
using System.Drawing;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed partial class MainForm
    {
        private void BuildPages()
        {
            _pageConnections = BuildConnectionsPage();
            _pageTransfers = BuildTransfersPage();
            _pageApps = BuildAppsPage();
            _pageDomains = BuildDomainsPage();
            _pageCategories = BuildCategoriesPage();

            _contentHost.Controls.Add(_pageConnections);
            _contentHost.Controls.Add(_pageTransfers);
            _contentHost.Controls.Add(_pageApps);
            _contentHost.Controls.Add(_pageDomains);
            _contentHost.Controls.Add(_pageCategories);

            _pageConnections.Visible = true;
            _pageTransfers.Visible = false;
            _pageApps.Visible = false;
            _pageDomains.Visible = false;
            _pageCategories.Visible = false;
        }

        private Panel BuildConnectionsPage()
        {
            Panel page = CreatePage();

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 98f));
            layout.Margin = new Padding(0);
            page.Controls.Add(layout);

            _lblConnInfo = Ui.Label("Соединения не загружены", Theme.FontUi, Theme.MutedDark);
            _btnLoadMore = new RoundButton();
            _btnLoadMore.Text = "Загрузить ещё";
            _btnLoadMore.Quiet = true;
            _btnLoadMore.Dock = DockStyle.Fill;
            _btnLoadMore.Margin = new Padding(0, 5, 0, 5);
            _btnLoadMore.Click += delegate { LoadConnectionsData(true); };
            layout.Controls.Add(CreateInfoRow(_lblConnInfo, "Двойной щелчок — подробности", _btnLoadMore, 140), 0, 0);

            _gridConnections = new BufferedDataGridView();
            _gridConnections.Dock = DockStyle.Fill;
            _gridConnections.Margin = new Padding(0);
            _gridConnections.EmptyText = "Нет соединений за выбранный период";
            _gridConnections.EmptyHint = "Измените период, фильтры или нажмите «Обновить»";
            _connectionsGrid = new VirtualGrid<ConnectionRow>(_gridConnections, CreateConnectionColumns());
            layout.Controls.Add(_gridConnections, 0, 1);

            CardPanel details = new CardPanel();
            details.Dock = DockStyle.Fill;
            details.Margin = new Padding(0, 8, 0, 0);
            details.Padding = new Padding(0);
            details.Radius = 10;
            layout.Controls.Add(details, 0, 2);

            _lblDetailApp = Ui.Label("Выберите соединение", Theme.FontBold, Theme.Text);
            _lblDetailApp.AutoEllipsis = true;
            details.Controls.Add(_lblDetailApp);

            _lblDetailNet = Ui.Label("", Theme.FontUiSmall, Theme.MutedDark);
            _lblDetailNet.AutoEllipsis = true;
            details.Controls.Add(_lblDetailNet);

            _lblDetailTraffic = Ui.Label("", Theme.FontUiSmall, Theme.Muted);
            _lblDetailTraffic.AutoEllipsis = true;
            details.Controls.Add(_lblDetailTraffic);

            _btnCopyConn = new RoundButton();
            _btnCopyConn.Text = "Копировать";
            _btnCopyConn.Quiet = true;
            _btnCopyConn.Size = new Size(100, 30);
            _btnCopyConn.Click += delegate { CopySelectedConnection(); };
            details.Controls.Add(_btnCopyConn);

            details.Resize += delegate
            {
                int buttonWidth = 100;
                int buttonLeft = Math.Max(16, details.ClientSize.Width - 16 - buttonWidth);
                _btnCopyConn.SetBounds(buttonLeft, 10, buttonWidth, 30);
                int labelWidth = Math.Max(120, buttonLeft - 32);
                _lblDetailApp.SetBounds(16, 7, labelWidth, 21);
                _lblDetailNet.SetBounds(16, 29, labelWidth, 20);
                _lblDetailTraffic.SetBounds(16, 50, Math.Max(120, details.ClientSize.Width - 32), 20);
            };

            _gridConnections.SelectionChanged += delegate { ShowConnectionDetails(_connectionsGrid.SelectedItem); };
            _gridConnections.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) ShowConnectionDetailsDialog();
            };
            _gridConnections.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    CopySelectedConnection();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    ShowConnectionDetailsDialog();
                    e.Handled = true;
                }
            };
            AttachConnectionsMenu(_gridConnections);

            return page;
        }

        private Panel BuildTransfersPage()
        {
            Panel page = CreatePage();

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Margin = new Padding(0);
            page.Controls.Add(layout);

            _lblTransfersInfo = Ui.Label("Трафик не рассчитан", Theme.FontUi, Theme.MutedDark);
            _btnReloadTransfers = new RoundButton();
            _btnReloadTransfers.Text = "Пересчитать";
            _btnReloadTransfers.Quiet = true;
            _btnReloadTransfers.Dock = DockStyle.Fill;
            _btnReloadTransfers.Margin = new Padding(0, 5, 0, 5);
            _btnReloadTransfers.Click += delegate { LoadTransfersData(true); };
            layout.Controls.Add(CreateInfoRow(_lblTransfersInfo, "Данные из таблицы Transfers", _btnReloadTransfers, 130), 0, 0);

            _gridTransfers = new BufferedDataGridView();
            _gridTransfers.Dock = DockStyle.Fill;
            _gridTransfers.Margin = new Padding(0);
            _gridTransfers.EmptyText = "Нет данных о трафике за выбранный период";
            _gridTransfers.EmptyHint = "Нажмите «Пересчитать», чтобы разобрать таблицу Transfers";
            _transfersGrid = new VirtualGrid<TrafficRow>(_gridTransfers, CreateTransferColumns());
            layout.Controls.Add(_gridTransfers, 0, 1);

            _gridTransfers.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) ShowTransferDetailsDialog();
            };
            _gridTransfers.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    CopySelectedTransfer();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    ShowTransferDetailsDialog();
                    e.Handled = true;
                }
            };
            AttachTransfersMenu(_gridTransfers);

            _transferOverlay = new LoadingOverlay();
            _transferOverlay.Dock = DockStyle.Fill;
            _transferOverlay.Visible = false;
            _transferOverlay.CancelRequested += delegate
            {
                if (_cts != null) _cts.Cancel();
                _transferOverlay.SetProgress(-1, "Отмена обработки…");
            };
            page.Controls.Add(_transferOverlay);
            _transferOverlay.BringToFront();

            return page;
        }

        private Panel BuildAppsPage()
        {
            Panel page = CreatePage();

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Margin = new Padding(0);
            page.Controls.Add(layout);

            _lblAppsInfo = Ui.Label("Сводка по программам не загружена", Theme.FontUi, Theme.MutedDark);
            _btnExportAppDomains = new RoundButton();
            _btnExportAppDomains.Text = "Домены и IP…";
            _btnExportAppDomains.Quiet = true;
            _btnExportAppDomains.Dock = DockStyle.Fill;
            _btnExportAppDomains.Margin = new Padding(0, 5, 0, 5);
            _btnExportAppDomains.Enabled = false;
            _btnExportAppDomains.Click += delegate { ShowProgramExportMenu(_btnExportAppDomains); };
            layout.Controls.Add(CreateInfoRow(_lblAppsInfo, "Сортировка — щелчок по заголовку столбца", _btnExportAppDomains, 150), 0, 0);

            _gridApps = new BufferedDataGridView();
            _gridApps.Dock = DockStyle.Fill;
            _gridApps.Margin = new Padding(0);
            _gridApps.EmptyText = "Нет данных по программам";
            _gridApps.EmptyHint = "Измените период или фильтры";
            _appsGrid = new VirtualGrid<AppAggregate>(_gridApps, CreateAppColumns());
            layout.Controls.Add(_gridApps, 0, 1);

            _gridApps.SelectionChanged += delegate { UpdateProgramExportButton(); };
            _gridApps.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) ShowAppDetailsDialog();
            };
            _gridApps.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    CopySelectedApp();
                    e.Handled = true;
                }
            };
            AttachAppsMenu(_gridApps);

            return page;
        }

        private Panel BuildDomainsPage()
        {
            Panel page = CreatePage();

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Margin = new Padding(0);
            page.Controls.Add(layout);

            _lblDomainsInfo = Ui.Label("Сводка по IP-адресам не загружена", Theme.FontUi, Theme.MutedDark);
            layout.Controls.Add(CreateInfoRow(_lblDomainsInfo, "Домен берётся из кэша обратных имён NetLimiter", null, 0), 0, 0);

            _gridDomains = new BufferedDataGridView();
            _gridDomains.Dock = DockStyle.Fill;
            _gridDomains.Margin = new Padding(0);
            _gridDomains.EmptyText = "Нет данных по IP-адресам";
            _gridDomains.EmptyHint = "Измените период или фильтры";
            _domainsGrid = new VirtualGrid<DomainRow>(_gridDomains, CreateDomainColumns());
            layout.Controls.Add(_gridDomains, 0, 1);

            _gridDomains.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) ShowDomainDetailsDialog();
            };
            _gridDomains.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    CopySelectedDomain();
                    e.Handled = true;
                }
            };
            AttachDomainsMenu(_gridDomains);

            return page;
        }

        private Panel CreateInfoRow(Label label, string hintText, Control rightControl, int rightWidth)
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Theme.Bg;
            panel.Margin = new Padding(0);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, rightWidth));
            layout.RowCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Margin = new Padding(0);
            panel.Controls.Add(layout);

            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.AutoEllipsis = true;
            layout.Controls.Add(label, 0, 0);

            Label hint = Ui.Label(hintText ?? "", Theme.FontUiSmall, Theme.Muted);
            hint.Dock = DockStyle.Fill;
            hint.TextAlign = ContentAlignment.MiddleRight;
            hint.AutoSize = true;
            hint.Margin = new Padding(8, 0, 10, 0);
            layout.Controls.Add(hint, 1, 0);

            if (rightControl != null)
                layout.Controls.Add(rightControl, 2, 0);

            return panel;
        }
    }
}
