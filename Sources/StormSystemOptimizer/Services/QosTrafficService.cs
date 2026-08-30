using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class GameQosProfile
    {
        public string GameName { get; set; } = string.Empty;
        public string ExecutableName { get; set; } = string.Empty;
        public int DscpValue { get; set; } = 46; // Expedited Forwarding (Highest Priority)
        public bool IsEnabled { get; set; }
    }

    public class QosTrafficService
    {
        private static QosTrafficService? _instance;
        public static QosTrafficService Instance => _instance ??= new QosTrafficService();

        private const string QosPolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\QoS";
        private const string TcpipQosKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\QoS";

        public List<GameQosProfile> DefaultGameProfiles => new()
        {
            new GameQosProfile { GameName = "Counter-Strike 2", ExecutableName = "cs2.exe", DscpValue = 46 },
            new GameQosProfile { GameName = "Valorant", ExecutableName = "VALORANT-Win64-Shipping.exe", DscpValue = 46 },
            new GameQosProfile { GameName = "Dota 2", ExecutableName = "dota2.exe", DscpValue = 46 },
            new GameQosProfile { GameName = "Apex Legends", ExecutableName = "r5apex.exe", DscpValue = 46 },
            new GameQosProfile { GameName = "PUBG: BATTLEGROUNDS", ExecutableName = "TslGame.exe", DscpValue = 46 },
            new GameQosProfile { GameName = "Fortnite", ExecutableName = "FortniteClient-Win64-Shipping.exe", DscpValue = 46 },
            new GameQosProfile { GameName = "Call of Duty / Warzone", ExecutableName = "cod.exe", DscpValue = 46 },
            new GameQosProfile { GameName = "Overwatch 2", ExecutableName = "Overwatch.exe", DscpValue = 46 },
            new GameQosProfile { GameName = "Rainbow Six Siege", ExecutableName = "RainbowSix.exe", DscpValue = 46 },
            new GameQosProfile { GameName = "Rust", ExecutableName = "RustClient.exe", DscpValue = 46 }
        };

        public async Task<List<GameQosProfile>> GetGameQosProfilesAsync()
        {
            return await Task.Run(() =>
            {
                var profiles = DefaultGameProfiles;
                try
                {
                    using var qosKey = Registry.LocalMachine.OpenSubKey(QosPolicyKey);
                    if (qosKey != null)
                    {
                        foreach (var subKeyName in qosKey.GetSubKeyNames())
                        {
                            using var policyKey = qosKey.OpenSubKey(subKeyName);
                            if (policyKey == null) continue;

                            string appName = policyKey.GetValue("Application Name") as string ?? string.Empty;
                            var match = profiles.Find(p => p.ExecutableName.Equals(appName, StringComparison.OrdinalIgnoreCase));
                            if (match != null)
                            {
                                match.IsEnabled = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[QosTrafficService] GetGameQosProfiles Error: {ex.Message}");
                }
                return profiles;
            });
        }

        /// <summary>
        /// Enables or disables QoS DSCP 46 Expedited Forwarding priority for a game executable.
        /// </summary>
        public async Task<bool> SetGameQosPolicyAsync(string gameName, string exeName, bool enable, int dscpValue = 46)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Enable QoS without NLA domain requirement
                    using (var tcpKey = Registry.LocalMachine.CreateSubKey(TcpipQosKey))
                    {
                        if (tcpKey != null)
                        {
                            tcpKey.SetValue("Do not use NLA", "1", RegistryValueKind.String);
                        }
                    }

                    string policySubPath = $@"{QosPolicyKey}\STORM_QoS_{exeName}";

                    if (enable)
                    {
                        using var polKey = Registry.LocalMachine.CreateSubKey(policySubPath);
                        if (polKey != null)
                        {
                            polKey.SetValue("Version", "1.0", RegistryValueKind.String);
                            polKey.SetValue("Application Name", exeName, RegistryValueKind.String);
                            polKey.SetValue("Protocol", "*", RegistryValueKind.String);
                            polKey.SetValue("Local IP", "*", RegistryValueKind.String);
                            polKey.SetValue("Local Port", "*", RegistryValueKind.String);
                            polKey.SetValue("Remote IP", "*", RegistryValueKind.String);
                            polKey.SetValue("Remote Port", "*", RegistryValueKind.String);
                            polKey.SetValue("DSCP Value", dscpValue.ToString(), RegistryValueKind.String);
                            polKey.SetValue("Throttle Rate", "-1", RegistryValueKind.String);
                        }
                    }
                    else
                    {
                        Registry.LocalMachine.DeleteSubKeyTree(policySubPath, throwOnMissingSubKey: false);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[QosTrafficService] SetGameQosPolicy Error: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Applies DSCP 46 to all supported games in one click.
        /// </summary>
        public async Task<bool> ApplyAllGamesQosAsync()
        {
            bool allOk = true;
            foreach (var g in DefaultGameProfiles)
            {
                bool res = await SetGameQosPolicyAsync(g.GameName, g.ExecutableName, true, 46);
                if (!res) allOk = false;
            }
            return allOk;
        }
    }
}
