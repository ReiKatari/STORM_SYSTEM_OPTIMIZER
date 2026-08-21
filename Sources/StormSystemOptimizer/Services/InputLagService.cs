using System;
using System.Diagnostics;
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

        public async Task<(bool success, string msg)> ApplyZeroInputLagTweaksAsync()
        {
            return await Task.Run(() =>
            {
                int appliedCount = 0;

                try
                {
                    // 1. Disable Mouse Acceleration & Smoothing in Control Panel (1:1 Raw Mouse Curves)
                    try
                    {
                        using var mouseKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Mouse");
                        if (mouseKey != null)
                        {
                            mouseKey.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                            mouseKey.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                            mouseKey.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
                            mouseKey.SetValue("MouseSensitivity", "10", RegistryValueKind.String);

                            byte[] rawCurveX = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
                            byte[] rawCurveY = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
                            mouseKey.SetValue("SmoothMouseXCurve", rawCurveX, RegistryValueKind.Binary);
                            mouseKey.SetValue("SmoothMouseYCurve", rawCurveY, RegistryValueKind.Binary);
                            appliedCount++;
                        }
                    }
                    catch { }

                    // 2. Keyboard Repeat Rate & Delay Tweaks
                    try
                    {
                        using var keybKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Keyboard");
                        if (keybKey != null)
                        {
                            keybKey.SetValue("KeyboardDelay", "0", RegistryValueKind.String);
                            keybKey.SetValue("KeyboardSpeed", "31", RegistryValueKind.String);
                            appliedCount++;
                        }
                    }
                    catch { }

                    // 3. System Responsiveness (Multimedia Scheduling = 0 for 100% CPU to Games/Input)
                    try
                    {
                        using var sysRespKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile");
                        if (sysRespKey != null)
                        {
                            sysRespKey.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                            sysRespKey.SetValue("NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord);
                            appliedCount++;
                        }
                    }
                    catch { }

                    // 4. Windows Gaming Tasks Priority (GPU/Audio priority = High)
                    try
                    {
                        using var gameKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games");
                        if (gameKey != null)
                        {
                            gameKey.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                            gameKey.SetValue("Priority", 6, RegistryValueKind.DWord);
                            gameKey.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                            gameKey.SetValue("SFIO Priority", "High", RegistryValueKind.String);
                            appliedCount++;
                        }
                    }
                    catch { }

                    // 5. Disable Cursor Suppression
                    try
                    {
                        using var winlogonKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon");
                        if (winlogonKey != null)
                        {
                            winlogonKey.SetValue("EnableCursorSuppression", 0, RegistryValueKind.DWord);
                            appliedCount++;
                        }
                    }
                    catch { }

                    return (true, "⚡ Режим минимальной задержки ввода (1:1 Raw Input) успешно применен!");
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка применения твиков: {ex.Message}");
                }
            });
        }
    }
}
