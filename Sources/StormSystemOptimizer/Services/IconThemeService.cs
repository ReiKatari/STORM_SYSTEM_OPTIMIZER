using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public partial class IconThemeItem : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _author = "STORM SOFT";

        [ObservableProperty]
        private string _format = "ICO / PNG";

        [ObservableProperty]
        private string _category = "Киберпанк";

        [ObservableProperty]
        private string _previewUrl = string.Empty;

        [ObservableProperty]
        private int _iconCount = 240;

        [ObservableProperty]
        private double _rating = 4.9;

        [ObservableProperty]
        private bool _isApplied = false;
    }

    public partial class StormIconEntry : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _category = string.Empty;

        [ObservableProperty]
        private string _targetSystemName = string.Empty;

        [ObservableProperty]
        private string _geometryKey = "GeoApps";

        [ObservableProperty]
        private bool _isSelected = true;
    }

    public class IconThemeService
    {
        private static IconThemeService? _instance;
        public static IconThemeService Instance => _instance ??= new IconThemeService();

        private readonly string _iconsDir;

        private IconThemeService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _iconsDir = Path.Combine(appData, "StormSystemOptimizer", "IconThemes");
            if (!Directory.Exists(_iconsDir))
            {
                Directory.CreateDirectory(_iconsDir);
            }
        }

        public List<IconThemeItem> GetCuratedIconThemes()
        {
            return new List<IconThemeItem>
            {
                new() {
                    Title = "STORM Cyber Glow",
                    Description = "Фирменный неоновый пак STORM SOFT с неоновыми контурами и объемными 3D градиентами",
                    Author = "STORM SOFT",
                    Format = "7tsp / IconPackager",
                    Category = "STORM Dark",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 320,
                    Rating = 5.0,
                    IsApplied = true
                },
                new() {
                    Title = "Fluent Dark Minimal",
                    Description = "Современный строгий дизайн в стиле Windows 11 Fluent с матовыми темными акцентами",
                    Author = "Microsoft Fluent Team",
                    Format = "7tsp / ICO",
                    Category = "Минимализм",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 450,
                    Rating = 4.9
                },
                new() {
                    Title = "Lumicons Neomorphism 3D",
                    Description = "Объемные неоморфные значки с глубокими мягкими тенями и парящими элементами",
                    Author = "LumiStudio",
                    Format = "IconPackager (.ip)",
                    Category = "3D Объем",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 210,
                    Rating = 4.8
                },
                new() {
                    Title = "Imperial Gothic 40K",
                    Author = "TitanForge",
                    Description = "Готические золотые значки, пергаменты и аугментированные шестерни механикус",
                    Format = "iPack / 7tsp",
                    Category = "Игры и Арт",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 180,
                    Rating = 4.9
                },
                new() {
                    Title = "Retro Windows 98 Nostalgia",
                    Description = "Аутентичные пиксельные значки классической эры Windows 95 и 98 в высоком разрешении",
                    Author = "RetroForge",
                    Format = "ICO / PNG",
                    Category = "Ретро",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 160,
                    Rating = 4.7
                },
                new() {
                    Title = "MacOS Monterey Glass",
                    Description = "Стеклянные закругленные сквиркл-значки с кристальной прозрачностью",
                    Author = "Cupertino Designers",
                    Format = "IconPackager (.iconpack)",
                    Category = "Минимализм",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 380,
                    Rating = 4.9
                }
            };
        }

        public async Task<bool> RebuildIconCacheAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 1. Kill explorer
                    foreach (var p in Process.GetProcessesByName("explorer"))
                    {
                        try { p.Kill(); p.WaitForExit(1500); } catch { }
                    }

                    // 2. Delete IconCache.db in %LOCALAPPDATA%
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string legacyCache = Path.Combine(localAppData, "IconCache.db");
                    if (File.Exists(legacyCache))
                    {
                        try { File.Delete(legacyCache); } catch { }
                    }

                    // 3. Delete modern icon and thumb caches
                    string explorerCacheDir = Path.Combine(localAppData, "Microsoft", "Windows", "Explorer");
                    if (Directory.Exists(explorerCacheDir))
                    {
                        var files = Directory.GetFiles(explorerCacheDir, "iconcache*.db")
                            .Concat(Directory.GetFiles(explorerCacheDir, "thumbcache*.db"));

                        foreach (var f in files)
                        {
                            try { File.Delete(f); } catch { }
                        }
                    }

                    // 4. Run ie4uinit.exe -show
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "ie4uinit.exe",
                            Arguments = "-show",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var proc = Process.Start(psi);
                        proc?.WaitForExit(2000);
                    }
                    catch { }

                    // 5. Restart Explorer
                    Process.Start("explorer.exe");

                    // 6. Notify Shell
                    NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);

                    return true;
                }
                catch
                {
                    // Always ensure explorer is running
                    if (Process.GetProcessesByName("explorer").Length == 0)
                    {
                        try { Process.Start("explorer.exe"); } catch { }
                    }
                    return false;
                }
            });
        }

        public bool SetSystemIcon(string target, string iconPath)
        {
            try
            {
                if (!File.Exists(iconPath)) return false;

                switch (target)
                {
                    case "ThisPC":
                        SetClsidDefaultIcon(@"{20D04FE0-3AEA-1069-A2D8-08002B30309D}", iconPath);
                        break;
                    case "RecycleBinEmpty":
                        SetClsidDefaultIconValue(@"{645FF040-5081-101B-9F08-00AA002F954E}", "empty", iconPath);
                        break;
                    case "RecycleBinFull":
                        SetClsidDefaultIconValue(@"{645FF040-5081-101B-9F08-00AA002F954E}", "full", iconPath);
                        break;
                    case "UserFolder":
                        SetClsidDefaultIcon(@"{59031a47-0728-4441-b571-3115503794b1}", iconPath);
                        break;
                    case "Network":
                        SetClsidDefaultIcon(@"{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", iconPath);
                        break;
                    case "Folders":
                        SetShellIcon("3", iconPath);
                        break;
                    case "Drives":
                        SetShellIcon("9", iconPath);
                        break;
                }

                NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ResetSystemIconsToDefault()
        {
            try
            {
                // Delete Shell Icons subkey
                Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", false);

                // Reset CLSIDs
                ResetClsidDefaultIcon(@"{20D04FE0-3AEA-1069-A2D8-08002B30309D}");
                ResetClsidDefaultIcon(@"{645FF040-5081-101B-9F08-00AA002F954E}");
                ResetClsidDefaultIcon(@"{59031a47-0728-4441-b571-3115503794b1}");
                ResetClsidDefaultIcon(@"{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}");

                NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SetClsidDefaultIcon(string clsid, string iconPath)
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{clsid}\DefaultIcon");
            key?.SetValue("", $"{iconPath},0");
        }

        private static void SetClsidDefaultIconValue(string clsid, string valName, string iconPath)
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{clsid}\DefaultIcon");
            key?.SetValue(valName, $"{iconPath},0");
        }

        private static void ResetClsidDefaultIcon(string clsid)
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\CLSID\{clsid}\DefaultIcon", false);
            }
            catch { }
        }

        private static void SetShellIcon(string index, string iconPath)
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons");
            key?.SetValue(index, $"{iconPath},0");
        }

        public async Task<bool> InstallIconPackageArchiveAsync(string packageFilePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(packageFilePath)) return false;

                    string ext = Path.GetExtension(packageFilePath).ToLowerInvariant();
                    string themeName = Path.GetFileNameWithoutExtension(packageFilePath);
                    string destDir = Path.Combine(_iconsDir, themeName);

                    if (Directory.Exists(destDir))
                    {
                        Directory.Delete(destDir, true);
                    }
                    Directory.CreateDirectory(destDir);

                    if (ext == ".zip" || ext == ".iconpack" || ext == ".ip" || ext == ".7tsp")
                    {
                        try
                        {
                            ZipFile.ExtractToDirectory(packageFilePath, destDir, true);
                        }
                        catch
                        {
                            // If zip fails (e.g. 7z format), try copying raw file
                            File.Copy(packageFilePath, Path.Combine(destDir, Path.GetFileName(packageFilePath)), true);
                        }
                    }
                    else if (ext == ".ico" || ext == ".png")
                    {
                        File.Copy(packageFilePath, Path.Combine(destDir, Path.GetFileName(packageFilePath)), true);
                    }

                    // Look for icons inside extracted tree and map best matches
                    var icoFiles = Directory.GetFiles(destDir, "*.ico", SearchOption.AllDirectories);
                    if (icoFiles.Length > 0)
                    {
                        // Match folder, drive, recycle, pc
                        var folderIco = icoFiles.FirstOrDefault(f => f.Contains("folder", StringComparison.OrdinalIgnoreCase)) ?? icoFiles[0];
                        var pcIco = icoFiles.FirstOrDefault(f => f.Contains("computer", StringComparison.OrdinalIgnoreCase) || f.Contains("thispc", StringComparison.OrdinalIgnoreCase)) ?? icoFiles[0];
                        var trashIco = icoFiles.FirstOrDefault(f => f.Contains("trash", StringComparison.OrdinalIgnoreCase) || f.Contains("recycle", StringComparison.OrdinalIgnoreCase)) ?? icoFiles[0];

                        SetSystemIcon("Folders", folderIco);
                        SetSystemIcon("ThisPC", pcIco);
                        SetSystemIcon("RecycleBinEmpty", trashIco);
                        SetSystemIcon("RecycleBinFull", trashIco);
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public List<StormIconEntry> GetStormCyberGlowCatalog()
        {
            var list = new List<StormIconEntry>(320);

            // 1. Система (40)
            string[] sysIcons = {
                "Этот компьютер", "Корзина (пустая)", "Корзина (полная)", "Папка пользователя", "Сеть",
                "Панель управления", "Параметры Windows", "Диспетчер задач", "Службы Windows", "Редактор реестра",
                "Командная строка", "PowerShell", "Терминал Windows", "Защитник Windows", "Центр обновления",
                "Брандмауэр Windows", "Управление дисками", "Диспетчер устройств", "Сведения о системе", "Планировщик заданий",
                "Монитор ресурсов", "Управление компьютером", "Групповые политики", "Очистка диска", "Дефрагментация",
                "Восстановление системы", "Электропитание", "Параметры звука", "Параметры экрана", "Bluetooth устройства",
                "Wi-Fi адаптер", "Сетевые подключения", "Шрифты системы", "Мышь и сенсор", "Клавиатура",
                "Регион и язык", "Дата и время", "Учетные записи", "Автозагрузка", "Буфер обмена"
            };
            string[] sysGeos = {
                "GeoMonitor", "GeoClean", "GeoClean", "GeoExplorer", "GeoNetwork",
                "GeoSettings", "GeoSettings", "GeoTask", "GeoServices", "GeoKey",
                "GeoTerminal", "GeoTerminal", "GeoTerminal", "GeoDefender", "GeoUpdate",
                "GeoFirewall", "GeoDisks", "GeoDevice", "GeoSystemInfo", "GeoTimer",
                "GeoBenchmarks", "GeoSystemTools", "GeoShield", "GeoClean", "GeoDisks",
                "GeoShield", "GeoPower", "GeoAudio", "GeoMonitor", "GeoUsb",
                "GeoNetwork", "GeoNetwork", "GeoComponent", "GeoDevice", "GeoDevice",
                "GeoSettings", "GeoTimer", "GeoShield", "GeoStartup", "GeoCopy"
            };
            for (int i = 0; i < sysIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = sysIcons[i],
                    Category = "Система",
                    GeometryKey = sysGeos[i % sysGeos.Length],
                    TargetSystemName = $"Sys_{i+1}",
                    IsSelected = true
                });
            }

            // 2. Папки и Диски (40)
            string[] folderIcons = {
                "Системная папка", "Рабочий стол", "Загрузки", "Документы", "Музыка",
                "Видео", "Изображения", "Облачное хранилище", "Локальный диск C:", "Локальный диск D:",
                "Локальный диск E:", "Локальный диск F:", "SSD накопитель", "NVMe M.2 накопитель", "USB флеш-накопитель",
                "Внешний жесткий диск", "Сетевой диск", "CD/DVD привод", "Виртуальный RAM диск", "Зашифрованный том",
                "Архивная папка", "Общая сетевая папка", "Избранное", "Недавние папки", "Временные файлы",
                "Системная папка Windows", "Папка Program Files", "Папка ProgramData", "Папка AppData", "Корзина диска",
                "Резервные копии", "Папка проектов", "Папка скриптов", "Папка логов", "Папка кэша",
                "Скрытая папка", "Защищенная папка", "Медиатека", "Фотоальбом", "Папка шаблонов"
            };
            string[] folderGeos = {
                "GeoExplorer", "GeoMonitor", "GeoAppUpdate", "GeoLog", "GeoAudio",
                "GeoVisual", "GeoBrush", "GeoNetwork", "GeoDisks", "GeoDisks",
                "GeoDisks", "GeoDisks", "GeoDisks", "GeoLightning", "GeoUsb",
                "GeoDisks", "GeoNetwork", "GeoDisks", "GeoRam", "GeoLock",
                "GeoExplorer", "GeoNetwork", "GeoStar", "GeoTimer", "GeoClean",
                "GeoExplorer", "GeoApps", "GeoExplorer", "GeoExplorer", "GeoClean",
                "GeoShield", "GeoTerminal", "GeoTerminal", "GeoLog", "GeoClean",
                "GeoEyeOff", "GeoFolderLock", "GeoVisual", "GeoBrush", "GeoComponent"
            };
            for (int i = 0; i < folderIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = folderIcons[i],
                    Category = "Папки и Диски",
                    GeometryKey = folderGeos[i % folderGeos.Length],
                    TargetSystemName = $"FolderDisk_{i+1}",
                    IsSelected = true
                });
            }

            // 3. Браузеры (40)
            string[] browserIcons = {
                "Google Chrome", "Mozilla Firefox", "Microsoft Edge", "Opera GX", "Brave Browser",
                "Vivaldi", "Tor Browser", "Yandex Browser", "Chromium", "Safari",
                "Waterfox", "LibreWolf", "DuckDuckGo", "Pale Moon", "Midori",
                "Arc Browser", "Maxthon", "Sidekick", "Zen Browser", "Thorium",
                "Floorp", "Baidu Browser", "SeaMonkey", "Sleipnir", "Iridium",
                "SRWare Iron", "Ungoogled Chromium", "Falkon", "Otter Browser", "NetSurf",
                "Avast Secure Browser", "CCleaner Browser", "Epic Privacy Browser", "Ghostery Dawn", "Mullvad Browser",
                "Min Browser", "Konqueror", "qutebrowser", "Nyxt Browser", "Lynx Browser"
            };
            for (int i = 0; i < browserIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = browserIcons[i],
                    Category = "Браузеры",
                    GeometryKey = "GeoBrowser",
                    TargetSystemName = $"Browser_{i+1}",
                    IsSelected = true
                });
            }

            // 4. Игры (40)
            string[] gameIcons = {
                "Steam", "Epic Games Store", "Battle.net", "GOG Galaxy", "EA App",
                "Ubisoft Connect", "Xbox App", "Discord", "RetroArch", "Citron Switch",
                "Eden Switch", "Yuzu Emulator", "Ryujinx", "MelonDS", "DeSmuME",
                "PCSX2", "RPCS3", "PPSSPP", "Dolphin Emulator", "DuckStation",
                "Cemu Wii U", "VBA-M", "MAME Arcade", "Flycast", "Xenia Xbox 360",
                "Vita3K", "shadPS4", "Minecraft", "Counter-Strike 2", "Dota 2",
                "Cyberpunk 2077", "The Witcher 3", "GTA V", "Valorant", "Genshin Impact",
                "Roblox", "Apex Legends", "Call of Duty", "Overwatch 2", "Warhammer 40K Space Marine"
            };
            for (int i = 0; i < gameIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = gameIcons[i],
                    Category = "Игры",
                    GeometryKey = (i % 2 == 0) ? "GeoGamepad" : "GeoGame",
                    TargetSystemName = $"Game_{i+1}",
                    IsSelected = true
                });
            }

            // 5. Разработка (40)
            string[] devIcons = {
                "Visual Studio 2022", "Visual Studio Code", "Git", "GitHub Desktop", "GitLab",
                "Docker Desktop", "JetBrains Rider", "PyCharm", "IntelliJ IDEA", "WebStorm",
                "CLion", "Android Studio", "Unity Editor", "Unreal Engine 5", "Godot Engine",
                "Node.js", "Python Runtime", ".NET SDK", "Rust Cargo", "Go Language",
                "C++ Toolchain", "Postman", "Insomnia", "DBeaver", "Navicat",
                "HeidiSQL", "SQLite Studio", "Wireshark", "Fiddler", "Sublime Text",
                "Notepad++ Dev", "Neovim", "Vim", "Emacs", "Windows Terminal Dev",
                "Kubernetes", "Redis Desktop", "RabbitMQ", "Kafka Manager", "Nginx Server"
            };
            for (int i = 0; i < devIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = devIcons[i],
                    Category = "Разработка",
                    GeometryKey = (i % 3 == 0) ? "GeoTerminal" : ((i % 3 == 1) ? "GeoCpu" : "GeoComponent"),
                    TargetSystemName = $"Dev_{i+1}",
                    IsSelected = true
                });
            }

            // 6. Мультимедиа (40)
            string[] mediaIcons = {
                "Spotify", "Yandex Music", "VLC Media Player", "AIMP Player", "MPC-HC",
                "PotPlayer", "OBS Studio", "Streamlabs", "Audacity", "FL Studio",
                "Ableton Live", "Adobe Photoshop", "Adobe Illustrator", "Adobe Premiere Pro", "Adobe After Effects",
                "Blender 3D", "DaVinci Resolve", "Foobar2000", "Winamp Modern", "CorelDRAW",
                "Paint.NET", "GIMP", "Krita", "Inkscape", "Cinema 4D",
                "Maya", "3ds Max", "ZBrush", "Substance Painter", "Reaper DAW",
                "Cubase", "HandBrake", "Format Factory", "FFmpeg CLI", "MusicBee",
                "Lightroom", "Vegas Pro", "Camtasia", "Shotcut", "Kdenlive"
            };
            for (int i = 0; i < mediaIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = mediaIcons[i],
                    Category = "Мультимедиа",
                    GeometryKey = (i % 3 == 0) ? "GeoAudio" : ((i % 3 == 1) ? "GeoVisual" : "GeoBrush"),
                    TargetSystemName = $"Media_{i+1}",
                    IsSelected = true
                });
            }

            // 7. Утилиты (40)
            string[] utilIcons = {
                "STORM SYSTEM OPTIMIZER", "STORM GAME SYSTEM", "STORM INSTALLER", "7-Zip Archiver", "WinRAR",
                "Process Hacker", "HWMonitor", "CPU-Z", "GPU-Z", "MSI Afterburner",
                "CrystalDiskInfo", "CrystalDiskMark", "Rufus", "BleachBit", "Everything Search",
                "Notepad++", "HWiNFO64", "Autoruns Sysinternals", "Process Explorer", "TCPView",
                "TreeSize Free", "SpaceSniffer", "Revo Uninstaller", "Geek Uninstaller", "AIDA64 Extreme",
                "FurMark", "OCCT", "Prime95", "MemTest86", "Victoria HDD",
                "QuickCPU", "CapFrameX", "RTSS Rivatuner", "Bulk Rename Utility", "FastStone Capture",
                "ShareX", "KeePassXC", "Bitwarden", "AnyDesk", "TeamViewer"
            };
            for (int i = 0; i < utilIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = utilIcons[i],
                    Category = "Утилиты",
                    GeometryKey = (i % 4 == 0) ? "GeoDashboard" : ((i % 4 == 1) ? "GeoSystemTools" : ((i % 4 == 2) ? "GeoSpeedTest" : "GeoScanner")),
                    TargetSystemName = $"Util_{i+1}",
                    IsSelected = true
                });
            }

            // 8. Типы файлов (40)
            string[] fileTypeIcons = {
                "Исполняемый файл (.exe)", "Библиотека (.dll)", "Архив ZIP (.zip)", "Архив RAR (.rar)", "Архив 7-Zip (.7z)",
                "Образ диска (.iso)", "Документ PDF (.pdf)", "Документ Word (.docx)", "Таблица Excel (.xlsx)", "Презентация (.pptx)",
                "Текстовый файл (.txt)", "Файл JSON (.json)", "Файл XML (.xml)", "Аудио MP3 (.mp3)", "Аудио FLAC (.flac)",
                "Аудио WAV (.wav)", "Видео MP4 (.mp4)", "Видео MKV (.mkv)", "Изображение PNG (.png)", "Изображение JPG (.jpg)",
                "Иконка (.ico)", "Вектор SVG (.svg)", "Исходный код C# (.cs)", "Исходный код C++ (.cpp)", "Заголовок C++ (.h)",
                "Исходный код Python (.py)", "Скрипт JavaScript (.js)", "Скрипт TypeScript (.ts)", "Стиль CSS (.css)", "Страница HTML (.html)",
                "Разметка Markdown (.md)", "База данных SQLite (.db)", "Скрипт SQL (.sql)", "Конфигурация YAML (.yaml)", "Конфигурация TOML (.toml)",
                "Пакетный файл (.bat)", "Скрипт PowerShell (.ps1)", "Файл реестра (.reg)", "Файл шрифта (.ttf)", "Файл шрифта (.otf)"
            };
            for (int i = 0; i < fileTypeIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = fileTypeIcons[i],
                    Category = "Типы файлов",
                    GeometryKey = (i % 3 == 0) ? "GeoLog" : ((i % 3 == 1) ? "GeoKey" : "GeoComponent"),
                    TargetSystemName = $"File_{i+1}",
                    IsSelected = true
                });
            }

            return list;
        }

        public async Task<bool> ApplySelectedCyberGlowIconsAsync(IEnumerable<StormIconEntry> selectedIcons)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string baseAppIcon = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AppIcon.ico");
                    if (!File.Exists(baseAppIcon))
                    {
                        baseAppIcon = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppIcon.ico");
                    }

                    foreach (var icon in selectedIcons)
                    {
                        if (icon.Name == "Этот компьютер") SetSystemIcon("ThisPC", baseAppIcon);
                        else if (icon.Name == "Корзина (пустая)") SetSystemIcon("RecycleBinEmpty", baseAppIcon);
                        else if (icon.Name == "Корзина (полная)") SetSystemIcon("RecycleBinFull", baseAppIcon);
                        else if (icon.Name == "Папка пользователя") SetSystemIcon("UserFolder", baseAppIcon);
                        else if (icon.Name == "Сеть") SetSystemIcon("Network", baseAppIcon);
                        else if (icon.Name == "Системная папка") SetSystemIcon("Folders", baseAppIcon);
                        else if (icon.Name.StartsWith("Локальный диск")) SetSystemIcon("Drives", baseAppIcon);
                    }

                    NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
