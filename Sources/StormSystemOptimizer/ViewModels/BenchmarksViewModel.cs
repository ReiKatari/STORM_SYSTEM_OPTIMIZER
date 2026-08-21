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
        private string _statusMessage = "Готов к запуску тестов производительности и стабильности.";

        [ObservableProperty]
        private double _stressProgress = 0;

        [ObservableProperty]
        private string _stressStatusText = "Нажмите «Запустить стресс-тест» для проверки термопакета";

        [ObservableProperty]
        private string _stressTimeRemainingText = "30 сек";

        [ObservableProperty]
        private string _cpuScoreText = "— Pts";

        [ObservableProperty]
        private string _cpuScoreDetail = "Готов к тестированию";

        [ObservableProperty]
        private string _ramSpeedText = "— ГБ/с";

        [ObservableProperty]
        private string _ramScoreDetail = "Готов к тестированию";

        [ObservableProperty]
        private string _diskSpeedText = "— МБ/с";

        [ObservableProperty]
        private string _diskScoreDetail = "Готов к тестированию";

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
            CpuScoreDetail = "Вычисление SHA256 и многопоточный стресс...";
            StatusMessage = "⚡ Выполняется тест процессора...";

            var res = await HardwareBenchmarkService.Instance.RunCpuBenchmarkAsync();
            CpuScoreText = $"{res.NumericScore:N0} Pts";
            CpuScoreDetail = res.Details;
            StatusMessage = $"Тест CPU завершен: {CpuScoreText} ({res.Rating})";

            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunRamBenchmarkAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            RamSpeedText = "Тест...";
            RamScoreDetail = "Копирование блоков 1 ГБ в памяти...";
            StatusMessage = "🧠 Выполняется тест пропускной способности RAM...";

            var res = await HardwareBenchmarkService.Instance.RunRamBenchmarkAsync();
            RamSpeedText = $"{res.NumericScore:F1} ГБ/с";
            RamScoreDetail = res.Details;
            StatusMessage = $"Тест RAM завершен: {RamSpeedText} ({res.Rating})";

            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunDiskBenchmarkAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            DiskSpeedText = "Тест...";
            DiskScoreDetail = "Замер последовательной записи/чтения...";
            StatusMessage = "💾 Выполняется тест скорости системного накопителя...";

            var res = await HardwareBenchmarkService.Instance.RunDiskBenchmarkAsync("C:\\");
            DiskSpeedText = $"{res.NumericScore:F0} МБ/с";
            DiskScoreDetail = res.Details;
            StatusMessage = $"Тест накопителя завершен: {DiskSpeedText} ({res.Rating})";

            IsBusy = false;
        }

        [RelayCommand]
        public async Task RunAllBenchmarksAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            StatusMessage = "Запуск комплексного тестирования CPU, RAM и Диска...";

            await RunCpuBenchmarkAsync();
            await RunRamBenchmarkAsync();
            await RunDiskBenchmarkAsync();

            StatusMessage = "Все тесты успешно завершены!";
            TrayService.Instance.ShowNotification("Тестирование завершено", "Все компоненты системы проверены. Результаты готовы.");
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
                        StressStatusText = $"{text} (Температура: {temp:F0}°C)";
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
