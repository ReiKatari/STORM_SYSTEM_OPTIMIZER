using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class BackupVaultViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _statusMessage = "Готов к созданию точек восстановления и резервных копий";

        public ObservableCollection<SystemBackupItem> Backups { get; } = new();

        public BackupVaultViewModel()
        {
            LoadBackups();
        }

        private void LoadBackups()
        {
            Backups.Clear();
            foreach (var item in BackupVaultService.Instance.GetExistingBackups())
            {
                Backups.Add(item);
            }
        }

        [RelayCommand]
        public async Task CreateRestorePointAsync()
        {
            StatusMessage = "Создание системной точки восстановления Windows...";
            var (ok, msg) = await BackupVaultService.Instance.CreateRestorePointAsync("STORM_Optimization_RestorePoint");
            StatusMessage = msg;
            TrayService.Instance.ShowNotification("Точка восстановления 🛡️", msg);
            LoadBackups();
        }

        [RelayCommand]
        public async Task CreateRegistryBackupAsync()
        {
            StatusMessage = "Экспорт ветвей реестра в защищенный архив...";
            var (ok, path) = await BackupVaultService.Instance.CreateRegistryBackupAsync();
            if (ok)
            {
                StatusMessage = $"Резервная копия реестра сохранена: {path}";
                TrayService.Instance.ShowNotification("Бэкап реестра 💾", "Резервная копия реестра успешно создана!");
                LoadBackups();
            }
        }
    }
}
