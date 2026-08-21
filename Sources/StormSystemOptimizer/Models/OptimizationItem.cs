using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StormSystemOptimizer.Models
{
    public partial class OptimizationItem : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private OptimizationCategory _category;

        [ObservableProperty]
        private RiskLevel _riskLevel = RiskLevel.Safe;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedSize))]
        private long _reclaimableBytes;

        [ObservableProperty]
        private string _formattedDetails = string.Empty;

        [ObservableProperty]
        private bool _isSelected = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotFixed))]
        [NotifyPropertyChangedFor(nameof(FormattedSize))]
        private bool _isFixed = false;

        public bool IsNotFixed => !IsFixed;

        [ObservableProperty]
        private bool _isFixing = false;

        [ObservableProperty]
        private string _statusText = "Требует внимания";

        public string CategoryName => Category switch
        {
            OptimizationCategory.JunkAndCache => "Системный мусор и кэш",
            OptimizationCategory.MemoryRam => "Оперативная память",
            OptimizationCategory.StartupApps => "Автозагрузка",
            OptimizationCategory.WindowsServices => "Фоновые службы",
            OptimizationCategory.NetworkAndDns => "Сеть и DNS",
            OptimizationCategory.PrivacyTelemetry => "Приватность и телеметрия",
            OptimizationCategory.SystemHealth => "Здоровье системы и дисков",
            OptimizationCategory.PowerAndVisual => "Электропитание и визуальные настройки",
            _ => "Оптимизация"
        };

        public string CategoryTextColor => Category switch
        {
            OptimizationCategory.JunkAndCache => "#00D2FF",
            OptimizationCategory.MemoryRam => "#C084FC",
            OptimizationCategory.StartupApps => "#FBBF24",
            OptimizationCategory.WindowsServices => "#34D399",
            OptimizationCategory.NetworkAndDns => "#38BDF8",
            OptimizationCategory.PrivacyTelemetry => "#FB7185",
            OptimizationCategory.SystemHealth => "#10B981",
            _ => "#00D2FF"
        };

        public string CategoryBgColor => Category switch
        {
            OptimizationCategory.JunkAndCache => "#1A00D2FF",
            OptimizationCategory.MemoryRam => "#1AC084FC",
            OptimizationCategory.StartupApps => "#1AFBBF24",
            OptimizationCategory.WindowsServices => "#1A34D399",
            OptimizationCategory.NetworkAndDns => "#1A38BDF8",
            OptimizationCategory.PrivacyTelemetry => "#1AFB7185",
            OptimizationCategory.SystemHealth => "#1A10B981",
            _ => "#1A00D2FF"
        };

        public string RiskBadgeText => RiskLevel switch
        {
            RiskLevel.Safe => "100% БЕЗОПАСНО",
            RiskLevel.Recommended => "РЕКОМЕНДУЕТСЯ",
            RiskLevel.Advanced => "ПРОДВИНУТЫЙ",
            _ => "БЕЗОПАСНО"
        };

        public string FormattedSize
        {
            get
            {
                if (IsFixed) return "0 Б (Очищено)";
                if (ReclaimableBytes <= 0) return "Оптимизация";
                if (ReclaimableBytes < 1024) return $"{ReclaimableBytes} Б";
                if (ReclaimableBytes < 1024 * 1024) return $"{ReclaimableBytes / 1024.0:F1} КБ";
                if (ReclaimableBytes < 1024 * 1024 * 1024) return $"{ReclaimableBytes / (1024.0 * 1024.0):F1} МБ";
                return $"{ReclaimableBytes / (1024.0 * 1024.0 * 1024.0):F2} ГБ";
            }
        }
    }
}
