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
    public partial class SoftwareUpdaterViewModel : ObservableObject
    {
        private List<SoftwareUpdateItem> _allApps = new();

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private bool _isCheckingOnline = false;

        [ObservableProperty]
        private string _statusText = "Готов к поиску обновлений программного обеспечения";

        [ObservableProperty]
        private string _statsSummary = "0 программ проанализировано";

        [ObservableProperty]
        private string _selectedFilter = "Все";

        public ObservableCollection<SoftwareUpdateItem> DisplayApps { get; } = new();

        public SoftwareUpdaterViewModel()
        {
            _ = LoadUpdatesAsync();
        }

        [RelayCommand]
        public async Task LoadUpdatesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            IsCheckingOnline = true;
            StatusText = "Мгновенное сканирование установленных программ...";

            // 1. Instant local load (0 ms)
            var localApps = await SoftwareUninstallerService.Instance.GetInstalledAppsAsync();
            _allApps = localApps.Select(a => new SoftwareUpdateItem
            {
                PackageId = a.Id,
                Name = a.DisplayName,
                InstalledVersion = !string.IsNullOrWhiteSpace(a.DisplayVersion) ? a.DisplayVersion : "1.0.0",
                AvailableVersion = !string.IsNullOrWhiteSpace(a.DisplayVersion) ? a.DisplayVersion : "1.0.0",
                Publisher = !string.IsNullOrWhiteSpace(a.Publisher) ? a.Publisher : "Официальное ПО",
                AppType = a.AppType,
                IsUpdateAvailable = false,
                IsBlacklisted = SoftwareUpdaterService.Instance.IsBlacklisted(a.DisplayName) || SoftwareUpdaterService.Instance.IsBlacklisted(a.Id)
            }).ToList();

            ApplyFilter();
            StatsSummary = $"{FormatHelper.FormatInt(_allApps.Count)} программ • Проверка обновлений в репозиториях...";
            StatusText = $"Найдено {FormatHelper.FormatInt(_allApps.Count)} приложений. Опрос Winget и облачных репозиториев...";

            // 2. Background multi-repository check
            _allApps = await SoftwareUpdaterService.Instance.ScanInstalledAppsForUpdatesAsync();
            ApplyFilter();

            int updatesCount = _allApps.Count(a => a.IsUpdateAvailable && !a.IsBlacklisted);
            int blacklistedCount = _allApps.Count(a => a.IsBlacklisted);

            StatsSummary = $"{FormatHelper.FormatInt(_allApps.Count)} программ • {(updatesCount > 0 ? $"{FormatHelper.FormatInt(updatesCount)} требуют обновления ⚡" : "Все программы обновлены ✅")}" +
                           (blacklistedCount > 0 ? $" • {FormatHelper.FormatInt(blacklistedCount)} в черном списке 🔒" : "");
            StatusText = $"Проверка завершена: найдено {FormatHelper.FormatInt(updatesCount)} доступных обновлений.";
            IsCheckingOnline = false;
            IsBusy = false;
        }

        public void SetFilter(string filter)
        {
            SelectedFilter = filter;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = _allApps.AsEnumerable();

            if (SelectedFilter == "Updates")
            {
                query = query.Where(a => a.IsUpdateAvailable && !a.IsBlacklisted);
            }
            else if (SelectedFilter == "Actual")
            {
                query = query.Where(a => !a.IsUpdateAvailable && !a.IsBlacklisted);
            }
            else if (SelectedFilter == "Blacklist")
            {
                query = query.Where(a => a.IsBlacklisted);
            }

            var list = query.ToList();
            App.Current?.Dispatcher?.Invoke(() =>
            {
                DisplayApps.Clear();
                foreach (var item in list)
                {
                    DisplayApps.Add(item);
                }
            });
        }

        [RelayCommand]
        public async Task SilentUpdateAppAsync(SoftwareUpdateItem? item)
        {
            if (item == null) return;
            IsBusy = true;
            StatusText = $"Скачивание и обновление «{item.Name}»...";

            var (success, msg) = await SoftwareUpdaterService.Instance.SilentUpdateAppAsync(item, progress =>
            {
                App.Current?.Dispatcher?.Invoke(() => StatusText = progress);
            });

            StatusText = msg;
            TrayService.Instance.ShowNotification("Обновление программ ⚡", msg);

            ApplyFilter();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task SilentUpdateAllAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Массовое фоновое обновление всех доступных программ...";

            var (updated, failed) = await SoftwareUpdaterService.Instance.SilentUpdateAllAppsAsync(_allApps, progress =>
            {
                App.Current?.Dispatcher?.Invoke(() => StatusText = progress);
            });

            string msg = $"Обновлено: {updated} программ. Ошибок: {failed}.";
            StatusText = msg;
            TrayService.Instance.ShowNotification("Пакетное обновление 🚀", msg);

            await LoadUpdatesAsync();
            IsBusy = false;
        }

        [RelayCommand]
        public void ToggleBlacklist(SoftwareUpdateItem? item)
        {
            if (item == null) return;
            string key = !string.IsNullOrEmpty(item.PackageId) ? item.PackageId : item.Name;
            bool isNowBlacklisted = SoftwareUpdaterService.Instance.ToggleBlacklist(key);
            item.IsBlacklisted = isNowBlacklisted;
            if (isNowBlacklisted) item.IsUpdateAvailable = false;

            string actionMsg = isNowBlacklisted 
                ? $"«{item.Name}» добавлена в черный список (больше не будет проверяться)." 
                : $"«{item.Name}» удалена из черного списка.";
            
            StatusText = actionMsg;
            TrayService.Instance.ShowNotification("Черный список 🔒", actionMsg);

            ApplyFilter();
        }
    }
}
