using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class UninstallerViewModel : ObservableObject
    {
        private List<InstalledAppItem> _allApps = new();

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusText = "Готов к глубокому анализу программ и игр";

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = "Все"; // Все, Игры, Программы, Windows Store

        [ObservableProperty]
        private string _statsSummary = "0 программ • 0 ГБ занято";

        [ObservableProperty]
        private InstalledAppItem? _selectedApp;

        public ObservableCollection<InstalledAppItem> DisplayApps { get; } = new();

        public UninstallerViewModel()
        {
            _ = LoadAppsAsync();
        }

        [RelayCommand]
        public async Task LoadAppsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Сканирование реестра 64-bit, 32-bit, Steam и Windows Store...";

            _allApps = await SoftwareUninstallerService.Instance.GetInstalledAppsAsync();

            ApplyFilters();

            double totalGb = _allApps.Sum(a => a.EstimatedSizeMb) / 1024.0;
            StatsSummary = $"{_allApps.Count} программ и игр • {totalGb:F1} ГБ на дисках";
            StatusText = $"Найдено {_allApps.Count} приложений в системе";
            IsBusy = false;
        }

        partial void OnSearchQueryChanged(string value) => ApplyFilters();
        partial void OnSelectedCategoryChanged(string value) => ApplyFilters();

        public void SetCategory(string category)
        {
            SelectedCategory = category;
        }

        private void ApplyFilters()
        {
            var query = _allApps.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                query = query.Where(a => a.DisplayName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                                         a.Publisher.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedCategory == "Игры")
            {
                query = query.Where(a => a.AppType == "Игра");
            }
            else if (SelectedCategory == "Программы")
            {
                query = query.Where(a => a.AppType == "Программа");
            }
            else if (SelectedCategory == "Windows Store")
            {
                query = query.Where(a => a.AppType == "Windows Store");
            }

            DisplayApps.Clear();
            foreach (var item in query)
            {
                DisplayApps.Add(item);
            }
        }

        [RelayCommand]
        public async Task ScanAppResidualsAsync(InstalledAppItem? item)
        {
            if (item == null) return;
            IsBusy = true;
            StatusText = $"Глубокий поиск остаточных файлов для «{item.DisplayName}»...";

            await SoftwareUninstallerService.Instance.ScanResidualClutterAsync(item);

            StatusText = item.ResidualStatusText;
            IsBusy = false;
        }

        [RelayCommand]
        public async Task DeepUninstallAppAsync(InstalledAppItem? item)
        {
            if (item == null) return;
            IsBusy = true;
            StatusText = $"Запуск деинсталлятора «{item.DisplayName}»...";

            var (success, msg) = await SoftwareUninstallerService.Instance.DeepUninstallAsync(item);
            StatusText = msg;
            TrayService.Instance.ShowNotification("Деинсталляция программы 🗑️", msg);

            await LoadAppsAsync();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task CleanResidualsOnlyAsync(InstalledAppItem? item)
        {
            if (item == null) return;
            IsBusy = true;
            StatusText = $"Очистка остаточных файлов «{item.DisplayName}»...";

            int cleaned = await SoftwareUninstallerService.Instance.CleanAllResidualsAsync(item);
            StatusText = $"Удалено {cleaned} остаточных объектов для «{item.DisplayName}».";
            TrayService.Instance.ShowNotification("Очистка остатков 🧹", StatusText);

            IsBusy = false;
        }
    }
}
