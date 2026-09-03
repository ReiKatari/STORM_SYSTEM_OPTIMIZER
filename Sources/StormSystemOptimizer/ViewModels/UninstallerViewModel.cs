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
        private List<InstalledAppItem> _orphanedResiduals = new();

        [ObservableProperty]
        private bool _isResidualsTabSelected = false;

        [ObservableProperty]
        private int _residualsCount = 0;

        [ObservableProperty]
        private double _residualsTotalSizeMb = 0;

        [ObservableProperty]
        private string _residualsSummaryText = "Остаточные следы не обнаружены";

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusText = "Готов к глубокому анализу программ и игр";

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = "Все"; // Все, Игры, Программы, Магазин Windows, Остатки программ

        [ObservableProperty]
        private string _selectedSort = "Размер ↓"; // "Размер ↓", "Размер ↑", "Имя (А-Я)", "Дата"

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
            StatusText = "Сканирование реестра 64-bit, 32-bit, Steam и Магазина Windows...";

            _allApps = await SoftwareUninstallerService.Instance.GetInstalledAppsAsync();

            ApplyFilters();
            UpdateStatsSummary();
            StatusText = $"Найдено {FormatHelper.FormatInt(_allApps.Count)} приложений в системе";
            IsBusy = false;
        }

        private void UpdateStatsSummary()
        {
            double totalGb = _allApps.Sum(a => a.EstimatedSizeMb) / 1024.0;
            StatsSummary = $"{FormatHelper.FormatInt(_allApps.Count)} программ и игр • {FormatHelper.FormatDouble(totalGb, 1)} ГБ на дисках";
        }

        partial void OnSearchQueryChanged(string value) => ApplyFilters();
        partial void OnSelectedCategoryChanged(string value) => ApplyFilters();
        partial void OnSelectedSortChanged(string value) => ApplyFilters();

        [RelayCommand]
        public void SetCategory(string category)
        {
            SelectedCategory = category;
            IsResidualsTabSelected = (category == "Остатки программ");
            if (IsResidualsTabSelected)
            {
                if (_orphanedResiduals.Count == 0)
                {
                    _ = LoadOrphanedResidualsAsync();
                }
                else
                {
                    ApplyFilters();
                }
            }
            else
            {
                ApplyFilters();
            }
        }

        [RelayCommand]
        public async Task LoadOrphanedResidualsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Анализ системы на наличие остаточных файлов от ранее удаленных программ...";

            _orphanedResiduals = await SoftwareUninstallerService.Instance.ScanOrphanedResidualsAsync(_allApps);
            ResidualsCount = _orphanedResiduals.Count;
            ResidualsTotalSizeMb = Math.Round(_orphanedResiduals.Sum(r => r.EstimatedSizeMb), 1);
            ResidualsSummaryText = $"Обнаружено {ResidualsCount} программ с остаточными следами ({ResidualsTotalSizeMb} МБ мусора)";

            ApplyFilters();
            StatusText = $"Сканирование завершено: найдено {ResidualsCount} остаточных программ.";
            IsBusy = false;
        }

        [RelayCommand]
        public async Task CleanAllResidualsAsync()
        {
            if (_orphanedResiduals.Count == 0 || IsBusy) return;
            IsBusy = true;
            StatusText = "Полная зачистка всех обнаруженных остаточных папок и записей реестра...";

            int cleanedCount = 0;
            foreach (var r in _orphanedResiduals.ToList())
            {
                var (ok, _) = await SoftwareUninstallerService.Instance.CleanResidualsAsync(r);
                if (ok) cleanedCount++;
            }

            _orphanedResiduals.Clear();
            ResidualsCount = 0;
            ResidualsTotalSizeMb = 0;
            ResidualsSummaryText = "Все остаточные файлы и ключи реестра успешно удалены!";
            ApplyFilters();

            TrayService.Instance.ShowNotification("Очистка остатков ⚡", $"Успешно удалены остатки {cleanedCount} ранее удаленных программ.");
            StatusText = $"Очистка завершена: удалены остатки {cleanedCount} программ.";
            IsBusy = false;
        }

        [RelayCommand]
        public void SetSort(string sort)
        {
            SelectedSort = sort;
        }

        private void ApplyFilters()
        {
            if (SelectedCategory == "Остатки программ")
            {
                var residualQuery = _orphanedResiduals.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    residualQuery = residualQuery.Where(a => a.DisplayName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
                }

                residualQuery = SelectedSort switch
                {
                    "Размер ↑" => residualQuery.OrderBy(a => a.EstimatedSizeMb),
                    "Имя (А-Я)" => residualQuery.OrderBy(a => a.DisplayName),
                    _ => residualQuery.OrderByDescending(a => a.EstimatedSizeMb)
                };

                DisplayApps.Clear();
                foreach (var r in residualQuery)
                {
                    DisplayApps.Add(r);
                }
                return;
            }

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
            else if (SelectedCategory == "Магазин Windows" || SelectedCategory == "Windows Store")
            {
                query = query.Where(a => a.AppType == "Магазин Windows" || a.AppType == "Windows Store");
            }

            query = SelectedSort switch
            {
                "Размер ↑" => query.OrderBy(a => a.EstimatedSizeMb),
                "Имя (А-Я)" => query.OrderBy(a => a.DisplayName),
                "Дата" => query.OrderByDescending(a => a.InstallDate),
                _ => query.OrderByDescending(a => a.EstimatedSizeMb)
            };

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
        public async Task CleanResidualsOnlyAsync(InstalledAppItem? item)
        {
            if (item == null) return;
            IsBusy = true;
            StatusText = $"Удаление остаточных следов для «{item.DisplayName}»...";

            var (success, msg) = await SoftwareUninstallerService.Instance.CleanResidualsAsync(item);
            StatusText = msg;

            if (item.AppType == "Остатки")
            {
                _orphanedResiduals.RemoveAll(r => r.Id == item.Id || r.DisplayName.Equals(item.DisplayName, StringComparison.OrdinalIgnoreCase));
                ResidualsCount = _orphanedResiduals.Count;
                ResidualsTotalSizeMb = Math.Round(_orphanedResiduals.Sum(r => r.EstimatedSizeMb), 1);
                ResidualsSummaryText = ResidualsCount > 0
                    ? $"Обнаружено {ResidualsCount} программ с остаточными следами ({ResidualsTotalSizeMb} МБ мусора)"
                    : "Все остаточные файлы и ключи реестра успешно удалены!";
                DisplayApps.Remove(item);
            }

            Controls.StormMessageBox.Show(msg, "Очистка хвостов", System.Windows.MessageBoxButton.OK, success ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Warning);
            IsBusy = false;
        }

        [RelayCommand]
        public async Task DeepUninstallAppAsync(InstalledAppItem? item)
        {
            if (item == null) return;

            string confirmPrompt = item.AppType == "Остатки"
                ? $"Удалить все найденные остаточные файлы и записи реестра для «{item.DisplayName}» ({item.ResidualFilesCount} файлов, {item.ResidualSizeMb} МБ)?"
                : $"Вы действительно хотите полностью удалить «{item.DisplayName}»?\n\nБудет запущен штатный деинсталлятор, после чего STORM автоматически закроет зависшие процессы, удалит каталог установки, зачистит все остаточные папки в AppData/ProgramData и записи реестра.";

            var confirm = Controls.StormMessageBox.Show(
                confirmPrompt,
                item.AppType == "Остатки" ? "Удаление остатков программы" : "Полное удаление программы",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            StatusText = item.AppType == "Остатки" ? $"Удаление остаточных следов «{item.DisplayName}»..." : $"Запуск деинсталлятора «{item.DisplayName}»...";

            var (success, msg) = await SoftwareUninstallerService.Instance.DeepUninstallAsync(item);
            StatusText = msg;

            // Immediately purge from UI collections so it instantly disappears
            _allApps.RemoveAll(a => a.Id == item.Id || a.DisplayName.Equals(item.DisplayName, StringComparison.OrdinalIgnoreCase));
            if (item.AppType == "Остатки")
            {
                _orphanedResiduals.RemoveAll(r => r.Id == item.Id || r.DisplayName.Equals(item.DisplayName, StringComparison.OrdinalIgnoreCase));
                ResidualsCount = _orphanedResiduals.Count;
                ResidualsTotalSizeMb = Math.Round(_orphanedResiduals.Sum(r => r.EstimatedSizeMb), 1);
                ResidualsSummaryText = ResidualsCount > 0
                    ? $"Обнаружено {ResidualsCount} программ с остаточными следами ({ResidualsTotalSizeMb} МБ мусора)"
                    : "Все остаточные файлы и ключи реестра успешно удалены!";
            }
            DisplayApps.Remove(item);
            ApplyFilters();
            UpdateStatsSummary();

            TrayService.Instance.ShowNotification("Деинсталляция программы 🗑️", msg);
            Controls.StormMessageBox.Show(msg, "Деинсталляция завершена", System.Windows.MessageBoxButton.OK, success ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Warning);

            // Rescan in background to ensure sync with system registry
            await Task.Delay(400);
            var refreshed = await SoftwareUninstallerService.Instance.GetInstalledAppsAsync();
            _allApps = refreshed;
            ApplyFilters();
            UpdateStatsSummary();

            IsBusy = false;
        }

        [RelayCommand]
        public async Task RemoveEdgeAsync()
        {
            IsBusy = true;
            StatusText = "Удаление встроенного браузера...";
            bool ok = await SoftwareUninstallerService.Instance.RemoveMicrosoftEdgeAsync();
            StatusText = ok ? "Встроенный браузер успешно удален!" : "Операция завершена";
            TrayService.Instance.ShowNotification("Удаление компонентов", StatusText);
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RemoveOneDriveAsync()
        {
            IsBusy = true;
            StatusText = "Удаление облачного хранилища...";
            bool ok = await SoftwareUninstallerService.Instance.RemoveOneDriveAsync();
            StatusText = ok ? "Облачное хранилище успешно удалено!" : "Операция завершена";
            TrayService.Instance.ShowNotification("Удаление компонентов", StatusText);
            IsBusy = false;
        }

        [RelayCommand]
        public async Task CleanComponentStoreAsync()
        {
            IsBusy = true;
            StatusText = "Очистка хранилища системных компонентов...";
            bool ok = await SoftwareUninstallerService.Instance.CleanComponentStoreAsync();
            StatusText = ok ? "Хранилище компонентов успешно очищено!" : "Операция завершена";
            TrayService.Instance.ShowNotification("Очистка компонентов", StatusText);
            IsBusy = false;
        }
    }
}
