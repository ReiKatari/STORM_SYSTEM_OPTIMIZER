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
        /// Formats integer with space separator (e.g. 2 834, 12 450)
        /// </summary>
        public static string FormatInt(long value)
        {
            return value.ToString("#,##0", RuCulture);
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
        /// Formats speed in MB/s (e.g. 2 834 МБ/с)
        /// </summary>
        public static string FormatSpeedMb(double value)
        {
            return $"{FormatInt((long)Math.Round(value))} МБ/с";
        }

        /// <summary>
        /// Formats speed in GB/s (e.g. 28,4 ГБ/с)
        /// </summary>
        public static string FormatSpeedGb(double value, int decimals = 1)
        {
            return $"{FormatDouble(value, decimals)} ГБ/с";
        }

        /// <summary>
        /// Formats size in MB/GB with space thousands
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
            return $"{FormatDouble(mb, 1)} МБ";
        }
    }
}
