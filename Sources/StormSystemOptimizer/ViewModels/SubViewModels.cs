using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private string _statusText = "Загрузка автозапуска...";

        public StartupViewModel()
        {
            LoadStartupApps();
        }

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
            StartupService.Instance.ToggleStartupEntry(entry, entry.IsEnabled);
        }
    }

    // --- Services View Model ---
    public partial class ServicesViewModel : ObservableObject
    {
        public ObservableCollection<ServiceEntry> ServicesList { get; } = new();

        [ObservableProperty]
        private string _selectedProfile = "Рекомендуемый (Balanced)";

        [ObservableProperty]
        private string _statusMessage = "Готово к настройке служб";

        public ServicesViewModel()
        {
            RefreshServices();
        }

        public void RefreshServices()
        {
            ServicesList.Clear();
            var list = WindowsServicesService.Instance.GetUnnecessaryServices();
            foreach (var s in list) ServicesList.Add(s);
        }

        [RelayCommand]
        public void ApplyPreset(string preset)
        {
            SelectedProfile = preset switch
            {
                "Gaming" => "Игровой профиль (Gaming)",
                "Extreme" => "Экстремальная скорость (Extreme)",
                "Default" => "По умолчанию Windows",
                _ => "Сбалансированный"
            };

            WindowsServicesService.Instance.ApplyProfile(preset);
            RefreshServices();
            StatusMessage = $"Применен профиль: {SelectedProfile}";
        }

        [RelayCommand]
        public void ToggleService(ServiceEntry service)
        {
            bool disable = service.Status == "Работает";
            WindowsServicesService.Instance.SetServiceState(service.ServiceName, disable);
            RefreshServices();
        }
    }

    // --- Network View Model ---
    public partial class NetworkViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _dnsStatus = "Кэш сопоставителя DNS в норме";

        [ObservableProperty]
        private string _pingText = "-- мс";

        [ObservableProperty]
        private string _pingTarget = "1.1.1.1 (Cloudflare DNS)";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotMeasuring))]
        private bool _isMeasuring = false;

        public bool IsNotMeasuring => !IsMeasuring;

        public NetworkViewModel()
        {
            _ = TestPingAsync();
        }

        [RelayCommand]
        public void FlushDns()
        {
            bool ok = NetworkOptimizerService.Instance.FlushDnsCache();
            DnsStatus = ok ? "Кэш DNS успешно очищен и сброшен!" : "Ошибка сброса DNS";
        }

        [RelayCommand]
        public void OptimizeTcp()
        {
            bool ok = NetworkOptimizerService.Instance.OptimizeTcpSettings();
            DnsStatus = ok ? "Настройки TCP/IP и стек оптимизированы (Autotuning=Normal, CTCP)!" : "Ошибка оптимизации TCP";
        }

        [RelayCommand]
        public async Task TestPingAsync()
        {
            if (IsMeasuring) return;
            IsMeasuring = true;
            PingText = "Замер...";
            long latency = await NetworkOptimizerService.Instance.MeasurePingAsync("1.1.1.1");
            PingText = latency >= 0 ? $"{latency} мс" : "Ошибка";
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
        private string _statusMessage = "Защита приватности активна";

        [RelayCommand]
        public void ApplyPrivacySettings()
        {
            PrivacyOptimizerService.Instance.DisableTelemetry();
            StatusMessage = "Настройки телеметрии и приватности успешно применены!";
        }
    }

    // --- System Tools View Model ---
    public partial class SystemToolsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _toolStatus = "Выберите инструмент для запуска";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy = false;

        public bool IsNotBusy => !IsBusy;

        [RelayCommand]
        public async Task CreateRestorePointAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Создание точки восстановления Windows...";
            bool ok = await SystemToolsService.Instance.CreateRestorePointAsync("STORM_Optimizer_SafePoint");
            ToolStatus = ok ? "Точка восстановления успешно создана!" : "Не удалось создать точку восстановления (проверьте службу VSS)";
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunSsdTrimAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Выполнение команды TRIM и оптимизации диска C:...";
            bool ok = await SystemToolsService.Instance.RunSsdTrimAsync("C:");
            ToolStatus = ok ? "Оптимизация накопителя C: успешно завершена!" : "Ошибка выполнения команды TRIM";
            IsBusy = false;
        }

        [RelayCommand]
        public void ActivateUltimatePowerPlan()
        {
            bool ok = SystemToolsService.Instance.ActivateUltimatePerformancePlan();
            ToolStatus = ok ? "План «Максимальная производительность» активирован!" : "Ошибка активации плана питания";
        }

        [RelayCommand]
        public void OptimizeVisualLatency()
        {
            bool ok = SystemToolsService.Instance.OptimizeMenuDelay();
            ToolStatus = ok ? "Задержка меню снижена до 10 мс!" : "Ошибка применения твика";
        }
    }

    // --- Settings View Model ---
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private ThemeType _selectedTheme;

        [ObservableProperty]
        private bool _minimizeToTray = true;

        [ObservableProperty]
        private bool _runAtStartup = false;

        [ObservableProperty]
        private string _appVersion = "0.0.1";

        public SettingsViewModel()
        {
            SelectedTheme = ThemeManager.Instance.CurrentTheme;
            ThemeManager.Instance.ThemeChanged += (s, t) => SelectedTheme = t;
        }

        [RelayCommand]
        public void ChangeTheme(string themeName)
        {
            var theme = themeName switch
            {
                "StormNight" => ThemeType.StormNight,
                "StormDay" => ThemeType.StormDay,
                "StormMidnight" => ThemeType.StormMidnight,
                _ => ThemeType.StormDark
            };

            SelectedTheme = theme;
            ThemeManager.Instance.ApplyTheme(theme, App.MainWindow);
        }
    }
}
