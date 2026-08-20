using System;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class PrivacyOptimizerService
    {
        private static PrivacyOptimizerService? _instance;
        public static PrivacyOptimizerService Instance => _instance ??= new PrivacyOptimizerService();

        private PrivacyOptimizerService() { }

        public bool DisableTelemetry()
        {
            try
            {
                // 1. DataCollection AllowTelemetry = 0
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                {
                    key?.SetValue("AllowTelemetry", 0, RegistryValueKind.DWord);
                    key?.SetValue("DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord);
                }

                // 2. Advertising ID
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"))
                {
                    key?.SetValue("Enabled", 0, RegistryValueKind.DWord);
                }
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo"))
                {
                    key?.SetValue("DisabledByGroupPolicy", 1, RegistryValueKind.DWord);
                }

                // 3. Tailored Experiences & Consumer Features
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\CloudContent"))
                {
                    key?.SetValue("DisableTailoredExperiencesWithDiagnosticData", 1, RegistryValueKind.DWord);
                    key?.SetValue("DisableWindowsConsumerFeatures", 1, RegistryValueKind.DWord);
                }
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
                {
                    key?.SetValue("DisableConsumerAccountStateContent", 1, RegistryValueKind.DWord);
                }

                // 4. Activity History & Cloud Sync
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                {
                    key?.SetValue("EnableActivityFeed", 0, RegistryValueKind.DWord);
                    key?.SetValue("PublishUserActivities", 0, RegistryValueKind.DWord);
                    key?.SetValue("UploadUserActivities", 0, RegistryValueKind.DWord);
                }

                // 5. Inking & Typing Personalization (Input Telemetry)
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\InputPersonalization"))
                {
                    key?.SetValue("RestrictImplicitInkCollection", 1, RegistryValueKind.DWord);
                    key?.SetValue("RestrictImplicitTextCollection", 1, RegistryValueKind.DWord);
                }
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\InputPersonalization\TrainedDataStore"))
                {
                    key?.SetValue("HarvestContacts", 0, RegistryValueKind.DWord);
                }

                // 6. Bing Search in Start Menu
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer"))
                {
                    key?.SetValue("DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord);
                }

                // 7. Location Sensor
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"))
                {
                    key?.SetValue("DisableLocation", 1, RegistryValueKind.DWord);
                    key?.SetValue("DisableLocationScripting", 1, RegistryValueKind.DWord);
                }

                // 8. Feedback Frequency (Never)
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Siuf\Rules"))
                {
                    key?.SetValue("NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord);
                    key?.SetValue("PeriodInNanoSeconds", 0, RegistryValueKind.DWord);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool EnableTelemetry()
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
                key?.SetValue("AllowTelemetry", 1, RegistryValueKind.DWord);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
