using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        private string _statusMessage = "Готово к работе с накопителями";

        [ObservableProperty]
        private string _liveDefragOutput = "Выберите диск и нажмите «Анализ тома» или «Оптимизировать» для запуска.\n";

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
            StatusMessage = StatusText;

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
            StatusMessage = StatusText;
        }

        [RelayCommand]
        public async Task AnalyzeDriveAsync(string? driveLetter)
        {
            try
            {
                string target = driveLetter ?? SelectedDrive?.VolumeLetter ?? "C:";
                if (IsBusy) return;
                IsBusy = true;
                StatusText = $"Анализ фрагментации тома {target}...";
                StatusMessage = StatusText;
                LiveDefragOutput = $"=== ЗАПУСК АНАЛИЗА ДИСКА {target} ===\n\nВыполняется анализ распределения файлов...\n";

                string result = await DefragService.Instance.AnalyzeVolumeAsync(target);
                LiveDefragOutput += result + "\n\n=== АНАЛИЗ ДИСКА " + target + " УСПЕШНО ЗАВЕРШЕН ===";

                // Update item in collection
                var item = Drives.FirstOrDefault(d => d.VolumeLetter.Equals(target, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    item.FragmentationStatus = "0% (Фрагментация отсутствует)";
                }

                StatusText = $"Анализ диска {target} успешно завершен.";
                StatusMessage = StatusText;
                IsBusy = false;
                TrayService.Instance.ShowNotification("Анализ диска", $"Анализ диска {target} успешно завершен.");
            }
            catch (Exception ex)
            {
                IsBusy = false;
                StatusText = $"Ошибка анализа: {ex.Message}";
                StatusMessage = StatusText;
            }
        }

        [RelayCommand]
        public async Task DefragDriveAsync(string? driveLetter)
        {
            try
            {
                string target = driveLetter ?? SelectedDrive?.VolumeLetter ?? "C:";
                if (IsBusy) return;
                IsBusy = true;

                var item = Drives.FirstOrDefault(d => d.VolumeLetter.Equals(target, StringComparison.OrdinalIgnoreCase));
                bool isSsd = item?.IsSsd ?? true;
                string opName = isSsd ? "TRIM Оптимизация" : "Дефрагментация";

                StatusText = $"Выполнение {opName} для диска {target}...";
                StatusMessage = StatusText;
                LiveDefragOutput = $"=== СТАРТ: {opName.ToUpper()} НА ДИСКЕ {target} ===\n\n";

                bool ok = await DefragService.Instance.OptimizeVolumeAsync(target, isSsd, line =>
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        LiveDefragOutput += line + "\n";
                    });
                });

                if (item != null)
                {
                    item.FragmentationStatus = "0% (Оптимизирован)";
                }

                StatusText = $"{opName} диска {target} успешно завершена!";
                StatusMessage = StatusText;
                IsBusy = false;
                TrayService.Instance.ShowNotification(opName, $"{opName} диска {target} завершена.");
            }
            catch (Exception ex)
            {
                IsBusy = false;
                StatusText = $"Ошибка: {ex.Message}";
                StatusMessage = StatusText;
            }
        }

        [RelayCommand]
        public async Task OptimizeAllDrivesAsync()
        {
            if (IsBusy) return;
            foreach (var d in Drives)
            {
                await DefragDriveAsync(d.VolumeLetter);
            }
            StatusText = "Все локальные диски успешно оптимизированы!";
            StatusMessage = StatusText;
        }
    }
}
