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
        private bool _isBusy = false;

        [ObservableProperty]
        private bool _isStressRunning = false;

        [ObservableProperty]
        private string _statusMessage = "Готов к запуску тестов производительности и стабильности.";

        [ObservableProperty]
        private double _stressProgressPercent = 0;

        [ObservableProperty]
        private string _liveCpuTempText = "-- °C";

        [ObservableProperty]
        private string _liveCpuLoadText = "0%";

        [ObservableProperty]
        private string _selectedStressDuration = "30"; // 15, 30, 60

        public ObservableCollection<HardwareSensorItem> TemperatureSensors { get; } = new();
        public ObservableCollection<BenchmarkResult> BenchmarkResults { get; } = new();
        public ObservableCollection<string> AvailableDrives { get; } = new();

        [ObservableProperty]
        private string _selectedDrive = "C:\\";

        private readonly DispatcherTimer _tempTimer;

        public BenchmarksViewModel()
        {
            _tempTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _tempTimer.Tick += async (s, e) => await RefreshSensorsAsync();
            _tempTimer.Start();

            LoadDrives();
            _ = RefreshSensorsAsync();
        }

        private void LoadDrives()
        {
            AvailableDrives.Clear();
            foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                AvailableDrives.Add(d.Name);
            }
            if (AvailableDrives.Count > 0) SelectedDrive = AvailableDrives[0];
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
        public async Task RunAllBenchmarksAsync()
        {
            if (IsBusy || IsStressRunning) return;
            IsBusy = true;
            StatusMessage = "Запуск комплексного тестирования CPU, RAM и Диска...";

            BenchmarkResults.Clear();

            // 1. CPU
            StatusMessage = "⚡ Выполняется тест производительности процессора...";
            var cpuRes = await HardwareBenchmarkService.Instance.RunCpuBenchmarkAsync();
            BenchmarkResults.Add(cpuRes);

            // 2. RAM
            StatusMessage = "💾 Выполняется тест скорости памяти RAM...";
            var ramRes = await HardwareBenchmarkService.Instance.RunRamBenchmarkAsync();
            BenchmarkResults.Add(ramRes);

            // 3. Disk
            if (!string.IsNullOrEmpty(SelectedDrive))
            {
                StatusMessage = $"💿 Выполняется тест скорости накопителя {SelectedDrive}...";
                var diskRes = await HardwareBenchmarkService.Instance.RunDiskBenchmarkAsync(SelectedDrive);
                BenchmarkResults.Add(diskRes);
            }

            IsBusy = false;
            StatusMessage = "Все тесты успешно завершены!";
            TrayService.Instance.ShowNotification("Тестирование завершено", "Все компоненты системы проверены. Результаты готовы к просмотру.");
        }

        [RelayCommand]
        public async Task StartSafeStressTestAsync()
        {
            if (IsBusy || IsStressRunning) return;

            int duration = int.TryParse(SelectedStressDuration, out int d) ? d : 30;
            IsStressRunning = true;
            StressProgressPercent = 0;
            StatusMessage = $"Запуск безопасного стресс-теста на {duration} секунд...";

            await HardwareBenchmarkService.Instance.RunSafeStressTestAsync(
                duration,
                (elapsedSec, temp, text) =>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        StressProgressPercent = (elapsedSec / (double)duration) * 100.0;
                        StatusMessage = text;
                        LiveCpuTempText = $"{temp:F0} °C";
                    });
                },
                (success, reason) =>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        IsStressRunning = false;
                        StressProgressPercent = 100;
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
                StatusMessage = "Остановка стресс-теста...";
            }
        }
    }
}
