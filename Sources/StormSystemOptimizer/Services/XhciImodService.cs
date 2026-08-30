using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class XhciImodService
    {
        private static XhciImodService? _instance;
        public static XhciImodService Instance => _instance ??= new XhciImodService();

        private const string UsbXhciParamsKey = @"SYSTEM\CurrentControlSet\Services\USBXHCI\Parameters";
        private const string UsbFlagsKey = @"SYSTEM\CurrentControlSet\Control\usbflags";

        /// <summary>
        /// Reads the current IMOD interval in microseconds (0 = Disabled, 1000 = Default).
        /// </summary>
        public int GetCurrentImodInterval()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(UsbXhciParamsKey);
                if (key != null)
                {
                    object? val = key.GetValue("InterruptModerationInterval");
                    if (val is int intVal) return intVal;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[XhciImodService] GetCurrentImodInterval Error: {ex.Message}");
            }
            return 1000; // Windows default
        }

        /// <summary>
        /// Sets the XHCI Interrupt Moderation (IMOD) interval in microseconds.
        /// 0 = Off (Immediate interrupt dispatch for 1K/4K/8K Hz mice)
        /// 50 = Fast Gaming
        /// 1000 = Windows Default
        /// </summary>
        public async Task<bool> SetImodIntervalAsync(int intervalMicroseconds)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var key = Registry.LocalMachine.CreateSubKey(UsbXhciParamsKey))
                    {
                        if (key != null)
                        {
                            key.SetValue("InterruptModerationInterval", intervalMicroseconds, RegistryValueKind.DWord);
                            key.SetValue("EnableInterruptModeration", intervalMicroseconds > 0 ? 1 : 0, RegistryValueKind.DWord);
                        }
                    }

                    // Also configure USB flags for minimum scheduling latency
                    using (var key = Registry.LocalMachine.CreateSubKey(UsbFlagsKey))
                    {
                        if (key != null)
                        {
                            key.SetValue("DisableSelectiveSuspend", 1, RegistryValueKind.DWord);
                            key.SetValue("osvc", new byte[] { 0x00, 0x00 }, RegistryValueKind.Binary);
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[XhciImodService] SetImodInterval Error: {ex.Message}");
                    return false;
                }
            });
        }
    }
}
