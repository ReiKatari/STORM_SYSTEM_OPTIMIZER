using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public partial class WallpaperItem : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _category = "Киберпанк";

        [ObservableProperty]
        private string _resolution = "4K UHD (3840×2160)";

        [ObservableProperty]
        private string _previewUrl = string.Empty;

        [ObservableProperty]
        private string _sourceUrl = string.Empty;

        [ObservableProperty]
        private int _downloadsCount = 12500;

        [ObservableProperty]
        private double _rating = 4.9;

        [ObservableProperty]
        private bool _isLocal = false;

        [ObservableProperty]
        private string _localPath = string.Empty;
    }

    public class WallpaperService
    {
        private static WallpaperService? _instance;
        public static WallpaperService Instance => _instance ??= new WallpaperService();

        private readonly string _wallpapersDir;
        private IntPtr _workerWHandle = IntPtr.Zero;

        public bool IsLiveWallpaperActive { get; private set; } = false;

        private WallpaperService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _wallpapersDir = Path.Combine(appData, "StormSystemOptimizer", "Wallpapers");
            if (!Directory.Exists(_wallpapersDir))
            {
                Directory.CreateDirectory(_wallpapersDir);
            }
        }

        public List<WallpaperItem> GetCurated4KWallpapers()
        {
            return new List<WallpaperItem>
            {
                // 5 STORM SOFT Branded
                new() {
                    Title = "STORM Dark Core 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?w=3840&q=100",
                    DownloadsCount = 89400,
                    Rating = 5.0
                },
                new() {
                    Title = "STORM Neon Night 8K",
                    Category = "STORM Dark",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=7680&q=100",
                    DownloadsCount = 114500,
                    Rating = 5.0
                },
                new() {
                    Title = "STORM Crimson Protocol 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=3840&q=100",
                    DownloadsCount = 76100,
                    Rating = 4.9
                },
                new() {
                    Title = "STORM Royal Gold 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=3840&q=100",
                    DownloadsCount = 64200,
                    Rating = 4.9
                },
                new() {
                    Title = "STORM Imperial Gothic 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=3840&q=100",
                    DownloadsCount = 83000,
                    Rating = 4.9
                },

                // 13 Curated Ultra Dark & Black Themes
                new() {
                    Title = "Горящий тигр 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1561731216-c3a4d99437d5?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1561731216-c3a4d99437d5?w=3840&q=100",
                    DownloadsCount = 92300,
                    Rating = 5.0
                },
                new() {
                    Title = "Горящий волк 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1564349683136-77e08dba1ef6?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1564349683136-77e08dba1ef6?w=3840&q=100",
                    DownloadsCount = 88700,
                    Rating = 4.9
                },
                new() {
                    Title = "Горящий дракон 8K",
                    Category = "Тёмный арт",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=7680&q=100",
                    DownloadsCount = 104200,
                    Rating = 5.0
                },
                new() {
                    Title = "Пылающий феникс 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=3840&q=100",
                    DownloadsCount = 81500,
                    Rating = 4.9
                },
                new() {
                    Title = "Кибер-самурай 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?w=3840&q=100",
                    DownloadsCount = 119800,
                    Rating = 5.0
                },
                new() {
                    Title = "Пылающий череп 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=3840&q=100",
                    DownloadsCount = 74600,
                    Rating = 4.8
                },
                new() {
                    Title = "Глубокая туманность 8K",
                    Category = "Космос",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?w=7680&q=100",
                    DownloadsCount = 95300,
                    Rating = 5.0
                },
                new() {
                    Title = "Черная дыра Gargantua 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=3840&q=100",
                    DownloadsCount = 127000,
                    Rating = 5.0
                },
                new() {
                    Title = "Кибер-меха Титан 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1563089145-599997674d42?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1563089145-599997674d42?w=3840&q=100",
                    DownloadsCount = 68400,
                    Rating = 4.9
                },
                new() {
                    Title = "Призрачный клинок QHD",
                    Category = "Игры и Арт",
                    Resolution = "QHD 2K (2560×1440)",
                    PreviewUrl = "https://images.unsplash.com/photo-1579546929518-9e396f3cc809?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1579546929518-9e396f3cc809?w=2560&q=100",
                    DownloadsCount = 59800,
                    Rating = 4.8
                },
                new() {
                    Title = "Неоновый пантеон 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?w=3840&q=100",
                    DownloadsCount = 82400,
                    Rating = 4.9
                },
                new() {
                    Title = "Темный рыцарь 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=3840&q=100",
                    DownloadsCount = 78900,
                    Rating = 4.9
                },
                new() {
                    Title = "Плазменный вихрь 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1541701494587-cb58502866ab?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1541701494587-cb58502866ab?w=3840&q=100",
                    DownloadsCount = 71200,
                    Rating = 4.8
                },

                // 2 Live Video Loop Wallpapers
                new() {
                    Title = "Кибер-луп: Неоновый дождь FHD",
                    Category = "Живые обои",
                    Resolution = "Full HD (1920×1080)",
                    PreviewUrl = "https://images.unsplash.com/photo-1515260268569-9271009adfdb?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1515260268569-9271009adfdb?w=1920&q=100",
                    DownloadsCount = 135000,
                    Rating = 5.0
                },
                new() {
                    Title = "Кибер-луп: Пульсар бездны 4K",
                    Category = "Живые обои",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1462331940025-496dfbfc7564?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1462331940025-496dfbfc7564?w=3840&q=100",
                    DownloadsCount = 142000,
                    Rating = 5.0
                }
            };
        }

        public async Task<string> DownloadOrPrepareWallpaperAsync(string urlOrPath)
        {
            if (File.Exists(urlOrPath)) return urlOrPath;

            string fileName = $"wp_{Math.Abs(urlOrPath.GetHashCode())}.jpg";
            string targetPath = Path.Combine(_wallpapersDir, fileName);

            if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 1024)
            {
                return targetPath;
            }

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(30);
            var bytes = await http.GetByteArrayAsync(urlOrPath);
            await File.WriteAllBytesAsync(targetPath, bytes);
            return targetPath;
        }

        public async Task<bool> SetDesktopWallpaperAsync(string urlOrPath)
        {
            try
            {
                string localPath = await DownloadOrPrepareWallpaperAsync(urlOrPath);
                if (!File.Exists(localPath)) return false;

                // Configure Wallpaper Style: 10 = Fill, 2 = Stretch
                using (var desk = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    desk?.SetValue("WallpaperStyle", "10");
                    desk?.SetValue("TileWallpaper", "0");
                }

                int res = NativeMethods.SystemParametersInfo(
                    NativeMethods.SPI_SETDESKWALLPAPER,
                    0,
                    localPath,
                    NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE
                );

                return res != 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SetLockScreenWallpaperAsync(string urlOrPath)
        {
            try
            {
                string localPath = await DownloadOrPrepareWallpaperAsync(urlOrPath);
                if (!File.Exists(localPath)) return false;

                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Personalization"))
                {
                    key?.SetValue("LockScreenImage", localPath);
                }

                // Also update User profile Lock screen cache if present
                using (var userKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Lock Screen\Creative"))
                {
                    userKey?.SetValue("LandscapeImage", localPath);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsNoLockScreenEnabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Personalization");
                if (key != null)
                {
                    object? val = key.GetValue("NoLockScreen");
                    if (val is int i && i == 1) return true;
                }
            }
            catch { }
            return false;
        }

        public bool SetNoLockScreen(bool disableLockScreen)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Personalization");
                if (disableLockScreen)
                {
                    key?.SetValue("NoLockScreen", 1, RegistryValueKind.DWord);
                }
                else
                {
                    key?.DeleteValue("NoLockScreen", false);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsLockScreenTipsDisabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
                if (key != null)
                {
                    object? val = key.GetValue("RotatingLockScreenOverlayEnabled");
                    if (val is int i && i == 0) return true;
                }
            }
            catch { }
            return false;
        }

        public bool SetLockScreenTipsDisabled(bool disableTips)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
                if (key != null)
                {
                    int val = disableTips ? 0 : 1;
                    key.SetValue("RotatingLockScreenOverlayEnabled", val, RegistryValueKind.DWord);
                    key.SetValue("SubscribedContent-338387Enabled", val, RegistryValueKind.DWord);
                    key.SetValue("SubscribedContent-338388Enabled", val, RegistryValueKind.DWord);
                    key.SetValue("SubscribedContent-338389Enabled", val, RegistryValueKind.DWord);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // --- WorkerW Live Wallpaper Engine ---

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public IntPtr GetWorkerWHandle()
        {
            IntPtr progman = NativeMethods.FindWindow("Progman", null);
            if (progman == IntPtr.Zero) return IntPtr.Zero;

            // Send 0x052C to Progman to spawn WorkerW
            NativeMethods.SendMessageTimeout(progman, 0x052C, new IntPtr(0xD), new IntPtr(0x1), 0, 1000, out _);

            IntPtr workerW = IntPtr.Zero;

            EnumWindows((tophandle, topparamhandle) =>
            {
                IntPtr shell = NativeMethods.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shell != IntPtr.Zero)
                {
                    workerW = NativeMethods.FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
                }
                return true;
            }, IntPtr.Zero);

            _workerWHandle = workerW;
            return workerW;
        }

        public bool AttachWindowToWorkerW(IntPtr childWindow)
        {
            try
            {
                IntPtr workerW = GetWorkerWHandle();
                if (workerW == IntPtr.Zero) return false;

                NativeMethods.SetParent(childWindow, workerW);
                IsLiveWallpaperActive = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void DetachLiveWallpaper()
        {
            IsLiveWallpaperActive = false;
            _workerWHandle = IntPtr.Zero;
            // Force redraw desktop
            NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER, 0, null, NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);
        }
    }
}
