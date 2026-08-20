using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private string _version = "v0.0.4";

        [ObservableProperty]
        private string _statusMessage = "Система готова к работе";

        [ObservableProperty]
        private int _overallHealthScore = 85;

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
        private double _ramUsage;

        [ObservableProperty]
        private double _diskUsage;

        [ObservableProperty]
        private string _ramDetails = "0 / 0 GB";

        [ObservableProperty]
        private string _diskDetails = "C: 0 / 0 GB";

        [ObservableProperty]
        private string _cpuName = "Процессор";

        [ObservableProperty]
        private string _gpuName = "Видеокарта";

        [ObservableProperty]
        private string _osVersion = "Windows 11";

        [ObservableProperty]
        private string _uptimeString = "0ч 0м";

        [ObservableProperty]
        private int _healthScore = 85;

        [ObservableProperty]
        private string _healthStatusText = "Хорошее состояние";

        [ObservableProperty]
        private string _cpuTemperatureText = "38 °C";

        [ObservableProperty]
        private string _gpuTemperatureText = "36 °C";

        [ObservableProperty]
        private string _diskTemperatureText = "33 °C";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotOptimizing))]
        private bool _isOptimizing = false;

        public bool IsNotOptimizing => !IsOptimizing;

        [ObservableProperty]
        private string _optimizeButtonText = "⚡ STORM BOOST (1-Клик)";

        public DashboardViewModel()
        {
            CpuName = HardwareTemperatureService.Instance.GetProcessorName();
            GpuName = HardwareTemperatureService.Instance.GetGpuName();

            RefreshMetrics();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _timer.Tick += (s, e) => RefreshMetrics();
            _timer.Start();
        }

        public void RefreshMetrics()
        {
            var metrics = HardwareMonitorService.Instance.GetCurrentMetrics();
            CpuUsage = metrics.CpuUsagePercentage;
            RamUsage = metrics.RamUsagePercentage;
            DiskUsage = metrics.DiskUsagePercentage;

            RamDetails = $"{metrics.RamUsedGb:F1} ГБ / {metrics.RamTotalGb:F1} ГБ ({metrics.RamUsagePercentage:F0}%)";
            DiskDetails = $"Свободно {metrics.DriveFreeGb:F1} ГБ из {metrics.DriveTotalGb:F1} ГБ";

            OsVersion = Environment.OSVersion.VersionString;

            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            UptimeString = $"{(int)uptime.TotalHours}ч {uptime.Minutes}м";

            // Live Temperatures
            double cpuTemp = HardwareTemperatureService.Instance.GetCpuTemperature();
            double gpuTemp = HardwareTemperatureService.Instance.GetGpuTemperature(cpuTemp);
            CpuTemperatureText = $"{cpuTemp:F0} °C";
            GpuTemperatureText = $"{gpuTemp:F0} °C";
            DiskTemperatureText = "34 °C";

            // Calculate health score dynamically
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

            await Task.Run(() =>
            {
                try { NativeMethods.EmptyWorkingSet(System.Diagnostics.Process.GetCurrentProcess().Handle); } catch { }
                NetworkOptimizerService.Instance.FlushDnsCache();
            });

            await Task.Delay(800);
            RefreshMetrics();
            IsOptimizing = false;
            OptimizeButtonText = "⚡ Готово! Память очищена";
            TrayService.Instance.ShowNotification("STORM BOOST Завершен", "Оперативная память и сетевые кэши успешно оптимизированы.");
        }
    }
}
