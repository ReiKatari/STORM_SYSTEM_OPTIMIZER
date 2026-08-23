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
        public string Manufacturer { get; set; } = "ASUS / MSI / Gigabyte";
        public string Model { get; set; } = "B650 / Z790 Gaming Motherboard";
        public string BiosVersion { get; set; } = "UEFI 2.0";
        public string BiosReleaseDate { get; set; } = "2024-2026";
        public string CpuArchitecture { get; set; } = "x64 AMD / Intel";
        public string CpuName { get; set; } = "High Performance Processor";
        public string CpuVendor { get; set; } = "Intel";
        public bool IsUefiBoot { get; set; } = true;
        public bool IsSecureBootEnabled { get; set; } = true;
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

            try
            {
                using var procSearcher = new ManagementObjectSearcher("SELECT Name, Manufacturer FROM Win32_Processor");
                foreach (ManagementObject obj in procSearcher.Get())
                {
                    info.CpuName = obj["Name"]?.ToString()?.Trim() ?? "Processor";
                    string mfg = obj["Manufacturer"]?.ToString()?.Trim() ?? "";
                    info.CpuVendor = (mfg.Contains("AMD", StringComparison.OrdinalIgnoreCase) || info.CpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase)) ? "AMD" : "Intel";
                    info.CpuArchitecture = $"x64 {info.CpuVendor} Architecture";
                    break;
                }
            }
            catch { }

            try
            {
                using var boardSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
                foreach (ManagementObject obj in boardSearcher.Get())
                {
                    info.Manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "ASUS";
                    info.Model = obj["Product"]?.ToString()?.Trim() ?? "Gaming Series Motherboard";
                    break;
                }
            }
            catch { }

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
                var board = GetMotherboardBiosInfo();
                var list = new List<BiosSettingItem>
                {
                    new BiosSettingItem
                    {
                        Id = "bios_xmp_expo",
                        Title = "Профиль памяти XMP 2.0/3.0 / AMD EXPO / DOCP",
                        Category = "Память (RAM)",
                        RecommendedValue = "Profile 1 (Включено)",
                        CurrentStatus = "Критически важно для максимального FPS",
                        PerformanceImpact = "+10–25% к FPS в играх и скорости ОЗУ",
                        SafetyLevel = "100% Заводской профиль (JEDEC/Intel/AMD)",
                        Explanation = "По умолчанию оперативная память работает на базовой низкой частоте (2133/4800 МГц). Включение профиля XMP/EXPO активирует паспортную частоту (3200, 3600, 6000+ МГц) и сжатые тайминги без ручного подбора напряжений.",
                        MenuPathAsus = "Ai Tweaker ➔ Ai Overclock Tuner [XMP I / DOCP / EXPO]",
                        MenuPathMsi = "OC (Overclocking) ➔ Extreme Memory Profile (X.M.P.) [Enabled] / A-XMP [Profile 1]",
                        MenuPathGigabyte = "Tweaker ➔ Extreme Memory Profile (X.M.P.) [Profile1] / EXPO [Profile1]",
                        MenuPathAsrock = "OC Tweaker ➔ DRAM Timing Configuration ➔ Load XMP Setting [Profile 1]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_rebar",
                        Title = "Resizable BAR и Above 4G Decoding (ReBAR)",
                        Category = "Видеокарта (GPU)",
                        RecommendedValue = "Above 4G [Enabled] + ReBAR [Auto/Enabled]",
                        CurrentStatus = "Рекомендуется для современных видеокарт",
                        PerformanceImpact = "+5–15% прироста в играх Direct3D 12 и Vulkan",
                        SafetyLevel = "Официальный стандарт PCI-SIG",
                        Explanation = "Позволяет центральному процессору адресовать весь массив видеопамяти VRAM видеокарты единым блоком вместо мелких пакетов по 256 МБ. Устраняет задержки рендеринга и ускоряет DirectStorage.",
                        MenuPathAsus = "Advanced ➔ PCI Subsystem Settings ➔ Above 4G Decoding [Enabled] ➔ Re-Size BAR Support [Auto]",
                        MenuPathMsi = "Settings ➔ Advanced ➔ PCIe / PCI Subsystem Settings ➔ Re-Size BAR Support [Enabled]",
                        MenuPathGigabyte = "Settings ➔ IO Ports ➔ Above 4G Decoding [Enabled] ➔ Re-Size BAR Support [Auto]",
                        MenuPathAsrock = "Advanced ➔ Chipset Configuration ➔ Above 4G Decoding [Enabled] ➔ Re-Size BAR Support [Auto]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_csm_uefi",
                        Title = "Отключение CSM (Compatibility Support Module) и UEFI GOP",
                        Category = "Загрузка и Безопасность",
                        RecommendedValue = "CSM Support [Disabled] / OS Type [Windows UEFI]",
                        CurrentStatus = "Рекомендуется для быстрого старта",
                        PerformanceImpact = "Ускорение старта ПК на 5–8 секунд, включение Resizable BAR",
                        SafetyLevel = "100% Безопасно (Стандарт Windows 11)",
                        Explanation = "CSM — это режим эмуляции устаревшего BIOS 90-х годов. Отключение CSM активирует графический драйвер GOP видеокарты, сокращает время инициализации устройств при старте и разблокирует чистый UEFI режим.",
                        MenuPathAsus = "Boot ➔ CSM (Compatibility Support Module) ➔ Launch CSM [Disabled]",
                        MenuPathMsi = "Settings ➔ Advanced ➔ Windows OS Configuration ➔ BIOS UEFI/CSM Mode [UEFI]",
                        MenuPathGigabyte = "Boot ➔ CSM Support [Disabled]",
                        MenuPathAsrock = "Boot ➔ CSM (Compatibility Support Module) [Disabled]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_pcie_speed",
                        Title = "Фиксация скорости PCIe Link Speed (Gen 4.0 / Gen 5.0)",
                        Category = "Шина PCIe и накопители",
                        RecommendedValue = "PCIe x16 Link Speed [Gen4 / Gen5]",
                        CurrentStatus = "Устранение микрофризов энергосбережения",
                        PerformanceImpact = "Стабилизация 0.1% Low FPS и скорости NVMe SSD",
                        SafetyLevel = "100% Безопасно (Фиксация штатной скорости)",
                        Explanation = "В режиме 'Auto' контроллер PCIe постоянно переключает линии между Gen 1/2 и Gen 4/5 в моменты смены нагрузок, что может вызывать микро-статтеры. Принудительная установка Gen 4 или Gen 5 фиксирует максимальную пропускную способность.",
                        MenuPathAsus = "Advanced ➔ System Agent (SA) Config / AMD PBS ➔ PCIEX16 Link Speed [Gen4]",
                        MenuPathMsi = "Settings ➔ Advanced ➔ PCIe / PCI Subsystem ➔ PCIe x16 Slot Speed [Gen4/Gen5]",
                        MenuPathGigabyte = "Settings ➔ Miscellaneous ➔ PCIe Slot Configuration [Gen4]",
                        MenuPathAsrock = "Advanced ➔ Chipset Configuration ➔ PCIE1 Link Speed [Gen4]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_speedshift_pbo",
                        Title = "Intel Speed Shift (HWP) / AMD Core Performance Boost",
                        Category = "Процессор (CPU)",
                        RecommendedValue = "Включено (Enabled / Aggressive)",
                        CurrentStatus = "Аппаратное переключение частот за 1 мс",
                        PerformanceImpact = "Моментальный отклик интерфейса и максимальный Turbo Boost",
                        SafetyLevel = "Штатная технология Intel/AMD",
                        Explanation = "Позволяет аппаратному контроллеру процессора менять частоту ядер за 1 мс напрямую на уровне кремния, минуя медленный программный планировщик Windows (который тратит до 30 мс).",
                        MenuPathAsus = "Advanced ➔ CPU Configuration ➔ CPU - Power Management Control ➔ Intel Speed Shift Technology [Enabled]",
                        MenuPathMsi = "OC ➔ CPU Features ➔ Intel Speed Shift Technology [Enabled] / AMD Precision Boost [Enabled]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ Speed Shift [Enabled] / Core Performance Boost [Auto]",
                        MenuPathAsrock = "Advanced ➔ CPU Configuration ➔ Intel Speed Shift Technology [Enabled]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_cstates",
                        Title = "Оптимизация Package C-State Limit (Энергосбережение)",
                        Category = "Процессор (CPU)",
                        RecommendedValue = "Package C-State [C2 / C0] или [Enabled]",
                        CurrentStatus = "Стабилизация тактовой частоты ядер",
                        PerformanceImpact = "Снижение задержки DPC Latency и инпут-лага в играх",
                        SafetyLevel = "100% Безопасно (Без перегрева)",
                        Explanation = "Глубокие состояния сна процессора (C6/C7/C8) экономят милливатты энергии, но вызывают задержку пробуждения ядер при резком начале движения в играх. Ограничение глубокого сна стабилизирует тайминги кадров.",
                        MenuPathAsus = "Advanced ➔ CPU Configuration ➔ CPU - Power Management ➔ Package C-State Limit [C2 / Auto]",
                        MenuPathMsi = "OC ➔ CPU Features ➔ Package C-State Limit [Auto / C2]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ Package C-State Limit [Auto]",
                        MenuPathAsrock = "Advanced ➔ CPU Configuration ➔ Package C State Support [Enabled]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_subtimings_trfc",
                        Title = "Вторичные тайминги ОЗУ (tRFC, tFAW, tREFI) для стабилизации фреймтайма",
                        Category = "Память (RAM)",
                        RecommendedValue = "tRFC [Тайминг под чип (Samsung/Hynix/Micron)] / tREFI [65535]",
                        CurrentStatus = "Устранение микростаттеров (0.1% Low FPS)",
                        PerformanceImpact = "+15–30% к стабильности минимального фреймтайма",
                        SafetyLevel = "100% Безопасно при корректном подборе под чипы памяти",
                        Explanation = "Тайминг tRFC (Refresh Cycle Time) определяет интервалы регенерации ячеек памяти. Снижение tRFC сокращает паузы ожидания доступа к ОЗУ контроллером памяти процессора, ликвидируя 90% микрофризов при резком повороте камеры в играх.",
                        MenuPathAsus = "Ai Tweaker ➔ DRAM Timing Control ➔ Secondary Timings ➔ tRFC / tFAW",
                        MenuPathMsi = "OC ➔ Advanced DRAM Configuration ➔ Main & Secondary Timings ➔ tRFC1/2/4",
                        MenuPathGigabyte = "Tweaker ➔ Advanced Memory Settings ➔ Standard Timing Control ➔ tRFC",
                        MenuPathAsrock = "OC Tweaker ➔ DRAM Timing Configuration ➔ Refresh Recovery Time (tRFC)"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_gear1_cr",
                        Title = "Memory Gear 1 Mode и Command Rate 1T",
                        Category = "Память (RAM)",
                        RecommendedValue = "Gear Mode [Gear 1] / Command Rate [1T]",
                        CurrentStatus = "Синхронная работа контроллера памяти 1:1",
                        PerformanceImpact = "Снижение задержки памяти RAM на 6–10 нс, рост FPS",
                        SafetyLevel = "100% Стабильно для частот до DDR4-3600 / DDR5-6000",
                        Explanation = "Режим Gear 1 обеспечивает синхронную работу контроллера памяти процессора на полной частоте без деления на 2, что дает минимальный пинг и задержку доступа к ОЗУ в играх.",
                        MenuPathAsus = "Ai Tweaker ➔ Memory Controller : DRAM Frequency Ratio [1:1 / Gear 1]",
                        MenuPathMsi = "OC ➔ CPU IMC : DRAM Frequency [1:1 (Gear 1)]",
                        MenuPathGigabyte = "Tweaker ➔ Memory Gear Mode [Gear 1]",
                        MenuPathAsrock = "OC Tweaker ➔ DRAM Timing Control ➔ Gear Mode [Gear 1]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_mcr_ddr5",
                        Title = "Memory Context Restore (Мгновенный старт ПК на DDR5)",
                        Category = "Память (RAM)",
                        RecommendedValue = "Memory Context Restore [Enabled] + Power Down [Enabled]",
                        CurrentStatus = "Ускорение холодного старта ПК в 4–5 раз",
                        PerformanceImpact = "Сокращение времени загрузки Windows с 45 сек до 9 сек",
                        SafetyLevel = "Официальная функция AMD AGESA и Intel",
                        Explanation = "Сохраняет карту калибровки сигналов оперативной памяти DDR5 в энергонезависимой памяти BIOS, пропуская долгую тренировку таймингов ОЗУ при каждом включении компьютера.",
                        MenuPathAsus = "Ai Tweaker ➔ DRAM Timing Control ➔ Memory Context Restore [Enabled]",
                        MenuPathMsi = "OC ➔ Advanced DRAM Configuration ➔ Memory Context Restore [Enabled]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced Memory Settings ➔ Memory Context Restore [Enabled]",
                        MenuPathAsrock = "OC Tweaker ➔ DRAM Configuration ➔ Memory Context Restore [Enabled]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_undervolt_pbo",
                        Title = "Андервольтинг CPU (AMD PBO Curve Optimizer / Intel Lite Load)",
                        Category = "Процессор (CPU)",
                        RecommendedValue = "AMD Curve Optimizer [-15..-25 All Cores] / Intel CPU Lite Load [Mode 5..7]",
                        CurrentStatus = "Снижение температур на 8–15°C и повышение частот",
                        PerformanceImpact = "Увеличение времени буста CPU до максимальных частот без троттлинга",
                        SafetyLevel = "100% Безопасно (Снижение напряжения питания)",
                        Explanation = "Тонкая настройка кривой напряжений процессора позволяет чипу работать на более высоких частотах при меньшем нагреве и потреблении энергии.",
                        MenuPathAsus = "Ai Tweaker ➔ Precision Boost Overdrive ➔ Curve Optimizer ➔ All Cores [Negative] ➔ Magnitude [15-20]",
                        MenuPathMsi = "OC ➔ DigitALL Power ➔ CPU Lite Load [Mode 5]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced Voltage Settings ➔ CPU Vcore Offset [-0.050V]",
                        MenuPathAsrock = "OC Tweaker ➔ CPU Core/Cache Voltage ➔ Offset Mode [-50mV]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_thread_director",
                        Title = "Приоритет ядер Intel Thread Director / CPPC Preferred Cores",
                        Category = "Процессор (CPU)",
                        RecommendedValue = "CPPC [Enabled] / CPPC Preferred Cores [Enabled]",
                        CurrentStatus = "Маршрутизация тяжелых игровых потоков на лучшие ядра",
                        PerformanceImpact = "Устранение ошибочной отправки игр на медленные E-ядра",
                        SafetyLevel = "Штатная технология Microsoft/Intel/AMD",
                        Explanation = "Аппаратно маркирует самые быстрые ядра кристалла процессора и передает эту карту диспетчеру Windows Thread Director, чтобы игровые процессы никогда не уходили на медленные ядра.",
                        MenuPathAsus = "Advanced ➔ CPU Configuration ➔ Collaborative Processor Performance Control (CPPC) [Enabled]",
                        MenuPathMsi = "OC ➔ CPU Features ➔ CPPC [Enabled] / CPPC Preferred Cores [Enabled]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ CPPC [Enabled]",
                        MenuPathAsrock = "Advanced ➔ CPU Configuration ➔ CPPC [Enabled]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_spread_spectrum",
                        Title = "Отключение Spread Spectrum (Фиксация BCLK 100.00 МГц)",
                        Category = "Процессор (CPU)",
                        RecommendedValue = "Spread Spectrum / Clock Spread [Disabled]",
                        CurrentStatus = "Стабилизация тактовой частоты шины процессора",
                        PerformanceImpact = "Устранение плавающей частоты CPU (99.2 ➔ 100.0 МГц) и дрожания таймеров",
                        SafetyLevel = "100% Безопасно для ПК",
                        Explanation = "Функция Spread Spectrum слегка модулирует базовую частоту шины (BCLK) для снижения электромагнитных помех при сертификации. Отключение фиксирует точную частоту 100.00 МГц и стабилизирует расчет тактов.",
                        MenuPathAsus = "Ai Tweaker ➔ SB Clock Spread Spectrum [Disabled] / VRM Spread Spectrum [Disabled]",
                        MenuPathMsi = "OC ➔ CPU Features ➔ BCLK Spread Spectrum [Disabled]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ Spread Spectrum [Disabled]",
                        MenuPathAsrock = "OC Tweaker ➔ Clock Spread Spectrum [Disabled]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_igpu_disable",
                        Title = "Отключение встроенного графического ядра (iGPU Multi-Monitor Disable)",
                        Category = "Видеокарта (GPU)",
                        RecommendedValue = "Internal Graphics / Primary Display [PEG/PCIe] / iGPU [Disabled]",
                        CurrentStatus = "Освобождение контроллера памяти и линий PCIe",
                        PerformanceImpact = "Снижение нагрева кристалла CPU на 3–6°C и освобождение 1–2 ГБ ОЗУ",
                        SafetyLevel = "100% Безопасно при наличии дискретной видеокарты",
                        Explanation = "Если у вас установлена дискретная видеокарта NVIDIA/AMD, отключение встроенного видеоядра процессора убирает арбитраж шины памяти и снижает энергопотребление кристалла CPU.",
                        MenuPathAsus = "Advanced ➔ System Agent (SA) Configuration ➔ Graphics Configuration ➔ iGPU Multi-Monitor [Disabled]",
                        MenuPathMsi = "Settings ➔ Advanced ➔ Integrated Graphics Configuration ➔ Initiate Graphic Adapter [PEG]",
                        MenuPathGigabyte = "Settings ➔ IO Ports ➔ Internal Graphics [Disabled]",
                        MenuPathAsrock = "Advanced ➔ Chipset Configuration ➔ Primary Graphics Adapter [PCI Express]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_tsv_iommu",
                        Title = "Отключение Intel VT-d / AMD-Vi IOMMU (для чисто игровых систем)",
                        Category = "Шина PCIe и накопители",
                        RecommendedValue = "Intel VT-d / AMD IOMMU [Disabled] (если не используются виртуалки)",
                        CurrentStatus = "Прямой доступ DMA для видеокарты и NVMe",
                        PerformanceImpact = "Снижение задержек прямого доступа к памяти DMA на 2–5%",
                        SafetyLevel = "100% Безопасно (При отсутствии использования виртуальных машин)",
                        Explanation = "IOMMU создает уровень трансляции виртуальных адресов для устройств PCIe при виртуализации. Отключение на ПК без виртуальных машин дает аппаратуре прямой доступ к физической памяти.",
                        MenuPathAsus = "Advanced ➔ System Agent (SA) Configuration ➔ VT-d [Disabled]",
                        MenuPathMsi = "OC ➔ CPU Features ➔ Intel VT-d / AMD-Vi [Disabled]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ VT-d / IOMMU [Disabled]",
                        MenuPathAsrock = "Advanced ➔ Chipset Configuration ➔ VT-d [Disabled]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_hpet_tsc",
                        Title = "Аппаратный счетчик Invariant TSC и High Precision Event Timer (HPET)",
                        Category = "Загрузка и безопасность",
                        RecommendedValue = "HPET [Disabled / Auto] (Использование аппаратного Invariant TSC)",
                        CurrentStatus = "Минимизация системного джиттера часов",
                        PerformanceImpact = "Устранение микродрожания кадров и рассинхрона видеопотока",
                        SafetyLevel = "100% Безопасно",
                        Explanation = "Современные процессоры используют аппаратный неизменяемый счетчик тактов Invariant TSC внутри каждого ядра. Старый таймер HPET на шине южного моста создает лишние задержки запросов времени.",
                        MenuPathAsus = "Advanced ➔ PCH Configuration ➔ High Precision Timer [Disabled]",
                        MenuPathMsi = "Settings ➔ Advanced ➔ Integrated Peripherals ➔ HPET [Disabled]",
                        MenuPathGigabyte = "Settings ➔ Miscellaneous ➔ High Precision Event Timer [Disabled]",
                        MenuPathAsrock = "Advanced ➔ Chipset Configuration ➔ HPET [Disabled]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_aspm",
                        Title = "PCIe ASPM Power Management (Энергосбережение шины)",
                        Category = "Шина PCIe и накопители",
                        RecommendedValue = "ASPM [Disabled] / Native ASPM [Disabled]",
                        CurrentStatus = "Максимальная отзывчивость NVMe и GPU",
                        PerformanceImpact = "Устранение просадок скорости NVMe SSD и задержек PCIe",
                        SafetyLevel = "100% Безопасно для стационарных ПК",
                        Explanation = "Отключение энергосбережения ASPM на стационарных ПК удерживает шины PCIe и NVMe в состоянии максимальной готовности (L0 State), ликвидируя задержки на пробуждение контроллера диска.",
                        MenuPathAsus = "Advanced ➔ Platform Misc Configuration ➔ PCI Express Native Power Management [Disabled]",
                        MenuPathMsi = "Settings ➔ Advanced ➔ Power Management Setup ➔ PCIe ASPM [Disabled]",
                        MenuPathGigabyte = "Settings ➔ Miscellaneous ➔ Native ASPM [Disabled]",
                        MenuPathAsrock = "Advanced ➔ Chipset Configuration ➔ ASPM Support [Disabled]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_fast_boot",
                        Title = "Fast Boot (Быстрая инициализация оборудования)",
                        Category = "Загрузка и безопасность",
                        RecommendedValue = "Fast Boot [Enabled]",
                        CurrentStatus = "Пропуск повторной самопроверки устройств",
                        PerformanceImpact = "Старт компьютера на 3–5 секунд быстрее",
                        SafetyLevel = "Штатная функция UEFI",
                        Explanation = "Пропускает повторное сканирование USB-портов и видеовыходов при включении, загружая операционную систему напрямую с основного накопителя.",
                        MenuPathAsus = "Boot ➔ Fast Boot [Enabled]",
                        MenuPathMsi = "Settings ➔ Boot ➔ Fast Boot [Enabled]",
                        MenuPathGigabyte = "Boot ➔ Fast Boot [Enabled]",
                        MenuPathAsrock = "Boot ➔ Fast Boot [Fast]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_fan_curves",
                        Title = "Калибровка Smart Fan 5 / Q-Fan Control (Охлаждение)",
                        Category = "Охлаждение и вентиляторы",
                        RecommendedValue = "PWM Mode / Silent Profile (100% при 75°C)",
                        CurrentStatus = "Бесшумность в простое и защита от троттлинга",
                        PerformanceImpact = "Предотвращение снижения частот видеокарты и процессора от перегрева",
                        SafetyLevel = "100% Защита от перегрева",
                        Explanation = "Настройка режима управления PWM (4-pin) вместо DC (3-pin) обеспечивает плавное регулирование оборотов кулера и бесшумную работу в браузере при сохранении запаса охлаждения в играх.",
                        MenuPathAsus = "Monitor ➔ Q-Fan Configuration ➔ CPU Q-Fan Control [PWM Mode] / Profile [Standard]",
                        MenuPathMsi = "Hardware Monitor ➔ Smart Fan Control [Enabled] ➔ Mode [PWM]",
                        MenuPathGigabyte = "Smart Fan 6 (F6) ➔ Temperature Source [CPU] ➔ Control Mode [PWM]",
                        MenuPathAsrock = "H/W Monitor ➔ CPU Fan 1 Setting [Customize / Silent]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_fan_hysteresis",
                        Title = "Hysteresis / Задержка вентиляторов Step-Up и Step-Down",
                        Category = "Охлаждение и вентиляторы",
                        RecommendedValue = "Fan Step-Up [0.2 sec] / Step-Down [1.0 - 2.0 sec]",
                        CurrentStatus = "Устранение резких завываний вентиляторов",
                        PerformanceImpact = "Бесшумная и плавная работа кулеров при кратковременных нагрузках",
                        SafetyLevel = "100% Безопасно для охлаждения",
                        Explanation = "Задержка снижения оборотов (Hysteresis) предотвращает постоянные резкие скачки скорости вентиляторов кулера при открытии вкладок браузера или запуске приложений.",
                        MenuPathAsus = "Monitor ➔ Q-Fan Configuration ➔ CPU Fan Step Up/Down Time [Level 1 - 2.0 sec]",
                        MenuPathMsi = "Hardware Monitor ➔ Fan Step Up Time [0.2s] / Fan Step Down Time [2.0s]",
                        MenuPathGigabyte = "Smart Fan 6 ➔ Temperature Interval / Smoothing [3]",
                        MenuPathAsrock = "H/W Monitor ➔ Fan Step-Down Delay [2.0s]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_tpm_secureboot",
                        Title = "TPM 2.0 (AMD fTPM / Intel PTT) и Secure Boot",
                        Category = "Загрузка и безопасность",
                        RecommendedValue = "Security Device Support [Enable] + Secure Boot [Enabled]",
                        CurrentStatus = "Полное соответствие требованиям Windows 11 и античитов (Vanguard/EAC)",
                        PerformanceImpact = "Аппаратная защита ядра и запуск современных соревновательных игр",
                        SafetyLevel = "100% Безопасно (Стандарт Microsoft)",
                        Explanation = "Активирует встроенный в процессор аппаратный криптопроцессор TPM 2.0 и проверку цифровых подписей загрузчика, что требуется для работы Windows 11, BitLocker и соревновательных античитов (Valorant, FACEIT, CoD).",
                        MenuPathAsus = "Advanced ➔ Trusted Computing ➔ Security Device Support [Enable] / AMD fTPM [Firmware TPM]",
                        MenuPathMsi = "Settings ➔ Security ➔ Trusted Computing ➔ Security Device Support [Enabled]",
                        MenuPathGigabyte = "Settings ➔ Miscellaneous ➔ Intel Platform Trust Tech (PTT) / AMD CPU fTPM [Enabled]",
                        MenuPathAsrock = "Advanced ➔ Security ➔ Intel PTT / AMD fTPM [Enabled]"
                    },
                    new BiosSettingItem
                    {
                        Id = "bios_virtualization",
                        Title = "Аппаратная виртуализация (Intel VT-x / AMD SVM)",
                        Category = "Процессор (CPU)",
                        RecommendedValue = "Intel Virtualization / SVM Mode [Enabled]",
                        CurrentStatus = "Необходимо для изоляции ядра и эмуляторов",
                        PerformanceImpact = "Аппаратное ускорение WSL2, Sandbox и защиты Windows Defender",
                        SafetyLevel = "100% Безопасно (Штатная инструкция CPU)",
                        Explanation = "Включает аппаратный гипервизор процессора для быстрой работы виртуальных машин, подсистемы Windows для Linux (WSL2), Google Play Games и защищенного ядра Windows 11.",
                        MenuPathAsus = "Advanced ➔ CPU Configuration ➔ Intel Virtualization Technology / SVM Mode [Enabled]",
                        MenuPathMsi = "OC ➔ CPU Features ➔ SVM Mode / Intel Virtualization [Enabled]",
                        MenuPathGigabyte = "Tweaker ➔ Advanced CPU Settings ➔ SVM Mode / VT-d [Enabled]",
                        MenuPathAsrock = "Advanced ➔ CPU Configuration ➔ SVM Mode [Enabled]"
                    }
                };

                foreach (var item in list)
                {
                    item.ResolveActiveBoardPath(board.Manufacturer, board.Model, board.CpuVendor);
                }

                return list;
            });
        }
    }
}
