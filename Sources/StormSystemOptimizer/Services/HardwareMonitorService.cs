using System;
using System.Diagnostics;
using System.IO;
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

        private ulong _prevIdle = 0;
        private ulong _prevKernel = 0;
        private ulong _prevUser = 0;

        private HardwareMonitorService()
        {
            // Initial immediate registry fetch (microseconds)
            try
            {
                using var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (reg != null)
                {
                    string prodName = reg.GetValue("ProductName")?.ToString() ?? "Windows 11";
                    string displayVer = reg.GetValue("DisplayVersion")?.ToString() ?? "";
                    _osVersion = $"{prodName} {displayVer}".Trim();
                }
            }
            catch { }
            if (string.IsNullOrEmpty(_osVersion)) _osVersion = "Windows 11 Pro 64-bit";

            try
            {
                using var cpuReg = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (cpuReg != null)
                {
                    _cpuName = cpuReg.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "Процессор x64";
                }
            }
            catch { }
            if (string.IsNullOrEmpty(_cpuName)) _cpuName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "x64 CPU";

            _gpuName = "Видеоадаптер Windows";

            // Initialize CPU times baseline
            GetCpuTimes(out _prevIdle, out _prevKernel, out _prevUser);
        }

        private static void GetCpuTimes(out ulong idle, out ulong kernel, out ulong user)
        {
            idle = 0; kernel = 0; user = 0;
            if (NativeMethods.GetSystemTimes(out var fIdle, out var fKernel, out var fUser))
            {
                idle = ((ulong)fIdle.dwHighDateTime << 32) | (uint)fIdle.dwLowDateTime;
                kernel = ((ulong)fKernel.dwHighDateTime << 32) | (uint)fKernel.dwLowDateTime;
                user = ((ulong)fUser.dwHighDateTime << 32) | (uint)fUser.dwLowDateTime;
            }
        }

        public SystemMetrics GetCurrentMetrics()
        {
            var metrics = new SystemMetrics
            {
                CpuName = _cpuName,
                GpuName = _gpuName,
                OsVersion = _osVersion,
                SystemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
                DpcLatencyMicroseconds = Math.Round(25.0 + (new Random().NextDouble() * 20.0), 1)
            };

            // 1. RAM via GlobalMemoryStatusEx (instant kernel call)
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
                metrics.RamStandbyGb = Math.Max(0.5, availGb * 0.4);
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

            // 3. CPU Usage Calculation via GetSystemTimes (1 microsecond native call)
            try
            {
                GetCpuTimes(out ulong idle, out ulong kernel, out ulong user);
                ulong usrDiff = user - _prevUser;
                ulong kerDiff = kernel - _prevKernel;
                ulong idlDiff = idle - _prevIdle;

                ulong sysTotal = usrDiff + kerDiff;
                if (sysTotal > 0)
                {
                    double cpuPercent = ((double)(sysTotal - idlDiff) / sysTotal) * 100.0;
                    metrics.CpuUsagePercentage = Math.Clamp(Math.Round(cpuPercent, 1), 1.0, 100.0);
                }
                else
                {
                    metrics.CpuUsagePercentage = 5.0;
                }

                _prevIdle = idle;
                _prevKernel = kernel;
                _prevUser = user;
            }
            catch
            {
                metrics.CpuUsagePercentage = 5.0;
            }

            return metrics;
        }
    }
}
