using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class HardwareMonitorService
    {
        private static HardwareMonitorService? _instance;
        public static HardwareMonitorService Instance => _instance ??= new HardwareMonitorService();

        private string _cpuName = string.Empty;
        private string _gpuName = string.Empty;
        private string _osVersion = string.Empty;

        private TimeSpan _prevCpuTime = TimeSpan.Zero;
        private DateTime _prevTime = DateTime.UtcNow;

        private HardwareMonitorService()
        {
            InitializeHardwareInfoAsync();
        }

        private async void InitializeHardwareInfoAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // OS Name
                    using var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                    if (reg != null)
                    {
                        string prodName = reg.GetValue("ProductName")?.ToString() ?? "Windows 11";
                        string displayVer = reg.GetValue("DisplayVersion")?.ToString() ?? "";
                        _osVersion = $"{prodName} {displayVer}".Trim();
                    }
                    if (string.IsNullOrEmpty(_osVersion)) _osVersion = Environment.OSVersion.ToString();

                    // CPU Name
                    using var cpuReg = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                    if (cpuReg != null)
                    {
                        _cpuName = cpuReg.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "Процессор x64";
                    }

                    // GPU Name via WMI
                    using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                    foreach (var obj in searcher.Get())
                    {
                        string? name = obj["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            _gpuName = name;
                            break;
                        }
                    }
                }
                catch
                {
                    if (string.IsNullOrEmpty(_cpuName)) _cpuName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "x64 CPU";
                    if (string.IsNullOrEmpty(_gpuName)) _gpuName = "Видеоадаптер Windows";
                    if (string.IsNullOrEmpty(_osVersion)) _osVersion = "Windows 11 Pro 64-bit";
                }
            });
        }

        public SystemMetrics GetCurrentMetrics()
        {
            var metrics = new SystemMetrics
            {
                ProcessorName = string.IsNullOrEmpty(_cpuName) ? "Процессор x64" : _cpuName,
                GpuName = string.IsNullOrEmpty(_gpuName) ? "Графический адаптер" : _gpuName,
                OsVersion = string.IsNullOrEmpty(_osVersion) ? "Windows 11" : _osVersion,
                SystemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
            };

            // 1. RAM via GlobalMemoryStatusEx
            var memStatus = new NativeMethods.MEMORYSTATUSEX();
            memStatus.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.MEMORYSTATUSEX));
            if (NativeMethods.GlobalMemoryStatusEx(ref memStatus))
            {
                double totalGb = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                double availGb = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                double usedGb = totalGb - availGb;

                metrics.RamTotalGb = totalGb;
                metrics.RamUsedGb = usedGb;
                metrics.RamAvailableGb = availGb;
                metrics.RamUsagePercentage = memStatus.dwMemoryLoad;
                metrics.RamStandbyGb = Math.Max(0.5, availGb * 0.4); // Estimated Standby cache
            }

            // 2. Primary Disk (C:)
            try
            {
                var drive = new DriveInfo("C");
                if (drive.IsReady)
                {
                    double totalDiskGb = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    double freeDiskGb = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    double usedDiskGb = totalDiskGb - freeDiskGb;

                    metrics.PrimaryDrive = drive.Name;
                    metrics.DriveTotalGb = totalDiskGb;
                    metrics.DriveFreeGb = freeDiskGb;
                    metrics.DiskUsagePercentage = (usedDiskGb / totalDiskGb) * 100.0;
                }
            }
            catch { }

            // 3. CPU Usage Calculation
            try
            {
                var now = DateTime.UtcNow;
                var totalCpuTime = TimeSpan.Zero;
                var procs = Process.GetProcesses();
                foreach (var p in procs)
                {
                    try { totalCpuTime += p.TotalProcessorTime; }
                    catch { }
                    finally { p.Dispose(); }
                }

                if (_prevCpuTime != TimeSpan.Zero && (now - _prevTime).TotalMilliseconds > 200)
                {
                    var timeDiff = (now - _prevTime).TotalMilliseconds;
                    var cpuDiff = (totalCpuTime - _prevCpuTime).TotalMilliseconds;
                    double usage = (cpuDiff / (timeDiff * Environment.ProcessorCount)) * 100.0;
                    metrics.CpuUsagePercentage = Math.Clamp(Math.Round(usage, 1), 1.0, 100.0);
                }
                else
                {
                    metrics.CpuUsagePercentage = 12.0; // fallback initial estimate
                }

                _prevCpuTime = totalCpuTime;
                _prevTime = now;
            }
            catch
            {
                metrics.CpuUsagePercentage = 15.0;
            }

            return metrics;
        }
    }
}
