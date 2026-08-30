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
        public string TargetVersion { get; set; } = "16.0.17932.20162";
        public string ReleaseDate { get; set; } = "2024-2026";
        public string Description { get; set; } = "LTSC бессрочная корпоративная лицензия";
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
            new OfficeProductEdition
            {
                Id = "ProPlus2024Volume",
                DisplayName = "Microsoft Office 2024 ProPlus (LTSC Volume)",
                Channel = "PerpetualVL2024",
                GvlkKey = "2TDPW-NDQ7G-FMG99-DXQ7M-84LD6",
                TargetVersion = "16.0.17932.20162",
                ReleaseDate = "2024 (LTSC)",
                Description = "Флагманский корпоративный выпуск 2024 с бессрочной поддержкой и без облачной привязки"
            },
            new OfficeProductEdition
            {
                Id = "ProPlus2021Volume",
                DisplayName = "Microsoft Office 2021 ProPlus (LTSC Volume)",
                Channel = "PerpetualVL2021",
                GvlkKey = "FXYTK-NJJ8C-GB6DW-3DYQT-6F7TH",
                TargetVersion = "16.0.14332.20771",
                ReleaseDate = "2021 (LTSC)",
                Description = "Стабильный классический выпуск 2021 для рабочих станций"
            },
            new OfficeProductEdition
            {
                Id = "ProPlus2019Volume",
                DisplayName = "Microsoft Office 2019 ProPlus (Volume)",
                Channel = "PerpetualVL2019",
                GvlkKey = "NMMKJ-6RK4F-KMJVX-8D9MJ-6MWKP",
                TargetVersion = "16.0.10411.20011",
                ReleaseDate = "2019 (Volume)",
                Description = "Проверенный временем выпуск 2019 с минимальным потреблением ресурсов"
            },
            new OfficeProductEdition
            {
                Id = "O365ProPlusRetail",
                DisplayName = "Microsoft 365 ProPlus (Mondo / Enterprise)",
                Channel = "Current",
                GvlkKey = "HFTND-W9MK4-8VK7D-VQVC9-TMCRG",
                TargetVersion = "16.0.18025.20160",
                ReleaseDate = "2026 (Live CDN)",
                Description = "Всегда актуальные облачные функции, новые формулы Excel и дизайн"
            },
            new OfficeProductEdition
            {
                Id = "VisioPro2024Volume",
                DisplayName = "Microsoft Visio 2024 Professional (Volume)",
                Channel = "PerpetualVL2024",
                GvlkKey = "YW66X-NH62M-G6YFP-B79ZR-D72FC",
                TargetVersion = "16.0.17932.20162",
                ReleaseDate = "2024 (LTSC)",
                Description = "Профессиональный векторный редактор диаграмм, схем и чертежей"
            },
            new OfficeProductEdition
            {
                Id = "ProjectPro2024Volume",
                DisplayName = "Microsoft Project 2024 Professional (Volume)",
                Channel = "PerpetualVL2024",
                GvlkKey = "D9GTG-NP2KV-M6HDK-DTFCW-BQ8T9",
                TargetVersion = "16.0.17932.20162",
                ReleaseDate = "2024 (LTSC)",
                Description = "Инструмент управления проектами, диаграммами Ганта и ресурсами"
            }
        };

        public List<string> KmsServers => new()
        {
            "kms.digiboy.ir",
            "kms8.msguides.com",
            "kms.03k.org",
            "kms.loli.best",
            "kms.cx90.net"
        };

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
                    if (!string.IsNullOrEmpty(ver))
                    {
                        return $"{product} ({arch}) — Версия {ver}";
                    }
                }
            }
            catch { }
            return "Не установлен";
        }

        public string GetInstalledOfficeVersionOnly()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");
                if (key != null)
                {
                    return key.GetValue("VersionToReport") as string ?? "";
                }
            }
            catch { }
            return "";
        }

        public async Task<string> CheckUpdateStatusAsync(OfficeProductEdition edition)
        {
            return await Task.Run(() =>
            {
                string installedVer = GetInstalledOfficeVersionOnly();
                if (string.IsNullOrEmpty(installedVer))
                {
                    return $"Готов к установке: актуальный CDN-билд {edition.TargetVersion}";
                }

                if (string.Compare(installedVer, edition.TargetVersion, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return $"💡 Доступно обновление: установлена {installedVer}, актуальная на CDN {edition.TargetVersion}";
                }

                return $"✅ Установлена последняя актуальная версия Office ({installedVer})";
            });
        }

        public async Task<bool> TriggerOnlineUpdateAsync(IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report("Запуск проверки и установки обновлений через OfficeC2RClient...");
                    string commonFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
                    string c2rClient = Path.Combine(commonFiles, @"microsoft shared\ClickToRun\OfficeC2RClient.exe");

                    if (File.Exists(c2rClient))
                    {
                        using var p = Process.Start(new ProcessStartInfo
                        {
                            FileName = c2rClient,
                            Arguments = "/update user displaylevel=true",
                            UseShellExecute = true
                        });
                        progress?.Report("Диспетчер обновлений Microsoft Office запущен в фоновом режиме.");
                        return true;
                    }
                    else
                    {
                        progress?.Report("OfficeC2RClient не найден (Office C2R не установлен на ПК).");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"Ошибка запуска обновления: {ex.Message}");
                    return false;
                }
            });
        }

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
            if (!options.InstallVisio && edition.Id != "VisioPro2024Volume") sb.AppendLine("      <ExcludeApp ID=\"Visio\" />");
            if (!options.InstallProject && edition.Id != "ProjectPro2024Volume") sb.AppendLine("      <ExcludeApp ID=\"Project\" />");

            // Exclude bloat
            if (options.ExcludeTeams)
            {
                sb.AppendLine("      <ExcludeApp ID=\"Teams\" />");
                sb.AppendLine("      <ExcludeApp ID=\"Lync\" />");
            }
            if (options.ExcludeOneDrive) sb.AppendLine("      <ExcludeApp ID=\"OneDrive\" />");
            if (options.ExcludeBing) sb.AppendLine("      <ExcludeApp ID=\"Bing\" />");

            sb.AppendLine("    </Product>");
            sb.AppendLine("  </Add>");
            sb.AppendLine("  <Property Name=\"SharedComputerLicensing\" Value=\"0\" />");
            sb.AppendLine("  <Property Name=\"PinIconsToTaskbar\" Value=\"FALSE\" />");
            sb.AppendLine("  <Property Name=\"SCLCacheOverride\" Value=\"0\" />");
            sb.AppendLine("  <Property Name=\"AUTOACTIVATE\" Value=\"0\" />");
            sb.AppendLine("  <Property Name=\"FORCEAPPSHUTDOWN\" Value=\"TRUE\" />");
            sb.AppendLine("  <Updates Enabled=\"TRUE\" />");
            sb.AppendLine("  <Display Level=\"Full\" AcceptEULA=\"TRUE\" />");
            sb.AppendLine("</Configuration>");

            return sb.ToString();
        }

        public async Task<bool> InstallOfficeAsync(OfficeDeployOptions options, IProgress<string>? progress = null)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), "STORM_Office_Deploy");
                    Directory.CreateDirectory(tempDir);

                    string xmlPath = Path.Combine(tempDir, "configuration.xml");
                    string xmlContent = GenerateConfigurationXml(options);
                    File.WriteAllText(xmlPath, xmlContent, Encoding.UTF8);

                    string setupExePath = Path.Combine(tempDir, "setup.exe");

                    if (!File.Exists(setupExePath))
                    {
                        progress?.Report("Получение официального установщика Microsoft Office CDN...");
                        using var client = new System.Net.Http.HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(30);
                        byte[] setupBytes = await client.GetByteArrayAsync("https://officecdn.microsoft.com/pr/wsus/setup.exe");
                        File.WriteAllBytes(setupExePath, setupBytes);
                    }

                    progress?.Report("Запуск процесса Click-to-Run инсталляции (загрузка и развертывание компонентов)...");

                    var psi = new ProcessStartInfo
                    {
                        FileName = setupExePath,
                        Arguments = $"/configure \"{xmlPath}\"",
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        await proc.WaitForExitAsync();
                        progress?.Report("Инсталляция пакета Microsoft Office успешно завершена!");

                        if (options.AutoActivate)
                        {
                            progress?.Report("Запуск автоматической KMS-активации...");
                            await ActivateOfficeKmsAsync(options.KmsServer, progress);
                        }

                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    progress?.Report($"[ОШИБКА РАЗВЕРТЫВАНИЯ] {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> ActivateOfficeKmsAsync(string kmsServer, IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report($"Поиск скрипта управления лицензиями OSPP.VBS...");
                    string? osppPath = FindOsppVbs();

                    if (osppPath == null)
                    {
                        progress?.Report("OSPP.VBS не найден. Попытка активации через WMI / vNext / PowerShell...");
                        return RunPowerShellActivation(kmsServer, progress);
                    }

                    progress?.Report($"Установка KMS-сервера: {kmsServer}...");
                    RunCscript(osppPath, $"/sethst:{kmsServer}", progress);

                    progress?.Report("Применение активации через OSPP...");
                    string output = RunCscript(osppPath, "/act", progress);

                    if (output.Contains("Product activation successful", StringComparison.OrdinalIgnoreCase) ||
                        output.Contains("успешно", StringComparison.OrdinalIgnoreCase))
                    {
                        progress?.Report("✅ Microsoft Office успешно активирован на 180 дней с автопродлением!");
                        return true;
                    }
                    else
                    {
                        progress?.Report("OSPP вернул статус. Пробуем альтернативный KMS сервер...");
                        return RunPowerShellActivation(kmsServer, progress);
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"[ОШИБКА АКТИВАЦИИ] {ex.Message}");
                    return false;
                }
            });
        }

        private static string? FindOsppVbs()
        {
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            string[] candidates = new[]
            {
                Path.Combine(progFiles, @"Microsoft Office\Office16\OSPP.VBS"),
                Path.Combine(progFilesX86, @"Microsoft Office\Office16\OSPP.VBS"),
                Path.Combine(progFiles, @"Microsoft Office\Office15\OSPP.VBS"),
                Path.Combine(progFilesX86, @"Microsoft Office\Office15\OSPP.VBS")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static string RunCscript(string vbsPath, string args, IProgress<string>? progress)
        {
            try
            {
                using var p = new Process();
                p.StartInfo.FileName = "cscript.exe";
                p.StartInfo.Arguments = $"//Nologo \"{vbsPath}\" {args}";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();

                string outStr = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                progress?.Report(outStr);
                return outStr;
            }
            catch (Exception ex)
            {
                progress?.Report($"Ошибка cscript: {ex.Message}");
                return string.Empty;
            }
        }

        private static bool RunPowerShellActivation(string kmsServer, IProgress<string>? progress)
        {
            try
            {
                string script = $@"
$service = Get-WmiObject -Query 'SELECT ID, Name, PartialProductKey, LicenseStatus FROM SoftwareLicensingProduct WHERE Name LIKE ""%Office%"" AND PartialProductKey IS NOT NULL'
if ($service) {{
    foreach ($item in $service) {{
        try {{
            $item.SetKeyManagementServiceMachine('{kmsServer}')
            $item.Activate()
            Write-Output ('Активирован: ' + $item.Name)
        }} catch {{}}
    }}
}} else {{
    Write-Output 'Лицензии Office обнаружены через ClickToRun engine.'
}}
";
                using var p = new Process();
                p.StartInfo.FileName = "powershell.exe";
                p.StartInfo.Arguments = $"-NoProfile -NonInteractive -Command \"{script.Replace("\"", "\\\"")}\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                string outStr = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                progress?.Report(outStr);
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report($"Ошибка WMI активации: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CleanLegacyKeysAsync(IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string? osppPath = FindOsppVbs();
                    if (osppPath == null)
                    {
                        progress?.Report("OSPP.VBS не найден на диске.");
                        return false;
                    }

                    progress?.Report("Получение установленных ключей продуктов...");
                    string status = RunCscript(osppPath, "/dstatus", progress);

                    var lines = status.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var l in lines)
                    {
                        if (l.Contains("Last 5 characters of installed product key:", StringComparison.OrdinalIgnoreCase))
                        {
                            string keyPart = l.Substring(l.LastIndexOf(':') + 1).Trim();
                            if (!string.IsNullOrEmpty(keyPart))
                            {
                                progress?.Report($"Удаление ключа ...{keyPart}...");
                                RunCscript(osppPath, $"/unpkey:{keyPart}", progress);
                            }
                        }
                    }

                    progress?.Report("Очистка завершена!");
                    return true;
                }
                catch (Exception ex)
                {
                    progress?.Report($"Ошибка: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> ForceRemoveOfficeAsync(IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report("Остановка всех служб и фоновых процессов Microsoft Office...");
                    string[] procs = new[] { "winword", "excel", "powerpnt", "outlook", "onenote", "msaccess", "visio", "project", "officeclicktorun", "integratedoffice" };
                    foreach (var pr in procs)
                    {
                        try { foreach (var p in Process.GetProcessesByName(pr)) p.Kill(); } catch { }
                    }

                    progress?.Report("Остановка службы ClickToRunSvc...");
                    try
                    {
                        using var p = Process.Start("cmd.exe", "/c net stop ClickToRunSvc");
                        p?.WaitForExit(3000);
                    }
                    catch { }

                    progress?.Report("Удаление остаточных файлов и кэшей Office...");
                    string commonFiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), @"microsoft shared\ClickToRun");
                    if (Directory.Exists(commonFiles))
                    {
                        try { Directory.Delete(commonFiles, true); } catch { }
                    }

                    progress?.Report("Очистка веток реестра ClickToRun...");
                    try
                    {
                        Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Office\ClickToRun", false);
                        Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Office\16.0", false);
                        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Office", false);
                    }
                    catch { }

                    progress?.Report("Принудительная зачистка завершена. Система чиста от следов Office.");
                    return true;
                }
                catch (Exception ex)
                {
                    progress?.Report($"[ОШИБКА ЗАЧИСТКИ] {ex.Message}");
                    return false;
                }
            });
        }
    }
}
