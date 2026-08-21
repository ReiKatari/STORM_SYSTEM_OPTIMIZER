using System;
using CommunityToolkit.Mvvm.ComponentModel;
using StormSystemOptimizer.Services;

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
        private string _fragmentationStatus = "0% (Оптимально)";

        // Deep Analysis & Live Optimization properties
        [ObservableProperty]
        private bool _hasAnalysisReport = false;

        [ObservableProperty]
        private string _clusterSizeText = "4 096 байт";

        [ObservableProperty]
        private long _fragmentedFilesCount = 0;

        [ObservableProperty]
        private long _totalFragmentsCount = 0;

        [ObservableProperty]
        private string _largestFreeBlockText = "120.5 ГБ";

        [ObservableProperty]
        private string _analysisRecommendation = "Том полностью оптимизирован";

        [ObservableProperty]
        private bool _isAnalyzing = false;

        [ObservableProperty]
        private bool _isOptimizing = false;

        [ObservableProperty]
        private string _currentOperationStatus = "";

        [ObservableProperty]
        private double _operationProgress = 0;

        public bool IsRunningOperation => IsAnalyzing || IsOptimizing;

        public string DriveType => MediaType;
        public string FreeSpaceText => $"{FormatHelper.FormatDouble(FreeSizeGb, 1)} ГБ";
        public string TotalSizeText => $"{FormatHelper.FormatDouble(TotalSizeGb, 1)} ГБ";
        public double UsedPercent => UsedPercentage;
        public string UsedPercentText => $"{FormatHelper.FormatDouble(UsedPercentage, 0)}%";
        public string SpaceUsageSummary => $"{FormatHelper.FormatDouble(UsedSizeGb, 1)} ГБ / {FormatHelper.FormatDouble(TotalSizeGb, 1)} ГБ ({FormatHelper.FormatDouble(UsedPercentage, 0)}%)";
        public string FreePercentSummary => $"Свободно {FormatHelper.FormatDouble(FreeSizeGb, 1)} ГБ ({FormatHelper.FormatDouble(Math.Max(0, 100 - UsedPercentage), 0)}%)";
        public string FragmentationSummary => FragmentationStatus;
        public string OptimizationActionName => IsSsd ? "TRIM Оптимизация" : "Дефрагментация";
        public string FormattedTotal => $"{FormatHelper.FormatDouble(TotalSizeGb, 1)} ГБ";
        public string FormattedFree => $"{FormatHelper.FormatDouble(FreeSizeGb, 1)} ГБ свободно";
        public string FormattedFragmentedFiles => $"{FormatHelper.FormatInt(FragmentedFilesCount)} файлов";
        public string FormattedTotalFragments => $"{FormatHelper.FormatInt(TotalFragmentsCount)} фрагментов";
    }
}
