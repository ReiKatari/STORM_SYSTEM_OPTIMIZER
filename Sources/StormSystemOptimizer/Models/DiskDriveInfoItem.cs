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
        private string _healthStatusText = "100% Отличное (S.M.A.R.T. OK)";

        [ObservableProperty]
        private string _healthColor = "#10B981";

        [ObservableProperty]
        private string _temperatureText = "32 °C";

        [ObservableProperty]
        private bool _isSsd = true;

        [ObservableProperty]
        private string _fragmentationStatus = "Анализ не проводился";

        public string FormattedTotal => $"{TotalSizeGb:F1} ГБ";
        public string FormattedFree => $"{FreeSizeGb:F1} ГБ свободно";
        public string FormattedUsed => $"{UsedSizeGb:F1} ГБ занято";
        public string FormattedUsageText => $"{UsedPercentage:F0}% заполнено";
    }
}
