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
                if (ReclaimableBytes <= 0) return string.Empty;
                if (ReclaimableBytes < 1024) return $"{ReclaimableBytes} Б";
                if (ReclaimableBytes < 1024 * 1024) return $"{ReclaimableBytes / 1024.0:F1} КБ";
                if (ReclaimableBytes < 1024 * 1024 * 1024) return $"{ReclaimableBytes / (1024.0 * 1024.0):F1} МБ";
                return $"{ReclaimableBytes / (1024.0 * 1024.0 * 1024.0):F2} ГБ";
            }
        }
    }
}
