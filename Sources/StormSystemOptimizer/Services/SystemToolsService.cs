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
    }
}
