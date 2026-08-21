using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StormSystemOptimizer.Models
{
    public enum BiosSettingCategory
    {
        Memory,         // Память и XMP/EXPO
        Graphics,       // Видеокарта и Resizable BAR
        Processor,      // Процессор и PBO/SpeedShift
        StoragePcie,    // Шина PCIe и NVMe Gen 4/5
        BootUefi,       // Загрузка UEFI и CSM
        Cooling         // Вентиляторы и кривые охлаждения
    }

    public partial class BiosSettingItem : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _category = "Память (XMP/EXPO)";

        [ObservableProperty]
        private string _recommendedValue = "Включено (Enabled)";

        [ObservableProperty]
        private string _currentStatus = "Рекомендуется включить";

        [ObservableProperty]
        private string _performanceImpact = "+15% к скорости памяти";

        [ObservableProperty]
        private string _safetyLevel = "100% Безопасно (WHQL/JEDEC)";

        [ObservableProperty]
        private string _explanation = string.Empty;

        [ObservableProperty]
        private string _menuPathAsus = string.Empty;

        [ObservableProperty]
        private string _menuPathMsi = string.Empty;

        [ObservableProperty]
        private string _menuPathGigabyte = string.Empty;

        [ObservableProperty]
        private string _menuPathAsrock = string.Empty;

        [ObservableProperty]
        private bool _isAppliedOrRecommended = true;

        public string ActiveBoardPath(string boardVendor)
        {
            string lower = (boardVendor ?? "").ToLowerInvariant();
            if (lower.Contains("asus") || lower.Contains("rog") || lower.Contains("tuf"))
                return MenuPathAsus;
            if (lower.Contains("msi") || lower.Contains("micro-star"))
                return MenuPathMsi;
            if (lower.Contains("gigabyte") || lower.Contains("aorus"))
                return MenuPathGigabyte;
            if (lower.Contains("asrock"))
                return MenuPathAsrock;
            return $"Раздел Advanced ➔ {Title}";
        }
    }
}
