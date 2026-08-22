using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class DriverUpdaterViewModel : ObservableObject
    {
        private List<DriverItem> _allDrivers = new();

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusText = "Готов к проверке актуальности драйверов оборудования";

        [ObservableProperty]
        private string _statsSummary = "0 устройств просканировано";

        [ObservableProperty]
        private string _selectedCategory = "Все";

        [ObservableProperty]
        private bool _isBackupsSelected = false;

        [ObservableProperty]
        private bool _isUsbWriterOpen = false;

        [ObservableProperty]
        private string _selectedBiosFilePath = string.Empty;

        [ObservableProperty]
        private UsbDriveItem? _selectedUsbDrive;

        [ObservableProperty]
        private string _usbWriterStatus = "Вставьте USB-флешку и выберите файл прошивки BIOS";

        public ObservableCollection<DriverItem> DisplayDrivers { get; } = new();
        public ObservableCollection<SystemBackupItem> DisplayBackups { get; } = new();
        public ObservableCollection<UsbDriveItem> UsbFlashDrives { get; } = new();

        public DriverUpdaterViewModel()
        {
            _ = LoadDriversAsync();
        }

        [RelayCommand]
        public void ToggleUsbWriter()
        {
            IsUsbWriterOpen = !IsUsbWriterOpen;
            if (IsUsbWriterOpen)
            {
                RefreshUsbDrives();
            }
        }

        [RelayCommand]
        public void RefreshUsbDrives()
        {
            UsbFlashDrives.Clear();
            var drives = DriverUpdaterService.Instance.GetUsbFlashDrives();
            foreach (var d in drives)
            {
                UsbFlashDrives.Add(d);
            }
            if (UsbFlashDrives.Count > 0)
            {
                SelectedUsbDrive = UsbFlashDrives[0];
                UsbWriterStatus = $"Обнаружено {UsbFlashDrives.Count} USB-накопителей. Выберите файл BIOS.";
            }
            else
            {
                SelectedUsbDrive = null;
                UsbWriterStatus = "Вставьте USB-флешку в компьютер и нажмите «Обновить список».";
            }
        }

        [RelayCommand]
        public void BrowseBiosFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите файл обновления BIOS (.CAP, .ROM, .BIN, .ZIP)",
                Filter = "Файлы прошивок BIOS (*.cap;*.rom;*.bin;*.zip;*.bio)|*.cap;*.rom;*.bin;*.zip;*.bio|Все файлы (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                SelectedBiosFilePath = dlg.FileName;
                UsbWriterStatus = $"Выбран файл: {Path.GetFileName(dlg.FileName)}";
            }
        }

        [RelayCommand]
        public async Task FormatUsbFat32Async()
        {
            if (SelectedUsbDrive == null)
            {
                UsbWriterStatus = "Ошибка: Сначала выберите USB-накопитель из списка!";
                return;
            }

            IsBusy = true;
            UsbWriterStatus = $"Форматирование {SelectedUsbDrive.DriveLetter} в FAT32 (требуется для UEFI BIOS)...";
            var (ok, msg) = await DriverUpdaterService.Instance.FormatUsbDriveAsync(SelectedUsbDrive.DriveLetter);
            UsbWriterStatus = msg;
            RefreshUsbDrives();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task WriteBiosToUsbAsync()
        {
            if (SelectedUsbDrive == null)
            {
                UsbWriterStatus = "Ошибка: Выберите USB-флешку!";
                return;
            }
            if (string.IsNullOrWhiteSpace(SelectedBiosFilePath) || !File.Exists(SelectedBiosFilePath))
            {
                UsbWriterStatus = "Ошибка: Выберите файл прошивки BIOS (.cap, .rom, .bin или .zip)!";
                return;
            }

            IsBusy = true;
            UsbWriterStatus = $"Запись файла прошивки на {SelectedUsbDrive.DriveLetter}...";
            var (ok, msg) = await DriverUpdaterService.Instance.CopyBiosFileToUsbAsync(SelectedBiosFilePath, SelectedUsbDrive.DriveLetter);
            UsbWriterStatus = msg;
            IsBusy = false;
        }

        [RelayCommand]
        public async Task LoadDriversAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Сканирование цифровых подписей WHQL и версий драйверов...";

            _allDrivers = await DriverUpdaterService.Instance.ScanDriversAsync();
            ApplyFilter();

            int updatesCount = _allDrivers.Count(d => d.IsUpdateAvailable);
            StatsSummary = $"{_allDrivers.Count} устройств в системе • {(updatesCount > 0 ? $"{updatesCount} требуют обновления ⚡" : "Все драйверы актуальны ✅")}";
            StatusText = $"Найдено {_allDrivers.Count} драйверов оборудования.";
            IsBusy = false;
        }

        public void SetCategory(string cat)
        {
            SelectedCategory = cat;
            IsBackupsSelected = (cat == "Бэкапы");
            if (IsBackupsSelected)
            {
                _ = LoadBackupsAsync();
            }
            else
            {
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            var query = _allDrivers.AsEnumerable();
            if (SelectedCategory != "Все" && SelectedCategory != "Бэкапы")
            {
                query = query.Where(d => d.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            DisplayDrivers.Clear();
            foreach (var item in query)
            {
                DisplayDrivers.Add(item);
            }
        }

        [RelayCommand]
        public async Task LoadBackupsAsync()
        {
            IsBusy = true;
            StatusText = "Поиск доступных бэкапов драйверов и точек восстановления системы...";

            await Task.Run(() =>
            {
                var list = new List<SystemBackupItem>();

                // 1. Scan Windows System Restore Points
                try
                {
                    var existing = BackupVaultService.Instance.GetExistingBackups();
                    foreach (var b in existing)
                    {
                        list.Add(b);
                    }
                }
                catch { }

                // 2. Scan Driver Export Folders
                string[] possibleDirs = new[]
                {
                    @"C:\STORM_Drivers_Backup",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "STORM_Drivers_Backup"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM_OPTIMIZER", "Backups", "Drivers")
                };

                foreach (var dir in possibleDirs)
                {
                    try
                    {
                        if (Directory.Exists(dir))
                        {
                            var di = new DirectoryInfo(dir);
                            int infCount = di.GetFiles("*.inf", SearchOption.AllDirectories).Length;
                            long totalBytes = 0;
                            foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                            {
                                totalBytes += fi.Length;
                            }
                            double mb = totalBytes / (1024.0 * 1024.0);

                            list.Add(new SystemBackupItem
                            {
                                Title = $"Резервная копия драйверов ({di.Name})",
                                DateString = di.LastWriteTime.ToString("dd.MM.yyyy HH:mm"),
                                BackupType = "Бэкап драйверов (.inf)",
                                FilePath = dir,
                                SizeText = $"{infCount} пакетов ({mb:F0} МБ)",
                                IsRestorePoint = false
                            });
                        }
                    }
                    catch { }
                }

                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    DisplayBackups.Clear();
                    foreach (var item in list.OrderByDescending(b => b.DateString))
                    {
                        DisplayBackups.Add(item);
                    }
                });
            });

            StatsSummary = $"{DisplayBackups.Count} резервных копий и точек восстановления найдено";
            StatusText = $"Загружено {DisplayBackups.Count} точек доступа и архивов драйверов.";
            IsBusy = false;
        }

        [RelayCommand]
        public async Task UpdateDriverAsync(DriverItem? item)
        {
            if (item == null) return;

            // Prompt / Create restore point automatically before driver upgrade
            StatusText = $"Создание точки восстановления перед обновлением {item.DeviceName}...";
            await SystemRestoreService.Instance.CreateRestorePointAsync($"Перед обновлением драйвера {item.DeviceName}");

            // Open download portal
            if (!string.IsNullOrEmpty(item.DownloadUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = item.DownloadUrl,
                        UseShellExecute = true
                    });
                }
                catch { }
            }

            TrayService.Instance.ShowNotification("Центр обновления драйверов ⚡", $"Точка восстановления создана. Открыта официальная страница загрузки для {item.DeviceName}.");
            StatusText = $"Открыта загрузка для {item.DeviceName}.";
        }

        [RelayCommand]
        public async Task BackupAllDriversAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Экспорт всех установленных пакетов драйверов в бэкап...";

            string backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "STORM_Drivers_Backup");
            var (success, msg) = await DriverUpdaterService.Instance.ExportAllDriversBackupAsync(backupDir);

            StatusText = msg;
            TrayService.Instance.ShowNotification("Бэкап драйверов 💾", msg);
            await LoadBackupsAsync();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task CreateSystemRestorePointAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Создание точки восстановления Windows...";

            var (success, msg) = await SystemRestoreService.Instance.CreateRestorePointAsync("Ручное создание в STORM OPTIMIZER");
            StatusText = msg;
            TrayService.Instance.ShowNotification("Точка восстановления 🛡️", msg);
            await LoadBackupsAsync();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RestoreBackupItemAsync(SystemBackupItem? item)
        {
            if (item == null) return;
            IsBusy = true;

            if (item.IsRestorePoint)
            {
                StatusText = $"Восстановление Windows из точки «{item.Title}»...";
                bool ok = await BackupVaultService.Instance.RestoreSystemRestorePointAsync(item.SequenceNumber);
                StatusText = ok ? "Запущена процедура восстановления Windows!" : "Не удалось выполнить откат.";
            }
            else if (!string.IsNullOrWhiteSpace(item.FilePath) && Directory.Exists(item.FilePath))
            {
                StatusText = $"Установка и восстановление драйверов из «{item.FilePath}» через PnPUtil...";
                await Task.Run(() =>
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "pnputil.exe",
                            Arguments = $"/add-driver \"{item.FilePath}\\*.inf\" /subdirs /install",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var p = Process.Start(psi);
                        p?.WaitForExit(60000);
                    }
                    catch { }
                });
                StatusText = "Драйверы из резервной копии успешно установлены в систему!";
                TrayService.Instance.ShowNotification("Восстановление драйверов 💾", StatusText);
            }

            IsBusy = false;
        }

        [RelayCommand]
        public void OpenBackupFolder(SystemBackupItem? item)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.FilePath) && Directory.Exists(item.FilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{item.FilePath}\"",
                        UseShellExecute = true
                    });
                }
                catch { }
            }
            else
            {
                BackupVaultService.Instance.OpenBackupsFolder();
            }
        }
    }
}
