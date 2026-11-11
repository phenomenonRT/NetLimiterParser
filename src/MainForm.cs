using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed partial class MainForm : Form
    {
        private static readonly int[] PeriodValues = new int[] { -1, 15, 60, 360, 1440, 10080, 43200 };
        private static readonly string[] PeriodLabels = new string[] { "Всё время", "15 мин", "1 час", "6 часов", "24 часа", "7 дней", "30 дней" };

        private readonly AppSettings _settings;
        private readonly List<CategoryDef> _categories;
        private readonly Classifier _classifier;
        private readonly string _startupWarning;
        private readonly ToolTip _tips;
        private readonly System.Windows.Forms.Timer _searchTimer;

        private NetLimiterDatabase _db;
        private DbStats _stats;
        private CancellationTokenSource _cts;
        private int _loadVersion;
        private bool _activeOperation;
        private bool _openingDatabase;
        private bool _closing;
        private bool _checkingUpdates;
        private bool _suppressFilters;
        private bool _suppressCategoryEvents;
        private bool _categoriesDirty;
        private int _currentTab;

        private long _fromMs;
        private long _toMs;
        private long _totalConnections;
        private int _protoFilter;
        private string _categoryFilter;
        private int _appFilterId;
        private string _appFilterName;

        private bool _connectionsLoaded;
        private bool _connectionsExhausted;
        private bool _connectionsHasCursor;
        private long _cursorTime;
        private long _cursorRowId;
        private readonly List<ConnectionRow> _connectionRows;

        private bool _transfersLoaded;
        private TransferSummary _transferSummary;

        private bool _appsLoaded;
        private List<AppAggregate> _appRows;

        private bool _domainsLoaded;
        private readonly List<DomainRow> _domainRows;

        private GradientPanel _header;
        private Label _lblTitle;
        private Label _lblHeaderPath;
        private Label _lblDbStats;
        private Label _lblPeriodInfo;
        private RoundButton _btnRefresh;
        private RoundButton _btnOpenDb;
        private RoundButton _btnUpdates;

        private BorderPanel _toolbar;
        private ChipButton[] _periodChips;
        private ChipButton _chipAppFilter;
        private ComboBox _cmbProto;
        private ComboBox _cmbCategory;
        private SearchBox _search;
        private RoundButton _btnResetFilters;
        private RoundButton _btnExport;

        private BorderPanel _tabsBar;
        private NavTabButton[] _tabs;
        private Panel _contentHost;
        private Panel _pageConnections;
        private Panel _pageTransfers;
        private Panel _pageApps;
        private Panel _pageDomains;
        private Panel _pageCategories;
        private LoadingOverlay _overlay;
        private LoadingOverlay _transferOverlay;

        private Label _lblStatus;
        private Label _lblStatusRight;
        private ProgressBar _statusProgress;

        private BufferedDataGridView _gridConnections;
        private BufferedDataGridView _gridTransfers;
        private BufferedDataGridView _gridApps;
        private BufferedDataGridView _gridDomains;
        private BufferedDataGridView _gridRules;

        private VirtualGrid<ConnectionRow> _connectionsGrid;
        private VirtualGrid<TrafficRow> _transfersGrid;
        private VirtualGrid<AppAggregate> _appsGrid;
        private VirtualGrid<DomainRow> _domainsGrid;

        private Label _lblConnInfo;
        private Label _lblTransfersInfo;
        private Label _lblAppsInfo;
        private RoundButton _btnExportAppDomains;
        private Label _lblDomainsInfo;
        private RoundButton _btnLoadMore;
        private RoundButton _btnReloadTransfers;
        private Label _lblDetailApp;
        private Label _lblDetailNet;
        private Label _lblDetailTraffic;
        private RoundButton _btnCopyConn;

        private ListBox _catList;
        private TextBox _txtCatName;
        private Panel _colorSwatch;
        private CheckBox _chkCatEnabled;
        private RoundButton _btnCatAdd;
        private RoundButton _btnCatDelete;
        private RoundButton _btnCatUp;
        private RoundButton _btnCatDown;
        private RoundButton _btnCatSave;
        private RoundButton _btnCatReset;
        private RoundButton _btnCatTest;
        private RoundButton _btnRuleAdd;
        private RoundButton _btnRuleEdit;
        private RoundButton _btnRuleDelete;
        private Label _lblCatDirty;

        public MainForm()
        {
            _settings = SettingsStore.Load();
            _categories = CategoryStore.LoadOrCreate();
            _startupWarning = CategoryStore.LastWarning;
            _classifier = new Classifier(_categories);
            _connectionRows = new List<ConnectionRow>();
            _appRows = new List<AppAggregate>();
            _domainRows = new List<DomainRow>();
            _protoFilter = 0;
            _categoriesDirty = false;

            _tips = new ToolTip();
            _tips.AutoPopDelay = 9000;
            _tips.InitialDelay = 450;
            _tips.ReshowDelay = 120;
            _tips.ShowAlways = true;

            _searchTimer = new System.Windows.Forms.Timer();
            _searchTimer.Interval = 320;
            _searchTimer.Tick += delegate
            {
                _searchTimer.Stop();
                ApplyCurrentFilters();
            };

            InitializeWindow();
            BuildUi();
            _overlay.CancelRequested += delegate
            {
                SetStatus("Отмена операции…");
                CancelActiveOperation();
            };
            ApplyWindowSettings();
            SelectPeriodChip(_settings.PeriodMinutes, false);
            UpdateHeaderPaths();
            SetStatus("Готово");
            UpdateButtons();

            if (!string.IsNullOrEmpty(_startupWarning))
                SetStatus(_startupWarning);

            Shown += delegate { StartDatabaseInitialization(); };
            FormClosing += MainFormFormClosing;
        }

        private void InitializeWindow()
        {
            Text = "NetLimiter Parser " + UpdateChecker.CurrentVersion + " — анализ сетевого трафика";
            Font = Theme.FontUi;
            BackColor = Theme.Bg;
            AutoScaleMode = AutoScaleMode.Font;
            MinimumSize = new Size(1280, 720);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            KeyPreview = true;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }
        }

        private void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.BackColor = Theme.Bg;
            root.ColumnCount = 1;
            root.RowCount = 5;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            root.Margin = new Padding(0);
            Controls.Add(root);

            BuildHeader();
            root.Controls.Add(_header, 0, 0);

            BuildToolbar();
            root.Controls.Add(_toolbar, 0, 1);

            BuildTabs();
            root.Controls.Add(_tabsBar, 0, 2);

            _contentHost = new Panel();
            _contentHost.Dock = DockStyle.Fill;
            _contentHost.BackColor = Theme.Bg;
            _contentHost.Padding = new Padding(16, 12, 16, 12);
            root.Controls.Add(_contentHost, 0, 3);

            BuildPages();

            _overlay = new LoadingOverlay();
            _overlay.Dock = DockStyle.Fill;
            _overlay.Visible = false;
            _contentHost.Controls.Add(_overlay);
            _overlay.BringToFront();

            root.Controls.Add(BuildStatusHost(), 0, 4);
        }

        private void BuildHeader()
        {
            _header = new GradientPanel();
            _header.Dock = DockStyle.Fill;
            _header.Margin = new Padding(0);

            LogoBadge logo = new LogoBadge();
            logo.SetBounds(24, 19, 46, 46);
            _header.Controls.Add(logo);

            _lblTitle = Ui.Label("NetLimiter Parser", Theme.FontTitle, Color.White);
            _lblTitle.SetBounds(84, 10, 420, 36);
            _header.Controls.Add(_lblTitle);

            _lblHeaderPath = Ui.Label("База: —", Theme.FontUiSmall, Color.FromArgb(203, 213, 225));
            _lblHeaderPath.SetBounds(86, 45, 500, 22);
            _lblHeaderPath.AutoEllipsis = true;
            _header.Controls.Add(_lblHeaderPath);

            _lblDbStats = Ui.Label("База не загружена", Theme.FontUiSmall, Color.FromArgb(203, 213, 225));
            _lblDbStats.SetBounds(940, 15, 390, 52);
            _lblDbStats.TextAlign = ContentAlignment.MiddleRight;
            _header.Controls.Add(_lblDbStats);

            _btnRefresh = new RoundButton();
            _btnRefresh.Text = "Обновить";
            _btnRefresh.Primary = true;
            _btnRefresh.SetBounds(1350, 25, 122, 34);
            _btnRefresh.Click += delegate { RefreshDataAsync(); };
            _header.Controls.Add(_btnRefresh);
            _tips.SetToolTip(_btnRefresh, "Обновить статистику и перечитать данные");

            _btnOpenDb = new RoundButton();
            _btnOpenDb.Text = "Открыть БД…";
            _btnOpenDb.Quiet = true;
            _btnOpenDb.Inverted = true;
            _btnOpenDb.SetBounds(1200, 25, 138, 34);
            _btnOpenDb.Click += delegate { ShowOpenDatabaseDialog(); };
            _header.Controls.Add(_btnOpenDb);
            _tips.SetToolTip(_btnOpenDb, "Выбрать другой файл nlstats.db");

            _btnUpdates = new RoundButton();
            _btnUpdates.Text = "Обновления";
            _btnUpdates.Quiet = true;
            _btnUpdates.Inverted = true;
            _btnUpdates.SetBounds(1060, 25, 124, 34);
            _btnUpdates.Click += delegate { CheckForUpdatesAsync(); };
            _header.Controls.Add(_btnUpdates);
            _tips.SetToolTip(_btnUpdates, "Проверить обновления в релизах на GitHub");

            _header.Resize += delegate { LayoutHeader(); };
        }

        private void LayoutHeader()
        {
            if (_header == null || _btnRefresh == null) return;
            int right = _header.ClientSize.Width - 24;
            _btnRefresh.Location = new Point(right - _btnRefresh.Width, 25);
            _btnOpenDb.Location = new Point(_btnRefresh.Left - 12 - _btnOpenDb.Width, 25);
            _btnUpdates.Location = new Point(_btnOpenDb.Left - 12 - _btnUpdates.Width, 25);
            _lblDbStats.Location = new Point(Math.Max(300, _btnUpdates.Left - 16 - _lblDbStats.Width), 16);

            int titleWidth = Math.Max(220, _lblDbStats.Left - 100);
            _lblTitle.Width = titleWidth;
            _lblHeaderPath.Width = titleWidth;
        }

        private void BuildToolbar()
        {
            _toolbar = new BorderPanel();
            _toolbar.Dock = DockStyle.Fill;
            _toolbar.DrawBottom = true;
            _toolbar.BorderColor = Theme.Border;
            _toolbar.Margin = new Padding(0);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Margin = new Padding(0);
            _toolbar.Controls.Add(layout);

            FlowLayoutPanel left = new FlowLayoutPanel();
            left.Dock = DockStyle.Fill;
            left.FlowDirection = FlowDirection.LeftToRight;
            left.WrapContents = false;
            left.AutoScroll = false;
            left.BackColor = Theme.Surface;
            left.Padding = new Padding(16, 14, 0, 0);
            left.Margin = new Padding(0);
            layout.Controls.Add(left, 0, 0);

            Label periodLabel = Ui.Label("Период", Theme.FontUiSmall, Theme.Muted);
            periodLabel.Size = new Size(50, 28);
            periodLabel.TextAlign = ContentAlignment.MiddleLeft;
            left.Controls.Add(periodLabel);

            _periodChips = new ChipButton[PeriodValues.Length];
            for (int i = 0; i < PeriodValues.Length; i++)
            {
                int value = PeriodValues[i];
                ChipButton chip = new ChipButton();
                chip.Text = PeriodLabels[i];
                chip.Tag = value;
                chip.Click += delegate(object senderObject, EventArgs args)
                {
                    ChipButton senderChip = senderObject as ChipButton;
                    if (senderChip == null) return;
                    SetPeriod((int)senderChip.Tag);
                };
                _periodChips[i] = chip;
                left.Controls.Add(chip);
            }

            Panel separator = new Panel();
            separator.Width = 1;
            separator.Height = 22;
            separator.BackColor = Theme.Border;
            separator.Margin = new Padding(8, 3, 8, 0);
            left.Controls.Add(separator);

            Label protocolLabel = Ui.Label("Протокол", Theme.FontUiSmall, Theme.Muted);
            protocolLabel.Size = new Size(62, 28);
            left.Controls.Add(protocolLabel);

            _cmbProto = Ui.Combo(108);
            _cmbProto.Margin = new Padding(0, 1, 8, 0);
            _cmbProto.Items.Add("Все");
            _cmbProto.Items.Add("TCP");
            _cmbProto.Items.Add("UDP");
            _cmbProto.Items.Add("ICMP");
            _cmbProto.Items.Add("ICMPv6");
            _cmbProto.SelectedIndex = 0;
            _cmbProto.SelectedIndexChanged += delegate { ProtocolFilterChanged(); };
            left.Controls.Add(_cmbProto);

            Label categoryLabel = Ui.Label("Категория", Theme.FontUiSmall, Theme.Muted);
            categoryLabel.Size = new Size(66, 28);
            left.Controls.Add(categoryLabel);

            _cmbCategory = Ui.Combo(160);
            _cmbCategory.DropDownWidth = 260;
            _cmbCategory.Margin = new Padding(0, 1, 6, 0);
            _cmbCategory.SelectedIndexChanged += delegate { CategoryFilterChanged(); };
            left.Controls.Add(_cmbCategory);

            _chipAppFilter = new ChipButton();
            _chipAppFilter.Checked = true;
            _chipAppFilter.Visible = false;
            _chipAppFilter.Margin = new Padding(4, 0, 0, 0);
            _chipAppFilter.Click += delegate { ClearAppFilter(); };
            left.Controls.Add(_chipAppFilter);

            FlowLayoutPanel right = new FlowLayoutPanel();
            right.Dock = DockStyle.Fill;
            right.FlowDirection = FlowDirection.LeftToRight;
            right.WrapContents = false;
            right.AutoSize = true;
            right.BackColor = Theme.Surface;
            right.Padding = new Padding(0, 14, 16, 0);
            right.Margin = new Padding(0);
            layout.Controls.Add(right, 1, 0);

            _search = new SearchBox("Поиск: программа, IP, домен…");
            _search.Size = new Size(245, 34);
            _search.Margin = new Padding(0, 0, 10, 0);
            _search.Input.TextChanged += delegate
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            };
            _search.Input.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    _searchTimer.Stop();
                    ApplyCurrentFilters();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            right.Controls.Add(_search);

            _btnResetFilters = new RoundButton();
            _btnResetFilters.Text = "Сброс";
            _btnResetFilters.Quiet = true;
            _btnResetFilters.Size = new Size(74, 32);
            _btnResetFilters.Margin = new Padding(0, 1, 8, 0);
            _btnResetFilters.Click += delegate { ResetFilters(); };
            right.Controls.Add(_btnResetFilters);

            _btnExport = new RoundButton();
            _btnExport.Text = "Экспорт CSV";
            _btnExport.Size = new Size(112, 32);
            _btnExport.Margin = new Padding(0, 1, 0, 0);
            _btnExport.Click += delegate { ExportCurrentTab(); };
            right.Controls.Add(_btnExport);

            RefreshCategoryCombo();
        }

        private void BuildTabs()
        {
            _tabsBar = new BorderPanel();
            _tabsBar.Dock = DockStyle.Fill;
            _tabsBar.DrawBottom = true;
            _tabsBar.BorderColor = Theme.Border;
            _tabsBar.Margin = new Padding(0);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320f));
            layout.RowCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Margin = new Padding(0);
            _tabsBar.Controls.Add(layout);

            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = false;
            flow.BackColor = Theme.Surface;
            flow.Padding = new Padding(16, 2, 0, 0);
            flow.Margin = new Padding(0);
            layout.Controls.Add(flow, 0, 0);

            string[] names = new string[] { "Соединения", "Трафик", "Программы", "Домены", "Категории" };
            _tabs = new NavTabButton[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                NavTabButton tab = new NavTabButton();
                tab.Text = names[i];
                tab.Click += delegate { SelectTab(index); };
                _tabs[i] = tab;
                flow.Controls.Add(tab);
            }
            _tabs[0].Checked = true;

            _lblPeriodInfo = Ui.Label("", Theme.FontUiSmall, Theme.Muted);
            _lblPeriodInfo.Dock = DockStyle.Fill;
            _lblPeriodInfo.TextAlign = ContentAlignment.MiddleRight;
            _lblPeriodInfo.Padding = new Padding(0, 0, 16, 0);
            layout.Controls.Add(_lblPeriodInfo, 1, 0);
        }

        private Control BuildStatusHost()
        {
            BorderPanel host = new BorderPanel();
            host.Dock = DockStyle.Fill;
            host.DrawTop = true;
            host.BorderColor = Theme.Border;
            host.Margin = new Padding(0);
            host.Padding = new Padding(16, 3, 16, 2);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390f));
            layout.RowCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Margin = new Padding(0);
            host.Controls.Add(layout);

            _lblStatus = Ui.Label("Готово", Theme.FontUiSmall, Theme.MutedDark);
            _lblStatus.Dock = DockStyle.Fill;
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            _lblStatus.AutoEllipsis = true;
            layout.Controls.Add(_lblStatus, 0, 0);

            _statusProgress = new ProgressBar();
            _statusProgress.Style = ProgressBarStyle.Marquee;
            _statusProgress.MarqueeAnimationSpeed = 25;
            _statusProgress.Dock = DockStyle.Fill;
            _statusProgress.Margin = new Padding(8, 6, 8, 5);
            _statusProgress.Visible = false;
            layout.Controls.Add(_statusProgress, 1, 0);

            _lblStatusRight = Ui.Label("", Theme.FontUiSmall, Theme.Muted);
            _lblStatusRight.Dock = DockStyle.Fill;
            _lblStatusRight.TextAlign = ContentAlignment.MiddleRight;
            _lblStatusRight.AutoEllipsis = true;
            layout.Controls.Add(_lblStatusRight, 2, 0);

            _lblStatusRight.Text = "SQLite " + NativeSqlite.Version();
            return host;
        }

        private Panel CreatePage()
        {
            Panel page = new Panel();
            page.Dock = DockStyle.Fill;
            page.BackColor = Theme.Bg;
            page.Visible = false;
            return page;
        }

        private void MainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_categoriesDirty && !_closing)
            {
                DialogResult answer = MessageBox.Show(
                    this,
                    "Есть несохранённые изменения категорий. Сохранить их перед закрытием?",
                    "NetLimiter Parser",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (answer == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (answer == DialogResult.Yes && !SaveCategories(false))
                {
                    e.Cancel = true;
                    return;
                }
            }

            _closing = true;
            if (_cts != null) _cts.Cancel();
            SaveSettings();
        }

        private void SaveSettings()
        {
            try
            {
                Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                if (bounds.Width > 0) _settings.WindowWidth = bounds.Width;
                if (bounds.Height > 0) _settings.WindowHeight = bounds.Height;
                _settings.Maximized = WindowState == FormWindowState.Maximized;
                _settings.LastTab = _currentTab;
                SettingsStore.Save(_settings);
            }
            catch (Exception ex)
            {
                Log.Error("Не удалось сохранить настройки окна.", ex);
            }
        }

        private void ApplyWindowSettings()
        {
            int width = _settings.WindowWidth;
            int height = _settings.WindowHeight;
            if (width < 1280) width = 1500;
            if (height < 720) height = 900;

            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            if (width > work.Width) width = work.Width;
            if (height > work.Height) height = work.Height;

            Size = new Size(width, height);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(
                work.Left + Math.Max(0, (work.Width - width) / 2),
                work.Top + Math.Max(0, (work.Height - height) / 2));

            if (_settings.Maximized)
                WindowState = FormWindowState.Maximized;
        }

        private void UpdateHeaderPaths()
        {
            string path = _settings.LastDbPath;
            _lblHeaderPath.Text = "База: " + (string.IsNullOrWhiteSpace(path) ? "не выбрана" : path);
            _tips.SetToolTip(_lblHeaderPath, _lblHeaderPath.Text);
        }

        private void SetStatus(string text)
        {
            if (_lblStatus != null) _lblStatus.Text = text ?? "";
        }
    }
}
