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
        private bool _isScanning = false;

        public bool IsNotScanning => !IsScanning;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotFixing))]
        private bool _isFixing = false;

        public bool IsNotFixing => !IsFixing;

        [ObservableProperty]
        private int _scanProgress = 0;

        [ObservableProperty]
        private string _scanStatus = "Нажмите «Начать сканирование» для проверки системы";

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

        public ScannerViewModel()
        {
            ScannerService.Instance.ProgressChanged += (s, p) => Application.Current?.Dispatcher.Invoke(() => ScanProgress = p);
            ScannerService.Instance.StatusChanged += (s, msg) => Application.Current?.Dispatcher.Invoke(() => ScanStatus = msg);
            OptimizationEngine.Instance.FixProgressChanged += (s, p) => Application.Current?.Dispatcher.Invoke(() => ScanProgress = p);
            OptimizationEngine.Instance.FixStatusChanged += (s, msg) => Application.Current?.Dispatcher.Invoke(() => ScanStatus = msg);
        }

        [RelayCommand]
        public async Task StartScanAsync()
        {
            if (IsScanning || IsFixing) return;

            IsScanning = true;
            ScanProgress = 0;
            AllIssues.Clear();
            FilteredIssues.Clear();

            var issues = await ScannerService.Instance.ScanAllAsync();

            AllIssues.Clear();
            foreach (var item in issues)
            {
                AllIssues.Add(item);
            }

            ApplyFilter(SelectedFilter);
            UpdateStatistics();

            IsScanning = false;
            ScanStatus = $"Сканирование завершено. Найдено проблем: {AllIssues.Count}.";
        }

        [RelayCommand]
        public async Task FixSelectedAsync()
        {
            var selected = AllIssues.Where(x => x.IsSelected && !x.IsFixed).ToList();
            if (selected.Count == 0 || IsFixing || IsScanning) return;

            IsFixing = true;
            ScanProgress = 0;

            await OptimizationEngine.Instance.FixItemsAsync(selected);

            UpdateStatistics();
            IsFixing = false;
            ScanStatus = $"Оптимизация завершена. Успешно исправлено {selected.Count(x => x.IsFixed)} элементов.";
        }

        [RelayCommand]
        public async Task FixSafeOnlyAsync()
        {
            foreach (var item in AllIssues)
            {
                item.IsSelected = (item.RiskLevel == RiskLevel.Safe && !item.IsFixed);
            }
            await FixSelectedAsync();
        }

        [RelayCommand]
        public void SelectAll()
        {
            foreach (var item in FilteredIssues.Where(x => !x.IsFixed))
            {
                item.IsSelected = true;
            }
        }

        [RelayCommand]
        public void DeselectAll()
        {
            foreach (var item in FilteredIssues)
            {
                item.IsSelected = false;
            }
        }

        public void ApplyFilter(string filter)
        {
            SelectedFilter = filter;
            FilteredIssues.Clear();

            var filtered = filter switch
            {
                "Кэш и Мусор" => AllIssues.Where(x => x.Category == OptimizationCategory.JunkAndCache),
                "Память (RAM)" => AllIssues.Where(x => x.Category == OptimizationCategory.MemoryRam),
                "Автозагрузка" => AllIssues.Where(x => x.Category == OptimizationCategory.StartupApps),
                "Службы Windows" => AllIssues.Where(x => x.Category == OptimizationCategory.WindowsServices),
                "Сеть и DNS" => AllIssues.Where(x => x.Category == OptimizationCategory.NetworkAndDns),
                "Приватность" => AllIssues.Where(x => x.Category == OptimizationCategory.PrivacyTelemetry),
                "Диски и TRIM" => AllIssues.Where(x => x.Category == OptimizationCategory.SystemHealth),
                "Питание" => AllIssues.Where(x => x.Category == OptimizationCategory.PowerAndVisual),
                _ => AllIssues
            };

            foreach (var item in filtered)
            {
                FilteredIssues.Add(item);
            }
        }

        private void UpdateStatistics()
        {
            IssuesCount = AllIssues.Count(x => !x.IsFixed);
            SafeIssuesCount = AllIssues.Count(x => x.RiskLevel == RiskLevel.Safe && !x.IsFixed);
            RecommendedCount = AllIssues.Count(x => x.RiskLevel == RiskLevel.Recommended && !x.IsFixed);

            long totalBytes = AllIssues.Where(x => !x.IsFixed).Sum(x => x.ReclaimableBytes);
            if (totalBytes > 1024 * 1024 * 1024)
            {
                TotalReclaimableText = $"{totalBytes / (1024.0 * 1024.0 * 1024.0):F2} ГБ";
            }
            else
            {
                TotalReclaimableText = $"{totalBytes / (1024.0 * 1024.0):F1} МБ";
            }
        }
    }
}
