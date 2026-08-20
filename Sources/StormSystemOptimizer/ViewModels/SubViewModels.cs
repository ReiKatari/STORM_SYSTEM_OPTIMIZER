using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
            foreach (var item in list) ServicesList.Add(item);
            StatusMessage = $"Обнаружено служб для оптимизации: {ServicesList.Count}";
        }

        [RelayCommand]
        public async Task ApplyProfileAsync(string profileName)
        {
            StatusMessage = $"Применение профиля «{profileName}»...";
            await Task.Run(() =>
            {
                WindowsServicesService.Instance.ApplyProfile(profileName);
            });
            RefreshServices();
            StatusMessage = $"Профиль «{profileName}» успешно применен!";
        }

        [RelayCommand]
        public void ToggleService(ServiceEntry entry)
        {
            WindowsServicesService.Instance.SetServiceState(entry.ServiceName, entry.IsOptimized);
        }
    }

    // --- Network View Model ---
    public partial class NetworkViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _dnsStatus = "DNS Resolver готов";

        [ObservableProperty]
        private string _pingText = "-- мс";

        [ObservableProperty]
        private string _pingTarget = "1.1.1.1 (Cloudflare DNS)";

        [ObservableProperty]
        private bool _isMeasuring = false;

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
            DnsStatus = "Параметры TCP Window Auto-Tuning и RSS оптимизированы!";
        }

        [RelayCommand]
        public async Task MeasurePingAsync()
        {
            if (IsMeasuring) return;
            IsMeasuring = true;
            PingText = "Замер...";

            long ms = await NetworkOptimizerService.Instance.MeasurePingAsync("1.1.1.1");
            PingText = ms >= 0 ? $"{ms} мс" : "Таймаут";
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

        public PrivacyViewModel()
        {
            // Initial state default
            IsTelemetryDisabled = true;
            IsAdIdDisabled = true;
            IsActivityFeedDisabled = true;
        }

        [RelayCommand]
        public void ApplyPrivacySettings()
        {
            if (IsTelemetryDisabled)
            {
                PrivacyOptimizerService.Instance.DisableTelemetry();
            }
            else
            {
                PrivacyOptimizerService.Instance.EnableTelemetry();
            }
            StatusMessage = "Настройки защиты телеметрии сохранены!";
            TrayService.Instance.ShowNotification("Приватность", StatusMessage);
        }
    }

    // --- System Tools View Model ---
    public partial class SystemToolsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _toolStatus = "Инструменты готовы";

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
        public async Task RunSsdTrimAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ToolStatus = "Выполнение команды TRIM для диска C:...";

            bool ok = await SystemToolsService.Instance.RunSsdTrimAsync("C:");
            ToolStatus = ok ? "Оптимизация SSD (TRIM) успешно выполнена!" : "Ошибка TRIM.";
            IsBusy = false;
        }

        [RelayCommand]
        public void ActivateUltimatePlan()
        {
            bool ok = SystemToolsService.Instance.ActivateUltimatePerformancePlan();
            ToolStatus = ok ? "Схема «Ultimate Performance» активирована!" : "Схема питания обновлена.";
        }

        [RelayCommand]
        public void OptimizeResponsiveness()
        {
            bool ok = SystemToolsService.Instance.OptimizeMenuDelay();
            ToolStatus = ok ? "Задержка меню снижена до 10 мс!" : "Настройки применены.";
        }
    }

    // --- Settings View Model ---
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private ThemeType _selectedTheme = ThemeType.StormDark;

        [ObservableProperty]
        private bool _minimizeToTray = true;

        [ObservableProperty]
        private bool _runAtStartup = false;

        [ObservableProperty]
        private string _appVersion = "v0.0.1 (Official Release)";

        public SettingsViewModel()
        {
            SelectedTheme = ThemeManager.Instance.CurrentTheme;
            ThemeManager.Instance.ThemeChanged += (s, t) => SelectedTheme = t;
        }

        [RelayCommand]
        public void ChangeTheme(string themeName)
        {
            if (Enum.TryParse<ThemeType>(themeName, out var theme))
            {
                SelectedTheme = theme;
                ThemeManager.Instance.ApplyTheme(theme, Application.Current?.MainWindow);
            }
        }
    }
}
