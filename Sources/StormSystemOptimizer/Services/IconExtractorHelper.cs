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
            using (var dc = group.Open())
            {
                DrawVectorLogo(dc, launcherId.ToLowerInvariant());
            }

            var img = new DrawingImage(group);
            img.Freeze();
            _iconCache[key] = img;
            return img;
        }

        private static void DrawVectorLogo(DrawingContext dc, string id)
        {
            switch (id)
            {
                case "playnite":
                    // Playnite Official Orange Diamond & Controller
                    var plBg = new LinearGradientBrush(System.Windows.Media.Color.FromRgb(255, 115, 0), System.Windows.Media.Color.FromRgb(217, 75, 0), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));
                    dc.DrawRoundedRectangle(plBg, null, new Rect(2, 2, 44, 44), 10, 10);
                    // Diamond polygon
                    var plDiamond = Geometry.Parse("M 24,9 L 39,18 L 39,30 L 24,39 L 9,30 L 9,18 Z");
                    dc.DrawGeometry(new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)), null, plDiamond);
                    // Inner diamond cut
                    var plInner = Geometry.Parse("M 24,14 L 34,20 L 34,28 L 24,34 L 14,28 L 14,20 Z");
                    dc.DrawGeometry(new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 81, 0)), null, plInner);
                    // Controller D-Pad cross & button dots
                    dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(17, 22, 5, 2));
                    dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(18.5, 20.5, 2, 5));
                    dc.DrawEllipse(System.Windows.Media.Brushes.White, null, new System.Windows.Point(28, 22), 1.4, 1.4);
                    dc.DrawEllipse(System.Windows.Media.Brushes.White, null, new System.Windows.Point(30.5, 24.5), 1.4, 1.4);
                    break;

                case "battlenet":
                    // Blizzard Battle.net Tri-Spiral Vortex
                    var bnBg = new LinearGradientBrush(System.Windows.Media.Color.FromRgb(0, 38, 77), System.Windows.Media.Color.FromRgb(0, 17, 36), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));
                    var bnBorder = new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(90, 0, 174, 255)), 1.5);
                    dc.DrawRoundedRectangle(bnBg, bnBorder, new Rect(2, 2, 44, 44), 10, 10);
                    // Three Blizzard vortex spirals
                    var vortexPen = new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 174, 255)), 3.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                    var arm1 = Geometry.Parse("M 24,24 C 24,15 31,11 36,13 C 33,18 29,20 24,24");
                    var arm2 = Geometry.Parse("M 24,24 C 17,28 13,34 16,38 C 21,37 25,32 24,24");
                    var arm3 = Geometry.Parse("M 24,24 C 31,29 32,37 28,40 C 26,35 24,30 24,24");
                    dc.DrawGeometry(null, vortexPen, arm1);
                    dc.DrawGeometry(null, vortexPen, arm2);
                    dc.DrawGeometry(null, vortexPen, arm3);
                    dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)), null, new System.Windows.Point(24, 24), 3, 3);
                    break;

                case "gog":
                    // GOG Galaxy Cosmic Planetary Orb & Orbit Ring
                    var gogBg = new LinearGradientBrush(System.Windows.Media.Color.FromRgb(26, 11, 46), System.Windows.Media.Color.FromRgb(11, 4, 24), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));
                    var gogBorder = new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(90, 168, 85, 247)), 1.5);
                    dc.DrawRoundedRectangle(gogBg, gogBorder, new Rect(2, 2, 44, 44), 10, 10);
                    // Glowing Center Planet
                    var planetBrush = new RadialGradientBrush(System.Windows.Media.Color.FromRgb(192, 132, 252), System.Windows.Media.Color.FromRgb(124, 58, 237));
                    dc.DrawEllipse(planetBrush, null, new System.Windows.Point(24, 24), 9, 9);
                    // Orbit Ring (tilted 30 deg)
                    dc.PushTransform(new RotateTransform(-28, 24, 24));
                    var orbitPen = new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 233, 213, 255)), 2.2);
                    dc.DrawEllipse(null, orbitPen, new System.Windows.Point(24, 24), 16, 5.5);
                    dc.Pop();
                    break;

                case "launchbox":
                    // LaunchBox Retro Console & Arcade Cabinet
                    var lbBg = new LinearGradientBrush(System.Windows.Media.Color.FromRgb(183, 28, 28), System.Windows.Media.Color.FromRgb(110, 8, 8), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));
                    var lbBorder = new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(90, 239, 68, 68)), 1.5);
                    dc.DrawRoundedRectangle(lbBg, lbBorder, new Rect(2, 2, 44, 44), 10, 10);
                    // Arcade Cabinet Outline
                    var cabinet = Geometry.Parse("M 14,10 L 34,10 L 31,24 L 35,37 L 13,37 L 17,24 Z");
                    dc.DrawGeometry(new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)), null, cabinet);
                    // Screen
                    dc.DrawRoundedRectangle(new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)), null, new Rect(17.5, 13.5, 13, 8), 1.5, 1.5);
                    // Marquee & Controller Accent
                    dc.DrawRectangle(new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)), null, new Rect(18, 11, 12, 1.8));
                    dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)), null, new System.Windows.Point(21, 29), 2, 2);
                    dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 165, 233)), null, new System.Windows.Point(27, 28), 1.4, 1.4);
                    dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)), null, new System.Windows.Point(29.5, 30.5), 1.4, 1.4);
                    break;

                default:
                    // Generic fallback with styled icon emoji
                    (System.Windows.Media.Color bg1, System.Windows.Media.Color bg2, string text, System.Windows.Media.Color textColor) = id switch
                    {
                        "steam" => (System.Windows.Media.Color.FromRgb(23, 36, 54), System.Windows.Media.Color.FromRgb(15, 23, 42), "♨", System.Windows.Media.Color.FromRgb(0, 210, 255)),
                        "epic" => (System.Windows.Media.Color.FromRgb(30, 41, 59), System.Windows.Media.Color.FromRgb(15, 23, 42), "⚡", System.Windows.Media.Color.FromRgb(241, 245, 249)),
                        "ea" => (System.Windows.Media.Color.FromRgb(225, 29, 72), System.Windows.Media.Color.FromRgb(159, 18, 57), "EA", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                        "retroarch" => (System.Windows.Media.Color.FromRgb(16, 185, 129), System.Windows.Media.Color.FromRgb(4, 120, 87), "👾", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                        "discord" => (System.Windows.Media.Color.FromRgb(88, 101, 242), System.Windows.Media.Color.FromRgb(67, 76, 182), "💬", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                        "ubisoft" => (System.Windows.Media.Color.FromRgb(6, 182, 212), System.Windows.Media.Color.FromRgb(14, 116, 144), "🌀", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                        "vkplay" => (System.Windows.Media.Color.FromRgb(255, 45, 85), System.Windows.Media.Color.FromRgb(217, 4, 41), "VK", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                        "rockstar" => (System.Windows.Media.Color.FromRgb(245, 158, 11), System.Windows.Media.Color.FromRgb(180, 83, 9), "★", System.Windows.Media.Color.FromRgb(255, 255, 255)),
                        _ => (System.Windows.Media.Color.FromRgb(71, 85, 105), System.Windows.Media.Color.FromRgb(30, 41, 59), "🎮", System.Windows.Media.Color.FromRgb(255, 255, 255))
                    };

                    var bg = new LinearGradientBrush(bg1, bg2, new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));
                    var border = new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 255, 255, 255)), 1.5);
                    dc.DrawRoundedRectangle(bg, border, new Rect(2, 2, 44, 44), 8, 8);

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
                    break;
            }
        }
    }
}
