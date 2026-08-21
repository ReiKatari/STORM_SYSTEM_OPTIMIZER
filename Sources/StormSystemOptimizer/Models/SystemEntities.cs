using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StormSystemOptimizer.Models
{
    public partial class StartupEntry : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _publisher = "Неизвестно";

        [ObservableProperty]
        private string _command = string.Empty;

        [ObservableProperty]
        private string _location = string.Empty;

        [ObservableProperty]
        private string _impact = "Низкое";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StateText))]
        private bool _isEnabled = true;

        public string StateText => IsEnabled ? "Активно" : "Отключено";

        [ObservableProperty]
        private string _registryPath = string.Empty;

        public string ImpactTextColor => Impact switch
        {
            "Высокое" => "#EF4444",
            "Среднее" => "#F59E0B",
            "Низкое" => "#10B981",
            _ => "#94A3B8"
        };

        public string ImpactBgColor => Impact switch
        {
            "Высокое" => "#26EF4444",
            "Среднее" => "#26F59E0B",
            "Низкое" => "#2610B981",
            _ => "#2694A3B8"
        };
    }

    public partial class ServiceEntry : ObservableObject
    {
        [ObservableProperty]
        private string _serviceName = string.Empty;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusColor))]
        [NotifyPropertyChangedFor(nameof(StatusBgColor))]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private string _status = "Работает";

        [ObservableProperty]
        private string _startupType = "Автоматически";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OptimizationStateText))]
        private bool _isOptimized = false;

        partial void OnIsOptimizedChanged(bool value)
        {
            if (value)
            {
                Status = "Отключена";
                StartupType = "Отключено";
            }
            else
            {
                Status = "Работает";
                StartupType = "Вручную";
            }
        }

        public string OptimizationStateText => IsOptimized ? "Оптимизирована" : "По умолчанию";

        [ObservableProperty]
        private bool _isSafeToDisable = true;

        [ObservableProperty]
        private string _recommendedAction = "Отключить";

        public string StatusText => Status;

        public string StatusColor => Status switch
        {
            "Работает" => "#10B981",
            "Остановлена" => "#F59E0B",
            "Отключено" => "#EF4444",
            "Отключена" => "#EF4444",
            _ => "#94A3B8"
        };

        public string StatusBgColor => Status switch
        {
            "Работает" => "#2610B981",
            "Остановлена" => "#26F59E0B",
            "Отключено" => "#26EF4444",
            "Отключена" => "#26EF4444",
            _ => "#2694A3B8"
        };
    }

    public class SystemMetrics
    {
        public double CpuUsagePercentage { get; set; }
        public double RamUsagePercentage { get; set; }
        public double RamTotalGb { get; set; }
        public double TotalRamGb { get => RamTotalGb; set => RamTotalGb = value; }
        public double RamUsedGb { get; set; }
        public double RamAvailableGb { get; set; }
        public double FreeRamGb { get => RamAvailableGb; set => RamAvailableGb = value; }
        public double RamStandbyGb { get; set; }
        public string PrimaryDrive { get; set; } = "C:\\";
        public double DriveTotalGb { get; set; }
        public double TotalDiskGb { get => DriveTotalGb; set => DriveTotalGb = value; }
        public double DriveFreeGb { get; set; }
        public double FreeDiskGb { get => DriveFreeGb; set => DriveFreeGb = value; }
        public double DiskUsagePercentage { get; set; }
        public double DiskReadSpeedMbps { get; set; }
        public double DiskWriteSpeedMbps { get; set; }
        public string OperatingSystem { get; set; } = string.Empty;
        public string OsVersion { get => OperatingSystem; set => OperatingSystem = value; }
        public string CpuName { get; set; } = string.Empty;
        public string ProcessorName { get => CpuName; set => CpuName = value; }
        public string GpuName { get; set; } = string.Empty;
        public TimeSpan SystemUptime { get; set; }
    }

    public class CoreMetricItem
    {
        public int CoreIndex { get; set; }
        public double LoadPercentage { get; set; }
        public string CoreName => $"Ядро #{CoreIndex + 1}";
        public string LoadText => $"{LoadPercentage:F0}%";
        public string CoreColor => LoadPercentage > 80 ? "#EF4444" : (LoadPercentage > 50 ? "#F59E0B" : "#10B981");
    }
}
