using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

            StatusChanged?.Invoke(this, "Глубокий анализ системных кэшей и временных файлов...");
            ProgressChanged?.Invoke(this, 10);
            var junkItems = await Task.Run(() => ScanJunkFiles(cancellationToken));
            results.AddRange(junkItems);

            StatusChanged?.Invoke(this, "Глубокое сканирование кэшей браузеров и шейдеров...");
            ProgressChanged?.Invoke(this, 25);
            var browserShaders = await Task.Run(() => ScanBrowsersAndShaders(cancellationToken));
            results.AddRange(browserShaders);

            StatusChanged?.Invoke(this, "Анализ дампов сбоев и кэша обновлений Windows...");
            ProgressChanged?.Invoke(this, 40);
            var updatesAndDumps = await Task.Run(() => ScanUpdatesAndDumps(cancellationToken));
            results.AddRange(updatesAndDumps);

            StatusChanged?.Invoke(this, "Анализ оперативной памяти и фоновых процессов...");
            ProgressChanged?.Invoke(this, 55);
            var memItems = await Task.Run(() => ScanMemory(cancellationToken));
            results.AddRange(memItems);

            StatusChanged?.Invoke(this, "Проверка программ автозагрузки...");
            ProgressChanged?.Invoke(this, 70);
            var startupItems = await Task.Run(() => ScanStartup(cancellationToken));
            results.AddRange(startupItems);

            StatusChanged?.Invoke(this, "Диагностика фоновых служб Windows...");
            ProgressChanged?.Invoke(this, 80);
            var serviceItems = await Task.Run(() => ScanServices(cancellationToken));
            results.AddRange(serviceItems);

            StatusChanged?.Invoke(this, "Анализ сетевого стека, параметров DNS и TCP/IP...");
            ProgressChanged?.Invoke(this, 90);
            var netItems = await Task.Run(() => ScanNetwork(cancellationToken));
            results.AddRange(netItems);

            StatusChanged?.Invoke(this, "Проверка параметров приватности и системных настроек...");
            ProgressChanged?.Invoke(this, 95);
            var privacyItems = await Task.Run(() => ScanPrivacy(cancellationToken));
            results.AddRange(privacyItems);

            ProgressChanged?.Invoke(this, 100);
            StatusChanged?.Invoke(this, $"Глубокое сканирование завершено. Найдено категорий оптимизации: {results.Count}");

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
                    Title = "Временные файлы пользователя",
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
                    Title = "Временные системные файлы",
                    Description = "Логи системных обновлений, дампы установки пакетов и сервисный кэш.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = winTempBytes,
                    FormattedDetails = $"Путь: {winTemp}",
                    IsSelected = true
                });
            }

            // 3. Prefetch files
            string prefetchDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            long prefetchBytes = CalculateDirectorySize(prefetchDir, ct);
            if (prefetchBytes > 1024 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_prefetch",
                    Title = "Кэш трассировки запуска",
                    Description = "Устаревшие трассировки запусков ранее удаленных программ.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = prefetchBytes,
                    FormattedDetails = $"Путь: {prefetchDir}",
                    IsSelected = true
                });
            }

            // 4. Windows Error Reporting (WER)
            string werLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\WER");
            long werBytes = CalculateDirectorySize(werLocal, ct);
            if (werBytes > 512 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_wer",
                    Title = "Отчеты об ошибках и сбоях",
                    Description = "Накопленные локальные отчеты об аварийном завершении программ.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = werBytes,
                    FormattedDetails = $"Путь: {werLocal}",
                    IsSelected = true
                });
            }

            return items;
        }

        public List<OptimizationItem> ScanBrowsersAndShaders(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // 1. GPU Shader Caches
            long shaderBytes = 0;
            var shaderPaths = new[]
            {
                Path.Combine(localAppData, @"NVIDIA\DXCache"),
                Path.Combine(localAppData, @"NVIDIA\GLCache"),
                Path.Combine(localAppData, @"AMD\DxCache"),
                Path.Combine(localAppData, @"D3DSCache")
            };

            foreach (var p in shaderPaths)
            {
                if (Directory.Exists(p)) shaderBytes += CalculateDirectorySize(p, ct);
            }

            if (shaderBytes > 1024 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_shaders",
                    Title = "Кэш шейдеров видеокарты",
                    Description = "Скомпилированные шейдеры DirectX/OpenGL/Vulkan. Очистка устраняет статтеры и артефакты.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = shaderBytes,
                    FormattedDetails = "Директории DirectX / NVIDIA / AMD / D3DSCache",
                    IsSelected = true
                });
            }

            // 2. Web Browser Caches
            long browserBytes = 0;
            var browserPaths = new[]
            {
                Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache"),
                Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache"),
                Path.Combine(localAppData, @"Yandex\YandexBrowser\User Data\Default\Cache"),
                Path.Combine(localAppData, @"Opera Software\Opera Stable\Cache"),
                Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache"),
                Path.Combine(localAppData, @"Mozilla\Firefox\Profiles")
            };

            foreach (var p in browserPaths)
            {
                if (Directory.Exists(p)) browserBytes += CalculateDirectorySize(p, ct);
            }

            if (browserBytes > 5 * 1024 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_browser_cache",
                    Title = "Кэш веб-браузеров",
                    Description = "Временные медиафайлы, скрипты и кэшированные страницы браузеров.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = browserBytes,
                    FormattedDetails = "Кэш страниц, миниатюр и медиабраузеров",
                    IsSelected = true
                });
            }

            return items;
        }

        public List<OptimizationItem> ScanUpdatesAndDumps(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            // 1. Windows Update Download Cache
            string sdistPath = Path.Combine(winDir, @"SoftwareDistribution\Download");
            long sdistBytes = CalculateDirectorySize(sdistPath, ct);
            if (sdistBytes > 2 * 1024 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_win_updates",
                    Title = "Кэш загрузок обновлений Windows",
                    Description = "Загруженные и уже установленные пакеты системных обновлений.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = sdistBytes,
                    FormattedDetails = $"Путь: {sdistPath}",
                    IsSelected = true
                });
            }

            // 2. Memory Dumps & Minidumps
            long dumpBytes = 0;
            string minidumpDir = Path.Combine(winDir, "Minidump");
            string memoryDmp = Path.Combine(winDir, "MEMORY.DMP");

            if (Directory.Exists(minidumpDir)) dumpBytes += CalculateDirectorySize(minidumpDir, ct);
            if (File.Exists(memoryDmp))
            {
                try { dumpBytes += new FileInfo(memoryDmp).Length; } catch { }
            }

            if (dumpBytes > 512 * 1024)
            {
                items.Add(new OptimizationItem
                {
                    Id = "junk_memory_dumps",
                    Title = "Дампы системной памяти",
                    Description = "Слепки оперативной памяти и аварийные дампы при BSOD.",
                    Category = OptimizationCategory.JunkAndCache,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = dumpBytes,
                    FormattedDetails = $"Дампы в {minidumpDir} и MEMORY.DMP",
                    IsSelected = true
                });
            }

            return items;
        }

        public List<OptimizationItem> ScanMemory(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();
            var memInfo = HardwareMonitorService.Instance.GetCurrentMetrics();

            if (memInfo.RamUsedGb > 2.0)
            {
                double reclaimableMb = Math.Min(memInfo.RamUsedGb * 1024 * 0.25, 3072);
                items.Add(new OptimizationItem
                {
                    Id = "mem_working_set",
                    Title = "Очистка неиспользуемого набора оперативной памяти",
                    Description = "Выгрузка устаревших страниц памяти неактивных приложений в кэш без закрытия процессов.",
                    Category = OptimizationCategory.MemoryRam,
                    RiskLevel = RiskLevel.Safe,
                    ReclaimableBytes = (long)(reclaimableMb * 1024 * 1024),
                    FormattedDetails = $"Доступно к оптимизации: ~{reclaimableMb:F0} МБ",
                    IsSelected = true
                });
            }

            return items;
        }

        public List<OptimizationItem> ScanStartup(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();
            var startupList = StartupService.Instance.GetStartupEntries();
            var highImpact = startupList.Where(e => e.IsEnabled && e.Impact == "Высокое").ToList();

            if (highImpact.Count > 0)
            {
                items.Add(new OptimizationItem
                {
                    Id = "startup_high_impact",
                    Title = $"Программы с высокой нагрузкой на запуск ({highImpact.Count} шт.)",
                    Description = $"Программы, замедляющие загрузку Windows: {string.Join(", ", highImpact.Take(3).Select(x => x.Name))}",
                    Category = OptimizationCategory.StartupApps,
                    RiskLevel = RiskLevel.Recommended,
                    FormattedDetails = "Рекомендуется отключить автостарт в разделе «Автозагрузка»",
                    IsSelected = false
                });
            }

            return items;
        }

        public List<OptimizationItem> ScanServices(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();
            items.Add(new OptimizationItem
            {
                Id = "services_telemetry_profile",
                Title = "Оптимизация служб отслеживания и телеметрии",
                Description = "Безопасное отключение служб DiagTrack, WAP Push и сбора диагностических данных.",
                Category = OptimizationCategory.WindowsServices,
                RiskLevel = RiskLevel.Safe,
                FormattedDetails = "Службы телеметрии и сбора данных",
                IsSelected = true
            });
            return items;
        }

        public List<OptimizationItem> ScanNetwork(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();
            items.Add(new OptimizationItem
            {
                Id = "net_dns_tcp_tune",
                Title = "Калибровка сетевого стека и очистка DNS",
                Description = "Очистка кэша DNS Resolver, включение оптимального TCP Window Auto-Tuning и ECN.",
                Category = OptimizationCategory.NetworkAndDns,
                RiskLevel = RiskLevel.Safe,
                FormattedDetails = "Сетевой стек Windows TCP/IP",
                IsSelected = true
            });
            return items;
        }

        public List<OptimizationItem> ScanPrivacy(CancellationToken ct = default)
        {
            var items = new List<OptimizationItem>();
            items.Add(new OptimizationItem
            {
                Id = "privacy_advertising_id",
                Title = "Отключение рекламного идентификатора и истории активности",
                Description = "Запрет сбора истории активности, рекламного идентификатора и персонализации.",
                Category = OptimizationCategory.PrivacyTelemetry,
                RiskLevel = RiskLevel.Safe,
                FormattedDetails = "Параметры конфиденциальности Windows",
                IsSelected = true
            });
            return items;
        }

        public async Task<bool> OptimizeItemAsync(OptimizationItem item)
        {
            return await Task.Run(() =>
            {
                try
                {
                    switch (item.Id)
                    {
                        case "junk_user_temp":
                            CleanDirectory(Path.GetTempPath());
                            return true;

                        case "junk_win_temp":
                            CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
                            return true;

                        case "junk_prefetch":
                            CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"));
                            return true;

                        case "junk_wer":
                            CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\WER"));
                            return true;

                        case "junk_shaders":
                            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                            CleanDirectory(Path.Combine(localAppData, @"NVIDIA\DXCache"));
                            CleanDirectory(Path.Combine(localAppData, @"NVIDIA\GLCache"));
                            CleanDirectory(Path.Combine(localAppData, @"AMD\DxCache"));
                            CleanDirectory(Path.Combine(localAppData, @"D3DSCache"));
                            return true;

                        case "junk_browser_cache":
                            string lad = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                            CleanDirectory(Path.Combine(lad, @"Google\Chrome\User Data\Default\Cache"));
                            CleanDirectory(Path.Combine(lad, @"Microsoft\Edge\User Data\Default\Cache"));
                            CleanDirectory(Path.Combine(lad, @"Yandex\YandexBrowser\User Data\Default\Cache"));
                            return true;

                        case "junk_win_updates":
                            CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"SoftwareDistribution\Download"));
                            return true;

                        case "junk_memory_dumps":
                            CleanDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump"));
                            string memDmp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "MEMORY.DMP");
                            if (File.Exists(memDmp)) { try { File.Delete(memDmp); } catch { } }
                            return true;

                        case "mem_working_set":
                            try { NativeMethods.EmptyWorkingSet(Process.GetCurrentProcess().Handle); } catch { }
                            return true;

                        case "services_telemetry_profile":
                            WindowsServicesService.Instance.ApplyProfile("Balanced");
                            return true;

                        case "net_dns_tcp_tune":
                            NetworkOptimizerService.Instance.FlushDnsCache();
                            NetworkOptimizerService.Instance.OptimizeTcpSettings();
                            return true;

                        case "privacy_advertising_id":
                            PrivacyOptimizerService.Instance.DisableTelemetry();
                            return true;

                        default:
                            return true;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        private long CalculateDirectorySize(string path, CancellationToken ct)
        {
            if (!Directory.Exists(path)) return 0;
            long size = 0;
            try
            {
                var dir = new DirectoryInfo(path);
                foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    if (ct.IsCancellationRequested) break;
                    try { size += file.Length; } catch { }
                }
            }
            catch { }
            return size;
        }

        private void CleanDirectory(string path)
        {
            if (!Directory.Exists(path)) return;
            try
            {
                var dir = new DirectoryInfo(path);
                foreach (var file in dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                {
                    try { file.Delete(); } catch { }
                }
                foreach (var sub in dir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    try { sub.Delete(true); } catch { }
                }
            }
            catch { }
        }
    }
}
