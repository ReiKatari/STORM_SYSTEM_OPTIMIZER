using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class SystemToolsService
    {
        private static SystemToolsService? _instance;
        public static SystemToolsService Instance => _instance ??= new SystemToolsService();

        private SystemToolsService() { }

        public async Task<bool> CreateRestorePointAsync(string description = "STORM Optimizer Snapshot")
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description '{description}' -RestorePointType 'MODIFY_SETTINGS'\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(15000);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> RunSfcScanAsync(Action<string>? onOutput = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "sfc.exe",
                        Arguments = "/scannow",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        p.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) onOutput?.Invoke(e.Data); };
                        p.BeginOutputReadLine();
                        p.WaitForExit(120000);
                        return p.ExitCode == 0;
                    }
                    return false;
                }
                catch { return false; }
            });
        }

        public async Task<bool> RunDismRestoreHealthAsync(Action<string>? onOutput = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = "/Online /Cleanup-Image /RestoreHealth",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        p.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) onOutput?.Invoke(e.Data); };
                        p.BeginOutputReadLine();
                        p.WaitForExit(180000);
                        return p.ExitCode == 0;
                    }
                    return false;
                }
                catch { return false; }
            });
        }

        public async Task<bool> CleanComponentStoreAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = "/Online /Cleanup-Image /StartComponentCleanup /ResetBase",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(60000);
                    return true;
                }
                catch { return false; }
            });
        }

        public bool RebuildIconCache()
        {
            try
            {
                string script = @"
Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Remove-Item -Force ""$env:LOCALAPPDATA\IconCache.db"" -ErrorAction SilentlyContinue
Remove-Item -Force ""$env:LOCALAPPDATA\Microsoft\Windows\Explorer\iconcache*"" -ErrorAction SilentlyContinue
Remove-Item -Force ""$env:LOCALAPPDATA\Microsoft\Windows\Explorer\thumbcache*"" -ErrorAction SilentlyContinue
Start-Process explorer.exe
";
                var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(5000);
                return true;
            }
            catch { return false; }
        }

        public bool ResetWinsock()
        {
            try
            {
                var psi = new ProcessStartInfo("netsh.exe", "winsock reset")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(3000);
                return true;
            }
            catch { return false; }
        }

        public bool ResetWindowsStore()
        {
            try
            {
                Process.Start(new ProcessStartInfo("wsreset.exe") { CreateNoWindow = true, UseShellExecute = true });
                return true;
            }
            catch { return false; }
        }

        public void LaunchSnapin(string command, string? args = null)
        {
            try
            {
                Process.Start(new ProcessStartInfo(command, args ?? "") { UseShellExecute = true });
            }
            catch { }
        }

        public async Task<bool> RunSsdTrimAsync(string driveLetter = "C:")
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "defrag.exe",
                        Arguments = $"{driveLetter} /O /U",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(30000);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public bool ActivateUltimatePerformancePlan()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(3000);

                var setPsi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "-setactive e9a42b02-d5df-448d-aa00-03f14749eb61",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var setP = Process.Start(setPsi);
                setP?.WaitForExit(3000);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool OptimizeMenuDelay()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
                if (key != null)
                {
                    key.SetValue("MenuShowDelay", "10", RegistryValueKind.String);
                    key.SetValue("WaitToKillAppTimeout", "2000", RegistryValueKind.String);
                    key.SetValue("HungAppTimeout", "1000", RegistryValueKind.String);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<System.Collections.Generic.List<SystemPortInfo>> GetActivePortsAsync()
        {
            return await Task.Run(() =>
            {
                var list = new System.Collections.Generic.List<SystemPortInfo>();
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "netstat.exe",
                        Arguments = "-ano",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(3000);

                        var procCache = new System.Collections.Generic.Dictionary<int, string>();

                        foreach (var line in output.Split('\n'))
                        {
                            var trimmed = line.Trim();
                            if (string.IsNullOrEmpty(trimmed)) continue;
                            if (trimmed.StartsWith("Proto", StringComparison.OrdinalIgnoreCase)) continue;
                            if (trimmed.StartsWith("Active", StringComparison.OrdinalIgnoreCase)) continue;

                            var parts = System.Text.RegularExpressions.Regex.Split(trimmed, @"\s+");
                            if (parts.Length >= 4)
                            {
                                string proto = parts[0].ToUpperInvariant();
                                string local = parts[1];
                                string foreign = parts[2];
                                string state = parts.Length >= 5 ? parts[3] : "LISTENING";
                                string pidStr = parts.Length >= 5 ? parts[4] : parts[3];

                                int port = 0;
                                int lastColon = local.LastIndexOf(':');
                                if (lastColon >= 0 && int.TryParse(local.Substring(lastColon + 1), out int parsedPort))
                                {
                                    port = parsedPort;
                                }

                                if (int.TryParse(pidStr, out int pid))
                                {
                                    if (!procCache.TryGetValue(pid, out var pName))
                                    {
                                        try
                                        {
                                            if (pid == 0) pName = "System Idle";
                                            else if (pid == 4) pName = "System Kernel";
                                            else pName = Process.GetProcessById(pid).ProcessName;
                                        }
                                        catch
                                        {
                                            pName = "Служба Windows";
                                        }
                                        procCache[pid] = pName;
                                    }

                                    list.Add(new SystemPortInfo
                                    {
                                        Protocol = proto,
                                        LocalAddress = local,
                                        LocalPort = port,
                                        ForeignAddress = foreign,
                                        State = state,
                                        ProcessId = pid,
                                        ProcessName = pName
                                    });
                                }
                            }
                        }
                    }
                }
                catch { }
                return list;
            });
        }

        public bool KillProcessByPid(int pid)
        {
            try
            {
                if (pid <= 4) return false;
                var proc = Process.GetProcessById(pid);
                proc.Kill(true);
                return true;
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo("taskkill.exe", $"/F /PID {pid}") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(2000);
                    return true;
                }
                catch { return false; }
            }
        }

        public async Task<bool> FlushDnsArpIpStackAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("ipconfig.exe", "/flushdns") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(2000);
                    Process.Start(new ProcessStartInfo("netsh.exe", "winsock reset") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(2000);
                    Process.Start(new ProcessStartInfo("netsh.exe", "int ip reset") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(2000);
                    Process.Start(new ProcessStartInfo("arp.exe", "-d *") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(2000);
                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> ClearPrintSpoolerQueueAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("net.exe", "stop spooler") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(3000);
                    string spoolDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\spool\PRINTERS");
                    if (Directory.Exists(spoolDir))
                    {
                        foreach (var f in Directory.GetFiles(spoolDir))
                        {
                            try { File.Delete(f); } catch { }
                        }
                    }
                    Process.Start(new ProcessStartInfo("net.exe", "start spooler") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(3000);
                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> ResyncSystemClockAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("net.exe", "start w32time") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(2000);
                    Process.Start(new ProcessStartInfo("w32tm.exe", "/resync /force") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(3000);
                    return true;
                }
                catch { return false; }
            });
        }
    }

    public class SystemPortInfo
    {
        public string Protocol { get; set; } = "TCP";
        public string LocalAddress { get; set; } = "0.0.0.0";
        public int LocalPort { get; set; }
        public string ForeignAddress { get; set; } = "*:*";
        public string State { get; set; } = "LISTENING";
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "Неизвестно";
    }
}
