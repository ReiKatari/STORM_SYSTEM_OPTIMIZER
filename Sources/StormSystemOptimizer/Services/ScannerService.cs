using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class ScannerService
    {
        private static ScannerService? _instance;
        public static ScannerService Instance => _instance ??= new ScannerService();

        public event EventHandler<int>? ProgressChanged;
        public event EventHandler<string>? StatusChanged;

        private ScannerService() { }

        public async Task<List<OptimizationItem>> ScanAllAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<OptimizationItem>();

            StatusChanged?.Invoke(this, "Анализ системных кэшей и временных файлов...");
            ProgressChanged?.Invoke(this, 10);
            var junkItems = await Task.Run(() => ScanJunkFiles(cancellationToken));
            results.AddRange(junkItems);

            StatusChanged?.Invoke(this, "Анализ оперативной памяти и фоновых процессов...");
            ProgressChanged?.Invoke(this, 30);
            var memItems = await Task.Run(() => ScanMemory(cancellationToken));
            results.AddRange(memItems);

            StatusChanged?.Invoke(this, "Проверка программ автозагрузки...");
            ProgressChanged?.Invoke(this, 45);
            var startupItems = await Task.Run(() => ScanStartup(cancellationToken));
            results.AddRange(startupItems);

            StatusChanged?.Invoke(this, "Диагностика фоновых служб Windows...");
            ProgressChanged?.Invoke(this, 60);
            var serviceItems = await Task.Run(() => ScanServices(cancellationToken));
            results.AddRange(serviceItems);

            StatusChanged?.Invoke(this, "Анализ сетевого стека и кэша DNS...");
            ProgressChanged?.Invoke(this, 75);
            var netItems = await Task.Run(() => ScanNetwork(cancellationToken));
            results.AddRange(netItems);

            StatusChanged?.Invoke(this, "Проверка параметров приватности и телеметрии...");
            ProgressChanged?.Invoke(this, 85);
            var privacyItems = await Task.Run(() => ScanPrivacy(cancellationToken));
            results.AddRange(privacyItems);

            StatusChanged?.Invoke(this, "Проверка оптимизации SSD, дисков и питания...");
            ProgressChanged?.Invoke(this, 95);
            var healthItems = await Task.Run(() => ScanSystemHealthAndPower(cancellationToken));
            results.AddRange(healthItems);

            ProgressChanged?.Invoke(this, 100);
            StatusChanged?.Invoke(this, $"Сканирование завершено. Найдено проблем: {results.Count}");

            return results;
        }

        public List<OptimizationItem> ScanJunkFiles(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();

            // 1. User Temp
            string userTemp = Path.GetTempPath();
            long userTempBytes = CalculateDirectorySize(userTemp, ct);
            if (userTempBytes > 1024 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_user_temp",
                    Title = "Временные файлы пользователя (User Temp)",
                    Description = "Кэш установок, временные файлы приложений и распаковщиков.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = userTempBytes,
                    FormattedDetails = $"Путь: {userTemp}",
                    IsSelected = true
                });
            }

            // 2. Windows Temp
            string winTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            long winTempBytes = CalculateDirectorySize(winTemp, ct);
            if (winTempBytes > 1024 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_win_temp",
                    Title = "Временные системные файлы (Windows Temp)",
                    Description = "Логи системных обновлений, дампы установки пакетов и сервисный кэш.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = winTempBytes,
                    FormattedDetails = $"Путь: {winTemp}",
                    IsSelected = true
                });
            }

            // 3. Windows Prefetch
            string prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            long prefetchBytes = CalculateDirectorySize(prefetch, ct);
            if (prefetchBytes > 2 * 1024 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_prefetch",
                    Title = "Кэш предварительной загрузки (Prefetch)",
                    Description = "Устаревшие индексы запуска удаленных или редко используемых программ.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = prefetchBytes,
                    FormattedDetails = $"Путь: {prefetch}",
                    IsSelected = true
                });
            }

            // 4. Crash Dumps & Windows Error Reporting
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string crashDumps = Path.Combine(localAppData, "CrashDumps");
            string werPath = Path.Combine(localAppData, "Microsoft", "Windows", "WER");
            long crashBytes = CalculateDirectorySize(crashDumps, ct) + CalculateDirectorySize(werPath, ct);
            if (crashBytes > 512 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_crash_dumps",
                    Title = "Отчеты об ошибках и дампы сбоев (Crash Dumps)",
                    Description = "Дампы памяти при завершении аварийных приложений и очереди WER.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = crashBytes,
                    FormattedDetails = "Каталоги WER и CrashDumps",
                    IsSelected = true
                });
            }

            // 5. Browser Caches (Edge, Chrome, Brave)
            long browserCacheBytes = 0;
            var cachePaths = new List<string>
            {
                Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache"),
                Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache"),
                Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache")
            };
            foreach (var p in cachePaths)
            {
                browserCacheBytes += CalculateDirectorySize(p, ct);
            }

            if (browserCacheBytes > 5 * 1024 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_browser_cache",
                    Title = "Кэш браузеров (Edge, Chrome, Chromium)",
                    Description = "Временные медиафайлы, скрипты и кэшированные страницы браузеров.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = browserCacheBytes,
                    FormattedDetails = "Кэш веб-ресурсов",
                    IsSelected = true
                });
            }

            // 6. Windows Delivery Optimization
            string softwareDist = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
            long deliveryBytes = CalculateDirectorySize(softwareDist, ct);
            if (deliveryBytes > 5 * 1024 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_delivery_cache",
                    Title = "Кэш обновлений Windows (SoftwareDistribution)",
                    Description = "Уже установленные пакеты обновлений Windows Update.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = deliveryBytes,
                    FormattedDetails = $"Путь: {softwareDist}",
                    IsSelected = true
                });
            }

            return items;
        }

        public List<OptimizationItem> ScanMemory(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();
            try
            {
                var memStatus = new NativeMethods.MEMORYSTATUSEX();
                memStatus.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.MEMORYSTATUSEX));
                if (NativeMethods.GlobalMemoryStatusEx(ref memStatus))
                {
                    double availGb = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                    long standbyEstBytes = (long)(memStatus.ullAvailPhys * 0.35);

                    if (memStatus.dwMemoryLoad > 40 && standbyEstBytes > 250 * 1024 * 1024)
                    {
                        items.Add(new OptimizationItem
                        {
                            Id = "mem_standby_purge",
                            Title = "Очистка кэша Standby и Working Set памяти",
                            Description = "Фоновые процессы удерживают память в неактивном кэше. Очистка освободит RAM для активных приложений и игр.",
                            Category = OptimizationCategory.MemoryRam,
                            RiskLevel = RiskLevel.Safe,
                            ReclaimableBytes = standbyEstBytes,
                            FormattedDetails = $"Текущая загрузка RAM: {memStatus.dwMemoryLoad}%",
                            IsSelected = true
                        });
                    }
                }
            }
            catch { }

            return items;
        }

        public List<OptimizationItem> ScanStartup(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();
            try
            {
                int highImpactCount = 0;
                var startupEntries = StartupService.Instance.GetStartupEntries();
                foreach (var entry in startupEntries)
                {
                    if (entry.IsEnabled && (entry.Impact == "Высокое" || entry.Impact == "Среднее"))
                    {
                        highImpactCount++;
                    }
                }

                if (highImpactCount > 0)
                {
                    items.Add(new OptimizationItem
                    {
                        Id = "startup_high_impact",
                        Title = $"Тяжелые программы в автозагрузке ({highImpactCount} шт.)",
                        Description = "Приложения, замедляющие запуск Windows и работающие в фоне без необходимости.",
                        Category = OptimizationCategory.StartupApps,
                        RiskLevel = RiskLevel.Recommended,
                        ReclaimableBytes = 0,
                        FormattedDetails = $"Обнаружено {highImpactCount} ресурсоемких приложений",
                        IsSelected = true
                    });
                }
            }
            catch { }

            return items;
        }

        public List<OptimizationItem> ScanServices(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();
            try
            {
                var candidates = WindowsServicesService.Instance.GetUnnecessaryServices();
                int runningBloatCount = candidates.Count(s => s.Status == "Работает");

                if (runningBloatCount > 0)
                {
                    items.Add(new OptimizationItem
                    {
                        Id = "services_telemetry_bloat",
                        Title = $"Фоновые телеметрические службы ({runningBloatCount} шт.)",
                        Description = "Службы сбора телеметрии, отчетов об ошибках и удаленного реестра, создающие фоновую нагрузку на процессор и диск.",
                        Category = OptimizationCategory.WindowsServices,
                        RiskLevel = RiskLevel.Recommended,
                        ReclaimableBytes = 0,
                        FormattedDetails = string.Join(", ", candidates.Take(4).Select(s => s.DisplayName)),
                        IsSelected = true
                    });
                }
            }
            catch { }

            return items;
        }

        public List<OptimizationItem> ScanNetwork(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();

            // DNS Cache
            items.Add(new OptimizationItem
            {
                Id = "net_dns_flush",
                Title = "Сброс системного кэша сопоставителя DNS",
                Description = "Очищает устаревшие DNS-записи, устраняет задержки открытия сайтов и сетевых подключений.",
                Category = OptimizationCategory.NetworkAndDns,
                RiskLevel = RiskLevel.Safe,
                ReclaimableBytes = 0,
                FormattedDetails = "Устранение сетевых задержек",
                IsSelected = true
            });

            // TCP AutoTuning
            items.Add(new OptimizationItem
            {
                Id = "net_tcp_autotune",
                Title = "Оптимизация TCP Window Auto-Tuning & Congestion",
                Description = "Включение алгоритма оптимального размера окна TCP для максимальной скорости и стабильности пинга.",
                Category = OptimizationCategory.NetworkAndDns,
                RiskLevel = RiskLevel.Recommended,
                ReclaimableBytes = 0,
                FormattedDetails = "netsh int tcp autotuning = normal",
                IsSelected = true
            });

            return items;
        }

        public List<OptimizationItem> ScanPrivacy(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
                object? telemetry = key?.GetValue("AllowTelemetry");
                bool isTelemetryEnabled = telemetry == null || Convert.ToInt32(telemetry) > 0;

                if (isTelemetryEnabled)
                {
                    items.Add(new OptimizationItem
                    {
                        Id = "privacy_telemetry_disable",
                        Title = "Отключение расширенной телеметрии и сбора диагностических данных",
                        Description = "Уменьшает сетевую активность в фоне и защищает конфиденциальность пользователя.",
                        Category = OptimizationCategory.PrivacyTelemetry,
                        RiskLevel = RiskLevel.Recommended,
                        ReclaimableBytes = 0,
                        FormattedDetails = "Параметр AllowTelemetry в реестре",
                        IsSelected = true
                    });
                }

                items.Add(new OptimizationItem
                {
                    Id = "privacy_advertising_id",
                    Title = "Отключение рекламного идентификатора и трекинга активности",
                    Description = "Отключает отслеживание интересов пользователя приложениями из Microsoft Store.",
                    Category = OptimizationCategory.PrivacyTelemetry,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = 0,
                    FormattedDetails = "AdvertisingInfo & User Activity History",
                    IsSelected = true
                });
            }
            catch { }

            return items;
        }

        public List<OptimizationItem> ScanSystemHealthAndPower(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();

            // SSD TRIM Check
            items.Add(new OptimizationItem
            {
                Id = "health_ssd_trim",
                Title = "Выполнение команды оптимизации SSD (TRIM)",
                Description = "Информирует SSD-накопитель о неиспользуемых блоках для предотвращения деградации скорости записи.",
                Category = OptimizationCategory.SystemHealth,
                RiskLevel = RiskLevel.Safe,
                ReclaimableBytes = 0,
                FormattedDetails = "Дефрагментация и TRIM диска C:",
                IsSelected = true
            });

            // Ultimate Performance Power Plan
            items.Add(new OptimizationItem
            {
                Id = "power_ultimate_plan",
                Title = "Активация плана электропитания «Максимальная производительность»",
                Description = "Устраняет задержки энергосбережения процессора и компонентов для максимального FPS и плавности.",
                Category = OptimizationCategory.PowerAndVisual,
                RiskLevel = RiskLevel.Recommended,
                ReclaimableBytes = 0,
                FormattedDetails = "PowerCfg Ultimate Performance",
                IsSelected = true
            });

            // UI Responsiveness Delay
            items.Add(new OptimizationItem
            {
                Id = "visual_menu_delay",
                Title = "Устранение задержки анимации меню и интерфейса",
                Description = "Снижает задержку отображения контекстных меню (MenuShowDelay с 400мс до 10мс).",
                Category = OptimizationCategory.PowerAndVisual,
                RiskLevel = RiskLevel.Safe,
                ReclaimableBytes = 0,
                FormattedDetails = "HKCU\\Control Panel\\Desktop\\MenuShowDelay",
                IsSelected = true
            });

            return items;
        }

        private long CalculateDirectorySize(string path, CancellationToken ct = default)
        {
            if (!Directory.Exists(path)) return 0;
            long size = 0;
            try
            {
                var dir = new DirectoryInfo(path);
                foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    if (ct.IsCancellationRequested) break;
                    try { size += file.Length; }
                    catch { }
                }
            }
            catch { }
            return size;
        }
    }
}
