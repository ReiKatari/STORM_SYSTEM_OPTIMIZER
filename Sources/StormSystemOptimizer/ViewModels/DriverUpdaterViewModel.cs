using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class DriverUpdaterViewModel : ObservableObject
    {
        private List<DriverItem> _allDrivers = new();

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusText = "Готов к проверке актуальности драйверов оборудования";

        [ObservableProperty]
        private string _statsSummary = "0 устройств просканировано";

        [ObservableProperty]
        private string _selectedCategory = "Все";

        public ObservableCollection<DriverItem> DisplayDrivers { get; } = new();

        public DriverUpdaterViewModel()
        {
            _ = LoadDriversAsync();
        }

        [RelayCommand]
        public async Task LoadDriversAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Сканирование цифровых подписей WHQL и версий драйверов...";

            _allDrivers = await DriverUpdaterService.Instance.ScanDriversAsync();
            ApplyFilter();

            int updatesCount = _allDrivers.Count(d => d.IsUpdateAvailable);
            StatsSummary = $"{_allDrivers.Count} устройств в системе • {(updatesCount > 0 ? $"{updatesCount} требуют обновления ⚡" : "Все драйверы актуальны ✅")}";
            StatusText = $"Найдено {_allDrivers.Count} драйверов оборудования.";
            IsBusy = false;
        }

        public void SetCategory(string cat)
        {
            SelectedCategory = cat;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = _allDrivers.AsEnumerable();
            if (SelectedCategory != "Все")
            {
                query = query.Where(d => d.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            DisplayDrivers.Clear();
            foreach (var item in query)
            {
                DisplayDrivers.Add(item);
            }
        }

        [RelayCommand]
        public async Task UpdateDriverAsync(DriverItem? item)
        {
            if (item == null) return;

            // Prompt / Create restore point automatically before driver upgrade
            StatusText = $"Создание точки восстановления перед обновлением {item.DeviceName}...";
            await SystemRestoreService.Instance.CreateRestorePointAsync($"Перед обновлением драйвера {item.DeviceName}");

            // Open download portal
            if (!string.IsNullOrEmpty(item.DownloadUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = item.DownloadUrl,
                        UseShellExecute = true
                    });
                }
                catch { }
            }

            TrayService.Instance.ShowNotification("Центр обновления драйверов ⚡", $"Точка восстановления создана. Открыта официальная страница загрузки для {item.DeviceName}.");
            StatusText = $"Открыта загрузка для {item.DeviceName}.";
        }

        [RelayCommand]
        public async Task BackupAllDriversAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Экспорт всех установленных пакетов драйверов в бэкап...";

            string backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "STORM_Drivers_Backup");
            var (success, msg) = await DriverUpdaterService.Instance.ExportAllDriversBackupAsync(backupDir);

            StatusText = msg;
            TrayService.Instance.ShowNotification("Бэкап драйверов 💾", msg);
            IsBusy = false;
        }

        [RelayCommand]
        public async Task CreateSystemRestorePointAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Создание точки восстановления Windows...";

            var (success, msg) = await SystemRestoreService.Instance.CreateRestorePointAsync("Ручное создание в STORM OPTIMIZER");
            StatusText = msg;
            TrayService.Instance.ShowNotification("Точка восстановления 🛡️", msg);

            IsBusy = false;
        }
    }
}
