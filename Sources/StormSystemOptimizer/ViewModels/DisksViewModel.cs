using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class DisksViewModel : ObservableObject
    {
        public ObservableCollection<DiskDriveInfoItem> Drives { get; } = new();

        [ObservableProperty]
        private DiskDriveInfoItem? _selectedDrive;

        [ObservableProperty]
        private string _statusText = "Готово к работе с дисками";

        [ObservableProperty]
        private string _logOutput = "Выберите диск и нажмите «Анализ фрагментации» или «Глубокая оптимизация / TRIM».\n";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy = false;

        public bool IsNotBusy => !IsBusy;

        public DisksViewModel()
        {
            _ = LoadDrivesAsync();
        }

        [RelayCommand]
        public async Task LoadDrivesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Опрос дисковой подсистемы и параметров S.M.A.R.T...";

            var list = await DiskInfoService.Instance.GetAllDrivesInfoAsync();
            Drives.Clear();
            foreach (var item in list)
            {
                Drives.Add(item);
            }

            if (Drives.Count > 0 && SelectedDrive == null)
            {
                SelectedDrive = Drives[0];
            }

            IsBusy = false;
            StatusText = $"Обнаружено локальных накопителей: {Drives.Count}";
        }

        [RelayCommand]
        public async Task AnalyzeSelectedDriveAsync()
        {
            if (SelectedDrive == null || IsBusy) return;
            IsBusy = true;
            StatusText = $"Анализ фрагментации тома {SelectedDrive.VolumeLetter}...";
            LogOutput = $"=== ЗАПУСК АНАЛИЗА ДИСКА {SelectedDrive.VolumeLetter} ===\n\n";

            string result = await DefragService.Instance.AnalyzeVolumeAsync(SelectedDrive.VolumeLetter);
            LogOutput += result + "\n\n=== АНАЛИЗ ЗАВЕРШЕН ===";

            StatusText = $"Анализ диска {SelectedDrive.VolumeLetter} завершен.";
            IsBusy = false;
        }

        [RelayCommand]
        public async Task OptimizeSelectedDriveAsync()
        {
            if (SelectedDrive == null || IsBusy) return;
            IsBusy = true;
            string opType = SelectedDrive.IsSsd ? "TRIM / Оптимизация ячеек" : "Глубокая дефрагментация";
            StatusText = $"Выполнение {opType} для диска {SelectedDrive.VolumeLetter}...";
            LogOutput = $"=== СТАРТ: {opType} НА ДИСКЕ {SelectedDrive.VolumeLetter} ===\n\n";

            bool ok = await DefragService.Instance.OptimizeVolumeAsync(SelectedDrive.VolumeLetter, SelectedDrive.IsSsd, line =>
            {
                LogOutput += line + "\n";
            });

            StatusText = ok ? $"Оптимизация {SelectedDrive.VolumeLetter} успешно завершена!" : "Ошибка при оптимизации.";
            TrayService.Instance.ShowNotification("Оптимизация диска", StatusText);
            IsBusy = false;
        }
    }
}
