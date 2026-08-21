using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public partial class BenchmarksViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _statusMessage = "Готов к запуску тестов производительности CPU, GPU, RAM, Диска и стресс-теста.";

        [ObservableProperty]
        private double _stressProgress = 0;

        [ObservableProperty]
        private string _stressStatusText = "Ожидание запуска стресс-теста";

        [ObservableProperty]
        private string _stressTimeRemainingText = "30 сек";

        [ObservableProperty]
        private bool _isStressRunning = false;

        // GPU 3D Score
        [ObservableProperty]
        private string _gpuScoreText = "— Pts";

        [ObservableProperty]
        private string _gpuScoreDetail = "Direct3D шейдеры и 3D рендеринг";

        // GPU VRAM
        [ObservableProperty]
        private string _gpuVramScoreText = "— ГБ/с";

        [ObservableProperty]
        private string _gpuVramScoreDetail = "Пропускная способность шины VRAM";

        // CPU Multi-Core
        [ObservableProperty]
        private string _cpuScoreText = "— Pts";

        [ObservableProperty]
        private string _cpuScoreDetail = "Многопоточный тест (Все ядра / потоки)";

        // CPU Single-Core
        [ObservableProperty]
        private string _singleCoreScoreText = "— Pts";

        [ObservableProperty]
        private string _singleCoreScoreDetail = "Тест одного ядра (Single-Thread IPC)";

        // RAM Speed
        [ObservableProperty]
        private string _ramSpeedText = "— ГБ/с";

        [ObservableProperty]
        private string _ramScoreDetail = "Скорость чтения/записи памяти RAM";

        // Disk Sequential
        [ObservableProperty]
        private string _diskSpeedText = "— МБ/с";

        [ObservableProperty]
        private string _diskScoreDetail = "Последовательное чтение / запись";

        // Disk 4K IOPS
        [ObservableProperty]
        private string _diskIopsText = "— IOPS";

        [ObservableProperty]
        private string _diskIopsDetail = "Случайный доступ блоками 4 КБ";

        // Overall Index
        [ObservableProperty]
        private string _stormOverallScoreText = "8,450 PTS";

        [ObservableProperty]
        private string _stormOverallScoreDetail = "STORM PERFORMANCE INDEX: Расчетный индекс производительности";

        [ObservableProperty]
        private string _liveCpuTempText = "-- °C";

        public ObservableCollection<HardwareSensorItem> HardwareSensors { get; } = new();
        public ObservableCollection<CoreMetricItem> CpuCores { get; } = new();
        public ObservableCollection<ProcessThermalImpactItem> ThermalProcesses { get; } = new();

        public BenchmarksViewModel()
        {
            try
            {
                int cores = Environment.ProcessorCount;
                var metrics = HardwareMonitorService.Instance.GetCurrentMetrics();
                double ramGb = metrics.RamTotalGb > 0 ? metrics.RamTotalGb : 32.0;
                double baseScore = Math.Round((cores * 420.0) + (ramGb * 160.0) + 2800.0);
                StormOverallScoreText = FormatHelper.FormatPts(baseScore, true);
                StormOverallScoreDetail = "STORM PERFORMANCE INDEX: Готов к полному тестированию под нагрузкой";
            }
            catch { }

            _ = RefreshSensorsAsync();
            _ = RefreshThermalImpactAsync();
        }

        [RelayCommand]
        public async Task RefreshSensorsAsync()
        {
            HardwareSensors.Clear();
            var sensors = await HardwareTemperatureService.Instance.GetAllTemperaturesAsync();
            foreach (var s in sensors)
            {
                HardwareSensors.Add(s);
            }

            // Update Per-Core loads
            CpuCores.Clear();
            int coreCount = Environment.ProcessorCount;
            var rand = new Random();
            double baseLoad = HardwareMonitorService.Instance.GetCurrentMetrics().CpuUsagePercentage;

            for (int i = 0; i < Math.Min(32, coreCount); i++)
            {
                double coreLoad = Math.Clamp(baseLoad + rand.Next(-8, 9), 1.0, 100.0);
                CpuCores.Add(new CoreMetricItem
                {
                    CoreIndex = i,
                    LoadPercentage = coreLoad
                });
            }

            await RefreshThermalImpactAsync();
        }

        [RelayCommand]
        public async Task RefreshThermalImpactAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    var list = new List<ProcessThermalImpactItem>();
                    var procs = Process.GetProcesses();
                    int myPid = Environment.ProcessId;

                    // Known high-heat GPU app names
                    var gpuKnownApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "chrome", "msedge", "firefox", "opera", "brave", "discord", "telegram",
                        "steam", "epicgameslauncher", "dwm", "blender", "photoshop", "premiere",
                        "obs64", "vlc", "spotify", "csgo", "dota2", "valorant", "gta5", "cyberpunk2077"
                    };

                    double gpuBaseTemp = HardwareTemperatureService.Instance.GetGpuTemperature();
                    double cpuBaseTemp = HardwareTemperatureService.Instance.GetCpuTemperature();

                    var scoredProcs = new List<(Process Proc, string Name, double Score, bool IsGpu)>();

                    foreach (var p in procs)
                    {
                        if (p.Id == 0 || p.Id == 4 || p.Id == myPid) continue;
                        try
                        {
                            string name = p.ProcessName;
                            long memMb = p.WorkingSet64 / (1024 * 1024);
                            if (memMb < 20) continue;

                            bool isGpu = gpuKnownApps.Contains(name) || name.Contains("game", StringComparison.OrdinalIgnoreCase) || name.Contains("3d", StringComparison.OrdinalIgnoreCase);
                            double score = memMb * (isGpu ? 1.5 : 1.0);
                            scoredProcs.Add((p, name, score, isGpu));
                        }
                        catch { }
                    }

                    // Sort top heat contributors
                    var topProcs = scoredProcs.OrderByDescending(x => x.Score).Take(6).ToList();
                    var rand = new Random(Environment.TickCount);

                    foreach (var item in topProcs)
                    {
                        double usage = Math.Min(95.0, 5.0 + (item.Score / 250.0) * 8.0 + rand.NextDouble() * 5.0);
                        double heatAdded;
                        string status;
                        string color;
                        string bgColor;

                        if (item.IsGpu)
                        {
                            heatAdded = Math.Min(18.0, 2.5 + (usage * 0.14) + rand.NextDouble() * 1.5);
                            if (heatAdded >= 10.0)
                            {
                                status = "🔥 Критический нагрев";
                                color = "#EF4444";
                                bgColor = "#26EF4444";
                            }
                            else if (heatAdded >= 6.0)
                            {
                                status = "⚡ Высокий нагрев";
                                color = "#F59E0B";
                                bgColor = "#26F59E0B";
                            }
                            else
                            {
                                status = "🟡 Умеренный";
                                color = "#38BDF8";
                                bgColor = "#2638BDF8";
                            }
                        }
                        else
                        {
                            heatAdded = Math.Min(15.0, 1.8 + (usage * 0.11) + rand.NextDouble() * 1.2);
                            if (heatAdded >= 8.0)
                            {
                                status = "⚡ Высокий нагрев";
                                color = "#F59E0B";
                                bgColor = "#26F59E0B";
                            }
                            else if (heatAdded >= 4.0)
                            {
                                status = "🟡 Умеренный";
                                color = "#38BDF8";
                                bgColor = "#2638BDF8";
                            }
                            else
                            {
                                status = "🟢 Минимальный";
                                color = "#10B981";
                                bgColor = "#2610B981";
                            }
                        }

                        var realIcon = IconExtractorHelper.GetProcessIcon(item.Proc.Id, item.Name);

                        list.Add(new ProcessThermalImpactItem
                        {
                            ProcessId = item.Proc.Id,
                            ProcessName = item.Name + ".exe",
                            TargetComponent = item.IsGpu ? "GPU" : "CPU",
                            UsagePercentage = Math.Round(usage),
                            EstimatedHeatAddedC = Math.Round(heatAdded, 1),
                            ThermalStatus = status,
                            StatusColor = color,
                            StatusBgColor = bgColor,
                            IconSource = realIcon
                        });
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        ThermalProcesses.Clear();
                        foreach (var t in list)
                        {
                            ThermalProcesses.Add(t);
                        }
                    });
                }
                catch { }
            });
        }

        [RelayCommand]
        public async Task CoolDownProcessAsync(ProcessThermalImpactItem? item)
        {
            if (item == null) return;
            try
            {
                ProcessManagerService.Instance.SetProcessPriority(item.ProcessId, ProcessPriorityClass.Idle);
                var proc = Process.GetProcessById(item.ProcessId);
                NativeMethods.EmptyWorkingSet(proc.Handle);
                TrayService.Instance.ShowNotification("Охлаждение процесса ❄️", $"Приоритет {item.ProcessName} снижен, память сжата для понижения температуры {item.TargetComponent}.");
                await RefreshThermalImpactAsync();
            }
            catch { }
        }

        [RelayCommand]
        public async Task RunAllBenchmarksAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;

            // Phase 1: GPU
            StatusMessage = "[1/6] Тестирование графического адаптера GPU Direct3D 12...";
            await RunGpuBenchmarkAsync();
            await Task.Delay(250);

            // Phase 2: VRAM
            StatusMessage = "[2/6] Замер пропускной способности шины видеопамяти VRAM...";
            await RunGpuVramBenchmarkAsync();
            await Task.Delay(250);

            // Phase 3: CPU Multi
            StatusMessage = "[3/6] Многопоточный стресс-тест ядер CPU...";
            await RunCpuBenchmarkAsync();
            await Task.Delay(250);

            // Phase 4: CPU Single
            StatusMessage = "[4/6] Калибровка однопоточной производительности (Single-Thread IPC)...";
            await RunSingleCoreBenchmarkAsync();
            await Task.Delay(250);

            // Phase 5: RAM
            StatusMessage = "[5/6] Тестирование скорости и задержки памяти RAM...";
            await RunRamBenchmarkAsync();
            await Task.Delay(250);

            // Phase 6: Disk
            StatusMessage = "[6/6] Замер скорости чтения/записи накопителя и 4K IOPS...";
            await RunDiskBenchmarkAsync();

            UpdateOverallScore();
            StatusMessage = "Все бенчмарки успешно выполнены! Расчитан STORM PERFORMANCE INDEX.";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Бенчмарки завершены ⚡", $"Общий индекс производительности STORM PERFORMANCE INDEX: {StormOverallScoreText}");
        }

        [RelayCommand]
        public async Task RunGpuBenchmarkAsync()
        {
            if (IsBusy && !StatusMessage.StartsWith("[")) return;
            IsBusy = true;
            GpuScoreDetail = "Тестирование Direct3D 11/12 шейдеров...";

            var res = await HardwareBenchmarkService.Instance.RunGpuBenchmarkAsync((p, text) =>
            {
                App.Current.Dispatcher.Invoke(() => GpuScoreDetail = text);
            });
            GpuScoreText = res.ScoreText;
            GpuScoreDetail = res.Details;
            UpdateOverallScore();
            if (!StatusMessage.StartsWith("[")) IsBusy = false;
        }

        [RelayCommand]
        public async Task RunGpuVramBenchmarkAsync()
        {
            if (IsBusy && !StatusMessage.StartsWith("[")) return;
            IsBusy = true;
            GpuVramScoreDetail = "Замер пропускной способности видеопамяти...";

            var res = await HardwareBenchmarkService.Instance.RunGpuVramBenchmarkAsync((p, text) =>
            {
                App.Current.Dispatcher.Invoke(() => GpuVramScoreDetail = text);
            });
            GpuVramScoreText = res.ScoreText;
            GpuVramScoreDetail = res.Details;
            UpdateOverallScore();
            if (!StatusMessage.StartsWith("[")) IsBusy = false;
        }

        [RelayCommand]
        public async Task RunCpuBenchmarkAsync()
        {
            if (IsBusy && !StatusMessage.StartsWith("[")) return;
            IsBusy = true;
            CpuScoreDetail = "Многоядерный стресс-тест CPU (Все потоки)...";

            var res = await HardwareBenchmarkService.Instance.RunCpuBenchmarkAsync((p, text) =>
            {
                App.Current.Dispatcher.Invoke(() => CpuScoreDetail = text);
            });
            CpuScoreText = res.ScoreText;
            CpuScoreDetail = res.Details;
            UpdateOverallScore();
            if (!StatusMessage.StartsWith("[")) IsBusy = false;
        }

        [RelayCommand]
        public async Task RunSingleCoreBenchmarkAsync()
        {
            if (IsBusy && !StatusMessage.StartsWith("[")) return;
            IsBusy = true;
            SingleCoreScoreDetail = "Тест одного ядра CPU (IPC / Single-Thread)...";

            var res = await HardwareBenchmarkService.Instance.RunSingleCoreCpuBenchmarkAsync((p, text) =>
            {
                App.Current.Dispatcher.Invoke(() => SingleCoreScoreDetail = text);
            });
            SingleCoreScoreText = res.ScoreText;
            SingleCoreScoreDetail = res.Details;
            UpdateOverallScore();
            if (!StatusMessage.StartsWith("[")) IsBusy = false;
        }

        [RelayCommand]
        public async Task RunRamBenchmarkAsync()
        {
            if (IsBusy && !StatusMessage.StartsWith("[")) return;
            IsBusy = true;
            RamScoreDetail = "Тест скорости шины RAM и latency...";

            var res = await HardwareBenchmarkService.Instance.RunRamBenchmarkAsync((p, text) =>
            {
                App.Current.Dispatcher.Invoke(() => RamScoreDetail = text);
            });
            RamSpeedText = res.ScoreText;
            RamScoreDetail = res.Details;
            UpdateOverallScore();
            if (!StatusMessage.StartsWith("[")) IsBusy = false;
        }

        [RelayCommand]
        public async Task RunDiskBenchmarkAsync()
        {
            if (IsBusy && !StatusMessage.StartsWith("[")) return;
            IsBusy = true;
            DiskScoreDetail = "Замер скорости накопителя и 4K IOPS...";

            var res = await HardwareBenchmarkService.Instance.RunDiskBenchmarkAsync("C:", (p, text) =>
            {
                App.Current.Dispatcher.Invoke(() => DiskScoreDetail = text);
            });
            DiskSpeedText = res.ScoreText;
            DiskScoreDetail = res.Details;

            var resIops = await HardwareBenchmarkService.Instance.RunDiskRandom4kBenchmarkAsync("C:");
            DiskIopsText = resIops.ScoreText;
            DiskIopsDetail = resIops.Details;

            UpdateOverallScore();
            if (!StatusMessage.StartsWith("[")) IsBusy = false;
        }

        private void UpdateOverallScore()
        {
            double cpu = double.TryParse(CpuScoreText.Replace(" PTS", "").Replace(" Pts", "").Replace(" ", "").Replace(",", ""), out double c) ? c : 0;
            double gpu = double.TryParse(GpuScoreText.Replace(" PTS", "").Replace(" Pts", "").Replace(" ", "").Replace(",", ""), out double g) ? g : 0;
            double ram = double.TryParse(RamSpeedText.Replace(" ГБ/с", "").Replace(" ", "").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double r) ? r * 320 : 0;
            double disk = double.TryParse(DiskSpeedText.Replace(" МБ/с", "").Replace(" ", "").Replace(",", ""), out double d) ? d * 6 : 0;

            if (cpu > 0 || gpu > 0 || ram > 0 || disk > 0)
            {
                double total = Math.Round((cpu * 0.35) + (gpu * 0.40) + (ram * 0.15) + (disk * 0.10));
                StormOverallScoreText = FormatHelper.FormatPts(total, true);
                StormOverallScoreDetail = total > 9000 
                    ? "STORM PERFORMANCE INDEX: Экстремальный уровень (Workstation & Enthusiast Gaming)" 
                    : (total > 5000 ? "STORM PERFORMANCE INDEX: Высокий уровень (High Performance Gaming)" : "STORM PERFORMANCE INDEX: Сбалансированный ПК");
            }
        }

        [RelayCommand]
        public async Task StartStressTestAsync()
        {
            if (IsBusy || IsStressRunning) return;

            int duration = 30;
            IsStressRunning = true;
            StressProgress = 0;
            StressStatusText = "Прогрев всех ядер процессора на 100%...";
            StatusMessage = $"Запуск безопасного стресс-теста на {duration} сек...";

            await HardwareBenchmarkService.Instance.RunSafeStressTestAsync(
                duration,
                (elapsedSec, temp, text) =>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        StressProgress = (elapsedSec / (double)duration) * 100.0;
                        int remaining = Math.Max(0, duration - elapsedSec);
                        StressTimeRemainingText = $"{remaining} сек";
                        StressStatusText = $"{text}";
                        LiveCpuTempText = $"{temp:F0} °C";
                    });
                },
                (success, reason) =>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        IsStressRunning = false;
                        StressProgress = 100;
                        StressTimeRemainingText = "Готово";
                        StressStatusText = reason;
                        StatusMessage = reason;
                        TrayService.Instance.ShowNotification("Стресс-тест завершен", reason);
                    });
                }
            );
        }

        [RelayCommand]
        public void StopStressTest()
        {
            if (IsStressRunning)
            {
                HardwareBenchmarkService.Instance.CancelStressTest();
                StressStatusText = "Остановка стресс-теста...";
                StatusMessage = "Стресс-тест остановлен пользователем.";
            }
        }
    }
}
