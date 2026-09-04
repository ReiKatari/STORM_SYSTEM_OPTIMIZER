using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class PowerSchemeStatus
    {
        public bool IsCoreParkingDisabled { get; set; }
        public bool IsCStatesOptimized { get; set; }
        public bool IsEppMaxPerformance { get; set; }
        public bool IsHeteroSchedulingActive { get; set; }
        public bool IsPcieAspmDisabled { get; set; }
        public bool IsProcessorBoostAggressive { get; set; }
        public bool IsSystemResponsivenessGaming { get; set; }
        public bool IsGpuMaxPerformance { get; set; }
        public bool IsUsbSelectiveSuspendDisabled { get; set; }
        public bool AreHiddenAttributesUnlocked { get; set; }
    }

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

        public PowerSchemeStatus QueryActivePowerSchemeStatus()
        {
            var status = new PowerSchemeStatus();
            try
            {
                // 1. System Responsiveness & Gaming profile
                try
                {
                    using var sp = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile");
                    if (sp != null)
                    {
                        var resp = sp.GetValue("SystemResponsiveness");
                        var net = sp.GetValue("NetworkThrottlingIndex");
                        if (resp is int r && r == 0 && (net is int n && (n == -1 || unchecked((uint)n) == 0xFFFFFFFF)))
                        {
                            status.IsSystemResponsivenessGaming = true;
                        }
                    }
                }
                catch { }

                // 2. GPU Max Performance & HAGS
                try
                {
                    using var gfx = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers");
                    if (gfx != null)
                    {
                        var hwSch = gfx.GetValue("HwSchMode");
                        var tdr = gfx.GetValue("TdrDelay");
                        if (hwSch is int h && h == 2 && tdr is int t && t >= 8)
                        {
                            status.IsGpuMaxPerformance = true;
                        }
                    }
                }
                catch { }

                // 3. Hidden Power Attributes
                try
                {
                    using var attrKey1 = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583");
                    using var attrKey2 = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\5d76a269-7444-4814-a82e-eb03a2b3b6cb");
                    if (attrKey1?.GetValue("Attributes") is int a1 && a1 == 2 && attrKey2?.GetValue("Attributes") is int a2 && a2 == 2)
                    {
                        status.AreHiddenAttributesUnlocked = true;
                    }
                }
                catch { }

                // 4. Core Parking registry check
                try
                {
                    using var parkKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583");
                    if (parkKey?.GetValue("ValueMax") is int v && v == 0)
                    {
                        status.IsCoreParkingDisabled = true;
                    }
                }
                catch { }

                // 5. Query active powercfg settings
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "/q SCHEME_CURRENT",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);

                    var settingValues = ParsePowerCfgQuery(output);

                    // CPMINCORES: 0cc5b647-c1df-4637-891a-dec35c318583 (100)
                    if (settingValues.TryGetValue("0cc5b647-c1df-4637-891a-dec35c318583", out int cpVal) && cpVal == 100)
                    {
                        status.IsCoreParkingDisabled = true;
                    }

                    // PROCTHROTTLEMIN: 893dee8e-2bef-41e0-89c6-b55d0929964c (100)
                    if (settingValues.TryGetValue("893dee8e-2bef-41e0-89c6-b55d0929964c", out int throttleMin) && throttleMin == 100)
                    {
                        status.IsCStatesOptimized = true;
                    }

                    // PERFEPP: 36687f9e-e376-4514-b166-73136440792f (0)
                    if (settingValues.TryGetValue("36687f9e-e376-4514-b166-73136440792f", out int eppVal) && eppVal == 0)
                    {
                        status.IsEppMaxPerformance = true;
                    }

                    // Hetero Scheduling: 93b8b6dc-0646-4d39-92a2-076e0f9ac606 (0)
                    if (settingValues.TryGetValue("93b8b6dc-0646-4d39-92a2-076e0f9ac606", out int heteroVal) && heteroVal == 0)
                    {
                        status.IsHeteroSchedulingActive = true;
                    }

                    // PCIe ASPM: 0012ee47-9041-4b5d-9b77-535fba8b1442 (0) or ee12f906-d277-404b-b6da-f5761a545605 (0)
                    if ((settingValues.TryGetValue("0012ee47-9041-4b5d-9b77-535fba8b1442", out int aspmVal) && aspmVal == 0) ||
                        (settingValues.TryGetValue("ee12f906-d277-404b-b6da-f5761a545605", out int aspmVal2) && aspmVal2 == 0))
                    {
                        status.IsPcieAspmDisabled = true;
                    }

                    // Boost Mode: be337238-0d82-4146-a960-4f3749d470c7 (2)
                    if (settingValues.TryGetValue("be337238-0d82-4146-a960-4f3749d470c7", out int boostVal) && boostVal == 2)
                    {
                        status.IsProcessorBoostAggressive = true;
                    }

                    // USB Selective Suspend: 48e6b63a-08b2-4590-80d6-6637f8138009 (0)
                    if (settingValues.TryGetValue("48e6b63a-08b2-4590-80d6-6637f8138009", out int usbVal) && usbVal == 0)
                    {
                        status.IsUsbSelectiveSuspendDisabled = true;
                    }
                }
            }
            catch { }

            return status;
        }

        private static System.Collections.Generic.Dictionary<string, int> ParsePowerCfgQuery(string output)
        {
            var dict = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(output)) return dict;

            string currentGuid = string.Empty;
            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("GUID настройки питания:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("Power Setting GUID:", StringComparison.OrdinalIgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        currentGuid = match.Groups[1].Value.ToLowerInvariant();
                    }
                }
                else if (!string.IsNullOrEmpty(currentGuid) &&
                         (line.Contains("от сети", StringComparison.OrdinalIgnoreCase) || line.Contains("Current AC", StringComparison.OrdinalIgnoreCase)) &&
                         line.Contains("0x", StringComparison.OrdinalIgnoreCase))
                {
                    int idx = line.IndexOf("0x", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        string hexPart = line.Substring(idx + 2).Trim();
                        int space = hexPart.IndexOf(' ');
                        if (space > 0) hexPart = hexPart.Substring(0, space);
                        if (int.TryParse(hexPart, System.Globalization.NumberStyles.HexNumber, null, out int val))
                        {
                            dict[currentGuid] = val;
                        }
                    }
                }
            }
            return dict;
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

        public async Task<bool> ApplyEnergyPerformancePreferenceEppAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Energy Performance Preference = 0 (Speed Shift / CPPC 100% responsiveness)
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFEPP 0",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(1000);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFEPP1 0",
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

        public async Task<bool> ApplyHeteroSchedulingPolicyAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Heterogeneous thread scheduling: Prefer high performance cores (P-Cores) for game threads
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/setacvalueindex SCHEME_CURRENT SUB_PROCESSOR 93b8b6dc-0646-4d39-92a2-076e0f9ac606 0",
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

        public async Task<bool> ApplyPcieAspmDisableAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // PCIe Link State Power Management = Off (0)
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/setacvalueindex SCHEME_CURRENT SUB_PCIEXPRESS 0012ee47-9041-4b5d-9b77-535fba8b1442 0",
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

        public async Task<bool> ApplyProcessorBoostModeAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Processor Performance Boost Mode = 2 (Aggressive)
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/setacvalueindex SCHEME_CURRENT SUB_PROCESSOR be337238-0d82-4146-a960-4f3749d470c7 2",
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

        public async Task<bool> ApplySystemResponsivenessMultimediaTweakAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var sp = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                    {
                        sp?.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                        sp?.SetValue("NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord);
                        sp?.SetValue("NoLazyMode", 1, RegistryValueKind.DWord);
                    }

                    using (var games = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"))
                    {
                        games?.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                        games?.SetValue("Priority", 6, RegistryValueKind.DWord);
                        games?.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                        games?.SetValue("SFIO Priority", "High", RegistryValueKind.String);
                    }

                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> ApplyGpuMaximumPerformancePowerPolicyAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Force HAGS (Hardware-accelerated GPU Scheduling)
                    using (var gfx = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                    {
                        gfx?.SetValue("HwSchMode", 2, RegistryValueKind.DWord);
                    }

                    // TDR Delay & TDR DdiDelay to prevent false GPU driver resets in intense rendering
                    using (var gfx = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                    {
                        gfx?.SetValue("TdrDelay", 8, RegistryValueKind.DWord);
                        gfx?.SetValue("TdrDdiDelay", 8, RegistryValueKind.DWord);
                    }

                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> ApplyUsbSelectiveSuspendDisableAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Disable USB Selective Suspend in Power Scheme
                    // SUB_USB: 2a737441-1930-4402-8d77-b2bebba4d5a0, USBSELECT: 48e6b63a-08b2-4590-80d6-6637f8138009
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/setacvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba4d5a0 48e6b63a-08b2-4590-80d6-6637f8138009 0",
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

        public async Task<bool> UnlockAllHiddenPowerSchemeAttributesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    const string rootPower = @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings";
                    using var rootKey = Registry.LocalMachine.OpenSubKey(rootPower, true);
                    if (rootKey != null)
                    {
                        foreach (var subGroup in rootKey.GetSubKeyNames())
                        {
                            using var groupKey = rootKey.OpenSubKey(subGroup, true);
                            if (groupKey != null)
                            {
                                foreach (var setting in groupKey.GetSubKeyNames())
                                {
                                    using var settingKey = groupKey.OpenSubKey(setting, true);
                                    settingKey?.SetValue("Attributes", 2, RegistryValueKind.DWord);
                                }
                            }
                        }
                    }
                    return true;
                }
                catch { return false; }
            });
        }
    }
}
