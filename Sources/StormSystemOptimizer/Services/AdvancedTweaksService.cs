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

        // 1. MSI Mode (Message Signaled Interrupts) for GPU and USB Controllers
        public bool EnableMsiModeForGpuAndUsb()
        {
            try
            {
                // Scan PCI devices in Registry
                using var pciKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI", true);
                if (pciKey != null)
                {
                    foreach (var vendorSubName in pciKey.GetSubKeyNames())
                    {
                        using var vendorKey = pciKey.OpenSubKey(vendorSubName, true);
                        if (vendorKey == null) continue;

                        foreach (var devInstanceName in vendorKey.GetSubKeyNames())
                        {
                            using var devKey = vendorKey.OpenSubKey(devInstanceName, true);
                            if (devKey == null) continue;

                            string classGuid = devKey.GetValue("ClassGUID")?.ToString() ?? "";
                            string desc = devKey.GetValue("DeviceDesc")?.ToString() ?? "";

                            // Display Adapters GUID {4d36e968-e325-11ce-bfc1-08002be10318} or USB {36fc9e60-c465-11cf-8056-444553540000}
                            bool isGpu = classGuid.Equals("{4d36e968-e325-11ce-bfc1-08002be10318}", StringComparison.OrdinalIgnoreCase) || desc.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || desc.Contains("Radeon", StringComparison.OrdinalIgnoreCase);
                            bool isUsb = classGuid.Equals("{36fc9e60-c465-11cf-8056-444553540000}", StringComparison.OrdinalIgnoreCase) || desc.Contains("xHCI", StringComparison.OrdinalIgnoreCase) || desc.Contains("USB", StringComparison.OrdinalIgnoreCase);

                            if (isGpu || isUsb)
                            {
                                using var msiKey = devKey.CreateSubKey(@"Device Parameters\Interrupt Management\MessageSignaledInterruptProperties");
                                if (msiKey != null)
                                {
                                    msiKey.SetValue("MSISupported", 1, RegistryValueKind.DWord);
                                    if (isGpu)
                                    {
                                        using var affKey = devKey.CreateSubKey(@"Device Parameters\Interrupt Management\Affinity Policy");
                                        affKey?.SetValue("DevicePriority", 3, RegistryValueKind.DWord); // High
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 2. DirectStorage 1.2 & NVMe Bypass Tuning
        public bool OptimizeDirectStorageAndIoRing()
        {
            try
            {
                using var fsKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem");
                if (fsKey != null)
                {
                    fsKey.SetValue("NtfsDisable8dot3NameCreation", 1, RegistryValueKind.DWord);
                    fsKey.SetValue("NtfsMemoryUsage", 2, RegistryValueKind.DWord);
                    fsKey.SetValue("Win32IoRingFlags", 1, RegistryValueKind.DWord);
                }

                using var storKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Storage");
                if (storKey != null)
                {
                    storKey.SetValue("BypassIoAllowed", 1, RegistryValueKind.DWord);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 3. TCP NoDelay & Nagle's Algorithm Disabling (Reduce Online Ping)
        public bool DisableNaglesAlgorithm()
        {
            try
            {
                using var netKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", true);
                if (netKey != null)
                {
                    foreach (var adapterGuid in netKey.GetSubKeyNames())
                    {
                        using var adapterKey = netKey.OpenSubKey(adapterGuid, true);
                        if (adapterKey != null)
                        {
                            adapterKey.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                            adapterKey.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                            adapterKey.SetValue("TcpDelAckTicks", 0, RegistryValueKind.DWord);
                        }
                    }
                }

                using var tcpParams = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters");
                if (tcpParams != null)
                {
                    tcpParams.SetValue("DefaultTTL", 64, RegistryValueKind.DWord);
                    tcpParams.SetValue("EnableTCPA", 1, RegistryValueKind.DWord);
                    tcpParams.SetValue("EnableWsd", 0, RegistryValueKind.DWord);
                }

                return true;
            }
            catch
            {
                return false;
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
                            string blockedDomains = $"\n{markerStart}\n" +
                                "0.0.0.0 telecommand.telemetry.microsoft.com\n" +
                                "0.0.0.0 vortex.data.microsoft.com\n" +
                                "0.0.0.0 vortex-win.data.microsoft.com\n" +
                                "0.0.0.0 telemetry.microsoft.com\n" +
                                "0.0.0.0 diagtrack.telemetry.microsoft.com\n" +
                                "0.0.0.0 watson.telemetry.microsoft.com\n" +
                                "0.0.0.0 settings-win.data.microsoft.com\n" +
                                "0.0.0.0 feedback.windows.com\n" +
                                $"{markerEnd}\n";

                            currentHosts += blockedDomains;
                        }

                        File.WriteAllText(hostsPath, currentHosts);
                        NativeMethods.DnsFlushResolverCache();
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        // 5. Snapshot & Rollback Engine: Backup registry keys before tweaks
        public async Task<string> CreateRegistryBackupSnapshotAsync(string snapshotName = "PreOptimizationSnapshot")
        {
            return await Task.Run(() =>
            {
                try
                {
                    string backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StormSystemOptimizer", "Backups");
                    Directory.CreateDirectory(backupDir);

                    string fileName = $"{snapshotName}_{DateTime.Now:yyyyMMdd_HHmmss}.reg";
                    string fullPath = Path.Combine(backupDir, fileName);

                    // Export critical performance branches
                    var psi = new ProcessStartInfo("reg.exe", $"export \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\" \"{fullPath}\" /y")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(4000);

                    return fullPath;
                }
                catch (Exception ex)
                {
                    return $"Ошибка бэкапа: {ex.Message}";
                }
            });
        }
    }
}
