using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        private readonly HttpClient _httpClient;

        // Comprehensive cloud catalog of official latest versions, categories and direct download links
        private static readonly Dictionary<string, (string LatestVersion, string DownloadUrl, string Publisher, string Category)> _cloudCatalog =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "WinRAR", ("7.10", "https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-701ru.exe", "RARLab", "Утилиты") },
                { "7-Zip", ("24.08", "https://www.7-zip.org/a/7z2408-x64.exe", "Igor Pavlov", "Утилиты") },
                { "Bitrix24", ("24.0.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "Bitrix", "Утилиты") },
                { "Битрикс24", ("24.0.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "Bitrix", "Утилиты") },
                { "Telegram", ("5.5.5", "https://telegram.org/dl/desktop/win64", "Telegram FZ-LLC", "Медиа") },
                { "Telegram Desktop", ("5.5.5", "https://telegram.org/dl/desktop/win64", "Telegram FZ-LLC", "Медиа") },
                { "Yandex", ("24.7.1.1120", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "YANDEX LLC", "Браузеры") },
                { "Яндекс Браузер", ("24.7.1.1120", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "YANDEX LLC", "Браузеры") },
                { "Google Chrome", ("128.0.6613.120", "https://dl.google.com/chrome/install/standalone/service/ChromeStandaloneSetup64.exe", "Google LLC", "Браузеры") },
                { "Mozilla Firefox", ("130.0", "https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=ru", "Mozilla Corporation", "Браузеры") },
                { "Opera Stable", ("113.0.5230.86", "https://net.geo.opera.com/opera/stable/windows", "Opera Software", "Браузеры") },
                { "Notepad++", ("8.7.5", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.5/npp.8.7.5.Installer.x64.exe", "Don HO", "Разработка") },
                { "AIMP", ("5.30.2563", "https://aimp.ru/files/aimp_5.30.2563_w64.exe", "Artem Izmaylov", "Медиа") },
                { "Discord", ("1.0.9168", "https://discord.com/api/download?platform=win", "Discord Inc.", "Медиа") },
                { "VLC media player", ("3.0.21", "https://get.videolan.org/vlc/3.0.21/win64/vlc-3.0.21-win64.exe", "VideoLAN", "Медиа") },
                { "Steam", ("1.0.0.79", "https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", "Valve Corporation", "Игры") },
                { "Epic Games Launcher", ("1.3.193.0", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", "Epic Games Inc.", "Игры") },
                { "qBittorrent", ("4.6.5", "https://downloads.sourceforge.net/project/qbittorrent/qbittorrent-win32/qbittorrent-4.6.5/qbittorrent_4.6.5_x64_setup.exe", "The qBittorrent Project", "Утилиты") },
                { "Total Commander", ("11.03", "https://totalcommander.ch/win/tcmd1103x64.exe", "Christian Ghisler", "Утилиты") },
                { "FastStone Image Viewer", ("7.8", "https://www.faststonesoft.net/DN/FSViewerSetup78.exe", "FastStone Soft", "Медиа") },
                { "CPU-Z", ("2.12", "https://download.cpuid.com/cpu-z/cpu-z_2.12-en.exe", "CPUID", "Утилиты") },
                { "GPU-Z", ("2.60.0", "https://us2-dl.techpowerup.com/files/1-K7R8k3sQ/GPU-Z.2.60.0.exe", "TechPowerUp", "Утилиты") },
                { "HWiNFO64", ("8.06", "https://www.sac.sk/download/utildi/hwi_806.exe", "REALiX", "Утилиты") },
                { "CrystalDiskInfo", ("9.3.2", "https://crystalmark.info/redirect.php?product=CrystalDiskInfoInstaller", "Crystal Dew World", "Утилиты") },
                { "Rufus", ("4.5", "https://github.com/pbatard/rufus/releases/download/v4.5/rufus-4.5.exe", "Pete Batard", "Утилиты") },
                { "OBS Studio", ("31.0.1", "https://github.com/obsproject/obs-studio/releases/download/31.0.1/OBS-Studio-31.0.1-Windows-Installer.exe", "OBS Project", "Медиа") },
                { "Zoom", ("6.1.5", "https://zoom.us/client/latest/ZoomInstallerFull.exe", "Zoom Video Communications", "Медиа") },
                { "Docker Desktop", ("4.33.1", "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe", "Docker Inc.", "Разработка") },
                { "AnyDesk", ("9.0.0", "https://download.anydesk.com/AnyDesk.exe", "AnyDesk Software GmbH", "Утилиты") },
                { "Git", ("2.46.0", "https://github.com/git-for-windows/git/releases/download/v2.46.0.windows.1/Git-2.46.0-64-bit.exe", "The Git Project", "Разработка") },
                { "IObit Uninstaller", ("13.6.0.4", "https://download.iobit.com/iobituninstaller.exe", "IObit", "Утилиты") },
                { "ShareX", ("16.1.0", "https://github.com/ShareX/ShareX/releases/download/v16.1.0/ShareX-16.1.0-setup.exe", "ShareX Team", "Утилиты") },
                { "K-Lite Codec Pack", ("18.5.0", "https://files3.codecguide.com/K-Lite_Codec_Pack_1850_Standard.exe", "Codec Guide", "Медиа") },
                { "Audacity", ("3.6.2", "https://github.com/audacity/audacity/releases/download/Audacity-3.6.2/audacity-win-3.6.2-64bit.exe", "Audacity Team", "Медиа") },
                { "GIMP", ("2.10.38", "https://download.gimp.org/gimp/v2.10/windows/gimp-2.10.38-setup.exe", "The GIMP Team", "Медиа") },
                { "Blender", ("4.2.1", "https://download.blender.org/release/Blender4.2/blender-4.2.1-windows-x64.msi", "Blender Foundation", "Медиа") },
                { "HandBrake", ("1.8.2", "https://github.com/HandBrake/HandBrake/releases/download/1.8.2/HandBrake-1.8.2-x86_64-Win_GUI.exe", "HandBrake Team", "Медиа") }
            };

        private SoftwareUpdaterService()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM_OPTIMIZER");
            if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
            _blacklistFilePath = Path.Combine(appData, "software_blacklist.json");
            LoadBlacklist();

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "STORM-SOFTWARE-UPDATER/0.3.2");
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

                // 1. Ultra-fast Registry scan
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

                                string category = DetermineCategory(name, pub);

                                installedList.Add(new SoftwareUpdateItem
                                {
                                    Name = name,
                                    PackageId = subName,
                                    Publisher = pub,
                                    InstalledVersion = ver,
                                    AvailableVersion = ver,
                                    AppType = category,
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

                // 2. Direct File Scanner for common standalone apps (WinRAR, 7-Zip, Notepad++, Git, Telegram)
                CheckDirectFileInstallation(installedList, "WinRAR", @"C:\Program Files\WinRAR\WinRAR.exe", "RARLab", "Утилиты");
                CheckDirectFileInstallation(installedList, "7-Zip", @"C:\Program Files\7-Zip\7zFM.exe", "Igor Pavlov", "Утилиты");
                CheckDirectFileInstallation(installedList, "Notepad++", @"C:\Program Files\Notepad++\notepad++.exe", "Don HO", "Разработка");
                CheckDirectFileInstallation(installedList, "Telegram Desktop", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Telegram Desktop\Telegram.exe"), "Telegram FZ-LLC", "Медиа");

                // 3. Match against Cloud Catalog
                foreach (var app in installedList)
                {
                    foreach (var kvp in _cloudCatalog)
                    {
                        if (IsAppNameMatching(app.Name, kvp.Key))
                        {
                            string cloudVer = kvp.Value.LatestVersion;
                            if (IsNewerVersion(cloudVer, app.InstalledVersion))
                            {
                                app.AvailableVersion = cloudVer;
                                app.IsUpdateAvailable = !app.IsBlacklisted;
                                if (app.Publisher == "Разработчик ПО") app.Publisher = kvp.Value.Publisher;
                                app.AppType = kvp.Value.Category;
                            }
                            break;
                        }
                    }
                }

                return installedList.OrderByDescending(a => a.IsUpdateAvailable && !a.IsBlacklisted)
                                   .ThenBy(a => a.Name).ToList();
            });
        }

        private static void CheckDirectFileInstallation(List<SoftwareUpdateItem> list, string appName, string exePath, string publisher, string category)
        {
            try
            {
                if (File.Exists(exePath))
                {
                    var fvi = FileVersionInfo.GetVersionInfo(exePath);
                    string ver = fvi.FileVersion?.Trim() ?? fvi.ProductVersion?.Trim() ?? "1.0.0";
                    var existing = list.FirstOrDefault(a => IsAppNameMatching(a.Name, appName));
                    if (existing != null)
                    {
                        if (string.IsNullOrEmpty(existing.InstalledVersion) || existing.InstalledVersion == "1.0.0")
                        {
                            existing.InstalledVersion = ver;
                        }
                    }
                    else
                    {
                        list.Add(new SoftwareUpdateItem
                        {
                            Name = appName,
                            PackageId = appName,
                            Publisher = publisher,
                            InstalledVersion = ver,
                            AvailableVersion = ver,
                            AppType = category,
                            IsUpdateAvailable = false
                        });
                    }
                }
            }
            catch { }
        }

        private static bool IsAppNameMatching(string actualName, string catalogName)
        {
            if (string.IsNullOrWhiteSpace(actualName) || string.IsNullOrWhiteSpace(catalogName)) return false;
            if (actualName.Equals(catalogName, StringComparison.OrdinalIgnoreCase)) return true;
            if (actualName.StartsWith(catalogName + " ", StringComparison.OrdinalIgnoreCase)) return true;
            if (actualName.StartsWith(catalogName + "-", StringComparison.OrdinalIgnoreCase)) return true;
            if (actualName.StartsWith(catalogName + "(", StringComparison.OrdinalIgnoreCase)) return true;

            // Handle Russian versions like "WinRAR 7.01 (64-разрядная)"
            if (actualName.IndexOf(catalogName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (catalogName.Equals("WinRAR", StringComparison.OrdinalIgnoreCase) && actualName.Contains("WinRAR", StringComparison.OrdinalIgnoreCase)) return true;
                if (catalogName.Equals("Bitrix24", StringComparison.OrdinalIgnoreCase) && actualName.Contains("Bitrix", StringComparison.OrdinalIgnoreCase)) return true;
                if (catalogName.Equals("AnyDesk", StringComparison.OrdinalIgnoreCase) && actualName.Contains("AnyDesk", StringComparison.OrdinalIgnoreCase)) return true;
                if (catalogName.Equals("Telegram", StringComparison.OrdinalIgnoreCase) && actualName.Contains("Telegram", StringComparison.OrdinalIgnoreCase)) return true;
                if (catalogName.Equals("7-Zip", StringComparison.OrdinalIgnoreCase) && actualName.Contains("7-Zip", StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static string DetermineCategory(string name, string publisher)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("game") || n.Contains("launcher") || n.Contains("steam") || n.Contains("epic") || n.Contains("ubisoft") || n.Contains("ea app") || n.Contains("riot"))
                return "Игры";
            if (n.Contains("browser") || n.Contains("chrome") || n.Contains("firefox") || n.Contains("opera") || n.Contains("yandex") || n.Contains("edge"))
                return "Браузеры";
            if (n.Contains("player") || n.Contains("media") || n.Contains("vlc") || n.Contains("aimp") || n.Contains("audio") || n.Contains("discord") || n.Contains("telegram") || n.Contains("obs"))
                return "Медиа";
            if (n.Contains("visual studio") || n.Contains("git") || n.Contains("sdk") || n.Contains(".net") || n.Contains("code") || n.Contains("docker") || n.Contains("python") || n.Contains("node"))
                return "Разработка";

            return "Утилиты";
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
                string name = item.Name;
                string targetVer = item.AvailableVersion;

                progressCallback?.Invoke($"Подготовка к установке обновления «{name}» (v{targetVer})...");

                foreach (var kvp in _cloudCatalog)
                {
                    if (IsAppNameMatching(name, kvp.Key))
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

                                progressCallback?.Invoke($"Скачивание инсталлятора «{name}»...");

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
                                    Process.Start(psi);
                                    
                                    item.InstalledVersion = targetVer;
                                    item.IsUpdateAvailable = false;
                                    return (true, $"Запущен официальный мастер обновления для «{name}» (v{targetVer}).");
                                }
                            }
                            catch { }

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
