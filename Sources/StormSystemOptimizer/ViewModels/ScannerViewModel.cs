using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class ScannerViewModel : ObservableObject
    {
        private readonly DispatcherQueue _dispatcher;

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
            _dispatcher = DispatcherQueue.GetForCurrentThread();

            ScannerService.Instance.ProgressChanged += (s, p) => _dispatcher.TryEnqueue(() => ScanProgress = p);
            ScannerService.Instance.StatusChanged += (s, msg) => _dispatcher.TryEnqueue(() => ScanStatus = msg);
            OptimizationEngine.Instance.FixProgressChanged += (s, p) => _dispatcher.TryEnqueue(() => ScanProgress = p);
            OptimizationEngine.Instance.FixStatusChanged += (s, msg) => _dispatcher.TryEnqueue(() => ScanStatus = msg);
        }

        [RelayCommand]
        public async Task StartScanAsync()
        {
            if (IsScanning || IsFixing) return;
            IsScanning = true;
            ScanProgress = 0;
            AllIssues.Clear();
            FilteredIssues.Clear();

            var results = await ScannerService.Instance.ScanAllAsync();

            foreach (var item in results)
            {
                AllIssues.Add(item);
            }

            UpdateStats();
            ApplyFilter();
            IsScanning = false;
        }

        [RelayCommand]
        public async Task FixSelectedAsync()
        {
            if (IsFixing || AllIssues.Count == 0) return;
            IsFixing = true;
            ScanProgress = 0;

            var itemsToFix = AllIssues.Where(i => i.IsSelected && !i.IsFixed).ToList();
            long totalFreed = await OptimizationEngine.Instance.FixItemsAsync(itemsToFix);

            UpdateStats();
            IsFixing = false;

            string freedMb = (totalFreed / (1024.0 * 1024.0)).ToString("F1");
            ScanStatus = $"Успешно устранено проблем: {itemsToFix.Count}. Освобождено: {freedMb} МБ";

            TrayService.Instance.ShowNotification("Оптимизация завершена!", $"Устранено проблем: {itemsToFix.Count}. Система ускорена.");
        }

        [RelayCommand]
        public async Task FixSafeOnlyAsync()
        {
            SelectSafeOnly();
            await FixSelectedAsync();
        }

        [RelayCommand]
        public void SelectAll()
        {
            foreach (var item in FilteredIssues) item.IsSelected = true;
        }

        [RelayCommand]
        public void DeselectAll()
        {
            foreach (var item in FilteredIssues) item.IsSelected = false;
        }

        [RelayCommand]
        public void SelectSafeOnly()
        {
            foreach (var item in AllIssues)
            {
                item.IsSelected = (item.RiskLevel == RiskLevel.Safe);
            }
        }

        public void SetFilter(string filter)
        {
            SelectedFilter = filter;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            FilteredIssues.Clear();
            var query = AllIssues.AsEnumerable();

            if (SelectedFilter == "Мусор и кэш")
                query = query.Where(i => i.Category == OptimizationCategory.JunkAndCache);
            else if (SelectedFilter == "Память")
                query = query.Where(i => i.Category == OptimizationCategory.MemoryRam);
            else if (SelectedFilter == "Автозапуск")
                query = query.Where(i => i.Category == OptimizationCategory.StartupApps);
            else if (SelectedFilter == "Службы")
                query = query.Where(i => i.Category == OptimizationCategory.WindowsServices);
            else if (SelectedFilter == "Сеть и DNS")
                query = query.Where(i => i.Category == OptimizationCategory.NetworkAndDns);
            else if (SelectedFilter == "Приватность")
                query = query.Where(i => i.Category == OptimizationCategory.PrivacyTelemetry);
            else if (SelectedFilter == "Система и Питание")
                query = query.Where(i => i.Category == OptimizationCategory.SystemHealth || i.Category == OptimizationCategory.PowerAndVisual);

            foreach (var item in query)
            {
                FilteredIssues.Add(item);
            }
        }

        private void UpdateStats()
        {
            IssuesCount = AllIssues.Count(i => !i.IsFixed);
            SafeIssuesCount = AllIssues.Count(i => !i.IsFixed && i.RiskLevel == RiskLevel.Safe);
            RecommendedCount = AllIssues.Count(i => !i.IsFixed && i.RiskLevel == RiskLevel.Recommended);

            long totalBytes = AllIssues.Where(i => !i.IsFixed).Sum(i => i.ReclaimableBytes);
            if (totalBytes <= 0) TotalReclaimableText = "0 МБ";
            else if (totalBytes < 1024 * 1024 * 1024) TotalReclaimableText = $"{totalBytes / (1024.0 * 1024.0):F1} МБ";
            else TotalReclaimableText = $"{totalBytes / (1024.0 * 1024.0 * 1024.0):F2} ГБ";
        }
    }
}
