using System;
using System.Diagnostics;
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
                // Create / duplicate Ultimate Performance GUID
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(3000);

                // Set active
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
