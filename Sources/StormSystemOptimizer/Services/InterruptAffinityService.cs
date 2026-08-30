using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class PciDeviceInterruptInfo
    {
        public string InstanceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Устройство";
        public string Driver { get; set; } = string.Empty;
        public int Irq { get; set; } = 0;
        public bool IsMsiSupported { get; set; }
        public bool IsMsiEnabled { get; set; }
        public ulong CurrentAffinityMask { get; set; } = 0; // 0 = Default (all cores)
        public string Priority { get; set; } = "Normal";
        public string StatusSummary { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public class InterruptAffinityService
    {
        private static InterruptAffinityService? _instance;
        public static InterruptAffinityService Instance => _instance ??= new InterruptAffinityService();

        public int LogicalCoreCount => Environment.ProcessorCount;

        /// <summary>
        /// Scans system PCI devices (Display, Network, USB XHCI, Audio) for MSI and Affinity policies.
        /// </summary>
        public async Task<List<PciDeviceInterruptInfo>> GetInterruptDevicesAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<PciDeviceInterruptInfo>();

                try
                {
                    using var pciKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI");
                    if (pciKey == null) return list;

                    foreach (var vendorSubKeyName in pciKey.GetSubKeyNames())
                    {
                        using var vendorKey = pciKey.OpenSubKey(vendorSubKeyName);
                        if (vendorKey == null) continue;

                        foreach (var instanceSubKeyName in vendorKey.GetSubKeyNames())
                        {
                            using var instanceKey = vendorKey.OpenSubKey(instanceSubKeyName);
                            if (instanceKey == null) continue;

                            string devDesc = instanceKey.GetValue("DeviceDesc") as string ?? string.Empty;
                            string friendlyName = instanceKey.GetValue("FriendlyName") as string ?? string.Empty;
                            string className = instanceKey.GetValue("Class") as string ?? string.Empty;
                            string driver = instanceKey.GetValue("Driver") as string ?? string.Empty;

                            // Clean description string if formatted like "@oem.inf,%device%;Display Adapter"
                            if (devDesc.Contains(';')) devDesc = devDesc.Split(';').Last();
                            if (friendlyName.Contains(';')) friendlyName = friendlyName.Split(';').Last();

                            string displayName = !string.IsNullOrWhiteSpace(friendlyName) ? friendlyName : devDesc;
                            if (string.IsNullOrWhiteSpace(displayName)) continue;

                            string category = "Другое";
                            bool isTarget = false;

                            if (className.Equals("Display", StringComparison.OrdinalIgnoreCase))
                            {
                                category = "Видеокарта (GPU)";
                                isTarget = true;
                            }
                            else if (className.Equals("Net", StringComparison.OrdinalIgnoreCase))
                            {
                                category = "Сетевой адаптер (NIC)";
                                isTarget = true;
                            }
                            else if (className.Equals("USB", StringComparison.OrdinalIgnoreCase) || devDesc.Contains("xHCI", StringComparison.OrdinalIgnoreCase) || devDesc.Contains("Host Controller", StringComparison.OrdinalIgnoreCase))
                            {
                                category = "Контроллер USB (xHCI)";
                                isTarget = true;
                            }
                            else if (className.Equals("MEDIA", StringComparison.OrdinalIgnoreCase) || className.Equals("AudioEndpoint", StringComparison.OrdinalIgnoreCase) || devDesc.Contains("High Definition Audio", StringComparison.OrdinalIgnoreCase))
                            {
                                category = "Аудио-контроллер";
                                isTarget = true;
                            }

                            if (!isTarget) continue;

                            string instancePath = $@"PCI\{vendorSubKeyName}\{instanceSubKeyName}";
                            var devInfo = new PciDeviceInterruptInfo
                            {
                                InstanceId = instancePath,
                                Name = displayName,
                                Category = category,
                                Driver = driver
                            };

                            // Read Device Parameters\Interrupt Management
                            string devParamsPath = $@"SYSTEM\CurrentControlSet\Enum\{instancePath}\Device Parameters\Interrupt Management";
                            using var interruptKey = Registry.LocalMachine.OpenSubKey(devParamsPath);

                            if (interruptKey != null)
                            {
                                // MSI properties
                                using var msiKey = interruptKey.OpenSubKey("MessageSignaledInterruptProperties");
                                if (msiKey != null)
                                {
                                    devInfo.IsMsiSupported = true;
                                    int msiSupportedVal = (int)(msiKey.GetValue("MSISupported") ?? 0);
                                    devInfo.IsMsiEnabled = msiSupportedVal == 1;
                                }

                                // Affinity policy
                                using var affinityKey = interruptKey.OpenSubKey("Affinity Policy");
                                if (affinityKey != null)
                                {
                                    object? overrideVal = affinityKey.GetValue("AssignmentSetOverride");
                                    if (overrideVal is byte[] rawBytes && rawBytes.Length >= 8)
                                    {
                                        devInfo.CurrentAffinityMask = BitConverter.ToUInt64(rawBytes, 0);
                                    }
                                    else if (overrideVal is int intVal)
                                    {
                                        devInfo.CurrentAffinityMask = (ulong)intVal;
                                    }
                                    else if (overrideVal is long longVal)
                                    {
                                        devInfo.CurrentAffinityMask = (ulong)longVal;
                                    }

                                    int prioVal = (int)(affinityKey.GetValue("DevicePriority") ?? 2);
                                    devInfo.Priority = prioVal switch
                                    {
                                        3 => "Высокий (High)",
                                        2 => "Обычный (Normal)",
                                        1 => "Низкий (Low)",
                                        _ => "По умолчанию"
                                    };
                                }
                            }

                            // Formulate recommendations
                            if (category.Contains("GPU"))
                            {
                                devInfo.Recommendation = "Выделить ядра (напр. CPU 2, 4) + включить MSI режим для устранения микрофризов";
                            }
                            else if (category.Contains("NIC"))
                            {
                                devInfo.Recommendation = "Выделить выделенное ядро для RSS (напр. CPU 1 или 3) для минимального пинга";
                            }
                            else if (category.Contains("USB"))
                            {
                                devInfo.Recommendation = "Изолировать от GPU ядра для мгновенного отклика мыши (1000-8000 Hz)";
                            }
                            else
                            {
                                devInfo.Recommendation = "Рекомендуется включить MSI для устранения аудио-щелчков";
                            }

                            devInfo.StatusSummary = $"MSI: {(devInfo.IsMsiEnabled ? "Включен ✅" : "Отключен (Line-based)")} | Маска: {(devInfo.CurrentAffinityMask == 0 ? "Все ядра" : "0x" + devInfo.CurrentAffinityMask.ToString("X"))}";

                            list.Add(devInfo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[InterruptAffinityService] Error: {ex.Message}");
                }

                return list;
            });
        }

        /// <summary>
        /// Sets the CPU affinity mask and MSI mode for a target PCI device in registry.
        /// </summary>
        public async Task<bool> SetDeviceAffinityAsync(string instanceId, ulong affinityMask, bool enableMsi, int priority = 3)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string basePath = $@"SYSTEM\CurrentControlSet\Enum\{instanceId}\Device Parameters\Interrupt Management";

                    // Affinity Policy
                    using (var affKey = Registry.LocalMachine.CreateSubKey($@"{basePath}\Affinity Policy"))
                    {
                        if (affKey != null)
                        {
                            if (affinityMask == 0)
                            {
                                // Default / all cores
                                try { affKey.DeleteValue("AssignmentSetOverride"); } catch { }
                                try { affKey.DeleteValue("DevicePolicy"); } catch { }
                            }
                            else
                            {
                                affKey.SetValue("DevicePolicy", 4, RegistryValueKind.DWord); // 4 = Specific processors
                                byte[] bytes = BitConverter.GetBytes(affinityMask);
                                affKey.SetValue("AssignmentSetOverride", bytes, RegistryValueKind.Binary);
                            }
                            affKey.SetValue("DevicePriority", priority, RegistryValueKind.DWord);
                        }
                    }

                    // MSI properties
                    using (var msiKey = Registry.LocalMachine.CreateSubKey($@"{basePath}\MessageSignaledInterruptProperties"))
                    {
                        if (msiKey != null)
                        {
                            msiKey.SetValue("MSISupported", enableMsi ? 1 : 0, RegistryValueKind.DWord);
                            msiKey.SetValue("MessageNumberLimit", 16, RegistryValueKind.DWord);
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[InterruptAffinityService] SetDeviceAffinity Error: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Applies an optimized esports layout:
        /// GPU -> Specific cores (e.g. Core 2, 4)
        /// NIC -> Specific core (e.g. Core 3)
        /// USB/Audio -> Specific core (e.g. Core 1)
        /// Core 0 left free for Windows kernel.
        /// </summary>
        public async Task<bool> ApplyEsportsAffinityPresetAsync()
        {
            var devices = await GetInterruptDevicesAsync();
            int cores = Environment.ProcessorCount;
            if (cores < 4) cores = 4; // Fallback

            // Core 0 = 0x1, Core 1 = 0x2, Core 2 = 0x4, Core 3 = 0x8, Core 4 = 0x10, Core 5 = 0x20
            ulong gpuMask = (cores >= 8) ? (0x4UL | 0x10UL) : 0x4UL; // Core 2 (+ Core 4 on 8+ threads)
            ulong nicMask = (cores >= 4) ? 0x8UL : 0x2UL;            // Core 3 (or Core 1)
            ulong usbMask = 0x2UL;                                   // Core 1

            bool allOk = true;
            foreach (var dev in devices)
            {
                if (dev.Category.Contains("GPU"))
                {
                    bool res = await SetDeviceAffinityAsync(dev.InstanceId, gpuMask, enableMsi: true, priority: 3);
                    if (!res) allOk = false;
                }
                else if (dev.Category.Contains("NIC"))
                {
                    bool res = await SetDeviceAffinityAsync(dev.InstanceId, nicMask, enableMsi: true, priority: 3);
                    if (!res) allOk = false;
                }
                else if (dev.Category.Contains("USB") || dev.Category.Contains("Аудио"))
                {
                    bool res = await SetDeviceAffinityAsync(dev.InstanceId, usbMask, enableMsi: true, priority: 2);
                    if (!res) allOk = false;
                }
            }

            return allOk;
        }

        /// <summary>
        /// Resets all devices to Windows default affinity (all cores).
        /// </summary>
        public async Task<bool> ResetAllDevicesToDefaultAsync()
        {
            var devices = await GetInterruptDevicesAsync();
            bool allOk = true;
            foreach (var dev in devices)
            {
                bool res = await SetDeviceAffinityAsync(dev.InstanceId, 0, dev.IsMsiEnabled, 2);
                if (!res) allOk = false;
            }
            return allOk;
        }
    }
}
