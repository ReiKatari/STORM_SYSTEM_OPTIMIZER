using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace StormSystemOptimizer.Services
{
    public class SmartDaemonService
    {
        private static SmartDaemonService? _instance;
        public static SmartDaemonService Instance => _instance ??= new SmartDaemonService();

        private DispatcherTimer? _daemonTimer;
        private bool _isRunning = false;
        private DateTime _lastIdleTrimTime = DateTime.MinValue;
        private int _idleCounter = 0;

        public bool IsRunning => _isRunning;

        private SmartDaemonService() { }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            _daemonTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _daemonTimer.Tick += async (s, e) => await OnDaemonTickAsync();
            _daemonTimer.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _daemonTimer?.Stop();
        }

        private async Task OnDaemonTickAsync()
        {
            await Task.Run(async () =>
            {
                try
                {
                    // 1. Check RAM usage: if > 88%, run smart compression
                    double ramLoad = MemoryOptimizerService.Instance.GetRamUsagePercentage();
                    if (ramLoad > 88.0)
                    {
                        var (procCount, freedMb) = await MemoryOptimizerService.Instance.SmartCompressMemoryAsync();
                        TrayService.Instance.ShowNotification("STORM Автопилот", $"Освобождено {freedMb:F0} МБ памяти у {procCount} фоновых процессов.");
                    }

                    // 2. Check CPU Temperature: if >= 85°C, show warning
                    double cpuTemp = HardwareTemperatureService.Instance.GetCpuTemperature();
                    if (cpuTemp >= 85.0)
                    {
                        TrayService.Instance.ShowNotification("Предупреждение о температуре ⚠️", $"Температура процессора достигла {cpuTemp:F0} °C! Проверьте охлаждение.");
                    }

                    // 3. Check System Idle for Auto-TRIM (once per 4 hours)
                    double cpuUsage = HardwareMonitorService.Instance.GetCurrentMetrics().CpuUsagePercentage;
                    if (cpuUsage < 8.0)
                    {
                        _idleCounter++;
                        if (_idleCounter >= 12 && (DateTime.Now - _lastIdleTrimTime).TotalHours >= 4)
                        {
                            _idleCounter = 0;
                            _lastIdleTrimTime = DateTime.Now;
                            await SystemToolsService.Instance.RunSsdTrimAsync("C:");
                        }
                    }
                    else
                    {
                        _idleCounter = 0;
                    }
                }
                catch { }
            });
        }
    }
}
