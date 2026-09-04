using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        private bool _isIconDetailOpen = false;

        [ObservableProperty]
        private StormIconEntry? _selectedIconDetail;

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
            SelectedCount = CatalogIcons.Count(i => i.IsSelected);
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
        public void OpenIconActionDialog(StormIconEntry? icon)
        {
            if (icon == null) return;
            SelectedIconDetail = icon;
            IsIconDetailOpen = true;
        }

        [RelayCommand]
        public void CloseIconActionDialog()
        {
            IsIconDetailOpen = false;
        }

        [RelayCommand]
        public async Task ApplyDetailIconToFolderAsync()
        {
            if (SelectedIconDetail == null) return;

            var dlg = new OpenFolderDialog
            {
                Title = $"Выберите папку для применения значка «{SelectedIconDetail.Name}»"
            };

            if (dlg.ShowDialog() == true)
            {
                string folder = dlg.FolderName;
                string activeTheme = IconThemeService.Instance.GetActiveThemeName();
                bool ok = IconThemeService.Instance.ApplyIconToFolder(folder, SelectedIconDetail.GeometryKey, activeTheme);
                if (ok)
                {
                    StatusMessage = $"Значок «{SelectedIconDetail.Name}» успешно применен к папке «{Path.GetFileName(folder)}»!";
                    TrayService.Instance.ShowNotification("Значки системы 📁", StatusMessage);
                    IsIconDetailOpen = false;
                }
                else
                {
                    StatusMessage = "Не удалось применить значок к папке.";
                }
            }
        }

        [RelayCommand]
        public async Task ApplyDetailIconToShortcutAsync()
        {
            if (SelectedIconDetail == null) return;

            var dlg = new OpenFileDialog
            {
                Title = $"Выберите ярлык (.lnk) для применения значка «{SelectedIconDetail.Name}»",
                Filter = "Ярлыки Windows (*.lnk)|*.lnk"
            };

            if (dlg.ShowDialog() == true)
            {
                string shortcut = dlg.FileName;
                string activeTheme = IconThemeService.Instance.GetActiveThemeName();
                bool ok = IconThemeService.Instance.ApplyIconToShortcut(shortcut, SelectedIconDetail.GeometryKey, activeTheme);
                if (ok)
                {
                    StatusMessage = $"Значок «{SelectedIconDetail.Name}» успешно применен к ярлыку «{Path.GetFileName(shortcut)}»!";
                    TrayService.Instance.ShowNotification("Значки системы 🔗", StatusMessage);
                    IsIconDetailOpen = false;
                }
                else
                {
                    StatusMessage = "Не удалось обновить значок ярлыка.";
                }
            }
        }

        [RelayCommand]
        public async Task ApplyDetailIconAsSystemAsync(string target)
        {
            if (SelectedIconDetail == null) return;

            string activeTheme = IconThemeService.Instance.GetActiveThemeName();
            string customAssignedDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StormSystemOptimizer", "IconThemes", "CustomAssigned");
            if (!Directory.Exists(customAssignedDir)) Directory.CreateDirectory(customAssignedDir);

            string tempIco = Path.Combine(customAssignedDir, $"{SelectedIconDetail.GeometryKey}.ico");
            IconThemeService.Instance.ExportIconToFile(tempIco, SelectedIconDetail.GeometryKey, activeTheme);

            bool ok = IconThemeService.Instance.SetSystemIcon(target, tempIco);
            if (ok)
            {
                await IconThemeService.Instance.RebuildIconCacheAsync();
                RefreshAppliedThemeStatus();
                StatusMessage = $"Значок «{SelectedIconDetail.Name}» успешно назначен для «{target}»!";
                TrayService.Instance.ShowNotification("Значки системы 💻", StatusMessage);
                IsIconDetailOpen = false;
            }
            else
            {
                StatusMessage = "Не удалось назначить системный значок.";
            }
        }

        [RelayCommand]
        public void ExportDetailIconAsIco()
        {
            if (SelectedIconDetail == null) return;

            var dlg = new SaveFileDialog
            {
                Title = $"Экспортировать значок «{SelectedIconDetail.Name}»",
                Filter = "Значки Windows (*.ico)|*.ico",
                FileName = $"{SelectedIconDetail.Name.Replace(" ", "_")}.ico"
            };

            if (dlg.ShowDialog() == true)
            {
                string activeTheme = IconThemeService.Instance.GetActiveThemeName();
                bool ok = IconThemeService.Instance.ExportIconToFile(dlg.FileName, SelectedIconDetail.GeometryKey, activeTheme);
                if (ok)
                {
                    StatusMessage = $"Файл значка успешно сохранен: {dlg.FileName}";
                    TrayService.Instance.ShowNotification("Экспорт значка 💾", StatusMessage);
                    IsIconDetailOpen = false;
                }
            }
        }

        [RelayCommand]
        public void CopyDetailIconToClipboard()
        {
            if (SelectedIconDetail == null) return;

            try
            {
                string activeTheme = IconThemeService.Instance.GetActiveThemeName();
                var geo = IconGenerator.GetGeometryFromKey(SelectedIconDetail.GeometryKey);
                if (geo != null)
                {
                    var rtb = IconGenerator.RenderIconFrame(geo, 256, activeTheme);
                    Clipboard.SetImage(rtb);
                    StatusMessage = $"Изображение значка «{SelectedIconDetail.Name}» (256×256 PNG) скопировано в буфер обмена!";
                    TrayService.Instance.ShowNotification("Буфер обмена 📋", StatusMessage);
                }
            }
            catch
            {
                StatusMessage = "Не удалось скопировать значок в буфер обмена.";
            }
        }

        [RelayCommand]
        public void SelectAllCatalogIcons(object? parameter)
        {
            bool select = parameter is bool b ? b : (parameter is string s && bool.TryParse(s, out var parsed) && parsed);
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
            string search = CatalogSearchText?.Trim().ToLowerInvariant() ?? string.Empty;

            foreach (var icon in CatalogIcons)
            {
                bool matchesCategory = SelectedCatalogCategory == "Все" || icon.Category == SelectedCatalogCategory;
                bool matchesSearch = string.IsNullOrEmpty(search) ||
                                     icon.Name.ToLowerInvariant().Contains(search) ||
                                     icon.Category.ToLowerInvariant().Contains(search);

                if (matchesCategory && matchesSearch)
                {
                    FilteredCatalogIcons.Add(icon);
                }
            }
        }

        [RelayCommand]
        public async Task ApplySelectedCatalogIconsAsync()
        {
            var selected = CatalogIcons.Where(i => i.IsSelected).ToList();
            if (selected.Count == 0)
            {
                StatusMessage = "Выберите хотя бы один значок для применения.";
                return;
            }

            IsBusy = true;
            StatusMessage = $"Генерация и применение {selected.Count} значков в систему...";

            string activeTheme = IconThemeService.Instance.GetActiveThemeName();
            bool ok = await IconThemeService.Instance.ApplyCuratedThemeAsync(activeTheme);
            if (ok)
            {
                await IconThemeService.Instance.RebuildIconCacheAsync();
                RefreshAppliedThemeStatus();
                StatusMessage = $"Успешно применены {selected.Count} значков в стиле «{activeTheme}»!";
                TrayService.Instance.ShowNotification("Каталог значков 🎨", StatusMessage);
            }
            else
            {
                StatusMessage = "Не удалось применить значки.";
            }

            IsBusy = false;
        }

        [RelayCommand]
        public void BrowseCustomPackage()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите архив с пакетом значков",
                Filter = "Пакеты значков (*.zip;*.iconpack;*.ip;*.7tsp;*.ico)|*.zip;*.iconpack;*.ip;*.7tsp;*.ico|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                CustomPackagePath = dlg.FileName;
            }
        }

        [RelayCommand]
        public async Task InstallCustomPackageAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomPackagePath) || !File.Exists(CustomPackagePath)) return;

            IsBusy = true;
            StatusMessage = "Установка стороннего пакета значков...";

            bool ok = await IconThemeService.Instance.InstallIconPackageArchiveAsync(CustomPackagePath);
            if (ok)
            {
                string themeName = Path.GetFileNameWithoutExtension(CustomPackagePath);
                IconThemeService.Instance.SetActiveThemeName(themeName);
                await IconThemeService.Instance.RebuildIconCacheAsync();
                LoadThemes();
                StatusMessage = $"Сторонний пакет «{themeName}» успешно установлен!";
                TrayService.Instance.ShowNotification("Пакет значков", StatusMessage);
            }
            else
            {
                StatusMessage = "Ошибка при распаковке и установке пакета значков.";
            }

            IsBusy = false;
        }

        [RelayCommand]
        public void BrowseCustomIconFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите файл значка (.ico или .png)",
                Filter = "Файлы значков (*.ico;*.png)|*.ico;*.png|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                CustomIconFilePath = dlg.FileName;
            }
        }

        [RelayCommand]
        public void ApplyCustomSingleIcon()
        {
            if (string.IsNullOrWhiteSpace(CustomIconFilePath) || !File.Exists(CustomIconFilePath)) return;

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

            bool ok = IconThemeService.Instance.SetSystemIcon(targetKey, CustomIconFilePath);
            if (ok)
            {
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

            StatusMessage = $"Применение темы значков «{item.Title}»...";
            bool ok = await IconThemeService.Instance.ApplyCuratedThemeAsync(item.Title);
            if (ok)
            {
                await IconThemeService.Instance.RebuildIconCacheAsync();
                RefreshAppliedThemeStatus();
                StatusMessage = item.Title.Contains("Default") || item.Title.Contains("Стандартные")
                    ? "Все системные значки успешно возвращены к стандарту Windows!"
                    : $"Тема значков «{item.Title}» успешно активирована в системе!";
                TrayService.Instance.ShowNotification("Значки системы ⚡", StatusMessage);
            }
            else
            {
                StatusMessage = $"Не удалось применить тему «{item.Title}». Проверьте права администратора.";
            }

            IsBusy = false;
        }
    }
}
