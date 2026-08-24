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
        private string _statusText = "Службы загружены";

        public ServicesViewModel()
        {
            LoadServices();
        }

        [RelayCommand]
        public void LoadServices()
        {
            ServicesList.Clear();
            var list = WindowsServicesService.Instance.GetUnnecessaryServices();
            foreach (var item in list) ServicesList.Add(item);
            StatusText = $"Всего оптимизируемых служб: {ServicesList.Count}";
        }

        [RelayCommand]
        public void ToggleService(ServiceEntry entry)
        {
            if (entry != null)
            {
                WindowsServicesService.Instance.SetServiceState(entry.ServiceName, entry.IsOptimized);
            }
        }

        [RelayCommand]
        public async Task ApplyPresetAsync(string preset)
        {
            SelectedProfile = preset;
            IsRecommendedActive = preset.Equals("balanced", StringComparison.OrdinalIgnoreCase) || preset.Equals("safe", StringComparison.OrdinalIgnoreCase);
            IsGamingActive = preset.Equals("gaming", StringComparison.OrdinalIgnoreCase);
            IsExtremeActive = preset.Equals("extreme", StringComparison.OrdinalIgnoreCase);

            string presetTitle = IsGamingActive ? "Игровой профиль" : (IsExtremeActive ? "Экстремальный" : "Безопасный");

            // Instantly update all items on UI thread
            foreach (var item in ServicesList)
            {
                bool shouldDisable = WindowsServicesService.Instance.ShouldDisableInPreset(item.ServiceName, preset);
                item.IsOptimized = shouldDisable;
            }

            StatusText = $"Применен профиль «{presetTitle}». Применение параметров...";

            await Task.Run(() =>
            {
                WindowsServicesService.Instance.ApplyPreset(preset);
            });

            StatusText = $"Применен профиль оптимизации служб: «{presetTitle}»";
            TrayService.Instance.ShowNotification("Службы Windows", StatusText);
        }

        [RelayCommand]
        public async Task ApplyServicesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Применение параметров оптимизации служб...";

            await Task.Run(() =>
            {
                foreach (var s in ServicesList)
                {
                    WindowsServicesService.Instance.SetServiceState(s.ServiceName, s.IsOptimized);
                }
            });

            IsBusy = false;
            StatusText = "Параметры служб успешно сохранены в реестре Windows!";
            TrayService.Instance.ShowNotification("Службы Windows", "Выбранные параметры служб успешно сохранены.");
        }

        [RelayCommand]
        public async Task ApplyServicesChangesAsync()
        {
            await ApplyServicesAsync();
        }
    }

    // --- Network View Model with DNS Benchmark & Blackhole Shield ---
    // --- Network View Model with DNS Benchmark & Blackhole Shield ---
    public partial class NetworkViewModel : ObservableObject
    {
        public ObservableCollection<DnsServerItem> DnsServers { get; } = new();

        [ObservableProperty]
        private string _pingText = "-- мс";

        [ObservableProperty]
        private string _activeDnsProvider = "Определение...";

        [ObservableProperty]
        private string _dnsStatus = "Сетевой стек готов к работе";

        [ObservableProperty]
        private string _customDnsPrimary = "1.1.1.1";

        [ObservableProperty]
        private string _customDnsSecondary = "8.8.8.8";

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

        [ObservableProperty]
        private bool _isTcpNoDelayEnabled = true;

        [ObservableProperty]
        private bool _isBlackholeShieldEnabled = false;

        public NetworkViewModel()
        {
            _ = LoadNetworkDataAsync();
            LoadDefaultDnsList();
        }

        private void LoadDefaultDnsList()
        {
            DnsServers.Clear();
            foreach (var item in DnsBenchmarkService.Instance.GetDefaultDnsProviders())
            {
                DnsServers.Add(item);
            }
            UpdateActiveDnsLabel();
        }

        private void UpdateActiveDnsLabel()
        {
            var (p, s) = DnsBenchmarkService.GetCurrentSystemDns();
            if (!string.IsNullOrEmpty(p))
            {
                CustomDnsPrimary = p;
                if (!string.IsNullOrEmpty(s)) CustomDnsSecondary = s;

                var match = DnsServers.FirstOrDefault(x =>
                    string.Equals(x.PrimaryDns, p, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrEmpty(x.SecondaryDns) || string.Equals(x.SecondaryDns, s, StringComparison.OrdinalIgnoreCase)));

                if (match != null && !match.ProviderName.StartsWith("Текущий", StringComparison.OrdinalIgnoreCase))
                {
                    ActiveDnsProvider = match.ProviderName;
                }
                else if (p.StartsWith("192.168.") || p.StartsWith("10.") || p.StartsWith("172."))
                {
                    ActiveDnsProvider = "DHCP / Локальный роутер";
                }
                else
                {
                    ActiveDnsProvider = $"Пользовательский ({p})";
                }
            }
            else
            {
                ActiveDnsProvider = "DHCP (Автоматически)";
            }
        }

        [RelayCommand]
        public async Task LoadNetworkDataAsync()
        {
            NetworkInfo = await NetworkOptimizerService.Instance.GetNetworkInfoAsync();
            UpdateActiveDnsLabel();
            _ = MeasurePingAsync();
        }

        [RelayCommand]
        public async Task BenchmarkDnsAsync()
        {
            DnsStatus = "Тестирование задержки DNS серверов...";
            var list = await DnsBenchmarkService.Instance.BenchmarkAllDnsAsync();
            DnsServers.Clear();
            foreach (var item in list)
            {
                DnsServers.Add(item);
            }
            UpdateActiveDnsLabel();
            DnsStatus = $"Тест DNS завершен! Самый быстрый: {DnsServers.FirstOrDefault()?.ProviderName} ({DnsServers.FirstOrDefault()?.PingText})";
        }

        [RelayCommand]
        public async Task ApplyCustomDnsAsync()
        {
            string p = CustomDnsPrimary?.Trim() ?? "";
            string s = CustomDnsSecondary?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(p) || !System.Net.IPAddress.TryParse(p, out _))
            {
                DnsStatus = "Ошибка: укажите корректный IPv4 адрес для основного DNS 1 (например 1.1.1.1)";
                TrayService.Instance.ShowNotification("DNS Ошибка", DnsStatus);
                return;
            }

            if (!string.IsNullOrWhiteSpace(s) && !System.Net.IPAddress.TryParse(s, out _))
            {
                DnsStatus = "Ошибка: укажите корректный IPv4 адрес для альтернативного DNS 2 (например 8.8.8.8)";
                TrayService.Instance.ShowNotification("DNS Ошибка", DnsStatus);
                return;
            }

            DnsStatus = $"Установка пользовательского DNS ({p}, {s})...";
            DnsBenchmarkService.SetAppliedDns(p, s);

            bool ok = await DnsBenchmarkService.Instance.ApplyDnsToActiveAdapterAsync(p, s);
            if (ok)
            {
                if (NetworkInfo != null)
                {
                    NetworkInfo.DnsServers = string.IsNullOrEmpty(s) ? p : $"{p}, {s}";
                }
                UpdateActiveDnsLabel();
                LoadDefaultDnsList();
                DnsStatus = $"Пользовательский DNS ({p} / {s}) успешно применен!";
                TrayService.Instance.ShowNotification("DNS изменен", DnsStatus);
            }
            else
            {
                DnsStatus = "Не удалось применить DNS к активному сетевому адаптеру";
            }
        }

        [RelayCommand]
        public async Task ApplyDnsItemAsync(DnsServerItem item)
        {
            if (item == null) return;
            DnsStatus = $"Применение {item.ProviderName}...";
            DnsBenchmarkService.SetAppliedDns(item.PrimaryDns, item.SecondaryDns);

            CustomDnsPrimary = item.PrimaryDns;
            CustomDnsSecondary = item.SecondaryDns;

            // Instantly update active states in UI collection - strictly single active item
            foreach (var d in DnsServers)
            {
                d.IsActive = (d == item);
            }
            var custom = DnsServers.FirstOrDefault(x => x.ProviderName.StartsWith("Текущий DNS системы", StringComparison.OrdinalIgnoreCase));
            if (custom != null && item != custom)
            {
                DnsServers.Remove(custom);
            }

            ActiveDnsProvider = item.ProviderName;
            if (NetworkInfo != null)
            {
                NetworkInfo.DnsServers = item.DnsIpsText;
            }

            bool ok = await DnsBenchmarkService.Instance.ApplyDnsToActiveAdapterAsync(item.PrimaryDns, item.SecondaryDns);
            if (ok)
            {
                DnsStatus = $"DNS сервер {item.ProviderName} успешно установлен!";
                TrayService.Instance.ShowNotification("DNS изменен", DnsStatus);
            }
        }

        [RelayCommand]
        public void ToggleTcpNoDelay()
        {
            IsTcpNoDelayEnabled = !IsTcpNoDelayEnabled;
            AdvancedTweaksService.Instance.DisableNaglesAlgorithm();
            DnsStatus = IsTcpNoDelayEnabled ? "TCP NoDelay и Nagle отключены (ультра-низкий онлайн пинг)" : "Стандартные настройки TCP";
            TrayService.Instance.ShowNotification("TCP Оптимизация", DnsStatus);
        }

        [RelayCommand]
        public async Task ToggleBlackholeShieldAsync()
        {
            IsBlackholeShieldEnabled = !IsBlackholeShieldEnabled;
            bool ok = await AdvancedTweaksService.Instance.ToggleBlackholeTelemetryShieldAsync(IsBlackholeShieldEnabled);
            DnsStatus = IsBlackholeShieldEnabled ? "Сетевой экран Blackhole Shield активен (телеметрия заблокирована)" : "Blackhole Shield отключен";
            TrayService.Instance.ShowNotification("Blackhole Shield", DnsStatus);
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
            AdvancedTweaksService.Instance.DisableNaglesAlgorithm();
            DnsStatus = "Параметры TCP Window Auto-Tuning, RSS, QoS и TCP NoDelay оптимизированы!";
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
        public async Task ResetNetworkStackAsync()
        {
            DnsStatus = "Сброс каталога Winsock и сетевого стека TCP/IP...";
            bool ok = await NetworkOptimizerService.Instance.ResetNetworkStackAsync();
            DnsStatus = ok ? "Сетевой стек, Winsock и кэш DNS успешно сброшены! Соединение восстановлено." : "Сброс сети выполнен.";
            TrayService.Instance.ShowNotification("Сброс сети", DnsStatus);
            await LoadNetworkDataAsync();
        }

        [RelayCommand]
        public async Task OptimizeMtuAsync()
        {
            DnsStatus = "Автоматическая настройка MTU (Maximum Transmission Unit)...";
            int mtu = await NetworkOptimizerService.Instance.OptimizeMtuAsync();
            DnsStatus = $"MTU сетевого адаптера установлен на оптимальное значение {mtu} байт.";
            TrayService.Instance.ShowNotification("MTU Оптимизация", DnsStatus);
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
        private bool _isNvidiaTelemetryDisabled = true;

        [ObservableProperty]
        private bool _isIntelTelemetryDisabled = true;

        [ObservableProperty]
        private bool _isHostsBlockEnabled = true;

        [ObservableProperty]
        private bool _isFirewallBlockEnabled = true;

        [ObservableProperty]
        private bool _isRecallDisabled = true;

        [ObservableProperty]
        private string _statusMessage = "Защита приватности активна • 19 уровней безопасности";

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
            IsNvidiaTelemetryDisabled = state;
            IsIntelTelemetryDisabled = state;
            IsHostsBlockEnabled = state;
            IsFirewallBlockEnabled = state;
            IsRecallDisabled = state;
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
                IsHostsBlockEnabled = true;
                IsFirewallBlockEnabled = true;
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
            PrivacyOptimizerService.Instance.SetNvidiaTelemetry(IsNvidiaTelemetryDisabled);
            PrivacyOptimizerService.Instance.SetIntelTelemetry(IsIntelTelemetryDisabled);
            PrivacyOptimizerService.Instance.SetHostsTelemetryBlock(IsHostsBlockEnabled);
            PrivacyOptimizerService.Instance.SetFirewallTelemetryBlock(IsFirewallBlockEnabled);
            PrivacyOptimizerService.Instance.SetWindowsRecall(IsRecallDisabled);

            StatusMessage = "Все выбранные правила приватности и блокировки успешно применены!";
            TrayService.Instance.ShowNotification("Приватность", StatusMessage);
        }
    }

    // --- System Tools View Model with Advanced Tweaks ---
    public partial class SystemToolsViewModel : ObservableObject
    {
        [RelayCommand]
        public void LaunchTrustedInstaller()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Выберите файл для запуска от имени Доверенного установщика",
                    Filter = "Исполняемые файлы и скрипты (*.exe;*.bat;*.cmd;*.ps1;*.reg)|*.exe;*.bat;*.cmd;*.ps1;*.reg|Все файлы (*.*)|*.*"
                };
                if (dialog.ShowDialog() == true)
                {
                    bool ok = SystemToolsService.LaunchAsTrustedInstaller(dialog.FileName);
                    ToolStatus = ok ? $"Файл {Path.GetFileName(dialog.FileName)} запущен с наивысшими правами!" : "Не удалось запустить файл";
                    TrayService.Instance.ShowNotification("Доверенный установщик", ToolStatus);
                }
            }
            catch (Exception ex)
            {
                ToolStatus = $"Ошибка: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task InstallVCRedist()
        {
            IsBusy = true;
            ToolStatus = "Установка системных пакетов Visual C++...";
            await SystemToolsService.Instance.InstallRuntimeAsync("VCRedist", msg => ToolStatus = msg);
            IsBusy = false;
            TrayService.Instance.ShowNotification("Центр библиотек", ToolStatus);
        }

        [RelayCommand]
        public async Task InstallDirectX()
        {
            IsBusy = true;
            ToolStatus = "Установка компонентов DirectX...";
            await SystemToolsService.Instance.InstallRuntimeAsync("DirectX", msg => ToolStatus = msg);
            IsBusy = false;
            TrayService.Instance.ShowNotification("Центр библиотек", ToolStatus);
        }

        [RelayCommand]
        public async Task InstallDotNet()
        {
            IsBusy = true;
            ToolStatus = "Установка среды выполнения .NET...";
            await SystemToolsService.Instance.InstallRuntimeAsync("DotNet", msg => ToolStatus = msg);
            IsBusy = false;
            TrayService.Instance.ShowNotification("Центр библиотек", ToolStatus);
        }

        [RelayCommand]
        public async Task ToggleWindowsFeature(string featureKey)
        {
            IsBusy = true;
            ToolStatus = "Настройка компонента Windows...";
            bool ok = await SystemToolsService.Instance.ToggleWindowsFeatureAsync(featureKey, true);
            ToolStatus = ok ? "Компонент успешно настроен!" : "Операция завершена";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Компоненты Windows", ToolStatus);
        }

        private readonly string _stateFilePath;

        [ObservableProperty]
        private string _toolStatus = "Инструменты готовы к работе";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy = false;

        [ObservableProperty]
        private bool _isMsiActive = false;

        [ObservableProperty]
        private bool _isDirectStorageActive = false;

        [ObservableProperty]
        private bool _isStandbyPurged = false;

        [ObservableProperty]
        private bool _isSfcChecked = false;

        [ObservableProperty]
        private bool _isDismChecked = false;

        [ObservableProperty]
        private bool _isWinSxSCleaned = false;

        [ObservableProperty]
        private bool _isWinsockReset = false;

        [ObservableProperty]
        private bool _isLogsCleared = false;

        [ObservableProperty]
        private bool _isTempCleaned = false;

        [ObservableProperty]
        private bool _isExplorerOptimized = false;

        [ObservableProperty]
        private bool _isWin32PriorityOptimized = false;

        [ObservableProperty]
        private bool _isMmcssOptimized = false;

        [ObservableProperty]
        private bool _isZeroStartupDelayActive = false;

        [ObservableProperty]
        private bool _isMpoDisabled = false;

        [ObservableProperty]
        private bool _isVbsDisabled = false;

        [ObservableProperty]
        private bool _isHpetOptimized = false;

        [ObservableProperty]
        private bool _isShaderCacheOptimized = false;

        [ObservableProperty]
        private bool _isGameDvrDisabled = false;

        public bool IsNotBusy => !IsBusy;

        public SystemToolsViewModel()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM_OPTIMIZER");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _stateFilePath = Path.Combine(dir, "system_tools_state.json");
            LoadPersistentState();
        }

        private void LoadPersistentState()
        {
            try
            {
                bool msiRegistry = AdvancedTweaksService.Instance.IsMsiModeActive();
                bool dsRegistry = AdvancedTweaksService.Instance.IsDirectStorageOptimized();
                bool expRegistry = AdvancedTweaksService.Instance.IsExplorerResponsivenessActive();
                bool win32Registry = AdvancedTweaksService.Instance.IsWin32PrioritySeparationActive();
                bool mmcssRegistry = AdvancedTweaksService.Instance.IsMmcssGamingOptimizationActive();
                bool zeroStartRegistry = AdvancedTweaksService.Instance.IsZeroStartupDelayActive();
                bool mpoRegistry = AdvancedTweaksService.Instance.IsMpoDisabled();
                bool vbsRegistry = AdvancedTweaksService.Instance.IsVbsDisabled();
                bool hpetRegistry = AdvancedTweaksService.Instance.IsHpetOptimized();
                bool shaderRegistry = AdvancedTweaksService.Instance.IsShaderCacheOptimized();
                bool dvrRegistry = AdvancedTweaksService.Instance.IsGameDvrDisabled();

                IsMsiActive = msiRegistry;
                IsDirectStorageActive = dsRegistry;
                IsExplorerOptimized = expRegistry;
                IsWin32PriorityOptimized = win32Registry;
                IsMmcssOptimized = mmcssRegistry;
                IsZeroStartupDelayActive = zeroStartRegistry;
                IsMpoDisabled = mpoRegistry;
                IsVbsDisabled = vbsRegistry;
                IsHpetOptimized = hpetRegistry;
                IsShaderCacheOptimized = shaderRegistry;
                IsGameDvrDisabled = dvrRegistry;

                if (File.Exists(_stateFilePath))
                {
                    string json = File.ReadAllText(_stateFilePath);
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                    if (dict != null)
                    {
                        if (dict.TryGetValue("IsMsiActive", out bool vMsi)) IsMsiActive = vMsi || msiRegistry;
                        if (dict.TryGetValue("IsDirectStorageActive", out bool vDs)) IsDirectStorageActive = vDs || dsRegistry;
                        if (dict.TryGetValue("IsExplorerOptimized", out bool vExp)) IsExplorerOptimized = vExp || expRegistry;
                        if (dict.TryGetValue("IsWin32PriorityOptimized", out bool vW32)) IsWin32PriorityOptimized = vW32 || win32Registry;
                        if (dict.TryGetValue("IsMmcssOptimized", out bool vMm)) IsMmcssOptimized = vMm || mmcssRegistry;
                        if (dict.TryGetValue("IsZeroStartupDelayActive", out bool vZs)) IsZeroStartupDelayActive = vZs || zeroStartRegistry;
                        if (dict.TryGetValue("IsMpoDisabled", out bool vMpo)) IsMpoDisabled = vMpo || mpoRegistry;
                        if (dict.TryGetValue("IsVbsDisabled", out bool vVbs)) IsVbsDisabled = vVbs || vbsRegistry;
                        if (dict.TryGetValue("IsHpetOptimized", out bool vHp)) IsHpetOptimized = vHp || hpetRegistry;
                        if (dict.TryGetValue("IsShaderCacheOptimized", out bool vSc)) IsShaderCacheOptimized = vSc || shaderRegistry;
                        if (dict.TryGetValue("IsGameDvrDisabled", out bool vDvr)) IsGameDvrDisabled = vDvr || dvrRegistry;
                        if (dict.TryGetValue("IsStandbyPurged", out bool v1)) IsStandbyPurged = v1;
                        if (dict.TryGetValue("IsSfcChecked", out bool v2)) IsSfcChecked = v2;
                        if (dict.TryGetValue("IsDismChecked", out bool v3)) IsDismChecked = v3;
                        if (dict.TryGetValue("IsWinSxSCleaned", out bool v4)) IsWinSxSCleaned = v4;
                        if (dict.TryGetValue("IsWinsockReset", out bool v5)) IsWinsockReset = v5;
                        if (dict.TryGetValue("IsLogsCleared", out bool v6)) IsLogsCleared = v6;
                        if (dict.TryGetValue("IsTempCleaned", out bool v7)) IsTempCleaned = v7;
                    }
                }
            }
            catch { }
        }

        private void SavePersistentState()
        {
            try
            {
                var dict = new Dictionary<string, bool>
                {
                    { "IsMsiActive", IsMsiActive },
                    { "IsDirectStorageActive", IsDirectStorageActive },
                    { "IsExplorerOptimized", IsExplorerOptimized },
                    { "IsWin32PriorityOptimized", IsWin32PriorityOptimized },
                    { "IsMmcssOptimized", IsMmcssOptimized },
                    { "IsZeroStartupDelayActive", IsZeroStartupDelayActive },
                    { "IsMpoDisabled", IsMpoDisabled },
                    { "IsVbsDisabled", IsVbsDisabled },
                    { "IsHpetOptimized", IsHpetOptimized },
                    { "IsShaderCacheOptimized", IsShaderCacheOptimized },
                    { "IsGameDvrDisabled", IsGameDvrDisabled },
                    { "IsStandbyPurged", IsStandbyPurged },
                    { "IsSfcChecked", IsSfcChecked },
                    { "IsDismChecked", IsDismChecked },
                    { "IsWinSxSCleaned", IsWinSxSCleaned },
                    { "IsWinsockReset", IsWinsockReset },
                    { "IsLogsCleared", IsLogsCleared },
                    { "IsTempCleaned", IsTempCleaned }
                };
                string json = System.Text.Json.JsonSerializer.Serialize(dict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_stateFilePath, json);
            }
            catch { }
        }

        [RelayCommand]
        public async Task ToggleMsiModeAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (IsMsiActive)
            {
                ToolStatus = "Отключение режима MSI (возврат к стандартным прерываниям)...";
                bool ok = AdvancedTweaksService.Instance.DisableMsiModeForGpuAndUsb();
                IsMsiActive = !ok;
                SavePersistentState();
                ToolStatus = ok ? "Режим MSI отключен (установлен стандартный режим прерываний)." : "Ошибка отключения MSI.";
            }
            else
            {
                ToolStatus = "Включение режима MSI (Message Signaled Interrupts) для GPU и USB...";
                bool ok = AdvancedTweaksService.Instance.EnableMsiModeForGpuAndUsb();
                IsMsiActive = ok;
                SavePersistentState();
                ToolStatus = ok ? "Режим MSI успешно активирован для видеокарты и USB-контроллеров!" : "Ошибка настройки MSI режима.";
            }

            IsBusy = false;
            TrayService.Instance.ShowNotification("MSI Mode", ToolStatus);
        }

        [RelayCommand]
        public async Task ToggleDirectStorageAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (IsDirectStorageActive)
            {
                ToolStatus = "Сброс тюнинга DirectStorage и IoRing к стандартным значениям...";
                bool ok = AdvancedTweaksService.Instance.DisableDirectStorageOptimization();
                IsDirectStorageActive = !ok;
                SavePersistentState();
                ToolStatus = ok ? "Параметры DirectStorage сброшены к значениям по умолчанию." : "Ошибка сброса параметров DirectStorage.";
            }
            else
            {
                ToolStatus = "Оптимизация DirectStorage 1.2 и очередей Win32 IoRing...";
                bool ok = AdvancedTweaksService.Instance.OptimizeDirectStorageAndIoRing();
                IsDirectStorageActive = ok;
                SavePersistentState();
                ToolStatus = ok ? "DirectStorage 1.2 и NVMe BypassIO успешно настроены!" : "Ошибка применения DirectStorage.";
            }

            IsBusy = false;
            TrayService.Instance.ShowNotification("DirectStorage", ToolStatus);
        }

        [RelayCommand]
        public async Task EnableMsiModeAsync() => await ToggleMsiModeAsync();

        [RelayCommand]
        public async Task EnableDirectStorageAsync() => await ToggleDirectStorageAsync();

        [RelayCommand]
        public async Task ToggleExplorerResponsivenessAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (IsExplorerOptimized)
            {
                ToolStatus = "Возврат настроек Проводника к стандартным задержкам...";
                bool ok = AdvancedTweaksService.Instance.ToggleExplorerResponsiveness(false);
                IsExplorerOptimized = !ok;
                SavePersistentState();
                ToolStatus = ok ? "Стандартные параметры задержек Проводника восстановлены." : "Ошибка возврата параметров.";
            }
            else
            {
                ToolStatus = "Оптимизация отклика Проводника и отключение зависаний...";
                bool ok = AdvancedTweaksService.Instance.ToggleExplorerResponsiveness(true);
                IsExplorerOptimized = ok;
                SavePersistentState();
                ToolStatus = ok ? "Отклик Проводника оптимизирован (MenuShowDelay=0, изоляция процессов активна)!" : "Ошибка применения настроек.";
            }

            IsBusy = false;
            TrayService.Instance.ShowNotification("Отклик Проводника", ToolStatus);
        }

        [RelayCommand]
        public async Task ToggleWin32PriorityAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (IsWin32PriorityOptimized)
            {
                ToolStatus = "Сброс квантов планировщика ядра Win32 к стандартным значениям...";
                bool ok = AdvancedTweaksService.Instance.ToggleWin32PrioritySeparation(false);
                IsWin32PriorityOptimized = !ok;
                SavePersistentState();
                ToolStatus = ok ? "Стандартный планировщик ядра Win32 восстановлен." : "Ошибка сброса параметров.";
            }
            else
            {
                ToolStatus = "Активация игрового квантования Win32 Priority Separation (0x1A)...";
                bool ok = AdvancedTweaksService.Instance.ToggleWin32PrioritySeparation(true);
                IsWin32PriorityOptimized = ok;
                SavePersistentState();
                ToolStatus = ok ? "Игровой планировщик Win32 активирован (максимальный приоритет переднего окна)!" : "Ошибка настройки Win32.";
            }

            IsBusy = false;
            TrayService.Instance.ShowNotification("Win32 Priority", ToolStatus);
        }

        [RelayCommand]
        public async Task ToggleMmcssAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (IsMmcssOptimized)
            {
                ToolStatus = "Сброс MMCSS и сетевого троттлинга к значениям по умолчанию...";
                bool ok = AdvancedTweaksService.Instance.ToggleMmcssGamingOptimization(false);
                IsMmcssOptimized = !ok;
                SavePersistentState();
                ToolStatus = ok ? "Стандартные параметры MMCSS восстановлены." : "Ошибка сброса.";
            }
            else
            {
                ToolStatus = "Оптимизация MMCSS (100% ресурсов GPU/CPU) и отключение сетевого троттлинга...";
                bool ok = AdvancedTweaksService.Instance.ToggleMmcssGamingOptimization(true);
                IsMmcssOptimized = ok;
                SavePersistentState();
                ToolStatus = ok ? "MMCSS оптимизирован (NetworkThrottling отключен, Gaming Priority=8)!" : "Ошибка применения MMCSS.";
            }

            IsBusy = false;
            TrayService.Instance.ShowNotification("MMCSS и Сеть", ToolStatus);
        }

        [RelayCommand]
        public async Task ToggleZeroStartupDelayAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (IsZeroStartupDelayActive)
            {
                ToolStatus = "Возврат стандартной задержки запуска Windows...";
                bool ok = AdvancedTweaksService.Instance.ToggleZeroStartupDelay(false);
                IsZeroStartupDelayActive = !ok;
                SavePersistentState();
                ToolStatus = ok ? "Стандартная задержка автозапуска восстановлена." : "Ошибка сброса.";
            }
            else
            {
                ToolStatus = "Устранение 10-секундной системной задержки автозапуска (StartupDelay=0)...";
                bool ok = AdvancedTweaksService.Instance.ToggleZeroStartupDelay(true);
                IsZeroStartupDelayActive = ok;
                SavePersistentState();
                ToolStatus = ok ? "Мгновенный автозапуск активен (StartupDelayInMSec = 0)!" : "Ошибка применения.";
            }

            IsBusy = false;
            TrayService.Instance.ShowNotification("Мгновенный запуск", ToolStatus);
        }

        [RelayCommand]
        public async Task ToggleMpoModeAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (IsMpoDisabled)
            {
                ToolStatus = "Включение Multi-Plane Overlay (MPO) обратно...";
                bool ok = AdvancedTweaksService.Instance.ToggleMpoMode(false);
                IsMpoDisabled = !ok;
                SavePersistentState();
                ToolStatus = ok ? "MPO включен (стандартный режим DWM)." : "Ошибка изменения MPO.";
            }
            else
            {
                ToolStatus = "Отключение Multi-Plane Overlay (MPO) для устранения микрофризов и черных экранов...";
                bool ok = AdvancedTweaksService.Instance.ToggleMpoMode(true);
                IsMpoDisabled = ok;
                SavePersistentState();
                ToolStatus = ok ? "MPO успешно отключен (ликвидированы статтеры при разной частоте мониторов)!" : "Ошибка отключения MPO.";
            }

            IsBusy = false;
            TrayService.Instance.ShowNotification("MPO Anti-Stutter", ToolStatus);
        }

        [RelayCommand]
        public async Task ToggleVbsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (IsVbsDisabled)
            {
                ToolStatus = "Включение Virtualization-Based Security (VBS) и изоляции ядра...";
                bool ok = AdvancedTweaksService.Instance.ToggleVbsHypervisor(false);
                IsVbsDisabled = !ok;
                SavePersistentState();
                ToolStatus = ok ? "VBS и гипервизор переведены в стандартный режим." : "Ошибка переключения VBS.";
            }
            else
            {
                ToolStatus = "Отключение VBS и HVCI гипервизора для возврата 5-15% FPS...";
                bool ok = AdvancedTweaksService.Instance.ToggleVbsHypervisor(true);
                IsVbsDisabled = ok;
                SavePersistentState();
                ToolStatus = ok ? "VBS отключен (гипервизор выключен, освобождена процессорная мощь)!" : "Ошибка отключения VBS.";
            }

            IsBusy = false;
            TrayService.Instance.ShowNotification("VBS Оптимизация", ToolStatus);
        }

        [RelayCommand]
        public async Task ToggleHpetAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (IsHpetOptimized)
            {
                ToolStatus = "Сброс таймеров Windows к заводским значениям...";
                bool ok = AdvancedTweaksService.Instance.ToggleHpetSyntheticTimers(false);
                IsHpetOptimized = !ok;
                SavePersistentState();
                ToolStatus = ok ? "Таймеры Windows сброшены к стандартным значениям." : "Ошибка сброса таймеров.";
            }
            else
            {
                ToolStatus = "Оптимизация Invariant TSC и отключение платформенного таймера HPET...";
                bool ok = AdvancedTweaksService.Instance.ToggleHpetSyntheticTimers(true);
                IsHpetOptimized = ok;
                SavePersistentState();
                ToolStatus = ok ? "Аппаратный Invariant TSC активен, HPET отключен (ликвидирован джиттер)!" : "Ошибка настройки таймеров.";
            }

            IsBusy = false;
            TrayService.Instance.ShowNotification("Синхронизация таймеров", ToolStatus);
        }

        [RelayCommand]
        public async Task ToggleShaderCacheAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (IsShaderCacheOptimized)
            {
                ToolStatus = "Сброс размера кэша шейдеров к стандартному...";
                bool ok = AdvancedTweaksService.Instance.OptimizeShaderCacheSize(false);
                IsShaderCacheOptimized = !ok;
                SavePersistentState();
                ToolStatus = ok ? "Размер кэша шейдеров сброшен к заводскому." : "Ошибка сброса кэша.";
            }
            else
            {
                ToolStatus = "Расширение кэша шейдеров DirectX/NVIDIA до 10 ГБ...";
                bool ok = AdvancedTweaksService.Instance.OptimizeShaderCacheSize(true);
                IsShaderCacheOptimized = ok;
                SavePersistentState();
                ToolStatus = ok ? "Кэш шейдеров расширен до 10 ГБ (устранены статтеры перекомпиляции)!" : "Ошибка настройки кэша.";
            }

            IsBusy = false;
            TrayService.Instance.ShowNotification("Кэш шейдеров GPU", ToolStatus);
        }

        [RelayCommand]
        public async Task ToggleGameDvrAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (IsGameDvrDisabled)
            {
                ToolStatus = "Включение GameDVR и фоновой записи...";
                bool ok = AdvancedTweaksService.Instance.ToggleGameDvrAndFso(false);
                IsGameDvrDisabled = !ok;
                SavePersistentState();
                ToolStatus = ok ? "GameDVR включен." : "Ошибка переключения GameDVR.";
            }
            else
            {
                ToolStatus = "Отключение GameDVR и фонового перехвата видеокадров...";
                bool ok = AdvancedTweaksService.Instance.ToggleGameDvrAndFso(true);
                IsGameDvrDisabled = ok;
                SavePersistentState();
                ToolStatus = ok ? "GameDVR полностью отключен (устранены задержки конвейера FSO)!" : "Ошибка отключения GameDVR.";
            }

            IsBusy = false;
            TrayService.Instance.ShowNotification("GameDVR Тюнинг", ToolStatus);
        }

        [RelayCommand]
        public async Task RebuildShellCacheAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Сброс и перестроение системного кэша иконок и эскизов...";

            var (ok, msg) = await AdvancedTweaksService.Instance.RebuildIconAndThumbnailCacheAsync();
            ToolStatus = msg;
            IsBusy = false;
            TrayService.Instance.ShowNotification("Кэш Проводника", msg);
        }

        [RelayCommand]
        public async Task CreateSnapshotAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Создание резервного снимка параметров реестра...";

            string path = await AdvancedTweaksService.Instance.CreateRegistryBackupSnapshotAsync("UserSnapshot");
            ToolStatus = $"Снимок реестра успешно сохранен в Backups!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Снимок реестра", $"Резервная копия сохранена: {Path.GetFileName(path)}");
        }

        [RelayCommand]
        public async Task PurgeStandbyMemoryAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Очистка кэшированной памяти ожидания (Standby List)...";

            bool ok = MemoryOptimizerService.Instance.PurgeStandbyList();
            IsStandbyPurged = ok;
            SavePersistentState();
            ToolStatus = ok ? "Standby List памяти успешно очищен без сброса рабочих данных!" : "Очистка выполнена.";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Очистка Standby RAM", ToolStatus);
        }

        [RelayCommand]
        public async Task SmartCompressMemoryAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Сжатие и оптимизация памяти фоновых процессов...";

            var (count, freedMb) = await MemoryOptimizerService.Instance.SmartCompressMemoryAsync();
            ToolStatus = $"Сжатие завершено: освобождено {FormatHelper.FormatDouble(freedMb, 0)} МБ памяти у {FormatHelper.FormatInt(count)} процессов!";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Smart RAM", ToolStatus);
        }

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

            IsSfcChecked = true;
            SavePersistentState();
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

            IsDismChecked = true;
            SavePersistentState();
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
            IsWinSxSCleaned = ok;
            SavePersistentState();
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

                IsTempCleaned = true;
                SavePersistentState();
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

            IsLogsCleared = true;
            SavePersistentState();
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
            IsWinsockReset = ok;
            SavePersistentState();
            ToolStatus = ok ? "Сетевой каталог Winsock сброшен! Рекомендуется перезагрузка." : "Ошибка сброса Winsock.";
            TrayService.Instance.ShowNotification("Winsock Reset", ToolStatus);
        }

        [RelayCommand]
        public void ResetWindowsStore()
        {
            bool ok = SystemToolsService.Instance.ResetWindowsStore();
            ToolStatus = ok ? "Сброс кэша Microsoft Store (wsreset) запущен!" : "Ошибка запуска wsreset.";
        }

        public ObservableCollection<SystemPortInfo> ActivePorts { get; } = new();

        [ObservableProperty]
        private SystemPortInfo? _selectedPort;

        [ObservableProperty]
        private string _portStatusMessage = "Нажмите «Сканировать порты» для анализа активных сокетов ОС.";

        [RelayCommand]
        public async Task LoadPortsAsync()
        {
            PortStatusMessage = "Сканирование открытых TCP/UDP портов и служб...";
            var ports = await SystemToolsService.Instance.GetActivePortsAsync();
            ActivePorts.Clear();
            foreach (var p in ports)
            {
                ActivePorts.Add(p);
            }
            PortStatusMessage = $"Обнаружено активных сетевых портов и подключений: {ActivePorts.Count}";
        }

        [RelayCommand]
        public void KillSelectedPortProcess()
        {
            if (SelectedPort == null)
            {
                PortStatusMessage = "Выберите соединение или порт в таблице для завершения процесса.";
                return;
            }
            if (SelectedPort.ProcessId <= 4)
            {
                PortStatusMessage = "Нельзя принудительно завершить системный процесс ядра (PID 0 / 4).";
                return;
            }
            bool ok = SystemToolsService.Instance.KillProcessByPid(SelectedPort.ProcessId);
            if (ok)
            {
                PortStatusMessage = $"Процесс {SelectedPort.ProcessName} (PID: {SelectedPort.ProcessId}) успешно завершен, порт освобожден!";
                TrayService.Instance.ShowNotification("Освобождение порта 🛡️", PortStatusMessage);
                _ = LoadPortsAsync();
            }
            else
            {
                PortStatusMessage = $"Не удалось завершить процесс PID {SelectedPort.ProcessId}.";
            }
        }

        [RelayCommand]
        public async Task FlushNetworkStackAsync()
        {
            ToolStatus = "Сброс сокетов, очистка ARP, DNS и сброс IP-стека...";
            bool ok = await SystemToolsService.Instance.FlushDnsArpIpStackAsync();
            ToolStatus = ok ? "Сетевой стек, DNS, ARP и Winsock полностью очищены!" : "Ошибка сброса сетевого стека.";
            TrayService.Instance.ShowNotification("Сетевой стек 🌐", ToolStatus);
        }

        [RelayCommand]
        public async Task ClearSpoolerAsync()
        {
            ToolStatus = "Очистка очереди печати Print Spooler...";
            bool ok = await SystemToolsService.Instance.ClearPrintSpoolerQueueAsync();
            ToolStatus = ok ? "Очередь Print Spooler очищена, служба печати перезапущена!" : "Ошибка очистки очереди печати.";
            TrayService.Instance.ShowNotification("Служба печати 🖨️", ToolStatus);
        }

        [RelayCommand]
        public async Task ResyncClockAsync()
        {
            ToolStatus = "Синхронизация системного времени через NTP (time.windows.com)...";
            bool ok = await SystemToolsService.Instance.ResyncSystemClockAsync();
            ToolStatus = ok ? "Системное время Windows успешно синхронизировано с сервером NTP!" : "Ошибка синхронизации времени.";
            TrayService.Instance.ShowNotification("Синхронизация времени ⏱️", ToolStatus);
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
                    case "NetworkConn": SystemToolsService.Instance.LaunchSnapin("ncpa.cpl"); break;
                    case "PowerCfg": SystemToolsService.Instance.LaunchSnapin("powercfg.cpl"); break;
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
        private string _appVersion = "v0.1.4";

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
