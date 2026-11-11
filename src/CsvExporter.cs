using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace NetLimiterParser
{
    internal static class CsvExporter
    {
        public static void ExportDomainRows(List<DomainRow> rows, string path)
        {
            if (rows == null || rows.Count == 0)
                throw new InvalidOperationException("Нет данных для экспорта.");

            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writer.WriteLine("IP;Домен;Категория;Протоколы;Соединений;Вход;Исход;Всего;Последняя активность");

                foreach (DomainRow row in rows)
                {
                    StringBuilder line = new StringBuilder();
                    AppendField(line, row.Ip);
                    line.Append(';');
                    AppendField(line, row.Domain);
                    line.Append(';');
                    AppendField(line, row.Service);
                    line.Append(';');
                    AppendField(line, row.Protocols);
                    line.Append(';');
                    line.Append(row.Connections.ToString(CultureInfo.InvariantCulture));
                    line.Append(';');
                    AppendField(line, Theme.FormatBytes(row.InBytes));
                    line.Append(';');
                    AppendField(line, Theme.FormatBytes(row.OutBytes));
                    line.Append(';');
                    AppendField(line, Theme.FormatBytes(row.TotalBytes));
                    line.Append(';');
                    AppendField(line, row.LastUtc == DateTime.MinValue
                        ? ""
                        : row.LastUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture));
                    writer.WriteLine(line.ToString());
                }
            }
        }

        public static int ExportDomainValues(List<DomainRow> rows, bool domains, string path)
        {
            if (rows == null || rows.Count == 0)
                throw new InvalidOperationException("Нет данных для экспорта.");

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> values = new List<string>();
            foreach (DomainRow row in rows)
            {
                string value = domains ? (row.Domain ?? "").Trim() : (row.Ip ?? "").Trim();
                if (value.Length == 0) continue;
                if (seen.Add(value)) values.Add(value);
            }

            if (values.Count == 0)
                throw new InvalidOperationException(domains
                    ? "Для этой программы за выбранный период не найдено доменных имён."
                    : "Нет IP-адресов для экспорта.");

            values.Sort(StringComparer.OrdinalIgnoreCase);

            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writer.WriteLine(domains ? "Домен" : "IP-адрес");
                foreach (string value in values)
                {
                    StringBuilder line = new StringBuilder();
                    AppendField(line, value);
                    writer.WriteLine(line.ToString());
                }
            }

            return values.Count;
        }
        public static void ExportVirtual<T>(VirtualGrid<T> grid, string path) where T : class
        {
            if (grid == null) throw new ArgumentNullException("grid");
            if (grid.Items.Count == 0)
                throw new InvalidOperationException("Нет данных для экспорта.");

            List<GridColumn<T>> columns = grid.Columns;
            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                StringBuilder line = new StringBuilder();
                for (int i = 0; i < columns.Count; i++)
                {
                    if (i > 0) line.Append(';');
                    AppendField(line, columns[i].Header);
                }
                writer.WriteLine(line.ToString());

                foreach (T item in grid.Items)
                {
                    line.Length = 0;
                    for (int i = 0; i < columns.Count; i++)
                    {
                        if (i > 0) line.Append(';');
                        string value = columns[i].Value != null ? columns[i].Value(item) : "";
                        AppendField(line, value);
                    }
                    writer.WriteLine(line.ToString());
                }
            }
        }

        public static void ExportGrid(DataGridView grid, string path)
        {
            if (grid == null) throw new ArgumentNullException("grid");
            if (grid.Rows.Count == 0)
                throw new InvalidOperationException("Нет данных для экспорта.");

            List<DataGridViewColumn> columns = new List<DataGridViewColumn>();
            for (int i = 0; i < grid.Columns.Count; i++)
            {
                if (grid.Columns[i].Visible)
                    columns.Add(grid.Columns[i]);
            }

            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                StringBuilder line = new StringBuilder();
                for (int i = 0; i < columns.Count; i++)
                {
                    if (i > 0) line.Append(';');
                    AppendField(line, columns[i].HeaderText);
                }
                writer.WriteLine(line.ToString());

                for (int rowIndex = 0; rowIndex < grid.Rows.Count; rowIndex++)
                {
                    line.Length = 0;
                    for (int i = 0; i < columns.Count; i++)
                    {
                        if (i > 0) line.Append(';');
                        object value = grid.Rows[rowIndex].Cells[columns[i].Index].Value;
                        AppendField(line, value == null ? "" : value.ToString());
                    }
                    writer.WriteLine(line.ToString());
                }
            }
        }

        private static void AppendField(StringBuilder line, string value)
        {
            if (value == null) value = "";

            bool quote = false;
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (ch == ';' || ch == '"' || ch == '\r' || ch == '\n')
                {
                    quote = true;
                    break;
                }
            }

            if (!quote)
            {
                line.Append(value);
                return;
            }

            line.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (ch == '"') line.Append('"');
                line.Append(ch);
            }
            line.Append('"');
        }
    }
}
