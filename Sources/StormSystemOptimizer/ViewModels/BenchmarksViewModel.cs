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

        [ObservableProperty]
        private string _liveCpuLoadText = "0%";

        public ObservableCollection<HardwareSensorItem> TemperatureSensors { get; } = new();
        public ObservableCollection<HardwareSensorItem> HardwareSensors => TemperatureSensors;
        public ObservableCollection<BenchmarkResult> BenchmarkResults { get; } = new();

        private readonly DispatcherTimer _tempTimer;

        public BenchmarksViewModel()
        {
            _tempTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _tempTimer.Tick += async (s, e) => await RefreshSensorsAsync();
            _tempTimer.Start();

            _ = RefreshSensorsAsync();
        }

        [RelayCommand]
        public async Task RefreshSensorsAsync()
        {
            var list = await HardwareTemperatureService.Instance.GetAllTemperaturesAsync();
            TemperatureSensors.Clear();
            foreach (var item in list)
            {
                TemperatureSensors.Add(item);
            }

            double cpuTemp = HardwareTemperatureService.Instance.GetCpuTemperature();
            LiveCpuTempText = $"{cpuTemp:F0} °C";
            LiveCpuLoadText = $"{HardwareMonitorService.Instance.GetCurrentMetrics().CpuUsagePercentage:F0}%";
        }

        [RelayCommand]
        public async Task RunCpuBenchmarkAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            CpuScoreText = "Тест...";
            CpuScoreDetail = "Многопоточный расчет...";
            StatusMessage = "⚡ Выполняется многоядерный тест процессора...";

            var res = await HardwareBenchmarkService.Instance.RunCpuBenchmarkAsync();
            CpuScoreText = $"{res.NumericScore:N0} Pts";
            CpuScoreDetail = res.Details;
            StatusMessage = $"Тест Multi-Core CPU завершен: {CpuScoreText} ({res.Rating})";

            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunSingleCoreBenchmarkAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            SingleCoreScoreText = "Тест...";
            SingleCoreScoreDetail = "Расчет 1 ядра...";
            StatusMessage = "⚡ Выполняется тест одноядерной производительности (IPC)...";

            var res = await HardwareBenchmarkService.Instance.RunSingleCoreCpuBenchmarkAsync();
            SingleCoreScoreText = $"{res.NumericScore:N0} Pts";
            SingleCoreScoreDetail = res.Details;
            StatusMessage = $"Тест Single-Core CPU завершен: {SingleCoreScoreText} ({res.Rating})";

            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunGpuBenchmarkAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            GpuScoreText = "Тест...";
            GpuScoreDetail = "3D Шейдеры и Direct3D...";
            StatusMessage = "🎮 Выполняется тест графического ускорителя (GPU)...";

            var res = await HardwareBenchmarkService.Instance.RunGpuBenchmarkAsync();
            GpuScoreText = $"{res.NumericScore:N0} Pts";
            GpuScoreDetail = res.Details;
            StatusMessage = $"Тест GPU завершен: {GpuScoreText} ({res.Rating})";

            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunGpuVramBenchmarkAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            GpuVramScoreText = "Тест...";
            GpuVramScoreDetail = "Тест шины видеопамяти...";
            StatusMessage = "🎮 Выполняется тест видеопамяти (VRAM)...";

            var res = await HardwareBenchmarkService.Instance.RunGpuVramBenchmarkAsync();
            GpuVramScoreText = $"{res.NumericScore:F1} ГБ/с";
            GpuVramScoreDetail = res.Details;
            StatusMessage = $"Тест VRAM завершен: {GpuVramScoreText} ({res.Rating})";

            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunRamBenchmarkAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            RamSpeedText = "Тест...";
            RamScoreDetail = "Копирование памяти...";
            StatusMessage = "🧠 Выполняется тест пропускной способности RAM...";

            var res = await HardwareBenchmarkService.Instance.RunRamBenchmarkAsync();
            RamSpeedText = $"{res.NumericScore:F1} ГБ/с";
            RamScoreDetail = res.Details;
            StatusMessage = $"Тест RAM завершен: {RamSpeedText} ({res.Rating})";

            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunDiskBenchmarkAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            DiskSpeedText = "Тест...";
            DiskScoreDetail = "Последовательный ввод-вывод...";
            StatusMessage = "💾 Выполняется тест скорости накопителя...";

            var res = await HardwareBenchmarkService.Instance.RunDiskBenchmarkAsync("C:\\");
            DiskSpeedText = $"{res.NumericScore:F0} МБ/с";
            DiskScoreDetail = res.Details;
            StatusMessage = $"Тест накопителя завершен: {DiskSpeedText} ({res.Rating})";

            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunDiskIopsBenchmarkAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            DiskIopsText = "Тест...";
            DiskIopsDetail = "Случайный доступ 4K...";
            StatusMessage = "💾 Выполняется тест случайного доступа 4K IOPS...";

            var res = await HardwareBenchmarkService.Instance.RunDiskRandom4kBenchmarkAsync("C:\\");
            DiskIopsText = $"{res.NumericScore:N0} IOPS";
            DiskIopsDetail = res.Details;
            StatusMessage = $"Тест 4K IOPS завершен: {DiskIopsText} ({res.Rating})";

            UpdateOverallScore();
            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunAllBenchmarksAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            StatusMessage = "Запуск комплексного стресс-тестирования всех компонентов системы...";

            await RunCpuBenchmarkAsync();
            await RunSingleCoreBenchmarkAsync();
            await RunGpuBenchmarkAsync();
            await RunGpuVramBenchmarkAsync();
            await RunRamBenchmarkAsync();
            await RunDiskBenchmarkAsync();
            await RunDiskIopsBenchmarkAsync();

            UpdateOverallScore();
            StatusMessage = $"Все 7 бенчмарков успешно выполнены! Общий индекс: {StormOverallScoreText}";
            TrayService.Instance.ShowNotification("Тестирование завершено", $"Все компоненты системы проверены. Общий балл: {StormOverallScoreText}");
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
