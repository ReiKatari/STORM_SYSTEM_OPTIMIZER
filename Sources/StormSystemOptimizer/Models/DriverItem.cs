using System;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StormSystemOptimizer.Models
{
    public partial class DriverItem : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DeviceName { get; set; } = string.Empty;
        public string Category { get; set; } = "Видеокарта"; // Видеокарта, Сеть, Звук, Накопители, Чипсет, USB
        public string ProviderName { get; set; } = "Microsoft";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private string _currentVersion = "1.0.0.0";

        public string DriverDate { get; set; } = "2024-01-01";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private string _latestVersion = "1.0.0.0";

        public string InfName { get; set; } = string.Empty;
        public string HardwareId { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        [NotifyPropertyChangedFor(nameof(StatusColor))]
        [NotifyPropertyChangedFor(nameof(StatusBgColor))]
        private bool _isUpdateAvailable = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        [NotifyPropertyChangedFor(nameof(StatusColor))]
        [NotifyPropertyChangedFor(nameof(StatusBgColor))]
        private bool _isUpdating = false;

        [ObservableProperty]
        private int _updateProgress = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private string _updateProgressText = "Подготовка...";

        [ObservableProperty]
        private string _statusDescription = string.Empty;

        public string StatusText => IsUpdating
            ? $"⚡ {UpdateProgressText}"
            : (IsUpdateAvailable ? "⚡ Доступно обновление" : "✅ Актуален (WHQL)");

        public string StatusColor => IsUpdating ? "#00D2FF" : (IsUpdateAvailable ? "#F59E0B" : "#10B981");
        public string StatusBgColor => IsUpdating ? "#2600D2FF" : (IsUpdateAvailable ? "#26F59E0B" : "#2610B981");
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

        public DriverItem Clone()
        {
            return new DriverItem
            {
                Id = Id,
                DeviceName = DeviceName,
                Category = Category,
                ProviderName = ProviderName,
                CurrentVersion = CurrentVersion,
                DriverDate = DriverDate,
                LatestVersion = LatestVersion,
                InfName = InfName,
                HardwareId = HardwareId,
                IsUpdateAvailable = IsUpdateAvailable,
                IsUpdating = IsUpdating,
                UpdateProgress = UpdateProgress,
                UpdateProgressText = UpdateProgressText,
                StatusDescription = StatusDescription,
                DownloadUrl = DownloadUrl,
                IconSource = IconSource
            };
        }
    }
}
