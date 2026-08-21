using System;
using System.Windows.Media;

namespace StormSystemOptimizer.Models
{
    public class SoftwareUpdateItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PackageId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string InstalledVersion { get; set; } = string.Empty;
        public string AvailableVersion { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string AppType { get; set; } = "Программа";
        public bool IsUpdateAvailable { get; set; } = false;
        public bool IsBlacklisted { get; set; } = false;

        public string TypeBadgeColor => AppType == "Игра" ? "#FB7185" : (AppType == "Windows Store" ? "#38BDF8" : "#00D2FF");
        public string TypeBadgeBg => AppType == "Игра" ? "#26FB7185" : (AppType == "Windows Store" ? "#2638BDF8" : "#2600D2FF");

        public ImageSource? IconSource { get; set; }
        public bool HasIcon => IconSource != null;

        public string StatusText => IsBlacklisted 
            ? "🔒 В черном списке (Игнорируется)" 
            : (IsUpdateAvailable ? $"⚡ Доступна v{AvailableVersion}" : "✅ Установлена последняя версия");

        public string StatusColor => IsBlacklisted ? "#64748B" : (IsUpdateAvailable ? "#F59E0B" : "#10B981");
        public string StatusBgColor => IsBlacklisted ? "#1E293B" : (IsUpdateAvailable ? "#26F59E0B" : "#2610B981");
    }
}
