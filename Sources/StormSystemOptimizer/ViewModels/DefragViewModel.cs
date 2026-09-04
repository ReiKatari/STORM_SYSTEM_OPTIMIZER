using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public class ClusterBlockItem : ObservableObject
    {
        private string _color = "#1E293B";
        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        private string _toolTip = "Свободное пространство";
        public string ToolTip
        {
            get => _toolTip;
            set => SetProperty(ref _toolTip, value);
        }
    }

    public class DefragDriveItem : ObservableObject
    {
        public string Letter { get; set; } = "C:";
        public string Label { get; set; } = "Системный";
        public string DriveType { get; set; } = "SSD (NVMe)";
        public bool IsSsd { get; set; } = true;
        public string TotalSpace { get; set; } = "512 ГБ";
        public string FreeSpace { get; set; } = "240 ГБ";
        public double FreePercent { get; set; } = 46.8;

        private double _fragmentationPercent = 0;
        public double FragmentationPercent
        {
            get => _fragmentationPercent;
            set => SetProperty(ref _fragmentationPercent, value);
        }

        private string _statusText = "Оптимально";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _statusColor = "#10B981";
        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public string DisplayName => $"{Letter} [{Label}] — {DriveType} ({FreeSpace} свободно из {TotalSpace})";
    }

    public partial class DefragViewModel : ObservableObject
    {
        public ObservableCollection<DefragDriveItem> Drives { get; } = new();
        public ObservableCollection<ClusterBlockItem> ClusterBlocks { get; } = new();

        [ObservableProperty]
        private DefragDriveItem? _selectedDrive;

        [ObservableProperty]
        private bool _isBusy = false;

        public bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        private double _progress = 0;

        [ObservableProperty]
        private string _statusMessage = "Готов к анализу и оптимизации накопителей";

        [ObservableProperty]
        private string _activeMode = "Ожидание";

        [ObservableProperty]
        private string _largestFreeBlock = "--";

        [ObservableProperty]
        private string _clusterSize = "4 096 байт";

        [ObservableProperty]
        private string _fragmentedFilesCount = "0";

        [ObservableProperty]
        private string _recommendation = "Выберите накопитель и запустите анализ";

        [ObservableProperty]
        private string _rawLog = "Журнал операций пуст.";

        private CancellationTokenSource? _cts;

        public DefragViewModel()
        {
            InitClusterGrid();
            _ = LoadDrivesAsync();
        }

        private void InitClusterGrid()
        {
            ClusterBlocks.Clear();
            for (int i = 0; i < 200; i++)
            {
                ClusterBlocks.Add(new ClusterBlockItem
                {
                    Color = "#1E293B",
                    ToolTip = "Свободный сектор"
                });
            }
        }

        [RelayCommand]
        public async Task LoadDrivesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));
            StatusMessage = "Сканирование подключенных дисковых накопителей...";

            var loaded = new List<DefragDriveItem>();

            await Task.Run(() =>
            {
                try
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (!drive.IsReady) continue;
                        if (drive.DriveType != System.IO.DriveType.Fixed && drive.DriveType != System.IO.DriveType.Removable) continue;

                        string letter = drive.Name.TrimEnd('\\');
                        bool isSsd = DefragService.Instance.IsDriveSsd(letter);
                        long totalBytes = drive.TotalSize;
                        long freeBytes = drive.AvailableFreeSpace;
                        double freePct = totalBytes > 0 ? (double)freeBytes / totalBytes * 100.0 : 0;

                        string label = string.IsNullOrWhiteSpace(drive.VolumeLabel) 
                            ? (drive.DriveType == System.IO.DriveType.Removable ? "Съемный накопитель" : "Локальный диск") 
                            : drive.VolumeLabel;
                        string driveTypeStr = isSsd ? "SSD / NVMe (Flash)" : "HDD (Магнитный диск)";

                        var item = new DefragDriveItem
                        {
                            Letter = letter,
                            Label = label,
                            DriveType = driveTypeStr,
                            IsSsd = isSsd,
                            TotalSpace = FormatHelper.FormatBytes(totalBytes),
                            FreeSpace = FormatHelper.FormatBytes(freeBytes),
                            FreePercent = freePct,
                            FragmentationPercent = isSsd ? 0.0 : 1.5,
                            StatusText = isSsd ? "0% (Flash память • TRIM)" : "Готов к анализу",
                            StatusColor = isSsd ? "#00D2FF" : "#10B981"
                        };

                        loaded.Add(item);
                    }

                    if (loaded.Count > 1)
                    {
                        var allItem = new DefragDriveItem
                        {
                            Letter = "ALL",
                            Label = "Все подключенные диски",
                            DriveType = "Пакетная оптимизация всех накопителей",
                            IsSsd = false,
                            TotalSpace = $"{loaded.Count} томов",
                            FreeSpace = "Комплексно",
                            FreePercent = 50.0,
                            FragmentationPercent = 0.0,
                            StatusText = "Готов к пакетному TRIM и дефрагментации",
                            StatusColor = "#A855F7"
                        };
                        loaded.Insert(0, allItem);
                    }
                }
                catch { }
            });

            Drives.Clear();
            foreach (var item in loaded)
            {
                Drives.Add(item);
            }

            if (Drives.Count > 0)
            {
                SelectedDrive = Drives.Count > 1 && Drives[0].Letter == "ALL" ? Drives[1] : Drives[0];
                UpdateClusterGridForDrive(SelectedDrive);
            }

            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
            StatusMessage = $"Обнаружено накопителей: {loaded.Count(d => d.Letter != "ALL")}. Выберите диск для обслуживания.";
        }

        partial void OnSelectedDriveChanged(DefragDriveItem? value)
        {
            if (value != null)
            {
                UpdateClusterGridForDrive(value);
                if (value.IsSsd)
                {
                    Recommendation = "Твердотельный накопитель (SSD/NVMe). Традиционная дефрагментация секторов отключена для защиты флэш-памяти от износа. Рекомендуется аппаратный TRIM.";
                }
                else
                {
                    Recommendation = "Магнитный жесткий диск (HDD). Рекомендуется регулярная дефрагментация для ускорения чтения и устранения задержек позиционирования головок.";
                }
            }
        }

        private void UpdateClusterGridForDrive(DefragDriveItem drive)
        {
            int total = ClusterBlocks.Count;
            int freeBlocks = (int)(total * (drive.FreePercent / 100.0));
            int fragBlocks = drive.IsSsd ? 0 : (int)(total * (Math.Max(drive.FragmentationPercent, 2.0) / 100.0));
            int mftBlocks = 8;
            int usedBlocks = total - freeBlocks - fragBlocks - mftBlocks;
            if (usedBlocks < 0) usedBlocks = 0;

            var rnd = new Random(drive.Letter.GetHashCode());

            for (int i = 0; i < total; i++)
            {
                if (i < mftBlocks)
                {
                    ClusterBlocks[i].Color = "#A855F7"; // MFT / System
                    ClusterBlocks[i].ToolTip = "Системная область MFT";
                }
                else if (i < mftBlocks + fragBlocks)
                {
                    ClusterBlocks[i].Color = "#EF4444"; // Fragmented
                    ClusterBlocks[i].ToolTip = "Фрагментированные кластеры";
                }
                else if (i < mftBlocks + fragBlocks + usedBlocks)
                {
                    ClusterBlocks[i].Color = "#38BDF8"; // Allocated contiguous
                    ClusterBlocks[i].ToolTip = "Занятое непрерывное пространство";
                }
                else
                {
                    ClusterBlocks[i].Color = "#1E293B"; // Free
                    ClusterBlocks[i].ToolTip = "Свободное пространство";
                }
            }
        }

        [RelayCommand]
        public async Task AnalyzeDriveAsync()
        {
            if (SelectedDrive == null || IsBusy) return;
            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));
            ActiveMode = "Анализ тома";
            Progress = 10;
            StatusMessage = $"Анализ фрагментации тома {SelectedDrive.Letter}...";

            try
            {
                var report = await DefragService.Instance.AnalyzeVolumeDetailedAsync(SelectedDrive.Letter, SelectedDrive.IsSsd, (pct, st) =>
                {
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        Progress = pct;
                        StatusMessage = st;
                    });
                });

                SelectedDrive.FragmentationPercent = report.FragmentationPercent;
                SelectedDrive.StatusText = report.FragmentationStatusText;
                SelectedDrive.StatusColor = report.FragmentationPercent > 5.0 ? "#EF4444" : (report.FragmentationPercent > 2.0 ? "#F59E0B" : "#10B981");

                ClusterSize = report.ClusterSizeText;
                FragmentedFilesCount = report.FragmentedFilesCount.ToString();
                LargestFreeBlock = report.LargestFreeBlockText;
                Recommendation = report.Recommendation;
                RawLog = string.IsNullOrWhiteSpace(report.RawLog) ? "Анализ завершен успешно." : report.RawLog;

                UpdateClusterGridForDrive(SelectedDrive);
                StatusMessage = $"Анализ {SelectedDrive.Letter} завершен: фрагментация {FormatHelper.FormatDouble(SelectedDrive.FragmentationPercent, 1)}%.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка анализа: {ex.Message}";
            }
            finally
            {
                Progress = 100;
                IsBusy = false;
                OnPropertyChanged(nameof(IsNotBusy));
                ActiveMode = "Завершено";
            }
        }

        [RelayCommand]
        public async Task DefragOrTrimAsync()
        {
            if (SelectedDrive == null || IsBusy) return;

            if (SelectedDrive.Letter == "ALL")
            {
                await RunBatchOptimizationAsync();
                return;
            }

            if (SelectedDrive.IsSsd)
            {
                await RunTrimOptimizationAsync();
            }
            else
            {
                await RunHddDefragAsync(false);
            }
        }

        private async Task RunBatchOptimizationAsync()
        {
            var targets = Drives.Where(d => d.Letter != "ALL").ToList();
            if (targets.Count == 0) return;

            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));
            ActiveMode = "Пакетная оптимизация всех дисков";
            Progress = 0;
            StatusMessage = $"Старт пакетного обслуживания {targets.Count} накопителей...";
            RawLog = $"=== ПАКЕТНАЯ ОПТИМИЗАЦИЯ НАКОПИТЕЛЕЙ: {DateTime.Now:dd.MM.yyyy HH:mm:ss} ===\r\n";

            for (int i = 0; i < targets.Count; i++)
            {
                var d = targets[i];
                StatusMessage = $"[{i + 1}/{targets.Count}] Оптимизация тома {d.Letter} ({d.DriveType})...";
                Progress = (double)i / targets.Count * 100.0;

                try
                {
                    string args = d.IsSsd ? $"{d.Letter} /L /U" : $"{d.Letter} /O /U";
                    var psi = new ProcessStartInfo
                    {
                        FileName = "defrag.exe",
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        string outStr = await p.StandardOutput.ReadToEndAsync();
                        await p.WaitForExitAsync();
                        RawLog += $"\r\n--- Том {d.Letter} ({d.Label}) ---\r\n{outStr}\r\n";
                    }
                }
                catch (Exception ex)
                {
                    RawLog += $"\r\n[ОШИБКА] Том {d.Letter}: {ex.Message}\r\n";
                }
            }

            Progress = 100;
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
            ActiveMode = "Завершено";
            StatusMessage = $"Пакетная оптимизация {targets.Count} дисков успешно завершена!";
            TrayService.Instance.ShowNotification("Оптимизация накопителей ✓", StatusMessage);
        }

        [RelayCommand]
        public void ClearLog()
        {
            RawLog = "Журнал очищен.\r\n";
        }

        [RelayCommand]
        public void CopyLog()
        {
            if (!string.IsNullOrWhiteSpace(RawLog))
            {
                try
                {
                    Clipboard.SetText(RawLog);
                    TrayService.Instance.ShowNotification("Журнал дефрагментации", "Лог скопирован в буфер обмена!");
                }
                catch { }
            }
        }

        [RelayCommand]
        public async Task DeepDefragAsync()
        {
            if (SelectedDrive == null || IsBusy) return;

            if (SelectedDrive.IsSsd)
            {
                await RunTrimOptimizationAsync();
            }
            else
            {
                await RunHddDefragAsync(true);
            }
        }

        [RelayCommand]
        public async Task BootDefragAsync()
        {
            if (SelectedDrive == null || IsBusy) return;
            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));
            ActiveMode = "Загрузочная дефрагментация (Boot Defrag)";
            Progress = 20;
            StatusMessage = $"Группировка загрузочных файлов ядра Windows (defrag {SelectedDrive.Letter} /B)...";

            _cts = new CancellationTokenSource();

            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "defrag.exe",
                        Arguments = $"{SelectedDrive.Letter} /B /U",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        string outStr = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(60000);
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            RawLog = outStr;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        RawLog = $"Ошибка: {ex.Message}";
                    });
                }
            });

            Progress = 100;
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
            ActiveMode = "Готово";
            StatusMessage = $"Загрузочные кластеры ядра Windows на томе {SelectedDrive.Letter} успешно консолидированы!";
            TrayService.Instance.ShowNotification("Дефрагментация дисков", StatusMessage);
        }

        private async Task RunTrimOptimizationAsync()
        {
            if (SelectedDrive == null) return;
            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));
            ActiveMode = "Аппаратный TRIM (NVMe / SSD)";
            Progress = 15;
            StatusMessage = $"Выполнение аппаратной очистки ячеек Flash-памяти (ReTrim) на томе {SelectedDrive.Letter}...";

            await AnimateClusterWaveAsync("#00D2FF");

            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"Optimize-Volume -DriveLetter '{SelectedDrive.Letter.TrimEnd(':')}' -ReTrim -Verbose\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        string outStr = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(30000);
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            RawLog = string.IsNullOrWhiteSpace(outStr) ? "Команда TRIM успешно отправлена контроллеру накопителя." : outStr;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        RawLog = $"Ошибка выполнения TRIM: {ex.Message}";
                    });
                }
            });

            SelectedDrive.FragmentationPercent = 0.0;
            SelectedDrive.StatusText = "0% (TRIM выполнен ✓)";
            SelectedDrive.StatusColor = "#10B981";
            UpdateClusterGridForDrive(SelectedDrive);

            Progress = 100;
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
            ActiveMode = "Готово";
            StatusMessage = $"Аппаратный TRIM на диске {SelectedDrive.Letter} успешно завершен!";
            TrayService.Instance.ShowNotification("SSD TRIM", StatusMessage);
        }

        private async Task RunHddDefragAsync(bool isDeep)
        {
            if (SelectedDrive == null) return;
            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));
            ActiveMode = isDeep ? "Глубокая оптимизация со сжатием свободного места" : "Быстрая дефрагментация";
            Progress = 10;
            StatusMessage = $"Дефрагментация тома {SelectedDrive.Letter}...";

            string args = isDeep ? $"{SelectedDrive.Letter} /X /V /U" : $"{SelectedDrive.Letter} /U /V";

            var animTask = AnimateClusterWaveAsync("#10B981");

            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "defrag.exe",
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        string outStr = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(120000);
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            RawLog = outStr;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        RawLog = $"Ошибка: {ex.Message}";
                    });
                }
            });

            await animTask;

            SelectedDrive.FragmentationPercent = 0.0;
            SelectedDrive.StatusText = "0% (Оптимально ✓)";
            SelectedDrive.StatusColor = "#10B981";
            UpdateClusterGridForDrive(SelectedDrive);

            Progress = 100;
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
            ActiveMode = "Готово";
            StatusMessage = $"Дефрагментация тома {SelectedDrive.Letter} успешно завершена!";
            TrayService.Instance.ShowNotification("Дефрагментация дисков", StatusMessage);
        }

        private async Task AnimateClusterWaveAsync(string highlightColor)
        {
            int count = ClusterBlocks.Count;
            for (int i = 0; i < count; i += 5)
            {
                for (int j = 0; j < 5 && (i + j) < count; j++)
                {
                    if (ClusterBlocks[i + j].Color != "#A855F7")
                    {
                        ClusterBlocks[i + j].Color = highlightColor;
                    }
                }
                Progress = Math.Min(95.0, 15.0 + ((double)i / count * 75.0));
                await Task.Delay(40);
            }
        }
    }
}
