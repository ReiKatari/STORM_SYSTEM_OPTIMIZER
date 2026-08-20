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
                }

                // 2. Advertising ID
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"))
                {
                    key?.SetValue("Enabled", 0, RegistryValueKind.DWord);
                }

                // 3. Tailored Experiences
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\CloudContent"))
                {
                    key?.SetValue("DisableTailoredExperiencesWithDiagnosticData", 1, RegistryValueKind.DWord);
                    key?.SetValue("DisableWindowsConsumerFeatures", 1, RegistryValueKind.DWord);
                }

                // 4. Activity History
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                {
                    key?.SetValue("EnableActivityFeed", 0, RegistryValueKind.DWord);
                    key?.SetValue("PublishUserActivities", 0, RegistryValueKind.DWord);
                    key?.SetValue("UploadUserActivities", 0, RegistryValueKind.DWord);
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
