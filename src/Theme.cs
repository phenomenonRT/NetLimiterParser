using System;
using System.Drawing;
using System.Globalization;

namespace NetLimiterParser
{
    internal static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(241, 245, 249);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceAlt = Color.FromArgb(248, 250, 252);
        public static readonly Color Border = Color.FromArgb(226, 232, 240);
        public static readonly Color BorderStrong = Color.FromArgb(203, 213, 225);
        public static readonly Color Text = Color.FromArgb(15, 23, 42);
        public static readonly Color Muted = Color.FromArgb(100, 116, 139);
        public static readonly Color MutedDark = Color.FromArgb(71, 85, 105);
        public static readonly Color Accent = Color.FromArgb(37, 99, 235);
        public static readonly Color AccentDark = Color.FromArgb(29, 78, 216);
        public static readonly Color AccentSoft = Color.FromArgb(239, 246, 255);
        public static readonly Color HeaderTop = Color.FromArgb(15, 23, 42);
        public static readonly Color HeaderBottom = Color.FromArgb(30, 41, 59);
        public static readonly Color Green = Color.FromArgb(22, 163, 74);
        public static readonly Color Red = Color.FromArgb(220, 38, 38);
        public static readonly Color Amber = Color.FromArgb(217, 119, 6);

        public static readonly Font FontUi = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontUiSmall = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontUiSmallBold = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FontBold = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FontLogo = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FontTitle = new Font("Segoe UI", 17F, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FontSubtitle = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FontSection = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);

        public static Color FromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Color.FromArgb(148, 163, 184);

            hex = hex.Trim();
            if (hex.StartsWith("#", StringComparison.Ordinal))
                hex = hex.Substring(1);

            try
            {
                if (hex.Length == 8)
                {
                    int a = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    int r = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    int g = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    int b = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return Color.FromArgb(a, r, g, b);
                }
                if (hex.Length == 6)
                {
                    int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return Color.FromArgb(r, g, b);
                }
            }
            catch
            {
            }

            return Color.FromArgb(148, 163, 184);
        }

        public static string ToHex(Color color)
        {
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        public static Color Blend(Color a, Color b, double amount)
        {
            if (amount < 0) amount = 0;
            if (amount > 1) amount = 1;
            return Color.FromArgb(
                (int)Math.Round(a.A + (b.A - a.A) * amount),
                (int)Math.Round(a.R + (b.R - a.R) * amount),
                (int)Math.Round(a.G + (b.G - a.G) * amount),
                (int)Math.Round(a.B + (b.B - a.B) * amount));
        }

        public static Color Darken(Color color, double amount)
        {
            return Blend(color, Color.Black, amount);
        }

        public static Color Lighten(Color color, double amount)
        {
            return Blend(color, Color.White, amount);
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 0) bytes = 0;
            string[] units = new string[] { "Б", "КБ", "МБ", "ГБ", "ТБ", "ПБ" };
            double value = bytes;
            int unit = 0;
            while (value >= 1024.0 && unit < units.Length - 1)
            {
                value /= 1024.0;
                unit++;
            }

            if (unit == 0)
                return bytes.ToString("N0", CultureInfo.CurrentCulture) + " " + units[unit];

            string format = value >= 100 ? "0" : (value >= 10 ? "0.#" : "0.##");
            return value.ToString(format, CultureInfo.CurrentCulture) + " " + units[unit];
        }

        public static string FormatDuration(double seconds)
        {
            if (seconds < 0) seconds = 0;
            if (seconds < 1)
                return "0 с";

            if (seconds < 60)
                return seconds.ToString("0.#", CultureInfo.CurrentCulture) + " с";

            if (seconds < 3600)
            {
                int m = (int)(seconds / 60);
                int s = (int)Math.Round(seconds - m * 60);
                if (s >= 60) { m++; s = 0; }
                return m.ToString(CultureInfo.CurrentCulture) + " мин " + s.ToString(CultureInfo.CurrentCulture) + " с";
            }

            int h = (int)(seconds / 3600);
            int mm = (int)((seconds - h * 3600) / 60);
            return h.ToString(CultureInfo.CurrentCulture) + " ч " + mm.ToString("00", CultureInfo.CurrentCulture) + " мин";
        }

        public static string FormatCount(long value)
        {
            return value.ToString("N0", CultureInfo.CurrentCulture);
        }
    }
}
