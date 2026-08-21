using System;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class InputLagService
    {
        private static InputLagService? _instance;
        public static InputLagService Instance => _instance ??= new InputLagService();

        private InputLagService() { }

        public bool IsEnhancedPointerPrecisionDisabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse");
                if (key != null)
                {
                    string? speed = key.GetValue("MouseSpeed")?.ToString();
                    return speed == "0";
                }
            }
            catch { }
            return false;
        }

        public async Task<bool> ApplyZeroInputLagTweaksAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 1. Disable Mouse Acceleration & Smoothing in Control Panel
                    using (var mouseKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                    {
                        if (mouseKey != null)
                        {
                            mouseKey.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                            mouseKey.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                            mouseKey.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
                            mouseKey.SetValue("MouseSensitivity", "10", RegistryValueKind.String); // 6/11 default 1:1 raw input

                            // Linear 1:1 raw mouse curves
                            byte[] rawCurveX = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
                            byte[] rawCurveY = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
                            mouseKey.SetValue("SmoothMouseXCurve", rawCurveX, RegistryValueKind.Binary);
                            mouseKey.SetValue("SmoothMouseYCurve", rawCurveY, RegistryValueKind.Binary);
                        }
                    }

                    // 2. Keyboard Repeat Rate & Delay Tweaks
                    using (var keybKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Keyboard", true))
                    {
                        if (keybKey != null)
                        {
                            keybKey.SetValue("KeyboardDelay", "0", RegistryValueKind.String);
                            keybKey.SetValue("KeyboardSpeed", "31", RegistryValueKind.String);
                        }
                    }

                    // 3. System Responsiveness (Multimedia Scheduling = 0 for 100% CPU to Games/Input)
                    using (var sysRespKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", true))
                    {
                        if (sysRespKey != null)
                        {
                            sysRespKey.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                            sysRespKey.SetValue("NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord);
                        }
                    }

                    // 4. Windows Gaming Tasks Priority (GPU/Audio priority = High)
                    using (var gameKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", true))
                    {
                        if (gameKey != null)
                        {
                            gameKey.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                            gameKey.SetValue("Priority", 6, RegistryValueKind.DWord);
                            gameKey.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                            gameKey.SetValue("SFIO Priority", "High", RegistryValueKind.String);
                        }
                    }

                    // 5. USB Polling buffer tweak in CSRSS
                    using (var winlogonKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true))
                    {
                        if (winlogonKey != null)
                        {
                            winlogonKey.SetValue("EnableCursorSuppression", 0, RegistryValueKind.DWord);
                        }
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
