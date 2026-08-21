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
                // Method 1: System.Drawing.Icon.ExtractAssociatedIcon
                if (File.Exists(filePath))
                {
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
                // Method 2: Win32 SHGetFileInfo
                try
                {
                    var shinfo = new SHFILEINFO();
                    IntPtr hImgSmall = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);
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
    }
}
