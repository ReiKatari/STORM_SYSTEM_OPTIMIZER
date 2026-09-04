using System;
using System.Collections.ObjectModel;
using System.IO;
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

        [ObservableProperty]
        private bool _isCatalogPreviewOpen = false;

        [ObservableProperty]
        private string _selectedCatalogCategory = "Все";

        [ObservableProperty]
        private string _catalogSearchText = string.Empty;

        [ObservableProperty]
        private int _selectedCount = 320;

        public ObservableCollection<IconThemeItem> IconThemes { get; } = new();
        public ObservableCollection<StormIconEntry> CatalogIcons { get; } = new();
        public ObservableCollection<StormIconEntry> FilteredCatalogIcons { get; } = new();

        public ObservableCollection<string> CatalogCategories { get; } = new()
        {
            "Все",
            "Система",
            "Папки и Диски",
            "Браузеры",
            "Игры",
            "Разработка",
            "Мультимедиа",
            "Утилиты",
            "Типы файлов"
        };

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
            LoadCatalog();
        }

        private void LoadThemes()
        {
            IconThemes.Clear();
            foreach (var th in IconThemeService.Instance.GetCuratedIconThemes())
            {
                IconThemes.Add(th);
            }
            RefreshAppliedThemeStatus();
        }

        public void RefreshAppliedThemeStatus()
        {
            bool isCustom = IconThemeService.Instance.IsCustomThemeApplied();
            string active = IconThemeService.Instance.GetActiveThemeName();

            foreach (var th in IconThemes)
            {
                if (!isCustom)
                {
                    th.IsApplied = th.Title.Contains("Стандартные") || th.Title.Contains("Default");
                }
                else
                {
                    th.IsApplied = th.Title.Equals(active, StringComparison.OrdinalIgnoreCase) ||
                                  (active.Contains("STORM") && th.Title.Contains("STORM"));
                }
            }
        }

        private void LoadCatalog()
        {
            CatalogIcons.Clear();
            foreach (var icon in IconThemeService.Instance.GetStormCyberGlowCatalog())
            {
                icon.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(StormIconEntry.IsSelected))
                    {
                        UpdateSelectedCount();
                    }
                };
                CatalogIcons.Add(icon);
            }
            ApplyCatalogFilter();
            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            SelectedCount = System.Linq.Enumerable.Count(CatalogIcons, i => i.IsSelected);
        }

        [RelayCommand]
        public void OpenCatalogPreview()
        {
            IsCatalogPreviewOpen = true;
        }

        [RelayCommand]
        public void CloseCatalogPreview()
        {
            IsCatalogPreviewOpen = false;
        }

        [RelayCommand]
        public void SelectAllCatalogIcons(object? parameter)
        {
            bool select = parameter is bool b ? b : (parameter?.ToString() == "True" || parameter?.ToString() == "true");
            foreach (var icon in FilteredCatalogIcons)
            {
                icon.IsSelected = select;
            }
            UpdateSelectedCount();
        }

        [RelayCommand]
        public void FilterCatalogCategory(string category)
        {
            SelectedCatalogCategory = category;
            ApplyCatalogFilter();
        }

        partial void OnCatalogSearchTextChanged(string value)
        {
            ApplyCatalogFilter();
        }

        private void ApplyCatalogFilter()
        {
            FilteredCatalogIcons.Clear();
            string search = CatalogSearchText?.Trim() ?? string.Empty;

            foreach (var icon in CatalogIcons)
            {
                bool matchesCat = SelectedCatalogCategory == "Все" || icon.Category.Equals(SelectedCatalogCategory, StringComparison.OrdinalIgnoreCase);
                bool matchesSearch = string.IsNullOrEmpty(search) || icon.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || icon.Category.Contains(search, StringComparison.OrdinalIgnoreCase);

                if (matchesCat && matchesSearch)
                {
                    FilteredCatalogIcons.Add(icon);
                }
            }
        }

        [RelayCommand]
        public async Task ApplySelectedIconsAsync()
        {
            var selected = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(CatalogIcons, i => i.IsSelected));
            if (selected.Count == 0)
            {
                StatusMessage = "Не выбрано ни одного значка для применения!";
                return;
            }

            IsBusy = true;
            StatusMessage = $"Применение {selected.Count} выбранных значков STORM Cyber Glow...";
            bool ok = await IconThemeService.Instance.ApplySelectedCyberGlowIconsAsync(selected);
            if (ok)
            {
                await IconThemeService.Instance.RebuildIconCacheAsync();
                RefreshAppliedThemeStatus();
                StatusMessage = $"Успешно применено {selected.Count} значков из пака STORM Cyber Glow!";
                TrayService.Instance.ShowNotification("Значки STORM Cyber Glow ⚡", StatusMessage);
                IsCatalogPreviewOpen = false;
            }
            else
            {
                StatusMessage = "Ошибка при установке значков.";
            }
            IsBusy = false;
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
                RefreshAppliedThemeStatus();
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
                RefreshAppliedThemeStatus();
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
                LoadThemes();
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
                RefreshAppliedThemeStatus();
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
            IsBusy = true;

            if (item.Title.Contains("STORM Cyber Glow"))
            {
                StatusMessage = "Применение системной темы значков STORM Cyber Glow...";
                bool ok = await IconThemeService.Instance.ApplySelectedCyberGlowIconsAsync(CatalogIcons);
                if (ok)
                {
                    IconThemeService.Instance.SetActiveThemeName(item.Title);
                    await IconThemeService.Instance.RebuildIconCacheAsync();
                    RefreshAppliedThemeStatus();
                    StatusMessage = "Тема значков STORM Cyber Glow успешно активирована в системе!";
                    TrayService.Instance.ShowNotification("Значки системы ⚡", StatusMessage);
                }
                else
                {
                    StatusMessage = "Не удалось применить тему STORM Cyber Glow.";
                }
            }
            else if (item.Title.Contains("Стандартные") || item.Title.Contains("Default") || item.Title.Contains("Windows"))
            {
                StatusMessage = "Восстановление стандартных системных значков Windows...";
                bool ok = IconThemeService.Instance.ResetSystemIconsToDefault();
                if (ok)
                {
                    IconThemeService.Instance.SetActiveThemeName(item.Title);
                    await IconThemeService.Instance.RebuildIconCacheAsync();
                    RefreshAppliedThemeStatus();
                    StatusMessage = "Все системные значки успешно возвращены к стандарту Windows!";
                    TrayService.Instance.ShowNotification("Значки системы", StatusMessage);
                }
                else
                {
                    StatusMessage = "Не удалось восстановить стандартные значки. Требуются права администратора.";
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(item.PreviewUrl) && File.Exists(item.PreviewUrl))
                {
                    StatusMessage = $"Активация пользовательской темы «{item.Title}»...";
                    bool ok = await IconThemeService.Instance.InstallIconPackageArchiveAsync(item.PreviewUrl);
                    if (ok)
                    {
                        IconThemeService.Instance.SetActiveThemeName(item.Title);
                        await IconThemeService.Instance.RebuildIconCacheAsync();
                        RefreshAppliedThemeStatus();
                        StatusMessage = $"Пользовательская тема «{item.Title}» успешно активирована!";
                        TrayService.Instance.ShowNotification("Темы значков", StatusMessage);
                    }
                    else
                    {
                        StatusMessage = "Не удалось применить пользовательскую тему.";
                    }
                }
                else
                {
                    StatusMessage = $"Применение темы значков «{item.Title}»...";
                    bool ok = await IconThemeService.Instance.ApplySelectedCyberGlowIconsAsync(CatalogIcons);
                    if (ok)
                    {
                        IconThemeService.Instance.SetActiveThemeName(item.Title);
                        await IconThemeService.Instance.RebuildIconCacheAsync();
                        RefreshAppliedThemeStatus();
                        StatusMessage = $"Тема значков «{item.Title}» успешно активирована в системе!";
                        TrayService.Instance.ShowNotification("Значки системы 🎨", StatusMessage);
                    }
                    else
                    {
                        StatusMessage = $"Не удалось применить тему «{item.Title}».";
                    }
                }
            }

            IsBusy = false;
        }
    }
}
