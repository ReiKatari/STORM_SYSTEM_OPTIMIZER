using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StormSystemOptimizer.Services
{
    public static class IconExtractorHelper
    {
        private static readonly ConcurrentDictionary<string, ImageSource?> _iconCache = new(StringComparer.OrdinalIgnoreCase);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public static ImageSource? GetProcessIcon(int processId, string processName)
        {
            if (_iconCache.TryGetValue(processName, out var cached))
            {
                return cached;
            }

            ImageSource? iconSource = null;

            try
            {
                var proc = Process.GetProcessById(processId);
                string? mainPath = proc.MainModule?.FileName;
                if (!string.IsNullOrEmpty(mainPath) && File.Exists(mainPath))
                {
                    iconSource = GetFileIcon(mainPath);
                }
            }
            catch { }

            if (iconSource == null)
            {
                // Fallback to process name search in system or Windows directories
                iconSource = TryFindIconByProcessName(processName);
            }

            _iconCache[processName] = iconSource;
            return iconSource;
        }

        public static ImageSource? GetFileIcon(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;

            if (_iconCache.TryGetValue(filePath, out var cached))
            {
                return cached;
            }

            ImageSource? source = null;

            try
            {
                if (filePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(filePath))
                    {
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.UriSource = new Uri(filePath, UriKind.Absolute);
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.EndInit();
                        bi.Freeze();
                        source = bi;
                    }
                }
                else if (File.Exists(filePath))
                {
                    // Method 1: System.Drawing.Icon.ExtractAssociatedIcon
                    using var icon = Icon.ExtractAssociatedIcon(filePath);
                    if (icon != null)
                    {
                        source = Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        source.Freeze();
                    }
                }
            }
            catch { }

            if (source == null)
            {
                // Method 2: Win32 SHGetFileInfo with High-DPI Large Icon
                try
                {
                    var shinfo = new SHFILEINFO();
                    IntPtr hImg = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);
                    if (shinfo.hIcon != IntPtr.Zero)
                    {
                        source = Imaging.CreateBitmapSourceFromHIcon(
                            shinfo.hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        source.Freeze();
                        DestroyIcon(shinfo.hIcon);
                    }
                }
                catch { }
            }

            _iconCache[filePath] = source;
            return source;
        }

        private static ImageSource? TryFindIconByProcessName(string name)
        {
            string clean = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : name + ".exe";

            string[] paths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), clean),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), clean),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), clean),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), clean)
            };

            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    var icon = GetFileIcon(p);
                    if (icon != null) return icon;
                }
            }

            return null;
        }

        public static ImageSource GetLauncherFallbackIcon(string launcherId)
        {
            string key = "fallback_launcher_" + launcherId.ToLowerInvariant();
            if (_iconCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var group = new DrawingGroup();
            var dc = group.Open();

            (System.Windows.Media.Color bg1, System.Windows.Media.Color bg2, string text, System.Windows.Media.Color textColor) = launcherId.ToLowerInvariant() switch
            {
                "steam" => (System.Windows.Media.Color.FromRgb(23, 36, 54), System.Windows.Media.Color.FromRgb(15, 23, 42), "♨", System.Windows.Media.Color.FromRgb(0, 210, 255)),
                "epic" => (System.Windows.Media.Color.FromRgb(30, 41, 59), System.Windows.Media.Color.FromRgb(15, 23, 42), "⚡", System.Windows.Media.Color.FromRgb(241, 245, 249)),
                "ea" => (System.Windows.Media.Color.FromRgb(225, 29, 72), System.Windows.Media.Color.FromRgb(159, 18, 57), "EA", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                "gog" => (System.Windows.Media.Color.FromRgb(124, 58, 237), System.Windows.Media.Color.FromRgb(76, 29, 149), "GOG", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                "battlenet" => (System.Windows.Media.Color.FromRgb(2, 132, 199), System.Windows.Media.Color.FromRgb(12, 74, 110), "⚔", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                "playnite" => (System.Windows.Media.Color.FromRgb(249, 115, 22), System.Windows.Media.Color.FromRgb(194, 65, 12), "🕹", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                "retroarch" => (System.Windows.Media.Color.FromRgb(16, 185, 129), System.Windows.Media.Color.FromRgb(4, 120, 87), "👾", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                "discord" => (System.Windows.Media.Color.FromRgb(88, 101, 242), System.Windows.Media.Color.FromRgb(67, 76, 182), "💬", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                "ubisoft" => (System.Windows.Media.Color.FromRgb(6, 182, 212), System.Windows.Media.Color.FromRgb(14, 116, 144), "🌀", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                "vkplay" => (System.Windows.Media.Color.FromRgb(255, 51, 75), System.Windows.Media.Color.FromRgb(220, 20, 50), "▶", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                "rockstar" => (System.Windows.Media.Color.FromRgb(245, 158, 11), System.Windows.Media.Color.FromRgb(180, 83, 9), "★", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                "launchbox" => (System.Windows.Media.Color.FromRgb(14, 165, 233), System.Windows.Media.Color.FromRgb(3, 105, 161), "📦", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                _ => (System.Windows.Media.Color.FromRgb(71, 85, 105), System.Windows.Media.Color.FromRgb(30, 41, 59), "🎮", System.Windows.Media.Color.FromRgb(255, 255, 255))
            };

            var bgBrush = new LinearGradientBrush(bg1, bg2, new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));
            var borderPen = new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 255, 255, 255)), 1.5);
            dc.DrawRoundedRectangle(bgBrush, borderPen, new Rect(2, 2, 44, 44), 8, 8);

            var ft = new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface(new System.Windows.Media.FontFamily("Segoe UI, Segoe UI Emoji, Arial"), System.Windows.FontStyles.Normal, System.Windows.FontWeights.Bold, System.Windows.FontStretches.Normal),
                text.Length > 2 ? 14 : 20,
                new SolidColorBrush(textColor),
                1.0);

            double x = (48 - ft.Width) / 2;
            double y = (48 - ft.Height) / 2;
            dc.DrawText(ft, new System.Windows.Point(x, y));

            dc.Close();
            var img = new DrawingImage(group);
            img.Freeze();
            _iconCache[key] = img;
            return img;
        }
    }
}
