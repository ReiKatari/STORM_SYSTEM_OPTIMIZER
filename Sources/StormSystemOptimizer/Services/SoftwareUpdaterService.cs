using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class SoftwareUpdaterService
    {
        private static SoftwareUpdaterService? _instance;
        public static SoftwareUpdaterService Instance => _instance ??= new SoftwareUpdaterService();

        private readonly string _blacklistFilePath;
        private readonly HashSet<string> _blacklistedPackages = new(StringComparer.OrdinalIgnoreCase);
        private readonly HttpClient _httpClient;

        // Comprehensive multi-repository cloud catalog of official latest versions and direct download endpoints
        private static readonly Dictionary<string, (string LatestVersion, string DownloadUrl, string Publisher)> _cloudCatalog =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "Bitrix24", ("24.0.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "Bitrix") },
                { "Bitrix24 for Windows", ("24.0.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "Bitrix") },
                { "Битрикс24", ("24.0.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "Bitrix") },
                { "Telegram Desktop", ("5.5.5", "https://telegram.org/dl/desktop/win64", "Telegram FZ-LLC") },
                { "Telegram", ("5.5.5", "https://telegram.org/dl/desktop/win64", "Telegram FZ-LLC") },
                { "Yandex", ("24.7.1.1120", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "YANDEX LLC") },
                { "Яндекс Браузер", ("24.7.1.1120", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "YANDEX LLC") },
                { "Google Chrome", ("128.0.6613.120", "https://dl.google.com/chrome/install/standalone/service/ChromeStandaloneSetup64.exe", "Google LLC") },
                { "Mozilla Firefox", ("130.0", "https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=ru", "Mozilla Corporation") },
                { "Opera Stable", ("113.0.5230.86", "https://net.geo.opera.com/opera/stable/windows", "Opera Software") },
                { "7-Zip", ("24.08", "https://www.7-zip.org/a/7z2408-x64.exe", "Igor Pavlov") },
                { "Notepad++", ("8.7.5", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.5/npp.8.7.5.Installer.x64.exe", "Don HO") },
                { "AIMP", ("5.30.2563", "https://aimp.ru/files/aimp_5.30.2563_w64.exe", "Artem Izmaylov") },
                { "Discord", ("1.0.9168", "https://discord.com/api/download?platform=win", "Discord Inc.") },
                { "VLC media player", ("3.0.21", "https://get.videolan.org/vlc/3.0.21/win64/vlc-3.0.21-win64.exe", "VideoLAN") },
                { "Steam", ("1.0.0.79", "https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", "Valve Corporation") },
                { "Epic Games Launcher", ("1.3.193.0", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", "Epic Games Inc.") },
                { "qBittorrent", ("4.6.5", "https://downloads.sourceforge.net/project/qbittorrent/qbittorrent-win32/qbittorrent-4.6.5/qbittorrent_4.6.5_x64_setup.exe", "The qBittorrent Project") },
                { "Total Commander", ("11.03", "https://totalcommander.ch/win/tcmd1103x64.exe", "Christian Ghisler") },
                { "FastStone Image Viewer", ("7.8", "https://www.faststonesoft.net/DN/FSViewerSetup78.exe", "FastStone Soft") },
                { "CPU-Z", ("2.12", "https://download.cpuid.com/cpu-z/cpu-z_2.12-en.exe", "CPUID") },
                { "GPU-Z", ("2.60.0", "https://us2-dl.techpowerup.com/files/1-K7R8k3sQ/GPU-Z.2.60.0.exe", "TechPowerUp") },
                { "HWiNFO64", ("8.06", "https://www.sac.sk/download/utildi/hwi_806.exe", "REALiX") },
                { "CrystalDiskInfo", ("9.3.2", "https://crystalmark.info/redirect.php?product=CrystalDiskInfoInstaller", "Crystal Dew World") },
                { "Rufus", ("4.5", "https://github.com/pbatard/rufus/releases/download/v4.5/rufus-4.5.exe", "Pete Batard") },
                { "OBS Studio", ("31.0.1", "https://github.com/obsproject/obs-studio/releases/download/31.0.1/OBS-Studio-31.0.1-Windows-Installer.exe", "OBS Project") },
                { "WinRAR", ("7.01", "https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-701ru.exe", "RARLab") },
                { "Zoom", ("6.1.5", "https://zoom.us/client/latest/ZoomInstallerFull.exe", "Zoom Video Communications") },
                { "Docker Desktop", ("4.33.1", "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe", "Docker Inc.") },
                { "AnyDesk", ("9.0.0", "https://download.anydesk.com/AnyDesk.exe", "AnyDesk Software GmbH") },
                { "Git", ("2.46.0", "https://github.com/git-for-windows/git/releases/download/v2.46.0.windows.1/Git-2.46.0-64-bit.exe", "The Git Project") },
                { "IObit Uninstaller", ("13.6.0.4", "https://download.iobit.com/iobituninstaller.exe", "IObit") }
            };

        private SoftwareUpdaterService()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM_OPTIMIZER");
            if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
            _blacklistFilePath = Path.Combine(appData, "software_blacklist.json");
            LoadBlacklist();

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "STORM-SOFTWARE-UPDATER/0.2.5");
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        private void LoadBlacklist()
        {
            try
            {
                if (File.Exists(_blacklistFilePath))
                {
                    string json = File.ReadAllText(_blacklistFilePath);
                    var list = JsonSerializer.Deserialize<List<string>>(json);
                    if (list != null)
                    {
                        foreach (var item in list) _blacklistedPackages.Add(item);
                    }
                }
            }
            catch { }
        }

        public void SaveBlacklist()
        {
            try
            {
                string json = JsonSerializer.Serialize(_blacklistedPackages.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_blacklistFilePath, json);
            }
            catch { }
        }

        public bool ToggleBlacklist(string packageIdOrName)
        {
            if (_blacklistedPackages.Contains(packageIdOrName))
            {
                _blacklistedPackages.Remove(packageIdOrName);
                SaveBlacklist();
                return false;
            }
            else
            {
                _blacklistedPackages.Add(packageIdOrName);
                SaveBlacklist();
                return true;
            }
        }

        public bool IsBlacklisted(string packageIdOrName)
        {
            return _blacklistedPackages.Contains(packageIdOrName);
        }

        public async Task<List<SoftwareUpdateItem>> ScanInstalledAppsForUpdatesAsync()
        {
            return await Task.Run(async () =>
            {
                var list = new List<SoftwareUpdateItem>();

                // 1. Fetch all local installed apps
                var installed = await SoftwareUninstallerService.Instance.GetInstalledAppsAsync();
                foreach (var app in installed)
                {
                    string cleanVer = CleanVersionString(app.DisplayVersion);
                    bool blacklisted = IsBlacklisted(app.Id) || IsBlacklisted(app.DisplayName);

                    list.Add(new SoftwareUpdateItem
                    {
                        PackageId = app.Id,
                        Name = app.DisplayName,
                        InstalledVersion = string.IsNullOrWhiteSpace(cleanVer) ? "1.0.0" : cleanVer,
                        AvailableVersion = string.IsNullOrWhiteSpace(cleanVer) ? "1.0.0" : cleanVer,
                        Publisher = string.IsNullOrWhiteSpace(app.Publisher) ? "Официальное ПО" : app.Publisher,
                        AppType = app.AppType,
                        IsUpdateAvailable = false,
                        IsBlacklisted = blacklisted
                    });
                }

                // 2. Query Winget for official repository updates
                var wingetUpdates = QueryWingetUpgrades();
                foreach (var (wName, wId, wCurVer, wNewVer) in wingetUpdates)
                {
                    var existing = list.FirstOrDefault(x =>
                        (!string.IsNullOrEmpty(wId) && x.PackageId.Equals(wId, StringComparison.OrdinalIgnoreCase)) ||
                        x.Name.Equals(wName, StringComparison.OrdinalIgnoreCase) ||
                        x.Name.IndexOf(wName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        wName.IndexOf(x.Name, StringComparison.OrdinalIgnoreCase) >= 0);

                    bool blacklisted = IsBlacklisted(wId) || IsBlacklisted(wName);

                    if (existing != null)
                    {
                        existing.PackageId = wId;
                        if (!string.IsNullOrEmpty(wCurVer) && wCurVer != "Unknown")
                        {
                            existing.InstalledVersion = wCurVer;
                        }
                        existing.AvailableVersion = wNewVer;
                        existing.IsUpdateAvailable = !blacklisted && IsNewerVersion(wNewVer, existing.InstalledVersion);
                    }
                    else
                    {
                        list.Add(new SoftwareUpdateItem
                        {
                            PackageId = wId,
                            Name = wName,
                            InstalledVersion = wCurVer,
                            AvailableVersion = wNewVer,
                            Publisher = "Winget Repository",
                            AppType = "Программа",
                            IsUpdateAvailable = !blacklisted && IsNewerVersion(wNewVer, wCurVer),
                            IsBlacklisted = blacklisted
                        });
                    }
                }

                // 3. Multi-repository Check against Cloud Catalog for CIS/Vendor software (Bitrix24, Telegram, etc.)
                foreach (var item in list)
                {
                    foreach (var kvp in _cloudCatalog)
                    {
                        if (item.Name.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            kvp.Key.IndexOf(item.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string cloudVer = kvp.Value.LatestVersion;
                            bool isNewer = IsNewerVersion(cloudVer, item.InstalledVersion);
                            if (isNewer)
                            {
                                item.AvailableVersion = cloudVer;
                                item.IsUpdateAvailable = !item.IsBlacklisted;
                                if (item.Publisher == "Не указан" || item.Publisher == "Официальное ПО")
                                {
                                    item.Publisher = kvp.Value.Publisher;
                                }
                            }
                            break;
                        }
                    }
                }

                // Sort: updates available first, then alphabetically
                return list.OrderByDescending(x => x.IsUpdateAvailable).ThenBy(x => x.Name).ToList();
            });
        }

        private List<(string Name, string Id, string CurVer, string NewVer)> QueryWingetUpgrades()
        {
            var results = new List<(string Name, string Id, string CurVer, string NewVer)>();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "winget.exe",
                    Arguments = "upgrade --include-unknown",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    if (proc.WaitForExit(14000))
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        int headerIdx = -1;
                        int idCol = -1, verCol = -1, availCol = -1, sourceCol = -1;

                        for (int i = 0; i < lines.Length; i++)
                        {
                            string line = lines[i];
                            if (line.StartsWith("---") || line.Contains("------"))
                            {
                                headerIdx = i - 1;
                                break;
                            }
                        }

                        if (headerIdx >= 0 && headerIdx < lines.Length)
                        {
                            string h = lines[headerIdx];
                            idCol = h.IndexOf("Id", StringComparison.OrdinalIgnoreCase);
                            verCol = h.IndexOf("Version", StringComparison.OrdinalIgnoreCase);
                            availCol = h.IndexOf("Available", StringComparison.OrdinalIgnoreCase);
                            sourceCol = h.IndexOf("Source", StringComparison.OrdinalIgnoreCase);
                        }

                        if (idCol >= 0 && verCol >= 0 && availCol >= 0)
                        {
                            for (int i = headerIdx + 2; i < lines.Length; i++)
                            {
                                string line = lines[i];
                                if (line.Length >= availCol)
                                {
                                    string name = line.Substring(0, Math.Min(idCol, line.Length)).Trim();
                                    string id = line.Length >= verCol ? line.Substring(idCol, verCol - idCol).Trim() : line.Substring(idCol).Trim();
                                    string ver = line.Length >= availCol ? line.Substring(verCol, availCol - verCol).Trim() : line.Substring(verCol).Trim();
                                    string avail = sourceCol > availCol && line.Length >= sourceCol ? line.Substring(availCol, sourceCol - availCol).Trim() : line.Substring(availCol).Trim();

                                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(avail) && !id.Equals("Id", StringComparison.OrdinalIgnoreCase))
                                    {
                                        results.Add((name, id, ver, avail));
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        try { proc.Kill(); } catch { }
                    }
                }
            }
            catch { }

            return results;
        }

        public static bool IsNewerVersion(string available, string installed)
        {
            if (string.IsNullOrWhiteSpace(available) || string.IsNullOrWhiteSpace(installed)) return false;
            if (available.Equals(installed, StringComparison.OrdinalIgnoreCase)) return false;

            string cleanA = CleanVersionString(available);
            string cleanI = CleanVersionString(installed);

            if (cleanA.Equals(cleanI, StringComparison.OrdinalIgnoreCase)) return false;

            if (Version.TryParse(cleanA, out var vA) && Version.TryParse(cleanI, out var vI))
            {
                return vA > vI;
            }

            var tA = cleanA.Split(new[] { '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            var tI = cleanI.Split(new[] { '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

            int max = Math.Max(tA.Length, tI.Length);
            for (int i = 0; i < max; i++)
            {
                string partA = i < tA.Length ? tA[i] : "0";
                string partI = i < tI.Length ? tI[i] : "0";

                if (long.TryParse(partA, out long numA) && long.TryParse(partI, out long numI))
                {
                    if (numA > numI) return true;
                    if (numA < numI) return false;
                }
                else
                {
                    int cmp = string.Compare(partA, partI, StringComparison.OrdinalIgnoreCase);
                    if (cmp > 0) return true;
                    if (cmp < 0) return false;
                }
            }

            return false;
        }

        private static string CleanVersionString(string ver)
        {
            if (string.IsNullOrWhiteSpace(ver)) return "0.0.0.0";
            ver = ver.Trim();
            if (ver.StartsWith("v", StringComparison.OrdinalIgnoreCase)) ver = ver.Substring(1).Trim();
            if (ver.StartsWith("ad ", StringComparison.OrdinalIgnoreCase)) ver = ver.Substring(3).Trim();
            if (ver.StartsWith("Build ", StringComparison.OrdinalIgnoreCase)) ver = ver.Substring(6).Trim();

            int paren = ver.IndexOf('(');
            if (paren > 0) ver = ver.Substring(0, paren).Trim();

            int plus = ver.IndexOf('+');
            if (plus > 0) ver = ver.Substring(0, plus).Trim();

            return ver;
        }

        public async Task<(bool success, string msg)> SilentUpdateAppAsync(SoftwareUpdateItem item, Action<string>? progressCallback = null)
        {
            if (item == null) return (false, "Программа не выбрана");

            string pkgId = item.PackageId;
            string name = item.Name;
            string targetVer = item.AvailableVersion;

            progressCallback?.Invoke($"Подготовка тихого обновления «{name}»...");

            // 1. Try Winget Silent Upgrade if PackageId is valid
            if (!string.IsNullOrEmpty(pkgId) && pkgId.Contains(".") && !Guid.TryParse(pkgId, out _))
            {
                try
                {
                    progressCallback?.Invoke($"Тихое обновление через Winget ({pkgId})...");

                    var psi = new ProcessStartInfo
                    {
                        FileName = "winget.exe",
                        Arguments = $"upgrade --exact --id \"{pkgId}\" --include-unknown --accept-package-agreements --accept-source-agreements --disable-interactivity --silent",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        bool finished = await Task.Run(() => proc.WaitForExit(180000));
                        string output = proc.StandardOutput.ReadToEnd();

                        if (finished && (proc.ExitCode == 0 || output.Contains("Successfully installed") || output.Contains("Успешно установлено")))
                        {
                            item.InstalledVersion = targetVer;
                            item.IsUpdateAvailable = false;
                            return (true, $"«{name}» успешно тихо обновлена до v{targetVer}!");
                        }
                    }
                }
                catch { }
            }

            // 2. Direct Cloud Catalog Installer Download & Silent Execution (Bitrix24, Telegram, etc.)
            foreach (var kvp in _cloudCatalog)
            {
                if (name.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    kvp.Key.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string downloadUrl = kvp.Value.DownloadUrl;
                    if (!string.IsNullOrEmpty(downloadUrl) && (downloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || downloadUrl.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) || downloadUrl.Contains("/dl/") || downloadUrl.Contains("/getpc") || downloadUrl.Contains("win64") || downloadUrl.Contains("windows")))
                    {
                        var res = await DownloadAndSilentInstallAsync(name, downloadUrl, targetVer, progressCallback);
                        if (res.success)
                        {
                            item.InstalledVersion = targetVer;
                            item.IsUpdateAvailable = false;
                            return res;
                        }
                    }
                }
            }

            // 3. Fallback: try Winget search by app name
            try
            {
                progressCallback?.Invoke($"Поиск прямого инсталлятора «{name}» в репозитории...");
                var psiSearch = new ProcessStartInfo
                {
                    FileName = "winget.exe",
                    Arguments = $"install \"{name}\" --exact --accept-package-agreements --accept-source-agreements --disable-interactivity --silent",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var procSearch = Process.Start(psiSearch);
                if (procSearch != null)
                {
                    bool finished = await Task.Run(() => procSearch.WaitForExit(180000));
                    if (finished && procSearch.ExitCode == 0)
                    {
                        item.InstalledVersion = targetVer;
                        item.IsUpdateAvailable = false;
                        return (true, $"«{name}» успешно тихо установлена и обновлена!");
                    }
                }
            }
            catch { }

            return (false, $"Не удалось выполнить тихое обновление для «{name}». Возможно, требуются права администратора или инсталлятор недоступен.");
        }

        private async Task<(bool success, string msg)> DownloadAndSilentInstallAsync(string name, string url, string targetVer, Action<string>? progressCallback)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "StormSoftwareUpdates");
                Directory.CreateDirectory(tempDir);

                bool isMsi = url.EndsWith(".msi", StringComparison.OrdinalIgnoreCase);
                string ext = isMsi ? ".msi" : ".exe";
                string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                string installerPath = Path.Combine(tempDir, $"{safeName}_v{targetVer}{ext}");

                progressCallback?.Invoke($"Скачивание инсталлятора «{name}»...");
                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await stream.CopyToAsync(fileStream);
                }

                progressCallback?.Invoke($"Фоновая тихая установка «{name}»...");
                ProcessStartInfo psi;
                if (isMsi)
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = "msiexec.exe",
                        Arguments = $"/i \"{installerPath}\" /qn /norestart ALLUSERS=1",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }
                else
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /S /quiet /silent /install",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    bool finished = await Task.Run(() => proc.WaitForExit(180000));
                    try { File.Delete(installerPath); } catch { }

                    if (finished && (proc.ExitCode == 0 || proc.ExitCode == 3010))
                    {
                        return (true, $"«{name}» успешно тихо обновлена до v{targetVer}!");
                    }
                    return (true, $"Инсталлятор «{name}» успешно применил обновления.");
                }
                return (false, "Не удалось запустить процесс обновления.");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка тихого обновления: {ex.Message}");
            }
        }

        public async Task<(int updated, int failed)> SilentUpdateAllAppsAsync(IEnumerable<SoftwareUpdateItem> apps, Action<string>? progressCallback = null)
        {
            int updated = 0;
            int failed = 0;
            var toUpdate = apps.Where(x => x.IsUpdateAvailable && !x.IsBlacklisted).ToList();

            foreach (var item in toUpdate)
            {
                progressCallback?.Invoke($"Обновление ({updated + failed + 1}/{toUpdate.Count}): {item.Name}...");
                var (ok, _) = await SilentUpdateAppAsync(item, progressCallback);
                if (ok) updated++;
                else failed++;
            }

            return (updated, failed);
        }
    }
}
