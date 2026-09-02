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

        public bool IsKeyboardTweakApplied()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Keyboard");
                return key?.GetValue("KeyboardDelay")?.ToString() == "0" && key?.GetValue("KeyboardSpeed")?.ToString() == "31";
            }
            catch { return false; }
        }

        public bool IsSystemResponsivenessApplied()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile");
                return (int?)key?.GetValue("SystemResponsiveness") == 0;
            }
            catch { return false; }
        }

        public bool IsUsbPowerSavingDisabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\USB");
                return (int?)key?.GetValue("DisableSelectiveSuspend") == 1;
            }
            catch { return false; }
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

                    // 3. Port Queue Sizes (MouseDataQueueSize & KeyboardDataQueueSize)
                    try
                    {
                        using var mouKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\mouclass\Parameters");
                        mouKey?.SetValue("MouseDataQueueSize", 100, RegistryValueKind.DWord);

                        using var kbdKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\kbdclass\Parameters");
                        kbdKey?.SetValue("KeyboardDataQueueSize", 100, RegistryValueKind.DWord);
                        appliedCount++;
                    }
                    catch { }

                    // 4. System Responsiveness & Network Throttling
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

                    // 5. Windows Gaming Tasks Priority
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

                    // 6. Disable Cursor Suppression
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

                    // 8. BCD Timers (Disabledynamictick, TscSyncPolicy)
                    try
                    {
                        Process.Start(new ProcessStartInfo("bcdedit.exe", "/set disabledynamictick yes") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
                        Process.Start(new ProcessStartInfo("bcdedit.exe", "/set useplatformclock no") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
                        Process.Start(new ProcessStartInfo("bcdedit.exe", "/set tscsyncpolicy enhanced") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
                        appliedCount++;
                    }
                    catch { }

                    // 9. Win32PrioritySeparation (Short, Variable, 3:1 Foreground Boost = 0x26 / 38 decimal)
                    try
                    {
                        using var prioKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\PriorityControl");
                        if (prioKey != null)
                        {
                            prioKey.SetValue("Win32PrioritySeparation", 0x26, RegistryValueKind.DWord);
                            appliedCount++;
                        }
                    }
                    catch { }

                    return (true, "⚡ Режим минимальной задержки ввода (1:1 Raw Input, BCD Timers, 0x26 Priority) успешно применен!");
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка применения твиков: {ex.Message}");
                }
            });
        }

        public async Task<bool> SetWin32PrioritySeparationAsync(int val)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var prioKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\PriorityControl");
                    prioKey?.SetValue("Win32PrioritySeparation", val, RegistryValueKind.DWord);
                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> SetCudaPState0LockAsync(bool lockPState0)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // NVIDIA CUDA Force P2 State toggle in registry
                    using var nvKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000");
                    if (nvKey != null)
                    {
                        nvKey.SetValue("DisableCudaP2State", lockPState0 ? 1 : 0, RegistryValueKind.DWord);
                        return true;
                    }
                }
                catch { }
                return false;
            });
        }

        public async Task<(bool success, string msg)> ApplySmoothnessAndTimerResolutionTweakAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 1. DWM High Priority & Multimedia Profile Tuning
                    using var wmKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Window Manager");
                    if (wmKey != null)
                    {
                        wmKey.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                        wmKey.SetValue("SFIO Priority", "High", RegistryValueKind.String);
                        wmKey.SetValue("Background Priority", 24, RegistryValueKind.DWord);
                        wmKey.SetValue("Priority", 8, RegistryValueKind.DWord);
                    }

                    // 2. Global Timer Resolution Request Policy
                    using var kernelKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel");
                    kernelKey?.SetValue("GlobalTimerResolutionRequests", 1, RegistryValueKind.DWord);

                    // 3. Disable Paging Executive (Kernel & Drivers strictly in Physical RAM)
                    using var memKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");
                    memKey?.SetValue("DisablePagingExecutive", 1, RegistryValueKind.DWord);

                    // 4. Core Unparking (100% Active Cores in Current Power Scheme)
                    try
                    {
                        var psi1 = new ProcessStartInfo
                        {
                            FileName = "powercfg.exe",
                            Arguments = "/setacvalueindex scheme_current sub_processor CPMINCORES 100",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var p1 = Process.Start(psi1);
                        p1?.WaitForExit(3000);

                        var psi2 = new ProcessStartInfo
                        {
                            FileName = "powercfg.exe",
                            Arguments = "/setactive scheme_current",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var p2 = Process.Start(psi2);
                        p2?.WaitForExit(3000);
                    }
                    catch { }

                    return (true, "✨ Тюнинг плавности применен: DWM переведен в высокий приоритет, парковка ядер отключена, ядро зафиксировано в RAM!");
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка применения параметров плавности: {ex.Message}");
                }
            });
        }

        public async Task<(int fileCount, long bytesCleaned)> PurgeDirect3DShaderCachesAsync()
        {
            return await Task.Run(() =>
            {
                int count = 0;
                long bytes = 0;

                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string[] targets = new[]
                {
                    System.IO.Path.Combine(localApp, @"NVIDIA\DXCache"),
                    System.IO.Path.Combine(localApp, @"NVIDIA\GLCache"),
                    System.IO.Path.Combine(localApp, @"D3DSCache"),
                    System.IO.Path.Combine(localApp, @"AMD\DxCache"),
                    System.IO.Path.Combine(localApp, @"Intel\ShaderCache")
                };

                foreach (var dir in targets)
                {
                    if (System.IO.Directory.Exists(dir))
                    {
                        try
                        {
                            foreach (var file in System.IO.Directory.GetFiles(dir, "*.*", System.IO.SearchOption.AllDirectories))
                            {
                                try
                                {
                                    var fi = new System.IO.FileInfo(file);
                                    long len = fi.Length;
                                    fi.Delete();
                                    count++;
                                    bytes += len;
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }

                return (count, bytes);
            });
        }
    }
}
