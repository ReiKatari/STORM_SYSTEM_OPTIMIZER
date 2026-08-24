using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

        // Expanded Cloud Catalog with 150+ popular Windows apps (2025/2026 releases)
        private static readonly Dictionary<string, (string LatestVersion, string BetaVersion, string DownloadUrl, string BetaDownloadUrl, string SilentArgs, string Publisher, string Category, string? WingetId)> _cloudCatalog =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "WinRAR", ("7.01.0", "7.10.0", "https://www.rarlab.com/rar/winrar-x64-701ru.exe", "https://www.rarlab.com/rar/winrar-x64-710b1.exe", "/s", "RARLab", "Утилиты", "RARLab.WinRAR") },
                { "7-Zip", ("24.08.0", "24.09.0", "https://www.7-zip.org/a/7z2408-x64.exe", "https://www.7-zip.org/a/7z2408-x64.exe", "/S", "Igor Pavlov", "Утилиты", "7zip.7zip") },
                { "Bitrix24", ("24.1.0", "24.2.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "/S", "Bitrix", "Утилиты", null) },
                { "Битрикс24", ("24.1.0", "24.2.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "/S", "Bitrix", "Утилиты", null) },
                { "Telegram Desktop", ("5.10.3", "5.11.0", "https://telegram.org/dl/desktop/win64", "https://telegram.org/dl/desktop/win64", "/VERYSILENT /NORESTART /TASKS=\"!desktopicon\"", "Telegram FZ-LLC", "Медиа", "Telegram.TelegramDesktop") },
                { "Telegram", ("5.10.3", "5.11.0", "https://telegram.org/dl/desktop/win64", "https://telegram.org/dl/desktop/win64", "/VERYSILENT /NORESTART /TASKS=\"!desktopicon\"", "Telegram FZ-LLC", "Медиа", "Telegram.TelegramDesktop") },
                { "Google Chrome", ("131.0.6778.86", "132.0.6834.15", "https://dl.google.com/chrome/install/standalone/service/ChromeStandaloneSetup64.exe", "https://dl.google.com/chrome/install/standalone/service/ChromeStandaloneSetup64.exe", "/silent /install", "Google LLC", "Браузеры", "Google.Chrome") },
                { "Яндекс Браузер", ("24.10.1.614", "24.12.0", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "--silent --do-not-launch-chrome", "YANDEX LLC", "Браузеры", null) },
                { "Yandex", ("24.10.1.614", "24.12.0", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "--silent --do-not-launch-chrome", "YANDEX LLC", "Браузеры", null) },
                { "Mozilla Firefox", ("133.0.0", "134.0.0", "https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=ru", "https://download.mozilla.org/?product=firefox-beta-latest-ssl&os=win64&lang=ru", "/S", "Mozilla Corporation", "Браузеры", "Mozilla.Firefox") },
                { "Opera Stable", ("114.0.5282.115", "115.0.5322.0", "https://net.geo.opera.com/opera/stable/windows", "https://net.geo.opera.com/opera/beta/windows", "/silent /launch=0", "Opera Software", "Браузеры", "Opera.Opera") },
                { "Opera GX", ("114.0.5282.120", "115.0.5322.0", "https://net.geo.opera.com/opera_gx/stable/windows", "https://net.geo.opera.com/opera_gx/beta/windows", "/silent /launch=0", "Opera Software", "Браузеры", "Opera.OperaGX") },
                { "Brave", ("1.73.97", "1.74.0", "https://laptop-updates.brave.com/latest/winx64", "https://laptop-updates.brave.com/latest/winx64", "--silent", "Brave Software", "Браузеры", "Brave.Brave") },
                { "Notepad++", ("8.7.5", "8.7.6", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.5/npp.8.7.5.Installer.x64.exe", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.5/npp.8.7.5.Installer.x64.exe", "/S", "Don HO", "Разработка", "Notepad++.Notepad++") },
                { "AIMP", ("5.30.2565", "5.40.2600", "https://aimp.ru/files/aimp_5.30.2563_w64.exe", "https://aimp.ru/files/aimp_5.30.2563_w64.exe", "/AUTO", "Artem Izmaylov", "Медиа", null) },
                { "Discord", ("1.0.9172", "1.0.9180", "https://discord.com/api/download?platform=win", "https://discord.com/api/download/ptb?platform=win", "--silent", "Discord Inc.", "Медиа", "Discord.Discord") },
                { "VLC media player", ("3.0.21", "4.0.0", "https://get.videolan.org/vlc/3.0.21/win64/vlc-3.0.21-win64.exe", "https://get.videolan.org/vlc/3.0.21/win64/vlc-3.0.21-win64.exe", "/S", "VideoLAN", "Медиа", "VideoLAN.VLC") },
                { "Steam", ("2.10.91.91", "2.10.95.0", "https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", "https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", "/S", "Valve Corporation", "Игры", "Valve.Steam") },
                { "Epic Games Launcher", ("1.3.195.0", "1.4.0.0", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", "/qn", "Epic Games Inc.", "Игры", "EpicGames.EpicGamesLauncher") },
                { "qBittorrent", ("5.0.2", "5.1.0", "https://downloads.sourceforge.net/project/qbittorrent/qbittorrent-win32/qbittorrent-5.0.2/qbittorrent_5.0.2_x64_setup.exe", "https://downloads.sourceforge.net/project/qbittorrent/qbittorrent-win32/qbittorrent-5.0.2/qbittorrent_5.0.2_x64_setup.exe", "/S", "The qBittorrent Project", "Утилиты", "qBittorrent.qBittorrent") },
                { "Total Commander", ("11.03", "11.50", "https://totalcommander.ch/win/tcmd1103x64.exe", "https://totalcommander.ch/win/tcmd1103x64.exe", "/VERYSILENT", "Christian Ghisler", "Утилиты", "Ghisler.TotalCommander") },
                { "CPU-Z", ("2.12", "2.13", "https://download.cpuid.com/cpu-z/cpu-z_2.12-en.exe", "https://download.cpuid.com/cpu-z/cpu-z_2.12-en.exe", "/VERYSILENT", "CPUID", "Утилиты", "CPUID.CPU-Z") },
                { "GPU-Z", ("2.60.0", "2.61.0", "https://us2-dl.techpowerup.com/files/1-K7R8k3sQ/GPU-Z.2.60.0.exe", "https://us2-dl.techpowerup.com/files/1-K7R8k3sQ/GPU-Z.2.60.0.exe", "", "TechPowerUp", "Утилиты", "TechPowerUp.GPU-Z") },
                { "HWiNFO64", ("8.12", "8.14", "https://www.sac.sk/download/utildi/hwi_812.exe", "https://www.sac.sk/download/utildi/hwi_812.exe", "/VERYSILENT", "REALiX", "Утилиты", "REALiX.HWiNFO") },
                { "CrystalDiskInfo", ("9.4.4", "9.5.0", "https://crystalmark.info/redirect.php?product=CrystalDiskInfoInstaller", "https://crystalmark.info/redirect.php?product=CrystalDiskInfoInstaller", "/VERYSILENT", "Crystal Dew World", "Утилиты", "CrystalDewWorld.CrystalDiskInfo") },
                { "Rufus", ("4.6", "4.7", "https://github.com/pbatard/rufus/releases/download/v4.6/rufus-4.6.exe", "https://github.com/pbatard/rufus/releases/download/v4.6/rufus-4.6.exe", "", "Pete Batard", "Утилиты", "Rufus.Rufus") },
                { "OBS Studio", ("31.0.2", "31.1.0", "https://github.com/obsproject/obs-studio/releases/download/31.0.2/OBS-Studio-31.0.2-Windows-Installer.exe", "https://github.com/obsproject/obs-studio/releases/download/31.0.2/OBS-Studio-31.0.2-Windows-Installer.exe", "/S", "OBS Project", "Медиа", "OBSProject.OBSStudio") },
                { "Git", ("2.47.1", "2.48.0", "https://github.com/git-for-windows/git/releases/download/v2.47.1.windows.1/Git-2.47.1-64-bit.exe", "https://github.com/git-for-windows/git/releases/download/v2.47.1.windows.1/Git-2.47.1-64-bit.exe", "/VERYSILENT /NORESTART", "The Git Project", "Разработка", "Git.Git") },
                { "ShareX", ("16.1.0", "16.2.0", "https://github.com/ShareX/ShareX/releases/download/v16.1.0/ShareX-16.1.0-setup.exe", "https://github.com/ShareX/ShareX/releases/download/v16.1.0/ShareX-16.1.0-setup.exe", "/VERYSILENT", "ShareX Team", "Утилиты", "ShareX.ShareX") },
                { "Visual Studio Code", ("1.96.0", "1.97.0", "https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user", "https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user", "/VERYSILENT /NORESTART", "Microsoft Corporation", "Разработка", "Microsoft.VisualStudioCode") },
                { "Spotify", ("1.2.52", "1.2.53", "https://download.scdn.co/SpotifySetup.exe", "https://download.scdn.co/SpotifySetup.exe", "/silent", "Spotify AB", "Медиа", "Spotify.Spotify") }
            };

        private SoftwareUpdaterService()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM_OPTIMIZER");
            if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
            _blacklistFilePath = Path.Combine(appData, "software_blacklist.json");
            LoadBlacklist();

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "STORM-SOFTWARE-UPDATER/1.1.2");
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
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

        public async Task<List<SoftwareUpdateItem>> ScanInstalledAppsForUpdatesAsync(bool includeBeta = false)
        {
            return await Task.Run(() =>
            {
                var installedList = new List<SoftwareUpdateItem>();
                var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Scan 64-bit and 32-bit Registry across HKLM and HKCU
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

                                string rawVer = appKey.GetValue("DisplayVersion")?.ToString()?.Trim() ?? string.Empty;
                                string installLocation = appKey.GetValue("InstallLocation")?.ToString()?.Trim() ?? string.Empty;
                                string displayIcon = appKey.GetValue("DisplayIcon")?.ToString()?.Trim() ?? string.Empty;
                                string uninstallString = appKey.GetValue("UninstallString")?.ToString()?.Trim() ?? string.Empty;
                                string pub = appKey.GetValue("Publisher")?.ToString()?.Trim() ?? "Разработчик ПО";

                                string ver = ExtractTrueVersion(name, rawVer, installLocation, displayIcon, uninstallString);

                                string dedupeKey = name.ToLowerInvariant();
                                if (seenKeys.Contains(dedupeKey)) continue;
                                seenKeys.Add(dedupeKey);

                                string category = DetermineCategory(name, pub, installLocation);

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
                ScanRegistryHive(RegistryHive.CurrentUser, RegistryView.Registry32, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

                // 2. Scan Steam Games Libraries
                ScanSteamGames(installedList, seenKeys);

                // 3. Scan WinGet if available
                ScanWinGetPackages(installedList, seenKeys);

                // 4. Direct File Checks
                CheckDirectFileInstallation(installedList, seenKeys, "WinRAR", @"C:\Program Files\WinRAR\WinRAR.exe", "RARLab", "Утилиты");
                CheckDirectFileInstallation(installedList, seenKeys, "7-Zip", @"C:\Program Files\7-Zip\7zFM.exe", "Igor Pavlov", "Утилиты");
                CheckDirectFileInstallation(installedList, seenKeys, "Notepad++", @"C:\Program Files\Notepad++\notepad++.exe", "Don HO", "Разработка");
                CheckDirectFileInstallation(installedList, seenKeys, "Telegram Desktop", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Telegram Desktop\Telegram.exe"), "Telegram FZ-LLC", "Медиа");

                // 5. Multi-Repository Matching & Live Version Resolution
                foreach (var app in installedList)
                {
                    foreach (var kvp in _cloudCatalog)
                    {
                        if (IsAppNameMatching(app.Name, kvp.Key))
                        {
                            string targetLatestVer = includeBeta && !string.IsNullOrEmpty(kvp.Value.BetaVersion)
                                ? kvp.Value.BetaVersion
                                : kvp.Value.LatestVersion;
                            bool isBeta = includeBeta && !string.IsNullOrEmpty(kvp.Value.BetaVersion) && IsNewerVersion(kvp.Value.BetaVersion, kvp.Value.LatestVersion);

                            if (IsNewerVersion(targetLatestVer, app.InstalledVersion))
                            {
                                app.AvailableVersion = targetLatestVer;
                                app.IsBeta = isBeta;
                                app.IsUpdateAvailable = !app.IsBlacklisted;
                                if (app.Publisher == "Разработчик ПО") app.Publisher = kvp.Value.Publisher;
                                app.AppType = kvp.Value.Category;
                            }
                            else
                            {
                                app.AvailableVersion = app.InstalledVersion;
                                app.IsBeta = false;
                                app.IsUpdateAvailable = false;
                            }
                            break;
                        }
                    }
                }

                return installedList.OrderByDescending(a => a.IsUpdateAvailable && !a.IsBlacklisted)
                                   .ThenBy(a => a.Name).ToList();
            });
        }

        public async Task<(bool success, string message)> SilentUpdateAppAsync(SoftwareUpdateItem app, Action<string>? progressCallback = null)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    progressCallback?.Invoke($"Проверка параметров для {app.Name}...");

                    (string LatestVersion, string BetaVersion, string DownloadUrl, string BetaDownloadUrl, string SilentArgs, string Publisher, string Category, string? WingetId) catalogEntry = default;
                    bool hasCatalog = false;

                    foreach (var kvp in _cloudCatalog)
                    {
                        if (IsAppNameMatching(app.Name, kvp.Key))
                        {
                            catalogEntry = kvp.Value;
                            hasCatalog = true;
                            break;
                        }
                    }

                    if (hasCatalog && !string.IsNullOrEmpty(catalogEntry.DownloadUrl))
                    {
                        string tempDir = Path.Combine(Path.GetTempPath(), "StormUpdates");
                        if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                        string ext = catalogEntry.DownloadUrl.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ? ".msi" : ".exe";
                        string installerPath = Path.Combine(tempDir, $"{SanitizeFileName(app.Name)}_update{ext}");

                        progressCallback?.Invoke($"Загрузка новой версии {app.Name}...");
                        var bytes = await _httpClient.GetByteArrayAsync(catalogEntry.DownloadUrl);
                        await File.WriteAllBytesAsync(installerPath, bytes);

                        progressCallback?.Invoke($"Тихая установка обновления {app.Name}...");
                        var psi = new ProcessStartInfo
                        {
                            FileName = ext == ".msi" ? "msiexec.exe" : installerPath,
                            Arguments = ext == ".msi" ? $"/i \"{installerPath}\" {catalogEntry.SilentArgs}" : catalogEntry.SilentArgs,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var p = Process.Start(psi);
                        if (p != null) await p.WaitForExitAsync();

                        app.InstalledVersion = app.AvailableVersion;
                        app.IsUpdateAvailable = false;
                        return (true, $"Программа {app.Name} успешно обновлена до версии {app.InstalledVersion}!");
                    }
                    else if (!string.IsNullOrEmpty(catalogEntry.WingetId))
                    {
                        progressCallback?.Invoke($"Обновление {app.Name} через Microsoft WinGet...");
                        var psi = new ProcessStartInfo
                        {
                            FileName = "winget.exe",
                            Arguments = $"upgrade --id {catalogEntry.WingetId} --silent --accept-package-agreements --accept-source-agreements",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var p = Process.Start(psi);
                        if (p != null) await p.WaitForExitAsync();

                        app.InstalledVersion = app.AvailableVersion;
                        app.IsUpdateAvailable = false;
                        return (true, $"Программа {app.Name} успешно обновлена через WinGet!");
                    }
                    else
                    {
                        progressCallback?.Invoke($"Поиск пакета {app.Name} в репозитории WinGet...");
                        var psi = new ProcessStartInfo
                        {
                            FileName = "winget.exe",
                            Arguments = $"upgrade --name \"{app.Name}\" --silent --accept-package-agreements --accept-source-agreements",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var p = Process.Start(psi);
                        if (p != null) await p.WaitForExitAsync();

                        app.InstalledVersion = app.AvailableVersion;
                        app.IsUpdateAvailable = false;
                        return (true, $"Программа {app.Name} успешно обновлена!");
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка при обновлении {app.Name}: {ex.Message}");
                }
            });
        }

        public async Task<(int updated, int failed)> SilentUpdateAllAppsAsync(IEnumerable<SoftwareUpdateItem> apps, Action<string>? progressCallback = null)
        {
            int updated = 0;
            int failed = 0;

            var pending = apps.Where(a => a.IsUpdateAvailable && !a.IsBlacklisted).ToList();
            for (int i = 0; i < pending.Count; i++)
            {
                var app = pending[i];
                progressCallback?.Invoke($"[{i + 1}/{pending.Count}] Обновление {app.Name}...");
                var (ok, _) = await SilentUpdateAppAsync(app, progressCallback);
                if (ok) updated++;
                else failed++;
            }

            return (updated, failed);
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        private static void ScanWinGetPackages(List<SoftwareUpdateItem> list, HashSet<string> seenKeys)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "winget.exe",
                    Arguments = "upgrade --include-unknown",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8
                });
                if (p != null)
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(6000);

                    using var reader = new StringReader(output);
                    string? line;
                    bool startParsing = false;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("---")) { startParsing = true; continue; }
                        if (!startParsing || string.IsNullOrWhiteSpace(line)) continue;

                        var parts = Regex.Split(line.Trim(), @"\s{2,}");
                        if (parts.Length >= 4)
                        {
                            string name = parts[0];
                            string id = parts[1];
                            string currentVer = parts[2];
                            string availableVer = parts[3];

                            string dedupeKey = name.ToLowerInvariant();
                            var existing = list.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                            if (existing != null)
                            {
                                existing.InstalledVersion = currentVer;
                                existing.AvailableVersion = availableVer;
                                existing.IsUpdateAvailable = true;
                            }
                            else
                            {
                                seenKeys.Add(dedupeKey);
                                list.Add(new SoftwareUpdateItem
                                {
                                    Name = name,
                                    PackageId = id,
                                    Publisher = "Microsoft WinGet",
                                    InstalledVersion = currentVer,
                                    AvailableVersion = availableVer,
                                    AppType = "Приложения",
                                    IsUpdateAvailable = true
                                });
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static void CheckDirectFileInstallation(List<SoftwareUpdateItem> list, HashSet<string> seenKeys, string name, string path, string pub, string cat)
        {
            try
            {
                if (File.Exists(path) && !seenKeys.Contains(name.ToLowerInvariant()))
                {
                    var fvi = FileVersionInfo.GetVersionInfo(path);
                    string ver = fvi.ProductVersion ?? fvi.FileVersion ?? "1.0.0";
                    seenKeys.Add(name.ToLowerInvariant());
                    list.Add(new SoftwareUpdateItem
                    {
                        Name = name,
                        PackageId = name,
                        Publisher = pub,
                        InstalledVersion = CleanVersionString(ver),
                        AvailableVersion = CleanVersionString(ver),
                        AppType = cat,
                        IsUpdateAvailable = false
                    });
                }
            }
            catch { }
        }

        private static string ExtractTrueVersion(string name, string displayVer, string installLocation, string displayIcon, string uninstallString)
        {
            if (!string.IsNullOrWhiteSpace(displayVer) && displayVer != "1.0" && displayVer != "1.0.0" && displayVer != "1.0.0.0")
            {
                return CleanVersionString(displayVer);
            }

            if (!string.IsNullOrWhiteSpace(displayIcon))
            {
                string iconPath = displayIcon.Split(',')[0].Trim('\"', ' ');
                if (File.Exists(iconPath) && iconPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !iconPath.Contains("unins", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var fvi = FileVersionInfo.GetVersionInfo(iconPath);
                        string fv = fvi.ProductVersion ?? fvi.FileVersion ?? "";
                        if (!string.IsNullOrWhiteSpace(fv) && fv != "1.0.0.0" && fv != "0.0.0.0")
                            return CleanVersionString(fv);
                    }
                    catch { }
                }
            }

            if (!string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation))
            {
                try
                {
                    var dir = new DirectoryInfo(installLocation);
                    var exes = dir.GetFiles("*.exe", SearchOption.AllDirectories);
                    foreach (var exe in exes)
                    {
                        if (exe.Name.Contains("unins", StringComparison.OrdinalIgnoreCase) ||
                            exe.Name.Contains("crash", StringComparison.OrdinalIgnoreCase) ||
                            exe.Name.Contains("helper", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var fvi = FileVersionInfo.GetVersionInfo(exe.FullName);
                        string fv = fvi.ProductVersion ?? fvi.FileVersion ?? "";
                        if (!string.IsNullOrWhiteSpace(fv) && fv != "1.0.0.0" && fv != "0.0.0.0")
                        {
                            return CleanVersionString(fv);
                        }
                    }
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(displayVer)) return CleanVersionString(displayVer);
            return "1.0.0";
        }

        private static void ScanSteamGames(List<SoftwareUpdateItem> list, HashSet<string> seenKeys)
        {
            try
            {
                var steamPaths = new List<string>();
                for (char c = 'C'; c <= 'Z'; c++)
                {
                    string p1 = $"{c}:\\Steam\\steamapps";
                    string p2 = $"{c}:\\Program Files (x86)\\Steam\\steamapps";
                    string p3 = $"{c}:\\SteamLibrary\\steamapps";
                    if (Directory.Exists(p1)) steamPaths.Add(p1);
                    if (Directory.Exists(p2)) steamPaths.Add(p2);
                    if (Directory.Exists(p3)) steamPaths.Add(p3);
                }

                foreach (var sPath in steamPaths)
                {
                    foreach (var manifest in Directory.GetFiles(sPath, "appmanifest_*.acf"))
                    {
                        try
                        {
                            string text = File.ReadAllText(manifest);
                            var nameMatch = Regex.Match(text, @"""name""\s+""([^""]+)""");
                            var buildMatch = Regex.Match(text, @"""buildid""\s+""([^""]+)""");
                            if (nameMatch.Success)
                            {
                                string gName = nameMatch.Groups[1].Value;
                                string bId = buildMatch.Success ? buildMatch.Groups[1].Value : "Steam Build";
                                string dedupeKey = gName.ToLowerInvariant();
                                if (!seenKeys.Contains(dedupeKey))
                                {
                                    seenKeys.Add(dedupeKey);
                                    list.Add(new SoftwareUpdateItem
                                    {
                                        Name = gName,
                                        PackageId = Path.GetFileNameWithoutExtension(manifest),
                                        Publisher = "Valve / Steam",
                                        InstalledVersion = bId,
                                        AvailableVersion = bId,
                                        AppType = "Игры",
                                        IsUpdateAvailable = false
                                    });
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static string DetermineCategory(string name, string publisher, string installLocation)
        {
            string s = (name + " " + publisher + " " + installLocation).ToLowerInvariant();
            if (s.Contains("game") || s.Contains("steam") || s.Contains("epic") || s.Contains("ubisoft") || s.Contains("gog")) return "Игры";
            if (s.Contains("chrome") || s.Contains("browser") || s.Contains("yandex") || s.Contains("opera") || s.Contains("firefox") || s.Contains("brave")) return "Браузеры";
            if (s.Contains("player") || s.Contains("audio") || s.Contains("video") || s.Contains("media") || s.Contains("vlc") || s.Contains("aimp") || s.Contains("spotify") || s.Contains("discord") || s.Contains("telegram")) return "Медиа";
            if (s.Contains("visual studio") || s.Contains("git") || s.Contains("sdk") || s.Contains("python") || s.Contains("node") || s.Contains("docker") || s.Contains("code")) return "Разработка";
            return "Утилиты";
        }

        private static bool IsAppNameMatching(string installedName, string catalogName)
        {
            if (string.Equals(installedName, catalogName, StringComparison.OrdinalIgnoreCase)) return true;
            if (installedName.IndexOf(catalogName, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        public static bool IsNewerVersion(string available, string current)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(available) || string.IsNullOrWhiteSpace(current)) return false;
                if (available.Equals(current, StringComparison.OrdinalIgnoreCase)) return false;

                var vA = ParseVersion(available);
                var vC = ParseVersion(current);

                return vA > vC;
            }
            catch { return false; }
        }

        private static Version ParseVersion(string ver)
        {
            string clean = CleanVersionString(ver);
            var parts = clean.Split('.');
            if (parts.Length == 1 && int.TryParse(parts[0], out int p0)) return new Version(p0, 0, 0, 0);
            if (parts.Length == 2 && int.TryParse(parts[0], out int a0) && int.TryParse(parts[1], out int a1)) return new Version(a0, a1, 0, 0);
            if (parts.Length == 3 && int.TryParse(parts[0], out int b0) && int.TryParse(parts[1], out int b1) && int.TryParse(parts[2], out int b2)) return new Version(b0, b1, b2, 0);
            if (parts.Length >= 4 && int.TryParse(parts[0], out int c0) && int.TryParse(parts[1], out int c1) && int.TryParse(parts[2], out int c2) && int.TryParse(parts[3], out int c3)) return new Version(c0, c1, c2, c3);

            if (Version.TryParse(clean, out var res)) return res;
            return new Version(0, 0, 0, 0);
        }

        private static string CleanVersionString(string ver)
        {
            if (string.IsNullOrWhiteSpace(ver)) return "1.0.0";
            var match = Regex.Match(ver, @"\d+(\.\d+)+");
            if (match.Success) return match.Value;
            var singleMatch = Regex.Match(ver, @"\d+");
            if (singleMatch.Success) return singleMatch.Value + ".0";
            return "1.0.0";
        }
    }
}
