using System;
using System.Drawing;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed partial class MainForm
    {
        private Panel BuildCategoriesPage()
        {
            Panel page = CreatePage();

            TableLayoutPanel outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 2;
            outer.RowCount = 1;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350f));
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            outer.Margin = new Padding(0);
            page.Controls.Add(outer);

            outer.Controls.Add(BuildCategoryListCard(), 0, 0);
            outer.Controls.Add(BuildCategoryEditorArea(), 1, 0);

            return page;
        }

        private Control BuildCategoryListCard()
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, 12, 0);
            card.Padding = new Padding(0);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 5;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            layout.Padding = new Padding(14, 10, 14, 10);
            layout.Margin = new Padding(0);
            card.Controls.Add(layout);

            Label title = Ui.Label("Категории и заготовки", Theme.FontSection, Theme.Text);
            title.Dock = DockStyle.Fill;
            layout.Controls.Add(title, 0, 0);

            _catList = new ListBox();
            _catList.Dock = DockStyle.Fill;
            _catList.BorderStyle = BorderStyle.FixedSingle;
            _catList.Font = Theme.FontUi;
            _catList.DrawMode = DrawMode.OwnerDrawFixed;
            _catList.ItemHeight = 28;
            _catList.IntegralHeight = false;
            _catList.BackColor = Theme.Surface;
            _catList.ForeColor = Theme.Text;
            _catList.Margin = new Padding(0, 0, 0, 8);
            _catList.DrawItem += CategoryListDrawItem;
            _catList.SelectedIndexChanged += delegate { CategorySelectionChanged(); };
            layout.Controls.Add(_catList, 0, 1);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = false;
            actions.Margin = new Padding(0);
            layout.Controls.Add(actions, 0, 2);

            _btnCatAdd = MakeCategoryButton("Добавить", 108);
            _btnCatAdd.Click += delegate { AddCategory(); };
            actions.Controls.Add(_btnCatAdd);

            _btnCatDelete = MakeCategoryButton("Удалить", 96);
            _btnCatDelete.Click += delegate { DeleteCategory(); };
            actions.Controls.Add(_btnCatDelete);

            FlowLayoutPanel moveActions = new FlowLayoutPanel();
            moveActions.Dock = DockStyle.Fill;
            moveActions.FlowDirection = FlowDirection.LeftToRight;
            moveActions.WrapContents = false;
            moveActions.Margin = new Padding(0);
            layout.Controls.Add(moveActions, 0, 3);

            _btnCatUp = MakeCategoryButton("Выше", 90);
            _btnCatUp.Click += delegate { MoveCategory(-1); };
            moveActions.Controls.Add(_btnCatUp);

            _btnCatDown = MakeCategoryButton("Ниже", 90);
            _btnCatDown.Click += delegate { MoveCategory(1); };
            moveActions.Controls.Add(_btnCatDown);

            Label hint = Ui.Label(
                "Порядок категорий влияет на приоритет при одинаковом совпадении правил.",
                Theme.FontUiSmall,
                Theme.Muted);
            hint.Dock = DockStyle.Fill;
            hint.AutoEllipsis = true;
            layout.Controls.Add(hint, 0, 4);

            return card;
        }

        private Control BuildCategoryEditorArea()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Margin = new Padding(0);

            layout.Controls.Add(BuildCategoryEditorCard(), 0, 0);
            layout.Controls.Add(BuildRulesCard(), 0, 1);
            return layout;
        }

        private Control BuildCategoryEditorCard()
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, 0, 12);
            card.Padding = new Padding(0);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 4;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Padding = new Padding(16, 10, 16, 10);
            layout.Margin = new Padding(0);
            card.Controls.Add(layout);

            Label title = Ui.Label("Редактор категории", Theme.FontSection, Theme.Text);
            title.Dock = DockStyle.Fill;
            layout.Controls.Add(title, 0, 0);
            layout.SetColumnSpan(title, 3);

            _lblCatDirty = Ui.Label("Сохранено", Theme.FontUiSmall, Theme.Green);
            _lblCatDirty.Dock = DockStyle.Fill;
            _lblCatDirty.TextAlign = ContentAlignment.MiddleRight;
            layout.Controls.Add(_lblCatDirty, 3, 0);

            Label nameLabel = Ui.Label("Название", Theme.FontUiSmall, Theme.MutedDark);
            nameLabel.Dock = DockStyle.Fill;
            layout.Controls.Add(nameLabel, 0, 1);

            _txtCatName = new TextBox();
            _txtCatName.Dock = DockStyle.Fill;
            _txtCatName.Font = Theme.FontUi;
            _txtCatName.BorderStyle = BorderStyle.FixedSingle;
            _txtCatName.Margin = new Padding(0, 5, 12, 5);
            _txtCatName.TextChanged += delegate { CategoryNameChanged(); };
            layout.Controls.Add(_txtCatName, 1, 1);

            Label colorLabel = Ui.Label("Цвет", Theme.FontUiSmall, Theme.MutedDark);
            colorLabel.Dock = DockStyle.Fill;
            layout.Controls.Add(colorLabel, 2, 1);

            FlowLayoutPanel colorFlow = new FlowLayoutPanel();
            colorFlow.Dock = DockStyle.Fill;
            colorFlow.FlowDirection = FlowDirection.LeftToRight;
            colorFlow.WrapContents = false;
            colorFlow.Margin = new Padding(0, 2, 0, 0);
            layout.Controls.Add(colorFlow, 3, 1);

            _colorSwatch = new Panel();
            _colorSwatch.Size = new Size(28, 28);
            _colorSwatch.Margin = new Padding(0, 1, 8, 0);
            _colorSwatch.BorderStyle = BorderStyle.FixedSingle;
            _colorSwatch.Cursor = Cursors.Hand;
            _colorSwatch.BackColor = Theme.SurfaceAlt;
            _colorSwatch.Click += delegate { ChooseCategoryColor(); };
            colorFlow.Controls.Add(_colorSwatch);

            RoundButton chooseColor = new RoundButton();
            chooseColor.Text = "Изменить цвет";
            chooseColor.Size = new Size(126, 30);
            chooseColor.Margin = new Padding(0, 0, 10, 0);
            chooseColor.Click += delegate { ChooseCategoryColor(); };
            colorFlow.Controls.Add(chooseColor);

            _chkCatEnabled = new CheckBox();
            _chkCatEnabled.Text = "Категория включена";
            _chkCatEnabled.Font = Theme.FontUi;
            _chkCatEnabled.ForeColor = Theme.Text;
            _chkCatEnabled.AutoSize = true;
            _chkCatEnabled.Margin = new Padding(0, 2, 0, 0);
            _chkCatEnabled.CheckedChanged += delegate { CategoryEnabledChanged(); };
            layout.Controls.Add(_chkCatEnabled, 1, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0);
            layout.Controls.Add(buttons, 3, 2);

            _btnCatSave = new RoundButton();
            _btnCatSave.Text = "Сохранить";
            _btnCatSave.Primary = true;
            _btnCatSave.Size = new Size(106, 32);
            _btnCatSave.Margin = new Padding(0, 0, 8, 0);
            _btnCatSave.Click += delegate { SaveCategories(true); };
            buttons.Controls.Add(_btnCatSave);

            _btnCatReset = new RoundButton();
            _btnCatReset.Text = "Сброс";
            _btnCatReset.Size = new Size(82, 32);
            _btnCatReset.Margin = new Padding(0, 0, 8, 0);
            _btnCatReset.Click += delegate { ResetCategories(); };
            buttons.Controls.Add(_btnCatReset);

            _btnCatTest = new RoundButton();
            _btnCatTest.Text = "Тест…";
            _btnCatTest.Size = new Size(82, 32);
            _btnCatTest.Margin = new Padding(0);
            _btnCatTest.Click += delegate { TestClassification(); };
            buttons.Controls.Add(_btnCatTest);

            return card;
        }

        private Control BuildRulesCard()
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0);
            card.Padding = new Padding(0);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Padding = new Padding(14, 8, 14, 12);
            layout.Margin = new Padding(0);
            card.Controls.Add(layout);

            TableLayoutPanel header = new TableLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.ColumnCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.RowCount = 1;
            header.Margin = new Padding(0);
            layout.Controls.Add(header, 0, 0);

            Label title = Ui.Label("Правила выбранной категории", Theme.FontSection, Theme.Text);
            title.Dock = DockStyle.Fill;
            header.Controls.Add(title, 0, 0);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = false;
            actions.AutoSize = true;
            actions.Margin = new Padding(0);
            header.Controls.Add(actions, 1, 0);

            _btnRuleAdd = new RoundButton();
            _btnRuleAdd.Text = "Добавить правило";
            _btnRuleAdd.Size = new Size(132, 32);
            _btnRuleAdd.Margin = new Padding(0, 0, 8, 0);
            _btnRuleAdd.Click += delegate { AddRule(); };
            actions.Controls.Add(_btnRuleAdd);

            _btnRuleEdit = new RoundButton();
            _btnRuleEdit.Text = "Изменить";
            _btnRuleEdit.Size = new Size(92, 32);
            _btnRuleEdit.Margin = new Padding(0, 0, 8, 0);
            _btnRuleEdit.Click += delegate { EditRule(); };
            actions.Controls.Add(_btnRuleEdit);

            _btnRuleDelete = new RoundButton();
            _btnRuleDelete.Text = "Удалить";
            _btnRuleDelete.Size = new Size(86, 32);
            _btnRuleDelete.Margin = new Padding(0);
            _btnRuleDelete.Click += delegate { DeleteRule(); };
            actions.Controls.Add(_btnRuleDelete);

            _gridRules = new BufferedDataGridView();
            _gridRules.Dock = DockStyle.Fill;
            _gridRules.Margin = new Padding(0);
            _gridRules.MultiSelect = false;
            _gridRules.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _gridRules.AllowUserToAddRows = false;
            _gridRules.AllowUserToDeleteRows = false;
            _gridRules.AllowUserToResizeRows = false;
            _gridRules.ReadOnly = true;
            _gridRules.AutoGenerateColumns = false;
            _gridRules.RowHeadersVisible = false;
            _gridRules.ColumnHeadersHeight = 32;
            _gridRules.BackgroundColor = Theme.Surface;
            _gridRules.BorderStyle = BorderStyle.None;
            _gridRules.GridColor = Theme.Border;
            _gridRules.Font = Theme.FontUiSmall;
            _gridRules.EnableHeadersVisualStyles = false;
            _gridRules.ColumnHeadersDefaultCellStyle.BackColor = Theme.SurfaceAlt;
            _gridRules.ColumnHeadersDefaultCellStyle.ForeColor = Theme.MutedDark;
            _gridRules.ColumnHeadersDefaultCellStyle.Font = Theme.FontUiSmallBold;
            _gridRules.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            _gridRules.DefaultCellStyle.BackColor = Theme.Surface;
            _gridRules.DefaultCellStyle.ForeColor = Theme.Text;
            _gridRules.DefaultCellStyle.SelectionBackColor = Theme.AccentSoft;
            _gridRules.DefaultCellStyle.SelectionForeColor = Theme.Text;
            _gridRules.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            _gridRules.RowTemplate.Height = 28;
            _gridRules.EmptyText = "У выбранной категории нет правил";
            _gridRules.EmptyHint = "Нажмите «Добавить правило»";

            DataGridViewTextBoxColumn typeColumn = new DataGridViewTextBoxColumn();
            typeColumn.HeaderText = "Тип";
            typeColumn.Name = "ruleType";
            typeColumn.Width = 128;
            typeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            _gridRules.Columns.Add(typeColumn);

            DataGridViewTextBoxColumn patternColumn = new DataGridViewTextBoxColumn();
            patternColumn.HeaderText = "Шаблон";
            patternColumn.Name = "rulePattern";
            patternColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            patternColumn.FillWeight = 100f;
            patternColumn.MinimumWidth = 160;
            patternColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            _gridRules.Columns.Add(patternColumn);

            DataGridViewTextBoxColumn noteColumn = new DataGridViewTextBoxColumn();
            noteColumn.HeaderText = "Примечание";
            noteColumn.Name = "ruleNote";
            noteColumn.Width = 260;
            noteColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            _gridRules.Columns.Add(noteColumn);

            _gridRules.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) EditRule();
            };
            _gridRules.SelectionChanged += delegate { UpdateRuleButtons(); };
            layout.Controls.Add(_gridRules, 0, 1);

            return card;
        }

        private RoundButton MakeCategoryButton(string text, int width)
        {
            RoundButton button = new RoundButton();
            button.Text = text;
            button.Size = new Size(width, 32);
            button.Margin = new Padding(0, 0, 8, 0);
            return button;
        }
    }
}
