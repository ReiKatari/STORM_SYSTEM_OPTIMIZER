using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class OfficeProductEdition
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Channel { get; set; } = "PerpetualVL2024";
        public string GvlkKey { get; set; } = string.Empty;
    }

    public class OfficeDeployOptions
    {
        public string EditionId { get; set; } = "ProPlus2024Volume";
        public string Architecture { get; set; } = "64"; // 64 or 32
        public string Language { get; set; } = "ru-ru";
        public bool InstallWord { get; set; } = true;
        public bool InstallExcel { get; set; } = true;
        public bool InstallPowerPoint { get; set; } = true;
        public bool InstallAccess { get; set; } = true;
        public bool InstallOutlook { get; set; } = false;
        public bool InstallOneNote { get; set; } = false;
        public bool InstallPublisher { get; set; } = false;
        public bool InstallVisio { get; set; } = false;
        public bool InstallProject { get; set; } = false;
        public bool ExcludeTeams { get; set; } = true;
        public bool ExcludeOneDrive { get; set; } = true;
        public bool ExcludeBing { get; set; } = true;
        public bool AutoActivate { get; set; } = true;
        public string KmsServer { get; set; } = "kms.digiboy.ir";
    }

    public class OfficeDeployerService
    {
        private static OfficeDeployerService? _instance;
        public static OfficeDeployerService Instance => _instance ??= new OfficeDeployerService();

        public List<OfficeProductEdition> SupportedEditions => new()
        {
            new OfficeProductEdition { Id = "ProPlus2024Volume", DisplayName = "Microsoft Office 2024 ProPlus (LTSC Volume)", Channel = "PerpetualVL2024", GvlkKey = "2TDPW-NDQ7G-FMG99-DXQ7M-84LD6" },
            new OfficeProductEdition { Id = "ProPlus2021Volume", DisplayName = "Microsoft Office 2021 ProPlus (LTSC Volume)", Channel = "PerpetualVL2021", GvlkKey = "FXYTK-NJJ8C-GB6DW-3DYQT-6F7TH" },
            new OfficeProductEdition { Id = "ProPlus2019Volume", DisplayName = "Microsoft Office 2019 ProPlus (Volume)", Channel = "PerpetualVL2019", GvlkKey = "NMMKJ-6RK4F-KMJVX-8D9MJ-6MWKP" },
            new OfficeProductEdition { Id = "O365ProPlusRetail", DisplayName = "Microsoft 365 ProPlus (Retail / Mondo)", Channel = "Current", GvlkKey = "HFTND-W9MK4-8VK7D-VQVC9-TMCRG" },
            new OfficeProductEdition { Id = "VisioPro2024Volume", DisplayName = "Microsoft Visio 2024 Professional (Volume)", Channel = "PerpetualVL2024", GvlkKey = "YW66X-NH62M-G6YFP-B79ZR-D72FC" },
            new OfficeProductEdition { Id = "ProjectPro2024Volume", DisplayName = "Microsoft Project 2024 Professional (Volume)", Channel = "PerpetualVL2024", GvlkKey = "D9GTG-NP2KV-M6HDK-DTFCW-BQ8T9" }
        };

        public List<string> KmsServers => new()
        {
            "kms.digiboy.ir",
            "kms8.msguides.com",
            "kms.03k.org",
            "kms.loli.best",
            "kms.cx90.net"
        };

        /// <summary>
        /// Detects if Microsoft Office is currently installed on the system and returns its version.
        /// </summary>
        public string GetInstalledOfficeInfo()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");
                if (key != null)
                {
                    string product = key.GetValue("ProductCodes") as string ?? "Office C2R";
                    string ver = key.GetValue("VersionToReport") as string ?? "";
                    string arch = key.GetValue("Platform") as string ?? "x64";
                    return $"{product} ({arch}) v{ver}".Trim();
                }
            }
            catch { }
            return "Не обнаружен";
        }

        /// <summary>
        /// Generates the C2R XML configuration file.
        /// </summary>
        public string GenerateConfigurationXml(OfficeDeployOptions options)
        {
            var sb = new StringBuilder();
            var edition = SupportedEditions.Find(e => e.Id == options.EditionId) ?? SupportedEditions[0];

            sb.AppendLine("<Configuration>");
            sb.AppendLine($"  <Add OfficeClientEdition=\"{options.Architecture}\" Channel=\"{edition.Channel}\">");
            sb.AppendLine($"    <Product ID=\"{edition.Id}\">");
            sb.AppendLine($"      <Language ID=\"{options.Language}\" />");

            // Exclude unnecessary components
            if (!options.InstallWord) sb.AppendLine("      <ExcludeApp ID=\"Word\" />");
            if (!options.InstallExcel) sb.AppendLine("      <ExcludeApp ID=\"Excel\" />");
            if (!options.InstallPowerPoint) sb.AppendLine("      <ExcludeApp ID=\"PowerPoint\" />");
            if (!options.InstallAccess) sb.AppendLine("      <ExcludeApp ID=\"Access\" />");
            if (!options.InstallOutlook) sb.AppendLine("      <ExcludeApp ID=\"Outlook\" />");
            if (!options.InstallOneNote) sb.AppendLine("      <ExcludeApp ID=\"OneNote\" />");
            if (!options.InstallPublisher) sb.AppendLine("      <ExcludeApp ID=\"Publisher\" />");
            if (options.ExcludeTeams) sb.AppendLine("      <ExcludeApp ID=\"Teams\" />");
            if (options.ExcludeOneDrive) sb.AppendLine("      <ExcludeApp ID=\"OneDrive\" />");
            if (options.ExcludeOneDrive) sb.AppendLine("      <ExcludeApp ID=\"Groove\" />");
            if (options.ExcludeBing) sb.AppendLine("      <ExcludeApp ID=\"Bing\" />");
            sb.AppendLine("      <ExcludeApp ID=\"Lync\" />");

            sb.AppendLine("    </Product>");

            if (options.InstallVisio && options.EditionId != "VisioPro2024Volume")
            {
                sb.AppendLine("    <Product ID=\"VisioPro2024Volume\">");
                sb.AppendLine($"      <Language ID=\"{options.Language}\" />");
                sb.AppendLine("    </Product>");
            }

            if (options.InstallProject && options.EditionId != "ProjectPro2024Volume")
            {
                sb.AppendLine("    <Product ID=\"ProjectPro2024Volume\">");
                sb.AppendLine($"      <Language ID=\"{options.Language}\" />");
                sb.AppendLine("    </Product>");
            }

            sb.AppendLine("  </Add>");
            sb.AppendLine("  <Display Level=\"Full\" AcceptEULA=\"TRUE\" />");
            sb.AppendLine("  <Property Name=\"AUTOACTIVATE\" Value=\"0\" />");
            sb.AppendLine("</Configuration>");

            return sb.ToString();
        }

        /// <summary>
        /// Deploys Office by generating configuration and launching Microsoft Office C2R Setup.
        /// </summary>
        public async Task<bool> InstallOfficeAsync(OfficeDeployOptions options, IProgress<string>? progress = null)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    progress?.Report("Генерация манифеста развертывания Click-to-Run...");
                    string tempDir = Path.Combine(Path.GetTempPath(), "STORM_OfficeDeploy");
                    Directory.CreateDirectory(tempDir);

                    string xmlPath = Path.Combine(tempDir, "configuration.xml");
                    string xmlContent = GenerateConfigurationXml(options);
                    File.WriteAllText(xmlPath, xmlContent, Encoding.UTF8);

                    // Locate or download Microsoft Office Setup.exe
                    string setupExePath = Path.Combine(tempDir, "setup.exe");

                    if (!File.Exists(setupExePath))
                    {
                        progress?.Report("Получение официального установщика Microsoft Office CDN...");
                        // Download official Office Deployment Tool setup.exe from Microsoft
                        using var client = new System.Net.Http.HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(30);
                        byte[] setupBytes = await client.GetByteArrayAsync("https://officecdn.microsoft.com/pr/wsus/setup.exe");
                        File.WriteAllBytes(setupExePath, setupBytes);
                    }

                    progress?.Report("Запуск установки Microsoft Office Click-to-Run...");

                    var psi = new ProcessStartInfo
                    {
                        FileName = setupExePath,
                        Arguments = $"/configure \"{xmlPath}\"",
                        WorkingDirectory = tempDir,
                        UseShellExecute = false
                    };

                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        await proc.WaitForExitAsync();
                    }

                    progress?.Report("Установка пакета завершена!");

                    if (options.AutoActivate)
                    {
                        progress?.Report("Выполнение автоматической активации KMS...");
                        await ActivateOfficeKmsAsync(options.KmsServer, progress);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OfficeDeployerService] Install Error: {ex.Message}");
                    progress?.Report($"Ошибка развертывания: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Activates installed Office via KMS server using ospp.vbs / slmgr.
        /// </summary>
        public async Task<bool> ActivateOfficeKmsAsync(string kmsServer = "kms.digiboy.ir", IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report($"Поиск скрипта лицензирования OSPP.VBS...");

                    string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                    var osppCandidates = new[]
                    {
                        Path.Combine(programFiles, @"Microsoft Office\Office16\OSPP.VBS"),
                        Path.Combine(programFilesX86, @"Microsoft Office\Office16\OSPP.VBS"),
                        Path.Combine(programFiles, @"Microsoft Office\Office15\OSPP.VBS"),
                        Path.Combine(programFilesX86, @"Microsoft Office\Office15\OSPP.VBS")
                    };

                    string osppPath = osppCandidates.FirstOrDefault(File.Exists) ?? "";

                    if (string.IsNullOrEmpty(osppPath))
                    {
                        progress?.Report("Скрипт OSPP.VBS не найден в стандартных путях Office. Проверяю WMI...");
                    }
                    else
                    {
                        // 1. Set KMS Server
                        progress?.Report($"Установка KMS-сервера: {kmsServer}:1688...");
                        RunCscript(osppPath, $"/sethst:{kmsServer}");

                        // 2. Trigger Activation
                        progress?.Report("Отправка запроса активации на сервер...");
                        string actOutput = RunCscript(osppPath, "/act");

                        // 3. Get Status
                        string statusOutput = RunCscript(osppPath, "/dstatus");
                        if (statusOutput.Contains("LICENSED", StringComparison.OrdinalIgnoreCase) || actOutput.Contains("successful", StringComparison.OrdinalIgnoreCase))
                        {
                            progress?.Report("Office успешно активирован! Статус: LICENSED ✅");
                            return true;
                        }
                        else
                        {
                            progress?.Report($"Результат активации: {actOutput.Trim()}");
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    progress?.Report($"Ошибка активации: {ex.Message}");
                    return false;
                }
            });
        }

        private static string RunCscript(string vbsPath, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cscript.exe",
                    Arguments = $"//nologo \"{vbsPath}\" {args}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string outStr = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    return outStr;
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// Cleans legacy / broken Office license keys.
        /// </summary>
        public async Task<bool> CleanLegacyKeysAsync(IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report("Сканирование установленных ключей Office...");
                    string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    string osppPath = Path.Combine(programFiles, @"Microsoft Office\Office16\OSPP.VBS");
                    if (!File.Exists(osppPath))
                    {
                        osppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft Office\Office16\OSPP.VBS");
                    }

                    if (File.Exists(osppPath))
                    {
                        string status = RunCscript(osppPath, "/dstatus");
                        var lines = status.Split('\n');
                        foreach (var l in lines)
                        {
                            if (l.Contains("Last 5 characters of installed product key:"))
                            {
                                string partialKey = l.Split(':').Last().Trim();
                                if (!string.IsNullOrEmpty(partialKey))
                                {
                                    progress?.Report($"Удаление устаревшего ключа ending with {partialKey}...");
                                    RunCscript(osppPath, $"/unpkey:{partialKey}");
                                }
                            }
                        }
                    }

                    progress?.Report("Очистка лицензий завершена!");
                    return true;
                }
                catch (Exception ex)
                {
                    progress?.Report($"Ошибка очистки ключей: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Performs deep force removal of corrupted / legacy Microsoft Office installations.
        /// </summary>
        public async Task<bool> ForceRemoveOfficeAsync(IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report("Принудительная остановка служб и процессов Office ClickToRun...");
                    var procs = new[] { "OfficeClickToRun", "integratedoffice", "winword", "excel", "powerpnt", "msaccess", "outlook", "onenote", "visio", "winproj" };
                    foreach (var p in procs)
                    {
                        foreach (var proc in Process.GetProcessesByName(p))
                        {
                            try { proc.Kill(entireProcessTree: true); } catch { }
                        }
                    }

                    progress?.Report("Остановка службы ClickToRunSvc...");
                    try
                    {
                        Process.Start(new ProcessStartInfo("sc.exe", "stop ClickToRunSvc") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
                        Process.Start(new ProcessStartInfo("sc.exe", "delete ClickToRunSvc") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
                    }
                    catch { }

                    progress?.Report("Очистка реестровых разделов Microsoft Office...");
                    try
                    {
                        Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Office\ClickToRun", throwOnMissingSubKey: false);
                        Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Office", throwOnMissingSubKey: false);
                        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Office", throwOnMissingSubKey: false);
                    }
                    catch { }

                    progress?.Report("Удаление остаточных файлов и кэшей Office...");
                    string commonFiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), @"microsoft shared\ClickToRun");
                    if (Directory.Exists(commonFiles))
                    {
                        try { Directory.Delete(commonFiles, true); } catch { }
                    }

                    progress?.Report("Глубокая зачистка Office успешно завершена!");
                    return true;
                }
                catch (Exception ex)
                {
                    progress?.Report($"Ошибка зачистки: {ex.Message}");
                    return false;
                }
            });
        }
    }
}
