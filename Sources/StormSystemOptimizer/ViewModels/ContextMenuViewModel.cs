using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class ContextMenuViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isClassicMenu = false;

        [ObservableProperty]
        private string _statusMessage = "Готов к настройке контекстного меню Windows 10/11";

        public ObservableCollection<ContextMenuItem> MenuItems { get; } = new();

        public ContextMenuViewModel()
        {
            IsClassicMenu = ContextMenuService.Instance.IsClassicWindows10MenuEnabled();
            LoadItems();
        }

        private void LoadItems()
        {
            MenuItems.Clear();
            foreach (var item in ContextMenuService.Instance.GetPopularContextMenuItems())
            {
                MenuItems.Add(item);
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
        public async Task CleanClutterAsync()
        {
            StatusMessage = "Очистка устаревших и мусорных пунктов контекстного меню...";
            bool ok = await ContextMenuService.Instance.CleanContextMenuClutterAsync();
            if (ok)
            {
                StatusMessage = "Контекстное меню очищено от лишних расширений и элементов 3D/Share.";
                TrayService.Instance.ShowNotification("Очистка меню ⚡", "Лишние пункты контекстного меню успешно удалены!");
            }
        }
    }
}
