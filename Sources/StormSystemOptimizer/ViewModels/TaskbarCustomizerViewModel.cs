using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class TaskbarCustomizerViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _statusMessage = "Готов к настройке панели задач и меню Пуск";

        [ObservableProperty]
        private int _selectedAlignmentIndex = 1; // 0 = Left, 1 = Center

        [ObservableProperty]
        private int _selectedSizeIndex = 1; // 0 = Small, 1 = Medium, 2 = Large

        [ObservableProperty]
        private int _selectedGroupingIndex = 0; // 0 = Always, 1 = When full, 2 = Never

        [ObservableProperty]
        private int _selectedSearchBoxIndex = 1; // 0 = Hidden, 1 = Icon, 2 = Box, 3 = Button

        [ObservableProperty]
        private string _customStartIconPath = string.Empty;

        [ObservableProperty]
        private string _quickToolbarFolderName = "STORM Быстрый доступ";

        [ObservableProperty]
        private string _quickToolbarFolderPath = string.Empty;

        [ObservableProperty]
        private bool _isTaskbarAutoHide;

        [ObservableProperty]
        private bool _isTaskViewVisible;

        [ObservableProperty]
        private bool _isWidgetsVisible;

        [ObservableProperty]
        private bool _isSecondsInClock;

        [ObservableProperty]
        private bool _isHideRecommended;

        [ObservableProperty]
        private bool _isTaskbarLocked;

        public bool IsLeftAlignment => SelectedAlignmentIndex == 0;
        public bool IsCenterAlignment => SelectedAlignmentIndex == 1;

        public bool IsSmallSize => SelectedSizeIndex == 0;
        public bool IsMediumSize => SelectedSizeIndex == 1;
        public bool IsLargeSize => SelectedSizeIndex == 2;

        public bool IsGroupingAlways => SelectedGroupingIndex == 0;
        public bool IsGroupingWhenFull => SelectedGroupingIndex == 1;
        public bool IsGroupingNever => SelectedGroupingIndex == 2;

        public bool IsSearchHidden => SelectedSearchBoxIndex == 0;
        public bool IsSearchIcon => SelectedSearchBoxIndex == 1;
        public bool IsSearchBox => SelectedSearchBoxIndex == 2;
        public bool IsSearchButton => SelectedSearchBoxIndex == 3;

        public ObservableCollection<string> AlignmentOptions { get; } = new()
        {
            "Слева (классический стиль Windows 10)",
            "По центру (стиль Windows 11)"
        };

        public ObservableCollection<string> SizeOptions { get; } = new()
        {
            "Компактная (Small — тонкая панель)",
            "Стандартная (Medium — системный размер)",
            "Увеличенная (Large — крупные кнопки)"
        };

        public ObservableCollection<string> GroupingOptions { get; } = new()
        {
            "Всегда объединять кнопки и скрывать метки",
            "Объединять только при переполнении панели задач",
            "Никогда не объединять (всегда показывать текстовые заголовки)"
        };

        public ObservableCollection<string> SearchOptions { get; } = new()
        {
            "Скрыть поиск с панели задач",
            "Только значок поиска (лупа)",
            "Полноразмерное поле поиска",
            "Кнопка поиска со значком"
        };

        public TaskbarCustomizerViewModel()
        {
            SelectedAlignmentIndex = TaskbarCustomizerService.Instance.GetTaskbarAlignment();
            SelectedSizeIndex = TaskbarCustomizerService.Instance.GetTaskbarSize();
            SelectedGroupingIndex = TaskbarCustomizerService.Instance.GetTaskbarGrouping();
            SelectedSearchBoxIndex = TaskbarCustomizerService.Instance.GetSearchBoxMode();

            _isTaskbarAutoHide = TaskbarCustomizerService.Instance.GetAutoHideTaskbar();
            _isTaskViewVisible = TaskbarCustomizerService.Instance.GetTaskViewButton();
            _isWidgetsVisible = TaskbarCustomizerService.Instance.GetWidgetsButton();
            _isSecondsInClock = TaskbarCustomizerService.Instance.GetShowSecondsInClock();
            _isHideRecommended = TaskbarCustomizerService.Instance.GetHideRecommendedStart();
            _isTaskbarLocked = TaskbarCustomizerService.Instance.GetLockTaskbar();
        }

        [RelayCommand]
        public void ToggleAutoHide()
        {
            IsTaskbarAutoHide = !IsTaskbarAutoHide;
            TaskbarCustomizerService.Instance.SetAutoHideTaskbar(IsTaskbarAutoHide);
            StatusMessage = IsTaskbarAutoHide ? "Автоматическое скрытие панели задач активировано!" : "Автоматическое скрытие панели задач отключено.";
            TrayService.Instance.ShowNotification("Панель задач", StatusMessage);
        }

        [RelayCommand]
        public void SelectAlignment(int index)
        {
            SelectedAlignmentIndex = index;
            OnPropertyChanged(nameof(IsLeftAlignment));
            OnPropertyChanged(nameof(IsCenterAlignment));
            ApplyAlignment();
        }

        [RelayCommand]
        public void SelectSize(int index)
        {
            SelectedSizeIndex = index;
            OnPropertyChanged(nameof(IsSmallSize));
            OnPropertyChanged(nameof(IsMediumSize));
            OnPropertyChanged(nameof(IsLargeSize));
            ApplySize();
        }

        [RelayCommand]
        public void SelectGrouping(int index)
        {
            SelectedGroupingIndex = index;
            OnPropertyChanged(nameof(IsGroupingAlways));
            OnPropertyChanged(nameof(IsGroupingWhenFull));
            OnPropertyChanged(nameof(IsGroupingNever));
            ApplyGrouping();
        }

        [RelayCommand]
        public void SelectSearchBox(int index)
        {
            SelectedSearchBoxIndex = index;
            OnPropertyChanged(nameof(IsSearchHidden));
            OnPropertyChanged(nameof(IsSearchIcon));
            OnPropertyChanged(nameof(IsSearchBox));
            OnPropertyChanged(nameof(IsSearchButton));
            ApplySearchBox();
        }

        [RelayCommand]
        public void ToggleTaskView()
        {
            IsTaskViewVisible = !IsTaskViewVisible;
            TaskbarCustomizerService.Instance.SetTaskViewButton(IsTaskViewVisible);
            StatusMessage = IsTaskViewVisible ? "Кнопка «Вид задач» включена на панели задач." : "Кнопка «Вид задач» скрыта с панели задач.";
            TrayService.Instance.ShowNotification("Панель задач", StatusMessage);
        }

        [RelayCommand]
        public void ToggleWidgets()
        {
            IsWidgetsVisible = !IsWidgetsVisible;
            TaskbarCustomizerService.Instance.SetWidgetsButton(IsWidgetsVisible);
            StatusMessage = IsWidgetsVisible ? "Виджеты и погода включены на панели задач." : "Виджеты и погода скрыты с панели задач.";
            TrayService.Instance.ShowNotification("Панель задач", StatusMessage);
        }

        [RelayCommand]
        public void ToggleSecondsInClock()
        {
            IsSecondsInClock = !IsSecondsInClock;
            TaskbarCustomizerService.Instance.SetShowSecondsInClock(IsSecondsInClock);
            StatusMessage = IsSecondsInClock ? "Отображение секунд в часах трея активировано!" : "Отображение секунд в часах трея отключено.";
            TrayService.Instance.ShowNotification("Часы Windows ⏱", StatusMessage);
        }

        [RelayCommand]
        public void ToggleHideRecommended()
        {
            IsHideRecommended = !IsHideRecommended;
            TaskbarCustomizerService.Instance.SetHideRecommendedStart(IsHideRecommended);
            StatusMessage = IsHideRecommended ? "Раздел «Рекомендуем» в меню Пуск скрыт." : "Раздел «Рекомендуем» в меню Пуск включен.";
            TrayService.Instance.ShowNotification("Меню Пуск", StatusMessage);
        }

        [RelayCommand]
        public void ToggleLockTaskbar()
        {
            IsTaskbarLocked = !IsTaskbarLocked;
            TaskbarCustomizerService.Instance.SetLockTaskbar(IsTaskbarLocked);
            StatusMessage = IsTaskbarLocked ? "Панель задач заблокирована от случайного перемещения." : "Панель задач разблокирована.";
            TrayService.Instance.ShowNotification("Панель задач", StatusMessage);
        }

        [RelayCommand]
        public void ApplyAlignment()
        {
            bool ok = TaskbarCustomizerService.Instance.SetTaskbarAlignment(SelectedAlignmentIndex);
            if (ok)
            {
                StatusMessage = SelectedAlignmentIndex == 0
                    ? "Выравнивание панели задач переключено влево (классика Windows 10)!"
                    : "Выравнивание панели задач переключено по центру (Windows 11).";
                TrayService.Instance.ShowNotification("Панель задач", StatusMessage);
            }
        }

        [RelayCommand]
        public void ApplySize()
        {
            bool ok = TaskbarCustomizerService.Instance.SetTaskbarSize(SelectedSizeIndex);
            if (ok)
            {
                StatusMessage = "Размер панели задач успешно изменен. Перезапустите Проводник при необходимости.";
                TrayService.Instance.ShowNotification("Панель задач", StatusMessage);
            }
        }

        [RelayCommand]
        public void ApplyGrouping()
        {
            bool ok = TaskbarCustomizerService.Instance.SetTaskbarGrouping(SelectedGroupingIndex);
            if (ok)
            {
                StatusMessage = "Режим группировки кнопок панели задач успешно обновлен!";
                TrayService.Instance.ShowNotification("Панель задач", StatusMessage);
            }
        }

        [RelayCommand]
        public void ApplySearchBox()
        {
            bool ok = TaskbarCustomizerService.Instance.SetSearchBoxMode(SelectedSearchBoxIndex);
            if (ok)
            {
                StatusMessage = "Режим отображения поиска на панели задач успешно обновлен!";
                TrayService.Instance.ShowNotification("Панель задач", StatusMessage);
            }
        }

        [RelayCommand]
        public void ApplyAcrylicBlur()
        {
            bool ok = TaskbarCustomizerService.Instance.ApplyTaskbarAcrylicBlur(180, 10, 11, 16);
            if (ok)
            {
                StatusMessage = "Акриловое размытие Acrylic Blur успешно применено к панели задач!";
                TrayService.Instance.ShowNotification("Акриловая панель задач ✨", StatusMessage);
            }
        }

        [RelayCommand]
        public void ApplyTransparent()
        {
            bool ok = TaskbarCustomizerService.Instance.ApplyTaskbarTransparent();
            if (ok)
            {
                StatusMessage = "Полная прозрачность панели задач успешно активирована!";
                TrayService.Instance.ShowNotification("Прозрачная панель задач 💎", StatusMessage);
            }
        }

        [RelayCommand]
        public void ResetTaskbarStyle()
        {
            bool ok = TaskbarCustomizerService.Instance.ResetTaskbarStyle();
            if (ok)
            {
                StatusMessage = "Стиль панели задач возвращен к стандартному системному.";
                TrayService.Instance.ShowNotification("Панель задач", StatusMessage);
            }
        }

        [RelayCommand]
        public void BrowseStartIcon()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите значок кнопки Пуск",
                Filter = "Изображения и значки (*.png;*.ico)|*.png;*.ico|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                CustomStartIconPath = dlg.FileName;
            }
        }

        [RelayCommand]
        public void ApplyStartIcon()
        {
            if (string.IsNullOrWhiteSpace(CustomStartIconPath)) return;
            bool ok = TaskbarCustomizerService.Instance.SetCustomStartIcon(CustomStartIconPath);
            if (ok)
            {
                StatusMessage = "Кастомный значок кнопки Пуск успешно сохранен и применен!";
                TrayService.Instance.ShowNotification("Кнопка Пуск 🔘", StatusMessage);
            }
        }

        [RelayCommand]
        public void BrowseQuickToolbarFolder()
        {
            var dlg = new OpenFolderDialog
            {
                Title = "Выберите папку для быстрой панели инструментов"
            };

            if (dlg.ShowDialog() == true)
            {
                QuickToolbarFolderPath = dlg.FolderName;
                if (string.IsNullOrWhiteSpace(QuickToolbarFolderName) || QuickToolbarFolderName == "STORM Быстрый доступ")
                {
                    QuickToolbarFolderName = System.IO.Path.GetFileName(dlg.FolderName);
                }
            }
        }

        [RelayCommand]
        public void AddQuickToolbarFolder()
        {
            if (string.IsNullOrWhiteSpace(QuickToolbarFolderPath)) return;
            bool ok = TaskbarCustomizerService.Instance.CreateQuickToolbarFolder(QuickToolbarFolderName, QuickToolbarFolderPath);
            if (ok)
            {
                StatusMessage = $"Папка «{QuickToolbarFolderName}» успешно закреплена на панели задач!";
                TrayService.Instance.ShowNotification("Панель инструментов 📁", StatusMessage);
            }
        }

        [RelayCommand]
        public void RestartExplorer()
        {
            StatusMessage = "Перезапуск Проводника Windows (explorer.exe)...";
            TaskbarCustomizerService.Instance.RestartExplorer();
            StatusMessage = "Проводник успешно перезапущен!";
        }
    }
}
