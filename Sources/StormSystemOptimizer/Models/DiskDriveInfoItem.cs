using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StormSystemOptimizer.Models
{
    public partial class DiskDriveInfoItem : ObservableObject
    {
        [ObservableProperty]
        private string _volumeLetter = "C:";

        [ObservableProperty]
        private string _volumeLabel = "Локальный диск";

        [ObservableProperty]
        private string _model = "SSD Накопитель";

        [ObservableProperty]
        private string _mediaType = "NVMe SSD";

        [ObservableProperty]
        private string _interfaceType = "NVMe / PCIe";

        [ObservableProperty]
        private string _fileSystem = "NTFS";

        [ObservableProperty]
        private double _totalSizeGb = 0;

        [ObservableProperty]
        private double _usedSizeGb = 0;

        [ObservableProperty]
        private double _freeSizeGb = 0;

        [ObservableProperty]
        private double _usedPercentage = 0;

        [ObservableProperty]
        private int _healthPercentage = 100;

        [ObservableProperty]
        private string _healthStatus = "Исправен 100%";

        [ObservableProperty]
        private string _statusColor = "#10B981";

        [ObservableProperty]
        private string _statusBgColor = "#2610B981";

        [ObservableProperty]
        private string _temperature = "34 °C";

        [ObservableProperty]
        private bool _isSsd = true;

        [ObservableProperty]
        private string _fragmentationStatus = "Готов к анализу";

        public string DriveType => MediaType;
        public string FreeSpaceText => $"{FreeSizeGb:F1} ГБ";
        public string TotalSizeText => $"{TotalSizeGb:F1} ГБ";
        public double UsedPercent => UsedPercentage;
        public string UsedPercentText => $"{UsedPercentage:F0}%";
        public string SpaceUsageSummary => $"{UsedSizeGb:F1} ГБ / {TotalSizeGb:F1} ГБ ({UsedPercentage:F0}%)";
        public string FreePercentSummary => $"Свободно {FreeSizeGb:F1} ГБ ({Math.Max(0, 100 - UsedPercentage):F0}%)";
        public string FragmentationSummary => FragmentationStatus;
        public string OptimizationActionName => IsSsd ? "⚡ TRIM Оптимизация" : "⚡ Дефрагментация";
        public string FormattedTotal => $"{TotalSizeGb:F1} ГБ";
        public string FormattedFree => $"{FreeSizeGb:F1} ГБ свободно";
    }
}
