using System;
using System.Globalization;

namespace StormSystemOptimizer.Services
{
    public static class FormatHelper
    {
        public static readonly CultureInfo RuCulture = new CultureInfo("ru-RU")
        {
            NumberFormat =
            {
                NumberGroupSeparator = " ",
                CurrencyGroupSeparator = " ",
                PercentGroupSeparator = " "
            }
        };

        /// <summary>
        /// Formats integer with space separator (e.g. 4 232, 12 912, 147 501 816)
        /// </summary>
        public static string FormatInt(long value)
        {
            return value.ToString("#,##0", RuCulture);
        }

        public static string FormatNumber(object? value)
        {
            if (value == null) return "0";
            if (value is long l) return FormatInt(l);
            if (value is int i) return FormatInt(i);
            if (value is double d) return FormatDouble(d);
            if (value is float f) return FormatDouble(f);
            if (long.TryParse(value.ToString(), out long parsedLong)) return FormatInt(parsedLong);
            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDbl)) return FormatDouble(parsedDbl);
            return value.ToString() ?? "0";
        }

        /// <summary>
        /// Formats double with space thousand separator and optional decimal precision
        /// </summary>
        public static string FormatDouble(double value, int decimals = 0)
        {
            if (decimals <= 0)
            {
                return Math.Round(value).ToString("#,##0", RuCulture);
            }
            string fmt = "#,##0." + new string('0', decimals);
            return value.ToString(fmt, RuCulture);
        }

        /// <summary>
        /// Formats score in PTS (e.g. 12 450 PTS)
        /// </summary>
        public static string FormatPts(double value, bool uppercase = true)
        {
            string unit = uppercase ? "PTS" : "Pts";
            return $"{FormatInt((long)Math.Round(value))} {unit}";
        }

        /// <summary>
        /// Formats IOPS (e.g. 45 000 IOPS)
        /// </summary>
        public static string FormatIops(long value)
        {
            return $"{FormatInt(value)} IOPS";
        }

        /// <summary>
        /// Formats frequency in MHz (e.g. 3 200 МГц, 6 000 МГц)
        /// </summary>
        public static string FormatMhz(double mhz)
        {
            return $"{FormatDouble(mhz, 0)} МГц";
        }

        /// <summary>
        /// Formats frequency in Hz (e.g. 1 000 Гц, 8 000 Гц)
        /// </summary>
        public static string FormatHz(double hz)
        {
            return $"{FormatDouble(hz, 0)} Гц";
        }

        /// <summary>
        /// Formats speed in MB/s (e.g. 2 834 МБ/с)
        /// </summary>
        public static string FormatSpeedMb(double value)
        {
            return $"{FormatDouble(value, 0)} МБ/с";
        }

        /// <summary>
        /// Formats speed in GB/s (e.g. 28,4 ГБ/с)
        /// </summary>
        public static string FormatSpeedGb(double value, int decimals = 1)
        {
            return $"{FormatDouble(value, decimals)} ГБ/с";
        }

        /// <summary>
        /// Formats size in MB/GB with space thousands (e.g. 4 096 МБ, 12 288 МБ)
        /// </summary>
        public static string FormatMegabytes(double mb)
        {
            if (mb >= 1024.0 * 1024.0)
            {
                return $"{FormatDouble(mb / (1024.0 * 1024.0), 2)} ТБ";
            }
            if (mb >= 1024.0)
            {
                return $"{FormatDouble(mb / 1024.0, 1)} ГБ";
            }
            return $"{FormatDouble(mb, 0)} МБ";
        }

        /// <summary>
        /// Formats bytes into B, KB, MB, GB, TB with space thousands
        /// </summary>
        public static string FormatSize(long bytes)
        {
            if (bytes < 0) bytes = 0;
            if (bytes >= 1024L * 1024L * 1024L * 1024L)
                return $"{FormatDouble((double)bytes / (1024L * 1024L * 1024L * 1024L), 2)} ТБ";
            if (bytes >= 1024L * 1024L * 1024L)
                return $"{FormatDouble((double)bytes / (1024L * 1024L * 1024L), 1)} ГБ";
            if (bytes >= 1024L * 1024L)
                return $"{FormatDouble((double)bytes / (1024L * 1024L), 1)} МБ";
            if (bytes >= 1024L)
                return $"{FormatDouble((double)bytes / 1024L, 1)} КБ";
            return $"{FormatInt(bytes)} Б";
        }

        public static string FormatBytes(long bytes) => FormatSize(bytes);

        public static string FormatOperatingTime(long totalHours)
        {
            if (totalHours <= 0) return "Менее 1 дня (новый)";
            long days = totalHours / 24;
            long years = days / 365;
            long remainingDays = days % 365;
            long months = remainingDays / 30;

            string formattedHours = FormatInt(totalHours);

            if (years > 0)
            {
                string yearStr = GetPlural(years, "год", "года", "лет");
                if (months > 0)
                {
                    string monthStr = GetPlural(months, "месяц", "месяца", "месяцев");
                    return $"{years} {yearStr}, {months} {monthStr} ({formattedHours} ч)";
                }
                return $"{years} {yearStr} ({formattedHours} ч)";
            }
            if (months > 0)
            {
                string monthStr = GetPlural(months, "месяц", "месяца", "месяцев");
                return $"{months} {monthStr} ({formattedHours} ч)";
            }
            string dayStr = GetPlural(Math.Max(1, days), "день", "дня", "дней");
            return $"{Math.Max(1, days)} {dayStr} ({formattedHours} ч)";
        }

        public static string GetPlural(long number, string one, string two, string five)
        {
            long n = Math.Abs(number) % 100;
            long n1 = n % 10;
            if (n > 10 && n < 20) return five;
            if (n1 > 1 && n1 < 5) return two;
            if (n1 == 1) return one;
            return five;
        }
    }
}
