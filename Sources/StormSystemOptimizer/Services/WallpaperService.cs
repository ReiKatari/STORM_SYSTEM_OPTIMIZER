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

    public class SystemMonitorInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Index { get; set; } = 0;
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public bool IsPrimary { get; set; } = false;
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

        public List<SystemMonitorInfo> GetSystemMonitors()
        {
            var list = new List<SystemMonitorInfo>
            {
                new() { DeviceId = "", DisplayName = "Все экраны системы", Index = -1 }
            };

            try
            {
                var wallpaper = (NativeMethods.IDesktopWallpaper)new NativeMethods.DesktopWallpaperClass();
                uint count = wallpaper.GetMonitorDevicePathCount();
                for (uint i = 0; i < count; i++)
                {
                    string monId = wallpaper.GetMonitorDevicePathAt(i);
                    var rect = wallpaper.GetMonitorRECT(monId);
                    int w = Math.Abs(rect.Right - rect.Left);
                    int h = Math.Abs(rect.Bottom - rect.Top);
                    bool isPrimary = (rect.Left == 0 && rect.Top == 0);
                    string name = isPrimary 
                        ? $"Монитор {i + 1} (Основной, {w}×{h})" 
                        : $"Монитор {i + 1} ({w}×{h})";

                    list.Add(new SystemMonitorInfo
                    {
                        DeviceId = monId,
                        DisplayName = name,
                        Index = (int)i,
                        Width = w,
                        Height = h,
                        IsPrimary = isPrimary
                    });
                }
            }
            catch
            {
                list.Add(new SystemMonitorInfo { DeviceId = "mon1", DisplayName = "Монитор 1 (Основной)", Index = 0, IsPrimary = true });
            }

            return list;
        }

        public List<WallpaperItem> GetCurated4KWallpapers()
        {
            return new List<WallpaperItem>
            {
                // 1. STORM Dark (8 фирменных обоев)
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
                    PreviewUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=7680&q=100",
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
                    PreviewUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=3840&q=100",
                    DownloadsCount = 83000,
                    Rating = 4.9
                },
                new() {
                    Title = "STORM Cyber Matrix 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=3840&q=100",
                    DownloadsCount = 71800,
                    Rating = 4.9
                },
                new() {
                    Title = "STORM Midnight Amethyst 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=3840&q=100",
                    DownloadsCount = 68900,
                    Rating = 4.8
                },
                new() {
                    Title = "STORM OLED Absolute 8K",
                    Category = "STORM Dark",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1550684848-fac1c5b4e853?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1550684848-fac1c5b4e853?w=7680&q=100",
                    DownloadsCount = 98200,
                    Rating = 5.0
                },

                // 2. Тёмный арт (10 работ)
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
                    PreviewUrl = "https://images.unsplash.com/photo-1577493340887-b7bdef550155?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1577493340887-b7bdef550155?w=7680&q=100",
                    DownloadsCount = 104200,
                    Rating = 5.0
                },
                new() {
                    Title = "Пылающий феникс 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1513836279014-a89f7a76ae86?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1513836279014-a89f7a76ae86?w=3840&q=100",
                    DownloadsCount = 81500,
                    Rating = 4.9
                },
                new() {
                    Title = "Пылающий череп 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1508214751196-bcfd4ca60f91?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1508214751196-bcfd4ca60f91?w=3840&q=100",
                    DownloadsCount = 74600,
                    Rating = 4.8
                },
                new() {
                    Title = "Ледяной левиафан 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1544551763-46a013bb70d5?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1544551763-46a013bb70d5?w=3840&q=100",
                    DownloadsCount = 67300,
                    Rating = 4.9
                },
                new() {
                    Title = "Темный рыцарь 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1514539079130-25950c84af65?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1514539079130-25950c84af65?w=3840&q=100",
                    DownloadsCount = 78900,
                    Rating = 4.9
                },
                new() {
                    Title = "Теневой демон 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1509281373149-e957c6296406?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1509281373149-e957c6296406?w=3840&q=100",
                    DownloadsCount = 63100,
                    Rating = 4.8
                },
                new() {
                    Title = "Черный ворон 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1516339901601-2e1b62dc0c45?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1516339901601-2e1b62dc0c45?w=3840&q=100",
                    DownloadsCount = 59400,
                    Rating = 4.9
                },
                new() {
                    Title = "Лунный грифон 8K",
                    Category = "Тёмный арт",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1579273166152-d725a4e2b755?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1579273166152-d725a4e2b755?w=7680&q=100",
                    DownloadsCount = 84200,
                    Rating = 5.0
                },

                // 3. Киберпанк (10 работ)
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
                    Title = "Неоновый пантеон 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?w=3840&q=100",
                    DownloadsCount = 82400,
                    Rating = 4.9
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
                    Title = "Неоновый Найт-Сити 8K",
                    Category = "Киберпанк",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1519501025264-65ba15a82390?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1519501025264-65ba15a82390?w=7680&q=100",
                    DownloadsCount = 129400,
                    Rating = 5.0
                },
                new() {
                    Title = "Кибер-мотоцикл Неон 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1558981806-ec527fa84c39?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1558981806-ec527fa84c39?w=3840&q=100",
                    DownloadsCount = 75300,
                    Rating = 4.9
                },
                new() {
                    Title = "Кибер-город 2099 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1514565131-fce0801e5785?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1514565131-fce0801e5785?w=3840&q=100",
                    DownloadsCount = 91200,
                    Rating = 4.9
                },
                new() {
                    Title = "Нейросетевой хакер QHD",
                    Category = "Киберпанк",
                    Resolution = "QHD 2K (2560×1440)",
                    PreviewUrl = "https://images.unsplash.com/photo-1510519138111-577d3542f958?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1510519138111-577d3542f958?w=2560&q=100",
                    DownloadsCount = 61800,
                    Rating = 4.8
                },
                new() {
                    Title = "Неоновый дождь Токио 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1503899036084-c55cdd92da26?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1503899036084-c55cdd92da26?w=3840&q=100",
                    DownloadsCount = 104500,
                    Rating = 5.0
                },
                new() {
                    Title = "Синтвейв трасса 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1509228468518-180dd4864904?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1509228468518-180dd4864904?w=3840&q=100",
                    DownloadsCount = 83200,
                    Rating = 4.9
                },
                new() {
                    Title = "Квантовый андроид 8K",
                    Category = "Киберпанк",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1531746020798-e6953c6e8e04?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1531746020798-e6953c6e8e04?w=7680&q=100",
                    DownloadsCount = 95600,
                    Rating = 5.0
                },

                // 4. Космос (10 работ)
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
                    Title = "Туманность Ориона 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1446776811953-b23d57bd21aa?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1446776811953-b23d57bd21aa?w=3840&q=100",
                    DownloadsCount = 88400,
                    Rating = 4.9
                },
                new() {
                    Title = "Галактика Млечный Путь 8K",
                    Category = "Космос",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1502134249126-9f3755a50d78?w=7680&q=100",
                    DownloadsCount = 112000,
                    Rating = 5.0
                },
                new() {
                    Title = "Рождение сверхновой 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1462331940025-496dfbfc7564?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1462331940025-496dfbfc7564?w=3840&q=100",
                    DownloadsCount = 74500,
                    Rating = 4.9
                },
                new() {
                    Title = "Солнечный протуберанец 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1532693322450-2cb5c511067d?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1532693322450-2cb5c511067d?w=3840&q=100",
                    DownloadsCount = 69200,
                    Rating = 4.8
                },
                new() {
                    Title = "Ледяная экзопланета 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1614728894747-a83421e2b9c9?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1614728894747-a83421e2b9c9?w=3840&q=100",
                    DownloadsCount = 77100,
                    Rating = 4.9
                },
                new() {
                    Title = "Космический телескоп 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1447433589675-4aaa569f3e05?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1447433589675-4aaa569f3e05?w=3840&q=100",
                    DownloadsCount = 63800,
                    Rating = 4.8
                },
                new() {
                    Title = "Столпы творения 8K",
                    Category = "Космос",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1543722530-d2c3201371e7?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1543722530-d2c3201371e7?w=7680&q=100",
                    DownloadsCount = 108300,
                    Rating = 5.0
                },
                new() {
                    Title = "Полярное сияние Земли 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1483347756197-71ef80e95f73?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1483347756197-71ef80e95f73?w=3840&q=100",
                    DownloadsCount = 89900,
                    Rating = 4.9
                },

                // 5. Игры и Арт (10 работ)
                new() {
                    Title = "Призрачный клинок QHD",
                    Category = "Игры и Арт",
                    Resolution = "QHD 2K (2560×1440)",
                    PreviewUrl = "https://images.unsplash.com/photo-1589241062272-c0a000072dfa?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1589241062272-c0a000072dfa?w=2560&q=100",
                    DownloadsCount = 59800,
                    Rating = 4.8
                },
                new() {
                    Title = "Драконий хребет 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=3840&q=100",
                    DownloadsCount = 84500,
                    Rating = 4.9
                },
                new() {
                    Title = "Страж цитадели 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1519681393784-d120267933ba?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1519681393784-d120267933ba?w=3840&q=100",
                    DownloadsCount = 76200,
                    Rating = 4.9
                },
                new() {
                    Title = "Древний храм рун 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1548013146-72479768bada?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1548013146-72479768bada?w=3840&q=100",
                    DownloadsCount = 71300,
                    Rating = 4.8
                },
                new() {
                    Title = "Магический портал 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1518709766631-a6a7f45921c3?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1518709766631-a6a7f45921c3?w=3840&q=100",
                    DownloadsCount = 82100,
                    Rating = 4.9
                },
                new() {
                    Title = "Рыцарь солнца 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=3840&q=100",
                    DownloadsCount = 65400,
                    Rating = 4.8
                },
                new() {
                    Title = "Подземный кузнечный горн 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1504917599217-d4dc5ebe6122?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1504917599217-d4dc5ebe6122?w=3840&q=100",
                    DownloadsCount = 69800,
                    Rating = 4.9
                },
                new() {
                    Title = "Парящие острова 8K",
                    Category = "Игры и Арт",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=7680&q=100",
                    DownloadsCount = 94300,
                    Rating = 5.0
                },
                new() {
                    Title = "Лесной дух хранитель 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1511497584788-87676104235f?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1511497584788-87676104235f?w=3840&q=100",
                    DownloadsCount = 73200,
                    Rating = 4.9
                },
                new() {
                    Title = "Эпическая битва титанов 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1579783928621-7a13d66a62d1?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1579783928621-7a13d66a62d1?w=3840&q=100",
                    DownloadsCount = 88700,
                    Rating = 4.9
                },

                // 6. Абстракция (10 работ)
                new() {
                    Title = "Плазменный вихрь 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1541701494587-cb58502866ab?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1541701494587-cb58502866ab?w=3840&q=100",
                    DownloadsCount = 71200,
                    Rating = 4.8
                },
                new() {
                    Title = "Темная жидкая сфера 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1634017839464-5c339ebe3cb4?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1634017839464-5c339ebe3cb4?w=3840&q=100",
                    DownloadsCount = 85400,
                    Rating = 4.9
                },
                new() {
                    Title = "Золотые фракталы 8K",
                    Category = "Абстракция",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=7680&q=100",
                    DownloadsCount = 92600,
                    Rating = 5.0
                },
                new() {
                    Title = "Неоморфная волна 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1579783902614-a3fb3927b675?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1579783902614-a3fb3927b675?w=3840&q=100",
                    DownloadsCount = 68300,
                    Rating = 4.8
                },
                new() {
                    Title = "Квантовые нити 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1507499739999-097706ad8914?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1507499739999-097706ad8914?w=3840&q=100",
                    DownloadsCount = 74100,
                    Rating = 4.9
                },
                new() {
                    Title = "Голографическая призма 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1550684847-75bdda21cc95?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1550684847-75bdda21cc95?w=3840&q=100",
                    DownloadsCount = 79500,
                    Rating = 4.9
                },
                new() {
                    Title = "Геометрический горизонт QHD",
                    Category = "Абстракция",
                    Resolution = "QHD 2K (2560×1440)",
                    PreviewUrl = "https://images.unsplash.com/photo-1513694203232-719a280e022f?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1513694203232-719a280e022f?w=2560&q=100",
                    DownloadsCount = 62400,
                    Rating = 4.8
                },
                new() {
                    Title = "Неоновый гиперкуб 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=3840&q=100",
                    DownloadsCount = 81700,
                    Rating = 4.9
                },
                new() {
                    Title = "Черный шелк и хром 8K",
                    Category = "Абстракция",
                    Resolution = "8K Ultra HD (7680×4320)",
                    PreviewUrl = "https://images.unsplash.com/photo-1507679799987-c73779587ccf?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1507679799987-c73779587ccf?w=7680&q=100",
                    DownloadsCount = 96400,
                    Rating = 5.0
                },
                new() {
                    Title = "Энергетический кристалл 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1567095761054-7a02e69e5c43?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1567095761054-7a02e69e5c43?w=3840&q=100",
                    DownloadsCount = 77800,
                    Rating = 4.9
                },

                // 7. Живые луп-обои WorkerW (4 видеопотока)
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
                    PreviewUrl = "https://images.unsplash.com/photo-1464802686167-b939a6910659?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1464802686167-b939a6910659?w=3840&q=100",
                    DownloadsCount = 142000,
                    Rating = 5.0
                },
                new() {
                    Title = "Кибер-луп: Плазменный шторм FHD",
                    Category = "Живые обои",
                    Resolution = "Full HD (1920×1080)",
                    PreviewUrl = "https://images.unsplash.com/photo-1558591710-4b4a1ae0f04d?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1558591710-4b4a1ae0f04d?w=1920&q=100",
                    DownloadsCount = 118000,
                    Rating = 4.9
                },
                new() {
                    Title = "Кибер-луп: Квантовый реактор 4K",
                    Category = "Живые обои",
                    Resolution = "4K UHD (3840×2160)",
                    PreviewUrl = "https://images.unsplash.com/photo-1520034475321-cbe63696469a?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1520034475321-cbe63696469a?w=3840&q=100",
                    DownloadsCount = 126500,
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

        public async Task<bool> SetDesktopWallpaperAsync(string urlOrPath, string? monitorDeviceId = null)
        {
            try
            {
                string localPath = await DownloadOrPrepareWallpaperAsync(urlOrPath);
                if (!File.Exists(localPath)) return false;

                try
                {
                    var wallpaper = (NativeMethods.IDesktopWallpaper)new NativeMethods.DesktopWallpaperClass();
                    wallpaper.SetPosition(NativeMethods.DesktopWallpaperPosition.Fill);
                    if (string.IsNullOrEmpty(monitorDeviceId))
                    {
                        wallpaper.SetWallpaper(null, localPath);
                    }
                    else
                    {
                        wallpaper.SetWallpaper(monitorDeviceId, localPath);
                    }
                    return true;
                }
                catch
                {
                    // Fallback to classic SystemParametersInfo
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
