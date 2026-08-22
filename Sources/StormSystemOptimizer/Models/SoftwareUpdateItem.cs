using System;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StormSystemOptimizer.Models
{
    public partial class SoftwareUpdateItem : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PackageId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        [NotifyPropertyChangedFor(nameof(StatusColor))]
        [NotifyPropertyChangedFor(nameof(StatusBgColor))]
        private string _installedVersion = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        [NotifyPropertyChangedFor(nameof(StatusColor))]
        [NotifyPropertyChangedFor(nameof(StatusBgColor))]
        private string _availableVersion = string.Empty;

        public string Publisher { get; set; } = string.Empty;
        public string AppType { get; set; } = "Программа";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        [NotifyPropertyChangedFor(nameof(StatusColor))]
        [NotifyPropertyChangedFor(nameof(StatusBgColor))]
        private bool _isUpdateAvailable = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        [NotifyPropertyChangedFor(nameof(StatusColor))]
        [NotifyPropertyChangedFor(nameof(StatusBgColor))]
        private bool _isBlacklisted = false;

        [ObservableProperty]
        private bool _isUpdating = false;

        [ObservableProperty]
        private int _updateProgress = 0;

        [ObservableProperty]
        private string _updateProgressText = "Обновление...";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        [NotifyPropertyChangedFor(nameof(StatusColor))]
        [NotifyPropertyChangedFor(nameof(StatusBgColor))]
        private bool _isBeta = false;

        public string TypeBadgeColor => AppType == "Игра" ? "#FB7185" : (AppType == "Windows Store" ? "#38BDF8" : "#00D2FF");
        public string TypeBadgeBg => AppType == "Игра" ? "#26FB7185" : (AppType == "Windows Store" ? "#2638BDF8" : "#2600D2FF");

        public ImageSource? IconSource { get; set; }
        public bool HasIcon => IconSource != null;

        public string StatusText => IsBlacklisted 
            ? "🔒 В черном списке (Игнорируется)" 
            : (IsUpdateAvailable ? (IsBeta ? $"🧪 Доступна Beta v{AvailableVersion}" : $"⚡ Доступна v{AvailableVersion}") : "✅ Актуальна (v" + InstalledVersion + ")");

        public string StatusColor => IsBlacklisted ? "#64748B" : (IsUpdateAvailable ? (IsBeta ? "#C084FC" : "#F59E0B") : "#10B981");
        public string StatusBgColor => IsBlacklisted ? "#1E293B" : (IsUpdateAvailable ? (IsBeta ? "#26C084FC" : "#26F59E0B") : "#2610B981");
    }
}
