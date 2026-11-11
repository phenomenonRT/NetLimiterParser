using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed partial class MainForm
    {
        private CategoryDef SelectedCategory
        {
            get
            {
                if (_catList == null) return null;
                int index = _catList.SelectedIndex;
                if (index < 0 || index >= _categories.Count) return null;
                return _categories[index];
            }
        }

        private void RefreshCategoryEditor()
        {
            RefreshCategoryList(_catList == null ? 0 : _catList.SelectedIndex);
        }

        private void RefreshCategoryList()
        {
            RefreshCategoryList(_catList == null ? 0 : _catList.SelectedIndex);
        }

        private void RefreshCategoryList(int selectIndex)
        {
            if (_catList == null) return;

            _suppressCategoryEvents = true;
            try
            {
                _catList.BeginUpdate();
                _catList.Items.Clear();
                foreach (CategoryDef category in _categories)
                    _catList.Items.Add(category.Name);

                if (_categories.Count == 0)
                    selectIndex = -1;
                else
                {
                    if (selectIndex < 0) selectIndex = 0;
                    if (selectIndex >= _categories.Count) selectIndex = _categories.Count - 1;
                }
                _catList.SelectedIndex = selectIndex;
            }
            finally
            {
                _catList.EndUpdate();
                _suppressCategoryEvents = false;
            }

            CategorySelectionChanged();
        }

        private void CategorySelectionChanged()
        {
            if (_suppressCategoryEvents) return;

            CategoryDef category = SelectedCategory;
            _suppressCategoryEvents = true;
            try
            {
                _txtCatName.Text = category == null ? "" : category.Name;
                _txtCatName.Enabled = category != null;
                _colorSwatch.BackColor = category == null ? Theme.SurfaceAlt : category.DisplayColor;
                _chkCatEnabled.Checked = category == null ? true : category.Enabled;
                _chkCatEnabled.Enabled = category != null;
                RefreshRulesGrid();
            }
            finally
            {
                _suppressCategoryEvents = false;
            }

            UpdateCategoryButtons();
        }

        private void CategoryListDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _categories.Count) return;
            CategoryDef category = _categories[e.Index];

            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color background = selected ? Theme.AccentSoft : Theme.Surface;
            using (SolidBrush brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, e.Bounds);

            int dotSize = 11;
            Rectangle dot = new Rectangle(
                e.Bounds.Left + 8,
                e.Bounds.Top + (e.Bounds.Height - dotSize) / 2,
                dotSize,
                dotSize);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (SolidBrush dotBrush = new SolidBrush(category.DisplayColor))
                e.Graphics.FillEllipse(dotBrush, dot);

            Color textColor = category.Enabled ? Theme.Text : Theme.Muted;
            Font font = selected ? Theme.FontBold : Theme.FontUi;
            Rectangle nameRect = new Rectangle(e.Bounds.Left + 28, e.Bounds.Top, e.Bounds.Width - 82, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                category.Name,
                font,
                nameRect,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            int rules = category.Rules == null ? 0 : category.Rules.Count;
            Rectangle countRect = new Rectangle(e.Bounds.Right - 56, e.Bounds.Top, 46, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                rules.ToString(),
                Theme.FontUiSmall,
                countRect,
                Theme.Muted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            if (selected)
                ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds, Theme.Text, background);
        }

        private void CategoryNameChanged()
        {
            if (_suppressCategoryEvents) return;
            CategoryDef category = SelectedCategory;
            if (category == null) return;

            category.Name = (_txtCatName.Text ?? "").Trim();
            if (_catList != null) _catList.Invalidate();
            MarkCategoriesDirty();
        }

        private void CategoryEnabledChanged()
        {
            if (_suppressCategoryEvents) return;
            CategoryDef category = SelectedCategory;
            if (category == null) return;

            category.Enabled = _chkCatEnabled.Checked;
            if (_catList != null) _catList.Invalidate();
            MarkCategoriesDirty();
        }

        private void ChooseCategoryColor()
        {
            CategoryDef category = SelectedCategory;
            if (category == null) return;

            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.FullOpen = true;
                dialog.Color = category.DisplayColor;
                dialog.AnyColor = true;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                category.Color = Theme.ToHex(dialog.Color);
                _colorSwatch.BackColor = dialog.Color;
                if (_catList != null) _catList.Invalidate();
                MarkCategoriesDirty();
            }
        }

        private void AddCategory()
        {
            string name = InputDialog.Show(this, "Новая категория", "Название новой категории:", "Новая категория");
            if (name == null) return;
            name = name.Trim();
            if (name.Length == 0) return;

            if (FindCategoryIndex(name) >= 0)
            {
                MessageBox.Show(this, "Категория с таким названием уже существует.", "Категории", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            CategoryDef category = new CategoryDef(name, "#94A3B8");
            _categories.Add(category);
            RefreshCategoryList(_categories.Count - 1);
            MarkCategoriesDirty();
            SetStatus("Категория добавлена. Не забудьте сохранить изменения.");
        }

        private void DeleteCategory()
        {
            CategoryDef category = SelectedCategory;
            if (category == null) return;

            if (string.Equals(category.Name, "Другое", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this,
                    "Категорию «Другое» удалить нельзя: она используется как заготовка по умолчанию.",
                    "Категории",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (_categories.Count <= 1)
            {
                MessageBox.Show(this, "Нельзя удалить последнюю категорию.", "Категории", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(this,
                    "Удалить категорию «" + category.Name + "» и все её правила?",
                    "Категории",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int index = _catList.SelectedIndex;
            _categories.RemoveAt(index);
            if (_categories.Count == 0) index = -1;
            else if (index >= _categories.Count) index = _categories.Count - 1;
            RefreshCategoryList(index);
            MarkCategoriesDirty();
            SetStatus("Категория удалена. Не забудьте сохранить изменения.");
        }

        private void MoveCategory(int delta)
        {
            if (_catList == null) return;
            int index = _catList.SelectedIndex;
            int target = index + delta;
            if (index < 0 || index >= _categories.Count) return;
            if (target < 0 || target >= _categories.Count) return;

            CategoryDef item = _categories[index];
            _categories[index] = _categories[target];
            _categories[target] = item;
            RefreshCategoryList(target);
            MarkCategoriesDirty();
        }

        private int FindCategoryIndex(string name)
        {
            for (int i = 0; i < _categories.Count; i++)
            {
                if (string.Equals(_categories[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private void RefreshRulesGrid()
        {
            if (_gridRules == null) return;

            int selected = _gridRules.CurrentRow == null ? -1 : _gridRules.CurrentRow.Index;
            _gridRules.Rows.Clear();

            CategoryDef category = SelectedCategory;
            if (category != null && category.Rules != null)
            {
                foreach (CategoryRule rule in category.Rules)
                {
                    _gridRules.Rows.Add(
                        rule.TypeDisplay,
                        rule.Pattern,
                        rule.Note ?? "");
                }
            }

            if (_gridRules.Rows.Count > 0)
            {
                if (selected < 0 || selected >= _gridRules.Rows.Count)
                    selected = 0;
                _gridRules.CurrentCell = _gridRules.Rows[selected].Cells[0];
            }

            UpdateRuleButtons();
        }

        private int SelectedRuleIndex()
        {
            CategoryDef category = SelectedCategory;
            if (category == null || _gridRules == null) return -1;
            if (_gridRules.CurrentRow == null) return -1;
            int index = _gridRules.CurrentRow.Index;
            if (index < 0 || index >= category.Rules.Count) return -1;
            return index;
        }

        private void AddRule()
        {
            CategoryDef category = SelectedCategory;
            if (category == null) return;

            CategoryRule rule = RuleEditDialog.Show(this, null);
            if (rule == null) return;

            category.Rules.Add(rule);
            RefreshRulesGrid();
            if (_gridRules.Rows.Count > 0)
                _gridRules.CurrentCell = _gridRules.Rows[_gridRules.Rows.Count - 1].Cells[0];
            MarkCategoriesDirty();
        }

        private void EditRule()
        {
            CategoryDef category = SelectedCategory;
            int index = SelectedRuleIndex();
            if (category == null || index < 0) return;

            CategoryRule rule = RuleEditDialog.Show(this, category.Rules[index]);
            if (rule == null) return;

            category.Rules[index] = rule;
            RefreshRulesGrid();
            MarkCategoriesDirty();
        }

        private void DeleteRule()
        {
            CategoryDef category = SelectedCategory;
            int index = SelectedRuleIndex();
            if (category == null || index < 0) return;

            CategoryRule rule = category.Rules[index];
            if (MessageBox.Show(this,
                    "Удалить правило «" + rule.Pattern + "»?",
                    "Правило",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            category.Rules.RemoveAt(index);
            RefreshRulesGrid();
            MarkCategoriesDirty();
        }

        private void UpdateRuleButtons()
        {
            if (_btnRuleAdd == null) return;
            bool hasCategory = SelectedCategory != null;
            _btnRuleAdd.Enabled = hasCategory;
            bool hasRule = hasCategory && SelectedRuleIndex() >= 0;
            _btnRuleEdit.Enabled = hasRule;
            _btnRuleDelete.Enabled = hasRule;
        }

        private void UpdateCategoryButtons()
        {
            if (_btnCatAdd == null) return;

            CategoryDef category = SelectedCategory;
            int index = _catList == null ? -1 : _catList.SelectedIndex;
            bool has = category != null;

            _btnCatAdd.Enabled = true;
            _btnCatDelete.Enabled = has &&
                !string.Equals(category.Name, "Другое", StringComparison.OrdinalIgnoreCase) &&
                _categories.Count > 1;
            _btnCatUp.Enabled = has && index > 0;
            _btnCatDown.Enabled = has && index >= 0 && index < _categories.Count - 1;
            _btnCatSave.Enabled = _categoriesDirty;
            _btnCatReset.Enabled = true;
            _btnCatTest.Enabled = true;
            _btnRuleAdd.Enabled = has;
            UpdateRuleButtons();
        }

        private void MarkCategoriesDirty()
        {
            _categoriesDirty = true;
            UpdateCategoryDirtyUi();
        }

        private void UpdateCategoryDirtyUi()
        {
            if (_lblCatDirty != null)
            {
                if (_categoriesDirty)
                {
                    _lblCatDirty.Text = "Есть несохранённые изменения";
                    _lblCatDirty.ForeColor = Theme.Amber;
                }
                else
                {
                    _lblCatDirty.Text = "Сохранено";
                    _lblCatDirty.ForeColor = Theme.Green;
                }
            }
            UpdateCategoryButtons();
        }

        private void TestClassification()
        {
            using (ClassificationTestDialog dialog = new ClassificationTestDialog(_classifier))
                dialog.ShowDialog(this);

            if (_categoriesDirty)
                SetStatus("Тест использует сохранённые правила. Сохраните изменения, чтобы проверить новые.");
        }

        private bool SaveCategories(bool refreshUi)
        {
            bool addedFallback = false;
            if (FindCategoryIndex("Другое") < 0)
            {
                _categories.Add(new CategoryDef("Другое", "#94A3B8"));
                addedFallback = true;
            }

            string error;
            CategoryDef badCategory;
            if (!ValidateCategories(out error, out badCategory))
            {
                if (badCategory != null)
                {
                    int index = _categories.IndexOf(badCategory);
                    if (index >= 0 && _catList != null) _catList.SelectedIndex = index;
                }

                MessageBox.Show(this, error, "Проверка категорий", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                CategoryStore.Save(_categories);
            }
            catch (Exception ex)
            {
                Log.Error("Не удалось сохранить категории.", ex);
                MessageBox.Show(this,
                    "Не удалось сохранить категории:" + Environment.NewLine + ex.Message,
                    "Ошибка сохранения",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            _classifier.Reload();
            _categoriesDirty = false;
            if (addedFallback && _catList != null) RefreshCategoryList(_catList.SelectedIndex);
            UpdateCategoryDirtyUi();
            RefreshCategoryCombo();
            ResetLoadedFlags();
            if (refreshUi && _currentTab != 4)
                LoadCurrentTabData();

            SetStatus("Категории сохранены: " + Theme.FormatCount(_categories.Count) + ".");
            return true;
        }

        private bool ValidateCategories(out string error, out CategoryDef badCategory)
        {
            error = "";
            badCategory = null;
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (CategoryDef category in _categories)
            {
                string name = (category.Name ?? "").Trim();
                if (name.Length == 0)
                {
                    error = "Название категории не может быть пустым.";
                    badCategory = category;
                    return false;
                }
                if (!names.Add(name))
                {
                    error = "Название категории повторяется: «" + name + "».";
                    badCategory = category;
                    return false;
                }
                category.Name = name;

                if (category.Rules == null)
                    category.Rules = new List<CategoryRule>();

                foreach (CategoryRule rule in category.Rules)
                {
                    if (rule == null) continue;
                    rule.Pattern = (rule.Pattern ?? "").Trim();
                    string ruleError;
                    if (!Classifier.ValidateRule(rule, out ruleError))
                    {
                        error = "Категория «" + name + "», правило «" + rule.Pattern + "»: " + ruleError;
                        badCategory = category;
                        return false;
                    }
                }
            }

            return true;
        }

        private void ResetCategories()
        {
            if (MessageBox.Show(this,
                    "Восстановить все заготовки категорий? Текущие правила будут заменены, пока вы не сохраните изменения.",
                    "Сброс категорий",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            List<CategoryDef> defaults = CategoryPresets.CreateDefaults();
            _categories.Clear();
            _categories.AddRange(defaults);
            RefreshCategoryList(0);
            MarkCategoriesDirty();
            SetStatus("Заготовки восстановлены. Нажмите «Сохранить», чтобы применить их.");
        }

        private void RefreshCategoryCombo()
        {
            if (_cmbCategory == null) return;

            string previous = _categoryFilter;
            _suppressFilters = true;
            try
            {
                _cmbCategory.BeginUpdate();
                _cmbCategory.Items.Clear();
                _cmbCategory.Items.Add("Все категории");
                foreach (CategoryDef category in _categories)
                    _cmbCategory.Items.Add(category.Name);

                int index = 0;
                if (!string.IsNullOrEmpty(previous))
                {
                    for (int i = 1; i < _cmbCategory.Items.Count; i++)
                    {
                        if (string.Equals(_cmbCategory.Items[i].ToString(), previous, StringComparison.OrdinalIgnoreCase))
                        {
                            index = i;
                            break;
                        }
                    }
                }
                _cmbCategory.SelectedIndex = index;
            }
            finally
            {
                _cmbCategory.EndUpdate();
                _suppressFilters = false;
            }

            if (_cmbCategory.SelectedIndex <= 0)
                _categoryFilter = null;
            else
                _categoryFilter = _cmbCategory.SelectedItem as string;
        }
    }
}
