using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Controls;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;
using StormSystemOptimizer.Themes;

namespace StormSystemOptimizer.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _appTitle = "STORM SYSTEM OPTIMIZER";

        [ObservableProperty]
        private string _version = "v0.1.0";

        [ObservableProperty]
        private string _statusMessage = "Система готова к работе";

        [ObservableProperty]
        private int _overallHealthScore = 95;

        [ObservableProperty]
        private ThemeType _currentTheme = ThemeType.StormDark;

        public MainViewModel()
        {
            CurrentTheme = ThemeManager.Instance.CurrentTheme;
            ThemeManager.Instance.ThemeChanged += (s, t) => CurrentTheme = t;
        }

        [RelayCommand]
        public void SetTheme(ThemeType theme)
        {
            ThemeManager.Instance.ApplyTheme(theme, Application.Current.MainWindow);
        }
    }

    public partial class DashboardViewModel : ObservableObject
    {
        private readonly DispatcherTimer _timer;

        [ObservableProperty]
        private double _cpuUsage;

        [ObservableProperty]
        private string _cpuUsageText = "0%";

        [ObservableProperty]
        private double _ramUsage;

        [ObservableProperty]
        private string _ramUsageText = "0%";

        [ObservableProperty]
        private double _diskUsage;

        [ObservableProperty]
        private string _diskUsageText = "0%";

        [ObservableProperty]
        private string _ramDetails = "0 / 0 ГБ";

        [ObservableProperty]
        private string _diskDetails = "C: 0 / 0 ГБ";

        [ObservableProperty]
        private string _systemDiskFreeText = "-- ГБ";

        [ObservableProperty]
        private string _systemDiskTotalText = "-- ГБ";

        [ObservableProperty]
        private string _cpuName = "Процессор";

        [ObservableProperty]
        private string _gpuName = "Видеокарта";

        [ObservableProperty]
        private string _motherboardName = "Материнская плата";

        [ObservableProperty]
        private string _computerName = "Компьютер";

        [ObservableProperty]
        private string _osVersion = "Windows 11";

        [ObservableProperty]
        private string _uptimeString = "0ч 0м";

        [ObservableProperty]
        private int _healthScore = 95;

        [ObservableProperty]
        private string _healthStatusText = "Отличное состояние";

        [ObservableProperty]
        private string _cpuTemperatureText = "35 °C";

        [ObservableProperty]
        private string _gpuTemperatureText = "34 °C";

        [ObservableProperty]
        private string _diskTemperatureText = "32 °C";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotOptimizing))]
        private bool _isOptimizing = false;

        public bool IsNotOptimizing => !IsOptimizing;

        [ObservableProperty]
        private string _optimizeButtonText = "⚡ Очистить RAM";

        [ObservableProperty]
        private bool _isGameBoostActive = false;

        [ObservableProperty]
        private string _gameBoostStatusText = "Игровой режим выключен";

        [ObservableProperty]
        private bool _isTimerResolutionActive = false;

        [ObservableProperty]
        private string _timerResolutionText = "Таймер Windows: 1.0 мс";

        public DashboardViewModel()
        {
            CpuName = HardwareTemperatureService.Instance.GetProcessorName();
            GpuName = HardwareTemperatureService.Instance.GetGpuName();
            ComputerName = Environment.MachineName + " / " + Environment.UserName;
            MotherboardName = "UEFI BIOS / ACPI x64";

            IsGameBoostActive = GameBoostService.Instance.IsGameBoostActive;
            IsTimerResolutionActive = GameBoostService.Instance.IsTimerResolutionEnabled;

            GameBoostService.Instance.GameBoostStateChanged += (active, text) =>
            {
                IsGameBoostActive = active;
                GameBoostStatusText = text;
            };

            RefreshMetrics();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _timer.Tick += (s, e) => RefreshMetrics();
            _timer.Start();

            // Start auto game detector & daemon
            GameBoostService.Instance.StartAutoGameDetection();
            SmartDaemonService.Instance.Start();
        }

        public void RefreshMetrics()
        {
            var metrics = HardwareMonitorService.Instance.GetCurrentMetrics();
            CpuUsage = metrics.CpuUsagePercentage;
            CpuUsageText = $"{CpuUsage:F0}%";

            RamUsage = metrics.RamUsagePercentage;
            RamUsageText = $"{RamUsage:F0}%";

            DiskUsage = metrics.DiskUsagePercentage;
            DiskUsageText = $"{DiskUsage:F0}%";

            RamDetails = $"{metrics.RamUsedGb:F1} ГБ / {metrics.RamTotalGb:F1} ГБ ({metrics.RamUsagePercentage:F0}%)";
            DiskDetails = $"Свободно {metrics.DriveFreeGb:F1} ГБ из {metrics.DriveTotalGb:F1} ГБ";
            SystemDiskFreeText = $"{metrics.DriveFreeGb:F1} ГБ свободно";
            SystemDiskTotalText = $"Общий объем: {metrics.DriveTotalGb:F1} ГБ";

            OsVersion = Environment.OSVersion.VersionString.Replace("Microsoft Windows NT ", "Windows ");

            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            UptimeString = $"{(int)uptime.TotalHours}ч {uptime.Minutes}м";

            // Live Temperatures
            double cpuTemp = HardwareTemperatureService.Instance.GetCpuTemperature();
            double gpuTemp = HardwareTemperatureService.Instance.GetGpuTemperature(cpuTemp);
            CpuTemperatureText = $"{cpuTemp:F0} °C";
            GpuTemperatureText = $"{gpuTemp:F0} °C";

            try
            {
                var diskTemps = HardwareTemperatureService.Instance.GetDiskTemperatures();
                if (diskTemps.Count > 0)
                {
                    DiskTemperatureText = $"{diskTemps[0].TemperatureCelsius:F0} °C";
                }
            }
            catch { }

            int score = 100;
            if (CpuUsage > 70) score -= 15;
            if (RamUsage > 75) score -= 15;
            if (DiskUsage > 85) score -= 15;
            if (cpuTemp > 75) score -= 15;
            HealthScore = Math.Max(50, score);
            HealthStatusText = HealthScore >= 80 ? "Отличное состояние" : "Требуется оптимизация";
        }

        [RelayCommand]
        public async Task QuickStormBoostAsync()
        {
            if (IsOptimizing) return;
            IsOptimizing = true;
            OptimizeButtonText = "Оптимизация...";

            var (count, freedMb) = await MemoryOptimizerService.Instance.SmartCompressMemoryAsync();
            NetworkOptimizerService.Instance.FlushDnsCache();

            await Task.Delay(400);
            RefreshMetrics();
            IsOptimizing = false;
            OptimizeButtonText = "⚡ Очистить RAM";
            TrayService.Instance.ShowNotification("Память оптимизирована ⚡", $"Освобождено {freedMb:F0} МБ памяти у {count} процессов. Standby List очищен!");
        }

        [RelayCommand]
        public void ToggleGameBoost()
        {
            if (IsGameBoostActive)
            {
                GameBoostService.Instance.DisableGameBoost();
                IsGameBoostActive = false;
                GameBoostStatusText = "Игровой режим выключен";
                TrayService.Instance.ShowNotification("STORM Game Boost", "Игровой режим выключен. Приоритеты сброшены.");
            }
            else
            {
                GameBoostService.Instance.SetHighResolutionTimer(true);
                GameBoostService.Instance.ApplyDwmLatencyTweaks();
                MemoryOptimizerService.Instance.PurgeStandbyList();
                IsGameBoostActive = true;
                GameBoostStatusText = "Игровой режим активен (P-Cores + 0.5ms Timer + DWM Low Latency)";
                TrayService.Instance.ShowNotification("STORM Game Boost ⚡", "Игровой режим активирован! Таймер 0.5 мс и DWM оптимизированы.");
            }
        }

        [RelayCommand]
        public void ToggleTimerResolution()
        {
            IsTimerResolutionActive = !IsTimerResolutionActive;
            GameBoostService.Instance.SetHighResolutionTimer(IsTimerResolutionActive);
            TimerResolutionText = IsTimerResolutionActive ? "Таймер Windows: 0.500 мс (Ultra Low Latency)" : "Таймер Windows: 1.000 мс (По умолчанию)";
            TrayService.Instance.ShowNotification("Таймер прерываний", TimerResolutionText);
        }

        [RelayCommand]
        public void ToggleHudOverlay()
        {
            StormOverlayWindow.Instance.ToggleVisibility();
        }

        [RelayCommand]
        public async Task ExportReportAsync()
        {
            string path = await SystemReportService.Instance.GenerateHtmlReportAsync();
            TrayService.Instance.ShowNotification("Отчет сгенерирован 📄", $"Диагностический отчет открыт в браузере и сохранен на Рабочий стол.");
        }
    }
}
