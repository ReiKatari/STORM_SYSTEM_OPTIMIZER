using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
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
                catch { return false; }
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
                foreach (var p in Process.GetProcessesByName("explorer"))
                {
                    try { p.Kill(); p.WaitForExit(1000); } catch { }
                }

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string iconDb = Path.Combine(localApp, "IconCache.db");
                if (File.Exists(iconDb)) File.Delete(iconDb);

                string explorerCache = Path.Combine(localApp, @"Microsoft\Windows\Explorer");
                if (Directory.Exists(explorerCache))
                {
                    foreach (var f in Directory.GetFiles(explorerCache, "iconcache_*.db"))
                    {
                        try { File.Delete(f); } catch { }
                    }
                }

                Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
                return true;
            }
            catch { return false; }
        }

        public bool ResetWinsock()
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo("netsh.exe", "winsock reset") { CreateNoWindow = true, UseShellExecute = false });
                p?.WaitForExit(3000);
                return p?.ExitCode == 0;
            }
            catch { return false; }
        }

        public bool ResetWindowsStore()
        {
            try
            {
                Process.Start(new ProcessStartInfo("wsreset.exe") { UseShellExecute = true });
                return true;
            }
            catch { return false; }
        }

        public async Task<List<SystemPortInfo>> GetActivePortsAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<SystemPortInfo>();
                try
                {
                    using var p = Process.Start(new ProcessStartInfo
                    {
                        FileName = "netstat.exe",
                        Arguments = "-ano",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    });

                    if (p != null)
                    {
                        string outStr = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(3000);

                        using var reader = new StringReader(outStr);
                        string? line;
                        var procCache = new Dictionary<int, string>();

                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (string.IsNullOrEmpty(line)) continue;

                            var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (tokens.Length >= 4 && (tokens[0] == "TCP" || tokens[0] == "UDP"))
                            {
                                string proto = tokens[0];
                                string local = tokens[1];
                                string foreign = tokens[2];
                                string state = proto == "TCP" && tokens.Length >= 5 ? tokens[3] : "LISTENING";
                                string pidStr = proto == "TCP" && tokens.Length >= 5 ? tokens[4] : tokens[3];

                                if (int.TryParse(pidStr, out int pid))
                                {
                                    if (!procCache.TryGetValue(pid, out var pName))
                                    {
                                        try
                                        {
                                            if (pid == 0) pName = "Системный простой";
                                            else if (pid == 4) pName = "Ядро Windows (System)";
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

        public static bool LaunchAsTrustedInstaller(string targetPath, string arguments = "")
        {
            try
            {
                if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath)) return false;

                using (var sc = Process.Start(new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = "start TrustedInstaller",
                    CreateNoWindow = true,
                    UseShellExecute = false
                }))
                {
                    sc?.WaitForExit(3000);
                }

                string safePath = targetPath.Replace("'", "''");
                string safeArgs = arguments.Replace("'", "''");
                string psScript = "$p = Get-Process -Name 'TrustedInstaller' -ErrorAction SilentlyContinue | Select-Object -First 1; Start-Process -FilePath '" + safePath + "' -ArgumentList '" + safeArgs + "' -Verb RunAs";

                using var ps = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + psScript + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                ps?.WaitForExit(4000);
                return true;
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetPath,
                        Arguments = arguments,
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                    return true;
                }
                catch { return false; }
            }
        }

        public async Task<bool> InstallRuntimeAsync(string packageKey, Action<string>? progressCallback = null)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), "StormRuntimes");
                    if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                    using var http = new HttpClient();
                    http.Timeout = TimeSpan.FromMinutes(3);

                    if (packageKey == "VCRedist")
                    {
                        progressCallback?.Invoke("Скачивание Visual C++ 2015-2022 Redistributable...");
                        string installer = Path.Combine(tempDir, "VC_redist.x64.exe");
                        var data = await http.GetByteArrayAsync("https://aka.ms/vs/17/release/vc_redist.x64.exe");
                        await File.WriteAllBytesAsync(installer, data);

                        progressCallback?.Invoke("Тихая установка Visual C++...");
                        using var p = Process.Start(new ProcessStartInfo(installer, "/quiet /norestart") { CreateNoWindow = true, UseShellExecute = false });
                        if (p != null) await p.WaitForExitAsync();

                        progressCallback?.Invoke("Пакеты Visual C++ успешно установлены!");
                        return true;
                    }
                    else if (packageKey == "DirectX")
                    {
                        progressCallback?.Invoke("Скачивание среды выполнения DirectX...");
                        string installer = Path.Combine(tempDir, "dxwebsetup.exe");
                        var data = await http.GetByteArrayAsync("https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E18C4C/dxwebsetup.exe");
                        await File.WriteAllBytesAsync(installer, data);

                        progressCallback?.Invoke("Тихая установка компонентов DirectX...");
                        using var p = Process.Start(new ProcessStartInfo(installer, "/Q") { CreateNoWindow = true, UseShellExecute = false });
                        if (p != null) await p.WaitForExitAsync();

                        progressCallback?.Invoke("Среда DirectX успешно обновлена!");
                        return true;
                    }
                    else if (packageKey == "DotNet")
                    {
                        progressCallback?.Invoke("Установка среды выполнения .NET 8...");
                        using var p = Process.Start(new ProcessStartInfo
                        {
                            FileName = "winget.exe",
                            Arguments = "install --id Microsoft.DotNet.DesktopRuntime.8 --silent --accept-package-agreements --accept-source-agreements",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                        if (p != null) await p.WaitForExitAsync();

                        progressCallback?.Invoke("Среда выполнения .NET успешно установлена!");
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    progressCallback?.Invoke($"Ошибка установки: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> ToggleWindowsFeatureAsync(string featureKey, bool enable)
        {
            return await Task.Run(async () =>
            {
                string featureName = featureKey switch
                {
                    "Sandbox" => "Containers-DisposableClientVM",
                    "HyperV" => "Microsoft-Hyper-V-All",
                    "WSL" => "Microsoft-Windows-Subsystem-Linux",
                    "DirectPlay" => "DirectPlay",
                    _ => featureKey
                };

                string action = enable ? "/enable-feature" : "/disable-feature";
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = $"/online {action} /featurename:{featureName} /norestart /all",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                if (p != null)
                {
                    await p.WaitForExitAsync();
                    return p.ExitCode == 0;
                }
                return false;
            });
        }

        public void LaunchSnapin(string snapinOrTool)
        {
            try
            {
                Process.Start(new ProcessStartInfo(snapinOrTool) { UseShellExecute = true });
            }
            catch { }
        }

        public async Task<bool> RunSsdTrimAsync(string driveLetter = "C:")
        {
            return await Task.Run(() =>
            {
                try
                {
                    string drive = (string.IsNullOrWhiteSpace(driveLetter) ? "C" : driveLetter).TrimEnd('\\', ':') + ":";
                    using var p = Process.Start(new ProcessStartInfo
                    {
                        FileName = "defrag.exe",
                        Arguments = $"{drive} /L /O",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    p?.WaitForExit(30000);
                    return true;
                }
                catch { return false; }
            });
        }

        public bool ActivateUltimatePerformancePlan()
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                p?.WaitForExit(3000);

                using var pSet = Process.Start(new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "/setactive e9a42b02-d5df-448d-aa00-03f14749eb61",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                pSet?.WaitForExit(3000);
                return true;
            }
            catch { return false; }
        }

        public bool OptimizeMenuDelay()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
                key?.SetValue("MenuShowDelay", "0", RegistryValueKind.String);
                return true;
            }
            catch { return false; }
        }
    }

    public class SystemPortInfo
    {
        public string Protocol { get; set; } = "TCP";
        public string LocalAddress { get; set; } = "0.0.0.0";
        public string ForeignAddress { get; set; } = "*:*";
        public string State { get; set; } = "LISTENING";
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "Неизвестно";
    }
}
