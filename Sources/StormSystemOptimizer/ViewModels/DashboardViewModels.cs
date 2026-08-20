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
        private string _version = "v0.0.2";

        [ObservableProperty]
        private string _statusMessage = "Система готова к работе";

        [ObservableProperty]
        private int _overallHealthScore = 78;

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
        private int _healthScore = 78;

        [ObservableProperty]
        private string _healthStatusText = "Хорошее состояние";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotOptimizing))]
        private bool _isOptimizing = false;

        public bool IsNotOptimizing => !IsOptimizing;

        [ObservableProperty]
        private string _optimizeButtonText = "⚡ STORM BOOST (1-Клик)";

        public DashboardViewModel()
        {
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

            CpuName = metrics.ProcessorName;
            GpuName = metrics.GpuName;
            OsVersion = metrics.OsVersion;
            UptimeString = $"{metrics.SystemUptime.Days * 24 + metrics.SystemUptime.Hours}ч {metrics.SystemUptime.Minutes}м";

            double penalty = (RamUsage > 75 ? 15 : 0) + (CpuUsage > 50 ? 10 : 0) + (DiskUsage > 85 ? 15 : 0);
            HealthScore = Math.Clamp((int)(100 - penalty), 40, 100);
            HealthStatusText = HealthScore >= 85 ? "Отличное состояние" : (HealthScore >= 70 ? "Требуется оптимизация" : "Система перегружена");
        }

        [RelayCommand]
        public async Task QuickStormBoostAsync()
        {
            if (IsOptimizing) return;
            IsOptimizing = true;
            OptimizeButtonText = "Очистка памяти и кэша...";

            await Task.Delay(400);
            long freed = await Task.Run(() =>
            {
                long bytes = OptimizationEngine.Instance.PurgeSystemWorkingSetMemory();
                NetworkOptimizerService.Instance.FlushDnsCache();
                return bytes;
            });

            await Task.Delay(500);
            RefreshMetrics();

            string freedMb = (freed / (1024.0 * 1024.0)).ToString("F0");
            OptimizeButtonText = $"Готово! Освобождено ~{freedMb} МБ";

            TrayService.Instance.ShowNotification("STORM BOOST завершен!", $"Успешно освобождено ~{freedMb} МБ оперативной памяти и сброшен кэш DNS.");

            await Task.Delay(2500);
            OptimizeButtonText = "⚡ STORM BOOST (1-Клик)";
            IsOptimizing = false;
        }
    }
}
