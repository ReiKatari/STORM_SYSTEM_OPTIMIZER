using System;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class PrivacyOptimizerService
    {
        private static PrivacyOptimizerService? _instance;
        public static PrivacyOptimizerService Instance => _instance ??= new PrivacyOptimizerService();

        private PrivacyOptimizerService() { }

        // 1. Diagnostic Telemetry
        public bool DisableTelemetry() => SetTelemetry(true);
        public bool EnableTelemetry() => SetTelemetry(false);

        public bool SetTelemetry(bool disable)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
                key?.SetValue("AllowTelemetry", disable ? 0 : 1, RegistryValueKind.DWord);
                key?.SetValue("DoNotShowFeedbackNotifications", disable ? 1 : 0, RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        // 2. Advertising ID
        public bool SetAdvertisingId(bool disable)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"))
                {
                    key?.SetValue("Enabled", disable ? 0 : 1, RegistryValueKind.DWord);
                }
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo"))
                {
                    key?.SetValue("DisabledByGroupPolicy", disable ? 1 : 0, RegistryValueKind.DWord);
                }
                return true;
            }
            catch { return false; }
        }

        // 3. Activity Feed / Timeline
        public bool SetActivityFeed(bool disable)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System");
                key?.SetValue("EnableActivityFeed", disable ? 0 : 1, RegistryValueKind.DWord);
                key?.SetValue("PublishUserActivities", disable ? 0 : 1, RegistryValueKind.DWord);
                key?.SetValue("UploadUserActivities", disable ? 0 : 1, RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        // 4. Input Telemetry / Keylogging
        public bool SetInputTelemetry(bool disable)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\InputPersonalization"))
                {
                    key?.SetValue("RestrictImplicitInkCollection", disable ? 1 : 0, RegistryValueKind.DWord);
                    key?.SetValue("RestrictImplicitTextCollection", disable ? 1 : 0, RegistryValueKind.DWord);
                }
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\InputPersonalization\TrainedDataStore"))
                {
                    key?.SetValue("HarvestContacts", disable ? 0 : 1, RegistryValueKind.DWord);
                }
                return true;
            }
            catch { return false; }
        }

        // 5. Bing Start Search
        public bool SetBingStartSearch(bool disable)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer");
                key?.SetValue("DisableSearchBoxSuggestions", disable ? 1 : 0, RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        // 6. Edge Browser Telemetry
        public bool SetEdgeTelemetry(bool disable)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Edge");
                key?.SetValue("MetricsReportingEnabled", disable ? 0 : 1, RegistryValueKind.DWord);
                key?.SetValue("PersonalizationReportingEnabled", disable ? 0 : 1, RegistryValueKind.DWord);
                key?.SetValue("SendSiteInfoToImproveServices", disable ? 0 : 1, RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        // 7. Location Sensor
        public bool SetLocationSensors(bool disable)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors");
                key?.SetValue("DisableLocation", disable ? 1 : 0, RegistryValueKind.DWord);
                key?.SetValue("DisableLocationScripting", disable ? 1 : 0, RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        // 8. Feedback Frequency
        public bool SetFeedbackFrequency(bool disable)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Siuf\Rules");
                key?.SetValue("NumberOfSIUFInPeriod", disable ? 0 : 1, RegistryValueKind.DWord);
                key?.SetValue("PeriodInNanoSeconds", disable ? 0 : 1, RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        // 9. Cortana and Copilot
        public bool SetCortanaCopilot(bool disable)
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"))
                {
                    key?.SetValue("AllowCortana", disable ? 0 : 1, RegistryValueKind.DWord);
                }
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\WindowsCopilot"))
                {
                    key?.SetValue("TurnOffWindowsCopilot", disable ? 1 : 0, RegistryValueKind.DWord);
                }
                return true;
            }
            catch { return false; }
        }

        // 10. Windows Error Reporting
        public bool SetErrorReporting(bool disable)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting");
                key?.SetValue("Disabled", disable ? 1 : 0, RegistryValueKind.DWord);
                key?.SetValue("DoNotSendAdditionalData", disable ? 1 : 0, RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        // 11. Wi-Fi Sense
        public bool SetWifiSense(bool disable)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config");
                key?.SetValue("AutoConnectAllowedOEM", disable ? 0 : 1, RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        // 12. App Compatibility Inventory
        public bool SetAppInventory(bool disable)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat");
                key?.SetValue("DisableInventory", disable ? 1 : 0, RegistryValueKind.DWord);
                key?.SetValue("AITEnable", disable ? 0 : 1, RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        // 13. Camera & Microphone Background Access
        public bool SetCameraMicBackgroundAccess(bool disable)
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"))
                {
                    key?.SetValue("LetAppsAccessCamera", disable ? 2 : 0, RegistryValueKind.DWord); // 2 = prompt/deny background
                    key?.SetValue("LetAppsAccessMicrophone", disable ? 2 : 0, RegistryValueKind.DWord);
                }
                return true;
            }
            catch { return false; }
        }

        // 14. Remote Registry and Assistance
        public bool SetRemoteAccess(bool disable)
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Remote Assistance"))
                {
                    key?.SetValue("fAllowToGetHelp", disable ? 0 : 1, RegistryValueKind.DWord);
                }
                return true;
            }
            catch { return false; }
        }

        // 15. Windows Recall & AI Analysis Blocker
        public bool SetWindowsRecall(bool disable)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsAI");
                key?.SetValue("DisableAIDataAnalysis", disable ? 1 : 0, RegistryValueKind.DWord);

                using var recallKey = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\WindowsAI");
                recallKey?.SetValue("DisableRecall", disable ? 1 : 0, RegistryValueKind.DWord);

                return true;
            }
            catch { return false; }
        }

                // 16. Р‘Р»РѕРєРёСЂРѕРІРєР° СЃРµСЂРІРµСЂРѕРІ С‚РµР»РµРјРµС‚СЂРёРё С‡РµСЂРµР· С„Р°Р№Р» С…РѕСЃС‚РѕРІ
        public bool SetHostsTelemetryBlock(bool enable)
        {
            try
            {
                string hostsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                if (!System.IO.File.Exists(hostsPath)) return false;

                string currentText = System.IO.File.ReadAllText(hostsPath);
                const string header = "# Р‘Р›РћРљРР РћР’РљРђ РўР•Р›Р•РњР•РўР РР STORM РќРђР§РђР›Рћ";
                const string footer = "# Р‘Р›РћРљРР РћР’РљРђ РўР•Р›Р•РњР•РўР РР STORM РљРћРќР•Р¦";

                int startIdx = currentText.IndexOf(header, StringComparison.OrdinalIgnoreCase);
                int endIdx = currentText.IndexOf(footer, StringComparison.OrdinalIgnoreCase);

                if (startIdx >= 0 && endIdx >= 0)
                {
                    currentText = currentText.Remove(startIdx, (endIdx + footer.Length) - startIdx).Trim();
                }

                if (enable)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine();
                    sb.AppendLine(header);
                    string[] domains = GetTelemetryDomains();
                    foreach (var domain in domains)
                    {
                        if (!string.IsNullOrWhiteSpace(domain))
                        {
                            sb.AppendLine($"0.0.0.0 {domain.Trim()}");
                        }
                    }
                    sb.AppendLine(footer);
                    currentText += sb.ToString();
                }

                System.IO.File.SetAttributes(hostsPath, System.IO.FileAttributes.Normal);
                System.IO.File.WriteAllText(hostsPath, currentText, System.Text.Encoding.UTF8);

                try
                {
                    using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ipconfig.exe",
                        Arguments = "/flushdns",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    p?.WaitForExit(2000);
                }
                catch { }

                return true;
            }
            catch { return false; }
        }

        // 17. Р‘Р»РѕРєРёСЂРѕРІРєР° РІ Р±СЂР°РЅРґРјР°СѓСЌСЂРµ
        public bool SetFirewallTelemetryBlock(bool enable)
        {
            try
            {
                const string ruleName = "STORM_Р‘Р›РћРљРР РћР’РљРђ_РўР•Р›Р•РњР•РўР РР";
                using (var pDel = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                }))
                {
                    pDel?.WaitForExit(2000);
                }

                if (enable)
                {
                    using var pAdd = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netsh.exe",
                        Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir=out action=block remoteip=13.107.4.50,20.54.89.106,20.190.159.0/24,40.77.226.250",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    pAdd?.WaitForExit(2000);
                }
                return true;
            }
            catch { return false; }
        }

        // 18. РћС‚РєР»СЋС‡РµРЅРёРµ СЃР±РѕСЂР° С‚РµР»РµРјРµС‚СЂРёРё РІРёРґРµРѕРєР°СЂС‚
        public bool SetNvidiaTelemetry(bool disable)
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\NVIDIA Corporation\Global\FTS"))
                {
                    key?.SetValue("EnableTelemetry", disable ? 0 : 1, RegistryValueKind.DWord);
                }
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\NVIDIA Corporation\NvControlPanel2\Client"))
                {
                    key?.SetValue("OptInOrOutPreference", disable ? 0 : 1, RegistryValueKind.DWord);
                }

                string[] nvServices = { "NvTelemetryContainer", "NvContainerLocalSystem" };
                foreach (var svc in nvServices)
                {
                    try
                    {
                        using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "sc.exe",
                            Arguments = disable ? $"config \"{svc}\" start= disabled" : $"config \"{svc}\" start= auto",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                        p?.WaitForExit(2000);

                        if (disable)
                        {
                            using var pStop = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "net.exe",
                                Arguments = $"stop \"{svc}\" /y",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            });
                            pStop?.WaitForExit(2000);
                        }
                    }
                    catch { }
                }
                return true;
            }
            catch { return false; }
        }

        // 19. РћС‚РєР»СЋС‡РµРЅРёРµ СЃР±РѕСЂР° С‚РµР»РµРјРµС‚СЂРёРё РїСЂРѕС†РµСЃСЃРѕСЂРѕРІ
        public bool SetIntelTelemetry(bool disable)
        {
            try
            {
                string[] intelServices = { "Intel(R) Content Protection HECI Service", "Intel(R) System Usage Report Service", "IntelCPHS", "esrv_svc", "SURsvc" };
                foreach (var svc in intelServices)
                {
                    try
                    {
                        using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "sc.exe",
                            Arguments = disable ? $"config \"{svc}\" start= disabled" : $"config \"{svc}\" start= auto",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                        p?.WaitForExit(2000);

                        if (disable)
                        {
                            using var pStop = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "net.exe",
                                Arguments = $"stop \"{svc}\" /y",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            });
                            pStop?.WaitForExit(2000);
                        }
                    }
                    catch { }
                }
                return true;
            }
            catch { return false; }
        }

        private static string[] GetTelemetryDomains()
        {
            return new string[]
            {
                "telemetry.microsoft.com",
                "vortex.data.microsoft.com",
                "vortex-win.data.microsoft.com",
                "telecommand.telemetry.microsoft.com",
                "telecommand.telemetry.microsoft.com.nsatc.net",
                "oca.telemetry.microsoft.com",
                "oca.telemetry.microsoft.com.nsatc.net",
                "sqm.telemetry.microsoft.com",
                "sqm.telemetry.microsoft.com.nsatc.net",
                "watson.telemetry.microsoft.com",
                "watson.telemetry.microsoft.com.nsatc.net",
                "redir.metaservices.microsoft.com",
                "choice.microsoft.com",
                "choice.microsoft.com.nsatc.net",
                "df.telemetry.microsoft.com",
                "reports.wes.df.telemetry.microsoft.com",
                "wes.df.telemetry.microsoft.com",
                "services.wes.df.telemetry.microsoft.com",
                "sqm.df.telemetry.microsoft.com",
                "telemetry.urs.microsoft.com",
                "settings-win.data.microsoft.com",
                "diagnostics.support.microsoft.com",
                "feedback.microsoft-hohm.com",
                "feedback.search.microsoft.com",
                "feedback.windows.com",
                "corp.sts.microsoft.com",
                "vortex-sandbox.data.microsoft.com",
                "i1.services.social.microsoft.com",
                "i1.services.social.microsoft.com.nsatc.net",
                "activity.windows.com",
                "edge.activity.windows.com",
                "functional.events.data.microsoft.com",
                "browser.pipe.aria.microsoft.com",
                "web.vortex.data.microsoft.com",
                "mobile.pipe.aria.microsoft.com",
                "onesettings-db5p.settings.data.microsoft.com",
                "onesettings-db5.settings.data.microsoft.com",
                "onesettings-bn2.settings.data.microsoft.com",
                "onesettings-cy2.settings.data.microsoft.com",
                "onesettings-hk2.settings.data.microsoft.com"
            };
        }
        public bool ApplyPreset(string presetName)
        {
            bool max = presetName == "Max" || presetName == "Recommended";
            bool balanced = presetName == "Balanced";
            bool def = presetName == "Default";

            bool disableAll = max;
            bool disableBalanced = max || balanced;

            SetTelemetry(disableBalanced);
            SetAdvertisingId(disableBalanced);
            SetActivityFeed(disableBalanced);
            SetInputTelemetry(disableBalanced);
            SetBingStartSearch(disableBalanced);
            SetEdgeTelemetry(disableBalanced);
            SetFeedbackFrequency(disableBalanced);

            SetLocationSensors(disableAll);
            SetCortanaCopilot(disableAll);
            SetErrorReporting(disableAll);
            SetWifiSense(disableAll);
            SetAppInventory(disableAll);
            SetCameraMicBackgroundAccess(disableAll);
            SetRemoteAccess(disableAll);

            if (def)
            {
                SetTelemetry(false);
                SetAdvertisingId(false);
                SetActivityFeed(false);
                SetInputTelemetry(false);
                SetBingStartSearch(false);
                SetEdgeTelemetry(false);
                SetLocationSensors(false);
                SetFeedbackFrequency(false);
                SetCortanaCopilot(false);
                SetErrorReporting(false);
                SetWifiSense(false);
                SetAppInventory(false);
                SetCameraMicBackgroundAccess(false);
                SetRemoteAccess(false);
            }

            return true;
        }
    }
}
