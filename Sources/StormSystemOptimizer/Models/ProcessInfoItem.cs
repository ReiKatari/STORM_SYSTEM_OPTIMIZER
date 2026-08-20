using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StormSystemOptimizer.Models
{
    public enum ProcessSafetyStatus
    {
        SafeToKill,        // 🟢 Безопасно завершить (фоновые лаунчеры, блоатваре, трекеры)
        UserApp,           // 🟡 Пользовательская программа
        CriticalSystem     // 🔴 Системный процесс Windows (защищен)
    }

    public partial class ProcessInfoItem : ObservableObject
    {
        [ObservableProperty]
        private int _processId;

        [ObservableProperty]
        private string _processName = string.Empty;

        [ObservableProperty]
        private string _windowTitle = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _publisher = string.Empty;

        [ObservableProperty]
        private double _cpuPercentage;

        [ObservableProperty]
        private double _memoryMegabytes;

        [ObservableProperty]
        private int _threadsCount;

        [ObservableProperty]
        private string _executablePath = string.Empty;

        [ObservableProperty]
        private ProcessSafetyStatus _safetyStatus = ProcessSafetyStatus.UserApp;

        [ObservableProperty]
        private string _recommendationText = string.Empty;

        [ObservableProperty]
        private bool _isSelected = false;

        public string FormattedMemory => MemoryMegabytes >= 1024
            ? $"{MemoryMegabytes / 1024.0:F2} ГБ"
            : $"{MemoryMegabytes:F1} МБ";

        public string FormattedCpu => $"{CpuPercentage:F1}%";

        public string StatusBadgeText => SafetyStatus switch
        {
            ProcessSafetyStatus.SafeToKill => "БЕЗОПАСНО ЗАВЕРШИТЬ",
            ProcessSafetyStatus.UserApp => "ПОЛЬЗОВАТЕЛЬСКОЕ ПО",
            ProcessSafetyStatus.CriticalSystem => "СИСТЕМНЫЙ ПРОЦЕСС",
            _ => "ПРОЦЕСС"
        };

        public bool CanBeTerminated => SafetyStatus != ProcessSafetyStatus.CriticalSystem;
    }
}
