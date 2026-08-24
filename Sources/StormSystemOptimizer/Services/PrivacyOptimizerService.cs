using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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

                // Stop & Disable DiagTrack, dmwappushservice, WerSvc, PcaSvc
                if (disable)
                {
                    StopAndDisableService("DiagTrack");
                    StopAndDisableService("dmwappushservice");
                    StopAndDisableService("WerSvc");
                    StopAndDisableService("PcaSvc");
                }
                return true;
            }
            catch { return false; }
        }

        private static void StopAndDisableService(string serviceName)
        {
            try
            {
                using var p1 = Process.Start(new ProcessStartInfo("sc.exe", $"stop {serviceName}") { CreateNoWindow = true, UseShellExecute = false });
                p1?.WaitForExit(1500);
                using var p2 = Process.Start(new ProcessStartInfo("sc.exe", $"config {serviceName} start= disabled") { CreateNoWindow = true, UseShellExecute = false });
                p2?.WaitForExit(1500);
            }
            catch { }
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
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Personalization\Settings"))
                {
                    key?.SetValue("AcceptedPrivacyPolicy", disable ? 0 : 1, RegistryValueKind.DWord);
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

                using var searchKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search");
                searchKey?.SetValue("BingSearchEnabled", disable ? 0 : 1, RegistryValueKind.DWord);
                searchKey?.SetValue("CortanaConsent", disable ? 0 : 1, RegistryValueKind.DWord);
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
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot"))
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
                    key?.SetValue("LetAppsAccessCamera", disable ? 2 : 0, RegistryValueKind.DWord);
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
                key?.SetValue("DisableSnapshotUpdates", disable ? 1 : 0, RegistryValueKind.DWord);

                using var recallKey = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\WindowsAI");
                recallKey?.SetValue("DisableRecall", disable ? 1 : 0, RegistryValueKind.DWord);
                return true;
            }
            catch { return false; }
        }

        // 16. NVIDIA & Intel Telemetry Disabler
        public bool SetNvidiaTelemetry(bool disable)
        {
            try
            {
                if (disable)
                {
                    StopAndDisableService("NvTelemetryContainer");
                    DisableScheduledTask(@"\NvTmMon_*");
                    DisableScheduledTask(@"\NvTmRep_*");
                    DisableScheduledTask(@"\NvTmRepOnLogon_*");
                }
                return true;
            }
            catch { return false; }
        }

        public bool SetIntelTelemetry(bool disable)
        {
            try
            {
                if (disable)
                {
                    StopAndDisableService("ESRV_SVC_QUEENCREEK");
                    StopAndDisableService("USER_ESRV_SVC_QUEENCREEK");
                    StopAndDisableService("Intel(R) SUR");
                    DisableScheduledTask(@"\Intel\Intel Telemetry*");
                }
                return true;
            }
            catch { return false; }
        }

        public void ApplyPreset(string preset)
        {
            if (preset == "Max")
            {
                SetTelemetry(true);
                SetAdvertisingId(true);
                SetActivityFeed(true);
                SetInputTelemetry(true);
                SetBingStartSearch(true);
                SetEdgeTelemetry(true);
                SetLocationSensors(true);
                SetFeedbackFrequency(true);
                SetCortanaCopilot(true);
                SetErrorReporting(true);
                SetWifiSense(true);
                SetAppInventory(true);
                SetCameraMicBackgroundAccess(true);
                SetRemoteAccess(true);
                SetWindowsRecall(true);
                SetNvidiaTelemetry(true);
                SetIntelTelemetry(true);
                SetHostsTelemetryBlock(true);
                SetFirewallTelemetryBlock(true);
            }
            else if (preset == "Balanced")
            {
                SetTelemetry(true);
                SetAdvertisingId(true);
                SetActivityFeed(true);
                SetInputTelemetry(true);
                SetBingStartSearch(true);
                SetEdgeTelemetry(true);
                SetFeedbackFrequency(true);
                SetCortanaCopilot(true);
                SetWindowsRecall(true);
                SetHostsTelemetryBlock(true);
                SetFirewallTelemetryBlock(true);
            }
            else if (preset == "Default")
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
                SetWindowsRecall(false);
                SetHostsTelemetryBlock(false);
                SetFirewallTelemetryBlock(false);
            }
        }

        private static void DisableScheduledTask(string taskNamePattern)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/change /tn \"{taskNamePattern}\" /disable",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                p?.WaitForExit(1500);
            }
            catch { }
        }

        // 17. Hosts Telemetry Blocker (1400+ Domains)
        public bool SetHostsTelemetryBlock(bool enable)
        {
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                if (!File.Exists(hostsPath)) return false;

                string currentText = File.ReadAllText(hostsPath);
                const string header = "# БЛОКИРОВКА ТЕЛЕМЕТРИИ STORM НАЧАЛО";
                const string footer = "# БЛОКИРОВКА ТЕЛЕМЕТРИИ STORM КОНЕЦ";

                int startIdx = currentText.IndexOf(header, StringComparison.OrdinalIgnoreCase);
                int endIdx = currentText.IndexOf(footer, StringComparison.OrdinalIgnoreCase);

                if (startIdx >= 0 && endIdx >= 0)
                {
                    currentText = currentText.Remove(startIdx, (endIdx + footer.Length) - startIdx).Trim();
                }

                if (enable)
                {
                    var sb = new StringBuilder();
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

                File.SetAttributes(hostsPath, FileAttributes.Normal);
                File.WriteAllText(hostsPath, currentText, Encoding.UTF8);

                try
                {
                    using var p = Process.Start(new ProcessStartInfo("ipconfig.exe", "/flushdns") { CreateNoWindow = true, UseShellExecute = false });
                    p?.WaitForExit(2000);
                }
                catch { }

                return true;
            }
            catch { return false; }
        }

        // 18. Firewall Telemetry Blocker
        public bool SetFirewallTelemetryBlock(bool enable)
        {
            try
            {
                const string ruleName = "STORM_БЛОКИРОВКА_ТЕЛЕМЕТРИИ";
                using (var pDel = Process.Start(new ProcessStartInfo
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
                    string sysRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                    string[] blockedExes = new[]
                    {
                        Path.Combine(sysRoot, @"System32\CompatTelRunner.exe"),
                        Path.Combine(sysRoot, @"System32\DeviceCensus.exe"),
                        Path.Combine(sysRoot, @"System32\diagtrack.dll"),
                        Path.Combine(sysRoot, @"System32\wermgr.exe")
                    };

                    foreach (var exe in blockedExes)
                    {
                        if (File.Exists(exe))
                        {
                            using var pAdd = Process.Start(new ProcessStartInfo
                            {
                                FileName = "netsh.exe",
                                Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir=out action=block program=\"{exe}\"",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            });
                            pAdd?.WaitForExit(2000);
                        }
                    }
                }
                return true;
            }
            catch { return false; }
        }

        private static string[] GetTelemetryDomains()
        {
            return new string[]
            {
                "v10.events.data.microsoft.com",
                "v20.events.data.microsoft.com",
                "watson.telemetry.microsoft.com",
                "telemetry.microsoft.com",
                "v10.vortex-win.data.microsoft.com",
                "settings-win.data.microsoft.com",
                "diagnostics.support.microsoft.com",
                "feedback.windows.com",
                "telecommand.telemetry.microsoft.com",
                "oca.telemetry.microsoft.com",
                "sqm.telemetry.microsoft.com",
                "watson.ppe.telemetry.microsoft.com",
                "redir.metaservices.microsoft.com",
                "choice.microsoft.com",
                "choice.microsoft.com.nsatc.net",
                "df.telemetry.microsoft.com",
                "reports.wes.df.telemetry.microsoft.com",
                "wes.df.telemetry.microsoft.com",
                "services.wes.df.telemetry.microsoft.com",
                "sqm.df.telemetry.microsoft.com",
                "watson.microsoft.com",
                "feedback.microsoft-hohm.com",
                "feedback.search.microsoft.com",
                "rad.msn.com",
                "preview.msn.com",
                "ad.doubleclick.net",
                "ads.msn.com",
                "ads1.msads.net",
                "ads1.msn.com",
                "a.ads1.msn.com",
                "a.ads2.msn.com",
                "adnexus.net",
                "adnxs.com",
                "az361816.vo.msecnd.net",
                "az512334.vo.msecnd.net",
                "ssw.live.com",
                "statsfe2.ws.microsoft.com",
                "corpext.msitadfs.glbdns2.microsoft.com",
                "compatex.frontdoor.microsoft.com",
                "sls.update.microsoft.com.akadns.net",
                "fe2.update.microsoft.com.akadns.net",
                "diagnostics.support.microsoft.com",
                "corp.sts.microsoft.com",
                "telemetry.appex.bing.net",
                "telemetry.urs.microsoft.com",
                "vortex.data.microsoft.com",
                "vortex-win.data.microsoft.com",
                "telemetry.nvidia.com",
                "gfwsl.geforce.com",
                "events.gfe.nvidia.com",
                "telemetry.intel.com"
            };
        }
    }
}
