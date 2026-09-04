using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class WallpapersViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _statusMessage = "Готов к персонализации обоев и экрана блокировки";

        [ObservableProperty]
        private string _selectedCategory = "Все";

        [ObservableProperty]
        private bool _isNoLockScreen = false;

        [ObservableProperty]
        private bool _isLockScreenTipsDisabled = false;

        [ObservableProperty]
        private string _customWallpaperPath = string.Empty;

        [ObservableProperty]
        private string _liveVideoPath = string.Empty;

        [ObservableProperty]
        private bool _isLiveWallpaperActive = false;

        public ObservableCollection<WallpaperItem> Wallpapers { get; } = new();
        public ObservableCollection<WallpaperItem> FilteredWallpapers { get; } = new();

        public ObservableCollection<string> Categories { get; } = new()
        {
            "Все",
            "STORM Dark",
            "Киберпанк",
            "Космос",
            "Природа",
            "Абстракция",
            "Минимализм",
            "Игры и Арт"
        };

        public WallpapersViewModel()
        {
            IsNoLockScreen = WallpaperService.Instance.IsNoLockScreenEnabled();
            IsLockScreenTipsDisabled = WallpaperService.Instance.IsLockScreenTipsDisabled();
            IsLiveWallpaperActive = WallpaperService.Instance.IsLiveWallpaperActive;
            LoadWallpapers();
        }

        private void LoadWallpapers()
        {
            Wallpapers.Clear();
            foreach (var wp in WallpaperService.Instance.GetCurated4KWallpapers())
            {
                Wallpapers.Add(wp);
            }
            FilterCategory(SelectedCategory);
        }

        [RelayCommand]
        public void FilterCategory(string cat)
        {
            SelectedCategory = cat;
            FilteredWallpapers.Clear();
            foreach (var wp in Wallpapers)
            {
                if (cat == "Все" || wp.Category.Equals(cat, StringComparison.OrdinalIgnoreCase))
                {
                    FilteredWallpapers.Add(wp);
                }
            }
        }

        [RelayCommand]
        public async Task ApplyDesktopWallpaperAsync(WallpaperItem item)
        {
            if (item == null) return;
            StatusMessage = $"Загрузка и установка обоев «{item.Title}» на рабочий стол...";
            bool ok = await WallpaperService.Instance.SetDesktopWallpaperAsync(item.SourceUrl);
            if (ok)
            {
                StatusMessage = $"Обои «{item.Title}» успешно установлены на рабочий стол!";
                TrayService.Instance.ShowNotification("Обои рабочего стола 🖼️", StatusMessage);
            }
            else
            {
                StatusMessage = "Не удалось применить обои рабочего стола.";
            }
        }

        [RelayCommand]
        public async Task ApplyLockScreenWallpaperAsync(WallpaperItem item)
        {
            if (item == null) return;
            StatusMessage = $"Установка обоев «{item.Title}» на экран блокировки...";
            bool ok = await WallpaperService.Instance.SetLockScreenWallpaperAsync(item.SourceUrl);
            if (ok)
            {
                StatusMessage = $"Обои «{item.Title}» успешно применены для экрана блокировки!";
                TrayService.Instance.ShowNotification("Экран блокировки 🔒", StatusMessage);
            }
            else
            {
                StatusMessage = "Не удалось обновить экран блокировки. Требуются права администратора.";
            }
        }

        [RelayCommand]
        public void ToggleNoLockScreen()
        {
            bool ok = WallpaperService.Instance.SetNoLockScreen(IsNoLockScreen);
            if (ok)
            {
                StatusMessage = IsNoLockScreen
                    ? "Экран блокировки полностью отключен. Вход в систему выполняется напрямую на рабочий стол!"
                    : "Экран блокировки Windows включен.";
                TrayService.Instance.ShowNotification("Экран блокировки", StatusMessage);
            }
        }

        [RelayCommand]
        public void ToggleLockScreenTips()
        {
            bool ok = WallpaperService.Instance.SetLockScreenTipsDisabled(IsLockScreenTipsDisabled);
            if (ok)
            {
                StatusMessage = IsLockScreenTipsDisabled
                    ? "Советы, реклама и факты на экране блокировки успешно отключены!"
                    : "Советы на экране блокировки включены.";
                TrayService.Instance.ShowNotification("Персонализация", StatusMessage);
            }
        }

        [RelayCommand]
        public void BrowseCustomWallpaper()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите фоновое изображение",
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.webp|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                CustomWallpaperPath = dlg.FileName;
            }
        }

        [RelayCommand]
        public async Task ApplyCustomDesktopWallpaperAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomWallpaperPath)) return;
            StatusMessage = "Применение выбранного изображения на рабочий стол...";
            bool ok = await WallpaperService.Instance.SetDesktopWallpaperAsync(CustomWallpaperPath);
            if (ok)
            {
                StatusMessage = "Пользовательские обои успешно установлены!";
                TrayService.Instance.ShowNotification("Обои рабочего стола", StatusMessage);
            }
        }

        [RelayCommand]
        public async Task ApplyCustomLockScreenWallpaperAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomWallpaperPath)) return;
            StatusMessage = "Применение выбранного изображения на экран блокировки...";
            bool ok = await WallpaperService.Instance.SetLockScreenWallpaperAsync(CustomWallpaperPath);
            if (ok)
            {
                StatusMessage = "Изображение экрана блокировки успешно обновлено!";
                TrayService.Instance.ShowNotification("Экран блокировки", StatusMessage);
            }
        }

        [RelayCommand]
        public void BrowseLiveVideo()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите видеофайл для живых обоев",
                Filter = "Видеофайлы (*.mp4;*.wmv;*.webm;*.avi)|*.mp4;*.wmv;*.webm;*.avi|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                LiveVideoPath = dlg.FileName;
            }
        }

        [RelayCommand]
        public void StartLiveWallpaper()
        {
            IntPtr workerW = WallpaperService.Instance.GetWorkerWHandle();
            if (workerW != IntPtr.Zero)
            {
                IsLiveWallpaperActive = true;
                StatusMessage = "Движок WorkerW успешно активирован под слоем значков рабочего стола!";
                TrayService.Instance.ShowNotification("Живые обои 🎬", StatusMessage);
            }
            else
            {
                StatusMessage = "Не удалось инициализировать слой WorkerW оболочки Проводника.";
            }
        }

        [RelayCommand]
        public void StopLiveWallpaper()
        {
            WallpaperService.Instance.DetachLiveWallpaper();
            IsLiveWallpaperActive = false;
            StatusMessage = "Живые обои остановлены. Восстановлен стандартный фон рабочего стола.";
            TrayService.Instance.ShowNotification("Живые обои", StatusMessage);
        }
    }
}
