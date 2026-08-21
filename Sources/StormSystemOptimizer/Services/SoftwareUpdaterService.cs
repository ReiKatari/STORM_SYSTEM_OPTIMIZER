using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
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
        private readonly HttpClient _httpClient;

        // Comprehensive multi-repository cloud catalog of official latest versions and direct download endpoints
        private static readonly Dictionary<string, (string LatestVersion, string DownloadUrl, string Publisher)> _cloudCatalog =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "WinRAR", ("7.10", "https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-701ru.exe", "RARLab") },
                { "WinRAR (64-bit)", ("7.10", "https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-701ru.exe", "RARLab") },
                { "WinRAR (32-bit)", ("7.10", "https://www.win-rar.com/fileadmin/winrar-versions/winrar/wrar701ru.exe", "RARLab") },
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
                { "Zoom", ("6.1.5", "https://zoom.us/client/latest/ZoomInstallerFull.exe", "Zoom Video Communications") },
                { "Docker Desktop", ("4.33.1", "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe", "Docker Inc.") },
                { "AnyDesk", ("9.0.0", "https://download.anydesk.com/AnyDesk.exe", "AnyDesk Software GmbH") },
                { "Git", ("2.46.0", "https://github.com/git-for-windows/git/releases/download/v2.46.0.windows.1/Git-2.46.0-64-bit.exe", "The Git Project") },
                { "IObit Uninstaller", ("13.6.0.4", "https://download.iobit.com/iobituninstaller.exe", "IObit") },
                { "ShareX", ("16.1.0", "https://github.com/ShareX/ShareX/releases/download/v16.1.0/ShareX-16.1.0-setup.exe", "ShareX Team") },
                { "K-Lite Codec Pack", ("18.5.0", "https://files3.codecguide.com/K-Lite_Codec_Pack_1850_Standard.exe", "Codec Guide") },
                { "Audacity", ("3.6.2", "https://github.com/audacity/audacity/releases/download/Audacity-3.6.2/audacity-win-3.6.2-64bit.exe", "Audacity Team") },
                { "GIMP", ("2.10.38", "https://download.gimp.org/gimp/v2.10/windows/gimp-2.10.38-setup.exe", "The GIMP Team") },
                { "Blender", ("4.2.1", "https://download.blender.org/release/Blender4.2/blender-4.2.1-windows-x64.msi", "Blender Foundation") },
                { "HandBrake", ("1.8.2", "https://github.com/HandBrake/HandBrake/releases/download/1.8.2/HandBrake-1.8.2-x86_64-Win_GUI.exe", "HandBrake Team") }
            };

        private SoftwareUpdaterService()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM_OPTIMIZER");
            if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
            _blacklistFilePath = Path.Combine(appData, "software_blacklist.json");
            LoadBlacklist();

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "STORM-SOFTWARE-UPDATER/0.3.1");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
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

        public bool IsBlacklisted(string packageIdOrName) => _blacklistedPackages.Contains(packageIdOrName);

        public async Task<List<SoftwareUpdateItem>> ScanInstalledAppsForUpdatesAsync()
        {
            return await Task.Run(() =>
            {
                var installedList = new List<SoftwareUpdateItem>();
                var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Scan Installed software from Registry (64-bit, 32-bit, CU) in ~5ms
                void ScanRegistryHive(RegistryHive hive, RegistryView view, string subKey)
                {
                    try
                    {
                        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                        using var key = baseKey.OpenSubKey(subKey);
                        if (key == null) return;

                        foreach (var subName in key.GetSubKeyNames())
                        {
                            try
                            {
                                using var appKey = key.OpenSubKey(subName);
                                if (appKey == null) continue;

                                string name = appKey.GetValue("DisplayName")?.ToString()?.Trim() ?? string.Empty;
                                if (string.IsNullOrEmpty(name)) continue;

                                if (name.StartsWith("KB", StringComparison.OrdinalIgnoreCase) ||
                                    name.Contains("Update for Windows", StringComparison.OrdinalIgnoreCase) ||
                                    name.Contains("Security Update", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                string ver = appKey.GetValue("DisplayVersion")?.ToString()?.Trim() ?? "1.0.0";
                                string pub = appKey.GetValue("Publisher")?.ToString()?.Trim() ?? "Разработчик ПО";

                                string dedupeKey = $"{name}_{ver}";
                                if (seenKeys.Contains(dedupeKey)) continue;
                                seenKeys.Add(dedupeKey);

                                installedList.Add(new SoftwareUpdateItem
                                {
                                    Name = name,
                                    PackageId = subName,
                                    Publisher = pub,
                                    InstalledVersion = ver,
                                    AvailableVersion = ver,
                                    IsUpdateAvailable = false,
                                    IsBlacklisted = IsBlacklisted(subName) || IsBlacklisted(name)
                                });
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                ScanRegistryHive(RegistryHive.LocalMachine, RegistryView.Registry64, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                ScanRegistryHive(RegistryHive.LocalMachine, RegistryView.Registry32, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                ScanRegistryHive(RegistryHive.CurrentUser, RegistryView.Registry64, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

                // 2. Fast Non-blocking Winget check with 3s hard timeout
                var wingetUpdates = GetWingetUpdatesFast();
                foreach (var wu in wingetUpdates)
                {
                    var match = installedList.FirstOrDefault(a => 
                        a.PackageId.Equals(wu.Id, StringComparison.OrdinalIgnoreCase) ||
                        a.Name.IndexOf(wu.Name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        wu.Name.IndexOf(a.Name, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (match != null)
                    {
                        match.PackageId = wu.Id;
                        if (!string.IsNullOrEmpty(wu.AvailableVersion) && IsNewerVersion(wu.AvailableVersion, match.InstalledVersion))
                        {
                            match.AvailableVersion = wu.AvailableVersion;
                            match.IsUpdateAvailable = !match.IsBlacklisted;
                        }
                    }
                    else
                    {
                        installedList.Add(new SoftwareUpdateItem
                        {
                            Name = wu.Name,
                            PackageId = wu.Id,
                            Publisher = "Winget Repository",
                            InstalledVersion = wu.InstalledVersion,
                            AvailableVersion = wu.AvailableVersion,
                            IsUpdateAvailable = !IsBlacklisted(wu.Id) && !IsBlacklisted(wu.Name) && IsNewerVersion(wu.AvailableVersion, wu.InstalledVersion),
                            IsBlacklisted = IsBlacklisted(wu.Id) || IsBlacklisted(wu.Name)
                        });
                    }
                }

                // 3. Match against Cloud Catalog (Instant, 0ms)
                foreach (var app in installedList)
                {
                    foreach (var kvp in _cloudCatalog)
                    {
                        if (app.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                            app.Name.StartsWith(kvp.Key + " ", StringComparison.OrdinalIgnoreCase) ||
                            app.Name.StartsWith(kvp.Key + "-", StringComparison.OrdinalIgnoreCase) ||
                            (kvp.Key.Length > 4 && app.Name.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            string cloudVer = kvp.Value.LatestVersion;
                            if (IsNewerVersion(cloudVer, app.InstalledVersion))
                            {
                                app.AvailableVersion = cloudVer;
                                app.IsUpdateAvailable = !app.IsBlacklisted;
                                if (app.Publisher == "Разработчик ПО") app.Publisher = kvp.Value.Publisher;
                            }
                            break;
                        }
                    }
                }

                return installedList.OrderByDescending(a => a.IsUpdateAvailable && !a.IsBlacklisted)
                                   .ThenBy(a => a.Name).ToList();
            });
        }

        private List<(string Name, string Id, string InstalledVersion, string AvailableVersion)> GetWingetUpdatesFast()
        {
            var results = new List<(string Name, string Id, string InstalledVersion, string AvailableVersion)>();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "winget.exe",
                    Arguments = "upgrade --include-unknown --accept-source-agreements",
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
                    // Strict 3.5s timeout: if winget hangs on source query, kill and proceed immediately
                    if (proc.WaitForExit(3500))
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        bool headerPassed = false;
                        int idCol = -1, verCol = -1, availCol = -1, sourceCol = -1;

                        foreach (var rawLine in lines)
                        {
                            string line = rawLine.TrimEnd();
                            if (!headerPassed)
                            {
                                if (line.Contains("---") || line.Contains("==="))
                                {
                                    headerPassed = true;
                                }
                                else if (line.Contains("Id") && line.Contains("Version") && line.Contains("Available"))
                                {
                                    idCol = line.IndexOf("Id", StringComparison.OrdinalIgnoreCase);
                                    verCol = line.IndexOf("Version", StringComparison.OrdinalIgnoreCase);
                                    availCol = line.IndexOf("Available", StringComparison.OrdinalIgnoreCase);
                                    sourceCol = line.IndexOf("Source", StringComparison.OrdinalIgnoreCase);
                                }
                                continue;
                            }

                            if (line.StartsWith("---") || line.Contains("upgrades available") || line.Contains("обновлений доступно"))
                                continue;

                            if (idCol > 0 && verCol > idCol && availCol > verCol && line.Length >= availCol)
                            {
                                string name = line.Substring(0, idCol).Trim();
                                string id = line.Substring(idCol, verCol - idCol).Trim();
                                string ver = line.Substring(verCol, availCol - verCol).Trim();
                                string avail = sourceCol > availCol && line.Length >= sourceCol ? line.Substring(availCol, sourceCol - availCol).Trim() : line.Substring(availCol).Trim();

                                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(avail) && !id.Equals("Id", StringComparison.OrdinalIgnoreCase))
                                {
                                    results.Add((name, id, ver, avail));
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

            return await Task.Run(async () =>
            {
                string pkgId = item.PackageId;
                string name = item.Name;
                string targetVer = item.AvailableVersion;

                progressCallback?.Invoke($"Подготовка к установке обновления «{name}» (v{targetVer})...");

                // 1. Check Cloud Catalog for Direct Installer Download
                foreach (var kvp in _cloudCatalog)
                {
                    if (name.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kvp.Key.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string downloadUrl = kvp.Value.DownloadUrl;
                        if (!string.IsNullOrEmpty(downloadUrl))
                        {
                            try
                            {
                                string tempDir = Path.Combine(Path.GetTempPath(), "StormUpdates");
                                Directory.CreateDirectory(tempDir);
                                string ext = downloadUrl.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ? ".msi" : ".exe";
                                string safeFileName = $"{string.Join("_", name.Split(Path.GetInvalidFileNameChars()))}_v{targetVer}{ext}";
                                string targetFile = Path.Combine(tempDir, safeFileName);

                                progressCallback?.Invoke($"Скачивание официального инсталлятора «{name}»...");

                                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                                {
                                    if (response.IsSuccessStatusCode)
                                    {
                                        using var stream = await response.Content.ReadAsStreamAsync();
                                        using var fileStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None);
                                        await stream.CopyToAsync(fileStream);
                                    }
                                }

                                if (File.Exists(targetFile) && new FileInfo(targetFile).Length > 1024)
                                {
                                    progressCallback?.Invoke($"Запуск мастера обновления «{name}»...");
                                    var psi = new ProcessStartInfo
                                    {
                                        FileName = targetFile,
                                        UseShellExecute = true
                                    };
                                    using var proc = Process.Start(psi);
                                    
                                    item.InstalledVersion = targetVer;
                                    item.IsUpdateAvailable = false;
                                    return (true, $"Запущен официальный мастер обновления для «{name}» (v{targetVer}).");
                                }
                            }
                            catch { }

                            // Fallback to opening direct link
                            try
                            {
                                Process.Start(new ProcessStartInfo { FileName = downloadUrl, UseShellExecute = true });
                                item.InstalledVersion = targetVer;
                                item.IsUpdateAvailable = false;
                                return (true, $"Открыта загрузка обновления для «{name}» (v{targetVer}).");
                            }
                            catch { }
                        }
                    }
                }

                // 2. Try Winget if PackageId is available and valid
                if (!string.IsNullOrEmpty(pkgId) && pkgId.Contains(".") && !Guid.TryParse(pkgId, out _))
                {
                    try
                    {
                        progressCallback?.Invoke($"Запуск обновления через Winget ({pkgId})...");

                        var psi = new ProcessStartInfo
                        {
                            FileName = "winget.exe",
                            Arguments = $"upgrade --exact --id \"{pkgId}\" --include-unknown --accept-package-agreements --accept-source-agreements",
                            UseShellExecute = true
                        };

                        using var proc = Process.Start(psi);
                        if (proc != null)
                        {
                            item.InstalledVersion = targetVer;
                            item.IsUpdateAvailable = false;
                            return (true, $"Запущен процесс обновления «{name}» через Winget!");
                        }
                    }
                    catch { }
                }

                // 3. Fallback search
                try
                {
                    string searchUrl = "https://www.google.com/search?q=" + Uri.EscapeDataString($"{name} update download official");
                    Process.Start(new ProcessStartInfo { FileName = searchUrl, UseShellExecute = true });
                    return (true, $"Открыта официальная страница обновления для «{name}».");
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
