using System;
using System.Windows.Media;

namespace StormSystemOptimizer.Models
{
    public class DriverItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DeviceName { get; set; } = string.Empty;
        public string Category { get; set; } = "Видеокарта"; // Видеокарта, Сеть, Звук, Накопители, Чипсет, USB
        public string ProviderName { get; set; } = "Microsoft";
        public string CurrentVersion { get; set; } = "1.0.0.0";
        public string DriverDate { get; set; } = "2024-01-01";
        public string LatestVersion { get; set; } = "1.0.0.0";
        public string InfName { get; set; } = string.Empty;
        public string HardwareId { get; set; } = string.Empty;
        public bool IsUpdateAvailable { get; set; } = false;
        public string StatusText => IsUpdateAvailable ? "⚡ Доступно обновление" : "✅ Актуален (WHQL)";
        public string StatusColor => IsUpdateAvailable ? "#F59E0B" : "#10B981";
        public string StatusBgColor => IsUpdateAvailable ? "#26F59E0B" : "#2610B981";
        public string DownloadUrl { get; set; } = string.Empty;

        public ImageSource? IconSource { get; set; }

        public string CategoryIcon => Category switch
        {
            "Видеокарта" => "🎮",
            "Сеть" => "🌐",
            "Звук" => "🔊",
            "Накопители" => "💾",
            "USB" => "🔌",
            _ => "⚡"
        };
    }
}
