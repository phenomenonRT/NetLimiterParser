using System;
using System.Drawing;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed class InputDialog : Form
    {
        private readonly TextBox _text;

        private InputDialog(string title, string prompt, string initial)
        {
            Text = title;
            Font = Theme.FontUi;
            BackColor = Theme.Bg;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(520, 220);

            GradientPanel header = new GradientPanel();
            header.Dock = DockStyle.Top;
            header.Height = 54;
            Controls.Add(header);

            Label titleLabel = Ui.Label(title, Theme.FontSection, Color.White);
            titleLabel.SetBounds(18, 14, 480, 26);
            header.Controls.Add(titleLabel);

            Label promptLabel = Ui.Label(prompt, Theme.FontUi, Theme.MutedDark);
            promptLabel.SetBounds(20, 74, 480, 24);
            Controls.Add(promptLabel);

            _text = new TextBox();
            _text.Font = Theme.FontUi;
            _text.BorderStyle = BorderStyle.FixedSingle;
            _text.SetBounds(20, 102, 480, 24);
            _text.Text = initial ?? "";
            Controls.Add(_text);

            RoundButton cancel = new RoundButton();
            cancel.Text = "Отмена";
            cancel.Quiet = true;
            cancel.SetBounds(302, 158, 92, 32);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            RoundButton ok = new RoundButton();
            ok.Text = "OK";
            ok.Primary = true;
            ok.SetBounds(402, 158, 98, 32);
            ok.Click += delegate
            {
                if (string.IsNullOrWhiteSpace(_text.Text))
                {
                    MessageBox.Show(this, "Введите значение.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _text.Focus();
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        public static string Show(IWin32Window owner, string title, string prompt, string initial)
        {
            using (InputDialog dialog = new InputDialog(title, prompt, initial))
            {
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                    return dialog._text.Text.Trim();
                return null;
            }
        }
    }

    internal sealed class RuleEditDialog : Form
    {
        private readonly ComboBox _type;
        private readonly TextBox _pattern;
        private readonly TextBox _note;
        private readonly Label _example;
        private CategoryRule _result;

        public CategoryRule Result
        {
            get { return _result; }
        }

        private RuleEditDialog(CategoryRule existing)
        {
            Text = existing == null ? "Новое правило" : "Изменение правила";
            Font = Theme.FontUi;
            BackColor = Theme.Bg;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(600, 390);

            GradientPanel header = new GradientPanel();
            header.Dock = DockStyle.Top;
            header.Height = 56;
            Controls.Add(header);

            Label title = Ui.Label(Text, Theme.FontSection, Color.White);
            title.SetBounds(18, 15, 550, 26);
            header.Controls.Add(title);

            Label typeLabel = Ui.Label("Тип правила", Theme.FontBold, Theme.Text);
            typeLabel.SetBounds(20, 78, 120, 24);
            Controls.Add(typeLabel);

            _type = Ui.Combo(230);
            _type.SetBounds(150, 74, 230, 28);
            _type.Items.Add("Процесс (имя или путь)");
            _type.Items.Add("Домен (и поддомены)");
            _type.Items.Add("IP-адрес или подсеть");
            _type.Items.Add("Порт / диапазон портов");
            _type.SelectedIndex = 0;
            _type.SelectedIndexChanged += delegate { UpdateExample(); };
            Controls.Add(_type);

            Label patternLabel = Ui.Label("Шаблон", Theme.FontBold, Theme.Text);
            patternLabel.SetBounds(20, 120, 120, 24);
            Controls.Add(patternLabel);

            _pattern = new TextBox();
            _pattern.Font = Theme.FontUi;
            _pattern.BorderStyle = BorderStyle.FixedSingle;
            _pattern.SetBounds(150, 116, 430, 24);
            Controls.Add(_pattern);

            Label noteLabel = Ui.Label("Примечание", Theme.FontBold, Theme.Text);
            noteLabel.SetBounds(20, 162, 120, 24);
            Controls.Add(noteLabel);

            _note = new TextBox();
            _note.Font = Theme.FontUi;
            _note.BorderStyle = BorderStyle.FixedSingle;
            _note.SetBounds(150, 158, 430, 24);
            Controls.Add(_note);

            _example = Ui.Label("", Theme.FontUi, Theme.Muted);
            _example.SetBounds(20, 200, 560, 48);
            Controls.Add(_example);

            Label hint = Ui.Label(
                "Для процессов без «\\» шаблон сравнивается с именем файла. Домен срабатывает также для всех поддоменов. " +
                "IP можно указать с маской: 192.168.0.0/16. Порт: 443, 27015-27050, tcp:3389, udp:53.",
                Theme.FontUiSmall,
                Theme.MutedDark);
            hint.SetBounds(20, 252, 560, 46);
            Controls.Add(hint);

            RoundButton cancel = new RoundButton();
            cancel.Text = "Отмена";
            cancel.Quiet = true;
            cancel.SetBounds(374, 326, 100, 34);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            RoundButton ok = new RoundButton();
            ok.Text = "Сохранить";
            ok.Primary = true;
            ok.SetBounds(484, 326, 96, 34);
            ok.Click += delegate { SaveRule(); };
            Controls.Add(ok);

            AcceptButton = ok;
            CancelButton = cancel;

            if (existing != null)
            {
                _type.SelectedIndex = TypeIndex(existing.Type);
                _pattern.Text = existing.Pattern ?? "";
                _note.Text = existing.Note ?? "";
            }

            UpdateExample();
            Shown += delegate { _pattern.Focus(); };
        }

        public static CategoryRule Show(IWin32Window owner, CategoryRule existing)
        {
            using (RuleEditDialog dialog = new RuleEditDialog(existing))
            {
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                    return dialog._result;
                return null;
            }
        }

        private static int TypeIndex(string type)
        {
            switch ((type ?? "").Trim().ToLowerInvariant())
            {
                case "domain": return 1;
                case "ip": return 2;
                case "port": return 3;
                default: return 0;
            }
        }

        private static string TypeName(int index)
        {
            switch (index)
            {
                case 1: return "domain";
                case 2: return "ip";
                case 3: return "port";
                default: return "process";
            }
        }

        private void UpdateExample()
        {
            switch (_type.SelectedIndex)
            {
                case 1:
                    _example.Text = "Пример: discord.com — правило применится к discord.com и всем его поддоменам.";
                    break;
                case 2:
                    _example.Text = "Пример: 192.168.0.0/16, 10.0.0.5 или 2a00:1450::/32.";
                    break;
                case 3:
                    _example.Text = "Пример: 443, 27015-27050, tcp:3389, udp:27015-27050.";
                    break;
                default:
                    _example.Text = "Пример: chrome.exe, discord*.exe или полный путь g:\\games\\game.exe.";
                    break;
            }
        }

        private void SaveRule()
        {
            CategoryRule rule = new CategoryRule();
            rule.Type = TypeName(_type.SelectedIndex);
            rule.Pattern = (_pattern.Text ?? "").Trim();
            rule.Note = (_note.Text ?? "").Trim();

            string error;
            if (!Classifier.ValidateRule(rule, out error))
            {
                MessageBox.Show(this, error, "Проверка правила", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _pattern.Focus();
                return;
            }

            _result = rule;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class ClassificationTestDialog : Form
    {
        private readonly Classifier _classifier;
        private readonly TextBox _app;
        private readonly TextBox _domain;
        private readonly TextBox _ip;
        private readonly TextBox _port;
        private readonly ComboBox _protocol;
        private readonly Panel _color;
        private readonly Label _result;
        private readonly Label _rule;
        private readonly Label _score;

        public ClassificationTestDialog(Classifier classifier)
        {
            _classifier = classifier;
            Text = "Проверка классификации";
            Font = Theme.FontUi;
            BackColor = Theme.Bg;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(620, 420);

            GradientPanel header = new GradientPanel();
            header.Dock = DockStyle.Top;
            header.Height = 56;
            Controls.Add(header);

            Label title = Ui.Label("Проверка классификации", Theme.FontSection, Color.White);
            title.SetBounds(18, 15, 560, 26);
            header.Controls.Add(title);

            Label appLabel = Ui.Label("Программа / путь", Theme.FontBold, Theme.Text);
            appLabel.SetBounds(20, 76, 130, 24);
            Controls.Add(appLabel);
            _app = MakeTextBox();
            _app.SetBounds(158, 72, 440, 24);
            _app.Text = "chrome.exe";
            Controls.Add(_app);

            Label domainLabel = Ui.Label("Домен", Theme.FontBold, Theme.Text);
            domainLabel.SetBounds(20, 112, 130, 24);
            Controls.Add(domainLabel);
            _domain = MakeTextBox();
            _domain.SetBounds(158, 108, 440, 24);
            _domain.Text = "discord.com";
            Controls.Add(_domain);

            Label ipLabel = Ui.Label("IP-адрес", Theme.FontBold, Theme.Text);
            ipLabel.SetBounds(20, 148, 130, 24);
            Controls.Add(ipLabel);
            _ip = MakeTextBox();
            _ip.SetBounds(158, 144, 230, 24);
            _ip.Text = "1.1.1.1";
            Controls.Add(_ip);

            Label portLabel = Ui.Label("Порт", Theme.FontBold, Theme.Text);
            portLabel.SetBounds(402, 148, 50, 24);
            Controls.Add(portLabel);
            _port = MakeTextBox();
            _port.SetBounds(458, 144, 60, 24);
            _port.Text = "443";
            Controls.Add(_port);

            Label protocolLabel = Ui.Label("Протокол", Theme.FontBold, Theme.Text);
            protocolLabel.SetBounds(20, 184, 130, 24);
            Controls.Add(protocolLabel);
            _protocol = Ui.Combo(120);
            _protocol.SetBounds(158, 180, 120, 28);
            _protocol.Items.Add("TCP");
            _protocol.Items.Add("UDP");
            _protocol.Items.Add("ICMP");
            _protocol.Items.Add("ICMPv6");
            _protocol.SelectedIndex = 0;
            Controls.Add(_protocol);

            RoundButton check = new RoundButton();
            check.Text = "Проверить";
            check.Primary = true;
            check.SetBounds(300, 178, 120, 32);
            check.Click += delegate { RunCheck(); };
            Controls.Add(check);

            CardPanel card = new CardPanel();
            card.SetBounds(20, 228, 580, 118);
            Controls.Add(card);

            _color = new Panel();
            _color.SetBounds(18, 18, 16, 16);
            _color.BackColor = Theme.Muted;
            card.Controls.Add(_color);

            _result = Ui.Label("", Theme.FontSection, Theme.Text);
            _result.SetBounds(44, 10, 520, 32);
            card.Controls.Add(_result);

            _rule = Ui.Label("", Theme.FontUi, Theme.MutedDark);
            _rule.SetBounds(18, 46, 544, 26);
            card.Controls.Add(_rule);

            _score = Ui.Label("", Theme.FontUiSmall, Theme.Muted);
            _score.SetBounds(18, 76, 544, 24);
            card.Controls.Add(_score);

            RoundButton close = new RoundButton();
            close.Text = "Закрыть";
            close.Quiet = true;
            close.SetBounds(500, 362, 100, 34);
            close.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(close);

            CancelButton = close;
            RunCheck();
        }

        private static TextBox MakeTextBox()
        {
            TextBox box = new TextBox();
            box.Font = Theme.FontUi;
            box.BorderStyle = BorderStyle.FixedSingle;
            return box;
        }

        private void RunCheck()
        {
            int port;
            if (!int.TryParse(_port.Text.Trim(), out port)) port = 0;
            if (port < 0) port = 0;
            if (port > 65535) port = 65535;

            string protocol = _protocol.SelectedItem == null ? "TCP" : _protocol.SelectedItem.ToString();
            CategoryMatch match = _classifier.Match(
                _app.Text.Trim(),
                _domain.Text.Trim(),
                _ip.Text.Trim(),
                port,
                0,
                protocol,
                false);

            CategoryDef category = match != null && match.Category != null ? match.Category : _classifier.Fallback;
            _color.BackColor = category.DisplayColor;
            _result.Text = "Категория: " + category.Name;
            if (match != null && match.Rule != null)
            {
                _rule.Text = "Правило: " + match.Rule.TypeDisplay + " — " + match.Rule.Pattern +
                    (string.IsNullOrWhiteSpace(match.Rule.Note) ? "" : " (" + match.Rule.Note + ")");
                _score.Text = "Приоритет совпадения: " + match.Score;
            }
            else
            {
                _rule.Text = "Ни одно правило не совпало — используется заготовка «Другое».";
                _score.Text = "Проверьте шаблоны правил в списке категорий.";
            }
        }
    }

    internal sealed class DetailsDialog : Form
    {
        public DetailsDialog(string title, string text)
        {
            Text = title;
            Font = Theme.FontUi;
            BackColor = Theme.Bg;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(680, 420);
            MinimumSize = new Size(480, 320);

            GradientPanel header = new GradientPanel();
            header.Dock = DockStyle.Top;
            header.Height = 54;
            Controls.Add(header);

            Label titleLabel = Ui.Label(title, Theme.FontSection, Color.White);
            titleLabel.SetBounds(18, 14, 620, 26);
            titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            header.Controls.Add(titleLabel);

            TextBox box = new TextBox();
            box.Multiline = true;
            box.ReadOnly = true;
            box.ScrollBars = ScrollBars.Both;
            box.WordWrap = false;
            box.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            box.BackColor = Theme.Surface;
            box.ForeColor = Theme.Text;
            box.BorderStyle = BorderStyle.None;
            box.Dock = DockStyle.Fill;
            box.Text = text;
            Panel padding = new Panel();
            padding.Dock = DockStyle.Fill;
            padding.Padding = new Padding(16, 12, 16, 52);
            padding.BackColor = Theme.Bg;
            padding.Controls.Add(box);
            Controls.Add(padding);
            padding.BringToFront();

            RoundButton copy = new RoundButton();
            copy.Text = "Копировать";
            copy.Quiet = true;
            copy.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            copy.SetBounds(468, 372, 100, 32);
            Controls.Add(copy);
            copy.BringToFront();
            copy.Click += delegate
            {
                try
                {
                    Clipboard.SetText(text ?? "");
                }
                catch (Exception ex)
                {
                    Log.Error("Не удалось скопировать текст в буфер обмена.", ex);
                }
            };

            RoundButton close = new RoundButton();
            close.Text = "Закрыть";
            close.Primary = true;
            close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            close.SetBounds(576, 372, 88, 32);
            Controls.Add(close);
            close.BringToFront();
            close.Click += delegate { Close(); };
            CancelButton = close;
        }
    }
}
