using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class ContextMenuViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isClassicMenu = false;

        [ObservableProperty]
        private string _statusMessage = "Готов к настройке контекстного меню Windows";

        [ObservableProperty]
        private string _selectedCategory = "Все";

        [ObservableProperty]
        private string _activeTab = "Tweaks"; // Tweaks, ShellExtensions, CustomBuilder

        [ObservableProperty]
        private string _shellSearchText = string.Empty;

        // Custom Item Builder properties
        [ObservableProperty]
        private string _newItemTitle = string.Empty;

        [ObservableProperty]
        private string _newItemCommandPath = string.Empty;

        [ObservableProperty]
        private string _newItemIconPath = string.Empty;

        [ObservableProperty]
        private string _newItemScope = "Все файлы";

        [ObservableProperty]
        private bool _newItemRunAsAdmin = false;

        public ObservableCollection<ContextMenuItem> MenuItems { get; } = new();
        public ObservableCollection<ContextMenuItem> FilteredItems { get; } = new();

        public ObservableCollection<ShellExtensionItem> ShellExtensions { get; } = new();
        public ObservableCollection<ShellExtensionItem> FilteredShellExtensions { get; } = new();

        public ObservableCollection<string> AvailableScopes { get; } = new()
        {
            "Все файлы",
            "Папки",
            "Рабочий стол и фон",
            "Диски"
        };

        public bool IsTweaksTab => ActiveTab == "Tweaks";
        public bool IsShellExtensionsTab => ActiveTab == "ShellExtensions";
        public bool IsCustomBuilderTab => ActiveTab == "CustomBuilder";

        public ContextMenuViewModel()
        {
            IsClassicMenu = ContextMenuService.Instance.IsClassicWindows10MenuEnabled();
            LoadItems();
            ScanShellExtensions();
        }

        [RelayCommand]
        public void SetActiveTab(string tab)
        {
            ActiveTab = tab;
            OnPropertyChanged(nameof(IsTweaksTab));
            OnPropertyChanged(nameof(IsShellExtensionsTab));
            OnPropertyChanged(nameof(IsCustomBuilderTab));
        }

        private void LoadItems()
        {
            MenuItems.Clear();
            foreach (var item in ContextMenuService.Instance.GetPopularContextMenuItems())
            {
                MenuItems.Add(item);
            }
            FilterCategory(SelectedCategory);
        }

        [RelayCommand]
        public void FilterCategory(string category)
        {
            SelectedCategory = category;
            FilteredItems.Clear();
            foreach (var item in MenuItems)
            {
                if (category == "Все" || item.Category.Contains(category, StringComparison.OrdinalIgnoreCase))
                {
                    FilteredItems.Add(item);
                }
            }
        }

        [RelayCommand]
        public void ScanShellExtensions()
        {
            ShellExtensions.Clear();
            var items = ContextMenuService.Instance.ScanThirdPartyShellExtensions();
            foreach (var it in items)
            {
                ShellExtensions.Add(it);
            }
            FilterShellExtensions();
            StatusMessage = $"Обнаружено расширений Shell: {ShellExtensions.Count}";
        }

        partial void OnShellSearchTextChanged(string value)
        {
            FilterShellExtensions();
        }

        private void FilterShellExtensions()
        {
            FilteredShellExtensions.Clear();
            string q = ShellSearchText?.Trim() ?? string.Empty;
            foreach (var it in ShellExtensions)
            {
                if (string.IsNullOrEmpty(q) ||
                    it.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    it.Location.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    it.Clsid.Contains(q, StringComparison.OrdinalIgnoreCase))
                {
                    FilteredShellExtensions.Add(it);
                }
            }
        }

        [RelayCommand]
        public void ToggleShellExtension(ShellExtensionItem item)
        {
            if (item == null) return;
            bool target = !item.IsEnabled;
            bool ok = ContextMenuService.Instance.ToggleShellExtension(item, target);
            if (ok)
            {
                StatusMessage = target
                    ? $"Расширение «{item.Name}» включено!"
                    : $"Расширение «{item.Name}» отключено в контекстном меню.";
                TrayService.Instance.ShowNotification("Расширения Shell", StatusMessage);
                OnPropertyChanged(nameof(FilteredShellExtensions));
            }
            else
            {
                StatusMessage = $"Не удалось изменить состояние расширения «{item.Name}». Требуются права администратора.";
            }
        }

        [RelayCommand]
        public async Task ToggleClassicMenuAsync()
        {
            StatusMessage = "Переключение стиля контекстного меню...";
            bool target = !IsClassicMenu;
            bool ok = await ContextMenuService.Instance.ToggleWindows11ClassicMenuAsync(target);
            if (ok)
            {
                IsClassicMenu = target;
                StatusMessage = target
                    ? "Активировано классическое быстрое контекстное меню Windows 10!"
                    : "Активировано стандартное контекстное меню Windows 11.";
                TrayService.Instance.ShowNotification("Контекстное меню 🎨", StatusMessage);
            }
        }

        [RelayCommand]
        public async Task ToggleItemStateAsync(ContextMenuItem item)
        {
            if (item == null) return;
            item.IsEnabled = !item.IsEnabled;
            bool ok = await ContextMenuService.Instance.ToggleItemStateAsync(item);
            if (ok)
            {
                StatusMessage = item.IsEnabled
                    ? $"Пункт «{item.Title}» успешно добавлен в контекстное меню!"
                    : $"Пункт «{item.Title}» отключен в контекстном меню.";
                TrayService.Instance.ShowNotification("Контекстное меню", StatusMessage);
            }
        }

        [RelayCommand]
        public async Task CleanClutterAsync()
        {
            StatusMessage = "Очистка устаревших и мусорных пунктов контекстного меню...";
            bool ok = await ContextMenuService.Instance.CleanContextMenuClutterAsync();
            if (ok)
            {
                StatusMessage = "Контекстное меню очищено от лишних расширений и элементов 3D и Share.";
                TrayService.Instance.ShowNotification("Очистка меню ⚡", "Лишние пункты контекстного меню успешно удалены!");
                LoadItems();
            }
        }

        [RelayCommand]
        public void BrowseCommandPath()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите исполняемый файл или скрипт",
                Filter = "Исполняемые файлы и скрипты (*.exe;*.cmd;*.bat;*.ps1)|*.exe;*.cmd;*.bat;*.ps1|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                NewItemCommandPath = dlg.FileName;
                if (string.IsNullOrWhiteSpace(NewItemTitle))
                {
                    NewItemTitle = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                }
            }
        }

        [RelayCommand]
        public void BrowseIconPath()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите файл значка",
                Filter = "Файлы значков (*.ico;*.exe;*.dll)|*.ico;*.exe;*.dll|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                NewItemIconPath = dlg.FileName;
            }
        }

        [RelayCommand]
        public void CreateCustomItem()
        {
            if (string.IsNullOrWhiteSpace(NewItemTitle) || string.IsNullOrWhiteSpace(NewItemCommandPath))
            {
                StatusMessage = "Укажите название и исполняемую команду для нового пункта!";
                return;
            }

            bool ok = ContextMenuService.Instance.CreateCustomContextMenuItem(
                NewItemTitle.Trim(),
                NewItemScope,
                NewItemCommandPath.Trim(),
                NewItemIconPath.Trim(),
                NewItemRunAsAdmin
            );

            if (ok)
            {
                StatusMessage = $"Пункт меню «{NewItemTitle}» успешно создан и зарегистрирован!";
                TrayService.Instance.ShowNotification("Контекстное меню ⚡", StatusMessage);
                NewItemTitle = string.Empty;
                NewItemCommandPath = string.Empty;
                NewItemIconPath = string.Empty;
                NewItemRunAsAdmin = false;
                ScanShellExtensions();
            }
            else
            {
                StatusMessage = "Не удалось создать пункт контекстного меню. Проверьте права доступа.";
            }
        }
    }
}
