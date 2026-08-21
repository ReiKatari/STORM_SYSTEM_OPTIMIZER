using System;
using System.Windows.Media;

namespace StormSystemOptimizer.Models
{
    public class ProcessThermalImpactItem
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string TargetComponent { get; set; } = "GPU"; // GPU or CPU
        public double UsagePercentage { get; set; }
        public double EstimatedHeatAddedC { get; set; }
        public string ThermalStatus { get; set; } = "Умеренный";
        public string StatusColor { get; set; } = "#10B981";
        public string StatusBgColor { get; set; } = "#1A10B981";
        public string FormattedHeat => $"+{EstimatedHeatAddedC:F1} °C";
        public string FormattedUsage => $"{UsagePercentage:F0}%";
        public ImageSource? IconSource { get; set; }
        public bool HasIcon => IconSource != null;
        public string ProcessIcon => TargetComponent == "GPU" ? "🎮" : "⚡";
        public string FormattedDescription => TargetComponent == "GPU" 
            ? $"3D шейдеры & VRAM нагрузка ({FormattedUsage})" 
            : $"Многопоточный расчет ядра ({FormattedUsage})";
    }
}
