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
        private string _format = "JPG Ultra-HD";

        [ObservableProperty]
        private string _aspectRatio = "16:9 Landscape";

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

            bool comSuccess = false;
            try
            {
                var wallpaper = (NativeMethods.IDesktopWallpaper)new NativeMethods.DesktopWallpaperClass();
                uint count = wallpaper.GetMonitorDevicePathCount();
                if (count > 0)
                {
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
                    comSuccess = true;
                }
            }
            catch { }

            if (!comSuccess || list.Count <= 1)
            {
                try
                {
                    uint devNum = 0;
                    var d = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>() };
                    while (NativeMethods.EnumDisplayDevices(null, devNum, ref d, 0))
                    {
                        if ((d.StateFlags & NativeMethods.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0)
                        {
                            var dm = new NativeMethods.DEVMODE { dmSize = (short)Marshal.SizeOf<NativeMethods.DEVMODE>() };
                            int width = 1920, height = 1080;
                            if (NativeMethods.EnumDisplaySettings(d.DeviceName, -1, ref dm))
                            {
                                width = dm.dmPelsWidth;
                                height = dm.dmPelsHeight;
                            }
                            bool isPrimary = (d.StateFlags & NativeMethods.DISPLAY_DEVICE_PRIMARY_DEVICE) != 0;
                            string name = isPrimary
                                ? $"Монитор {list.Count} (Основной, {width}×{height})"
                                : $"Монитор {list.Count} ({width}×{height})";
                            list.Add(new SystemMonitorInfo
                            {
                                DeviceId = d.DeviceName,
                                DisplayName = name,
                                Index = (int)devNum,
                                Width = width,
                                Height = height,
                                IsPrimary = isPrimary
                            });
                        }
                        devNum++;
                        d.cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>();
                    }
                }
                catch
                {
                    list.Add(new SystemMonitorInfo { DeviceId = "mon1", DisplayName = "Монитор 1 (Основной, 3840×2160)", Index = 0, IsPrimary = true });
                }
            }

            return list;
        }

        public List<WallpaperItem> GetCurated4KWallpapers()
        {
            return new List<WallpaperItem>
            {
                // 1. STORM Dark (8 фирменных темных обоев)
                new() {
                    Title = "STORM Кибер-материя 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?w=3840&q=100"
                },
                new() {
                    Title = "STORM Неоновый поток 8K",
                    Category = "STORM Dark",
                    Resolution = "8K Ultra HD (7680×4320)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=7680&q=100"
                },
                new() {
                    Title = "STORM Жидкий обсидиан 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=3840&q=100"
                },
                new() {
                    Title = "STORM Хромированный абстракт 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/abstract/Walkhrome.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/abstract/Walkhrome.jpg"
                },
                new() {
                    Title = "STORM Светящиеся медузы в бездне 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/animals/NOUNS-jellyfish.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/animals/NOUNS-jellyfish.jpg"
                },
                new() {
                    Title = "STORM Кибер-сфера OLED 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/oled.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/oled.jpg"
                },
                new() {
                    Title = "STORM Звездная бездна 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?w=3840&q=100"
                },
                new() {
                    Title = "STORM Неоновый пульс 4K",
                    Category = "STORM Dark",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1541701494587-cb58502866ab?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1541701494587-cb58502866ab?w=3840&q=100"
                },

                // 2. Тёмный арт (8 обоев, включая настоящий череп)
                new() {
                    Title = "Неоновый череп 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2550)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1553610074-8c838fa2e56e?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1553610074-8c838fa2e56e?w=3840&q=100"
                },
                new() {
                    Title = "Одинокое дерево на утесе 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/art/ARTWORK-lonely-tree.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/art/ARTWORK-lonely-tree.jpg"
                },
                new() {
                    Title = "Эхо и нарциссы 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/art/ARTWORK-echo-and-narcassias.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/art/ARTWORK-echo-and-narcassias.jpg"
                },
                new() {
                    Title = "Мрачная цитадель в тумане 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=3840&q=100"
                },
                new() {
                    Title = "Ночной лес и светлячки 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1511497584788-87676104235f?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1511497584788-87676104235f?w=3840&q=100"
                },
                new() {
                    Title = "Готический собор в лунном свете 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1548625361-16eb792ff4fe?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1548625361-16eb792ff4fe?w=3840&q=100"
                },
                new() {
                    Title = "Абстрактный темный монолит 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=3840&q=100"
                },
                new() {
                    Title = "Ледяной шторм в ночи 4K",
                    Category = "Тёмный арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1517824806704-9040b037703b?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1517824806704-9040b037703b?w=3840&q=100"
                },

                // 3. Космос (8 обоев от NASA, JWST и астрофотографов)
                new() {
                    Title = "NASA Кольца Сатурна 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (4320×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "2:1 Ultrawide",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/NOUNS-saturn.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/NOUNS-saturn.jpg"
                },
                new() {
                    Title = "Туманность Альдебаран 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/aldebaran-s-qtRF_RxCAo0-unsplash.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/aldebaran-s-qtRF_RxCAo0-unsplash.jpg"
                },
                new() {
                    Title = "Звездные врата и сверхновая 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/aperture-vintage-Z6EpCdMcoUU-unsplash.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/aperture-vintage-Z6EpCdMcoUU-unsplash.jpg"
                },
                new() {
                    Title = "Галактика Андромеды 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/doug-walters-QQ9MzSs-o1I-unsplash.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/doug-walters-QQ9MzSs-o1I-unsplash.jpg"
                },
                new() {
                    Title = "Космическая пыль и созвездия 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/guillermo-ferla-Oze6U2m1oYU-unsplash.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/guillermo-ferla-Oze6U2m1oYU-unsplash.jpg"
                },
                new() {
                    Title = "Лагерь под звездным небом 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/alejagalesa-camp.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/alejagalesa-camp.jpg"
                },
                new() {
                    Title = "Спокойная ночь и Млечный Путь 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "PNG 4K",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/Electronic_Sample_96-calm-night.png",
                    SourceUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/Electronic_Sample_96-calm-night.png"
                },
                new() {
                    Title = "Планета на горизонте 4K",
                    Category = "Космос",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/aldebaran-s-uXchDIKs4qI-unsplash.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/aldebaran-s-uXchDIKs4qI-unsplash.jpg"
                },

                // 4. Киберпанк (8 обоев)
                new() {
                    Title = "Киберпанк Ночной Сити 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=3840&q=100"
                },
                new() {
                    Title = "Неоновый переулок Токио 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=3840&q=100"
                },
                new() {
                    Title = "Футуристический суперкар 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1511919884226-fd3cad34687c?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1511919884226-fd3cad34687c?w=3840&q=100"
                },
                new() {
                    Title = "Кибер-улица под дождем 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1514565131-fce0801e5785?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1514565131-fce0801e5785?w=3840&q=100"
                },
                new() {
                    Title = "Неоновый мегаполис сверху 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1519501025264-65ba15a82390?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1519501025264-65ba15a82390?w=3840&q=100"
                },
                new() {
                    Title = "Кибер-серверная стойка 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=3840&q=100"
                },
                new() {
                    Title = "Голографический интерфейс 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=3840&q=100"
                },
                new() {
                    Title = "Кибер-самурай в тумане 4K",
                    Category = "Киберпанк",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=3840&q=100"
                },

                // 5. Игры и Арт (8 обоев)
                new() {
                    Title = "Ретро планета Sci-Fi 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/VorgBardo-midjourney-1960s-sci-fi-planet.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/VorgBardo-midjourney-1960s-sci-fi-planet.jpg"
                },
                new() {
                    Title = "Цветущая сакура и горы 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/P82En-cherry-blossom-mountain-range.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/P82En-cherry-blossom-mountain-range.jpg"
                },
                new() {
                    Title = "Другой мир Sci-Fi 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/alejagalesa-another-world.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/alejagalesa-another-world.jpg"
                },
                new() {
                    Title = "Морская свадебная процессия 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/art/ARTWORK-bridal-procession-on-the-hardangerfjord.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/art/ARTWORK-bridal-procession-on-the-hardangerfjord.jpg"
                },
                new() {
                    Title = "Мир Кристины Классический Арт 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/art/ARTWORK-christianas-world.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/art/ARTWORK-christianas-world.jpg"
                },
                new() {
                    Title = "Миф о Гиласе и нимфах 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/art/ARTWORK-hylas-and-the-nymphs.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/art/ARTWORK-hylas-and-the-nymphs.jpg"
                },
                new() {
                    Title = "Геймерский сетап с подсветкой 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1616588589676-62b3bd4ff6d2?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1616588589676-62b3bd4ff6d2?w=3840&q=100"
                },
                new() {
                    Title = "Воин в золотых доспехах 4K",
                    Category = "Игры и Арт",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1579783902614-a3fb3927b675?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1579783902614-a3fb3927b675?w=3840&q=100"
                },

                // 6. Абстракция (8 обоев)
                new() {
                    Title = "Акриловые волны 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/abstract/acrylic-paint-1.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/abstract/acrylic-paint-1.jpg"
                },
                new() {
                    Title = "Жидкий неон 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/abstract/acrylic-paint-2.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/abstract/acrylic-paint-2.jpg"
                },
                new() {
                    Title = "Глубокий индиго 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/abstract/acrylic-paint-3.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/abstract/acrylic-paint-3.jpg"
                },
                new() {
                    Title = "Красный горизонт заката 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "PNG 4K",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/afreen-red-sunset-horizon.png",
                    SourceUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/afreen-red-sunset-horizon.png"
                },
                new() {
                    Title = "Спокойный день Минимализм 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/alejagalesa-calm-day.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/alejagalesa-calm-day.jpg"
                },
                new() {
                    Title = "Психоделический кристалл 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1541701494587-cb58502866ab?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1541701494587-cb58502866ab?w=3840&q=100"
                },
                new() {
                    Title = "Геометрическая призма 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1550684848-fac1c5b4e853?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1550684848-fac1c5b4e853?w=3840&q=100"
                },
                new() {
                    Title = "Золотые фрактальные волны 4K",
                    Category = "Абстракция",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=3840&q=100"
                },

                // 7. Природа и Пейзажи (8 обоев)
                new() {
                    Title = "Шотландские утесы Storr 4K",
                    Category = "Природа и Пейзажи",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/landscape/Old%20Man%20of%20Storr.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/landscape/Old%20Man%20of%20Storr.jpg"
                },
                new() {
                    Title = "Горный массив на закате 4K",
                    Category = "Природа и Пейзажи",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/OGARart-eagle-mountain-sunset-minimalist.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/OGARart-eagle-mountain-sunset-minimalist.jpg"
                },
                new() {
                    Title = "Всадник на закате 4K",
                    Category = "Природа и Пейзажи",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/alejagalesa-horse-in-the-sunset.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/DenverCoder1/minimalistic-wallpaper-collection/main/images/alejagalesa-horse-in-the-sunset.jpg"
                },
                new() {
                    Title = "Дельфины в лазурном океане 4K",
                    Category = "Природа и Пейзажи",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/animals/NOUNS-dolphins.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/animals/NOUNS-dolphins.jpg"
                },
                new() {
                    Title = "Стадо зебр на закате 4K",
                    Category = "Природа и Пейзажи",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/animals/Zebras.jpg",
                    SourceUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/animals/Zebras.jpg"
                },
                new() {
                    Title = "Северное сияние над фьордом 4K",
                    Category = "Природа и Пейзажи",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1517411032315-54ef2cb783bb?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1517411032315-54ef2cb783bb?w=3840&q=100"
                },
                new() {
                    Title = "Могучий водопад в ущелье 4K",
                    Category = "Природа и Пейзажи",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1432405972618-c60b0225b8f9?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1432405972618-c60b0225b8f9?w=3840&q=100"
                },
                new() {
                    Title = "Доломитовые Альпы на рассвете 4K",
                    Category = "Природа и Пейзажи",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "JPG Ultra-HD",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&q=80",
                    SourceUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=3840&q=100"
                },

                // 8. Живые обои (видеолупы 60 FPS)
                new() {
                    Title = "Кибер-луп: Неоновый туннель 4K",
                    Category = "Живые обои",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "MP4 60 FPS",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=800&q=80",
                    SourceUrl = "https://assets.mixkit.co/videos/preview/mixkit-tunnel-of-futuristic-neon-lights-seamless-loop-41566-large.mp4"
                },
                new() {
                    Title = "Кибер-луп: Квантовый реактор 4K",
                    Category = "Живые обои",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "MP4 60 FPS",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=800&q=80",
                    SourceUrl = "https://assets.mixkit.co/videos/preview/mixkit-digital-animation-of-screens-with-code-seamless-loop-31910-large.mp4"
                },
                new() {
                    Title = "Кибер-луп: Звездный гиперпрыжок 4K",
                    Category = "Живые обои",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "MP4 60 FPS",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/aperture-vintage-Z6EpCdMcoUU-unsplash.jpg",
                    SourceUrl = "https://assets.mixkit.co/videos/preview/mixkit-flying-through-a-starfield-in-space-seamless-loop-32986-large.mp4"
                },
                new() {
                    Title = "Кибер-луп: Неоновая сетка Synthwave 4K",
                    Category = "Живые обои",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "MP4 60 FPS",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&q=80",
                    SourceUrl = "https://assets.mixkit.co/videos/preview/mixkit-retro-futuristic-grid-tunnel-seamless-loop-41565-large.mp4"
                },
                new() {
                    Title = "Кибер-луп: Матричный водопад кода 4K",
                    Category = "Живые обои",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "MP4 60 FPS",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=800&q=80",
                    SourceUrl = "https://assets.mixkit.co/videos/preview/mixkit-matrix-style-binary-code-rain-loop-41562-large.mp4"
                },
                new() {
                    Title = "Кибер-луп: Галактический вихрь 4K",
                    Category = "Живые обои",
                    Resolution = "4K UHD (3840×2160)",
                    Format = "MP4 60 FPS",
                    AspectRatio = "16:9 Landscape",
                    PreviewUrl = "https://raw.githubusercontent.com/makccr/wallpapers/master/wallpapers/space/doug-walters-QQ9MzSs-o1I-unsplash.jpg",
                    SourceUrl = "https://assets.mixkit.co/videos/preview/mixkit-hypnotic-swirl-of-space-gas-and-stars-loop-41558-large.mp4"
                }
            };
        }

        public async Task<string> DownloadOrPrepareWallpaperAsync(string urlOrPath)
        {
            if (File.Exists(urlOrPath)) return urlOrPath;

            string ext = Path.GetExtension(urlOrPath.Split('?')[0]);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            string fileName = $"wp_{Math.Abs(urlOrPath.GetHashCode())}{ext}";
            string targetPath = Path.Combine(_wallpapersDir, fileName);

            if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 1024)
            {
                return targetPath;
            }

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(45);
            http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) StormOptimizer/3.0.2");
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
                        uint count = wallpaper.GetMonitorDevicePathCount();
                        if (count > 0)
                        {
                            for (uint i = 0; i < count; i++)
                            {
                                string monId = wallpaper.GetMonitorDevicePathAt(i);
                                wallpaper.SetWallpaper(monId, localPath);
                            }
                        }
                        else
                        {
                            wallpaper.SetWallpaper(null, localPath);
                        }
                    }
                    else
                    {
                        wallpaper.SetWallpaper(monitorDeviceId, localPath);
                    }
                    return true;
                }
                catch
                {
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

                try
                {
                    using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Personalization");
                    key?.SetValue("LockScreenImage", localPath, RegistryValueKind.String);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool SetNoLockScreen(bool disable)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Personalization");
                if (disable)
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

        public bool IsNoLockScreenEnabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Personalization");
                if (key?.GetValue("NoLockScreen") is int val && val == 1)
                {
                    return true;
                }
            }
            catch { }
            return false;
        }

        public bool SetLockScreenTipsDisabled(bool disable)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
                key?.SetValue("RotatingLockScreenOverlayEnabled", disable ? 0 : 1, RegistryValueKind.DWord);
                key?.SetValue("SubscribedContent-338387Enabled", disable ? 0 : 1, RegistryValueKind.DWord);
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
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
                if (key?.GetValue("RotatingLockScreenOverlayEnabled") is int val && val == 0)
                {
                    return true;
                }
            }
            catch { }
            return false;
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
            NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER, 0, null, NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);
        }
    }
}
