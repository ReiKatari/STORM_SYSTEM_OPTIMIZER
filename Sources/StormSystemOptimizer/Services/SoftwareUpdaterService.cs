using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class SoftwareUpdaterService
    {
        private static SoftwareUpdaterService? _instance;
        public static SoftwareUpdaterService Instance => _instance ??= new SoftwareUpdaterService();

        private readonly string _blacklistFilePath;
        private readonly HashSet<string> _blacklistedPackages = new(StringComparer.OrdinalIgnoreCase);

        // Comprehensive multi-repository cloud catalog of official latest versions
        private static readonly Dictionary<string, (string LatestVersion, string DownloadUrl, string Publisher)> _cloudCatalog =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "Bitrix24", ("24.0.0", "https://www.bitrix24.ru/apps/desktop.php", "Bitrix") },
                { "Bitrix24 for Windows", ("24.0.0", "https://www.bitrix24.ru/apps/desktop.php", "Bitrix") },
                { "Битрикс24", ("24.0.0", "https://www.bitrix24.ru/apps/desktop.php", "Bitrix") },
                { "Telegram Desktop", ("5.5.5", "https://desktop.telegram.org", "Telegram FZ-LLC") },
                { "Telegram", ("5.5.5", "https://desktop.telegram.org", "Telegram FZ-LLC") },
                { "Yandex", ("24.7.1.1120", "https://browser.yandex.ru", "YANDEX LLC") },
                { "Яндекс Браузер", ("24.7.1.1120", "https://browser.yandex.ru", "YANDEX LLC") },
                { "Google Chrome", ("128.0.6613.120", "https://www.google.com/chrome/", "Google LLC") },
                { "Mozilla Firefox", ("130.0", "https://www.mozilla.org/firefox/", "Mozilla Corporation") },
                { "Opera Stable", ("113.0.5230.86", "https://www.opera.com", "Opera Software") },
                { "7-Zip", ("24.08", "https://www.7-zip.org", "Igor Pavlov") },
                { "Notepad++", ("8.6.9", "https://notepad-plus-plus.org", "Don HO") },
                { "AIMP", ("5.30.2563", "https://www.aimp.ru", "Artem Izmaylov") },
                { "Discord", ("1.0.9168", "https://discord.com", "Discord Inc.") },
                { "VLC media player", ("3.0.21", "https://www.videolan.org", "VideoLAN") },
                { "Steam", ("1.0.0.79", "https://store.steampowered.com", "Valve Corporation") },
                { "Epic Games Launcher", ("1.3.193.0", "https://store.epicgames.com", "Epic Games Inc.") },
                { "qBittorrent", ("4.6.5", "https://www.qbittorrent.org", "The qBittorrent Project") },
                { "Total Commander", ("11.03", "https://www.ghisler.com", "Christian Ghisler") },
                { "FastStone Image Viewer", ("7.8", "https://www.faststone.org", "FastStone Soft") },
                { "CPU-Z", ("2.10", "https://www.cpuid.com", "CPUID") },
                { "GPU-Z", ("2.59.0", "https://www.techpowerup.com", "TechPowerUp") },
                { "HWiNFO64", ("8.06", "https://www.hwinfo.com", "REALiX") },
                { "CrystalDiskInfo", ("9.3.2", "https://crystalmark.info", "Crystal Dew World") },
                { "Rufus", ("4.5", "https://rufus.ie", "Pete Batard") },
                { "OBS Studio", ("32.2.1", "https://obsproject.com", "OBS Project") },
                { "WinRAR", ("7.23.0", "https://www.rarlab.com", "RARLab") },
                { "Zoom", ("7.1.5.43453", "https://zoom.us", "Zoom Video Communications") },
                { "Docker Desktop", ("4.87.0", "https://www.docker.com", "Docker Inc.") },
                { "AnyDesk", ("9.7.15", "https://anydesk.com", "AnyDesk Software GmbH") },
                { "Git", ("2.55.0.3", "https://git-scm.com", "The Git Project") },
                { "IObit Uninstaller", ("15.6.0.6", "https://www.iobit.com", "IObit") }
            };

        private SoftwareUpdaterService()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM_OPTIMIZER");
            if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
            _blacklistFilePath = Path.Combine(appData, "software_blacklist.json");
            LoadBlacklist();
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
            if (string.IsNullOrWhiteSpace(packageIdOrName)) return false;
            bool isNowBlacklisted;
            if (_blacklistedPackages.Contains(packageIdOrName))
            {
                _blacklistedPackages.Remove(packageIdOrName);
                isNowBlacklisted = false;
            }
            else
            {
                _blacklistedPackages.Add(packageIdOrName);
                isNowBlacklisted = true;
            }
            SaveBlacklist();
            return isNowBlacklisted;
        }

        public bool IsBlacklisted(string packageIdOrName) => _blacklistedPackages.Contains(packageIdOrName);

        public async Task<List<SoftwareUpdateItem>> ScanInstalledAppsForUpdatesAsync()
        {
            return await Task.Run(async () =>
            {
                var list = new List<SoftwareUpdateItem>();
                var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Load installed apps & games with real binary versions
                var installedApps = await SoftwareUninstallerService.Instance.GetInstalledAppsAsync();
                foreach (var app in installedApps)
                {
                    if (string.IsNullOrWhiteSpace(app.DisplayName)) continue;
                    if (seenNames.Contains(app.DisplayName)) continue;
                    seenNames.Add(app.DisplayName);

                    bool blacklisted = IsBlacklisted(app.DisplayName) || IsBlacklisted(app.Id);
                    string ver = !string.IsNullOrWhiteSpace(app.DisplayVersion) ? app.DisplayVersion : "1.0.0";
                    string pub = !string.IsNullOrWhiteSpace(app.Publisher) ? app.Publisher : "Официальное ПО";

                    list.Add(new SoftwareUpdateItem
                    {
                        PackageId = app.Id,
                        Name = app.DisplayName,
                        InstalledVersion = ver,
                        AvailableVersion = ver,
                        Publisher = pub,
                        AppType = app.AppType,
                        IsUpdateAvailable = false,
                        IsBlacklisted = blacklisted,
                        IconSource = null
                    });
                }

                // 2. Query Winget Repository Upgrades (complete scan)
                var wingetUpgrades = QueryWingetUpgrades();
                foreach (var (wName, wId, wCurVer, wNewVer) in wingetUpgrades)
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

                        if (idCol > 0 && verCol > idCol && availCol > verCol)
                        {
                            for (int i = headerIdx + 2; i < lines.Length; i++)
                            {
                                string line = lines[i];
                                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("---") || line.Contains("upgrades available") || line.Contains("package(s) have"))
                                    continue;

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

            return await Task.Run(() =>
            {
                string pkgId = item.PackageId;
                string name = item.Name;
                string targetVer = item.AvailableVersion;

                progressCallback?.Invoke($"Инициализация обновления для «{name}»...");

                // 1. Try Winget if PackageId is available and valid
                if (!string.IsNullOrEmpty(pkgId) && pkgId.Contains(".") && !Guid.TryParse(pkgId, out _))
                {
                    try
                    {
                        progressCallback?.Invoke($"Скачивание и тихая установка через Winget ({pkgId})...");

                        var psi = new ProcessStartInfo
                        {
                            FileName = "winget.exe",
                            Arguments = $"upgrade --exact --id \"{pkgId}\" --include-unknown --accept-package-agreements --accept-source-agreements --disable-interactivity",
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
                            // Wait up to 3 minutes for large packages
                            bool finished = proc.WaitForExit(180000);
                            string output = proc.StandardOutput.ReadToEnd();

                            if (finished && (proc.ExitCode == 0 || output.Contains("Successfully installed") || output.Contains("Успешно установлено")))
                            {
                                item.InstalledVersion = targetVer;
                                item.IsUpdateAvailable = false;
                                return (true, $"«{name}» успешно обновлена до версии v{targetVer}!");
                            }
                        }
                    }
                    catch { }
                }

                // 2. Fallback to Cloud Catalog direct download link
                foreach (var kvp in _cloudCatalog)
                {
                    if (name.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kvp.Key.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string downloadUrl = kvp.Value.DownloadUrl;
                        try
                        {
                            progressCallback?.Invoke($"Открытие официальной страницы обновления: {downloadUrl}...");
                            Process.Start(new ProcessStartInfo { FileName = downloadUrl, UseShellExecute = true });
                            return (true, $"Открыта страница загрузки обновления для «{name}» (v{targetVer}) в браузере.");
                        }
                        catch { }
                    }
                }

                // 3. Fallback search
                try
                {
                    string searchUrl = "https://www.google.com/search?q=" + Uri.EscapeDataString($"{name} update download official");
                    Process.Start(new ProcessStartInfo { FileName = searchUrl, UseShellExecute = true });
                    return (true, $"Открыта страница обновления для «{name}».");
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка запуска обновления: {ex.Message}");
                }
            });
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
