using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class AdvancedTweaksService
    {
        private static AdvancedTweaksService? _instance;
        public static AdvancedTweaksService Instance => _instance ??= new AdvancedTweaksService();

        private AdvancedTweaksService() { }

        private static void RunRegCommand(string args)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                p?.WaitForExit(1500);
            }
            catch { }
        }

        private static void RunCmdCommand(string args)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {args}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                p?.WaitForExit(2500);
            }
            catch { }
        }

        // 1. MSI Mode (Message Signaled Interrupts) for GPU and USB Controllers
        public bool EnableMsiModeForGpuAndUsb()
        {
            int configuredCount = 0;
            try
            {
                // Scan PCI devices in Registry (Read-only scan)
                using var pciKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI", false);
                if (pciKey != null)
                {
                    foreach (var vendorSubName in pciKey.GetSubKeyNames())
                    {
                        using var vendorKey = pciKey.OpenSubKey(vendorSubName, false);
                        if (vendorKey == null) continue;

                        foreach (var devInstanceName in vendorKey.GetSubKeyNames())
                        {
                            using var devKey = vendorKey.OpenSubKey(devInstanceName, false);
                            if (devKey == null) continue;

                            string classGuid = devKey.GetValue("ClassGUID")?.ToString() ?? "";
                            string desc = devKey.GetValue("DeviceDesc")?.ToString() ?? "";

                            // Display Adapters GUID {4d36e968-e325-11ce-bfc1-08002be10318} or USB {36fc9e60-c465-11cf-8056-444553540000}
                            bool isGpu = classGuid.Equals("{4d36e968-e325-11ce-bfc1-08002be10318}", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("Intel(R) Arc", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("Graphics", StringComparison.OrdinalIgnoreCase);

                            bool isUsb = classGuid.Equals("{36fc9e60-c465-11cf-8056-444553540000}", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("xHCI", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("Host Controller", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("USB", StringComparison.OrdinalIgnoreCase);

                            if (isGpu || isUsb)
                            {
                                string subPath = $@"SYSTEM\CurrentControlSet\Enum\PCI\{vendorSubName}\{devInstanceName}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";
                                RunRegCommand($"add \"HKLM\\{subPath}\" /v MSISupported /t REG_DWORD /d 1 /f");

                                if (isGpu)
                                {
                                    string affPath = $@"SYSTEM\CurrentControlSet\Enum\PCI\{vendorSubName}\{devInstanceName}\Device Parameters\Interrupt Management\Affinity Policy";
                                    RunRegCommand($"add \"HKLM\\{affPath}\" /v DevicePriority /t REG_DWORD /d 3 /f");
                                }
                                configuredCount++;
                            }
                        }
                    }
                }

                return true;
            }
            catch
            {
                return configuredCount > 0;
            }
        }

        public bool IsMsiModeActive()
        {
            try
            {
                using var pciKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI", false);
                if (pciKey != null)
                {
                    foreach (var vendorSubName in pciKey.GetSubKeyNames())
                    {
                        using var vendorKey = pciKey.OpenSubKey(vendorSubName, false);
                        if (vendorKey == null) continue;

                        foreach (var devInstanceName in vendorKey.GetSubKeyNames())
                        {
                            using var devKey = vendorKey.OpenSubKey(devInstanceName, false);
                            if (devKey == null) continue;

                            string classGuid = devKey.GetValue("ClassGUID")?.ToString() ?? "";
                            string desc = devKey.GetValue("DeviceDesc")?.ToString() ?? "";

                            bool isGpu = classGuid.Equals("{4d36e968-e325-11ce-bfc1-08002be10318}", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("Radeon", StringComparison.OrdinalIgnoreCase);

                            if (isGpu)
                            {
                                using var msiKey = devKey.OpenSubKey(@"Device Parameters\Interrupt Management\MessageSignaledInterruptProperties", false);
                                if (msiKey != null)
                                {
                                    var val = msiKey.GetValue("MSISupported");
                                    if (val is int msiVal && msiVal == 1) return true;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        // 2. DirectStorage 1.2 & NVMe Bypass Tuning
        public bool OptimizeDirectStorageAndIoRing()
        {
            try
            {
                // 1. Direct .NET Registry Attempt
                try
                {
                    using var fsKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem");
                    if (fsKey != null)
                    {
                        fsKey.SetValue("NtfsDisable8dot3NameCreation", 1, RegistryValueKind.DWord);
                        fsKey.SetValue("NtfsMemoryUsage", 2, RegistryValueKind.DWord);
                        fsKey.SetValue("Win32IoRingFlags", 1, RegistryValueKind.DWord);
                    }
                }
                catch { }

                try
                {
                    using var storKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Storage");
                    if (storKey != null)
                    {
                        storKey.SetValue("BypassIoAllowed", 1, RegistryValueKind.DWord);
                    }
                }
                catch { }

                // 2. Shell Command Fallback
                RunRegCommand("add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v NtfsDisable8dot3NameCreation /t REG_DWORD /d 1 /f");
                RunRegCommand("add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v NtfsMemoryUsage /t REG_DWORD /d 2 /f");
                RunRegCommand("add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v Win32IoRingFlags /t REG_DWORD /d 1 /f");
                RunRegCommand("add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Storage\" /v BypassIoAllowed /t REG_DWORD /d 1 /f");
                RunCmdCommand("fsutil behavior set disable8dot3 1");

                return true;
            }
            catch
            {
                return true;
            }
        }

        public bool DisableMsiModeForGpuAndUsb()
        {
            try
            {
                using var pciKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI", false);
                if (pciKey != null)
                {
                    foreach (var vendorSubName in pciKey.GetSubKeyNames())
                    {
                        using var vendorKey = pciKey.OpenSubKey(vendorSubName, false);
                        if (vendorKey == null) continue;

                        foreach (var devInstanceName in vendorKey.GetSubKeyNames())
                        {
                            using var devKey = vendorKey.OpenSubKey(devInstanceName, false);
                            if (devKey == null) continue;

                            string classGuid = devKey.GetValue("ClassGUID")?.ToString() ?? "";
                            string desc = devKey.GetValue("DeviceDesc")?.ToString() ?? "";

                            bool isGpu = classGuid.Equals("{4d36e968-e325-11ce-bfc1-08002be10318}", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("Intel(R) Arc", StringComparison.OrdinalIgnoreCase);

                            bool isUsb = classGuid.Equals("{36fc9e60-c465-11cf-8056-444553540000}", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("Host Controller", StringComparison.OrdinalIgnoreCase) ||
                                         desc.Contains("USB", StringComparison.OrdinalIgnoreCase);

                            if (isGpu || isUsb)
                            {
                                string subPath = $@"SYSTEM\CurrentControlSet\Enum\PCI\{vendorSubName}\{devInstanceName}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";
                                RunRegCommand($"add \"HKLM\\{subPath}\" /v MSISupported /t REG_DWORD /d 0 /f");
                            }
                        }
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public bool DisableDirectStorageOptimization()
        {
            try
            {
                RunRegCommand("delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v Win32IoRingFlags /f");
                RunRegCommand("delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Storage\" /v BypassIoAllowed /f");
                RunRegCommand("add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v NtfsDisable8dot3NameCreation /t REG_DWORD /d 0 /f");
                RunRegCommand("add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v NtfsMemoryUsage /t REG_DWORD /d 1 /f");
                return true;
            }
            catch { return false; }
        }

        public bool IsDirectStorageOptimized()
        {
            try
            {
                using var fsKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem", false);
                if (fsKey != null)
                {
                    var ioRing = fsKey.GetValue("Win32IoRingFlags");
                    var memUsage = fsKey.GetValue("NtfsMemoryUsage");
                    var bypass = fsKey.GetValue("NtfsDisable8dot3NameCreation");
                    if (ioRing != null || (memUsage != null && Convert.ToInt32(memUsage) >= 2) || (bypass != null && Convert.ToInt32(bypass) == 1))
                        return true;
                }

                using var storageKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Storage", false);
                if (storageKey != null)
                {
                    var bypassIo = storageKey.GetValue("BypassIoAllowed");
                    if (bypassIo != null && Convert.ToInt32(bypassIo) == 1) return true;
                }
            }
            catch { }
            return false;
        }

        // 3. TCP NoDelay & Nagle's Algorithm Disabling (Reduce Online Ping)
        public bool DisableNaglesAlgorithm()
        {
            try
            {
                using var netKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", false);
                if (netKey != null)
                {
                    foreach (var adapterGuid in netKey.GetSubKeyNames())
                    {
                        string adapterPath = $@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{adapterGuid}";
                        RunRegCommand($"add \"HKLM\\{adapterPath}\" /v TcpAckFrequency /t REG_DWORD /d 1 /f");
                        RunRegCommand($"add \"HKLM\\{adapterPath}\" /v TCPNoDelay /t REG_DWORD /d 1 /f");
                        RunRegCommand($"add \"HKLM\\{adapterPath}\" /v TcpDelAckTicks /t REG_DWORD /d 0 /f");
                    }
                }

                RunRegCommand("add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\" /v DefaultTTL /t REG_DWORD /d 64 /f");
                RunRegCommand("add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\" /v EnableTCPA /t REG_DWORD /d 1 /f");
                RunRegCommand("add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\" /v EnableWsd /t REG_DWORD /d 0 /f");

                return true;
            }
            catch
            {
                return true;
            }
        }

        // 4. Blackhole Shield (Telemetry & Adware DNS/Firewall Blocking)
        public async Task<bool> ToggleBlackholeTelemetryShieldAsync(bool enable)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                    string markerStart = "# --- STORM BLACKHOLE TELEMETRY BLOCK START ---";
                    string markerEnd = "# --- STORM BLACKHOLE TELEMETRY BLOCK END ---";

                    if (File.Exists(hostsPath))
                    {
                        string currentHosts = File.ReadAllText(hostsPath);

                        if (currentHosts.Contains(markerStart))
                        {
                            int startIdx = currentHosts.IndexOf(markerStart);
                            int endIdx = currentHosts.IndexOf(markerEnd) + markerEnd.Length;
                            currentHosts = currentHosts.Remove(startIdx, endIdx - startIdx).Trim();
                        }

                        if (enable)
                        {
                            string[] blockedDomains = new[]
                            {
                                "0.0.0.0 v10.events.data.microsoft.com",
                                "0.0.0.0 v20.events.data.microsoft.com",
                                "0.0.0.0 telemetry.microsoft.com",
                                "0.0.0.0 vortex.data.microsoft.com",
                                "0.0.0.0 vortex-win.data.microsoft.com",
                                "0.0.0.0 settings-win.data.microsoft.com",
                                "0.0.0.0 diagnostics.support.microsoft.com",
                                "0.0.0.0 watson.telemetry.microsoft.com",
                                "0.0.0.0 sqm.telemetry.microsoft.com",
                                "0.0.0.0 choice.microsoft.com",
                                "0.0.0.0 df.telemetry.microsoft.com",
                                "0.0.0.0 reports.wes.df.telemetry.microsoft.com"
                            };

                            currentHosts += "\n\n" + markerStart + "\n" + string.Join("\n", blockedDomains) + "\n" + markerEnd + "\n";
                        }

                        // Remove Read-only attribute if present
                        File.SetAttributes(hostsPath, FileAttributes.Normal);
                        File.WriteAllText(hostsPath, currentHosts);
                    }

                    // Flush DNS after changes
                    NativeMethods.DnsFlushResolverCache();
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        // 5. Snapshot & Rollback Engine
        public async Task<string> CreateRegistryBackupSnapshotAsync(string snapshotName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StormSystemOptimizer", "Backups");
                    Directory.CreateDirectory(backupDir);

                    string fileName = $"STORM_Snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.reg";
                    string fullPath = Path.Combine(backupDir, fileName);

                    // Export critical registry hives via reg.exe
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = $"export \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" \"{fullPath}\" /y",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })?.WaitForExit(3000);

                    return fullPath;
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            });
        }

        // 6. Explorer Extreme Responsiveness & Anti-Hang
        public bool ToggleExplorerResponsiveness(bool enable)
        {
            try
            {
                if (enable)
                {
                    RunRegCommand("add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 0 /f");
                    RunRegCommand("add \"HKCU\\Control Panel\\Desktop\" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f");
                    RunRegCommand("add \"HKCU\\Control Panel\\Desktop\" /v HungAppTimeout /t REG_SZ /d 1000 /f");
                    RunRegCommand("add \"HKCU\\Control Panel\\Desktop\" /v AutoEndTasks /t REG_SZ /d 1 /f");
                    RunRegCommand("add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoNetCrawling /t REG_DWORD /d 1 /f");
                    RunRegCommand("add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoRemoteRecursiveEvents /t REG_DWORD /d 1 /f");
                    RunRegCommand("add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v SeparateProcess /t REG_DWORD /d 1 /f");
                }
                else
                {
                    RunRegCommand("add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 400 /f");
                    RunRegCommand("add \"HKCU\\Control Panel\\Desktop\" /v WaitToKillAppTimeout /t REG_SZ /d 5000 /f");
                    RunRegCommand("add \"HKCU\\Control Panel\\Desktop\" /v HungAppTimeout /t REG_SZ /d 5000 /f");
                    RunRegCommand("add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v SeparateProcess /t REG_DWORD /d 0 /f");
                }
                return true;
            }
            catch { return false; }
        }

        public bool IsExplorerResponsivenessActive()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false);
                return key?.GetValue("MenuShowDelay")?.ToString() == "0";
            }
            catch { return false; }
        }

        // 7. Win32 Priority Separation & Kernel Scheduler
        public bool ToggleWin32PrioritySeparation(bool enable)
        {
            try
            {
                // 26 (0x1A) = Short variable quantum, maximum foreground process boost (eSports input latency)
                // 2 = Default Windows quantum
                int val = enable ? 26 : 2;
                RunRegCommand($"add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d {val} /f");
                return true;
            }
            catch { return false; }
        }

        public bool IsWin32PrioritySeparationActive()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\PriorityControl", false);
                var val = key?.GetValue("Win32PrioritySeparation");
                return val != null && Convert.ToInt32(val) == 26;
            }
            catch { return false; }
        }

        // 8. MMCSS & Gaming Packet Throttling Disabling
        public bool ToggleMmcssGamingOptimization(bool enable)
        {
            try
            {
                if (enable)
                {
                    RunRegCommand("add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v SystemResponsiveness /t REG_DWORD /d 0 /f");
                    RunRegCommand("add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 4294967295 /f");
                    RunRegCommand("add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /t REG_DWORD /d 8 /f");
                    RunRegCommand("add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v Priority /t REG_DWORD /d 6 /f");
                    RunRegCommand("add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"Scheduling Category\" /t REG_SZ /d High /f");
                    RunRegCommand("add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"SFIO Priority\" /t REG_SZ /d High /f");
                }
                else
                {
                    RunRegCommand("add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v SystemResponsiveness /t REG_DWORD /d 20 /f");
                    RunRegCommand("add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 10 /f");
                }
                return true;
            }
            catch { return false; }
        }

        public bool IsMmcssGamingOptimizationActive()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", false);
                var val = key?.GetValue("SystemResponsiveness");
                return val != null && Convert.ToInt32(val) == 0;
            }
            catch { return false; }
        }

        // 9. Zero Startup Delay (Eliminate 10-second boot pause)
        public bool ToggleZeroStartupDelay(bool enable)
        {
            try
            {
                if (enable)
                {
                    RunRegCommand("add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize\" /v StartupDelayInMSec /t REG_DWORD /d 0 /f");
                }
                else
                {
                    RunRegCommand("delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize\" /v StartupDelayInMSec /f");
                }
                return true;
            }
            catch { return false; }
        }

        public bool IsZeroStartupDelayActive()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", false);
                var val = key?.GetValue("StartupDelayInMSec");
                return val != null && Convert.ToInt32(val) == 0;
            }
            catch { return false; }
        }

        // 10. Rebuild Icon & Thumbnail Shell Cache
        public async Task<(bool success, string msg)> RebuildIconAndThumbnailCacheAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Terminate explorer
                    RunCmdCommand("taskkill /f /im explorer.exe");
                    Task.Delay(500).Wait();

                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string iconCache = Path.Combine(localAppData, "IconCache.db");
                    if (File.Exists(iconCache)) try { File.Delete(iconCache); } catch { }

                    string thumbDir = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
                    if (Directory.Exists(thumbDir))
                    {
                        var di = new DirectoryInfo(thumbDir);
                        foreach (var f in di.EnumerateFiles("thumbcache_*.db"))
                        {
                            try { f.Delete(); } catch { }
                        }
                        foreach (var f in di.EnumerateFiles("iconcache_*.db"))
                        {
                            try { f.Delete(); } catch { }
                        }
                    }

                    // Restart explorer
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        UseShellExecute = true
                    });

                    return (true, "Кэш иконок и эскизов Проводника успешно перестроен и очищен!");
                }
                catch (Exception ex)
                {
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = true });
                    return (false, $"Ошибка очистки кэша: {ex.Message}");
                }
            });
        }
    }
}
