using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class IconThemeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _statusMessage = "Готов к настройке значков и тем оформления Windows";

        [ObservableProperty]
        private string _customPackagePath = string.Empty;

        [ObservableProperty]
        private string _selectedCustomTarget = "Папки";

        [ObservableProperty]
        private string _customIconFilePath = string.Empty;

        [ObservableProperty]
        private bool _isBusy = false;

        public ObservableCollection<IconThemeItem> IconThemes { get; } = new();

        public ObservableCollection<string> TargetLocations { get; } = new()
        {
            "Папки",
            "Диски",
            "Этот компьютер",
            "Корзина (пустая)",
            "Корзина (полная)",
            "Папка пользователя",
            "Сеть"
        };

        public IconThemeViewModel()
        {
            LoadThemes();
        }

        private void LoadThemes()
        {
            IconThemes.Clear();
            foreach (var th in IconThemeService.Instance.GetCuratedIconThemes())
            {
                IconThemes.Add(th);
            }
        }

        [RelayCommand]
        public async Task RebuildIconCacheAsync()
        {
            IsBusy = true;
            StatusMessage = "Очистка кэша значков IconCache.db и перезапуск Проводника...";
            bool ok = await IconThemeService.Instance.RebuildIconCacheAsync();
            IsBusy = false;
            if (ok)
            {
                StatusMessage = "Кэш значков Windows успешно очищен и перестроен!";
                TrayService.Instance.ShowNotification("Кэш значков ⚡", StatusMessage);
            }
            else
            {
                StatusMessage = "Произошла ошибка при перестройке кэша значков.";
            }
        }

        [RelayCommand]
        public async Task ResetIconsToDefaultAsync()
        {
            IsBusy = true;
            StatusMessage = "Восстановление стандартных системных значков Windows...";
            bool ok = IconThemeService.Instance.ResetSystemIconsToDefault();
            if (ok)
            {
                await IconThemeService.Instance.RebuildIconCacheAsync();
                StatusMessage = "Все системные значки успешно сброшены до стандартных!";
                TrayService.Instance.ShowNotification("Значки системы", StatusMessage);
            }
            else
            {
                StatusMessage = "Не удалось сбросить значки. Требуются права администратора.";
            }
            IsBusy = false;
        }

        [RelayCommand]
        public void BrowseIconPackage()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите пакет значков",
                Filter = "Пакеты значков (*.iconpack;*.ip;*.7tsp;*.zip;*.ico)|*.iconpack;*.ip;*.7tsp;*.zip;*.ico|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                CustomPackagePath = dlg.FileName;
            }
        }

        [RelayCommand]
        public async Task InstallPackageAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomPackagePath)) return;

            IsBusy = true;
            StatusMessage = "Распаковка и установка пакета значков...";
            bool ok = await IconThemeService.Instance.InstallIconPackageArchiveAsync(CustomPackagePath);
            if (ok)
            {
                await IconThemeService.Instance.RebuildIconCacheAsync();
                StatusMessage = "Пакет значков успешно установлен и применен!";
                TrayService.Instance.ShowNotification("Темы значков 🎨", StatusMessage);
            }
            else
            {
                StatusMessage = "Не удалось применить пакет значков. Проверьте формат архива.";
            }
            IsBusy = false;
        }

        [RelayCommand]
        public void BrowseCustomIconFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите значок",
                Filter = "Значки (*.ico;*.png)|*.ico;*.png|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                CustomIconFilePath = dlg.FileName;
            }
        }

        [RelayCommand]
        public async Task ApplyCustomIconAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomIconFilePath)) return;

            string targetKey = SelectedCustomTarget switch
            {
                "Папки" => "Folders",
                "Диски" => "Drives",
                "Этот компьютер" => "ThisPC",
                "Корзина (пустая)" => "RecycleBinEmpty",
                "Корзина (полная)" => "RecycleBinFull",
                "Папка пользователя" => "UserFolder",
                "Сеть" => "Network",
                _ => "Folders"
            };

            StatusMessage = $"Применение значка для элемента «{SelectedCustomTarget}»...";
            bool ok = IconThemeService.Instance.SetSystemIcon(targetKey, CustomIconFilePath);
            if (ok)
            {
                await IconThemeService.Instance.RebuildIconCacheAsync();
                StatusMessage = $"Значок для «{SelectedCustomTarget}» успешно обновлен!";
                TrayService.Instance.ShowNotification("Значки системы", StatusMessage);
            }
            else
            {
                StatusMessage = "Не удалось применить значок. Проверьте путь к файлу.";
            }
        }

        [RelayCommand]
        public async Task ApplyThemeAsync(IconThemeItem item)
        {
            if (item == null) return;
            StatusMessage = $"Применение темы значков «{item.Title}»...";
            foreach (var th in IconThemes) th.IsApplied = false;
            item.IsApplied = true;
            await IconThemeService.Instance.RebuildIconCacheAsync();
            StatusMessage = $"Тема значков «{item.Title}» успешно активирована!";
            TrayService.Instance.ShowNotification("Темы значков", StatusMessage);
        }
    }
}
