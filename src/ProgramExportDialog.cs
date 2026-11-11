using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal sealed class ProgramExportDialog : Form
    {
        private readonly ComboBox _what;
        private readonly ComboBox _format;
        private readonly ComboBox _mask;
        private readonly CheckBox _chkSubdomains;
        private readonly CheckBox _chkUnique;
        private readonly CheckBox _chkMerge;
        private ProgramExportOptions _options;

        public ProgramExportOptions Options
        {
            get { return _options; }
        }

        public ProgramExportDialog()
        {
            Text = "Свой формат вывода";
            Font = Theme.FontUi;
            BackColor = Theme.Bg;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(640, 600);

            GradientPanel header = new GradientPanel();
            header.Dock = DockStyle.Top;
            header.Height = 64;
            Controls.Add(header);

            Label title = Ui.Label("Свой формат вывода", Theme.FontSection, Color.White);
            title.SetBounds(18, 10, 590, 26);
            header.Controls.Add(title);

            Label subtitle = Ui.Label("TXT или CSV, маска IPv4, слияние подсетей, поддомены и дубликаты",
                Theme.FontUiSmall, Color.FromArgb(203, 213, 225));
            subtitle.SetBounds(18, 36, 590, 20);
            header.Controls.Add(subtitle);

            Label whatLabel = Ui.Label("Что выгрузить", Theme.FontBold, Theme.Text);
            whatLabel.SetBounds(20, 82, 190, 24);
            Controls.Add(whatLabel);

            _what = Ui.Combo(400);
            _what.SetBounds(220, 78, 400, 28);
            _what.Items.Add("IP и домены — в один столбец");
            _what.Items.Add("Только IP-адреса");
            _what.Items.Add("Только домены");
            _what.SelectedIndex = 0;
            _what.SelectedIndexChanged += delegate { UpdateEnabledState(); };
            Controls.Add(_what);

            Label formatLabel = Ui.Label("Формат файла", Theme.FontBold, Theme.Text);
            formatLabel.SetBounds(20, 122, 190, 24);
            Controls.Add(formatLabel);

            _format = Ui.Combo(400);
            _format.SetBounds(220, 118, 400, 28);
            _format.Items.Add("TXT — по одному значению в строке");
            _format.Items.Add("CSV — один столбец");
            _format.SelectedIndex = 0;
            Controls.Add(_format);

            CardPanel ipCard = new CardPanel();
            ipCard.SetBounds(20, 166, 600, 158);
            Controls.Add(ipCard);

            Label maskLabel = Ui.Label("Маска IPv4", Theme.FontBold, Theme.Text);
            maskLabel.SetBounds(18, 16, 120, 24);
            ipCard.Controls.Add(maskLabel);

            _mask = Ui.Combo(330);
            _mask.SetBounds(150, 12, 330, 28);
            _mask.MaxDropDownItems = 12;
            _mask.Items.Add(MaskDisplay(-1));
            for (int prefix = 32; prefix >= 0; prefix--)
                _mask.Items.Add(MaskDisplay(prefix));
            _mask.SelectedIndex = 0;
            _mask.SelectedIndexChanged += delegate { UpdateEnabledState(); };
            ipCard.Controls.Add(_mask);

            _chkMerge = new CheckBox();
            _chkMerge.Text = "Сливать соседние подсети в более крупные";
            _chkMerge.Font = Theme.FontUi;
            _chkMerge.ForeColor = Theme.Text;
            _chkMerge.BackColor = Color.Transparent;
            _chkMerge.AutoSize = true;
            _chkMerge.Checked = true;
            _chkMerge.SetBounds(18, 48, 460, 24);
            ipCard.Controls.Add(_chkMerge);

            Label ipHint = Ui.Label(
                "Маска задаёт начальную гранулярность: адреса одной подсети дают один CIDR. " +
                "Слияние объединяет соседние блоки: 10.0.0.0/24 + 10.0.1.0/24 → 10.0.0.0/23; " +
                "вложенные подсети удаляются. IPv6 выводится точными адресами.",
                Theme.FontUiSmall, Theme.Muted);
            ipHint.SetBounds(18, 78, 562, 68);
            ipHint.TextAlign = ContentAlignment.TopLeft;
            ipCard.Controls.Add(ipHint);

            CardPanel domainCard = new CardPanel();
            domainCard.SetBounds(20, 334, 600, 104);
            Controls.Add(domainCard);

            _chkSubdomains = new CheckBox();
            _chkSubdomains.Text = "Оставлять поддомены (полные имена)";
            _chkSubdomains.Font = Theme.FontUi;
            _chkSubdomains.ForeColor = Theme.Text;
            _chkSubdomains.BackColor = Color.Transparent;
            _chkSubdomains.AutoSize = true;
            _chkSubdomains.Checked = true;
            _chkSubdomains.SetBounds(18, 14, 360, 24);
            domainCard.Controls.Add(_chkSubdomains);

            Label domainHint = Ui.Label(
                "Если снять — останется основной домен: cdn.discordapp.com → discordapp.com. " +
                "Домены без известного имени не выводятся.",
                Theme.FontUiSmall, Theme.Muted);
            domainHint.SetBounds(18, 44, 562, 48);
            domainHint.TextAlign = ContentAlignment.TopLeft;
            domainCard.Controls.Add(domainHint);

            _chkUnique = new CheckBox();
            _chkUnique.Text = "Удалять дубликаты";
            _chkUnique.Font = Theme.FontUi;
            _chkUnique.ForeColor = Theme.Text;
            _chkUnique.AutoSize = true;
            _chkUnique.Checked = true;
            _chkUnique.SetBounds(24, 452, 220, 24);
            Controls.Add(_chkUnique);

            Label outputHint = Ui.Label(
                "TXT — значения без заголовка, по одному в строке; удобно для sing-box, " +
                "HydraRoute Neo и MagiTrickle. CSV — один столбец с заголовком «Значение».",
                Theme.FontUiSmall, Theme.Muted);
            outputHint.SetBounds(24, 482, 590, 48);
            outputHint.TextAlign = ContentAlignment.TopLeft;
            Controls.Add(outputHint);

            RoundButton cancel = new RoundButton();
            cancel.Text = "Отмена";
            cancel.Quiet = true;
            cancel.SetBounds(420, 546, 100, 34);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            RoundButton ok = new RoundButton();
            ok.Text = "Экспорт…";
            ok.Primary = true;
            ok.SetBounds(530, 546, 90, 34);
            ok.Click += delegate { SaveOptions(); };
            Controls.Add(ok);

            AcceptButton = ok;
            CancelButton = cancel;
            UpdateEnabledState();
        }

        private void UpdateEnabledState()
        {
            bool includeIps = _what.SelectedIndex != 2;
            _mask.Enabled = includeIps;
            _chkMerge.Enabled = includeIps && _mask.SelectedIndex > 0;
            _chkSubdomains.Enabled = _what.SelectedIndex != 1;
        }

        private void SaveOptions()
        {
            ProgramExportOptions options = new ProgramExportOptions();
            options.IncludeIps = _what.SelectedIndex != 2;
            options.IncludeDomains = _what.SelectedIndex != 1;
            options.TxtFormat = _format.SelectedIndex == 0;
            options.IncludeSubdomains = _chkSubdomains.Checked;
            options.RemoveDuplicates = _chkUnique.Checked;
            options.Ipv4Prefix = _what.SelectedIndex == 2 || _mask.SelectedIndex <= 0
                ? -1
                : 33 - _mask.SelectedIndex;
            options.MergeSubnets = options.Ipv4Prefix >= 0 && _chkMerge.Checked;

            _options = options;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static string MaskDisplay(int prefix)
        {
            if (prefix < 0) return "Не объединять — точный IP";

            uint mask = prefix == 0 ? 0u : 0xFFFFFFFFu << (32 - prefix);
            byte[] bytes = new byte[4];
            bytes[0] = (byte)(mask >> 24);
            bytes[1] = (byte)(mask >> 16);
            bytes[2] = (byte)(mask >> 8);
            bytes[3] = (byte)mask;

            return "/" + prefix + " — " + new IPAddress(bytes).ToString();
        }
    }
}
