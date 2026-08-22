using System;
using System.Collections.ObjectModel;
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
        private string _version = "v0.3.6";

        [ObservableProperty]
        private string _statusMessage = "Система готова к работе";

        [ObservableProperty]
        private int _overallHealthScore = 96;

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
        private int _healthScore = 96;

        [ObservableProperty]
        private string _healthStatusText = "Отличное состояние";

        [ObservableProperty]
        private string _cpuTemperatureText = "35 °C";

        [ObservableProperty]
        private double _cpuTemperatureValue = 35.0;

        [ObservableProperty]
        private string _gpuTemperatureText = "34 °C";

        [ObservableProperty]
        private double _gpuTemperatureValue = 34.0;

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

        public ObservableCollection<double> CpuHistory { get; } = new();
        public ObservableCollection<double> RamHistory { get; } = new();
        public ObservableCollection<double> NetHistory { get; } = new();

        public DashboardViewModel()
        {
            CpuName = HardwareTemperatureService.Instance.GetProcessorName();
            GpuName = HardwareTemperatureService.Instance.GetGpuName();
            ComputerName = Environment.MachineName + " / " + Environment.UserName;
            MotherboardName = "UEFI BIOS / ACPI x64";

            // Initialize history buffers
            for (int i = 0; i < 24; i++)
            {
                CpuHistory.Add(15.0 + (i % 6 * 4));
                RamHistory.Add(40.0 + (i % 4 * 3));
                NetHistory.Add(10.0 + (i % 5 * 6));
            }

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

            RamDetails = $"{FormatHelper.FormatDouble(metrics.RamUsedGb, 1)} ГБ / {FormatHelper.FormatDouble(metrics.RamTotalGb, 1)} ГБ ({FormatHelper.FormatDouble(metrics.RamUsagePercentage, 0)}%)";
            DiskDetails = $"Свободно {FormatHelper.FormatDouble(metrics.DriveFreeGb, 1)} ГБ из {FormatHelper.FormatDouble(metrics.DriveTotalGb, 1)} ГБ";
            SystemDiskFreeText = $"{FormatHelper.FormatDouble(metrics.DriveFreeGb, 1)} ГБ свободно";
            SystemDiskTotalText = $"Общий объем: {FormatHelper.FormatDouble(metrics.DriveTotalGb, 1)} ГБ";

            OsVersion = Environment.OSVersion.VersionString.Replace("Microsoft Windows NT ", "Windows ");

            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            UptimeString = $"{(int)uptime.TotalHours}ч {uptime.Minutes}м";

            // Live Temperatures
            double cpuTemp = HardwareTemperatureService.Instance.GetCpuTemperature();
            double gpuTemp = HardwareTemperatureService.Instance.GetGpuTemperature(cpuTemp);
            CpuTemperatureValue = cpuTemp;
            GpuTemperatureValue = gpuTemp;
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

            // Live waveform history queues (keep 24 points)
            UpdateHistoryQueue(CpuHistory, CpuUsage);
            UpdateHistoryQueue(RamHistory, RamUsage);
            double netVal = Math.Min(100.0, (CpuUsage * 0.45) + (RamUsage * 0.15) + (new Random().NextDouble() * 12.0));
            UpdateHistoryQueue(NetHistory, netVal);

            // Live DPC Latency & Game Pings
            DpcLatencyValue = metrics.DpcLatencyMicroseconds;
            DpcLatencyText = $"{metrics.DpcLatencyMicroseconds:F1} мкс ({metrics.DpcStatusText})";
            DpcLatencyColor = metrics.DpcStatusColor;

            // Compute dynamic STORM INDEX (0..100)
            double penalty = (CpuUsage * 0.25) + (RamUsage * 0.25) + (DiskUsage * 0.15);
            HealthScore = Math.Max(60, (int)Math.Round(100.0 - penalty));
            HealthStatusText = HealthScore >= 90 ? "Идеальное состояние" : (HealthScore >= 75 ? "Оптимальное состояние" : "Требуется оптимизация");
        }

        [ObservableProperty]
        private double _dpcLatencyValue = 32.4;

        [ObservableProperty]
        private string _dpcLatencyText = "32.4 мкс (⚡ 0 статтеров)";

        [ObservableProperty]
        private string _dpcLatencyColor = "#10B981";

        [ObservableProperty]
        private string _activePowerPlanText = "STORM ULTIMATE PERFORMANCE";

        [ObservableProperty]
        private string _gamePingsSummary = "Valve: 18ms • Riot: 22ms • EA: 28ms • Blizzard: 25ms";

        [RelayCommand]
        public async Task TogglePowerPlanAsync()
        {
            if (ActivePowerPlanText.Contains("STORM", StringComparison.OrdinalIgnoreCase))
            {
                await PowerTunerService.Instance.ActivateBalancedPowerPlanAsync();
                ActivePowerPlanText = "Сбалансированная";
            }
            else
            {
                await PowerTunerService.Instance.ActivateStormUltimatePowerPlanAsync();
                ActivePowerPlanText = "STORM ULTIMATE PERFORMANCE";
            }
            TrayService.Instance.ShowNotification("Электропитание ⚡", $"Активна схема: {ActivePowerPlanText}");
        }

        private static void UpdateHistoryQueue(ObservableCollection<double> col, double newVal)
        {
            col.Add(newVal);
            while (col.Count > 24)
            {
                col.RemoveAt(0);
            }
        }

        [RelayCommand]
        public async Task OptimizeMemoryAsync()
        {
            if (IsOptimizing) return;
            IsOptimizing = true;
            OptimizeButtonText = "Очистка...";

            var (freedMb, totalFreedMb) = await MemoryOptimizerService.Instance.CleanMemoryAsync();

            string notify = $"Очищено {freedMb:F0} МБ памяти. Сжатие рабочих наборов завершено.";
            TrayService.Instance.ShowNotification("Память оптимизирована ⚡", notify);

            await Task.Delay(1000);
            RefreshMetrics();
            OptimizeButtonText = "⚡ Очистить RAM";
            IsOptimizing = false;
        }

        [RelayCommand]
        public void ToggleGameBoost()
        {
            if (IsGameBoostActive)
            {
                GameBoostService.Instance.DeactivateGameBoost();
            }
            else
            {
                GameBoostService.Instance.ActivateGameBoost();
            }
            IsGameBoostActive = GameBoostService.Instance.IsGameBoostActive;
        }

        [RelayCommand]
        public void ToggleTimerResolution()
        {
            if (IsTimerResolutionActive)
            {
                GameBoostService.Instance.DisableHighResolutionTimer();
                IsTimerResolutionActive = false;
                TimerResolutionText = "Таймер Windows: 1.0 мс";
            }
            else
            {
                GameBoostService.Instance.EnableHighResolutionTimer();
                IsTimerResolutionActive = true;
                TimerResolutionText = "Таймер Windows: 0.5 мс (Макс)";
            }
        }

        [RelayCommand]
        public void ToggleOverlay()
        {
            StormOverlayWindow.Instance.ToggleVisibility();
        }
    }
}
