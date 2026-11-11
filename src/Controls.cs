using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal static class UiGeometry
    {
        public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return path;

            float limit = Math.Min(bounds.Width, bounds.Height) / 2f;
            if (radius > limit) radius = limit;
            if (radius <= 0.5f)
            {
                path.AddRectangle(bounds);
                return path;
            }

            float d = radius * 2f;
            path.AddArc(bounds.X, bounds.Y, d, d, 180f, 90f);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270f, 90f);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0f, 90f);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class RoundButton : Control, IButtonControl
    {
        private bool _hover;
        private bool _pressed;
        private DialogResult _dialogResult;

        public bool Primary { get; set; }
        public bool Quiet { get; set; }
        public bool Danger { get; set; }
        public bool Inverted { get; set; }
        public int CornerRadius { get; set; }

        public DialogResult DialogResult
        {
            get { return _dialogResult; }
            set { _dialogResult = value; }
        }

        public void NotifyDefault(bool value)
        {
        }

        public void PerformClick()
        {
            if (Enabled && Visible)
                OnClick(EventArgs.Empty);
        }

        public RoundButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Font = Theme.FontUi;
            Cursor = Cursors.Hand;
            CornerRadius = 8;
            TabStop = true;
            Height = 32;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Color fill;
            Color border;
            Color text;
            bool drawBorder = !Quiet && !Primary && !Danger;

            if (!Enabled)
            {
                fill = Theme.SurfaceAlt;
                border = Theme.Border;
                text = Theme.Muted;
                drawBorder = true;
            }
            else if (Primary)
            {
                fill = _pressed ? Theme.AccentDark : (_hover ? Theme.AccentDark : Theme.Accent);
                border = fill;
                text = Color.White;
            }
            else if (Danger)
            {
                fill = _pressed ? Theme.Darken(Theme.Red, 0.15) : (_hover ? Theme.Red : Theme.Lighten(Theme.Red, 0.04));
                border = fill;
                text = Color.White;
            }
            else if (Quiet)
            {
                if (Inverted)
                {
                    fill = _hover ? Color.FromArgb(45, 255, 255, 255) : Color.FromArgb(22, 255, 255, 255);
                    border = Color.FromArgb(85, 255, 255, 255);
                    text = Color.White;
                }
                else
                {
                    fill = _hover ? Theme.AccentSoft : Color.Transparent;
                    border = Color.Transparent;
                    text = _hover ? Theme.Accent : Theme.MutedDark;
                }
            }
            else
            {
                fill = _pressed ? Theme.Border : (_hover ? Theme.SurfaceAlt : Theme.Surface);
                border = _hover ? Theme.BorderStrong : Theme.Border;
                text = Theme.Text;
            }

            using (GraphicsPath path = UiGeometry.RoundedRectangle(rect, CornerRadius))
            {
                if (fill != Color.Transparent)
                {
                    using (SolidBrush brush = new SolidBrush(fill))
                        e.Graphics.FillPath(brush, path);
                }
                if (drawBorder || (Quiet && Inverted))
                {
                    using (Pen pen = new Pen(border, 1f))
                        e.Graphics.DrawPath(pen, path);
                }
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text ?? "",
                Font,
                new Rectangle(4, 0, Math.Max(0, Width - 8), Height),
                text,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);

            if (Focused && Enabled && ShowFocusCues)
            {
                Rectangle focus = new Rectangle(3, 3, Math.Max(0, Width - 7), Math.Max(0, Height - 7));
                ControlPaint.DrawFocusRectangle(e.Graphics, focus, text, fill);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }
    }

    internal sealed class ChipButton : Control
    {
        private bool _hover;
        private bool _checked;

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
            }
        }

        public ChipButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Font = Theme.FontUiSmall;
            Cursor = Cursors.Hand;
            AutoSize = true;
            Height = 28;
            TabStop = true;
            Margin = new Padding(0, 0, 6, 0);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = TextRenderer.MeasureText(Text ?? "", Font);
            return new Size(text.Width + 24, 28);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Color fill;
            Color border;
            Color text;

            if (_checked)
            {
                fill = _hover ? Theme.AccentDark : Theme.Accent;
                border = fill;
                text = Color.White;
            }
            else
            {
                fill = _hover ? Theme.AccentSoft : Theme.Surface;
                border = _hover ? Theme.BorderStrong : Theme.Border;
                text = _hover ? Theme.Accent : Theme.MutedDark;
            }

            using (GraphicsPath path = UiGeometry.RoundedRectangle(rect, Height / 2f))
            {
                using (SolidBrush brush = new SolidBrush(fill))
                    e.Graphics.FillPath(brush, path);
                using (Pen pen = new Pen(border, 1f))
                    e.Graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text ?? "",
                Font,
                new Rectangle(2, 0, Math.Max(0, Width - 4), Height),
                text,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
    }

    internal sealed class NavTabButton : Control
    {
        private bool _hover;
        private bool _checked;

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
            }
        }

        public NavTabButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Font = Theme.FontUi;
            Cursor = Cursors.Hand;
            AutoSize = true;
            Height = 42;
            TabStop = true;
            Margin = new Padding(0, 0, 4, 0);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Font measure = _checked ? Theme.FontBold : Theme.FontUi;
            Size text = TextRenderer.MeasureText(Text ?? "", measure);
            return new Size(text.Width + 32, 42);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hover && !_checked)
            {
                using (SolidBrush brush = new SolidBrush(Theme.AccentSoft))
                    e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            Font font = _checked ? Theme.FontBold : Theme.FontUi;
            Color color = _checked ? Theme.Accent : (_hover ? Theme.Text : Theme.MutedDark);

            TextRenderer.DrawText(
                e.Graphics,
                Text ?? "",
                font,
                new Rectangle(6, 0, Math.Max(0, Width - 12), Height - 3),
                color,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);

            if (_checked)
            {
                using (SolidBrush brush = new SolidBrush(Theme.Accent))
                    e.Graphics.FillRectangle(brush, 10, Height - 3, Math.Max(0, Width - 20), 3);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
    }

    internal sealed class LogoBadge : Control
    {
        public LogoBadge()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Size = new Size(46, 46);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (GraphicsPath path = UiGeometry.RoundedRectangle(rect, 12f))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)),
                    Color.FromArgb(59, 130, 246),
                    Color.FromArgb(37, 99, 235),
                    45f))
                {
                    e.Graphics.FillPath(brush, path);
                }
                using (Pen pen = new Pen(Color.FromArgb(110, 255, 255, 255), 1f))
                    e.Graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                "NL",
                Theme.FontLogo,
                ClientRectangle,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    internal sealed class GradientPanel : Panel
    {
        public Color TopColor { get; set; }
        public Color BottomColor { get; set; }

        public GradientPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            TopColor = Theme.HeaderTop;
            BottomColor = Theme.HeaderBottom;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(
                ClientRectangle,
                TopColor,
                BottomColor,
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }
    }

    internal sealed class BorderPanel : Panel
    {
        public Color BorderColor { get; set; }
        public bool DrawTop { get; set; }
        public bool DrawBottom { get; set; }
        public bool DrawLeft { get; set; }
        public bool DrawRight { get; set; }

        public BorderPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            BorderColor = Theme.Border;
            BackColor = Theme.Surface;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(BorderColor, 1f))
            {
                if (DrawTop) e.Graphics.DrawLine(pen, 0, 0, Width, 0);
                if (DrawBottom) e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
                if (DrawLeft) e.Graphics.DrawLine(pen, 0, 0, 0, Height);
                if (DrawRight) e.Graphics.DrawLine(pen, Width - 1, 0, Width - 1, Height);
            }
        }
    }

    internal sealed class CardPanel : Panel
    {
        private int _radius;
        private bool _showBorder;

        public int Radius
        {
            get { return _radius; }
            set
            {
                _radius = Math.Max(0, value);
                UpdateRegion();
                Invalidate();
            }
        }

        public bool ShowBorder
        {
            get { return _showBorder; }
            set
            {
                _showBorder = value;
                Invalidate();
            }
        }

        public CardPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            BackColor = Theme.Surface;
            _radius = 10;
            _showBorder = true;
            Padding = new Padding(1);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath path = UiGeometry.RoundedRectangle(new RectangleF(0, 0, Width, Height), _radius))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_showBorder)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = UiGeometry.RoundedRectangle(
                    new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), _radius))
                using (Pen pen = new Pen(Theme.Border, 1f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }

    internal sealed class SearchBox : Control
    {
        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private readonly TextBox _input;
        private readonly string _cue;
        private Rectangle _clearRect;
        private bool _focused;

        public TextBox Input
        {
            get { return _input; }
        }

        public SearchBox(string cue)
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            BackColor = Theme.Surface;
            Height = 34;
            Size = new Size(260, 34);
            _cue = cue ?? "";

            _input = new TextBox();
            _input.BorderStyle = BorderStyle.None;
            _input.Font = Theme.FontUi;
            _input.ForeColor = Theme.Text;
            _input.BackColor = Theme.Surface;
            _input.TabStop = true;
            _input.TextChanged += delegate { Invalidate(); OnTextChanged(EventArgs.Empty); };
            _input.Enter += delegate { _focused = true; Invalidate(); };
            _input.Leave += delegate { _focused = false; Invalidate(); };
            Controls.Add(_input);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _input.Handle.ToString();
            SendMessage(_input.Handle, EM_SETCUEBANNER, new IntPtr(1), _cue);
        }

        public override string Text
        {
            get { return _input.Text; }
            set { _input.Text = value ?? ""; }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInput();
        }

        private void LayoutInput()
        {
            if (_input == null) return;
            int rightReserve = 30;
            _input.SetBounds(31, 8, Math.Max(20, Width - 31 - rightReserve), 19);
            _clearRect = new Rectangle(Width - 26, 7, 20, 20);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = UiGeometry.RoundedRectangle(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 7f))
            {
                using (SolidBrush brush = new SolidBrush(Theme.Surface))
                    e.Graphics.FillPath(brush, path);
                using (Pen pen = new Pen(_focused ? Theme.Accent : Theme.Border, 1f))
                    e.Graphics.DrawPath(pen, path);
            }

            using (Pen pen = new Pen(Theme.Muted, 1.6f))
            {
                e.Graphics.DrawEllipse(pen, 10f, 12f, 9f, 9f);
                e.Graphics.DrawLine(pen, 18f, 20f, 22f, 24f);
            }

            if (!string.IsNullOrEmpty(_input.Text))
            {
                using (Font font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point))
                {
                    TextRenderer.DrawText(
                        e.Graphics,
                        "×",
                        font,
                        _clearRect,
                        Theme.Muted,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!string.IsNullOrEmpty(_input.Text) && _clearRect.Contains(e.Location))
            {
                _input.Text = "";
                _input.Focus();
            }
            else
            {
                _input.Focus();
            }
            base.OnMouseDown(e);
        }
    }

    internal sealed class BufferedDataGridView : DataGridView
    {
        private string _emptyText;
        private string _emptyHint;

        public string EmptyText
        {
            get { return _emptyText; }
            set { _emptyText = value; Invalidate(); }
        }

        public string EmptyHint
        {
            get { return _emptyHint; }
            set { _emptyHint = value; Invalidate(); }
        }

        public BufferedDataGridView()
        {
            DoubleBuffered = true;
            BackgroundColor = Theme.Surface;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            GridColor = Theme.Border;
            RowHeadersVisible = false;
            EnableHeadersVisualStyles = false;
            ColumnHeadersHeight = 34;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            AllowUserToOrderColumns = false;
            ReadOnly = true;
            MultiSelect = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            AutoGenerateColumns = false;
            ShowCellToolTips = true;
            RowTemplate.Height = 27;
            Font = Theme.FontUiSmall;

            ColumnHeadersDefaultCellStyle.BackColor = Theme.SurfaceAlt;
            ColumnHeadersDefaultCellStyle.ForeColor = Theme.MutedDark;
            ColumnHeadersDefaultCellStyle.Font = Theme.FontUiSmallBold;
            ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.SurfaceAlt;
            ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.MutedDark;
            ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            DefaultCellStyle.BackColor = Theme.Surface;
            DefaultCellStyle.ForeColor = Theme.Text;
            DefaultCellStyle.SelectionBackColor = Theme.AccentSoft;
            DefaultCellStyle.SelectionForeColor = Theme.Text;
            DefaultCellStyle.Font = Theme.FontUiSmall;
            DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(251, 253, 255);
            AlternatingRowsDefaultCellStyle.SelectionBackColor = Theme.AccentSoft;
            _emptyText = "Нет данных";
            _emptyHint = "Измените период или фильтры";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (RowCount == 0 && Visible && Height > ColumnHeadersHeight + 40)
            {
                Rectangle area = new Rectangle(0, ColumnHeadersHeight, Width, Height - ColumnHeadersHeight);
                Rectangle titleRect = new Rectangle(area.X + 20, area.Y + area.Height / 2 - 30, area.Width - 40, 26);
                Rectangle hintRect = new Rectangle(area.X + 20, area.Y + area.Height / 2 + 1, area.Width - 40, 22);
                TextRenderer.DrawText(
                    e.Graphics,
                    _emptyText ?? "",
                    Theme.FontSection,
                    titleRect,
                    Theme.MutedDark,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                if (!string.IsNullOrEmpty(_emptyHint))
                {
                    TextRenderer.DrawText(
                        e.Graphics,
                        _emptyHint,
                        Theme.FontUi,
                        hintRect,
                        Theme.Muted,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
            }
        }
    }

    internal sealed class GridColumn<T>
    {
        public string Header { get; set; }
        public int Width { get; set; }
        public Func<T, string> Value { get; set; }
        public Func<T, string> ToolTip { get; set; }
        public Func<T, Color> Background { get; set; }
        public Func<T, Color> Foreground { get; set; }
        public Func<T, object> SortKey { get; set; }
        public DataGridViewContentAlignment Alignment { get; set; }
        public bool Bold { get; set; }
        public bool Fill { get; set; }
        public float FillWeight { get; set; }
        public bool DescendingFirst { get; set; }

        public GridColumn()
        {
            Width = 120;
            Alignment = DataGridViewContentAlignment.MiddleLeft;
            FillWeight = 100f;
        }
    }

    internal sealed class VirtualGrid<T> where T : class
    {
        private readonly BufferedDataGridView _grid;
        private readonly List<GridColumn<T>> _columns;
        private readonly List<T> _items;
        private int _sortColumn;
        private bool _sortAscending;

        public BufferedDataGridView Grid
        {
            get { return _grid; }
        }

        public List<T> Items
        {
            get { return _items; }
        }

        public List<GridColumn<T>> Columns
        {
            get { return _columns; }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public VirtualGrid(BufferedDataGridView grid, List<GridColumn<T>> columns)
        {
            _grid = grid;
            _columns = columns ?? new List<GridColumn<T>>();
            _items = new List<T>();
            _sortColumn = -1;
            _sortAscending = true;

            _grid.VirtualMode = true;
            _grid.AutoGenerateColumns = false;
            _grid.RowCount = 0;
            _grid.Columns.Clear();

            for (int i = 0; i < _columns.Count; i++)
            {
                GridColumn<T> definition = _columns[i];
                DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                column.Name = "vc" + i;
                column.HeaderText = definition.Header ?? "";
                column.Width = Math.Max(40, definition.Width);
                column.MinimumWidth = 40;
                column.SortMode = DataGridViewColumnSortMode.Programmatic;
                column.DefaultCellStyle.Alignment = definition.Alignment;
                column.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
                if (definition.Fill)
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    column.FillWeight = definition.FillWeight;
                }
                _grid.Columns.Add(column);
            }

            _grid.CellValueNeeded += GridCellValueNeeded;
            _grid.CellFormatting += GridCellFormatting;
            _grid.CellToolTipTextNeeded += GridCellToolTipTextNeeded;
            _grid.ColumnHeaderMouseClick += GridColumnHeaderMouseClick;
        }

        public void SetItems(List<T> items)
        {
            _items.Clear();
            if (items != null) _items.AddRange(items);

            if (_sortColumn >= 0 && _sortColumn < _columns.Count)
                SortItemsInternal();

            _grid.RowCount = 0;
            _grid.RowCount = _items.Count;
            _grid.Invalidate();
        }

        public void Clear()
        {
            SetItems(null);
        }

        public T GetItem(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _items.Count) return null;
            return _items[rowIndex];
        }

        public T SelectedItem
        {
            get
            {
                if (_grid.CurrentCell == null) return null;
                return GetItem(_grid.CurrentCell.RowIndex);
            }
        }

        private void GridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= _columns.Count) return;

            GridColumn<T> column = _columns[e.ColumnIndex];
            if (column.Value == null) return;
            string value = column.Value(_items[e.RowIndex]);
            e.Value = value ?? "";
        }

        private void GridCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= _columns.Count) return;

            GridColumn<T> column = _columns[e.ColumnIndex];
            T item = _items[e.RowIndex];

            e.CellStyle.BackColor = Theme.Surface;
            e.CellStyle.ForeColor = Theme.Text;
            e.CellStyle.SelectionBackColor = Theme.AccentSoft;
            e.CellStyle.SelectionForeColor = Theme.Text;
            e.CellStyle.Font = column.Bold ? Theme.FontUiSmallBold : Theme.FontUiSmall;

            if (column.Background != null)
            {
                Color color = column.Background(item);
                if (color != Color.Empty)
                {
                    e.CellStyle.BackColor = color;
                    e.CellStyle.SelectionBackColor = Theme.Blend(color, Theme.Accent, 0.20);
                }
            }

            if (column.Foreground != null)
            {
                Color color = column.Foreground(item);
                if (color != Color.Empty)
                {
                    e.CellStyle.ForeColor = color;
                    e.CellStyle.SelectionForeColor = color;
                }
            }
        }

        private void GridCellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= _columns.Count) return;

            GridColumn<T> column = _columns[e.ColumnIndex];
            if (column.ToolTip != null)
                e.ToolTipText = column.ToolTip(_items[e.RowIndex]);
        }

        private void GridColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.ColumnIndex >= _columns.Count) return;
            GridColumn<T> column = _columns[e.ColumnIndex];
            if (column.SortKey == null) return;

            if (_sortColumn == e.ColumnIndex)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = e.ColumnIndex;
                _sortAscending = !column.DescendingFirst;
            }

            SortItemsInternal();

            for (int i = 0; i < _grid.Columns.Count; i++)
                _grid.Columns[i].HeaderCell.SortGlyphDirection = SortOrder.None;
            _grid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = _sortAscending ? SortOrder.Ascending : SortOrder.Descending;
            _grid.Invalidate();
        }

        private void SortItemsInternal()
        {
            if (_sortColumn < 0 || _sortColumn >= _columns.Count) return;
            GridColumn<T> column = _columns[_sortColumn];
            if (column.SortKey == null) return;

            _items.Sort(delegate(T a, T b)
            {
                object valueA = column.SortKey(a);
                object valueB = column.SortKey(b);
                int result = CompareValues(valueA, valueB);
                return _sortAscending ? result : -result;
            });
        }

        private static int CompareValues(object a, object b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            string textA = a as string;
            string textB = b as string;
            if (textA != null && textB != null)
                return StringComparer.CurrentCultureIgnoreCase.Compare(textA, textB);

            IComparable comparable = a as IComparable;
            if (comparable != null)
            {
                try
                {
                    return comparable.CompareTo(b);
                }
                catch
                {
                }
            }

            return StringComparer.CurrentCultureIgnoreCase.Compare(a.ToString(), b.ToString());
        }
    }

    internal sealed class LoadingOverlay : Control
    {
        private readonly Timer _timer;
        private string _message;
        private string _detail;
        private int _progress;
        private bool _marquee;
        private bool _allowCancel;
        private bool _cancelHover;
        private Rectangle _cancelRect;
        private int _tick;

        public event EventHandler CancelRequested;

        public LoadingOverlay()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            BackColor = Theme.Bg;
            Visible = false;
            _message = "";
            _detail = "";
            _progress = -1;
            _marquee = true;
            _timer = new Timer();
            _timer.Interval = 35;
            _timer.Tick += delegate
            {
                _tick++;
                if (_marquee) Invalidate();
            };
        }

        public void ShowOverlay(string message, string detail, bool allowCancel)
        {
            _message = message ?? "";
            _detail = detail ?? "";
            _allowCancel = allowCancel;
            _progress = -1;
            _marquee = true;
            _cancelHover = false;
            _tick = 0;
            Visible = true;
            BringToFront();
            _timer.Start();
            Invalidate();
        }

        public void SetProgress(int percent, string detail)
        {
            if (percent < 0)
            {
                _marquee = true;
            }
            else
            {
                _marquee = false;
                _progress = Math.Max(0, Math.Min(100, percent));
            }
            if (detail != null) _detail = detail;
            if (!_timer.Enabled) _timer.Start();
            Invalidate();
        }

        public void HideOverlay()
        {
            _timer.Stop();
            Visible = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Theme.Bg);

            int cardWidth = 430;
            int cardHeight = 170;
            Rectangle card = new Rectangle(
                Math.Max(12, (Width - cardWidth) / 2),
                Math.Max(12, (Height - cardHeight) / 2),
                Math.Min(cardWidth, Math.Max(180, Width - 24)),
                cardHeight);

            using (GraphicsPath path = UiGeometry.RoundedRectangle(new RectangleF(card.X, card.Y, card.Width, card.Height), 12f))
            {
                using (SolidBrush brush = new SolidBrush(Theme.Surface))
                    e.Graphics.FillPath(brush, path);
                using (Pen pen = new Pen(Theme.Border, 1f))
                    e.Graphics.DrawPath(pen, path);
            }

            int contentWidth = card.Width - 60;
            int left = card.X + 30;

            TextRenderer.DrawText(
                e.Graphics,
                _message,
                Theme.FontSection,
                new Rectangle(left, card.Y + 24, contentWidth, 26),
                Theme.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(
                e.Graphics,
                _detail,
                Theme.FontUi,
                new Rectangle(left, card.Y + 54, contentWidth, 34),
                Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);

            Rectangle bar = new Rectangle(left, card.Y + 100, contentWidth, 7);
            using (GraphicsPath trackPath = UiGeometry.RoundedRectangle(bar, 3.5f))
            {
                using (SolidBrush track = new SolidBrush(Theme.Border))
                    e.Graphics.FillPath(track, trackPath);
            }

            Rectangle fill = bar;
            if (_marquee)
            {
                int segment = Math.Max(60, contentWidth / 3);
                int offset = (_tick * 5) % (contentWidth + segment);
                int x = left + offset - segment;
                int from = Math.Max(left, x);
                int to = Math.Min(left + contentWidth, x + segment);
                if (to > from)
                {
                    fill = new Rectangle(from, bar.Y, to - from, bar.Height);
                }
                else
                {
                    fill = new Rectangle(left, bar.Y, 0, bar.Height);
                }
            }
            else
            {
                fill = new Rectangle(left, bar.Y, contentWidth * _progress / 100, bar.Height);
            }

            if (fill.Width > 2)
            {
                using (GraphicsPath fillPath = UiGeometry.RoundedRectangle(fill, 3.5f))
                using (SolidBrush brush = new SolidBrush(Theme.Accent))
                    e.Graphics.FillPath(brush, fillPath);
            }

            if (_allowCancel)
            {
                _cancelRect = new Rectangle(card.X + card.Width / 2 - 55, card.Y + 122, 110, 30);
                RectangleF buttonRect = new RectangleF(_cancelRect.X + 0.5f, _cancelRect.Y + 0.5f, _cancelRect.Width - 1f, _cancelRect.Height - 1f);
                using (GraphicsPath path = UiGeometry.RoundedRectangle(buttonRect, 7f))
                {
                    if (_cancelHover)
                    {
                        using (SolidBrush brush = new SolidBrush(Theme.AccentSoft))
                            e.Graphics.FillPath(brush, path);
                    }
                    using (Pen pen = new Pen(_cancelHover ? Theme.Accent : Theme.Border, 1f))
                        e.Graphics.DrawPath(pen, path);
                }
                TextRenderer.DrawText(
                    e.Graphics,
                    "Отмена",
                    Theme.FontUi,
                    _cancelRect,
                    _cancelHover ? Theme.Accent : Theme.MutedDark,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
            else
            {
                _cancelRect = Rectangle.Empty;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool hover = _allowCancel && _cancelRect.Contains(e.Location);
            if (hover != _cancelHover)
            {
                _cancelHover = hover;
                Cursor = hover ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _cancelHover = false;
            Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (_allowCancel && _cancelRect.Contains(e.Location))
            {
                EventHandler handler = CancelRequested;
                if (handler != null) handler(this, EventArgs.Empty);
            }
            base.OnMouseClick(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class ModernColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground
        {
            get { return Theme.Surface; }
        }

        public override Color MenuBorder
        {
            get { return Theme.Border; }
        }

        public override Color MenuItemBorder
        {
            get { return Theme.AccentSoft; }
        }

        public override Color MenuItemSelected
        {
            get { return Theme.AccentSoft; }
        }

        public override Color MenuItemSelectedGradientBegin
        {
            get { return Theme.AccentSoft; }
        }

        public override Color MenuItemSelectedGradientEnd
        {
            get { return Theme.AccentSoft; }
        }

        public override Color MenuItemPressedGradientBegin
        {
            get { return Theme.AccentSoft; }
        }

        public override Color MenuItemPressedGradientEnd
        {
            get { return Theme.AccentSoft; }
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Theme.Surface; }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return Theme.Surface; }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return Theme.Surface; }
        }

        public override Color SeparatorDark
        {
            get { return Theme.Border; }
        }

        public override Color SeparatorLight
        {
            get { return Theme.SurfaceAlt; }
        }
    }

    internal static class Ui
    {
        public static ContextMenuStrip CreateMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = Theme.FontUi;
            menu.BackColor = Theme.Surface;
            menu.ForeColor = Theme.Text;
            menu.Renderer = new ToolStripProfessionalRenderer(new ModernColorTable());
            menu.ShowImageMargin = false;
            return menu;
        }

        public static Label Label(string text, Font font, Color color)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = font;
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.AutoSize = false;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        public static ComboBox Combo(int width)
        {
            ComboBox combo = new ComboBox();
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.FlatStyle = FlatStyle.Flat;
            combo.Font = Theme.FontUi;
            combo.BackColor = Theme.Surface;
            combo.ForeColor = Theme.Text;
            combo.Width = width;
            combo.Height = 28;
            combo.Margin = new Padding(0, 2, 10, 0);
            return combo;
        }
    }
}
