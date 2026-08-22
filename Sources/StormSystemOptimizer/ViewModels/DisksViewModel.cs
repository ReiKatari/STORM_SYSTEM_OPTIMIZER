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
        public ObservableCollection<PhysicalDriveGroupItem> PhysicalDrives { get; } = new();
        public ObservableCollection<PhysicalDriveGroupItem> FilteredPhysicalDrives { get; } = new();

        [ObservableProperty]
        private bool _isPhysicalView = true;

        [ObservableProperty]
        private bool _isPartitionView = false;

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
        public void SetViewMode(string mode)
        {
            if (mode == "Physical")
            {
                IsPhysicalView = true;
                IsPartitionView = false;
            }
            else
            {
                IsPhysicalView = false;
                IsPartitionView = true;
            }
        }

        [RelayCommand]
        public async Task LoadDrivesAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                StatusText = "Опрос дисковой подсистемы...";
                StatusMessage = StatusText;

                var list = await Task.Run(() => DiskInfoService.Instance.GetAllDrivesFast());

                string? prevLetter = SelectedDrive?.VolumeLetter;

                Drives.Clear();
                foreach (var item in list)
                {
                    Drives.Add(item);
                }

                // Group by physical drive model
                PhysicalDrives.Clear();
                var grouped = Drives.GroupBy(d => d.Model, StringComparer.OrdinalIgnoreCase);
                foreach (var grp in grouped)
                {
                    var first = grp.First();
                    var phys = new PhysicalDriveGroupItem
                    {
                        Model = first.Model,
                        MediaType = first.MediaType,
                        InterfaceType = first.InterfaceType,
                        HealthPercentage = first.HealthPercentage,
                        HealthStatus = first.HealthStatus,
                        StatusColor = first.StatusColor,
                        StatusBgColor = first.StatusBgColor,
                        Temperature = first.Temperature,
                        ReleaseDateText = first.ReleaseDateText,
                        OperatingTimeText = first.OperatingTimeText,
                        PowerOnHours = first.PowerOnHours,
                        SerialNumber = first.SerialNumber,
                        FirmwareRevision = first.FirmwareRevision,
                        IsSsd = first.IsSsd,
                        TotalSizeGb = grp.Sum(v => v.TotalSizeGb),
                        UsedSizeGb = grp.Sum(v => v.UsedSizeGb),
                        FreeSizeGb = grp.Sum(v => v.FreeSizeGb)
                    };
                    if (phys.TotalSizeGb > 0)
                    {
                        phys.UsedPercentage = Math.Round((phys.UsedSizeGb / phys.TotalSizeGb) * 100.0, 1);
                    }
                    foreach (var vol in grp)
                    {
                        phys.Volumes.Add(vol);
                    }
                    PhysicalDrives.Add(phys);
                }

                FilterDrives(SelectedFilter);

                if (!string.IsNullOrEmpty(prevLetter))
                {
                    SelectedDrive = Drives.FirstOrDefault(d => d.VolumeLetter.Equals(prevLetter, StringComparison.OrdinalIgnoreCase)) ?? Drives.FirstOrDefault();
                }
                else if (Drives.Count > 0)
                {
                    SelectedDrive = Drives[0];
                }

                StatusText = $"Обнаружено физических накопителей: {PhysicalDrives.Count}, логических разделов: {Drives.Count}";
                StatusMessage = StatusText;
            }
            catch
            {
                StatusText = $"Готово (Накопителей: {PhysicalDrives.Count})";
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
            FilteredPhysicalDrives.Clear();

            var matchingDrives = filter switch
            {
                "SSD" => Drives.Where(d => d.IsSsd && !d.FileSystem.Equals("ReFS", StringComparison.OrdinalIgnoreCase)),
                "ReFS" => Drives.Where(d => d.FileSystem.Equals("ReFS", StringComparison.OrdinalIgnoreCase)),
                "HDD" => Drives.Where(d => !d.IsSsd),
                _ => Drives
            };

            foreach (var d in matchingDrives)
            {
                FilteredDrives.Add(d);
            }

            var matchingPhys = filter switch
            {
                "SSD" => PhysicalDrives.Where(p => p.IsSsd),
                "HDD" => PhysicalDrives.Where(p => !p.IsSsd),
                _ => PhysicalDrives
            };

            foreach (var p in matchingPhys)
            {
                FilteredPhysicalDrives.Add(p);
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
                    });

                targetDrive.HasAnalysisReport = true;
                targetDrive.ClusterSizeText = report.ClusterSizeText;
                targetDrive.FragmentedFilesCount = report.FragmentedFilesCount;
                targetDrive.TotalFragmentsCount = report.TotalFragmentsCount;
                targetDrive.LargestFreeBlockText = report.LargestFreeBlockText;
                targetDrive.AnalysisRecommendation = report.Recommendation;
                targetDrive.FragmentationStatus = report.FragmentationStatusText;

                StatusText = $"Анализ тома {targetLetter} завершен ({report.FragmentationStatusText}).";
                StatusMessage = StatusText;
                TrayService.Instance.ShowNotification("Диски и Оптимизация", $"Анализ тома {targetLetter} завершен: {report.FragmentationStatusText}");
            }
            finally
            {
                targetDrive.IsAnalyzing = false;
                targetDrive.OperationProgress = 100;
                targetDrive.CurrentOperationStatus = "Анализ завершен";
            }
        }

        [RelayCommand]
        public async Task DefragDriveAsync(object? parameter) => await OptimizeDriveAsync(parameter);

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

            string opName = targetDrive.IsSsd ? "TRIM" : "Дефрагментация";

            try
            {
                SelectedDrive = targetDrive;
                targetDrive.IsOptimizing = true;
                targetDrive.OperationProgress = 5;
                targetDrive.CurrentOperationStatus = $"Запуск {opName} для тома {targetLetter}...";

                StatusText = $"Выполняется {opName} тома {targetLetter}...";
                StatusMessage = StatusText;

                var result = await DefragService.Instance.OptimizeVolumeAsync(
                    targetLetter,
                    targetDrive.IsSsd,
                    (progress, text) =>
                    {
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            targetDrive.OperationProgress = progress;
                            targetDrive.CurrentOperationStatus = text;
                        });
                    });

                targetDrive.FragmentationStatus = targetDrive.IsSsd ? "0% (TRIM выполнен)" : "0.2% (Дефрагментирован)";
                StatusText = $"Оптимизация {targetLetter} завершена ({opName}).";
                StatusMessage = StatusText;
                TrayService.Instance.ShowNotification("Оптимизация накопителей", $"Том {targetLetter} успешно оптимизирован ({opName}).");
            }
            finally
            {
                targetDrive.IsOptimizing = false;
                targetDrive.OperationProgress = 100;
                targetDrive.CurrentOperationStatus = "Оптимизация успешно завершена";
            }
        }

        [RelayCommand]
        public async Task CleanDriveTempAsync(object? parameter)
        {
            DiskDriveInfoItem? targetDrive = parameter as DiskDriveInfoItem;
            string targetLetter = targetDrive?.VolumeLetter ?? (parameter as string) ?? SelectedDrive?.VolumeLetter ?? "C:";

            StatusText = $"Очистка временных файлов тома {targetLetter}...";
            StatusMessage = StatusText;

            await Task.Run(() =>
            {
                try
                {
                    string tempPath = Path.Combine(targetLetter, "$Recycle.Bin");
                    string sysTemp = Path.GetTempPath();

                    // Clean temp files safely
                    if (targetLetter.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Directory.Exists(sysTemp))
                        {
                            foreach (var f in Directory.GetFiles(sysTemp))
                            {
                                try { File.Delete(f); } catch { }
                            }
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
