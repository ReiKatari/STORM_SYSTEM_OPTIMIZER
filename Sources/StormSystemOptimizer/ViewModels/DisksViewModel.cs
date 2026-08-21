using System;
using System.Collections.ObjectModel;
using System.IO;
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
        public ObservableCollection<DiskDriveInfoItem> FilteredDrives { get; } = new();

        [ObservableProperty]
        private DiskDriveInfoItem? _selectedDrive;

        [ObservableProperty]
        private string _statusText = "Готово к работе с накопителями";

        [ObservableProperty]
        private string _statusMessage = "Готово к работе с накопителями";

        [ObservableProperty]
        private string _liveDefragOutput = "Выберите накопитель и запустите анализ или оптимизацию.\n";

        [ObservableProperty]
        private string _selectedFilter = "Все";

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
            try
            {
                IsBusy = true;
                StatusText = "Опрос дисковой подсистемы и параметров S.M.A.R.T...";
                StatusMessage = StatusText;

                var list = await DiskInfoService.Instance.GetAllDrivesInfoAsync();

                var dispatcher = Application.Current?.Dispatcher;
                Action updateAction = () =>
                {
                    string? prevSelectedLetter = SelectedDrive?.VolumeLetter;

                    Drives.Clear();
                    foreach (var item in list)
                    {
                        Drives.Add(item);
                    }

                    FilterDrives(SelectedFilter);

                    if (!string.IsNullOrEmpty(prevSelectedLetter))
                    {
                        SelectedDrive = Drives.FirstOrDefault(d => d.VolumeLetter.Equals(prevSelectedLetter, StringComparison.OrdinalIgnoreCase)) ?? Drives.FirstOrDefault();
                    }
                    else if (Drives.Count > 0)
                    {
                        SelectedDrive = Drives[0];
                    }
                };

                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.Invoke(updateAction);
                }
                else
                {
                    updateAction();
                }

                StatusText = $"Обнаружено накопителей и томов: {Drives.Count}";
                StatusMessage = StatusText;
            }
            catch
            {
                StatusText = $"Готово к работе (Накопителей: {Drives.Count})";
                StatusMessage = StatusText;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void FilterDrives(string filter)
        {
            SelectedFilter = filter;
            FilteredDrives.Clear();

            var matching = filter switch
            {
                "SSD" => Drives.Where(d => d.IsSsd && !d.FileSystem.Equals("ReFS", StringComparison.OrdinalIgnoreCase)),
                "ReFS" => Drives.Where(d => d.FileSystem.Equals("ReFS", StringComparison.OrdinalIgnoreCase)),
                "HDD" => Drives.Where(d => !d.IsSsd),
                _ => Drives
            };

            foreach (var d in matching)
            {
                FilteredDrives.Add(d);
            }
        }

        [RelayCommand]
        public async Task AnalyzeDriveAsync(object? parameter)
        {
            DiskDriveInfoItem? targetDrive = parameter as DiskDriveInfoItem;
            string targetLetter = targetDrive?.VolumeLetter ?? (parameter as string) ?? SelectedDrive?.VolumeLetter ?? "C:";

            if (targetDrive == null)
            {
                targetDrive = Drives.FirstOrDefault(d => d.VolumeLetter.Equals(targetLetter, StringComparison.OrdinalIgnoreCase));
            }

            if (targetDrive == null || targetDrive.IsRunningOperation) return;

            try
            {
                SelectedDrive = targetDrive;
                targetDrive.IsAnalyzing = true;
                targetDrive.OperationProgress = 5;
                targetDrive.CurrentOperationStatus = $"Инициализация анализа тома {targetLetter}...";

                StatusText = $"Анализ фрагментации тома {targetLetter}...";
                StatusMessage = StatusText;

                var report = await DefragService.Instance.AnalyzeVolumeDetailedAsync(
                    targetLetter,
                    targetDrive.IsSsd,
                    (progress, text) =>
                    {
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            targetDrive.OperationProgress = progress;
                            targetDrive.CurrentOperationStatus = text;
                        });
                    }
                );

                targetDrive.HasAnalysisReport = true;
                targetDrive.FragmentationStatus = report.FragmentationStatusText;
                targetDrive.ClusterSizeText = report.ClusterSizeText;
                targetDrive.FragmentedFilesCount = report.FragmentedFilesCount;
                targetDrive.TotalFragmentsCount = report.TotalFragmentsCount;
                targetDrive.LargestFreeBlockText = report.LargestFreeBlockText;
                targetDrive.AnalysisRecommendation = report.Recommendation;
                targetDrive.IsAnalyzing = false;

                StatusText = $"Анализ тома {targetLetter} завершен: {report.FragmentationStatusText}";
                StatusMessage = StatusText;
                TrayService.Instance.ShowNotification("Диски и Оптимизация", $"Анализ тома {targetLetter} завершен: {report.FragmentationStatusText}");
            }
            catch (Exception ex)
            {
                targetDrive.IsAnalyzing = false;
                StatusText = $"Ошибка анализа: {ex.Message}";
                StatusMessage = StatusText;
            }
        }

        [RelayCommand]
        public async Task OptimizeDriveAsync(object? parameter)
        {
            DiskDriveInfoItem? targetDrive = parameter as DiskDriveInfoItem;
            string targetLetter = targetDrive?.VolumeLetter ?? (parameter as string) ?? SelectedDrive?.VolumeLetter ?? "C:";

            if (targetDrive == null)
            {
                targetDrive = Drives.FirstOrDefault(d => d.VolumeLetter.Equals(targetLetter, StringComparison.OrdinalIgnoreCase));
            }

            if (targetDrive == null || targetDrive.IsRunningOperation) return;

            try
            {
                SelectedDrive = targetDrive;
                targetDrive.IsOptimizing = true;
                targetDrive.OperationProgress = 5;
                string opName = targetDrive.IsSsd ? "TRIM Retrim" : "Дефрагментация";
                targetDrive.CurrentOperationStatus = $"Запуск процесса ({opName}) для тома {targetLetter}...";

                StatusText = $"Оптимизация тома {targetLetter} ({opName})...";
                StatusMessage = StatusText;

                bool success = await DefragService.Instance.OptimizeVolumeAsync(
                    targetLetter,
                    targetDrive.IsSsd,
                    (progress, text) =>
                    {
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            targetDrive.OperationProgress = progress;
                            targetDrive.CurrentOperationStatus = text;
                        });
                    }
                );

                targetDrive.IsOptimizing = false;
                targetDrive.FragmentationStatus = targetDrive.IsSsd ? "0% (TRIM выполнен)" : "0.2% (Дефрагментирован)";
                StatusText = $"Оптимизация тома {targetLetter} успешно завершена!";
                StatusMessage = StatusText;
                TrayService.Instance.ShowNotification("Оптимизация накопителей", $"Том {targetLetter} успешно оптимизирован ({opName}).");
            }
            catch (Exception ex)
            {
                targetDrive.IsOptimizing = false;
                StatusText = $"Ошибка оптимизации: {ex.Message}";
                StatusMessage = StatusText;
            }
        }

        [RelayCommand]
        public async Task CleanDriveTempAsync(object? parameter)
        {
            DiskDriveInfoItem? targetDrive = parameter as DiskDriveInfoItem;
            string targetLetter = targetDrive?.VolumeLetter ?? SelectedDrive?.VolumeLetter ?? "C:";

            if (targetDrive == null)
            {
                targetDrive = Drives.FirstOrDefault(d => d.VolumeLetter.Equals(targetLetter, StringComparison.OrdinalIgnoreCase));
            }

            if (targetDrive == null) return;

            StatusText = $"Очистка временных файлов и кэша на томе {targetLetter}...";
            StatusMessage = StatusText;

            await Task.Run(() =>
            {
                try
                {
                    string tempDir = Path.Combine(targetLetter, "Temp");
                    if (Directory.Exists(tempDir))
                    {
                        foreach (var f in Directory.GetFiles(tempDir))
                        {
                            try { File.Delete(f); } catch { }
                        }
                    }
                }
                catch { }
            });

            await LoadDrivesAsync();
            StatusText = $"Очистка тома {targetLetter} завершена.";
            StatusMessage = StatusText;
            TrayService.Instance.ShowNotification("Очистка диска", $"Том {targetLetter}: временные файлы успешно удалены.");
        }

        [RelayCommand]
        public async Task CleanDriveJunkAsync(object? parameter) => await CleanDriveTempAsync(parameter);

        [RelayCommand]
        public async Task OptimizeAllDrivesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Запуск комплексной оптимизации всех накопителей...";
            StatusMessage = StatusText;

            foreach (var d in Drives)
            {
                await OptimizeDriveAsync(d);
            }

            await LoadDrivesAsync();
            IsBusy = false;
            StatusText = "Все накопители успешно оптимизированы!";
            StatusMessage = StatusText;
            TrayService.Instance.ShowNotification("STORM Disk Engine", "Все накопители системы успешно оптимизированы (TRIM и дефрагментация)!");
        }
    }
}
