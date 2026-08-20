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
        private bool _isEnabled = true;

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
        private string _status = "Работает";

        [ObservableProperty]
        private string _startupType = "Автоматически";

        [ObservableProperty]
        private bool _isOptimized = false;

        [ObservableProperty]
        private bool _isSafeToDisable = true;

        [ObservableProperty]
        private string _recommendedAction = "Отключить";

        public string StatusColor => Status switch
        {
            "Работает" => "#10B981",
            "Отключено" => "#EF4444",
            _ => "#94A3B8"
        };

        public string StatusBgColor => Status switch
        {
            "Работает" => "#2610B981",
            "Отключено" => "#26EF4444",
            _ => "#2694A3B8"
        };
    }

    public class SystemMetrics
    {
        public double CpuUsagePercentage { get; set; }
        public double RamUsagePercentage { get; set; }
        public double RamTotalGb { get; set; }
        public double RamUsedGb { get; set; }
        public double RamAvailableGb { get; set; }
        public double RamStandbyGb { get; set; }
        public double DiskUsagePercentage { get; set; }
        public string PrimaryDrive { get; set; } = "C:";
        public double DriveFreeGb { get; set; }
        public double DriveTotalGb { get; set; }
        public string OsVersion { get; set; } = string.Empty;
        public string ProcessorName { get; set; } = string.Empty;
        public string GpuName { get; set; } = string.Empty;
        public TimeSpan SystemUptime { get; set; }
    }

    public class ScanSummary
    {
        public int TotalIssuesFound { get; set; }
        public int SafeIssuesCount { get; set; }
        public int RecommendedIssuesCount { get; set; }
        public int AdvancedIssuesCount { get; set; }
        public long TotalReclaimableBytes { get; set; }
        public int SystemHealthScore { get; set; } = 65;
        public DateTime ScanCompletedAt { get; set; } = DateTime.Now;

        public string FormattedTotalSize
        {
            get
            {
                if (TotalReclaimableBytes <= 0) return "0 МБ";
                if (TotalReclaimableBytes < 1024 * 1024) return $"{TotalReclaimableBytes / 1024.0:F1} КБ";
                if (TotalReclaimableBytes < 1024 * 1024 * 1024) return $"{TotalReclaimableBytes / (1024.0 * 1024.0):F1} МБ";
                return $"{TotalReclaimableBytes / (1024.0 * 1024.0 * 1024.0):F2} ГБ";
            }
        }
    }
}
