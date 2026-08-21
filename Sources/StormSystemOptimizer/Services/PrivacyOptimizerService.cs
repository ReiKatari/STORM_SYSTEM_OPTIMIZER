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
