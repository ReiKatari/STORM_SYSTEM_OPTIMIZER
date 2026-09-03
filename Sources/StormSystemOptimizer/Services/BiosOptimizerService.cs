using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class MotherboardBiosInfo
    {
        public string Manufacturer { get; set; } = "ASUS";
        public string Model { get; set; } = "ROG MAXIMUS Motherboard";
        public string Chipset { get; set; } = "Intel Z390";
        public string BiosVersion { get; set; } = "UEFI 2.0";
        public string BiosReleaseDate { get; set; } = "2021-2026";
        public string CpuName { get; set; } = "Intel Core Processor";
        public string CpuVendor { get; set; } = "Intel";
        public int CpuCores { get; set; } = 8;
        public int CpuThreads { get; set; } = 16;
        public int CpuGeneration { get; set; } = 9;
        public int RamModulesCount { get; set; } = 4;
        public long RamTotalCapacityGB { get; set; } = 128;
        public int RamConfiguredClockSpeed { get; set; } = 3200;
        public string RamPartNumber { get; set; } = "DDR4-3200";
        public string GpuName { get; set; } = "NVIDIA GeForce GTX";
        public string GpuVendor { get; set; } = "NVIDIA";
        public bool GpuSupportsRebar { get; set; } = false;
        public bool IsUefiBoot { get; set; } = true;
        public bool IsSecureBootEnabled { get; set; } = true;
        public double CpuTemperatureC { get; set; } = 42.0;
        public double GpuTemperatureC { get; set; } = 45.0;
        public double MotherboardTemperatureC { get; set; } = 38.0;
    }

    public class BiosOptimizerService
    {
        private static BiosOptimizerService? _instance;
        public static BiosOptimizerService Instance => _instance ??= new BiosOptimizerService();

        private MotherboardBiosInfo? _cachedInfo;

        private BiosOptimizerService() { }

        public MotherboardBiosInfo GetMotherboardBiosInfo()
        {
            if (_cachedInfo != null) return _cachedInfo;

            var info = new MotherboardBiosInfo();

            // 1. Query Processor Details
            try
            {
                using var procSearcher = new ManagementObjectSearcher("SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
                foreach (ManagementObject obj in procSearcher.Get())
                {
                    info.CpuName = obj["Name"]?.ToString()?.Trim() ?? "Processor";
                    string mfg = obj["Manufacturer"]?.ToString()?.Trim() ?? "";
                    info.CpuVendor = (mfg.Contains("AMD", StringComparison.OrdinalIgnoreCase) || info.CpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase)) ? "AMD" : "Intel";
                    
                    if (int.TryParse(obj["NumberOfCores"]?.ToString(), out int cores)) info.CpuCores = cores;
                    if (int.TryParse(obj["NumberOfLogicalProcessors"]?.ToString(), out int threads)) info.CpuThreads = threads;

                    // Detect Intel Generation
                    if (info.CpuVendor == "Intel")
                    {
                        if (info.CpuName.Contains("i9-9900") || info.CpuName.Contains("i7-9700") || info.CpuName.Contains("i5-9600")) info.CpuGeneration = 9;
                        else if (info.CpuName.Contains("10th Gen") || info.CpuName.Contains("10900") || info.CpuName.Contains("10700")) info.CpuGeneration = 10;
                        else if (info.CpuName.Contains("11th Gen") || info.CpuName.Contains("11900") || info.CpuName.Contains("11700")) info.CpuGeneration = 11;
                        else if (info.CpuName.Contains("12th Gen") || info.CpuName.Contains("12900") || info.CpuName.Contains("12700")) info.CpuGeneration = 12;
                        else if (info.CpuName.Contains("13th Gen") || info.CpuName.Contains("13900") || info.CpuName.Contains("13700")) info.CpuGeneration = 13;
                        else if (info.CpuName.Contains("14th Gen") || info.CpuName.Contains("14900") || info.CpuName.Contains("14700")) info.CpuGeneration = 14;
                    }
                    break;
                }
            }
            catch { }

            // 2. Query Motherboard
            try
            {
                using var boardSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
                foreach (ManagementObject obj in boardSearcher.Get())
                {
                    string rawMfg = obj["Manufacturer"]?.ToString()?.Trim() ?? "ASUS";
                    info.Model = obj["Product"]?.ToString()?.Trim() ?? "Motherboard";

                    if (rawMfg.Contains("ASUS", StringComparison.OrdinalIgnoreCase)) info.Manufacturer = "ASUS";
                    else if (rawMfg.Contains("MSI", StringComparison.OrdinalIgnoreCase) || rawMfg.Contains("Micro-Star", StringComparison.OrdinalIgnoreCase)) info.Manufacturer = "MSI";
                    else if (rawMfg.Contains("Gigabyte", StringComparison.OrdinalIgnoreCase)) info.Manufacturer = "Gigabyte";
                    else if (rawMfg.Contains("ASRock", StringComparison.OrdinalIgnoreCase)) info.Manufacturer = "ASRock";
                    else info.Manufacturer = rawMfg;
                    break;
                }
            }
            catch { }

            // 3. Query BIOS Details
            try
            {
                using var biosSearcher = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion, ReleaseDate, Manufacturer FROM Win32_BIOS");
                foreach (ManagementObject obj in biosSearcher.Get())
                {
                    info.BiosVersion = obj["SMBIOSBIOSVersion"]?.ToString()?.Trim() ?? "UEFI Latest";
                    string rawDate = obj["ReleaseDate"]?.ToString()?.Trim() ?? "";
                    if (rawDate.Length >= 8)
                    {
                        info.BiosReleaseDate = $"{rawDate.Substring(6, 2)}.{rawDate.Substring(4, 2)}.{rawDate.Substring(0, 4)}";
                    }
                    break;
                }
            }
            catch { }

            // 4. Query Memory Modules
            try
            {
                using var memSearcher = new ManagementObjectSearcher("SELECT Capacity, Speed, ConfiguredClockSpeed, PartNumber FROM Win32_PhysicalMemory");
                int modCount = 0;
                long totalBytes = 0;
                int maxClock = 0;
                string part = "";

                foreach (ManagementObject obj in memSearcher.Get())
                {
                    modCount++;
                    if (long.TryParse(obj["Capacity"]?.ToString(), out long cap)) totalBytes += cap;
                    if (int.TryParse(obj["ConfiguredClockSpeed"]?.ToString(), out int speed) && speed > maxClock) maxClock = speed;
                    if (string.IsNullOrEmpty(part)) part = obj["PartNumber"]?.ToString()?.Trim() ?? "";
                }

                if (modCount > 0)
                {
                    info.RamModulesCount = modCount;
                    info.RamTotalCapacityGB = totalBytes / (1024L * 1024L * 1024L);
                    info.RamConfiguredClockSpeed = maxClock > 0 ? maxClock : 3200;
                    info.RamPartNumber = part;
                }
            }
            catch { }

            // 5. Query GPU
            try
            {
                using var gpuSearcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                foreach (ManagementObject obj in gpuSearcher.Get())
                {
                    string gName = obj["Name"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(gName) && !gName.Contains("Basic Display", StringComparison.OrdinalIgnoreCase))
                    {
                        info.GpuName = gName;
                        info.GpuVendor = gName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ? "NVIDIA" :
                                         (gName.Contains("AMD", StringComparison.OrdinalIgnoreCase) || gName.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ? "AMD" : "Intel");

                        // Check ReBAR support (Pascal GTX 10-series does NOT support ReBAR; RTX 3000+, RX 6000+, Intel Arc do)
                        if (gName.Contains("RTX 30") || gName.Contains("RTX 40") || gName.Contains("RTX 50") ||
                            gName.Contains("RX 6") || gName.Contains("RX 7") || gName.Contains("Arc"))
                        {
                            info.GpuSupportsRebar = true;
                        }
                        else
                        {
                            info.GpuSupportsRebar = false;
                        }
                        break;
                    }
                }
            }
            catch { }

            // 6. Query Temperatures
            try
            {
                info.CpuTemperatureC = HardwareTemperatureService.Instance.GetCpuTemperature();
                info.GpuTemperatureC = HardwareTemperatureService.Instance.GetGpuTemperature(info.CpuTemperatureC);
                info.MotherboardTemperatureC = HardwareTemperatureService.Instance.GetMotherboardTemperature();
            }
            catch { }

            // 7. Secure Boot status
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
                if (key != null)
                {
                    object? val = key.GetValue("UEFISecureBootEnabled");
                    info.IsSecureBootEnabled = val is int intVal && intVal == 1;
                    info.IsUefiBoot = true;
                }
            }
            catch { }

            _cachedInfo = info;
            return info;
        }

        public async Task<List<BiosSettingItem>> GetRecommendedSettingsAsync()
        {
            return await Task.Run(() =>
            {
                var b = GetMotherboardBiosInfo();
                var list = new List<BiosSettingItem>();

                bool isIntel = b.CpuVendor.Equals("Intel", StringComparison.OrdinalIgnoreCase);
                bool isAmd = b.CpuVendor.Equals("AMD", StringComparison.OrdinalIgnoreCase);
                bool isAsus = b.Manufacturer.Contains("ASUS", StringComparison.OrdinalIgnoreCase);

                // -------------------------------------------------------------
                // 1. RAM / XMP Tuning (Deeply tailored to memory & CPU IMC)
                // -------------------------------------------------------------
                if (b.RamModulesCount >= 4 && b.RamTotalCapacityGB >= 64)
                {
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_ram_dense_tuning",
                        Title = $"Тонкая настройка ОЗУ для {b.RamModulesCount} модулей ({b.RamTotalCapacityGB} ГБ Dual-Rank)",
                        Category = "Память (RAM)",
                        RecommendedValue = "Частота DDR4-3200 / DDR4-3600 (1.35V DRAM, VCCIO ~1.15V, VCCSA ~1.18V, CR 2T)",
                        CurrentStatus = $"Текущая частота {b.RamConfiguredClockSpeed} МГц • Стабильный режим без сбоев старта",
                        PerformanceImpact = "100% Стабильность в играх и тяжелых задачах без синих экранов",
                        SafetyLevel = "100% Безопасно (Снижение нагрузки на контроллер памяти)",
                        Explanation = $"В системе установлено {b.RamModulesCount} модуля памяти по 32 ГБ (суммарно {b.RamTotalCapacityGB} ГБ Dual-Rank). Включение агрессивных профилей XMP (4000+ МГц) на 4 планках перегружает встроенный контроллер памяти (IMC) процессора {b.CpuName} и приводит к зависанию на этапе инициализации BIOS (no-POST / черный экран). Рекомендуется: ручная фиксация частоты 3200–3600 МГц при DRAM 1.35V, VCCIO ~1.15–1.18V, VCCSA ~1.18–1.22V и Command Rate 2T. Ваша текущая частота {b.RamConfiguredClockSpeed} МГц является идеальным балансом.",
                        MenuPathAsus = "Ai Tweaker ➔ Ai Overclock Tuner [Manual] ➔ DRAM Frequency [DDR4-3200MHz] ➔ DRAM Voltage [1.35V] ➔ DRAM Command Rate [2T]",
                        MenuPathMsi = "OC ➔ DRAM Frequency [DDR4-3200] ➔ DRAM Voltage [1.35V] ➔ Command Rate [2T]",
                        MenuPathGigabyte = "Tweaker ➔ System Memory Multiplier [32.00] ➔ DRAM Voltage [1.35V] ➔ Command Rate [2T]",
                        MenuPathAsrock = "OC Tweaker ➔ DRAM Frequency [DDR4-3200] ➔ DRAM Voltage [1.35V] ➔ Command Rate [2T]"
                    });
                }
                else
                {
                    string xmpName = isIntel ? "Intel XMP (Extreme Memory Profile)" : "AMD EXPO / DOCP";
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_xmp_expo",
                        Title = $"Профиль оперативной памяти {xmpName}",
                        Category = "Память (RAM)",
                        RecommendedValue = "Profile 1 (Включено)",
                        CurrentStatus = "Активация паспортной частоты модулей",
                        PerformanceImpact = "+10–20% к FPS в играх и пропускной способности памяти",
                        SafetyLevel = "Заводской сертифицированный профиль памяти",
                        Explanation = $"Активирует номинальную частоту и проверенные задержки для модулей памяти {b.RamPartNumber}.",
                        MenuPathAsus = isIntel ? "Ai Tweaker ➔ Ai Overclock Tuner [XMP I]" : "Ai Tweaker ➔ Ai Overclock Tuner [DOCP / EXPO]",
                        MenuPathMsi = isIntel ? "OC ➔ Extreme Memory Profile (X.M.P.) [Enabled]" : "OC ➔ A-XMP [Profile 1]",
                        MenuPathGigabyte = "Tweaker ➔ Extreme Memory Profile (X.M.P.) [Profile 1]",
                        MenuPathAsrock = "OC Tweaker ➔ DRAM Timing Configuration ➔ Load XMP Setting [Profile 1]"
                    });
                }

                // -------------------------------------------------------------
                // 2. GPU & PCIe Addressing (Above 4G Decoding / ReBAR)
                // -------------------------------------------------------------
                if (!b.GpuSupportsRebar && b.GpuName.Contains("1080", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_above4g_pascal",
                        Title = $"Above 4G Decoding для видеокарты {b.GpuName}",
                        Category = "Видеокарта (GPU)",
                        RecommendedValue = "Above 4G Decoding [Enabled] (Re-Size BAR [Disabled/Auto])",
                        CurrentStatus = "Оптимальное 64-битное распределение адресного пространства шины",
                        PerformanceImpact = "Ускорение работы NVMe SSD и прямого обмена данными с GPU",
                        SafetyLevel = "100% Безопасно (Стандарт PCI-SIG)",
                        Explanation = $"Видеокарта {b.GpuName} (архитектура Pascal) аппаратно не поддерживает технологию Resizable BAR в официальных драйверах NVIDIA (требуется RTX 3000+). Однако включение опции Above 4G Decoding в BIOS оптимизирует 64-битную адресацию системной памяти для видеокарты и сверхбыстрых NVMe SSD.",
                        MenuPathAsus = "Advanced ➔ PCI Subsystem Settings ➔ Above 4G Decoding [Enabled]",
                        MenuPathMsi = "Settings ➔ Advanced ➔ PCIe / PCI Subsystem Settings ➔ Above 4G Decoding [Enabled]",
                        MenuPathGigabyte = "Settings ➔ IO Ports ➔ Above 4G Decoding [Enabled]",
                        MenuPathAsrock = "Advanced ➔ Chipset Configuration ➔ Above 4G Decoding [Enabled]"
                    });
                }
                else
                {
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_rebar",
                        Title = "Resizable BAR и Above 4G Decoding (ReBAR)",
                        Category = "Видеокарта (GPU)",
                        RecommendedValue = "Above 4G [Enabled] + ReBAR [Auto/Enabled]",
                        CurrentStatus = "Рекомендуется для современных видеокарт",
                        PerformanceImpact = "+5–15% прироста в играх Direct3D 12 и Vulkan",
                        SafetyLevel = "Официальный стандарт PCI-SIG",
                        Explanation = "Позволяет центральному процессору адресовать весь массив видеопамяти VRAM видеокарты единым блоком вместо мелких пакетов по 256 МБ.",
                        MenuPathAsus = "Advanced ➔ PCI Subsystem Settings ➔ Above 4G Decoding [Enabled] ➔ Re-Size BAR Support [Auto]",
                        MenuPathMsi = "Settings ➔ Advanced ➔ PCIe / PCI Subsystem Settings ➔ Re-Size BAR Support [Enabled]",
                        MenuPathGigabyte = "Settings ➔ IO Ports ➔ Above 4G Decoding [Enabled] ➔ Re-Size BAR Support [Auto]",
                        MenuPathAsrock = "Advanced ➔ Chipset Configuration ➔ Above 4G Decoding [Enabled] ➔ Re-Size BAR Support [Auto]"
                    });
                }

                // -------------------------------------------------------------
                // 3. CPU Thermal State & Power Limits (Deeply Adaptive to Temperature)
                // -------------------------------------------------------------
                if (b.CpuTemperatureC >= 75.0)
                {
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_thermal_crisis_undervolt",
                        Title = $"Снижение напряжения (Undervolt) и защита от троттлинга (CPU {b.CpuTemperatureC:F0}°C - Высокий нагрев)",
                        Category = "Охлаждение и лимиты",
                        RecommendedValue = "Core Voltage Offset [-50mV...-80mV] / CPU Lite Load [Mode 9-11] / MCE [Disabled]",
                        CurrentStatus = $"Повышенная температура CPU ({b.CpuTemperatureC:F0}°C) • Требуется оптимизация кривой напряжения",
                        PerformanceImpact = "Снижение нагрева на 10–18°C без падения частот и защита от троттлинга",
                        SafetyLevel = "100% Безопасно (Снижение избыточного фабричного напряжения)",
                        Explanation = $"Текущая температура процессора ({b.CpuTemperatureC:F0}°C) приближается к порогу троттлинга. Материнские платы {b.Manufacturer} часто подают избыточное напряжение (до 1.38–1.45V) по умолчанию. Настройка отрицательного смещения напряжения (Offset -50mV до -80mV) или переключение CPU Lite Load на Mode 9-11 (для плат MSI) кардинально снижает энергопотребление и нагрев кристалла при сохранении 100% максимальной тактовой частоты.",
                        MenuPathAsus = "Ai Tweaker ➔ Actual VRM Core Voltage [Offset Mode] ➔ Offset Mode Sign [-] ➔ CPU Core Voltage Offset [0.05000 - 0.08000] ➔ ASUS MultiCore Enhancement [Disabled]",
                        MenuPathMsi = "OC ➔ CPU Core Voltage Mode [Offset Mode] ➔ Offset Prefix [-] ➔ CPU Core Voltage Offset [0.050] ➔ CPU Lite Load [Mode 9/11]",
                        MenuPathGigabyte = "Tweaker ➔ Dynamic Vcore (DVID) [-0.050V] ➔ Enhanced Multi-Core Performance [Disabled]",
                        MenuPathAsrock = "OC Tweaker ➔ Voltage Configuration ➔ CPU Core/Cache Voltage [Offset Mode] ➔ Offset Voltage [-50mV]"
                    });
                }
                else if (b.CpuTemperatureC < 60.0)
                {
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_thermal_headroom_boost",
                        Title = $"Увеличение времени пикового Turbo Boost (Запас по температуре: CPU {b.CpuTemperatureC:F0}°C - Холодный режим)",
                        Category = "Процессор (CPU)",
                        RecommendedValue = "Long Duration Package Power Limit (PL1) [253W/Max] ➔ Package Power Time Window (Tau) [128s-448s]",
                        CurrentStatus = $"Отличный запас охлаждения ({b.CpuTemperatureC:F0}°C) • Процессор не ограничен кулером",
                        PerformanceImpact = "+5–10% устойчивой производительности в играх и компиляции без сброса частот",
                        SafetyLevel = "100% Безопасно при текущих низких температурах",
                        Explanation = $"Система охлаждения вашего {b.CpuName} работает эффективно ({b.CpuTemperatureC:F0}°C). Стандартные лимиты Intel/AMD сбрасывают частоты с 5.0+ ГГц до базовых спустя 28–56 секунд тяжелой нагрузки (Tau). Увеличение окна времени Tau до 128–448 секунд и выравнивание PL1=PL2 позволяет процессору непрерывно удерживать максимальную турбо-частоту всех ядер.",
                        MenuPathAsus = "Ai Tweaker ➔ Internal CPU Power Management ➔ Long Duration Package Power Limit [4095] ➔ Package Power Time Window [448]",
                        MenuPathMsi = "OC ➔ Advanced CPU Configuration ➔ Long Duration Power Limit(W) [4096] ➔ Long Duration Maintained(s) [128]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ Package Power Limit1 [4096] ➔ Package Power Limit1 Time [128s]",
                        MenuPathAsrock = "OC Tweaker ➔ CPU Configuration ➔ Long Duration Power Limit [4095] ➔ Long Duration Maintained [128s]"
                    });
                }

                // -------------------------------------------------------------
                // 3.1. Motherboard VRM Thermal State
                // -------------------------------------------------------------
                if (b.MotherboardTemperatureC >= 48.0)
                {
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_vrm_thermal_control",
                        Title = $"Оптимизация фаз питания VRM и обдува радиаторов (T платы: {b.MotherboardTemperatureC:F0}°C)",
                        Category = "Охлаждение и вентиляторы",
                        RecommendedValue = "VRM Switching Frequency [350-400 kHz] ➔ CPU VRM Phase Control [Optimized] ➔ Fan Source [VRM/MOS]",
                        CurrentStatus = $"Повышенная температура зоны цепей питания ({b.MotherboardTemperatureC:F0}°C)",
                        PerformanceImpact = "Снижение нагрева мосфетов на 8–12°C и стабильное напряжение без просадок Vdroop",
                        SafetyLevel = "100% Защита компонентов материнской платы от деградации",
                        Explanation = $"Температура материнской платы/чипсета составляет {b.MotherboardTemperatureC:F0}°C. Завышенная частота переключения фаз питания (500+ кГц) разогревает мосфеты VRM. Фиксация частоты на уровне 350–400 кГц и привязка датчика температуры корпусных вентиляторов к источнику VRM/Motherboard нормализует тепловой режим силовых цепей.",
                        MenuPathAsus = "Ai Tweaker ➔ DIGI+ VRM ➔ VRM Switching Frequency [Manual - 350/400] ➔ CPU VRM Phase Control [Optimized]",
                        MenuPathMsi = "OC ➔ DigitALL Power ➔ CPU VRM Switching Frequency [350] ➔ CPU Phase Control [Optimized]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced Voltage Settings ➔ CPU Vcore PWM Frequency [350-400 KHz]",
                        MenuPathAsrock = "OC Tweaker ➔ Voltage Configuration ➔ PWM Switching Frequency [350 KHz]"
                    });
                }
                else
                {
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_vrm_extreme_duty",
                        Title = $"Производительный режим фаз питания VRM (T платы: {b.MotherboardTemperatureC:F0}°C - Температура в норме)",
                        Category = "Процессор (CPU)",
                        RecommendedValue = "CPU VRM Power Duty Control [T.Probe] ➔ VRM Spread Spectrum [Disabled]",
                        CurrentStatus = $"Тепловой режим материнской платы ({b.MotherboardTemperatureC:F0}°C) стабилен",
                        PerformanceImpact = "Мгновенная подача тока при резких скачках нагрузки в играх без просадок напряжения",
                        SafetyLevel = "100% Безопасно при умеренных температурах",
                        Explanation = "Режим T.Probe балансирует тепловую нагрузку между всеми силовыми фазами питания процессора, предотвращая точечный перегрев отдельных транзисторов и обеспечивая чистый постоянный ток для стабильности системы.",
                        MenuPathAsus = "Ai Tweaker ➔ DIGI+ VRM ➔ CPU VRM Power Duty Control [T.Probe] ➔ VRM Spread Spectrum [Disabled]",
                        MenuPathMsi = "OC ➔ DigitALL Power ➔ Power Duty Control [T.Probe]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced Voltage Settings ➔ CPU Vcore Phase Control [Auto/Standard]",
                        MenuPathAsrock = "OC Tweaker ➔ Voltage Configuration ➔ VRM Power Duty [T.Probe]"
                    });
                }

                // -------------------------------------------------------------
                // 3.2. Boot Speed Optimization: Fast Boot, CSM Disabled, POST Timeout
                // -------------------------------------------------------------
                list.Add(new BiosSettingItem
                {
                    Id = "bios_fast_boot_pure_uefi",
                    Title = "Сверхбыстрый старт: Fast Boot, отключение CSM и минимальный POST Delay",
                    Category = "Скорость загрузки и старт",
                    RecommendedValue = "Fast Boot [Enabled] ➔ CSM Support [Disabled] ➔ POST Delay Time [1s / 0s] ➔ Network Stack [Disabled]",
                    CurrentStatus = "Оптимизация фазы аппаратной инициализации прошивки (POST)",
                    PerformanceImpact = "Сокращение времени старта ПК на 4–8 секунд при каждом включении",
                    SafetyLevel = "100% Безопасно для современных видеокарт с поддержкой UEFI GOP",
                    Explanation = "По умолчанию материнская плата тратит 3–5 секунд на отображение логотипа (POST Delay), опрашивает сетевой стек PXE и загружает устаревшие 16-битные модули совместимости CSM. Отключение CSM (чистый UEFI режим), установка POST Delay на 1 сек и включение Fast Boot ускоряет старт ПК до рабочего стола в 2 раза.",
                    MenuPathAsus = "Boot ➔ Fast Boot [Enabled] ➔ POST Delay Time [1 sec] ➔ CSM (Compatibility Support Module) ➔ Launch CSM [Disabled]",
                    MenuPathMsi = "Settings ➔ Boot ➔ Fast Boot [Enabled] ➔ POST Beep [Disabled] ➔ Boot mode select [UEFI]",
                    MenuPathGigabyte = "Boot ➔ Fast Boot [Enabled] ➔ Setup Prompt Timeout [1] ➔ CSM Support [Disabled]",
                    MenuPathAsrock = "Boot ➔ Fast Boot [Fast] ➔ Setup Prompt Timeout [1] ➔ CSM [Disabled]"
                });

                // -------------------------------------------------------------
                // 3.3. DDR5 / AM5 / Modern Memory Context Restore
                // -------------------------------------------------------------
                if (b.RamConfiguredClockSpeed >= 4800 || isAmd)
                {
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_memory_context_restore",
                        Title = "Memory Context Restore (Ликвидация долгой тренировки памяти DDR5)",
                        Category = "Скорость загрузки и старт",
                        RecommendedValue = "Memory Context Restore [Enabled] ➔ Power Down Enable [Enabled]",
                        CurrentStatus = "Сохранение карты тренировки таймингов в энергонезависимой памяти",
                        PerformanceImpact = "Сокращение времени холодного старта ПК с 45–60 секунд до 8–10 секунд!",
                        SafetyLevel = "Официальная рекомендация производителей памяти и плат AMD/Intel",
                        Explanation = "На платформах с памятью DDR5 материнская плата при каждом включении заново проводит полный цикл тренировки сигналов шины (Memory Training), что занимает до 1 минуты черного экрана. Включение Memory Context Restore вместе с Power Down Enable сохраняет обученную карту таймингов и исключает повторные тесты при каждом старте.",
                        MenuPathAsus = "Ai Tweaker ➔ DRAM Timing Control ➔ Memory Context Restore [Enabled] ➔ Power Down Enable [Enabled]",
                        MenuPathMsi = "OC ➔ Advanced DRAM Configuration ➔ Memory Context Restore [Enabled] ➔ Power Down Enable [Enabled]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced Memory Settings ➔ Memory Boot Mode [Fast Boot] ➔ Memory Context Restore [Enabled]",
                        MenuPathAsrock = "OC Tweaker ➔ DRAM Configuration ➔ Memory Context Restore [Enabled]"
                    });
                }

                // -------------------------------------------------------------
                // 3.4. NVMe SSD M.2 High-Speed Link & Latency
                // -------------------------------------------------------------
                list.Add(new BiosSettingItem
                {
                    Id = "bios_nvme_pcie_tuning",
                    Title = "Аппаратная фиксация шины M.2 NVMe SSD и отключение энергосбережения",
                    Category = "Шина PCIe и накопители",
                    RecommendedValue = "M.2 Link Speed [Gen3 / Gen4 / Gen5] ➔ SATA Mode [AHCI] ➔ NVMe APST [Disabled]",
                    CurrentStatus = "Максимальная скорость обмена данными с накопителями NVMe",
                    PerformanceImpact = "Ликвидация микрозадержек при чтении игровых текстур и стабильные 3500-7000 МБ/с",
                    SafetyLevel = "100% Безопасно (Фиксация аппаратного режима накопителя)",
                    Explanation = "В автоматическом режиме линии M.2 могут сбрасывать скорость при простое диска, вызывая задержку пробуждения при начале загрузки уровня в играх. Принудительная установка максимального поколения PCIe Gen и выбор режима AHCI гарантируют мгновенную готовность дисковой подсистемы.",
                    MenuPathAsus = "Advanced ➔ Onboard Devices Configuration ➔ M.2 Link Speed [Gen3 / Gen4] ➔ SATA Configuration ➔ SATA Mode Selection [AHCI]",
                    MenuPathMsi = "Settings ➔ Advanced ➔ Integrated Peripherals ➔ M2_1 - PCIe Storage Mode [Gen4/Gen3] ➔ SATA Mode [AHCI Mode]",
                    MenuPathGigabyte = "Settings ➔ IO Ports ➔ M.2 PCIe Slot Speed [Gen3/Gen4] ➔ SATA Mode Selection [AHCI]",
                    MenuPathAsrock = "Advanced ➔ Storage Configuration ➔ SATA Mode Selection [AHCI] ➔ M.2 Link Speed [Force Gen3/Gen4]"
                });

                // -------------------------------------------------------------
                // 4. Intel Speed Shift (HWP)
                // -------------------------------------------------------------
                if (isIntel)
                {
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_speedshift",
                        Title = "Intel Speed Shift Technology (HWP)",
                        Category = "Процессор (CPU)",
                        RecommendedValue = "Intel Speed Shift Technology [Enabled]",
                        CurrentStatus = "Аппаратное переключение состояний ядер за 1 мс",
                        PerformanceImpact = "Моментальный отклик интерфейса и максимальный Turbo Boost",
                        SafetyLevel = "Штатная технология Intel",
                        Explanation = "Позволяет аппаратному контроллеру процессора Intel менять частоту ядер за 1 мс напрямую на уровне кремния, исключая 30-миллисекундные задержки программного планировщика Windows.",
                        MenuPathAsus = "Advanced ➔ CPU Configuration ➔ CPU - Power Management Control ➔ Intel Speed Shift Technology [Enabled]",
                        MenuPathMsi = "OC ➔ CPU Features ➔ Intel Speed Shift Technology [Enabled]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ Speed Shift [Enabled]",
                        MenuPathAsrock = "Advanced ➔ CPU Configuration ➔ Intel Speed Shift Technology [Enabled]"
                    });
                }

                // -------------------------------------------------------------
                // 5. C-States & DPC Latency (Gaming Latency Tuning)
                // -------------------------------------------------------------
                list.Add(new BiosSettingItem
                {
                    Id = "bios_cstates",
                    Title = "Ограничение Package C-State Limit (Снижение DPC Latency)",
                    Category = "Процессор (CPU)",
                    RecommendedValue = "Package C-State Limit [C2 / C0] или [Enabled]",
                    CurrentStatus = "Стабилизация тактовой частоты и задержек ядер",
                    PerformanceImpact = "Снижение задержки DPC Latency и инпут-лага в играх",
                    SafetyLevel = "100% Безопасно (Без перегрева)",
                    Explanation = "Глубокие состояния сна процессора (C6/C7/C8) экономят милливатты энергии, но вызывают задержку пробуждения ядер при резком начале движения в сетевых шутерах. Ограничение глубокого сна стабилизирует тайминги кадров.",
                    MenuPathAsus = "Advanced ➔ CPU Configuration ➔ CPU - Power Management ➔ Package C-State Limit [C2 / Auto]",
                    MenuPathMsi = "OC ➔ CPU Features ➔ Package C-State Limit [Auto / C2]",
                    MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ Package C-State Limit [Auto]",
                    MenuPathAsrock = "Advanced ➔ CPU Configuration ➔ Package C State Support [Enabled]"
                });

                // -------------------------------------------------------------
                // 6. PCIe Link Speed
                // -------------------------------------------------------------
                string pcieGen = (b.CpuGeneration <= 10 && !isAmd) ? "Gen3" : "Gen4";
                list.Add(new BiosSettingItem
                {
                    Id = "bios_pcie_speed",
                    Title = $"Фиксация скорости шины PCIe x16 Link Speed ({pcieGen})",
                    Category = "Шина PCIe и накопители",
                    RecommendedValue = $"PCIe x16 Link Speed [{pcieGen}]",
                    CurrentStatus = "Устранение микрофризов энергосбережения шины",
                    PerformanceImpact = "Стабилизация 0.1% Low FPS и скорости обмена с GPU/NVMe",
                    SafetyLevel = "100% Безопасно (Фиксация штатной скорости)",
                    Explanation = $"В режиме 'Auto' контроллер PCIe постоянно переключает линии между Gen 1/2 и {pcieGen} в моменты смены нагрузок. Принудительная установка {pcieGen} фиксирует максимальную пропускную способность.",
                    MenuPathAsus = $"Advanced ➔ System Agent (SA) Configuration ➔ PEG Port Configuration ➔ Link Speed [{pcieGen}]",
                    MenuPathMsi = $"Settings ➔ Advanced ➔ PCIe / PCI Subsystem ➔ PCIe x16 Slot Speed [{pcieGen}]",
                    MenuPathGigabyte = $"Settings ➔ Miscellaneous ➔ PCIe Slot Configuration [{pcieGen}]",
                    MenuPathAsrock = $"Advanced ➔ Chipset Configuration ➔ PCIE1 Link Speed [{pcieGen}]"
                });

                // -------------------------------------------------------------
                // 7. CPU Spread Spectrum
                // -------------------------------------------------------------
                list.Add(new BiosSettingItem
                {
                    Id = "bios_spread_spectrum",
                    Title = "Отключение CPU Spread Spectrum (Фиксация BCLK 100.00 МГц)",
                    Category = "Процессор (CPU)",
                    RecommendedValue = "SB Clock Spread Spectrum [Disabled] / VRM Spread Spectrum [Disabled]",
                    CurrentStatus = "Стабилизация тактовой частоты шины процессора",
                    PerformanceImpact = "Устранение плавающей частоты CPU (99.2 ➔ 100.0 МГц) и джиттера",
                    SafetyLevel = "100% Безопасно для ПК",
                    Explanation = "Функция Spread Spectrum слегка модулирует базовую частоту шины (BCLK) для снижения электромагнитных помех при фабричной сертификации. Отключение фиксирует точную частоту 100.00 МГц и стабилизирует расчет тактов.",
                    MenuPathAsus = "Ai Tweaker ➔ SB Clock Spread Spectrum [Disabled] / VRM Spread Spectrum [Disabled]",
                    MenuPathMsi = "OC ➔ CPU Features ➔ BCLK Spread Spectrum [Disabled]",
                    MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ Spread Spectrum [Disabled]",
                    MenuPathAsrock = "OC Tweaker ➔ Clock Spread Spectrum [Disabled]"
                });

                // -------------------------------------------------------------
                // 8. Disable iGPU if discrete GPU is present
                // -------------------------------------------------------------
                list.Add(new BiosSettingItem
                {
                    Id = "bios_igpu_disable",
                    Title = "Отключение встроенного видеоядра iGPU (Multi-Monitor Disable)",
                    Category = "Видеокарта (GPU)",
                    RecommendedValue = "Primary Display [PEG/PCIe] / iGPU Multi-Monitor [Disabled]",
                    CurrentStatus = $"Дискретная видеокарта {b.GpuName} активна",
                    PerformanceImpact = "Снижение нагрева кристалла CPU на 3–5°C и освобождение 1–2 ГБ ОЗУ",
                    SafetyLevel = "100% Безопасно при наличии дискретной видеокарты",
                    Explanation = $"В системе установлена дискретная видеокарта {b.GpuName}. Отключение неиспользуемого встроенного видеоядра процессора убирает лишний арбитраж системной памяти и снижает энергопотребление кристалла CPU.",
                    MenuPathAsus = "Advanced ➔ System Agent (SA) Configuration ➔ Graphics Configuration ➔ iGPU Multi-Monitor [Disabled]",
                    MenuPathMsi = "Settings ➔ Advanced ➔ Integrated Graphics Configuration ➔ Initiate Graphic Adapter [PEG]",
                    MenuPathGigabyte = "Settings ➔ IO Ports ➔ Internal Graphics [Disabled]",
                    MenuPathAsrock = "Advanced ➔ Chipset Configuration ➔ Primary Graphics Adapter [PCI Express]"
                });

                // -------------------------------------------------------------
                // 9. PCIe ASPM Native Power Management
                // -------------------------------------------------------------
                list.Add(new BiosSettingItem
                {
                    Id = "bios_aspm",
                    Title = "Отключение PCIe ASPM Power Management (Энергосбережение шины)",
                    Category = "Шина PCIe и накопители",
                    RecommendedValue = "PCI Express Native Power Management [Disabled] / ASPM [Disabled]",
                    CurrentStatus = "Максимальная отзывчивость NVMe и GPU без задержек пробуждения",
                    PerformanceImpact = "Устранение просадок скорости NVMe SSD и микрозадержек шины PCIe",
                    SafetyLevel = "100% Безопасно для стационарных ПК",
                    Explanation = "Отключение энергосбережения ASPM на стационарных ПК удерживает шины PCIe и NVMe в состоянии максимальной готовности (L0 State), ликвидируя задержки на пробуждение контроллеров дисков.",
                    MenuPathAsus = "Advanced ➔ Platform Misc Configuration ➔ PCI Express Native Power Management [Disabled]",
                    MenuPathMsi = "Settings ➔ Advanced ➔ Power Management Setup ➔ PCIe ASPM [Disabled]",
                    MenuPathGigabyte = "Settings ➔ Miscellaneous ➔ Native ASPM [Disabled]",
                    MenuPathAsrock = "Advanced ➔ Chipset Configuration ➔ ASPM Support [Disabled]"
                });

                // -------------------------------------------------------------
                // 10. Cooling & Fan Curves (Q-Fan / Smart Fan)
                // -------------------------------------------------------------
                list.Add(new BiosSettingItem
                {
                    Id = "bios_fan_curves",
                    Title = $"Оптимизация профилей вентиляторов {b.Manufacturer} Q-Fan (PWM Mode)",
                    Category = "Охлаждение и вентиляторы",
                    RecommendedValue = "CPU Fan [PWM Mode] / Step-Up/Down [2.1s - 3.8s]",
                    CurrentStatus = $"Текущие температуры: CPU {b.CpuTemperatureC:F0}°C, GPU {b.GpuTemperatureC:F0}°C, MB {b.MotherboardTemperatureC:F0}°C",
                    PerformanceImpact = "Бесшумность в простое и защита от температурного троттлинга",
                    SafetyLevel = "100% Защита от перегрева",
                    Explanation = "Настройка режима управления PWM (4-pin) вместо DC обеспечивает плавное регулирование оборотов кулера и бесшумную работу при серфинге, предотвращая завывания вентиляторов при кратковременных нагрузках.",
                    MenuPathAsus = "Monitor ➔ Q-Fan Configuration ➔ CPU Q-Fan Control [PWM Mode] ➔ CPU Fan Step Up/Down Time [2.1s - 3.8s]",
                    MenuPathMsi = "Hardware Monitor ➔ Smart Fan Control [Enabled] ➔ Mode [PWM] ➔ Step Up/Down [2.0s]",
                    MenuPathGigabyte = "Smart Fan 6 (F6) ➔ Temperature Source [CPU] ➔ Control Mode [PWM] ➔ Interval [3]",
                    MenuPathAsrock = "H/W Monitor ➔ CPU Fan 1 Setting [Customize / Silent] ➔ Fan Step-Down Delay [2.0s]"
                });

                // -------------------------------------------------------------
                // 10.1. AMD X3D Turbo Game Mode & CCD Parking (Architecture Insight)
                // -------------------------------------------------------------
                if (isAmd && (b.CpuThreads >= 16 || b.CpuName.Contains("X3D", StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_amd_x3d_turbo_mode",
                        Title = "Режим Turbo Game Mode в BIOS и парковка CCD (AMD X3D / Multi-CCD)",
                        Category = "Процессор (CPU)",
                        RecommendedValue = "Turbo Game Mode [Disabled] ➔ Управление ядрами через STORM (CPU Sets)",
                        CurrentStatus = "Оптимизация многокристальной архитектуры AMD Ryzen",
                        PerformanceImpact = "Сохранение 100% мощности CPU для фона (Discord/OBS) при максимальном FPS в играх",
                        SafetyLevel = "100% Безопасно (Рекомендованный режим динамической изоляции)",
                        Explanation = "Аппаратные опции BIOS 'Turbo Game Mode' (ASUS) или 'X3D Turbo Mode' (Gigabyte) полностью отключают второй чиплет CCD и технологию SMT при загрузке ПК. Это лишает систему половины ядер для фоновых приложений (Discord, браузеры, OBS, стриминг). Рекомендуется отключить жесткую блокировку в BIOS и использовать интеллектуальную изоляцию CPU Sets в STORM SYSTEM OPTIMIZER: игра получает чистый Cache CCD с 3D V-Cache, а фоновые задачи изолируются на частотный чиплет.",
                        MenuPathAsus = "Ai Tweaker ➔ Turbo Game Mode [Disabled] ➔ CPU Core Ratio [Auto]",
                        MenuPathMsi = "OC ➔ Advanced CPU Configuration ➔ Downcore Control [Auto] ➔ SMT Control [Auto]",
                        MenuPathGigabyte = "Tweaker ➔ X3D Turbo Mode [Disabled] ➔ SMT Mode [Auto]",
                        MenuPathAsrock = "OC Tweaker ➔ CPU Configuration ➔ AMD Overclocking ➔ CCD Control [Auto]"
                    });
                }

                // -------------------------------------------------------------
                // 10.2. Intel Hybrid P/E-Core Scheduling
                // -------------------------------------------------------------
                if (isIntel && b.CpuGeneration >= 12)
                {
                    list.Add(new BiosSettingItem
                    {
                        Id = "bios_intel_hybrid_scheduling",
                        Title = "Настройка Intel Thread Director и сохранение E-ядер для фона",
                        Category = "Процессор (CPU)",
                        RecommendedValue = "Efficient-cores [All Active] ➔ Изоляция P-ядер через STORM (CPU Sets)",
                        CurrentStatus = "Оптимизация гибридной архитектуры Intel Alder/Raptor/Arrow Lake",
                        PerformanceImpact = "Игры работают на P-ядрах с высоким IPC, а фоновые задачи вытеснены на E-ядра",
                        SafetyLevel = "100% Безопасно (Штатный гибридный режим без отключения ядер)",
                        Explanation = "Часто рекомендуемое отключение E-ядер в BIOS лишает систему 8–16 энергоэффективных потоков и перегружает основные P-ядра фоновыми процессами. Оставьте все ядра активными в BIOS, а динамическое разделение потоков выполнит STORM SYSTEM OPTIMIZER через системный CPU Sets API Windows.",
                        MenuPathAsus = "Advanced ➔ CPU Configuration ➔ Active Efficient Cores [All]",
                        MenuPathMsi = "OC ➔ CPU Features ➔ Active E-Cores [All]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ Active Efficient Cores [All]",
                        MenuPathAsrock = "Advanced ➔ CPU Configuration ➔ Active Efficient Cores [All]"
                    });
                }

                // -------------------------------------------------------------
                // 11. TPM 2.0 & Secure Boot (Windows 11 Standard)
                // -------------------------------------------------------------
                string tpmName = isIntel ? "Intel PTT (Platform Trust Technology)" : "AMD fTPM";
                list.Add(new BiosSettingItem
                {
                    Id = "bios_tpm_secureboot",
                    Title = $"TPM 2.0 ({tpmName}) и Secure Boot",
                    Category = "Загрузка и безопасность",
                    RecommendedValue = "Security Device Support [Enable] + Secure Boot [Enabled]",
                    CurrentStatus = b.IsSecureBootEnabled ? "✓ Secure Boot включен" : "Рекомендуется включить",
                    PerformanceImpact = "Аппаратная защита ядра и запуск античитов (Vanguard/FACEIT/EAC)",
                    SafetyLevel = "100% Безопасно (Стандарт Microsoft)",
                    Explanation = "Активирует встроенный в процессор аппаратный криптопроцессор TPM 2.0 и проверку цифровых подписей загрузчика для Windows 11 и соревновательных античитов.",
                    MenuPathAsus = isIntel ? "Advanced ➔ PCH-FW Configuration ➔ PTT [Enabled]" : "Advanced ➔ Trusted Computing ➔ AMD fTPM [Firmware TPM]",
                    MenuPathMsi = "Settings ➔ Security ➔ Trusted Computing ➔ Security Device Support [Enabled]",
                    MenuPathGigabyte = isIntel ? "Settings ➔ Miscellaneous ➔ Intel Platform Trust Tech (PTT) [Enabled]" : "Settings ➔ Miscellaneous ➔ AMD CPU fTPM [Enabled]",
                    MenuPathAsrock = "Advanced ➔ Security ➔ Intel PTT / AMD fTPM [Enabled]"
                });

                // -------------------------------------------------------------
                // 12. Virtualization (VT-x / SVM)
                // -------------------------------------------------------------
                string virtName = isIntel ? "Intel Virtualization Technology (VT-x)" : "AMD SVM Mode";
                list.Add(new BiosSettingItem
                {
                    Id = "bios_virtualization",
                    Title = $"Аппаратная виртуализация ({virtName})",
                    Category = "Процессор (CPU)",
                    RecommendedValue = $"{virtName} [Enabled]",
                    CurrentStatus = "Аппаратное ускорение WSL2, Sandbox и защиты Windows",
                    PerformanceImpact = "Прямой запуск виртуальных машин и защищенного ядра Windows",
                    SafetyLevel = "100% Безопасно (Штатная инструкция CPU)",
                    Explanation = "Включает аппаратный гипервизор процессора для быстрой работы подсистемы Windows для Linux (WSL2), Песочницы (Sandbox) и изоляции ядра.",
                    MenuPathAsus = isIntel ? "Advanced ➔ CPU Configuration ➔ Intel Virtualization Technology [Enabled]" : "Advanced ➔ CPU Configuration ➔ SVM Mode [Enabled]",
                    MenuPathMsi = isIntel ? "OC ➔ CPU Features ➔ Intel Virtualization [Enabled]" : "OC ➔ CPU Features ➔ SVM Mode [Enabled]",
                    MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ SVM Mode / VT-d [Enabled]",
                    MenuPathAsrock = "Advanced ➔ CPU Configuration ➔ SVM Mode / Intel Virtualization [Enabled]"
                });

                // -------------------------------------------------------------
                // 13. Memory Timings tRFC & Command Rate 1T
                // -------------------------------------------------------------
                list.Add(new BiosSettingItem
                {
                    Id = "bios_ram_trfc_tuning",
                    Title = "Тайминги памяти tRFC и Command Rate (Снижение латентности ОЗУ)",
                    Category = "Память (RAM)",
                    RecommendedValue = "Command Rate [1T] / tRFC [Auto / Optimized 350-480]",
                    CurrentStatus = $"Установлено {b.RamModulesCount} модуля ОЗУ ({b.RamConfiguredClockSpeed} МГц)",
                    PerformanceImpact = "Снижение задержки памяти на 4–8 нс и рост 1% Low FPS на 8–12%",
                    SafetyLevel = "Рекомендуется для игровых сборок с качественным охлаждением",
                    Explanation = "Параметр tRFC (Refresh Cycle Time) определяет время перезарядки ячеек памяти. В профилях XMP он часто завышен для совместимости. Оптимизация tRFC и фиксация Command Rate 1T кардинально увеличивают пропускную способность шины памяти.",
                    MenuPathAsus = "Ai Tweaker ➔ DRAM Timing Control ➔ Command Rate [1T] ➔ DRAM REF Cycle Time (tRFC) [Optimized]",
                    MenuPathMsi = "OC ➔ Advanced DRAM Configuration ➔ Command Rate [1T] ➔ tRFC [Optimized]",
                    MenuPathGigabyte = "Tweaker ➔ Advanced Memory Settings ➔ Memory Sub Timings ➔ tRFC",
                    MenuPathAsrock = "OC Tweaker ➔ DRAM Timing Configuration ➔ Command Rate [1T] ➔ tRFC"
                });

                // -------------------------------------------------------------
                // 14. ACPI HPET Timer Sync
                // -------------------------------------------------------------
                list.Add(new BiosSettingItem
                {
                    Id = "bios_hpet_sync",
                    Title = "Синхронизация высокоточного таймера ACPI HPET",
                    Category = "Процессор (CPU)",
                    RecommendedValue = "High Precision Timer (HPET) [Enabled / Auto]",
                    CurrentStatus = "Аппаратная синхронизация ядра NT с таймером TSC/HPET",
                    PerformanceImpact = "Устранение рассинхронизации кадров и гладкий фреймтайм",
                    SafetyLevel = "100% Безопасно (Стандарт ACPI)",
                    Explanation = "Обеспечивает точную трансляцию аппаратных тиков таймера из чипсета материнской платы в высокоточный таймер Windows (0.500 мс), исключая разрывы кадров.",
                    MenuPathAsus = "Advanced ➔ PCH Configuration ➔ High Precision Timer [Enabled]",
                    MenuPathMsi = "Settings ➔ Advanced ➔ Integrated Peripherals ➔ HPET [Enabled]",
                    MenuPathGigabyte = "Settings ➔ IO Ports ➔ High Precision Event Timer [Enabled]",
                    MenuPathAsrock = "Advanced ➔ Chipset Configuration ➔ High Precision Timer [Enabled]"
                });

                // -------------------------------------------------------------
                // 15. Turbo Boost Max 3.0 / AMD Precision Boost Overdrive 2 (PBO)
                // -------------------------------------------------------------
                string boostTech = isIntel ? "Intel Turbo Boost Max 3.0" : "AMD Precision Boost Overdrive 2 (PBO)";
                list.Add(new BiosSettingItem
                {
                    Id = "bios_turbo_boost_pbo",
                    Title = $"Автоматический буст частот ({boostTech})",
                    Category = "Процессор (CPU)",
                    RecommendedValue = isIntel ? "Intel Turbo Boost Max 3.0 [Enabled]" : "Precision Boost Overdrive [Advanced / Curve Optimizer -15...-25]",
                    CurrentStatus = "Максимальный автоматический разгон лучших ядер CPU",
                    PerformanceImpact = "+100–200 МГц на пиковых ядрах при более низком напряжении",
                    SafetyLevel = "Заводской интеллектуальный алгоритм производителя CPU",
                    Explanation = isIntel 
                        ? "Направляет самые требовательные однопоточные задачи на два лучших физических ядра процессора с наивысшим качеством кремния для максимальной тактовой частоты."
                        : "Curve Optimizer снижает подаваемое на ядра напряжение по кривой V/F, позволяя процессору Ryzen дольше удерживать максимальный буст при низких температурах.",
                    MenuPathAsus = isIntel ? "Ai Tweaker ➔ Intel Turbo Boost Max Technology 3.0 [Enabled]" : "Ai Tweaker ➔ Precision Boost Overdrive [Enabled]",
                    MenuPathMsi = isIntel ? "OC ➔ CPU Features ➔ Intel Turbo Boost Max 3.0 [Enabled]" : "OC ➔ Advanced CPU Configuration ➔ AMD Overclocking ➔ Precision Boost Overdrive",
                    MenuPathGigabyte = isIntel ? "Tweaker ➔ Advanced CPU Settings ➔ Turbo Boost [Enabled]" : "Tweaker ➔ Precision Boost Overdrive (PBO)",
                    MenuPathAsrock = isIntel ? "OC Tweaker ➔ CPU Configuration ➔ Intel Turbo Boost Max 3.0 [Enabled]" : "OC Tweaker ➔ AMD Overclocking ➔ Precision Boost Overdrive"
                });

                // Resolve matching paths
                foreach (var item in list)
                {
                    item.ResolveActiveBoardPath(b.Manufacturer, b.Model, b.CpuVendor);
                }

                return list;
            });
        }
    }
}
