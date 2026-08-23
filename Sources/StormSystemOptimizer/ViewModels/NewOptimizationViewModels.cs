using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    // ==========================================
    // 1. EXPLORER TWEAKS VIEW MODEL
    // ==========================================
    public partial class ExplorerTweaksViewModel : ObservableObject
    {
        [ObservableProperty] private bool _fastThumbnails = true;
        [ObservableProperty] private bool _disableRecentFiles = true;
        [ObservableProperty] private bool _verboseStatus = true;
        [ObservableProperty] private bool _showFileExtensions = true;
        [ObservableProperty] private bool _showHiddenFiles = false;
        [ObservableProperty] private bool _useCompactMode = false;
        [ObservableProperty] private bool _disallowShaking = true;
        [ObservableProperty] private bool _disableSearchBing = true;
        [ObservableProperty] private bool _classicContextMenu = true;
        [ObservableProperty] private bool _launchToThisPC = true;
        [ObservableProperty] private bool _removeShortcutSuffix = true;
        [ObservableProperty] private string _statusMessage = "Готов к оптимизации Проводника";
        [ObservableProperty] private bool _isBusy = false;

        public ExplorerTweaksViewModel()
        {
            FastThumbnails = ExplorerTweaksService.Instance.IsThumbnailCacheFast();
            DisableRecentFiles = ExplorerTweaksService.Instance.IsRecentFilesDisabled();
            VerboseStatus = ExplorerTweaksService.Instance.IsVerboseStatusEnabled();
            ShowFileExtensions = ExplorerTweaksService.Instance.IsFileExtensionsShown();
            ShowHiddenFiles = ExplorerTweaksService.Instance.IsHiddenFilesShown();
            UseCompactMode = ExplorerTweaksService.Instance.IsCompactModeEnabled();
            DisallowShaking = ExplorerTweaksService.Instance.IsShakeToMinimizeDisabled();
        }

        [RelayCommand]
        public async Task ApplyTweaksAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Применение параметров Shell и Проводника...";

            await Task.Run(() =>
            {
                var s = ExplorerTweaksService.Instance;
                s.ApplyExplorerTweak("FastThumbnails", FastThumbnails);
                s.ApplyExplorerTweak("DisableRecentFiles", DisableRecentFiles);
                s.ApplyExplorerTweak("ShowFileExtensions", ShowFileExtensions);
                s.ApplyExplorerTweak("ShowHiddenFiles", ShowHiddenFiles);
                s.ApplyExplorerTweak("UseCompactMode", UseCompactMode);
                s.ApplyExplorerTweak("DisallowShaking", DisallowShaking);
                s.ApplyExplorerTweak("VerboseStatus", VerboseStatus);
                s.ApplyExplorerTweak("DisableSearchBing", DisableSearchBing);
                s.ApplyExplorerTweak("ClassicContextMenu", ClassicContextMenu);
                s.ApplyExplorerTweak("LaunchToThisPC", LaunchToThisPC);
                s.ApplyExplorerTweak("RemoveShortcutSuffix", RemoveShortcutSuffix);
            });

            StatusMessage = "Оптимизация Проводника успешно сохранена!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Проводник и Shell", "Все параметры Проводника успешно оптимизированы.");
        }

        [RelayCommand]
        public void RestartExplorer()
        {
            ExplorerTweaksService.Instance.RestartExplorer();
            StatusMessage = "Проводник (explorer.exe) успешно перезапущен!";
        }
    }

    // ==========================================
    // 2. BROWSER TURBO VIEW MODEL
    // ==========================================
    // ==========================================
    public partial class BrowserTurboViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<BrowserTabItem> _browserTabs = new();
        [ObservableProperty] private BrowserTabItem? _selectedBrowser;
        [ObservableProperty] private string _detectedCacheSize = "Сканирование...";
        [ObservableProperty] private bool _disableBackgroundMode = true;
        [ObservableProperty] private bool _enableGpuAcceleration = true;
        [ObservableProperty] private string _statusMessage = "Готов к очистке и тюнингу браузеров";
        [ObservableProperty] private bool _isBusy = false;

        public BrowserTurboViewModel()
        {
            _ = RefreshStatsAsync();
        }

        [RelayCommand]
        public async Task RefreshStatsAsync()
        {
            IsBusy = true;
            StatusMessage = "Сканирование установленных браузеров, профилей и кэшей...";
            
            var tabs = await Task.Run(() => BrowserTurboService.Instance.GetDetailedBrowserTabs());
            
            BrowserTabs.Clear();
            foreach (var t in tabs)
            {
                BrowserTabs.Add(t);
            }

            if (SelectedBrowser == null || !BrowserTabs.Any(x => x.Id == SelectedBrowser.Id))
            {
                SelectedBrowser = BrowserTabs.FirstOrDefault();
            }
            else
            {
                SelectedBrowser = BrowserTabs.FirstOrDefault(x => x.Id == SelectedBrowser.Id);
            }

            var all = BrowserTabs.FirstOrDefault(x => x.Id == "all");
            DetectedCacheSize = all?.CacheSizeFormatted ?? "0 Б";
            StatusMessage = $"Обнаружено {BrowserTabs.Count(x => x.IsInstalled && x.Id != "all")} браузеров в системе. Общий кэш: {DetectedCacheSize}";
            IsBusy = false;
        }

        [RelayCommand]
        public void SelectBrowser(BrowserTabItem browser)
        {
            if (browser != null)
            {
                SelectedBrowser = browser;
            }
        }

        [RelayCommand]
        public async Task CleanSelectedBrowserCacheAsync()
        {
            if (IsBusy || SelectedBrowser == null) return;
            IsBusy = true;
            StatusMessage = $"Очистка кэша для {SelectedBrowser.Name}...";

            int cleaned = await BrowserTurboService.Instance.CleanSpecificBrowserCacheAsync(SelectedBrowser);
            await RefreshStatsAsync();

            StatusMessage = $"Успешно очищено {cleaned} файлов кэша ({SelectedBrowser.Name})!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Очистка браузера 🧹", $"{SelectedBrowser.Name}: кэш и временные файлы успешно удалены!");
        }

        [RelayCommand]
        public async Task DefragSelectedBrowserSqliteAsync()
        {
            if (IsBusy || SelectedBrowser == null) return;
            IsBusy = true;
            StatusMessage = $"Дефрагментация и сжатие баз SQLite ({SelectedBrowser.Name})...";

            int defragged = await BrowserTurboService.Instance.DefragBrowserSqliteDatabasesAsync(SelectedBrowser);
            StatusMessage = $"Сжато и оптимизировано {defragged} баз данных истории и закладок!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("SQLite Оптимизация ⚡", $"{SelectedBrowser.Name}: базы данных успешно дефрагментированы!");
        }

        [RelayCommand]
        public async Task ApplySelectedBrowserTweaksAsync()
        {
            if (IsBusy || SelectedBrowser == null) return;
            IsBusy = true;
            StatusMessage = $"Применение настроек оптимизации ({SelectedBrowser.Name})...";

            bool ok = await BrowserTurboService.Instance.ApplyBrowserCustomPoliciesAsync(SelectedBrowser);
            if (ok)
            {
                StatusMessage = $"Политики и твики для {SelectedBrowser.Name} успешно применены!";
                TrayService.Instance.ShowNotification("Тюнинг браузера ⚡", $"{SelectedBrowser.Name}: индивидуальные твики успешно применены!");
            }
            else
            {
                StatusMessage = "Не удалось применить все твики (требуются права администратора).";
            }
            IsBusy = false;
        }

        [RelayCommand]
        public void LaunchWithTurboGpu()
        {
            if (SelectedBrowser == null) return;
            BrowserTurboService.Instance.LaunchBrowserWithTurboGpuFlags(SelectedBrowser);
            StatusMessage = $"Запущен {SelectedBrowser.Name} с аппаратным Zero-Copy и GPU-растеризацией!";
        }

        [RelayCommand]
        public async Task CleanCachesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Глобальная очистка кэша всех установленных браузеров...";

            int deleted = await BrowserTurboService.Instance.CleanBrowserCachesAsync();
            await RefreshStatsAsync();

            StatusMessage = $"Очищено {deleted} файлов кэша во всех браузерах!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Глобальная очистка браузеров 🧹", "Кэш шейдеров и временные данные всех браузеров удалены!");
        }

        [RelayCommand]
        public async Task ApplyBrowserPolicyAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Глобальное применение политик оптимизации для всех браузеров...";

            await Task.Run(() =>
            {
                BrowserTurboService.Instance.ApplyBrowserBackgroundExtensionTweak(DisableBackgroundMode);
            });

            var all = BrowserTabs.FirstOrDefault(x => x.Id == "all");
            if (all != null)
            {
                await BrowserTurboService.Instance.ApplyBrowserCustomPoliciesAsync(all);
            }

            StatusMessage = "Политики для всех браузеров успешно обновлены!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Тюнинг браузеров ⚡", "Фоновая активность отключена, аппаратное ускорение форсировано!");
        }
    }

    // ==========================================
    // 3. GAME LAUNCHERS VIEW MODEL
    // ==========================================
    // 3. GAME LAUNCHERS VIEW MODEL
    // ==========================================
    public partial class GameLaunchersViewModel : ObservableObject
    {
        [ObservableProperty] private bool _optimizeSteam = true;
        [ObservableProperty] private bool _optimizeDiscord = true;
        [ObservableProperty] private bool _disableXboxGameBar = true;
        [ObservableProperty] private bool _optimizeLaunchBox = true;
        [ObservableProperty] private string _statusMessage = "Готов к индивидуальному и общему управлению лаунчерами";
        [ObservableProperty] private bool _isBusy = false;
        
        public ObservableCollection<GameLauncherDetail> Launchers { get; } = new();
        public ObservableCollection<string> DetectedLaunchers { get; } = new();

        public GameLaunchersViewModel()
        {
            _ = DetectLaunchersAsync();
        }

        [RelayCommand]
        public async Task DetectLaunchersAsync()
        {
            IsBusy = true;
            StatusMessage = "Сканирование всех дисков на наличие игровых платформ...";

            var list = await Task.Run(() => GameLaunchersService.Instance.GetDetailedLaunchers());
            
            Launchers.Clear();
            DetectedLaunchers.Clear();
            
            foreach (var l in list)
            {
                Launchers.Add(l);
                DetectedLaunchers.Add(l.Name);
            }

            IsBusy = false;
            StatusMessage = $"Обнаружено {Launchers.Count} игровых платформ и медиатек";
        }

        [RelayCommand]
        public async Task CleanLauncherCacheAsync(GameLauncherDetail? launcher)
        {
            if (launcher == null) return;
            IsBusy = true;
            StatusMessage = $"Очистка кэша «{launcher.Name}»...";

            int deleted = await Task.Run(() => GameLaunchersService.Instance.CleanSpecificLauncherCache(launcher));
            
            // Refresh launcher card
            await DetectLaunchersAsync();

            StatusMessage = $"Очищено {deleted} файлов кэша для {launcher.Name}";
            TrayService.Instance.ShowNotification("Кэш очищен 🧹", $"Очищено {deleted} временных файлов для {launcher.Name}");
            IsBusy = false;
        }

        [RelayCommand]
        public async Task OptimizeLauncherAsync(GameLauncherDetail? launcher)
        {
            if (launcher == null) return;
            IsBusy = true;
            StatusMessage = $"Оптимизация параметров «{launcher.Name}»...";

            await Task.Run(() => GameLaunchersService.Instance.OptimizeSpecificLauncher(launcher));

            StatusMessage = $"Оптимизация {launcher.Name} успешно применена!";
            TrayService.Instance.ShowNotification("Лаунчер оптимизирован ⚡", $"Параметры {launcher.Name} настроены для максимальной производительности.");
            IsBusy = false;
        }

        [RelayCommand]
        public void OpenFolder(GameLauncherDetail? launcher)
        {
            if (launcher == null) return;
            GameLaunchersService.Instance.OpenLauncherFolder(launcher);
        }

        [RelayCommand]
        public void LaunchApp(GameLauncherDetail? launcher)
        {
            if (launcher == null) return;
            GameLaunchersService.Instance.LaunchGameLauncher(launcher);
        }

        [RelayCommand]
        public async Task KillLauncherAsync(GameLauncherDetail? launcher)
        {
            if (launcher == null) return;
            await Task.Run(() => GameLaunchersService.Instance.KillLauncherProcesses(launcher));
            await DetectLaunchersAsync();
            StatusMessage = $"Фоновые процессы {launcher.Name} выгружены из памяти";
        }

        [RelayCommand]
        public async Task KillAllRunningLaunchersAsync()
        {
            IsBusy = true;
            StatusMessage = "Выгрузка всех фоновых игровых клиентов перед игрой...";

            await Task.Run(() =>
            {
                var s = GameLaunchersService.Instance;
                var list = s.GetDetailedLaunchers();
                foreach (var l in list)
                {
                    if (l.IsRunning) s.KillLauncherProcesses(l);
                }
            });

            await DetectLaunchersAsync();
            StatusMessage = "Все фоновые лаунчеры выгружены, оперативная память освобождена!";
            TrayService.Instance.ShowNotification("Игровой режим 🚀", "Фоновые лаунчеры выгружены для максимального FPS.");
            IsBusy = false;
        }

        [RelayCommand]
        public async Task ApplyLauncherTweaksAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Снижение фоновой нагрузки лаунчеров...";

            await Task.Run(() =>
            {
                var s = GameLaunchersService.Instance;
                if (OptimizeSteam) s.OptimizeSteamSettings(true);
                if (OptimizeDiscord) s.OptimizeDiscordOverhead(true);
                if (DisableXboxGameBar) s.OptimizeXboxGameBar(true);
                
                var detailed = s.GetDetailedLaunchers();
                foreach (var l in detailed)
                {
                    s.OptimizeSpecificLauncher(l);
                }
            });

            StatusMessage = "Оверлеи и фоновые процессы всех лаунчеров оптимизированы!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Игровые лаунчеры", "Фоновая нагрузка Steam, Discord, LaunchBox и GameDVR снижена.");
        }

        [RelayCommand]
        public async Task CleanAllLauncherCachesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Очистка кэша веб-интерфейсов, медиа и шейдеров лаунчеров...";

            int deleted = await GameLaunchersService.Instance.CleanAllLauncherCachesAsync();
            await DetectLaunchersAsync();
            
            StatusMessage = $"Очищено {deleted} файлов кэша всех платформ!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Кэш лаунчеров", $"Очищено {deleted} кэшированных файлов игровых платформ.");
        }
    }

    // ==========================================
    // 4. DEFENDER TWEAKER VIEW MODEL
    // ==========================================
    public partial class DefenderTweakerViewModel : ObservableObject
    {
        [ObservableProperty] private int _defenderCpuLimit = 20;
        [ObservableProperty] private bool _disableTelemetry = true;
        [ObservableProperty] private string _customExclusionFolder = @"D:\Games";
        [ObservableProperty] private string _statusMessage = "Готов к настройке Защитника";
        [ObservableProperty] private bool _isBusy = false;
        public ObservableCollection<string> ActiveExclusions { get; } = new();

        public DefenderTweakerViewModel()
        {
            DefenderCpuLimit = DefenderTweakerService.Instance.GetDefenderCpuLimit();
            _ = LoadExclusionsAsync();
        }

        [RelayCommand]
        public async Task LoadExclusionsAsync()
        {
            var list = await Task.Run(() => DefenderTweakerService.Instance.GetActiveExclusions());
            ActiveExclusions.Clear();
            foreach (var item in list) ActiveExclusions.Add(item);
            if (ActiveExclusions.Count == 0) ActiveExclusions.Add("Исключения не добавлены");
        }

        [RelayCommand]
        public async Task ApplyDefenderSettingsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Применение параметров Защитника Windows...";

            await Task.Run(() =>
            {
                var s = DefenderTweakerService.Instance;
                s.SetDefenderCpuLimit(DefenderCpuLimit);
                s.DisableTelemetryAndSampleSubmission(DisableTelemetry);
            });

            StatusMessage = $"Лимит нагрузки CPU Защитника установлен на {DefenderCpuLimit}%!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Защитник Windows", $"Квота сканирования CPU ограничена до {DefenderCpuLimit}%.");
        }

        [RelayCommand]
        public async Task AddExclusionAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomExclusionFolder)) return;
            StatusMessage = $"Добавление «{CustomExclusionFolder}» в исключения...";

            await Task.Run(() =>
            {
                DefenderTweakerService.Instance.AddFolderExclusion(CustomExclusionFolder);
            });

            await LoadExclusionsAsync();
            StatusMessage = $"Папка «{CustomExclusionFolder}» добавлена в исключения!";
            TrayService.Instance.ShowNotification("Исключения сканирования", $"Папка {CustomExclusionFolder} исключена из проверок.");
        }

        [RelayCommand]
        public async Task AddAllDrivesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Массовое добавление всех дисков системы в исключения...";

            int count = await DefenderTweakerService.Instance.AddAllDrivesToExclusionsAsync();
            await LoadExclusionsAsync();

            StatusMessage = $"Добавлено {count} локальных разделов в исключения Защитника!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Массовые исключения", $"Все доступные разделы дисков ({count} шт.) добавлены в исключения.");
        }

        [RelayCommand]
        public async Task RemoveExclusionAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Contains("не добавлены")) return;
            await Task.Run(() => DefenderTweakerService.Instance.RemoveFolderExclusion(path));
            await LoadExclusionsAsync();
            StatusMessage = $"Исключение «{path}» удалено.";
        }
    }

    // ==========================================
    // 5. MEMORY MASTER VIEW MODEL
    // ==========================================
    public partial class MemoryMasterViewModel : ObservableObject
    {
        [ObservableProperty] private bool _enableLargeSystemCache = true;
        [ObservableProperty] private bool _optimizePools = true;
        [ObservableProperty] private bool _clearPagefileAtShutdown = false;
        [ObservableProperty] private string _currentPagefileSetting = "";
        [ObservableProperty] private string _pagefileDrive = "C";
        [ObservableProperty] private string _customPagefileInitialMbText = "4 096";
        [ObservableProperty] private string _customPagefileMaxMbText = "8 192";
        [ObservableProperty] private string _ramSummary = "Оперативная память в норме";
        [ObservableProperty] private string _statusMessage = "Готов к оптимизации памяти";
        [ObservableProperty] private bool _isBusy = false;

        public ObservableCollection<string> AvailableDrives { get; } = new();

        public MemoryMasterViewModel()
        {
            EnableLargeSystemCache = MemoryMasterService.Instance.IsLargeSystemCacheEnabled();
            CurrentPagefileSetting = MemoryMasterService.Instance.GetCurrentPagefileSetting();
            CustomPagefileInitialMbText = FormatHelper.FormatInt(4096);
            CustomPagefileMaxMbText = FormatHelper.FormatInt(8192);

            LoadDrives();
        }

        private void LoadDrives()
        {
            AvailableDrives.Clear();
            foreach (var d in MemoryMasterService.Instance.GetReadyDrives())
            {
                AvailableDrives.Add(d);
            }
        }

        private int ParseMb(string text, int fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            string clean = text.Replace(" ", "").Replace("\u00A0", "").Trim();
            return int.TryParse(clean, out int val) && val > 0 ? val : fallback;
        }

        [RelayCommand]
        public async Task FlushMemoryAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Мгновенная очистка Standby List и рабочих наборов...";

            await MemoryMasterService.Instance.FlushStandbyListAsync();
            MemoryMasterService.Instance.EmptyAllProcessesWorkingSet();

            StatusMessage = "Кэш памяти и Standby List успешно очищены!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Очистка памяти", "Standby List и неиспользуемые рабочие наборы сброшены.");
        }

        [RelayCommand]
        public async Task ApplyMemoryTweaksAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Применение системных параметров управления памятью...";

            await Task.Run(() =>
            {
                var s = MemoryMasterService.Instance;
                s.SetLargeSystemCache(EnableLargeSystemCache);
                s.SetClearPagefileOnShutdown(ClearPagefileAtShutdown);
                if (OptimizePools) s.OptimizeMemoryPools();
            });

            StatusMessage = "Параметры LargeSystemCache и пулов памяти сохранены!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Управление памятью", "Системный кэш и пулы памяти оптимизированы.");
        }

        [RelayCommand]
        public void ApplyCustomPagefile()
        {
            string cleanDrive = PagefileDrive.Split(' ')[0].Trim().TrimEnd(':');
            if (string.IsNullOrWhiteSpace(cleanDrive)) cleanDrive = "C";

            int initMb = ParseMb(CustomPagefileInitialMbText, 4096);
            int maxMb = ParseMb(CustomPagefileMaxMbText, 8192);
            CustomPagefileInitialMbText = FormatHelper.FormatInt(initMb);
            CustomPagefileMaxMbText = FormatHelper.FormatInt(maxMb);

            MemoryMasterService.Instance.SetCustomPagefile(cleanDrive, initMb, maxMb);
            CurrentPagefileSetting = MemoryMasterService.Instance.GetCurrentPagefileSetting();
            StatusMessage = $"Файл подкачки на диске {cleanDrive}: настроен ({FormatHelper.FormatInt(initMb)} - {FormatHelper.FormatInt(maxMb)} МБ)!";
            TrayService.Instance.ShowNotification("Файл подкачки", $"Размер файла подкачки зафиксирован на {FormatHelper.FormatInt(initMb)} - {FormatHelper.FormatInt(maxMb)} МБ.");
        }

        [RelayCommand]
        public void ApplySystemManagedPagefile()
        {
            string cleanDrive = PagefileDrive.Split(' ')[0].Trim().TrimEnd(':');
            if (string.IsNullOrWhiteSpace(cleanDrive)) cleanDrive = "C";

            MemoryMasterService.Instance.SetSystemManagedPagefile(cleanDrive);
            CurrentPagefileSetting = MemoryMasterService.Instance.GetCurrentPagefileSetting();
            StatusMessage = $"Файл подкачки на диске {cleanDrive}: переведен в режим автоматического выбора системы!";
            TrayService.Instance.ShowNotification("Файл подкачки", "Файл подкачки переведен в режим автоматического выбора Windows.");
        }

        [RelayCommand]
        public void SetPreset(string preset)
        {
            string cleanDrive = PagefileDrive.Split(' ')[0].Trim().TrimEnd(':');
            if (string.IsNullOrWhiteSpace(cleanDrive)) cleanDrive = "C";

            switch (preset.ToLowerInvariant())
            {
                case "gaming":
                    CustomPagefileInitialMbText = FormatHelper.FormatInt(4096);
                    CustomPagefileMaxMbText = FormatHelper.FormatInt(8192);
                    ApplyCustomPagefile();
                    break;
                case "fixed8gb":
                    CustomPagefileInitialMbText = FormatHelper.FormatInt(8192);
                    CustomPagefileMaxMbText = FormatHelper.FormatInt(8192);
                    ApplyCustomPagefile();
                    break;
                case "fixed16gb":
                    CustomPagefileInitialMbText = FormatHelper.FormatInt(16384);
                    CustomPagefileMaxMbText = FormatHelper.FormatInt(16384);
                    ApplyCustomPagefile();
                    break;
                case "auto":
                    ApplySystemManagedPagefile();
                    break;
                case "disable":
                    MemoryMasterService.Instance.DisablePagefile(cleanDrive);
                    CurrentPagefileSetting = MemoryMasterService.Instance.GetCurrentPagefileSetting();
                    StatusMessage = $"Файл подкачки на диске {cleanDrive}: отключен!";
                    TrayService.Instance.ShowNotification("Файл подкачки", "Файл подкачки отключен.");
                    break;
            }
        }
    }

    // ==========================================
    // 6. AUDIO LATENCY VIEW MODEL
    // ==========================================
    public partial class AudioLatencyViewModel : ObservableObject
    {
        [ObservableProperty] private bool _boostMmcss = true;
        [ObservableProperty] private int _audiodgCore = 2;
        [ObservableProperty] private string _statusMessage = "Готов к тюнингу задержек аудио";
        [ObservableProperty] private bool _isBusy = false;

        public AudioLatencyViewModel()
        {
            BoostMmcss = AudioLatencyService.Instance.IsMmcssAudioOptimized();
        }

        [RelayCommand]
        public async Task ApplyAudioTweaksAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Тюнинг MMCSS Pro Audio и приоритетов звукового тракта...";

            await Task.Run(() =>
            {
                AudioLatencyService.Instance.ApplyProAudioTweaks();
                AudioLatencyService.Instance.SetAudiodgAffinityAndPriority(AudiodgCore);
            });

            StatusMessage = $"Приоритет MMCSS Pro Audio установлен на High, audiodg.exe закреплен за ядром {AudiodgCore}!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Звуковой тракт", "Задержки аудио MMCSS снижены, треск и щелчки звука устранены.");
        }
    }

    // ==========================================
    // 7. USB POLLING VIEW MODEL
    // ==========================================
    public partial class UsbPollingViewModel : ObservableObject
    {
        [ObservableProperty] private bool _disableSelectiveSuspend = true;
        [ObservableProperty] private bool _disableHubPowerSavings = true;
        [ObservableProperty] private bool _enableXhciMsi = true;
        [ObservableProperty] private string _statusMessage = "Готов к оптимизации USB контроллеров";
        [ObservableProperty] private bool _isBusy = false;

        [RelayCommand]
        public async Task ApplyUsbTweaksAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Отключение энергосбережения USB портов и концентраторов...";

            await Task.Run(() =>
            {
                var s = UsbPollingService.Instance;
                if (DisableSelectiveSuspend) s.DisableUsbSelectiveSuspend();
                if (DisableHubPowerSavings) s.DisableUsbHubPowerSavings();
                if (EnableXhciMsi) s.EnableXhciMsiMode();
            });

            StatusMessage = "USB Selective Suspend отключен, xHCI переведен в MSI режим!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Питание USB", "Энергосбережение контроллеров USB отключено. Частота опроса мыши стабильна.");
        }
    }

    // ==========================================
    // 8. UPDATE COMPONENT VIEW MODEL
    // ==========================================
    public partial class UpdateComponentViewModel : ObservableObject
    {
        [ObservableProperty] private string _distCacheSize = "Сканирование...";
        [ObservableProperty] private bool _disableDeliveryP2P = true;
        [ObservableProperty] private string _statusMessage = "Готов к очистке хранилища обновлений";
        [ObservableProperty] private bool _isBusy = false;

        public UpdateComponentViewModel()
        {
            _ = RefreshStatsAsync();
        }

        [RelayCommand]
        public async Task RefreshStatsAsync()
        {
            var size = await Task.Run(() => UpdateComponentService.Instance.GetSoftwareDistributionSize());
            DistCacheSize = FormatHelper.FormatSize(size);
        }

        [RelayCommand]
        public async Task CleanCacheAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Очистка кэша загрузок SoftwareDistribution...";

            await UpdateComponentService.Instance.CleanSoftwareDistributionCacheAsync();
            await RefreshStatsAsync();

            StatusMessage = "Кэш загрузок Windows Update успешно очищен!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Очистка обновлений", "Папка кэша SoftwareDistribution\\Download очищена.");
        }

        [RelayCommand]
        public async Task RunDismCleanupAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Глубокая очистка WinSxS хранилища компонентов DISM (может занять 2-3 минуты)...";

            string res = await UpdateComponentService.Instance.RunDismStoreCleanupAsync();

            StatusMessage = res;
            IsBusy = false;
            TrayService.Instance.ShowNotification("DISM WinSxS", res);
        }

        [RelayCommand]
        public void PauseUpdatesUntil2099()
        {
            UpdateComponentService.Instance.PauseUpdatesUntilYear(2099);
            StatusMessage = "Обновления Windows приостановлены до 2099 года!";
            TrayService.Instance.ShowNotification("Windows Update", "Обновления Windows приостановлены до 2099 года.");
        }

        [RelayCommand]
        public void ResumeUpdatesNow()
        {
            UpdateComponentService.Instance.ResumeUpdates();
            StatusMessage = "Штатный режим обновлений Windows восстановлен!";
            TrayService.Instance.ShowNotification("Windows Update", "Штатный поиск обновлений возобновлен.");
        }
    }

    // ==========================================
    // 9. VISUAL PERFORMANCE VIEW MODEL
    // ==========================================
    public partial class VisualPerformanceViewModel : ObservableObject
    {
        [ObservableProperty] private bool _enableHags = true;
        [ObservableProperty] private bool _optimizeVisuals = true;
        [ObservableProperty] private bool _enableWindowedOpt = true;
        [ObservableProperty] private string _statusMessage = "Готов к тюнингу DWM и визуальных эффектов";
        [ObservableProperty] private bool _isBusy = false;

        public VisualPerformanceViewModel()
        {
            EnableHags = VisualPerformanceService.Instance.IsHagsEnabled();
        }

        [RelayCommand]
        public async Task ApplyVisualTweaksAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Настройка DWM, HAGS и анимаций интерфейса...";

            await Task.Run(() =>
            {
                var s = VisualPerformanceService.Instance;
                s.SetHags(EnableHags);
                s.OptimizeVisualEffects(OptimizeVisuals);
                s.SetWindowedGamingOptimization(EnableWindowedOpt);
            });

            StatusMessage = "Визуальные параметры, четкость шрифтов и HAGS применены!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("DWM и Визуализация", "Анимации окон оптимизированы, HAGS сконфигурирован.");
        }
    }

    // ==========================================
    // 10. BOOT PROFILER VIEW MODEL
    // ==========================================
    public partial class BootProfilerViewModel : ObservableObject
    {
        [ObservableProperty] private double _totalBootSec = 12.4;
        [ObservableProperty] private double _mainPathSec = 6.8;
        [ObservableProperty] private double _kernelPostSec = 5.6;
        [ObservableProperty] private string _lastBootDate = "Сегодня";
        [ObservableProperty] private string _rating = "Сверхбыстрый запуск системы ⚡";
        [ObservableProperty] private bool _reducedHiberfile = true;
        [ObservableProperty] private string _statusMessage = "Готов к анализу времени загрузки";
        [ObservableProperty] private bool _isBusy = false;

        public BootProfilerViewModel()
        {
            _ = LoadBootMetricsAsync();
        }

        [RelayCommand]
        public async Task LoadBootMetricsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Чтение журнала диагностики производительности Windows...";

            var metrics = await Task.Run(() => BootProfilerService.Instance.GetLastBootMetrics());
            TotalBootSec = metrics.TotalBootTimeSec;
            MainPathSec = metrics.MainPathBootTimeSec;
            KernelPostSec = metrics.KernelPostBootTimeSec;
            LastBootDate = metrics.LastBootDate;
            Rating = metrics.PerformanceRating;

            StatusMessage = $"Время загрузки: {TotalBootSec} сек (Ядро: {MainPathSec} сек, Рабочий стол: {KernelPostSec} сек)";
            IsBusy = false;
        }

        [RelayCommand]
        public void ApplyHiberfileOptimization()
        {
            BootProfilerService.Instance.SetReducedHiberfile(ReducedHiberfile);
            BootProfilerService.Instance.SetZeroStartupDelay(true);
            StatusMessage = ReducedHiberfile ? "Размер hiberfil.sys сжат (Reduced), освобождено до 16-32 ГБ диска!" : "Размер hiberfil.sys восстановлен на полный.";
            TrayService.Instance.ShowNotification("Файл гибернации", StatusMessage);
        }
    }
}
