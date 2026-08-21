using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class PowerTunerService
    {
        private static PowerTunerService? _instance;
        public static PowerTunerService Instance => _instance ??= new PowerTunerService();

        private PowerTunerService() { }

        public string GetActivePowerSchemeName()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "/getactivescheme",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(2000);
                    if (output.Contains("STORM", StringComparison.OrdinalIgnoreCase)) return "STORM ULTIMATE PLAN";
                    if (output.Contains("Ultimate", StringComparison.OrdinalIgnoreCase)) return "STORM ULTIMATE PLAN";
                    if (output.Contains("High", StringComparison.OrdinalIgnoreCase) || output.Contains("Высокая", StringComparison.OrdinalIgnoreCase)) return "Высокая производительность";
                    if (output.Contains("Balanced", StringComparison.OrdinalIgnoreCase) || output.Contains("Сбалансированная", StringComparison.OrdinalIgnoreCase)) return "Сбалансированная";
                    return "Активная схема Windows";
                }
            }
            catch { }
            return "Сбалансированная";
        }

        public bool IsCoreParkingDisabled()
        {
            try
            {
                const string powerKeyPath = @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583";
                using var key = Registry.LocalMachine.OpenSubKey(powerKeyPath);
                if (key != null)
                {
                    var valMax = key.GetValue("ValueMax");
                    if (valMax is int v && v == 0) return true;
                }

                string scheme = GetActivePowerSchemeName();
                if (scheme.Contains("STORM", StringComparison.OrdinalIgnoreCase) || scheme.Contains("Ultimate", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { }
            return false;
        }

        public async Task<bool> ActivateStormUltimatePowerPlanAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 1. Duplicate Ultimate Performance Scheme GUID e9a42b02-d5df-448d-aa00-03f14749eb61
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    string output = string.Empty;
                    if (p != null)
                    {
                        output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(3000);
                    }

                    // Extract GUID
                    var match = System.Text.RegularExpressions.Regex.Match(output, @"([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    string targetGuid = match.Success ? match.Groups[1].Value : "e9a42b02-d5df-448d-aa00-03f14749eb61";

                    // Rename scheme to STORM ULTIMATE PLAN
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = $"-changename {targetGuid} \"STORM ULTIMATE PLAN\" \"План максимальной производительности STORM без троттлинга и задержек\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(2000);

                    // Set Active
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = $"/setactive {targetGuid}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(2000);

                    // 2. Disable Core Parking (100% min/max processor state on AC and DC)
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = $"/setacvalueindex {targetGuid} SUB_PROCESSOR CPMINCORES 100",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(1000);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = $"/setacvalueindex {targetGuid} SUB_PROCESSOR CPMAXCORES 100",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(1000);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = $"/setacvalueindex {targetGuid} SUB_PROCESSOR PROCTHROTTLEMIN 100",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(1000);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = $"/setacvalueindex {targetGuid} SUB_PROCESSOR PROCTHROTTLEMAX 100",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(1000);

                    // Disable idle sleep for PCIe / Disks
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = $"/setacvalueindex {targetGuid} SUB_DISK DISKIDLE 0",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(1000);

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> ActivateBalancedPowerPlanAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Balanced GUID: 381b4222-f694-41f0-9685-ff5bb260df2e
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(2000);
                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> ApplyCoreParkingDisableTweaksAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Registry tweaks for unparking all cores across all power attributes
                    const string powerKeyPath = @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583";
                    using var key = Registry.LocalMachine.OpenSubKey(powerKeyPath, true);
                    if (key != null)
                    {
                        key.SetValue("ValueMin", 0, RegistryValueKind.DWord);
                        key.SetValue("ValueMax", 0, RegistryValueKind.DWord);
                        key.SetValue("Attributes", 2, RegistryValueKind.DWord);
                    }

                    // Apply to current active scheme
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(1000);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/setactive SCHEME_CURRENT",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(1000);

                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> ApplyCStatesTweaksAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Tweak processor throttle and idle transition thresholds
                    using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\5d76a269-7444-4814-a82e-eb03a2b3b6cb");
                    if (key != null)
                    {
                        key.SetValue("Attributes", 2, RegistryValueKind.DWord);
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 100",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(1000);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/setactive SCHEME_CURRENT",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(1000);

                    return true;
                }
                catch { return false; }
            });
        }
    }
}
