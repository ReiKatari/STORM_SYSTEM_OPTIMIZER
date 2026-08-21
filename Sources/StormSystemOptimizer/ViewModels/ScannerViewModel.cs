using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class ScannerViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotScanning))]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isScanning = false;

        public bool IsNotScanning => !IsScanning;
        public bool IsNotBusy => !IsScanning && !IsFixing;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotFixing))]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isFixing = false;

        public bool IsNotFixing => !IsFixing;

        [ObservableProperty]
        private int _scanProgress = 0;

        [ObservableProperty]
        private string _scanStatus = "Нажмите «Начать сканирование» для поиска мусора";

        [ObservableProperty]
        private string _scanStatusText = "Сканирование готово к запуску";

        [ObservableProperty]
        private string _currentScanCategory = "Системный кэш и временные файлы";

        [ObservableProperty]
        private string _totalReclaimableText = "0 МБ";

        [ObservableProperty]
        private int _issuesCount = 0;

        [ObservableProperty]
        private int _safeIssuesCount = 0;

        [ObservableProperty]
        private int _recommendedCount = 0;

        [ObservableProperty]
        private string _selectedFilter = "Все категории";

        public ObservableCollection<OptimizationItem> AllIssues { get; } = new();
        public ObservableCollection<OptimizationItem> FilteredIssues { get; } = new();
        public ObservableCollection<OptimizationItem> ScanItems => FilteredIssues;

        public bool CanFix => FilteredIssues.Any(x => x.IsSelected && !x.IsFixed) && !IsScanning && !IsFixing;

        public ScannerViewModel()
        {
            ScannerService.Instance.ProgressChanged += (s, p) => Application.Current?.Dispatcher.Invoke(() => ScanProgress = p);
            ScannerService.Instance.StatusChanged += (s, msg) => Application.Current?.Dispatcher.Invoke(() =>
            {
                ScanStatus = msg;
                ScanStatusText = msg;
                CurrentScanCategory = msg;
            });
            OptimizationEngine.Instance.FixProgressChanged += (s, p) => Application.Current?.Dispatcher.Invoke(() => ScanProgress = p);
            OptimizationEngine.Instance.FixStatusChanged += (s, msg) => Application.Current?.Dispatcher.Invoke(() =>
            {
                ScanStatus = msg;
                ScanStatusText = msg;
            });

            _ = StartScanAsync();
        }

        [RelayCommand]
        public async Task StartScanAsync()
        {
            if (IsScanning || IsFixing) return;

            IsScanning = true;
            ScanProgress = 0;
            ScanStatus = "Выполняется глубокое сканирование системы...";
            ScanStatusText = "Поиск мусора, временных файлов, сетевых логов и кэша...";
            CurrentScanCategory = "Анализ системных каталогов...";

            AllIssues.Clear();
            FilteredIssues.Clear();
            OnPropertyChanged(nameof(CanFix));

            var issues = await ScannerService.Instance.ScanAllAsync();

            AllIssues.Clear();
            FilteredIssues.Clear();
            foreach (var item in issues)
            {
                AllIssues.Add(item);
                FilteredIssues.Add(item);
            }

            UpdateStatistics();

            IsScanning = false;
            ScanStatus = $"Сканирование завершено. Найдено проблем: {AllIssues.Count}.";
            ScanStatusText = $"Найдено {AllIssues.Count} элементов для безопасной очистки и оптимизации.";
            CurrentScanCategory = "Сканирование завершено";
            OnPropertyChanged(nameof(CanFix));
        }

        [RelayCommand]
        public async Task FixSelectedAsync()
        {
            var selected = AllIssues.Where(x => x.IsSelected && !x.IsFixed).ToList();
            if (selected.Count == 0 || IsFixing || IsScanning) return;

            IsFixing = true;
            ScanProgress = 0;
            ScanStatusText = "Оптимизация и удаление выбранных элементов...";

            await OptimizationEngine.Instance.FixItemsAsync(selected);

            UpdateStatistics();
            IsFixing = false;
            ScanStatus = $"Оптимизация завершена. Успешно очищено {selected.Count(x => x.IsFixed)} элементов.";
            ScanStatusText = $"Оптимизация завершена! Освобождено место на диске и ускорена система.";
            OnPropertyChanged(nameof(CanFix));

            TrayService.Instance.ShowNotification("Оптимизация выполнена", $"Успешно оптимизировано {selected.Count(x => x.IsFixed)} элементов.");
        }

        [RelayCommand]
        public void SelectAll()
        {
            foreach (var item in AllIssues)
            {
                item.IsSelected = true;
            }
            OnPropertyChanged(nameof(CanFix));
        }

        [RelayCommand]
        public void DeselectAll()
        {
            foreach (var item in AllIssues)
            {
                item.IsSelected = false;
            }
            OnPropertyChanged(nameof(CanFix));
        }

        public void ApplyFilter(string filter)
        {
            SelectedFilter = filter;
            FilteredIssues.Clear();

            var matching = filter switch
            {
                "Кэш и мусор" => AllIssues.Where(x => x.Category == OptimizationCategory.JunkAndCache),
                "Оперативная память" => AllIssues.Where(x => x.Category == OptimizationCategory.MemoryRam),
                "Автозагрузка" => AllIssues.Where(x => x.Category == OptimizationCategory.StartupApps),
                "Службы Windows" => AllIssues.Where(x => x.Category == OptimizationCategory.WindowsServices),
                "Сеть и DNS" => AllIssues.Where(x => x.Category == OptimizationCategory.NetworkAndDns),
                "Приватность" => AllIssues.Where(x => x.Category == OptimizationCategory.PrivacyTelemetry),
                _ => AllIssues
            };

            foreach (var item in matching)
            {
                FilteredIssues.Add(item);
            }

            OnPropertyChanged(nameof(CanFix));
        }

        private void UpdateStatistics()
        {
            IssuesCount = AllIssues.Count(x => !x.IsFixed);
            SafeIssuesCount = AllIssues.Count(x => x.RiskLevel == RiskLevel.Safe && !x.IsFixed);
            RecommendedCount = AllIssues.Count(x => x.RiskLevel == RiskLevel.Recommended && !x.IsFixed);

            long totalBytes = AllIssues.Where(x => !x.IsFixed).Sum(x => x.ReclaimableBytes);
            if (totalBytes < 1024) TotalReclaimableText = $"{totalBytes} Б";
            else if (totalBytes < 1024 * 1024) TotalReclaimableText = $"{totalBytes / 1024.0:F1} КБ";
            else if (totalBytes < 1024 * 1024 * 1024) TotalReclaimableText = $"{totalBytes / (1024.0 * 1024.0):F1} МБ";
            else TotalReclaimableText = $"{totalBytes / (1024.0 * 1024.0 * 1024.0):F2} ГБ";

            OnPropertyChanged(nameof(CanFix));
        }
    }
}
