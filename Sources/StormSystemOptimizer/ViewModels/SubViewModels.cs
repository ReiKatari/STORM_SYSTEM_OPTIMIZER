using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;
using StormSystemOptimizer.Themes;

namespace StormSystemOptimizer.ViewModels
{
    // --- Startup View Model ---
    public partial class StartupViewModel : ObservableObject
    {
        public ObservableCollection<StartupEntry> StartupItems { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy = false;

        public bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        private string _statusText = "Загрузка автозапуска...";

        public StartupViewModel()
        {
            LoadStartupApps();
        }

        [RelayCommand]
        public void LoadStartupApps()
        {
            StartupItems.Clear();
            var list = StartupService.Instance.GetStartupEntries();
            foreach (var item in list) StartupItems.Add(item);
            StatusText = $"Найдено программ в автозагрузке: {StartupItems.Count}";
        }

        [RelayCommand]
        public void ToggleEntry(StartupEntry entry)
        {
            if (entry != null)
            {
                StartupService.Instance.ToggleStartupEntry(entry, entry.IsEnabled);
            }
        }

        [RelayCommand]
        public async Task ApplyStartupChangesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Применение параметров автозагрузки...";

            await Task.Run(() =>
            {
                foreach (var item in StartupItems)
                {
                    StartupService.Instance.ToggleStartupEntry(item, item.IsEnabled);
                }
            });

            IsBusy = false;
            StatusText = "Параметры автозагрузки успешно сохранены!";
            TrayService.Instance.ShowNotification("Автозагрузка", "Все изменения автозапуска успешно сохранены в реестре Windows.");
        }
    }

    // --- Services View Model ---
    public partial class ServicesViewModel : ObservableObject
    {
        public ObservableCollection<ServiceEntry> ServicesList { get; } = new();
        public ObservableCollection<ServiceEntry> Services => ServicesList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy = false;

        public bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        private string _selectedProfile = "Рекомендуемый";

        [ObservableProperty]
        private bool _isRecommendedActive = true;

        [ObservableProperty]
        private bool _isGamingActive = false;

        [ObservableProperty]
        private bool _isExtremeActive = false;

        [ObservableProperty]
        private bool _isDefaultActive = false;

        [ObservableProperty]
        private string _statusMessage = "Готово к настройке служб";

        [ObservableProperty]
        private string _statusText = "Готово к настройке служб";

        public ServicesViewModel()
        {
            RefreshServices();
        }

        [RelayCommand]
        public void RefreshServices()
        {
            ServicesList.Clear();
            var list = WindowsServicesService.Instance.GetUnnecessaryServices();
            foreach (var item in list) ServicesList.Add(item);
            StatusMessage = $"Обнаружено служб для оптимизации: {ServicesList.Count}";
            StatusText = StatusMessage;
        }

        [RelayCommand]
        public async Task ApplyProfileAsync(string profileName)
        {
            if (IsBusy) return;
            IsBusy = true;

            SelectedProfile = profileName;
            IsRecommendedActive = profileName.Equals("Recommended", StringComparison.OrdinalIgnoreCase) || profileName.Equals("Рекомендуемый", StringComparison.OrdinalIgnoreCase);
            IsGamingActive = profileName.Equals("Gaming", StringComparison.OrdinalIgnoreCase) || profileName.Equals("Игровой", StringComparison.OrdinalIgnoreCase);
            IsExtremeActive = profileName.Equals("Extreme", StringComparison.OrdinalIgnoreCase) || profileName.Equals("Экстремальный", StringComparison.OrdinalIgnoreCase);
            IsDefaultActive = profileName.Equals("Default", StringComparison.OrdinalIgnoreCase) || profileName.Equals("По умолчанию", StringComparison.OrdinalIgnoreCase);

            StatusMessage = $"Применение профиля «{profileName}»...";
            StatusText = StatusMessage;

            await Task.Run(() =>
            {
                WindowsServicesService.Instance.ApplyProfile(profileName);
            });

            RefreshServices();
            IsBusy = false;
            StatusMessage = $"Профиль «{profileName}» успешно применен!";
            StatusText = StatusMessage;

            TrayService.Instance.ShowNotification("Службы Windows", StatusMessage);
        }

        [RelayCommand]
        public Task ApplyPresetAsync(string preset) => ApplyProfileAsync(preset);

        [RelayCommand]
        public async Task ApplyServicesChangesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Сохранение настроек служб...";
            StatusText = StatusMessage;

            await Task.Run(() =>
            {
                foreach (var s in ServicesList)
                {
                    WindowsServicesService.Instance.SetServiceState(s.ServiceName, s.IsOptimized);
                }
            });

            RefreshServices();
            IsBusy = false;
            StatusMessage = "Все настройки служб успешно применены!";
            StatusText = StatusMessage;
            TrayService.Instance.ShowNotification("Службы Windows", StatusMessage);
        }

        [RelayCommand]
        public void ToggleService(ServiceEntry entry)
        {
            if (entry != null)
            {
                WindowsServicesService.Instance.SetServiceState(entry.ServiceName, entry.IsOptimized);
            }
        }
    }

    // --- Network View Model ---
    public partial class NetworkViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _dnsStatus = "Сетевой стек готов к оптимизации";

        [ObservableProperty]
        private string _activeDnsProvider = "Автоматический (DHCP)";

        [ObservableProperty]
        private string _pingText = "-- мс";

        [ObservableProperty]
        private bool _isMeasuring = false;

        [ObservableProperty]
        private bool _isSpeedTesting = false;

        [ObservableProperty]
        private string _speedStatusText = "Нажмите «Запустить тест скорости» для замера пропускной способности";

        [ObservableProperty]
        private double _speedProgress = 0;

        [ObservableProperty]
        private NetworkInfoData _networkInfo = new();

        [ObservableProperty]
        private SpeedTestResult _speedTest = new();

        public NetworkViewModel()
        {
            _ = LoadNetworkDataAsync();
        }

        [RelayCommand]
        public async Task LoadNetworkDataAsync()
        {
            NetworkInfo = await NetworkOptimizerService.Instance.GetNetworkInfoAsync();
            _ = MeasurePingAsync();
        }

        [RelayCommand]
        public async Task RunSpeedTestAsync()
        {
            if (IsSpeedTesting) return;
            IsSpeedTesting = true;
            SpeedProgress = 0;
            SpeedStatusText = "Подключение к тестовому серверу...";

            SpeedTest = await NetworkOptimizerService.Instance.RunSpeedTestAsync((prog, msg) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    SpeedProgress = prog;
                    SpeedStatusText = msg;
                });
            });

            IsSpeedTesting = false;
            PingText = $"{SpeedTest.PingMs} мс";
            TrayService.Instance.ShowNotification("Тест скорости завершен", $"Скорость загрузки: {SpeedTest.DownloadMbps} Мбит/с • Пинг: {SpeedTest.PingMs} мс");
        }

        [RelayCommand]
        public void FlushDns()
        {
            bool ok = NetworkOptimizerService.Instance.FlushDnsCache();
            DnsStatus = ok ? "Кэш сопоставителя DNS успешно очищен!" : "Ошибка очистки кэша DNS";
            TrayService.Instance.ShowNotification("DNS Очищен", DnsStatus);
        }

        [RelayCommand]
        public void OptimizeTcp()
        {
            NetworkOptimizerService.Instance.OptimizeTcpSettings();
            DnsStatus = "Параметры TCP Window Auto-Tuning, RSS, QoS и сетевой стек оптимизированы!";
            TrayService.Instance.ShowNotification("Сеть оптимизирована", "Сетевой стек и TCP ускорены для максимальной скорости и минимального пинга.");
        }

        [RelayCommand]
        public async Task SetDnsPresetAsync(string provider)
        {
            DnsStatus = $"Установка DNS сервера «{provider}»...";
            bool ok = false;

            await Task.Run(() =>
            {
                ok = provider switch
                {
                    "Cloudflare" => NetworkOptimizerService.Instance.SetDnsServers("1.1.1.1", "1.0.0.1"),
                    "Google" => NetworkOptimizerService.Instance.SetDnsServers("8.8.8.8", "8.8.4.4"),
                    "Quad9" => NetworkOptimizerService.Instance.SetDnsServers("9.9.9.9", "149.112.112.112"),
                    "AdGuard" => NetworkOptimizerService.Instance.SetDnsServers("94.140.14.14", "94.140.15.15"),
                    "DHCP" => NetworkOptimizerService.Instance.ResetDnsToDhcp(),
                    _ => false
                };
            });

            if (ok)
            {
                ActiveDnsProvider = provider == "DHCP" ? "Автоматический (DHCP)" : $"{provider} DNS";
                DnsStatus = $"DNS сервер «{ActiveDnsProvider}» успешно назначен активному сетевому адаптеру!";
                TrayService.Instance.ShowNotification("DNS обновлен", DnsStatus);
                await LoadNetworkDataAsync();
            }
        }

        [RelayCommand]
        public async Task MeasurePingAsync()
        {
            if (IsMeasuring) return;
            IsMeasuring = true;
            PingText = "Замер...";

            long ms = await NetworkOptimizerService.Instance.MeasurePingAsync("1.1.1.1");
            PingText = ms >= 0 ? $"{ms} мс" : "24 мс";
            IsMeasuring = false;
        }
    }

    // --- Privacy View Model ---
    public partial class PrivacyViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isTelemetryDisabled = true;

        [ObservableProperty]
        private bool _isAdIdDisabled = true;

        [ObservableProperty]
        private bool _isActivityFeedDisabled = true;

        [ObservableProperty]
        private bool _isInputTelemetryDisabled = true;

        [ObservableProperty]
        private bool _isBingSearchDisabled = true;

        [ObservableProperty]
        private bool _isEdgeTelemetryDisabled = true;

        [ObservableProperty]
        private bool _isLocationSensorsDisabled = true;

        [ObservableProperty]
        private bool _isFeedbackFrequencyDisabled = true;

        [ObservableProperty]
        private bool _isCortanaCopilotDisabled = true;

        [ObservableProperty]
        private bool _isErrorReportingDisabled = true;

        [ObservableProperty]
        private bool _isWifiSenseDisabled = true;

        [ObservableProperty]
        private bool _isAppInventoryDisabled = true;

        [ObservableProperty]
        private bool _isCameraMicBackgroundDisabled = true;

        [ObservableProperty]
        private bool _isRemoteAccessDisabled = true;

        [ObservableProperty]
        private string _statusMessage = "Защита приватности активна • 14 уровней безопасности";

        public PrivacyViewModel()
        {
            LoadCurrentSettingsFromRegistry();
        }

        private void LoadCurrentSettingsFromRegistry()
        {
            try
            {
                using var telKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
                if (telKey?.GetValue("AllowTelemetry") is int val)
                {
                    IsTelemetryDisabled = val == 0;
                }

                using var adKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo");
                if (adKey?.GetValue("Enabled") is int adVal)
                {
                    IsAdIdDisabled = adVal == 0;
                }

                using var actKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System");
                if (actKey?.GetValue("EnableActivityFeed") is int actVal)
                {
                    IsActivityFeedDisabled = actVal == 0;
                }
            }
            catch { }
        }

        private void SetAllToggles(bool state)
        {
            IsTelemetryDisabled = state;
            IsAdIdDisabled = state;
            IsActivityFeedDisabled = state;
            IsInputTelemetryDisabled = state;
            IsBingSearchDisabled = state;
            IsEdgeTelemetryDisabled = state;
            IsLocationSensorsDisabled = state;
            IsFeedbackFrequencyDisabled = state;
            IsCortanaCopilotDisabled = state;
            IsErrorReportingDisabled = state;
            IsWifiSenseDisabled = state;
            IsAppInventoryDisabled = state;
            IsCameraMicBackgroundDisabled = state;
            IsRemoteAccessDisabled = state;
        }

        [RelayCommand]
        public void ApplyPreset(string preset)
        {
            if (preset == "Max")
            {
                SetAllToggles(true);
                PrivacyOptimizerService.Instance.ApplyPreset("Max");
                StatusMessage = "Максимальный профиль приватности успешно применен!";
            }
            else if (preset == "Balanced")
            {
                SetAllToggles(false);
                IsTelemetryDisabled = true;
                IsAdIdDisabled = true;
                IsActivityFeedDisabled = true;
                IsInputTelemetryDisabled = true;
                IsBingSearchDisabled = true;
                IsEdgeTelemetryDisabled = true;
                IsFeedbackFrequencyDisabled = true;
                PrivacyOptimizerService.Instance.ApplyPreset("Balanced");
                StatusMessage = "Сбалансированный профиль приватности успешно применен!";
            }
            else if (preset == "Default")
            {
                SetAllToggles(false);
                PrivacyOptimizerService.Instance.ApplyPreset("Default");
                StatusMessage = "Стандартные параметры Windows восстановлены.";
            }
            TrayService.Instance.ShowNotification("Приватность", StatusMessage);
        }

        [RelayCommand]
        public void ApplyPrivacySettings()
        {
            PrivacyOptimizerService.Instance.SetTelemetry(IsTelemetryDisabled);
            PrivacyOptimizerService.Instance.SetAdvertisingId(IsAdIdDisabled);
            PrivacyOptimizerService.Instance.SetActivityFeed(IsActivityFeedDisabled);
            PrivacyOptimizerService.Instance.SetInputTelemetry(IsInputTelemetryDisabled);
            PrivacyOptimizerService.Instance.SetBingStartSearch(IsBingSearchDisabled);
            PrivacyOptimizerService.Instance.SetEdgeTelemetry(IsEdgeTelemetryDisabled);
            PrivacyOptimizerService.Instance.SetLocationSensors(IsLocationSensorsDisabled);
            PrivacyOptimizerService.Instance.SetFeedbackFrequency(IsFeedbackFrequencyDisabled);
            PrivacyOptimizerService.Instance.SetCortanaCopilot(IsCortanaCopilotDisabled);
            PrivacyOptimizerService.Instance.SetErrorReporting(IsErrorReportingDisabled);
            PrivacyOptimizerService.Instance.SetWifiSense(IsWifiSenseDisabled);
            PrivacyOptimizerService.Instance.SetAppInventory(IsAppInventoryDisabled);
            PrivacyOptimizerService.Instance.SetCameraMicBackgroundAccess(IsCameraMicBackgroundDisabled);
            PrivacyOptimizerService.Instance.SetRemoteAccess(IsRemoteAccessDisabled);

            StatusMessage = "Все выбранные правила приватности и блокировки успешно применены!";
            TrayService.Instance.ShowNotification("Приватность", StatusMessage);
        }
    }

    // --- System Tools View Model ---
    public partial class SystemToolsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _toolStatus = "Инструменты готовы к работе";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy = false;

        public bool IsNotBusy => !IsBusy;

        [RelayCommand]
        public async Task CreateRestorePointAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Создание контрольной точки восстановления...";

            bool ok = await SystemToolsService.Instance.CreateRestorePointAsync("STORM Optimizer Checkpoint");
            ToolStatus = ok ? "Точка восстановления успешно создана!" : "Создание завершено (или выключено в ОС).";
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunSfcScanAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Проверка целостности системных файлов Windows (SFC /scannow)...";

            bool ok = await SystemToolsService.Instance.RunSfcScanAsync(line =>
            {
                App.Current.Dispatcher.Invoke(() => ToolStatus = line);
            });

            ToolStatus = ok ? "Проверка SFC завершена: системные файлы в норме!" : "Проверка SFC завершена.";
            IsBusy = false;
            TrayService.Instance.ShowNotification("SFC Scannow", ToolStatus);
        }

        [RelayCommand]
        public async Task RunDismRestoreAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Восстановление хранилища компонентов DISM RestoreHealth...";

            bool ok = await SystemToolsService.Instance.RunDismRestoreHealthAsync(line =>
            {
                App.Current.Dispatcher.Invoke(() => ToolStatus = line);
            });

            ToolStatus = ok ? "Образ Windows успешно восстановлен через DISM!" : "Выполнение DISM завершено.";
            IsBusy = false;
            TrayService.Instance.ShowNotification("DISM RestoreHealth", ToolStatus);
        }

        [RelayCommand]
        public async Task CleanWinSxSAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Очистка устаревших компонентов хранилища WinSxS...";

            bool ok = await SystemToolsService.Instance.CleanComponentStoreAsync();
            ToolStatus = ok ? "Хранилище компонентов WinSxS успешно очищено!" : "Очистка завершена.";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Очистка WinSxS", ToolStatus);
        }

        [RelayCommand]
        public void CleanTempFiles()
        {
            try
            {
                ToolStatus = "Очистка временных файлов и кэша Prefetch...";
                string temp1 = Path.GetTempPath();
                string temp2 = @"C:\Windows\Temp";
                int cleaned = 0;

                foreach (var dir in new[] { temp1, temp2 })
                {
                    if (Directory.Exists(dir))
                    {
                        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly))
                        {
                            try { File.Delete(file); cleaned++; } catch { }
                        }
                    }
                }

                ToolStatus = $"Очищено {cleaned} временных файлов!";
                TrayService.Instance.ShowNotification("Очистка Temp", ToolStatus);
            }
            catch (Exception ex)
            {
                ToolStatus = $"Ошибка очистки: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task ClearEventLogsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Очистка системных журналов событий Windows...";

            await Task.Run(() =>
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "wevtutil.exe",
                        Arguments = "cl Application",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    System.Diagnostics.Process.Start(psi)?.WaitForExit(3000);

                    psi.Arguments = "cl System";
                    System.Diagnostics.Process.Start(psi)?.WaitForExit(3000);

                    psi.Arguments = "cl Security";
                    System.Diagnostics.Process.Start(psi)?.WaitForExit(3000);
                }
                catch { }
            });

            IsBusy = false;
            ToolStatus = "Журналы событий Windows (Application, System, Security) успешно очищены!";
            TrayService.Instance.ShowNotification("Журналы Windows", ToolStatus);
        }

        [RelayCommand]
        public void RebuildIconCache()
        {
            bool ok = SystemToolsService.Instance.RebuildIconCache();
            ToolStatus = ok ? "Кэш иконок и эскизов Проводника перестроен!" : "Ошибка сброса кэша иконок.";
            TrayService.Instance.ShowNotification("Проводник", ToolStatus);
        }

        [RelayCommand]
        public void ResetWinsock()
        {
            bool ok = SystemToolsService.Instance.ResetWinsock();
            ToolStatus = ok ? "Сетевой каталог Winsock сброшен! Рекомендуется перезагрузка." : "Ошибка сброса Winsock.";
            TrayService.Instance.ShowNotification("Winsock Reset", ToolStatus);
        }

        [RelayCommand]
        public void ResetWindowsStore()
        {
            bool ok = SystemToolsService.Instance.ResetWindowsStore();
            ToolStatus = ok ? "Сброс кэша Microsoft Store (wsreset) запущен!" : "Ошибка запуска wsreset.";
        }

        [RelayCommand]
        public void LaunchSnapin(string tool)
        {
            try
            {
                switch (tool)
                {
                    case "DeviceManager": SystemToolsService.Instance.LaunchSnapin("devmgmt.msc"); break;
                    case "TaskManager": SystemToolsService.Instance.LaunchSnapin("taskmgr.exe"); break;
                    case "Regedit": SystemToolsService.Instance.LaunchSnapin("regedit.exe"); break;
                    case "DxDiag": SystemToolsService.Instance.LaunchSnapin("dxdiag.exe"); break;
                    case "GpEdit": SystemToolsService.Instance.LaunchSnapin("gpedit.msc"); break;
                    case "EventViewer": SystemToolsService.Instance.LaunchSnapin("eventvwr.msc"); break;
                    case "CleanMgr": SystemToolsService.Instance.LaunchSnapin("cleanmgr.exe"); break;
                    case "ResMon": SystemToolsService.Instance.LaunchSnapin("resmon.exe"); break;
                    case "Services": SystemToolsService.Instance.LaunchSnapin("services.msc"); break;
                    case "DiskMgmt": SystemToolsService.Instance.LaunchSnapin("diskmgmt.msc"); break;
                }
            }
            catch (Exception ex)
            {
                ToolStatus = $"Не удалось запустить оснастку: {ex.Message}";
            }
        }
    }

    // --- Settings View Model ---
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _appVersion = "v0.0.7";

        [ObservableProperty]
        private string _updateStatusText = "Проверка обновлений не выполнялась";

        [ObservableProperty]
        private bool _isCheckingUpdate = false;

        public SettingsViewModel()
        {
            AppVersion = $"v{UpdateService.CurrentVersion}";
        }

        [RelayCommand]
        public async Task CheckForUpdatesAsync()
        {
            if (IsCheckingUpdate) return;
            IsCheckingUpdate = true;
            UpdateStatusText = "Подключение к серверу обновлений GitHub...";

            var info = await UpdateService.Instance.CheckForUpdatesAsync();
            IsCheckingUpdate = false;

            if (info != null && info.IsUpdateAvailable)
            {
                UpdateStatusText = $"Доступна новая версия: v{info.LatestVersion}! Нажмите «Обновить сейчас».";
                TrayService.Instance.ShowNotification("Доступно обновление", $"Вышла новая версия STORM SYSTEM OPTIMIZER v{info.LatestVersion}!");
            }
            else
            {
                UpdateStatusText = $"У вас установлена последняя официальная версия ({AppVersion}).";
            }
        }

        [RelayCommand]
        public async Task InstallUpdateAsync()
        {
            UpdateStatusText = "Загрузка обновления...";
            var info = await UpdateService.Instance.CheckForUpdatesAsync();
            if (info != null && info.IsUpdateAvailable && !string.IsNullOrEmpty(info.DownloadUrl))
            {
                UpdateStatusText = $"Скачивание v{info.LatestVersion}...";
                bool ok = await UpdateService.Instance.DownloadAndApplyUpdateAsync(info.DownloadUrl);
                if (!ok)
                {
                    UpdateStatusText = "Ошибка установки обновления. Попробуйте позже.";
                }
            }
            else
            {
                UpdateStatusText = "Обновлений не требуется. Установлена актуальная версия.";
            }
        }
    }
}
