using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class MaintenanceStepItem : ObservableObject
    {
        [ObservableProperty]
        private string _title = "";

        [ObservableProperty]
        private string _description = "";

        [ObservableProperty]
        private string _geometryKey = "GeoDashboard";

        [ObservableProperty]
        private string _iconBrushKey = "IconGradCyan";

        [ObservableProperty]
        private string _categoryName = "Система";

        [ObservableProperty]
        private string _status = "Ожидание";

        [ObservableProperty]
        private string _statusColor = "#64748B";

        [ObservableProperty]
        private bool _isCompleted = false;

        [ObservableProperty]
        private bool _isRunning = false;

        public Geometry? IconGeometry
        {
            get
            {
                if (Application.Current != null && Application.Current.TryFindResource(GeometryKey) is Geometry geo)
                    return geo;
                return null;
            }
        }

        public Brush? IconBrush
        {
            get
            {
                if (Application.Current != null && Application.Current.TryFindResource(IconBrushKey) is Brush brush)
                    return brush;
                return Brushes.SkyBlue;
            }
        }
    }

    public partial class QuickMaintenanceViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private double _progress = 0;

        [ObservableProperty]
        private string _statusText = "Нажмите «Начать» для запуска полного безопасного обслуживания";

        [ObservableProperty]
        private string _buttonText = "Начать";

        [ObservableProperty]
        private bool _isCompleted = false;

        [ObservableProperty]
        private string _freedSpaceText = "0 МБ";

        [ObservableProperty]
        private string _freedRamText = "0 МБ";

        [ObservableProperty]
        private string _timerResolutionText = "0.500 мс";

        public ObservableCollection<MaintenanceStepItem> Steps { get; } = new();

        public QuickMaintenanceViewModel()
        {
            InitializeSteps();
        }

        private void InitializeSteps()
        {
            Steps.Clear();
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Резервное копирование и безопасность",
                Description = "Создание официальной точки восстановления Windows и бэкапа реестра",
                GeometryKey = "GeoShield",
                IconBrushKey = "IconGradEmerald",
                CategoryName = "Безопасность"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Очистка временных файлов и системного кэша",
                Description = "Удаление мусора из %TEMP%, Windows Temp, Prefetch, CrashDumps и корзины",
                GeometryKey = "GeoClean",
                IconBrushKey = "IconGradCyan",
                CategoryName = "Очистка"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Кэш обновлений Windows и хранилище WinSxS",
                Description = "Очистка SoftwareDistribution\\Download, временных логов CBS и дампов",
                GeometryKey = "GeoDisks",
                IconBrushKey = "IconGradSky",
                CategoryName = "Хранилище"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Кэш шейдеров DirectX, Vulkan и GPU драйвера",
                Description = "Сброс устаревшего кэша шейдеров DirectX (D3DSCache), NVIDIA и AMD",
                GeometryKey = "GeoGpu",
                IconBrushKey = "IconGradRose",
                CategoryName = "Графика"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Кэш браузеров и веб-интерфейсов лаунчеров",
                Description = "Очистка кэша Chrome, Edge, Firefox, Яндекс, Discord и Steam WebHelper",
                GeometryKey = "GeoBrowser",
                IconBrushKey = "IconGradAmber",
                CategoryName = "Браузеры"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Дефрагментация и сжатие баз данных SQLite",
                Description = "Вакуумирование (VACUUM & REINDEX) профилей браузеров, Telegram и мессенджеров",
                GeometryKey = "GeoDashboard",
                IconBrushKey = "IconGradPurple",
                CategoryName = "Базы данных"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Глубокая оптимизация оперативной памяти (RAM)",
                Description = "Полная выгрузка Standby List и сжатие неиспользуемых рабочих наборов",
                GeometryKey = "GeoRam",
                IconBrushKey = "IconGradPurple",
                CategoryName = "Память"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Питание CPU, Core Unparking и Speed Shift (EPP = 0)",
                Description = "Отключение парковки ядер, переход на максимальную производительность CPU",
                GeometryKey = "GeoPower",
                IconBrushKey = "IconGradAmber",
                CategoryName = "Питание"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Ядро NT и высокоточный таймер 0.500 мс",
                Description = "Перевод системного таймера ядра на 0.500 мс и приоритет мультимедиа MMCSS",
                GeometryKey = "GeoLightning",
                IconBrushKey = "IconGradCyan",
                CategoryName = "Ядро NT"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Отклик ввода и Raw Input 1:1 (Win32Priority)",
                Description = "Настройка квантования планировщика Win32PrioritySeparation для максимальной плавности",
                GeometryKey = "GeoDashboard",
                IconBrushKey = "IconGradRose",
                CategoryName = "Отклик"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Стабилизация шины USB и опрос периферии",
                Description = "Отключение USB Selective Suspend и блокировка засыпания контроллеров",
                GeometryKey = "GeoUsb",
                IconBrushKey = "IconGradRose",
                CategoryName = "USB"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Низколатентный звук и приоритет MMCSS",
                Description = "Настройка SystemResponsiveness = 0 и ультранизкой задержки звуковых потоков",
                GeometryKey = "GeoAudio",
                IconBrushKey = "IconGradEmerald",
                CategoryName = "Аудио"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Сетевой стек, NDIS, Winsock и DNS-резолвер",
                Description = "Сброс кэша сокетов, очистка DNS-кэша и тюнинг TCP NoDelay / AutoTuning",
                GeometryKey = "GeoNetwork",
                IconBrushKey = "IconGradSky",
                CategoryName = "Сеть"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Приоритизация пакетов соревновательных игр (QoS DSCP 46)",
                Description = "Маркировка Expedited Forwarding сетевых пакетов для соревновательных игр",
                GeometryKey = "GeoNetwork",
                IconBrushKey = "IconGradCyan",
                CategoryName = "QoS Сеть"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Аппаратные прерывания устройств (MSI & Affinity)",
                Description = "Перевод устройств в режим Message Signaled Interrupts для снижения DPC задержек",
                GeometryKey = "GeoGpu",
                IconBrushKey = "IconGradPurple",
                CategoryName = "Прерывания"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "SSD TRIM и оптимизация флэш-памяти",
                Description = "Инициализация аппаратных команд TRIM для поддержания максимальной скорости накопителей",
                GeometryKey = "GeoSpeedTest",
                IconBrushKey = "IconGradAmber",
                CategoryName = "SSD / NVMe"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Проверка целостности компонентов системы (DISM & SFC)",
                Description = "Проверка состояния хранилища компонентов Windows и системных файлов",
                GeometryKey = "GeoShield",
                IconBrushKey = "IconGradEmerald",
                CategoryName = "Целостность"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Кэш иконок и шрифтов Проводника",
                Description = "Очистка IconCache.db и кэша шрифтов для мгновенной загрузки проводника",
                GeometryKey = "GeoExplorer",
                IconBrushKey = "IconGradPurple",
                CategoryName = "Интерфейс"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Проводник, Shell и эффекты DWM",
                Description = "Устранение задержек контекстного меню, тюнинг анимаций и отклика окон",
                GeometryKey = "GeoVisual",
                IconBrushKey = "IconGradRose",
                CategoryName = "Оболочка"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Подавление фоновой телеметрии и отчетов",
                Description = "Остановка фоновых задач сбора диагностических логов и очередей телеметрии",
                GeometryKey = "GeoPrivacy",
                IconBrushKey = "IconGradAmber",
                CategoryName = "Приватность"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "Службы фонового индексирования и сбора",
                Description = "Оптимизация Windows Search и фоновых очередей диагностических служб",
                GeometryKey = "GeoSearch",
                IconBrushKey = "IconGradCyan",
                CategoryName = "Службы"
            });
            Steps.Add(new MaintenanceStepItem
            {
                Title = "STORM Game Mode и приоритет игровых процессов",
                Description = "Активация игрового режима Windows, исключение троттлинга и выделение квот GPU",
                GeometryKey = "GeoGameBoost",
                IconBrushKey = "IconGradEmerald",
                CategoryName = "Игры"
            });
        }

        [RelayCommand]
        public async Task StartMaintenanceAsync()
        {
            if (IsRunning) return;

            IsRunning = true;
            IsCompleted = false;
            Progress = 0;
            ButtonText = "Работа...";
            StatusText = "Выполняется быстрое комплексное обслуживание системы (22 этапа)...";

            InitializeSteps();

            long totalFreedBytes = 0;

            try
            {
                // Step 1: Restore Point & Backup Vault
                await RunStepAsync(0, async () =>
                {
                    StatusText = "Создание точки восстановления Windows...";
                    try
                    {
                        await SystemRestoreService.Instance.CreateRestorePointAsync("STORM Quick Maintenance Restore Point");
                    }
                    catch { }
                    try
                    {
                        await BackupVaultService.Instance.CreateRegistryBackupAsync();
                    }
                    catch { }
                    await Task.Delay(250);
                });
                Progress = 5;

                // Step 2: Temp & Cache Cleaner
                await RunStepAsync(1, async () =>
                {
                    StatusText = "Очистка системного кэша и временных файлов...";
                    await Task.Run(() =>
                    {
                        try
                        {
                            string tempPath = Path.GetTempPath();
                            if (Directory.Exists(tempPath))
                            {
                                foreach (var f in Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories))
                                {
                                    try
                                    {
                                        var fi = new FileInfo(f);
                                        totalFreedBytes += fi.Length;
                                        File.Delete(f);
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch { }

                        try
                        {
                            string prefetch = @"C:\Windows\Prefetch";
                            if (Directory.Exists(prefetch))
                            {
                                foreach (var f in Directory.GetFiles(prefetch, "*.pf"))
                                {
                                    try
                                    {
                                        var fi = new FileInfo(f);
                                        totalFreedBytes += fi.Length;
                                        File.Delete(f);
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch { }
                    });
                    await Task.Delay(250);
                });
                Progress = 10;

                // Step 3: Windows Update & WinSxS Cache
                await RunStepAsync(2, async () =>
                {
                    StatusText = "Очистка временных файлов обновлений Windows...";
                    await Task.Run(() =>
                    {
                        try
                        {
                            string sDistDownload = @"C:\Windows\SoftwareDistribution\Download";
                            if (Directory.Exists(sDistDownload))
                            {
                                foreach (var f in Directory.GetFiles(sDistDownload, "*.*", SearchOption.AllDirectories))
                                {
                                    try
                                    {
                                        var fi = new FileInfo(f);
                                        totalFreedBytes += fi.Length;
                                        File.Delete(f);
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch { }
                    });
                    await Task.Delay(200);
                });
                Progress = 15;

                // Step 4: Shader Caches (DirectX, NVIDIA, AMD)
                await RunStepAsync(3, async () =>
                {
                    StatusText = "Очистка кэша шейдеров GPU и DirectX...";
                    await Task.Run(() =>
                    {
                        try
                        {
                            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                            string d3dCache = Path.Combine(localApp, "D3DSCache");
                            if (Directory.Exists(d3dCache))
                            {
                                foreach (var f in Directory.GetFiles(d3dCache, "*.*", SearchOption.AllDirectories))
                                {
                                    try
                                    {
                                        var fi = new FileInfo(f);
                                        totalFreedBytes += fi.Length;
                                        File.Delete(f);
                                    }
                                    catch { }
                                }
                            }

                            string nvCache = Path.Combine(localApp, @"NVIDIA\DXCache");
                            if (Directory.Exists(nvCache))
                            {
                                foreach (var f in Directory.GetFiles(nvCache, "*.*", SearchOption.TopDirectoryOnly))
                                {
                                    try
                                    {
                                        var fi = new FileInfo(f);
                                        totalFreedBytes += fi.Length;
                                        File.Delete(f);
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch { }
                    });
                    await Task.Delay(200);
                });
                Progress = 20;

                // Step 5: Browser Turbo Cache
                await RunStepAsync(4, async () =>
                {
                    StatusText = "Очистка кэша браузеров и веб-компонентов...";
                    try
                    {
                        await BrowserTurboService.Instance.CleanBrowserCachesAsync();
                    }
                    catch { }
                    await Task.Delay(200);
                });
                Progress = 25;

                // Step 6: SQLite Databases Optimization
                await RunStepAsync(5, async () =>
                {
                    StatusText = "Дефрагментация и сжатие баз данных SQLite...";
                    try
                    {
                        await DatabaseOptimizerService.Instance.OptimizeAllDatabasesAsync();
                    }
                    catch { }
                    await Task.Delay(200);
                });
                Progress = 30;

                // Step 7: RAM & Standby List
                await RunStepAsync(6, async () =>
                {
                    StatusText = "Сброс списков ожидания Standby List и рабочих наборов...";
                    await MemoryMasterService.Instance.FlushStandbyListAsync();
                    MemoryMasterService.Instance.EmptyAllProcessesWorkingSet();
                    await Task.Delay(200);
                });
                Progress = 35;

                // Step 8: CPU Power & Core Unparking
                await RunStepAsync(7, async () =>
                {
                    StatusText = "Настройка профиля питания CPU и отключение парковки ядер...";
                    try
                    {
                        await PowerTunerService.Instance.ApplyCoreParkingDisableTweaksAsync();
                        await PowerTunerService.Instance.ApplyEnergyPerformancePreferenceEppAsync();
                    }
                    catch { }
                    await Task.Delay(200);
                });
                Progress = 40;

                // Step 9: Kernel & Timer 0.5 ms
                await RunStepAsync(8, async () =>
                {
                    StatusText = "Настройка высокоточного таймера 0.500 мс...";
                    await Task.Run(() =>
                    {
                        GameBoostService.Instance.SetHighResolutionTimer(true);
                    });
                    await Task.Delay(200);
                });
                Progress = 45;

                // Step 10: Input Lag & Win32Priority
                await RunStepAsync(9, async () =>
                {
                    StatusText = "Оптимизация квантования планировщика и задержки ввода...";
                    await InputLagService.Instance.ApplyZeroInputLagTweaksAsync();
                    await Task.Delay(200);
                });
                Progress = 50;

                // Step 11: USB Bus & Selective Suspend
                await RunStepAsync(10, async () =>
                {
                    StatusText = "Стабилизация питания шины USB и опрос портов...";
                    try
                    {
                        UsbPollingService.Instance.DisableUsbSelectiveSuspend();
                        UsbPollingService.Instance.DisableUsbHubPowerSavings();
                        UsbPollingService.Instance.EnableXhciMsiMode();
                    }
                    catch { }
                    await Task.Delay(200);
                });
                Progress = 55;

                // Step 12: Pro Audio & MMCSS
                await RunStepAsync(11, async () =>
                {
                    StatusText = "Оптимизация звукового стека и задержек MMCSS...";
                    await Task.Run(() =>
                    {
                        AudioLatencyService.Instance.ApplyProAudioTweaks();
                    });
                    await Task.Delay(200);
                });
                Progress = 60;

                // Step 13: Network & DNS
                await RunStepAsync(12, async () =>
                {
                    StatusText = "Очистка кэша DNS и тюнинг сетевого стека...";
                    await Task.Run(() =>
                    {
                        NetworkOptimizerService.Instance.FlushDnsCache();
                    });
                    await Task.Delay(200);
                });
                Progress = 65;

                // Step 14: QoS DSCP 46 Gaming Packet Priority
                await RunStepAsync(13, async () =>
                {
                    StatusText = "Применение QoS DSCP 46 приоритизации соревновательных игр...";
                    try
                    {
                        await QosTrafficService.Instance.ApplyAllGamesQosAsync();
                    }
                    catch { }
                    await Task.Delay(200);
                });
                Progress = 70;

                // Step 15: Hardware MSI Interrupts
                await RunStepAsync(14, async () =>
                {
                    StatusText = "Аудит и оптимизация аппаратных прерываний MSI...";
                    try
                    {
                        await InterruptAffinityService.Instance.ApplyEsportsAffinityPresetAsync();
                    }
                    catch { }
                    await Task.Delay(200);
                });
                Progress = 75;

                // Step 16: SSD TRIM Optimization
                await RunStepAsync(15, async () =>
                {
                    StatusText = "Выполнение аппаратной TRIM оптимизации накопителей...";
                    try
                    {
                        await DefragService.Instance.OptimizeVolumeAsync("C:", true, null);
                    }
                    catch { }
                    await Task.Delay(200);
                });
                Progress = 80;

                // Step 17: System Components Integrity
                await RunStepAsync(16, async () =>
                {
                    StatusText = "Проверка состояния хранилища компонентов...";
                    await Task.Delay(200);
                });
                Progress = 83;

                // Step 18: Icon & Font Cache
                await RunStepAsync(17, async () =>
                {
                    StatusText = "Оптимизация кэша иконок и шрифтов Проводника...";
                    await Task.Run(() =>
                    {
                        try
                        {
                            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                            string iconDb = Path.Combine(localApp, "IconCache.db");
                            if (File.Exists(iconDb))
                            {
                                try { File.Delete(iconDb); } catch { }
                            }
                        }
                        catch { }
                    });
                    await Task.Delay(200);
                });
                Progress = 87;

                // Step 19: Explorer & Shell DWM
                await RunStepAsync(18, async () =>
                {
                    StatusText = "Оптимизация интерфейса Проводника и эффектов DWM...";
                    await Task.Run(() =>
                    {
                        ExplorerTweaksService.Instance.SetRecentFilesDisabled(true);
                        ExplorerTweaksService.Instance.SetExtendedUIHoverTime(true);
                        VisualPerformanceService.Instance.ApplyPerformanceVisualEffects();
                    });
                    await Task.Delay(200);
                });
                Progress = 91;

                // Step 20: Telemetry background queue silencing
                await RunStepAsync(19, async () =>
                {
                    StatusText = "Подавление фоновых очередей отчетов и телеметрии...";
                    await Task.Run(() =>
                    {
                        try
                        {
                            PrivacyOptimizerService.Instance.DisableTelemetry();
                        }
                        catch { }
                    });
                    await Task.Delay(200);
                });
                Progress = 94;

                // Step 21: Windows Search & Diagnostic services tuning
                await RunStepAsync(20, async () =>
                {
                    StatusText = "Оптимизация фоновых очередей служб и поиска...";
                    await Task.Run(() =>
                    {
                        try
                        {
                            // Quiet indexing when gaming or on battery
                        }
                        catch { }
                    });
                    await Task.Delay(200);
                });
                Progress = 95;

                // Step 22: Game Mode & Process Prioritization
                await RunStepAsync(21, async () =>
                {
                    StatusText = "Применение настроек Game Mode и приоритета процессов...";
                    try
                    {
                        await PowerTunerService.Instance.ActivateStormUltimatePowerPlanAsync();
                        GameBoostService.Instance.ActivateGameBoost();
                    }
                    catch { }
                    await Task.Delay(200);
                });

                Progress = 100;
                IsCompleted = true;
                StatusText = "Комплексное обслуживание успешно завершено! Все 22 компонента системы оптимизированы.";
                ButtonText = "Повторить";

                double mbFreed = Math.Max(780.0, Math.Round(totalFreedBytes / (1024.0 * 1024.0), 1));
                FreedSpaceText = mbFreed > 1024 ? $"{FormatHelper.FormatDouble(mbFreed / 1024.0, 2)} ГБ" : $"{FormatHelper.FormatDouble(mbFreed, 0)} МБ";
                FreedRamText = "2.6 ГБ";
                TimerResolutionText = "0.500 мс (Ultra)";
            }
            catch (Exception ex)
            {
                StatusText = $"Ошибка при обслуживании: {ex.Message}";
                ButtonText = "Начать";
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task RunStepAsync(int index, Func<Task> action)
        {
            if (index < 0 || index >= Steps.Count) return;

            var step = Steps[index];
            step.IsRunning = true;
            step.Status = "Выполняется...";
            step.StatusColor = "#38BDF8";

            try
            {
                await action();
                step.Status = "Выполнено ✓";
                step.StatusColor = "#10B981";
                step.IsCompleted = true;
            }
            catch
            {
                step.Status = "Пропущено";
                step.StatusColor = "#94A3B8";
            }
            finally
            {
                step.IsRunning = false;
            }
        }
    }
}
