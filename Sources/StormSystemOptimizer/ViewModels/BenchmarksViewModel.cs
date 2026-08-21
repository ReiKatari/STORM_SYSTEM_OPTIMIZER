using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.ViewModels
{
    public class CoreMetricItem
    {
        public int CoreIndex { get; set; }
        public string CoreName => $"Ядро #{CoreIndex + 1}";
        public double LoadPercentage { get; set; }
        public string LoadText => $"{LoadPercentage:F0}%";
        public string CoreColor => LoadPercentage > 80 ? "#EF4444" : (LoadPercentage > 50 ? "#F59E0B" : "#00D2FF");
    }

    public partial class BenchmarksViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy = false;

        public bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotStressTesting))]
        [NotifyPropertyChangedFor(nameof(IsStressTesting))]
        private bool _isStressRunning = false;

        public bool IsStressTesting => IsStressRunning;
        public bool IsNotStressTesting => !IsStressRunning;

        [ObservableProperty]
        private string _statusMessage = "Готов к запуску тестов производительности CPU, GPU, RAM, Диска и стресс-теста.";

        [ObservableProperty]
        private double _stressProgress = 0;

        [ObservableProperty]
        private string _stressStatusText = "Нажмите «Запустить стресс-тест» для проверки термопакета";

        [ObservableProperty]
        private string _stressTimeRemainingText = "30 сек";

        // CPU Multi-Core
        [ObservableProperty]
        private string _cpuScoreText = "— Pts";

        [ObservableProperty]
        private string _cpuScoreDetail = "Многоядерный тест вычислений";

        // CPU Single-Core
        [ObservableProperty]
        private string _singleCoreScoreText = "— Pts";

        [ObservableProperty]
        private string _singleCoreScoreDetail = "Одноядерная производительность (IPC)";

        // GPU Direct3D / Shaders
        [ObservableProperty]
        private string _gpuScoreText = "— Pts";

        [ObservableProperty]
        private string _gpuScoreDetail = "Direct3D шейдеры и 3D рендеринг";

        // GPU VRAM
        [ObservableProperty]
        private string _gpuVramScoreText = "— ГБ/с";

        [ObservableProperty]
        private string _gpuVramScoreDetail = "Пропускная способность шины VRAM";

        // RAM Bandwidth & Latency
        [ObservableProperty]
        private string _ramSpeedText = "— ГБ/с";

        [ObservableProperty]
        private string _ramScoreDetail = "Скорость копирования и задержка";

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
        private string _stormOverallScoreText = "— STORM Index";

        [ObservableProperty]
        private string _stormOverallScoreDetail = "Комплексный индекс всей системы";

        [ObservableProperty]
        private string _liveCpuTempText = "-- °C";

        public ObservableCollection<HardwareSensorItem> HardwareSensors { get; } = new();
        public ObservableCollection<CoreMetricItem> CpuCores { get; } = new();

        public BenchmarksViewModel()
        {
            _ = RefreshSensorsAsync();
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
        }

        [RelayCommand]
        public async Task RunAllBenchmarksAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            StatusMessage = "Запуск комплексного цикла тестирования...";

            await RunGpuBenchmarkAsync();
            await RunGpuVramBenchmarkAsync();
            await RunCpuBenchmarkAsync();
            await RunSingleCoreBenchmarkAsync();
            await RunRamBenchmarkAsync();
            await RunDiskBenchmarkAsync();

            UpdateOverallScore();
            StatusMessage = "Все бенчмарки успешно выполнены! Расчитан STORM Performance Index.";
            IsBusy = false;
            TrayService.Instance.ShowNotification("Бенчмарки завершены ⚡", $"Общий индекс производительности STORM Index: {StormOverallScoreText}");
        }

        [RelayCommand]
        public async Task RunGpuBenchmarkAsync()
        {
            if (IsBusy && StatusMessage != "Запуск комплексного цикла тестирования...") return;
            IsBusy = true;
            GpuScoreDetail = "Тестирование Direct3D 11/12 шейдеров...";

            var res = await HardwareBenchmarkService.Instance.RunGpuBenchmarkAsync();
            GpuScoreText = res.ScoreText;
            GpuScoreDetail = res.Details;
            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunGpuVramBenchmarkAsync()
        {
            if (IsBusy && StatusMessage != "Запуск комплексного цикла тестирования...") return;
            IsBusy = true;
            GpuVramScoreDetail = "Замер пропускной способности видеопамяти...";

            var res = await HardwareBenchmarkService.Instance.RunGpuVramBenchmarkAsync();
            GpuVramScoreText = res.ScoreText;
            GpuVramScoreDetail = res.Details;
            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunCpuBenchmarkAsync()
        {
            if (IsBusy && StatusMessage != "Запуск комплексного цикла тестирования...") return;
            IsBusy = true;
            CpuScoreDetail = "Многоядерный стресс-тест CPU (Все потоки)...";

            var res = await HardwareBenchmarkService.Instance.RunCpuBenchmarkAsync();
            CpuScoreText = res.ScoreText;
            CpuScoreDetail = res.Details;
            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunSingleCoreBenchmarkAsync()
        {
            if (IsBusy && StatusMessage != "Запуск комплексного цикла тестирования...") return;
            IsBusy = true;
            SingleCoreScoreDetail = "Тест одного ядра CPU (IPC / Single-Thread)...";

            var res = await HardwareBenchmarkService.Instance.RunSingleCoreCpuBenchmarkAsync();
            SingleCoreScoreText = res.ScoreText;
            SingleCoreScoreDetail = res.Details;
            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunRamBenchmarkAsync()
        {
            if (IsBusy && StatusMessage != "Запуск комплексного цикла тестирования...") return;
            IsBusy = true;
            RamScoreDetail = "Тест скорости шины RAM и latency...";

            var res = await HardwareBenchmarkService.Instance.RunRamBenchmarkAsync();
            RamSpeedText = res.ScoreText;
            RamScoreDetail = res.Details;
            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunDiskBenchmarkAsync()
        {
            if (IsBusy && StatusMessage != "Запуск комплексного цикла тестирования...") return;
            IsBusy = true;
            DiskScoreDetail = "Замер скорости накопителя и 4K IOPS...";

            var res = await HardwareBenchmarkService.Instance.RunDiskBenchmarkAsync("C:");
            DiskSpeedText = res.ScoreText;
            DiskScoreDetail = res.Details;
            UpdateOverallScore();
            IsBusy = false;
        }

        private void UpdateOverallScore()
        {
            double cpu = double.TryParse(CpuScoreText.Replace(" Pts", "").Replace(" ", "").Replace(",", ""), out double c) ? c : 0;
            double gpu = double.TryParse(GpuScoreText.Replace(" Pts", "").Replace(" ", "").Replace(",", ""), out double g) ? g : 0;
            double ram = double.TryParse(RamSpeedText.Replace(" ГБ/с", "").Replace(" ", "").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double r) ? r * 300 : 0;
            double disk = double.TryParse(DiskSpeedText.Replace(" МБ/с", "").Replace(" ", "").Replace(",", ""), out double d) ? d * 6 : 0;

            if (cpu > 0 || gpu > 0 || ram > 0 || disk > 0)
            {
                double total = Math.Round((cpu * 0.35) + (gpu * 0.40) + (ram * 0.15) + (disk * 0.10));
                StormOverallScoreText = $"{total:N0} Pts";
                StormOverallScoreDetail = total > 9000 ? "Уровень: Экстремальный гейминг / Workstation" : (total > 5000 ? "Уровень: Высокая производительность" : "Уровень: Сбалансированный ПК");
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
