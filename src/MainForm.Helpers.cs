using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed partial class MainForm
    {
        private List<GridColumn<ConnectionRow>> CreateConnectionColumns()
        {
            List<GridColumn<ConnectionRow>> columns = new List<GridColumn<ConnectionRow>>();

            GridColumn<ConnectionRow> time = new GridColumn<ConnectionRow>();
            time.Header = "Время";
            time.Width = 140;
            time.Value = delegate(ConnectionRow row) { return FormatUtc(row.TimeUtc); };
            time.SortKey = delegate(ConnectionRow row) { return row.TimeUtc; };
            time.DescendingFirst = true;
            columns.Add(time);

            GridColumn<ConnectionRow> duration = new GridColumn<ConnectionRow>();
            duration.Header = "Длит.";
            duration.Width = 76;
            duration.Alignment = DataGridViewContentAlignment.MiddleRight;
            duration.Value = delegate(ConnectionRow row) { return Theme.FormatDuration(row.DurationSec); };
            duration.SortKey = delegate(ConnectionRow row) { return row.DurationSec; };
            duration.DescendingFirst = true;
            columns.Add(duration);

            GridColumn<ConnectionRow> app = new GridColumn<ConnectionRow>();
            app.Header = "Программа";
            app.Width = 175;
            app.Bold = true;
            app.Value = delegate(ConnectionRow row) { return DisplayNames.FromPath(row.AppPath); };
            app.ToolTip = delegate(ConnectionRow row) { return row.AppPath; };
            app.SortKey = delegate(ConnectionRow row) { return DisplayNames.FromPath(row.AppPath); };
            columns.Add(app);

            GridColumn<ConnectionRow> category = new GridColumn<ConnectionRow>();
            category.Header = "Категория";
            category.Width = 135;
            category.Value = delegate(ConnectionRow row) { return row.Category; };
            category.Background = delegate(ConnectionRow row) { return CategoryBackground(row.Category); };
            category.Foreground = delegate(ConnectionRow row) { return CategoryForeground(row.Category); };
            category.SortKey = delegate(ConnectionRow row) { return row.Category; };
            columns.Add(category);

            GridColumn<ConnectionRow> protocol = new GridColumn<ConnectionRow>();
            protocol.Header = "Протокол";
            protocol.Width = 84;
            protocol.Value = delegate(ConnectionRow row) { return Protocols.Name(row.Proto); };
            protocol.SortKey = delegate(ConnectionRow row) { return Protocols.Name(row.Proto); };
            columns.Add(protocol);

            GridColumn<ConnectionRow> local = new GridColumn<ConnectionRow>();
            local.Header = "Локальный адрес";
            local.Width = 158;
            local.Value = delegate(ConnectionRow row) { return EndPoint(row.LocalIp, row.LocalPort, row.IsIpv6); };
            local.SortKey = delegate(ConnectionRow row) { return row.LocalIp; };
            columns.Add(local);

            GridColumn<ConnectionRow> remote = new GridColumn<ConnectionRow>();
            remote.Header = "Удалённый адрес";
            remote.Width = 172;
            remote.Value = delegate(ConnectionRow row) { return EndPoint(row.RemoteIp, row.RemotePort, row.IsIpv6); };
            remote.SortKey = delegate(ConnectionRow row) { return row.RemoteIp; };
            columns.Add(remote);

            GridColumn<ConnectionRow> domain = new GridColumn<ConnectionRow>();
            domain.Header = "Домен";
            domain.Width = 210;
            domain.Fill = true;
            domain.FillWeight = 120f;
            domain.Value = delegate(ConnectionRow row) { return string.IsNullOrEmpty(row.Domain) ? "—" : row.Domain; };
            domain.ToolTip = delegate(ConnectionRow row) { return row.Domain; };
            domain.SortKey = delegate(ConnectionRow row) { return row.Domain; };
            columns.Add(domain);

            GridColumn<ConnectionRow> inBytes = new GridColumn<ConnectionRow>();
            inBytes.Header = "Вход";
            inBytes.Width = 92;
            inBytes.Alignment = DataGridViewContentAlignment.MiddleRight;
            inBytes.Value = delegate(ConnectionRow row) { return Theme.FormatBytes(row.InBytes); };
            inBytes.SortKey = delegate(ConnectionRow row) { return row.InBytes; };
            inBytes.DescendingFirst = true;
            columns.Add(inBytes);

            GridColumn<ConnectionRow> outBytes = new GridColumn<ConnectionRow>();
            outBytes.Header = "Исход";
            outBytes.Width = 92;
            outBytes.Alignment = DataGridViewContentAlignment.MiddleRight;
            outBytes.Value = delegate(ConnectionRow row) { return Theme.FormatBytes(row.OutBytes); };
            outBytes.SortKey = delegate(ConnectionRow row) { return row.OutBytes; };
            outBytes.DescendingFirst = true;
            columns.Add(outBytes);

            GridColumn<ConnectionRow> total = new GridColumn<ConnectionRow>();
            total.Header = "Всего";
            total.Width = 92;
            total.Alignment = DataGridViewContentAlignment.MiddleRight;
            total.Value = delegate(ConnectionRow row) { return Theme.FormatBytes(row.InBytes + row.OutBytes); };
            total.SortKey = delegate(ConnectionRow row) { return row.InBytes + row.OutBytes; };
            total.DescendingFirst = true;
            columns.Add(total);

            return columns;
        }

        private List<GridColumn<TrafficRow>> CreateTransferColumns()
        {
            List<GridColumn<TrafficRow>> columns = new List<GridColumn<TrafficRow>>();

            GridColumn<TrafficRow> app = new GridColumn<TrafficRow>();
            app.Header = "Программа";
            app.Width = 170;
            app.Bold = true;
            app.Value = delegate(TrafficRow row) { return DisplayNames.FromPath(row.AppPath); };
            app.ToolTip = delegate(TrafficRow row) { return row.AppPath; };
            app.SortKey = delegate(TrafficRow row) { return DisplayNames.FromPath(row.AppPath); };
            columns.Add(app);

            GridColumn<TrafficRow> category = new GridColumn<TrafficRow>();
            category.Header = "Категория";
            category.Width = 135;
            category.Value = delegate(TrafficRow row) { return row.Category; };
            category.Background = delegate(TrafficRow row) { return CategoryBackground(row.Category); };
            category.Foreground = delegate(TrafficRow row) { return CategoryForeground(row.Category); };
            category.SortKey = delegate(TrafficRow row) { return row.Category; };
            columns.Add(category);

            GridColumn<TrafficRow> protocol = new GridColumn<TrafficRow>();
            protocol.Header = "Протокол";
            protocol.Width = 84;
            protocol.Value = delegate(TrafficRow row) { return Protocols.Name(row.Proto); };
            protocol.SortKey = delegate(TrafficRow row) { return Protocols.Name(row.Proto); };
            columns.Add(protocol);

            GridColumn<TrafficRow> local = new GridColumn<TrafficRow>();
            local.Header = "Локальный IP";
            local.Width = 150;
            local.Value = delegate(TrafficRow row) { return row.LocalIp; };
            local.SortKey = delegate(TrafficRow row) { return row.LocalIp; };
            columns.Add(local);

            GridColumn<TrafficRow> remote = new GridColumn<TrafficRow>();
            remote.Header = "Удалённый адрес";
            remote.Width = 172;
            remote.Value = delegate(TrafficRow row) { return EndPoint(row.RemoteIp, row.RemotePort, row.IsIpv6); };
            remote.SortKey = delegate(TrafficRow row) { return row.RemoteIp; };
            columns.Add(remote);

            GridColumn<TrafficRow> domain = new GridColumn<TrafficRow>();
            domain.Header = "Домен";
            domain.Width = 210;
            domain.Fill = true;
            domain.FillWeight = 120f;
            domain.Value = delegate(TrafficRow row) { return string.IsNullOrEmpty(row.Domain) ? "—" : row.Domain; };
            domain.ToolTip = delegate(TrafficRow row) { return row.Domain; };
            domain.SortKey = delegate(TrafficRow row) { return row.Domain; };
            columns.Add(domain);

            GridColumn<TrafficRow> inBytes = new GridColumn<TrafficRow>();
            inBytes.Header = "Вход";
            inBytes.Width = 92;
            inBytes.Alignment = DataGridViewContentAlignment.MiddleRight;
            inBytes.Value = delegate(TrafficRow row) { return Theme.FormatBytes(row.InBytes); };
            inBytes.SortKey = delegate(TrafficRow row) { return row.InBytes; };
            inBytes.DescendingFirst = true;
            columns.Add(inBytes);

            GridColumn<TrafficRow> outBytes = new GridColumn<TrafficRow>();
            outBytes.Header = "Исход";
            outBytes.Width = 92;
            outBytes.Alignment = DataGridViewContentAlignment.MiddleRight;
            outBytes.Value = delegate(TrafficRow row) { return Theme.FormatBytes(row.OutBytes); };
            outBytes.SortKey = delegate(TrafficRow row) { return row.OutBytes; };
            outBytes.DescendingFirst = true;
            columns.Add(outBytes);

            GridColumn<TrafficRow> total = new GridColumn<TrafficRow>();
            total.Header = "Всего";
            total.Width = 92;
            total.Alignment = DataGridViewContentAlignment.MiddleRight;
            total.Value = delegate(TrafficRow row) { return Theme.FormatBytes(row.TotalBytes); };
            total.SortKey = delegate(TrafficRow row) { return row.TotalBytes; };
            total.DescendingFirst = true;
            columns.Add(total);

            GridColumn<TrafficRow> records = new GridColumn<TrafficRow>();
            records.Header = "Записей";
            records.Width = 82;
            records.Alignment = DataGridViewContentAlignment.MiddleRight;
            records.Value = delegate(TrafficRow row) { return Theme.FormatCount(row.RecordCount); };
            records.SortKey = delegate(TrafficRow row) { return row.RecordCount; };
            records.DescendingFirst = true;
            columns.Add(records);

            GridColumn<TrafficRow> first = new GridColumn<TrafficRow>();
            first.Header = "Первое";
            first.Width = 138;
            first.Value = delegate(TrafficRow row) { return FormatUtc(row.FirstUtc); };
            first.SortKey = delegate(TrafficRow row) { return row.FirstUtc; };
            first.DescendingFirst = true;
            columns.Add(first);

            GridColumn<TrafficRow> last = new GridColumn<TrafficRow>();
            last.Header = "Последнее";
            last.Width = 138;
            last.Value = delegate(TrafficRow row) { return FormatUtc(row.LastUtc); };
            last.SortKey = delegate(TrafficRow row) { return row.LastUtc; };
            last.DescendingFirst = true;
            columns.Add(last);

            return columns;
        }

        private List<GridColumn<AppAggregate>> CreateAppColumns()
        {
            List<GridColumn<AppAggregate>> columns = new List<GridColumn<AppAggregate>>();

            GridColumn<AppAggregate> app = new GridColumn<AppAggregate>();
            app.Header = "Программа";
            app.Width = 260;
            app.Fill = true;
            app.FillWeight = 150f;
            app.Bold = true;
            app.Value = delegate(AppAggregate row) { return DisplayNames.FromPath(row.AppPath); };
            app.ToolTip = delegate(AppAggregate row) { return row.AppPath; };
            app.SortKey = delegate(AppAggregate row) { return DisplayNames.FromPath(row.AppPath); };
            columns.Add(app);

            GridColumn<AppAggregate> category = new GridColumn<AppAggregate>();
            category.Header = "Категория";
            category.Width = 150;
            category.Value = delegate(AppAggregate row) { return row.Category; };
            category.Background = delegate(AppAggregate row) { return CategoryBackground(row.Category); };
            category.Foreground = delegate(AppAggregate row) { return CategoryForeground(row.Category); };
            category.SortKey = delegate(AppAggregate row) { return row.Category; };
            columns.Add(category);

            GridColumn<AppAggregate> connections = new GridColumn<AppAggregate>();
            connections.Header = "Соединений";
            connections.Width = 110;
            connections.Alignment = DataGridViewContentAlignment.MiddleRight;
            connections.Value = delegate(AppAggregate row) { return Theme.FormatCount(row.Connections); };
            connections.SortKey = delegate(AppAggregate row) { return row.Connections; };
            connections.DescendingFirst = true;
            columns.Add(connections);

            GridColumn<AppAggregate> inBytes = new GridColumn<AppAggregate>();
            inBytes.Header = "Вход";
            inBytes.Width = 100;
            inBytes.Alignment = DataGridViewContentAlignment.MiddleRight;
            inBytes.Value = delegate(AppAggregate row) { return Theme.FormatBytes(row.InBytes); };
            inBytes.SortKey = delegate(AppAggregate row) { return row.InBytes; };
            inBytes.DescendingFirst = true;
            columns.Add(inBytes);

            GridColumn<AppAggregate> outBytes = new GridColumn<AppAggregate>();
            outBytes.Header = "Исход";
            outBytes.Width = 100;
            outBytes.Alignment = DataGridViewContentAlignment.MiddleRight;
            outBytes.Value = delegate(AppAggregate row) { return Theme.FormatBytes(row.OutBytes); };
            outBytes.SortKey = delegate(AppAggregate row) { return row.OutBytes; };
            outBytes.DescendingFirst = true;
            columns.Add(outBytes);

            GridColumn<AppAggregate> total = new GridColumn<AppAggregate>();
            total.Header = "Всего";
            total.Width = 100;
            total.Alignment = DataGridViewContentAlignment.MiddleRight;
            total.Value = delegate(AppAggregate row) { return Theme.FormatBytes(row.InBytes + row.OutBytes); };
            total.SortKey = delegate(AppAggregate row) { return row.InBytes + row.OutBytes; };
            total.DescendingFirst = true;
            columns.Add(total);

            GridColumn<AppAggregate> unique = new GridColumn<AppAggregate>();
            unique.Header = "Уникальных IP";
            unique.Width = 112;
            unique.Alignment = DataGridViewContentAlignment.MiddleRight;
            unique.Value = delegate(AppAggregate row) { return Theme.FormatCount(row.UniqueRemoteIps); };
            unique.SortKey = delegate(AppAggregate row) { return row.UniqueRemoteIps; };
            unique.DescendingFirst = true;
            columns.Add(unique);

            GridColumn<AppAggregate> last = new GridColumn<AppAggregate>();
            last.Header = "Последняя активность";
            last.Width = 158;
            last.Value = delegate(AppAggregate row) { return FormatUtc(row.LastUtc); };
            last.SortKey = delegate(AppAggregate row) { return row.LastUtc; };
            last.DescendingFirst = true;
            columns.Add(last);

            return columns;
        }

        private List<GridColumn<DomainRow>> CreateDomainColumns()
        {
            List<GridColumn<DomainRow>> columns = new List<GridColumn<DomainRow>>();

            GridColumn<DomainRow> ip = new GridColumn<DomainRow>();
            ip.Header = "IP-адрес";
            ip.Width = 205;
            ip.Bold = true;
            ip.Value = delegate(DomainRow row) { return row.Ip; };
            ip.SortKey = delegate(DomainRow row) { return row.Ip; };
            columns.Add(ip);

            GridColumn<DomainRow> domain = new GridColumn<DomainRow>();
            domain.Header = "Домен";
            domain.Width = 250;
            domain.Fill = true;
            domain.FillWeight = 140f;
            domain.Value = delegate(DomainRow row) { return string.IsNullOrEmpty(row.Domain) ? "—" : row.Domain; };
            domain.ToolTip = delegate(DomainRow row) { return row.Domain; };
            domain.SortKey = delegate(DomainRow row) { return row.Domain; };
            columns.Add(domain);

            GridColumn<DomainRow> service = new GridColumn<DomainRow>();
            service.Header = "Категория";
            service.Width = 150;
            service.Value = delegate(DomainRow row) { return row.Service; };
            service.Background = delegate(DomainRow row) { return CategoryBackground(row.Service); };
            service.Foreground = delegate(DomainRow row) { return CategoryForeground(row.Service); };
            service.SortKey = delegate(DomainRow row) { return row.Service; };
            columns.Add(service);

            GridColumn<DomainRow> protocols = new GridColumn<DomainRow>();
            protocols.Header = "Протоколы";
            protocols.Width = 110;
            protocols.Value = delegate(DomainRow row) { return row.Protocols; };
            protocols.SortKey = delegate(DomainRow row) { return row.Protocols; };
            columns.Add(protocols);

            GridColumn<DomainRow> connections = new GridColumn<DomainRow>();
            connections.Header = "Соединений";
            connections.Width = 108;
            connections.Alignment = DataGridViewContentAlignment.MiddleRight;
            connections.Value = delegate(DomainRow row) { return Theme.FormatCount(row.Connections); };
            connections.SortKey = delegate(DomainRow row) { return row.Connections; };
            connections.DescendingFirst = true;
            columns.Add(connections);

            GridColumn<DomainRow> inBytes = new GridColumn<DomainRow>();
            inBytes.Header = "Вход";
            inBytes.Width = 98;
            inBytes.Alignment = DataGridViewContentAlignment.MiddleRight;
            inBytes.Value = delegate(DomainRow row) { return Theme.FormatBytes(row.InBytes); };
            inBytes.SortKey = delegate(DomainRow row) { return row.InBytes; };
            inBytes.DescendingFirst = true;
            columns.Add(inBytes);

            GridColumn<DomainRow> outBytes = new GridColumn<DomainRow>();
            outBytes.Header = "Исход";
            outBytes.Width = 98;
            outBytes.Alignment = DataGridViewContentAlignment.MiddleRight;
            outBytes.Value = delegate(DomainRow row) { return Theme.FormatBytes(row.OutBytes); };
            outBytes.SortKey = delegate(DomainRow row) { return row.OutBytes; };
            outBytes.DescendingFirst = true;
            columns.Add(outBytes);

            GridColumn<DomainRow> total = new GridColumn<DomainRow>();
            total.Header = "Всего";
            total.Width = 98;
            total.Alignment = DataGridViewContentAlignment.MiddleRight;
            total.Value = delegate(DomainRow row) { return Theme.FormatBytes(row.TotalBytes); };
            total.SortKey = delegate(DomainRow row) { return row.TotalBytes; };
            total.DescendingFirst = true;
            columns.Add(total);

            GridColumn<DomainRow> last = new GridColumn<DomainRow>();
            last.Header = "Последняя активность";
            last.Width = 158;
            last.Value = delegate(DomainRow row) { return FormatUtc(row.LastUtc); };
            last.SortKey = delegate(DomainRow row) { return row.LastUtc; };
            last.DescendingFirst = true;
            columns.Add(last);

            return columns;
        }

        private Color CategoryBackground(string category)
        {
            Color color = _classifier.GetColor(category);
            return Theme.Blend(color, Color.White, 0.84);
        }

        private Color CategoryForeground(string category)
        {
            Color color = _classifier.GetColor(category);
            return Theme.Darken(color, 0.42);
        }

        private static string FormatUtc(DateTime utc)
        {
            if (utc == DateTime.MinValue) return "";
            return utc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture);
        }

        private static string EndPoint(string ip, int port, bool ipv6)
        {
            if (port <= 0) return ip ?? "";
            if (ipv6) return "[" + ip + "]:" + port.ToString(CultureInfo.InvariantCulture);
            return ip + ":" + port.ToString(CultureInfo.InvariantCulture);
        }

        private void AttachConnectionsMenu(BufferedDataGridView grid)
        {
            ContextMenuStrip menu = Ui.CreateMenu();
            ToolStripMenuItem details = new ToolStripMenuItem("Подробности…");
            ToolStripMenuItem copyRow = new ToolStripMenuItem("Копировать строку");
            ToolStripMenuItem copyApp = new ToolStripMenuItem("Копировать путь программы");
            ToolStripMenuItem filterApp = new ToolStripMenuItem("Фильтровать по программе");
            ToolStripMenuItem filterDomain = new ToolStripMenuItem("Фильтровать по домену");
            ToolStripMenuItem filterCategory = new ToolStripMenuItem("Показать только эту категорию");
            ToolStripMenuItem exportProgram = CreateProgramExportMenuItem(
                delegate
                {
                    ConnectionRow row = _connectionsGrid.SelectedItem;
                    return row != null ? row.AppId : 0;
                },
                delegate
                {
                    ConnectionRow row = _connectionsGrid.SelectedItem;
                    return row != null ? row.AppPath : "";
                });

            details.Click += delegate { ShowConnectionDetailsDialog(); };
            copyRow.Click += delegate { CopySelectedConnection(); };
            copyApp.Click += delegate
            {
                ConnectionRow row = _connectionsGrid.SelectedItem;
                if (row != null) CopyText(row.AppPath, "путь программы");
            };
            filterApp.Click += delegate
            {
                ConnectionRow row = _connectionsGrid.SelectedItem;
                if (row != null) SetAppFilter(row.AppId, row.AppPath);
            };
            filterDomain.Click += delegate
            {
                ConnectionRow row = _connectionsGrid.SelectedItem;
                if (row != null && !string.IsNullOrEmpty(row.Domain)) SetSearchText(row.Domain);
            };
            filterCategory.Click += delegate
            {
                ConnectionRow row = _connectionsGrid.SelectedItem;
                if (row != null) SetCategoryFilter(row.Category);
            };

            menu.Items.Add(details);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(copyRow);
            menu.Items.Add(copyApp);
            menu.Items.Add(exportProgram);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(filterApp);
            menu.Items.Add(filterDomain);
            menu.Items.Add(filterCategory);

            menu.Opening += delegate
            {
                ConnectionRow row = _connectionsGrid.SelectedItem;
                bool has = row != null;
                details.Enabled = has;
                copyRow.Enabled = has;
                copyApp.Enabled = has;
                exportProgram.Enabled = has && row.AppId > 0;
                filterApp.Enabled = has;
                filterDomain.Enabled = has && !string.IsNullOrEmpty(row.Domain);
                filterCategory.Enabled = has;
            };

            grid.ContextMenuStrip = menu;
        }

        private void AttachTransfersMenu(BufferedDataGridView grid)
        {
            ContextMenuStrip menu = Ui.CreateMenu();
            ToolStripMenuItem details = new ToolStripMenuItem("Подробности…");
            ToolStripMenuItem copyRow = new ToolStripMenuItem("Копировать строку");
            ToolStripMenuItem copyApp = new ToolStripMenuItem("Копировать путь программы");
            ToolStripMenuItem filterApp = new ToolStripMenuItem("Фильтровать по программе");
            ToolStripMenuItem filterDomain = new ToolStripMenuItem("Фильтровать по домену");
            ToolStripMenuItem filterCategory = new ToolStripMenuItem("Показать только эту категорию");
            ToolStripMenuItem exportProgram = CreateProgramExportMenuItem(
                delegate
                {
                    TrafficRow row = _transfersGrid.SelectedItem;
                    return row != null ? row.AppId : 0;
                },
                delegate
                {
                    TrafficRow row = _transfersGrid.SelectedItem;
                    return row != null ? row.AppPath : "";
                });

            details.Click += delegate { ShowTransferDetailsDialog(); };
            copyRow.Click += delegate { CopySelectedTransfer(); };
            copyApp.Click += delegate
            {
                TrafficRow row = _transfersGrid.SelectedItem;
                if (row != null) CopyText(row.AppPath, "путь программы");
            };
            filterApp.Click += delegate
            {
                TrafficRow row = _transfersGrid.SelectedItem;
                if (row != null) SetAppFilter(row.AppId, row.AppPath);
            };
            filterDomain.Click += delegate
            {
                TrafficRow row = _transfersGrid.SelectedItem;
                if (row != null && !string.IsNullOrEmpty(row.Domain)) SetSearchText(row.Domain);
            };
            filterCategory.Click += delegate
            {
                TrafficRow row = _transfersGrid.SelectedItem;
                if (row != null) SetCategoryFilter(row.Category);
            };

            menu.Items.Add(details);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(copyRow);
            menu.Items.Add(copyApp);
            menu.Items.Add(exportProgram);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(filterApp);
            menu.Items.Add(filterDomain);
            menu.Items.Add(filterCategory);

            menu.Opening += delegate
            {
                TrafficRow row = _transfersGrid.SelectedItem;
                bool has = row != null;
                details.Enabled = has;
                copyRow.Enabled = has;
                copyApp.Enabled = has;
                exportProgram.Enabled = has && row.AppId > 0;
                filterApp.Enabled = has;
                filterDomain.Enabled = has && !string.IsNullOrEmpty(row.Domain);
                filterCategory.Enabled = has;
            };

            grid.ContextMenuStrip = menu;
        }

        private void AttachAppsMenu(BufferedDataGridView grid)
        {
            ContextMenuStrip menu = Ui.CreateMenu();
            ToolStripMenuItem details = new ToolStripMenuItem("Подробности…");
            ToolStripMenuItem copyRow = new ToolStripMenuItem("Копировать строку");
            ToolStripMenuItem copyPath = new ToolStripMenuItem("Копировать путь программы");
            ToolStripMenuItem filterApp = new ToolStripMenuItem("Фильтровать по программе");
            ToolStripMenuItem filterCategory = new ToolStripMenuItem("Показать только эту категорию");
            ToolStripMenuItem exportProgram = CreateProgramExportMenuItem(
                delegate
                {
                    AppAggregate row = _appsGrid.SelectedItem;
                    return row != null ? row.AppId : 0;
                },
                delegate
                {
                    AppAggregate row = _appsGrid.SelectedItem;
                    return row != null ? row.AppPath : "";
                });

            details.Click += delegate { ShowAppDetailsDialog(); };
            copyRow.Click += delegate { CopySelectedApp(); };
            copyPath.Click += delegate
            {
                AppAggregate row = _appsGrid.SelectedItem;
                if (row != null) CopyText(row.AppPath, "путь программы");
            };
            filterApp.Click += delegate
            {
                AppAggregate row = _appsGrid.SelectedItem;
                if (row != null) SetAppFilter(row.AppId, row.AppPath);
            };
            filterCategory.Click += delegate
            {
                AppAggregate row = _appsGrid.SelectedItem;
                if (row != null) SetCategoryFilter(row.Category);
            };

            menu.Items.Add(details);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(copyRow);
            menu.Items.Add(copyPath);
            menu.Items.Add(exportProgram);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(filterApp);
            menu.Items.Add(filterCategory);

            menu.Opening += delegate
            {
                AppAggregate row = _appsGrid.SelectedItem;
                bool has = row != null;
                details.Enabled = has;
                copyRow.Enabled = has;
                copyPath.Enabled = has;
                exportProgram.Enabled = has && row.AppId > 0;
                filterApp.Enabled = has;
                filterCategory.Enabled = has;
            };

            grid.ContextMenuStrip = menu;
        }

        private void AttachDomainsMenu(BufferedDataGridView grid)
        {
            ContextMenuStrip menu = Ui.CreateMenu();
            ToolStripMenuItem details = new ToolStripMenuItem("Подробности…");
            ToolStripMenuItem copyRow = new ToolStripMenuItem("Копировать строку");
            ToolStripMenuItem copyIp = new ToolStripMenuItem("Копировать IP-адрес");
            ToolStripMenuItem copyDomain = new ToolStripMenuItem("Копировать домен");
            ToolStripMenuItem search = new ToolStripMenuItem("Искать этот IP или домен");
            ToolStripMenuItem filterCategory = new ToolStripMenuItem("Показать только эту категорию");

            details.Click += delegate { ShowDomainDetailsDialog(); };
            copyRow.Click += delegate { CopySelectedDomain(); };
            copyIp.Click += delegate
            {
                DomainRow row = _domainsGrid.SelectedItem;
                if (row != null) CopyText(row.Ip, "IP-адрес");
            };
            copyDomain.Click += delegate
            {
                DomainRow row = _domainsGrid.SelectedItem;
                if (row != null) CopyText(row.Domain, "домен");
            };
            search.Click += delegate
            {
                DomainRow row = _domainsGrid.SelectedItem;
                if (row == null) return;
                SetSearchText(string.IsNullOrEmpty(row.Domain) ? row.Ip : row.Domain);
            };
            filterCategory.Click += delegate
            {
                DomainRow row = _domainsGrid.SelectedItem;
                if (row != null) SetCategoryFilter(row.Service);
            };

            menu.Items.Add(details);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(copyRow);
            menu.Items.Add(copyIp);
            menu.Items.Add(copyDomain);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(search);
            menu.Items.Add(filterCategory);

            menu.Opening += delegate
            {
                DomainRow row = _domainsGrid.SelectedItem;
                bool has = row != null;
                details.Enabled = has;
                copyRow.Enabled = has;
                copyIp.Enabled = has;
                copyDomain.Enabled = has && !string.IsNullOrEmpty(row.Domain);
                search.Enabled = has;
                filterCategory.Enabled = has;
            };

            grid.ContextMenuStrip = menu;
        }

        private void CopySelectedConnection()
        {
            ConnectionRow row = _connectionsGrid.SelectedItem;
            if (row == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Программа: " + row.AppPath);
            sb.AppendLine("Категория: " + row.Category);
            sb.AppendLine("Время: " + FormatUtc(row.TimeUtc));
            sb.AppendLine("Длительность: " + Theme.FormatDuration(row.DurationSec));
            sb.AppendLine("Протокол: " + Protocols.Name(row.Proto));
            sb.AppendLine("Локально: " + EndPoint(row.LocalIp, row.LocalPort, row.IsIpv6));
            sb.AppendLine("Удалённо: " + EndPoint(row.RemoteIp, row.RemotePort, row.IsIpv6));
            sb.AppendLine("Домен: " + (string.IsNullOrEmpty(row.Domain) ? "—" : row.Domain));
            sb.AppendLine("Вход: " + Theme.FormatBytes(row.InBytes));
            sb.AppendLine("Исход: " + Theme.FormatBytes(row.OutBytes));
            sb.Append("Всего: " + Theme.FormatBytes(row.InBytes + row.OutBytes));
            CopyText(sb.ToString(), "соединение");
        }

        private void CopySelectedTransfer()
        {
            TrafficRow row = _transfersGrid.SelectedItem;
            if (row == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Программа: " + row.AppPath);
            sb.AppendLine("Категория: " + row.Category);
            sb.AppendLine("Протокол: " + Protocols.Name(row.Proto));
            sb.AppendLine("Локально: " + row.LocalIp);
            sb.AppendLine("Удалённо: " + EndPoint(row.RemoteIp, row.RemotePort, row.IsIpv6));
            sb.AppendLine("Домен: " + (string.IsNullOrEmpty(row.Domain) ? "—" : row.Domain));
            sb.AppendLine("Вход: " + Theme.FormatBytes(row.InBytes));
            sb.AppendLine("Исход: " + Theme.FormatBytes(row.OutBytes));
            sb.AppendLine("Всего: " + Theme.FormatBytes(row.TotalBytes));
            sb.AppendLine("Записей: " + Theme.FormatCount(row.RecordCount));
            sb.AppendLine("Первое: " + FormatUtc(row.FirstUtc));
            sb.Append("Последнее: " + FormatUtc(row.LastUtc));
            CopyText(sb.ToString(), "строку трафика");
        }

        private void CopySelectedApp()
        {
            AppAggregate row = _appsGrid.SelectedItem;
            if (row == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Программа: " + row.AppPath);
            sb.AppendLine("Категория: " + row.Category);
            sb.AppendLine("Соединений: " + Theme.FormatCount(row.Connections));
            sb.AppendLine("Вход: " + Theme.FormatBytes(row.InBytes));
            sb.AppendLine("Исход: " + Theme.FormatBytes(row.OutBytes));
            sb.AppendLine("Всего: " + Theme.FormatBytes(row.InBytes + row.OutBytes));
            sb.AppendLine("Уникальных IP: " + Theme.FormatCount(row.UniqueRemoteIps));
            sb.Append("Последняя активность: " + FormatUtc(row.LastUtc));
            CopyText(sb.ToString(), "сводку программы");
        }

        private void CopySelectedDomain()
        {
            DomainRow row = _domainsGrid.SelectedItem;
            if (row == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("IP-адрес: " + row.Ip);
            sb.AppendLine("Домен: " + (string.IsNullOrEmpty(row.Domain) ? "—" : row.Domain));
            sb.AppendLine("Категория: " + row.Service);
            sb.AppendLine("Протоколы: " + row.Protocols);
            sb.AppendLine("Соединений: " + Theme.FormatCount(row.Connections));
            sb.AppendLine("Вход: " + Theme.FormatBytes(row.InBytes));
            sb.AppendLine("Исход: " + Theme.FormatBytes(row.OutBytes));
            sb.AppendLine("Всего: " + Theme.FormatBytes(row.TotalBytes));
            sb.Append("Последняя активность: " + FormatUtc(row.LastUtc));
            CopyText(sb.ToString(), "IP-адрес");
        }

        private void CopyText(string text, string description)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                Clipboard.SetText(text);
                SetStatus("Скопировано: " + description);
            }
            catch (Exception ex)
            {
                Log.Error("Не удалось скопировать в буфер обмена.", ex);
                SetStatus("Не удалось скопировать в буфер обмена");
            }
        }

        private void ShowConnectionDetails(ConnectionRow row)
        {
            if (row == null)
            {
                _lblDetailApp.Text = "Выберите соединение";
                _lblDetailApp.ForeColor = Theme.Text;
                _lblDetailNet.Text = "";
                _lblDetailTraffic.Text = "";
                _tips.SetToolTip(_lblDetailApp, "");
                return;
            }

            _lblDetailApp.Text = DisplayNames.FromPath(row.AppPath) + "  ·  " + row.Category;
            _lblDetailApp.ForeColor = CategoryForeground(row.Category);
            _lblDetailNet.Text = Protocols.Name(row.Proto) + "   " +
                EndPoint(row.LocalIp, row.LocalPort, row.IsIpv6) + "  →  " +
                EndPoint(row.RemoteIp, row.RemotePort, row.IsIpv6) +
                (string.IsNullOrEmpty(row.Domain) ? "" : "   ·   " + row.Domain);
            _lblDetailTraffic.Text = "Вход: " + Theme.FormatBytes(row.InBytes) +
                "   ·   Исход: " + Theme.FormatBytes(row.OutBytes) +
                "   ·   Всего: " + Theme.FormatBytes(row.InBytes + row.OutBytes) +
                "   ·   Длительность: " + Theme.FormatDuration(row.DurationSec) +
                "   ·   " + FormatUtc(row.TimeUtc);
            _tips.SetToolTip(_lblDetailApp, row.AppPath);
            _tips.SetToolTip(_lblDetailNet, _lblDetailNet.Text);
        }

        private static string BuildConnectionDetailsText(ConnectionRow row)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Программа: " + row.AppPath);
            sb.AppendLine("Категория: " + row.Category);
            sb.AppendLine();
            sb.AppendLine("Время начала: " + FormatUtc(row.TimeUtc));
            sb.AppendLine("Длительность: " + Theme.FormatDuration(row.DurationSec));
            sb.AppendLine("Протокол: " + Protocols.Name(row.Proto));
            sb.AppendLine("Флаги: 0x" + row.Flags.ToString("X", CultureInfo.InvariantCulture) + (row.IsIpv6 ? " (IPv6)" : " (IPv4)"));
            sb.AppendLine("Локальный адрес: " + EndPoint(row.LocalIp, row.LocalPort, row.IsIpv6));
            sb.AppendLine("Удалённый адрес: " + EndPoint(row.RemoteIp, row.RemotePort, row.IsIpv6));
            sb.AppendLine("Домен: " + (string.IsNullOrEmpty(row.Domain) ? "—" : row.Domain));
            sb.AppendLine("Вход: " + Theme.FormatBytes(row.InBytes) + " (" + row.InBytes.ToString("N0", CultureInfo.CurrentCulture) + " байт)");
            sb.AppendLine("Исход: " + Theme.FormatBytes(row.OutBytes) + " (" + row.OutBytes.ToString("N0", CultureInfo.CurrentCulture) + " байт)");
            sb.AppendLine("Всего: " + Theme.FormatBytes(row.InBytes + row.OutBytes));
            sb.AppendLine();
            sb.Append("ID записи: " + row.RowId.ToString(CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        private void ShowConnectionDetailsDialog()
        {
            ConnectionRow row = _connectionsGrid.SelectedItem;
            if (row == null) return;
            using (DetailsDialog dialog = new DetailsDialog("Соединение", BuildConnectionDetailsText(row)))
                dialog.ShowDialog(this);
        }

        private void ShowTransferDetailsDialog()
        {
            TrafficRow row = _transfersGrid.SelectedItem;
            if (row == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Программа: " + row.AppPath);
            sb.AppendLine("Категория: " + row.Category);
            sb.AppendLine();
            sb.AppendLine("Протокол: " + Protocols.Name(row.Proto));
            sb.AppendLine("Локальный IP: " + row.LocalIp);
            sb.AppendLine("Удалённый адрес: " + EndPoint(row.RemoteIp, row.RemotePort, row.IsIpv6));
            sb.AppendLine("Домен: " + (string.IsNullOrEmpty(row.Domain) ? "—" : row.Domain));
            sb.AppendLine("Вход: " + Theme.FormatBytes(row.InBytes) + " (" + row.InBytes.ToString("N0", CultureInfo.CurrentCulture) + " байт)");
            sb.AppendLine("Исход: " + Theme.FormatBytes(row.OutBytes) + " (" + row.OutBytes.ToString("N0", CultureInfo.CurrentCulture) + " байт)");
            sb.AppendLine("Всего: " + Theme.FormatBytes(row.TotalBytes));
            sb.AppendLine("Записей: " + row.RecordCount.ToString("N0", CultureInfo.CurrentCulture));
            sb.AppendLine("Первое событие: " + FormatUtc(row.FirstUtc));
            sb.Append("Последнее событие: " + FormatUtc(row.LastUtc));

            using (DetailsDialog dialog = new DetailsDialog("Строка трафика", sb.ToString()))
                dialog.ShowDialog(this);
        }

        private void ShowAppDetailsDialog()
        {
            AppAggregate row = _appsGrid.SelectedItem;
            if (row == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Программа: " + row.AppPath);
            sb.AppendLine("Категория: " + row.Category);
            sb.AppendLine();
            sb.AppendLine("Соединений: " + row.Connections.ToString("N0", CultureInfo.CurrentCulture));
            sb.AppendLine("Вход: " + Theme.FormatBytes(row.InBytes) + " (" + row.InBytes.ToString("N0", CultureInfo.CurrentCulture) + " байт)");
            sb.AppendLine("Исход: " + Theme.FormatBytes(row.OutBytes) + " (" + row.OutBytes.ToString("N0", CultureInfo.CurrentCulture) + " байт)");
            sb.AppendLine("Всего: " + Theme.FormatBytes(row.InBytes + row.OutBytes));
            sb.AppendLine("Уникальных удалённых IP: " + row.UniqueRemoteIps.ToString("N0", CultureInfo.CurrentCulture));
            sb.Append("Последняя активность: " + FormatUtc(row.LastUtc));

            using (DetailsDialog dialog = new DetailsDialog("Сводка по программе", sb.ToString()))
                dialog.ShowDialog(this);
        }

        private void ShowDomainDetailsDialog()
        {
            DomainRow row = _domainsGrid.SelectedItem;
            if (row == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("IP-адрес: " + row.Ip + (row.IsIpv6 ? " (IPv6)" : " (IPv4)"));
            sb.AppendLine("Домен: " + (string.IsNullOrEmpty(row.Domain) ? "—" : row.Domain));
            sb.AppendLine("Категория: " + row.Service);
            sb.AppendLine("Протоколы: " + row.Protocols);
            sb.AppendLine();
            sb.AppendLine("Соединений: " + row.Connections.ToString("N0", CultureInfo.CurrentCulture));
            sb.AppendLine("Вход: " + Theme.FormatBytes(row.InBytes) + " (" + row.InBytes.ToString("N0", CultureInfo.CurrentCulture) + " байт)");
            sb.AppendLine("Исход: " + Theme.FormatBytes(row.OutBytes) + " (" + row.OutBytes.ToString("N0", CultureInfo.CurrentCulture) + " байт)");
            sb.AppendLine("Всего: " + Theme.FormatBytes(row.TotalBytes));
            sb.Append("Последняя активность: " + FormatUtc(row.LastUtc));

            using (DetailsDialog dialog = new DetailsDialog("IP-адрес", sb.ToString()))
                dialog.ShowDialog(this);
        }

        private void ExportCurrentTab()
        {
            string suffix = "data";
            string title = "Экспорт данных";
            bool categoriesTab = _currentTab == 4;

            switch (_currentTab)
            {
                case 0: suffix = "connections"; title = "Экспорт соединений"; break;
                case 1: suffix = "traffic"; title = "Экспорт трафика"; break;
                case 2: suffix = "programs"; title = "Экспорт программ"; break;
                case 3: suffix = "domains"; title = "Экспорт IP-адресов"; break;
                case 4: suffix = "categories"; title = "Экспорт правил категорий"; break;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = title;
                dialog.Filter = "CSV (разделитель ;)|*.csv|Все файлы (*.*)|*.*";
                dialog.DefaultExt = "csv";
                dialog.AddExtension = true;
                dialog.FileName = "netlimiter-" + suffix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + ".csv";

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    if (categoriesTab)
                        CsvExporter.ExportGrid(_gridRules, dialog.FileName);
                    else if (_currentTab == 0)
                        CsvExporter.ExportVirtual(_connectionsGrid, dialog.FileName);
                    else if (_currentTab == 1)
                        CsvExporter.ExportVirtual(_transfersGrid, dialog.FileName);
                    else if (_currentTab == 2)
                        CsvExporter.ExportVirtual(_appsGrid, dialog.FileName);
                    else if (_currentTab == 3)
                        CsvExporter.ExportVirtual(_domainsGrid, dialog.FileName);

                    SetStatus("Экспортировано: " + dialog.FileName);
                    MessageBox.Show(this,
                        "Данные экспортированы:" + Environment.NewLine + dialog.FileName,
                        "Готово",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Log.Error("Ошибка экспорта CSV.", ex);
                    MessageBox.Show(this, ex.Message, "Ошибка экспорта", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SetSearchText(string text)
        {
            _searchTimer.Stop();
            _search.Text = text ?? "";
            ApplyCurrentFilters();
        }

        private void SetCategoryFilter(string category)
        {
            if (string.IsNullOrEmpty(category)) return;
            int index = -1;
            for (int i = 0; i < _cmbCategory.Items.Count; i++)
            {
                if (string.Equals(_cmbCategory.Items[i].ToString(), category, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            if (index < 0) return;

            _suppressFilters = true;
            _cmbCategory.SelectedIndex = index;
            _suppressFilters = false;
            _categoryFilter = category;
            ApplyCurrentFilters();
        }
    }
}
