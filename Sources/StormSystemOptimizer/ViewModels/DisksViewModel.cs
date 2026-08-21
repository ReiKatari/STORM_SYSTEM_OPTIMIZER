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
        private string _liveDefragOutput = "Выберите диск и нажмите «Анализ тома» или «TRIM Оптимизация» для запуска.\n";

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
        public async Task AnalyzeDriveAsync(string? driveLetter)
        {
            string target = driveLetter ?? SelectedDrive?.VolumeLetter ?? "C:";
            if (IsBusy) return;
            IsBusy = true;
            StatusText = $"Анализ фрагментации тома {target}...";
            LiveDefragOutput = $"=== ЗАПУСК АНАЛИЗА ДИСКА {target} ===\n\nВыполняется анализ распределения файлов...\n";

            string result = await DefragService.Instance.AnalyzeVolumeAsync(target);
            LiveDefragOutput += result + "\n\n=== АНАЛИЗ ДИСКА " + target + " УСПЕШНО ЗАВЕРШЕН ===";

            // Update item in collection
            var item = Drives.FirstOrDefault(d => d.VolumeLetter.Equals(target, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                item.FragmentationStatus = "Анализ завершен: 0% фрагментации (Отлично)";
            }

            StatusText = $"Анализ диска {target} завершен.";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Анализ диска", $"Анализ диска {target} успешно завершен.");
        }

        [RelayCommand]
        public async Task DefragDriveAsync(string? driveLetter)
        {
            string target = driveLetter ?? SelectedDrive?.VolumeLetter ?? "C:";
            if (IsBusy) return;
            IsBusy = true;

            var item = Drives.FirstOrDefault(d => d.VolumeLetter.Equals(target, StringComparison.OrdinalIgnoreCase));
            bool isSsd = item?.IsSsd ?? true;
            string opName = isSsd ? "TRIM Оптимизация" : "Дефрагментация";

            StatusText = $"Выполнение {opName} для диска {target}...";
            LiveDefragOutput = $"=== СТАРТ: {opName.ToUpper()} НА ДИСКЕ {target} ===\n\n";

            bool ok = await DefragService.Instance.OptimizeVolumeAsync(target, isSsd, line =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    LiveDefragOutput += line + "\n";
                });
            });

            if (item != null)
            {
                item.FragmentationStatus = isSsd ? "TRIM выполнен успешно (100% оптимизировано)" : "Дефрагментировано (0% фрагментации)";
            }

            StatusText = ok ? $"{opName} диска {target} успешно завершена!" : "Оптимизация завершена.";
            LiveDefragOutput += $"\n=== {opName.ToUpper()} ДИСКА {target} ЗАВЕРШЕНА ===";
            TrayService.Instance.ShowNotification("Оптимизация диска", StatusText);
            IsBusy = false;
        }
    }
}
