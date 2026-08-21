using System;
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
    public partial class FolderProtectionViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage = "Сейф папок готов. Выберите каталог для защиты.";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFolderPath))]
        private string _selectedFolderPath = string.Empty;

        public bool HasFolderPath => !string.IsNullOrWhiteSpace(SelectedFolderPath);

        [ObservableProperty]
        private int _selectedModeIndex = 2; // 0 = Stealth, 1 = Password, 2 = Stealth + Password

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPasswordInput))]
        private string _passwordInput = string.Empty;

        public bool HasPasswordInput => !string.IsNullOrWhiteSpace(PasswordInput);

        [ObservableProperty]
        private string _unlockPasswordInput = string.Empty;

        [ObservableProperty]
        private ProtectedFolderItem? _selectedFolder;

        [ObservableProperty]
        private string _vaultStats = "0 защищенных папок";

        [ObservableProperty]
        private bool _isVaultEmpty = true;

        public ObservableCollection<ProtectedFolderItem> ProtectedFolders { get; } = new();

        public FolderProtectionViewModel()
        {
            RefreshList();
        }

        public void RefreshList()
        {
            ProtectedFolders.Clear();
            var list = FolderProtectionService.Instance.GetProtectedFolders();
            foreach (var item in list)
            {
                ProtectedFolders.Add(item);
            }

            IsVaultEmpty = ProtectedFolders.Count == 0;
            int lockedCount = list.Count(f => f.IsLocked);
            VaultStats = $"{list.Count} каталогов в сейфе ({lockedCount} заблокировано)";
        }

        [RelayCommand]
        public void BrowseFolder()
        {
            var dlg = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Выберите папку для защиты и скрытия",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                SelectedFolderPath = dlg.FolderName;
            }
        }

        [RelayCommand]
        public async Task ProtectSelectedFolderAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedFolderPath))
            {
                StatusMessage = "Укажите путь к папке!";
                return;
            }

            var mode = SelectedModeIndex switch
            {
                0 => ProtectionMode.StealthOnly,
                1 => ProtectionMode.PasswordLockOnly,
                _ => ProtectionMode.StealthAndPassword
            };

            if (mode != ProtectionMode.StealthOnly && string.IsNullOrWhiteSpace(PasswordInput))
            {
                StatusMessage = "Введите пароль для блокировки доступа!";
                return;
            }

            IsBusy = true;
            StatusMessage = "Применение параметров безопасности и скрытия...";

            var (success, msg) = await FolderProtectionService.Instance.ProtectFolderAsync(
                SelectedFolderPath,
                mode,
                PasswordInput ?? string.Empty);

            StatusMessage = msg;
            TrayService.Instance.ShowNotification("Защита папок 🛡️", msg);

            SelectedFolderPath = string.Empty;
            PasswordInput = string.Empty;

            RefreshList();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task UnlockFolderAsync(ProtectedFolderItem? item)
        {
            if (item == null) return;
            IsBusy = true;

            var (success, msg) = await FolderProtectionService.Instance.UnlockFolderAsync(item, UnlockPasswordInput);
            StatusMessage = msg;
            TrayService.Instance.ShowNotification("Сейф папок", msg);

            UnlockPasswordInput = string.Empty;
            RefreshList();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task LockFolderAsync(ProtectedFolderItem? item)
        {
            if (item == null) return;
            IsBusy = true;

            var (success, msg) = await FolderProtectionService.Instance.LockFolderAsync(item);
            StatusMessage = msg;
            TrayService.Instance.ShowNotification("Сейф папок", msg);

            RefreshList();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RemoveProtectionAsync(ProtectedFolderItem? item)
        {
            if (item == null) return;
            IsBusy = true;

            var (success, msg) = await FolderProtectionService.Instance.RemoveProtectionAsync(item, UnlockPasswordInput);
            StatusMessage = msg;
            TrayService.Instance.ShowNotification("Сейф папок", msg);

            UnlockPasswordInput = string.Empty;
            RefreshList();
            IsBusy = false;
        }

        [RelayCommand]
        public void OpenInExplorer(ProtectedFolderItem? item)
        {
            if (item == null || string.IsNullOrEmpty(item.FolderPath)) return;
            if (Directory.Exists(item.FolderPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{item.FolderPath}\"",
                        UseShellExecute = true
                    });
                }
                catch { }
            }
            else
            {
                StatusMessage = "Папка заблокирована или скрыта. Сначала разблокируйте её!";
            }
        }
    }
}
