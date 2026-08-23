using System;
using System.Collections.Generic;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StormSystemOptimizer.Models
{
    public partial class InstalledAppItem : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DisplayName { get; set; } = string.Empty;
        public string DisplayVersion { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string InstallDate { get; set; } = string.Empty;
        public string InstallLocation { get; set; } = string.Empty;
        public string UninstallString { get; set; } = string.Empty;
        public string QuietUninstallString { get; set; } = string.Empty;
        public double EstimatedSizeMb { get; set; } = 0;
        public string FormattedSize => EstimatedSizeMb >= 1024 ? $"{EstimatedSizeMb / 1024.0:F1} ГБ" : (EstimatedSizeMb > 0 ? $"{EstimatedSizeMb:F0} МБ" : "—");
        public string AppType { get; set; } = "Программа"; // Игра, Программа, Windows Store, Системное
        public string DisplayIconPath { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isSelected = false;

        public ImageSource? IconSource { get; set; }
        public bool HasIcon => IconSource != null;

        // Residual info after scan
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResidualStatusText))]
        private bool _isScanned = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResidualStatusText))]
        private int _residualFilesCount = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResidualStatusText))]
        private int _residualRegistryCount = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResidualStatusText))]
        private double _residualSizeMb = 0;

        public string ResidualStatusText
        {
            get
            {
                if (!IsScanned)
                {
                    return "🔍 Нажмите «Поиск следов» для сканирования хвостов в реестре и AppData";
                }
                if (ResidualFilesCount > 0 || ResidualRegistryCount > 0)
                {
                    return $"⚠️ Найдено {ResidualFilesCount} остаточных папок и {ResidualRegistryCount} ключей реестра ({ResidualSizeMb:F1} МБ)";
                }
                return "✅ Остаточные следы не обнаружены / полностью зачищены";
            }
        }

        public List<string> FoundFolders { get; set; } = new();
        public List<string> FoundRegistryKeys { get; set; } = new();

        public string TypeBadgeColor => AppType switch
        {
            "Игра" => "#C084FC",
            "Windows Store" => "#38BDF8",
            "Системное" => "#FB7185",
            _ => "#10B981"
        };

        public string TypeBadgeBg => AppType switch
        {
            "Игра" => "#26C084FC",
            "Windows Store" => "#2638BDF8",
            "Системное" => "#26FB7185",
            _ => "#2610B981"
        };
    }
}
