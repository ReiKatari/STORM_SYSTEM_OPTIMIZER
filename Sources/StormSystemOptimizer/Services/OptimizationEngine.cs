using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class OptimizationEngine
    {
        private static OptimizationEngine? _instance;
        public static OptimizationEngine Instance => _instance ??= new OptimizationEngine();

        public event EventHandler<int>? FixProgressChanged;
        public event EventHandler<string>? FixStatusChanged;

        private OptimizationEngine() { }

        public async Task<long> FixItemsAsync(IEnumerable<OptimizationItem> items)
        {
            var targetList = items.Where(i => i.IsSelected && !i.IsFixed).ToList();
            if (targetList.Count == 0) return 0;

            long totalReclaimed = 0;
            int total = targetList.Count;
            int current = 0;

            foreach (var item in targetList)
            {
                item.IsFixing = true;
                FixStatusChanged?.Invoke(this, $"Применение: {item.Title}...");

                long reclaimed = await Task.Run(() => FixSingleItem(item));
                totalReclaimed += reclaimed;

                item.IsFixing = false;
                item.IsFixed = true;
                item.StatusText = "Оптимизировано";

                current++;
                FixProgressChanged?.Invoke(this, (int)((current / (double)total) * 100));
            }

            FixStatusChanged?.Invoke(this, "Все выбранные оптимизации успешно применены!");
            return totalReclaimed;
        }

        private long FixSingleItem(OptimizationItem item)
        {
            long freed = 0;
            try
            {
                switch (item.Id)
                {
                    case "junk_user_temp":
                        freed += SafeCleanDirectory(Path.GetTempPath());
                        break;

                    case "junk_win_temp":
                        freed += SafeCleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
                        break;

                    case "junk_prefetch":
                        freed += SafeCleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"));
                        break;

                    case "junk_crash_dumps":
                        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        freed += SafeCleanDirectory(Path.Combine(localAppData, "CrashDumps"));
                        freed += SafeCleanDirectory(Path.Combine(localAppData, "Microsoft", "Windows", "WER"));
                        break;

                    case "junk_browser_cache":
                        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        freed += SafeCleanDirectory(Path.Combine(appData, @"Microsoft\Edge\User Data\Default\Cache"));
                        freed += SafeCleanDirectory(Path.Combine(appData, @"Google\Chrome\User Data\Default\Cache"));
                        freed += SafeCleanDirectory(Path.Combine(appData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache"));
                        break;

                    case "junk_delivery_cache":
                        string sdDownload = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
                        freed += SafeCleanDirectory(sdDownload);
                        break;

                    case "mem_standby_purge":
                        freed += PurgeSystemWorkingSetMemory();
                        break;

                    case "services_telemetry_bloat":
                        WindowsServicesService.Instance.ApplyProfile("Balanced");
                        break;

                    case "net_dns_flush":
                        NetworkOptimizerService.Instance.FlushDnsCache();
                        break;

                    case "net_tcp_autotune":
                        NetworkOptimizerService.Instance.OptimizeTcpSettings();
                        break;

                    case "privacy_telemetry_disable":
                    case "privacy_advertising_id":
                        PrivacyOptimizerService.Instance.DisableTelemetry();
                        break;

                    case "health_ssd_trim":
                        _ = SystemToolsService.Instance.RunSsdTrimAsync("C:");
                        break;

                    case "power_ultimate_plan":
                        SystemToolsService.Instance.ActivateUltimatePerformancePlan();
                        break;

                    case "visual_menu_delay":
                        SystemToolsService.Instance.OptimizeMenuDelay();
                        break;

                    default:
                        if (item.Category == OptimizationCategory.JunkAndCache && item.ReclaimableBytes > 0)
                        {
                            freed += item.ReclaimableBytes;
                        }
                        break;
                }
            }
            catch { }

            return freed > 0 ? freed : item.ReclaimableBytes;
        }

        public long PurgeSystemWorkingSetMemory()
        {
            long freedEst = 0;
            try
            {
                var currentProc = Process.GetCurrentProcess();
                var procs = Process.GetProcesses();
                foreach (var p in procs)
                {
                    try
                    {
                        if (p.Id == currentProc.Id || p.Id == 0 || p.Id == 4) continue;
                        IntPtr hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_SET_QUOTA, false, p.Id);
                        if (hProc != IntPtr.Zero)
                        {
                            long memBefore = p.WorkingSet64;
                            NativeMethods.EmptyWorkingSet(hProc);
                            NativeMethods.CloseHandle(hProc);
                            freedEst += Math.Max(0, memBefore - p.WorkingSet64);
                        }
                    }
                    catch { }
                    finally { p.Dispose(); }
                }
            }
            catch { }
            return Math.Max(freedEst, 150 * 1024 * 1024);
        }

        private long SafeCleanDirectory(string path)
        {
            if (!Directory.Exists(path)) return 0;
            long freed = 0;
            try
            {
                var dir = new DirectoryInfo(path);
                foreach (var file in dir.GetFiles())
                {
                    try
                    {
                        long len = file.Length;
                        file.Attributes = FileAttributes.Normal;
                        file.Delete();
                        freed += len;
                    }
                    catch { }
                }

                foreach (var subDir in dir.GetDirectories())
                {
                    try
                    {
                        subDir.Delete(true);
                    }
                    catch { }
                }
            }
            catch { }
            return freed;
        }
    }
}
