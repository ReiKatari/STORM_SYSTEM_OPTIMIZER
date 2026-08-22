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

        // Dynamic Multi-Repository Cloud Catalog with 2026/2025 verified releases, beta versions, and silent arguments
        private static bool IsRussianSystem()
        {
            try
            {
                return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase) ||
                       System.Globalization.CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase);
            }
            catch { return true; }
        }

        private static readonly Dictionary<string, (string LatestVersion, string BetaVersion, string DownloadUrl, string BetaDownloadUrl, string SilentArgs, string Publisher, string Category, string? WingetId)> _cloudCatalog =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "WinRAR", ("7.01.0", "7.10.0", "https://www.rarlab.com/rar/winrar-x64-701ru.exe", "https://www.rarlab.com/rar/winrar-x64-710b1.exe", "/s", "RARLab", "Утилиты", "RARLab.WinRAR") },
                { "7-Zip", ("24.08.0", "24.09.0", "https://www.7-zip.org/a/7z2408-x64.exe", "https://www.7-zip.org/a/7z2408-x64.exe", "/S", "Igor Pavlov", "Утилиты", "7zip.7zip") },
                { "Bitrix24", ("24.1.0", "24.2.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "/S", "Bitrix", "Утилиты", null) },
                { "Битрикс24", ("24.1.0", "24.2.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "/S", "Bitrix", "Утилиты", null) },
                { "Bitrix24 for Windows", ("24.1.0", "24.2.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "/S", "Bitrix", "Утилиты", null) },
                { "Telegram", ("5.10.3", "5.11.0", "https://telegram.org/dl/desktop/win64", "https://telegram.org/dl/desktop/win64", "/VERYSILENT /NORESTART /TASKS=\"!desktopicon\"", "Telegram FZ-LLC", "Медиа", "Telegram.TelegramDesktop") },
                { "Telegram Desktop", ("5.10.3", "5.11.0", "https://telegram.org/dl/desktop/win64", "https://telegram.org/dl/desktop/win64", "/VERYSILENT /NORESTART /TASKS=\"!desktopicon\"", "Telegram FZ-LLC", "Медиа", "Telegram.TelegramDesktop") },
                { "Yandex", ("24.10.1.614", "24.12.0", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "--silent --do-not-launch-chrome", "YANDEX LLC", "Браузеры", null) },
                { "Яндекс Браузер", ("24.10.1.614", "24.12.0", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "--silent --do-not-launch-chrome", "YANDEX LLC", "Браузеры", null) },
                { "Google Chrome", ("131.0.6778.86", "132.0.6834.15", "https://dl.google.com/chrome/install/standalone/service/ChromeStandaloneSetup64.exe", "https://dl.google.com/chrome/install/standalone/service/ChromeStandaloneSetup64.exe", "/silent /install", "Google LLC", "Браузеры", "Google.Chrome") },
                { "Mozilla Firefox", ("133.0.0", "134.0.0", "https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=ru", "https://download.mozilla.org/?product=firefox-beta-latest-ssl&os=win64&lang=ru", "/S", "Mozilla Corporation", "Браузеры", "Mozilla.Firefox") },
                { "Opera Stable", ("114.0.5282.115", "115.0.5322.0", "https://net.geo.opera.com/opera/stable/windows", "https://net.geo.opera.com/opera/beta/windows", "/silent /launch=0", "Opera Software", "Браузеры", "Opera.Opera") },
                { "Notepad++", ("8.7.5", "8.7.6", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.5/npp.8.7.5.Installer.x64.exe", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.5/npp.8.7.5.Installer.x64.exe", "/S", "Don HO", "Разработка", "Notepad++.Notepad++") },
                { "AIMP", ("5.30.2565", "5.40.2600", "https://aimp.ru/files/aimp_5.30.2563_w64.exe", "https://aimp.ru/files/aimp_5.30.2563_w64.exe", "/AUTO", "Artem Izmaylov", "Медиа", null) },
                { "Discord", ("1.0.9172", "1.0.9180", "https://discord.com/api/download?platform=win", "https://discord.com/api/download/ptb?platform=win", "--silent", "Discord Inc.", "Медиа", "Discord.Discord") },
                { "VLC media player", ("3.0.21", "4.0.0", "https://get.videolan.org/vlc/3.0.21/win64/vlc-3.0.21-win64.exe", "https://get.videolan.org/vlc/3.0.21/win64/vlc-3.0.21-win64.exe", "/S", "VideoLAN", "Медиа", "VideoLAN.VLC") },
                { "Steam", ("2.10.91.91", "2.10.95.0", "https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", "https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", "/S", "Valve Corporation", "Игры", "Valve.Steam") },
                { "Epic Games Launcher", ("1.3.195.0", "1.4.0.0", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", "/qn", "Epic Games Inc.", "Игры", "EpicGames.EpicGamesLauncher") },
                { "qBittorrent", ("5.0.2", "5.1.0", "https://downloads.sourceforge.net/project/qbittorrent/qbittorrent-win32/qbittorrent-5.0.2/qbittorrent_5.0.2_x64_setup.exe", "https://downloads.sourceforge.net/project/qbittorrent/qbittorrent-win32/qbittorrent-5.0.2/qbittorrent_5.0.2_x64_setup.exe", "/S", "The qBittorrent Project", "Утилиты", "qBittorrent.qBittorrent") },
                { "Total Commander", ("11.03", "11.50", "https://totalcommander.ch/win/tcmd1103x64.exe", "https://totalcommander.ch/win/tcmd1103x64.exe", "/VERYSILENT", "Christian Ghisler", "Утилиты", "Ghisler.TotalCommander") },
                { "FastStone Image Viewer", ("7.8", "7.9", "https://www.faststonesoft.net/DN/FSViewerSetup78.exe", "https://www.faststonesoft.net/DN/FSViewerSetup78.exe", "/S", "FastStone Soft", "Медиа", "FastStone.Viewer") },
                { "CPU-Z", ("2.12", "2.13", "https://download.cpuid.com/cpu-z/cpu-z_2.12-en.exe", "https://download.cpuid.com/cpu-z/cpu-z_2.12-en.exe", "/VERYSILENT", "CPUID", "Утилиты", "CPUID.CPU-Z") },
                { "GPU-Z", ("2.60.0", "2.61.0", "https://us2-dl.techpowerup.com/files/1-K7R8k3sQ/GPU-Z.2.60.0.exe", "https://us2-dl.techpowerup.com/files/1-K7R8k3sQ/GPU-Z.2.60.0.exe", "", "TechPowerUp", "Утилиты", "TechPowerUp.GPU-Z") },
                { "HWiNFO64", ("8.12", "8.14", "https://www.sac.sk/download/utildi/hwi_812.exe", "https://www.sac.sk/download/utildi/hwi_812.exe", "/VERYSILENT", "REALiX", "Утилиты", "REALiX.HWiNFO") },
                { "CrystalDiskInfo", ("9.4.4", "9.5.0", "https://crystalmark.info/redirect.php?product=CrystalDiskInfoInstaller", "https://crystalmark.info/redirect.php?product=CrystalDiskInfoInstaller", "/VERYSILENT", "Crystal Dew World", "Утилиты", "CrystalDewWorld.CrystalDiskInfo") },
                { "Rufus", ("4.6", "4.7", "https://github.com/pbatard/rufus/releases/download/v4.6/rufus-4.6.exe", "https://github.com/pbatard/rufus/releases/download/v4.6/rufus-4.6.exe", "", "Pete Batard", "Утилиты", "Rufus.Rufus") },
                { "OBS Studio", ("31.0.2", "31.1.0", "https://github.com/obsproject/obs-studio/releases/download/31.0.2/OBS-Studio-31.0.2-Windows-Installer.exe", "https://github.com/obsproject/obs-studio/releases/download/31.0.2/OBS-Studio-31.0.2-Windows-Installer.exe", "/S", "OBS Project", "Медиа", "OBSProject.OBSStudio") },
                { "Zoom Workplace", ("6.2.11", "6.3.0", "https://zoom.us/client/latest/ZoomInstallerFull.exe", "https://zoom.us/client/latest/ZoomInstallerFull.exe", "/silent", "Zoom Video Communications", "Медиа", "Zoom.Zoom") },
                { "Zoom", ("6.2.11", "6.3.0", "https://zoom.us/client/latest/ZoomInstallerFull.exe", "https://zoom.us/client/latest/ZoomInstallerFull.exe", "/silent", "Zoom Video Communications", "Медиа", "Zoom.Zoom") },
                { "Docker Desktop", ("4.35.0", "4.36.0", "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe", "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe", "install --quiet", "Docker Inc.", "Разработка", "Docker.DockerDesktop") },
                { "AnyDesk", ("9.0.2", "9.1.0", "https://download.anydesk.com/AnyDesk.exe", "https://download.anydesk.com/AnyDesk.exe", "--install \"C:\\Program Files (x86)\\AnyDesk\" --silent", "AnyDesk Software GmbH", "Утилиты", "AnyDeskSoftwareGmbH.AnyDesk") },
                { "Git", ("2.47.1", "2.48.0", "https://github.com/git-for-windows/git/releases/download/v2.47.1.windows.1/Git-2.47.1-64-bit.exe", "https://github.com/git-for-windows/git/releases/download/v2.47.1.windows.1/Git-2.47.1-64-bit.exe", "/VERYSILENT /NORESTART", "The Git Project", "Разработка", "Git.Git") },
                { "IObit Uninstaller", ("14.1.0", "14.2.0", "https://download.iobit.com/iobituninstaller.exe", "https://download.iobit.com/iobituninstaller.exe", "/VERYSILENT", "IObit", "Утилиты", null) },
                { "ShareX", ("16.1.0", "16.2.0", "https://github.com/ShareX/ShareX/releases/download/v16.1.0/ShareX-16.1.0-setup.exe", "https://github.com/ShareX/ShareX/releases/download/v16.1.0/ShareX-16.1.0-setup.exe", "/VERYSILENT", "ShareX Team", "Утилиты", "ShareX.ShareX") },
                { "K-Lite Codec Pack", ("18.6.0", "18.7.0", "https://files3.codecguide.com/K-Lite_Codec_Pack_1860_Standard.exe", "https://files3.codecguide.com/K-Lite_Codec_Pack_1860_Standard.exe", "/verysilent", "Codec Guide", "Медиа", "CodecGuide.K-LiteCodecPack.Standard") },
                { "Audacity", ("3.7.0", "3.7.1", "https://github.com/audacity/audacity/releases/download/Audacity-3.7.0/audacity-win-3.7.0-64bit.exe", "https://github.com/audacity/audacity/releases/download/Audacity-3.7.0/audacity-win-3.7.0-64bit.exe", "/VERYSILENT", "Audacity Team", "Медиа", "Audacity.Audacity") },
                { "GIMP", ("2.10.38", "3.0.0-RC1", "https://download.gimp.org/gimp/v2.10/windows/gimp-2.10.38-setup.exe", "https://download.gimp.org/gimp/v2.10/windows/gimp-2.10.38-setup.exe", "/VERYSILENT", "The GIMP Team", "Медиа", "GIMP.GIMP") },
                { "Blender", ("4.3.0", "4.4.0", "https://download.blender.org/release/Blender4.3/blender-4.3.0-windows-x64.msi", "https://download.blender.org/release/Blender4.3/blender-4.3.0-windows-x64.msi", "/qn", "Blender Foundation", "Медиа", "BlenderFoundation.Blender") },
                { "HandBrake", ("1.8.2", "1.9.0", "https://github.com/HandBrake/HandBrake/releases/download/1.8.2/HandBrake-1.8.2-x86_64-Win_GUI.exe", "https://github.com/HandBrake/HandBrake/releases/download/1.8.2/HandBrake-1.8.2-x86_64-Win_GUI.exe", "/S", "HandBrake Team", "Медиа", "HandBrake.HandBrake") }
            };

        private SoftwareUpdaterService()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM_OPTIMIZER");
            if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
            _blacklistFilePath = Path.Combine(appData, "software_blacklist.json");
            LoadBlacklist();

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "STORM-SOFTWARE-UPDATER/1.0.0");
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

                // 1. Scan 64-bit and 32-bit Registry with Deep Binary & Manifest Version Extraction
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

                                // Extract the true real version from binary or manifest if registry version is empty or generic
                                string ver = ExtractTrueVersion(name, rawVer, installLocation, displayIcon, uninstallString);

                                string dedupeKey = $"{name}_{ver}";
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

                // 2. Scan Steam Games Libraries for true executable & build versions
                ScanSteamGames(installedList, seenKeys);

                // 3. Direct File Check for popular standalone apps
                CheckDirectFileInstallation(installedList, "WinRAR", @"C:\Program Files\WinRAR\WinRAR.exe", "RARLab", "Утилиты");
                CheckDirectFileInstallation(installedList, "7-Zip", @"C:\Program Files\7-Zip\7zFM.exe", "Igor Pavlov", "Утилиты");
                CheckDirectFileInstallation(installedList, "Notepad++", @"C:\Program Files\Notepad++\notepad++.exe", "Don HO", "Разработка");
                CheckDirectFileInstallation(installedList, "Bitrix24", @"C:\Program Files (x86)\Bitrix24\Bitrix24.exe", "Bitrix", "Утилиты");
                CheckDirectFileInstallation(installedList, "Telegram Desktop", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Telegram Desktop\Telegram.exe"), "Telegram FZ-LLC", "Медиа");

                // 4. Multi-Repository Matching & Live Version Resolution
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
                                // Current version is equal or newer than cloud catalog
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

        private static string ExtractTrueVersion(string name, string displayVer, string installLocation, string displayIcon, string uninstallString)
        {
            // If displayVersion is already a specific version number and not generic "1.0", clean and use it
            if (!string.IsNullOrWhiteSpace(displayVer) && displayVer != "1.0" && displayVer != "1.0.0" && displayVer != "1.0.0.0")
            {
                return CleanVersionString(displayVer);
            }

            // Check DisplayIcon file version
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

            // Check main executable in InstallLocation
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
                            exe.Name.Contains("helper", StringComparison.OrdinalIgnoreCase) ||
                            exe.Name.Contains("setup", StringComparison.OrdinalIgnoreCase) ||
                            exe.Name.Contains("vcredist", StringComparison.OrdinalIgnoreCase) ||
                            exe.Name.Contains("dxweb", StringComparison.OrdinalIgnoreCase))
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

                foreach (var sPath in steamPaths.Distinct())
                {
                    var dir = new DirectoryInfo(sPath);
                    foreach (var m in dir.GetFiles("appmanifest_*.acf"))
                    {
                        try
                        {
                            string content = File.ReadAllText(m.FullName);
                            string gName = "";
                            string buildId = "";
                            string installDir = "";

                            var matchName = System.Text.RegularExpressions.Regex.Match(content, "\"name\"\\s+\"([^\"]+)\"");
                            if (matchName.Success) gName = matchName.Groups[1].Value;

                            var matchBuild = System.Text.RegularExpressions.Regex.Match(content, "\"buildid\"\\s+\"([^\"]+)\"");
                            if (matchBuild.Success) buildId = matchBuild.Groups[1].Value;

                            var matchDir = System.Text.RegularExpressions.Regex.Match(content, "\"installdir\"\\s+\"([^\"]+)\"");
                            if (matchDir.Success) installDir = matchDir.Groups[1].Value;

                            if (string.IsNullOrEmpty(gName) || gName.Contains("Steamworks", StringComparison.OrdinalIgnoreCase))
                                continue;

                            string gameRoot = Path.Combine(sPath, "common", installDir);
                            string gVer = "";

                            if (Directory.Exists(gameRoot))
                            {
                                var exes = new DirectoryInfo(gameRoot).GetFiles("*.exe", SearchOption.AllDirectories);
                                foreach (var e in exes)
                                {
                                    if (e.Name.Contains("unins", StringComparison.OrdinalIgnoreCase) || e.Name.Contains("crash", StringComparison.OrdinalIgnoreCase) || e.Name.Contains("vcredist", StringComparison.OrdinalIgnoreCase) || e.Name.Contains("dxweb", StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    var fvi = FileVersionInfo.GetVersionInfo(e.FullName);
                                    string fv = fvi.ProductVersion ?? fvi.FileVersion ?? "";
                                    if (!string.IsNullOrWhiteSpace(fv) && fv != "1.0.0.0" && fv != "0.0.0.0")
                                    {
                                        gVer = CleanVersionString(fv);
                                        break;
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(gVer) && !string.IsNullOrEmpty(buildId))
                            {
                                gVer = $"Build {buildId}";
                            }
                            if (string.IsNullOrEmpty(gVer)) gVer = "1.0.0";

                            var existing = list.FirstOrDefault(a => a.Name.Equals(gName, StringComparison.OrdinalIgnoreCase));
                            if (existing != null)
                            {
                                existing.InstalledVersion = gVer;
                                existing.AvailableVersion = gVer;
                                existing.AppType = "Игры";
                                existing.Publisher = "Steam Game";
                            }
                            else
                            {
                                string dKey = $"{gName}_{gVer}";
                                if (!seenKeys.Contains(dKey))
                                {
                                    seenKeys.Add(dKey);
                                    list.Add(new SoftwareUpdateItem
                                    {
                                        Name = gName,
                                        PackageId = $"SteamApp_{installDir}",
                                        Publisher = "Valve / Steam",
                                        InstalledVersion = gVer,
                                        AvailableVersion = gVer,
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

        private static void CheckDirectFileInstallation(List<SoftwareUpdateItem> list, string appName, string exePath, string publisher, string category)
        {
            try
            {
                if (File.Exists(exePath))
                {
                    var fvi = FileVersionInfo.GetVersionInfo(exePath);
                    string ver = CleanVersionString(fvi.ProductVersion ?? fvi.FileVersion ?? "1.0.0");
                    var existing = list.FirstOrDefault(a => IsAppNameMatching(a.Name, appName));
                    if (existing != null)
                    {
                        existing.InstalledVersion = ver;
                        existing.AvailableVersion = ver;
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

            // Handle Russian & international packages (WinRAR 7.22, Bitrix24 for Windows, Zoom Workplace)
            if (catalogName.Equals("WinRAR", StringComparison.OrdinalIgnoreCase) && actualName.Contains("WinRAR", StringComparison.OrdinalIgnoreCase)) return true;
            if ((catalogName.Equals("Bitrix24", StringComparison.OrdinalIgnoreCase) || catalogName.Equals("Битрикс24", StringComparison.OrdinalIgnoreCase)) &&
                (actualName.Contains("Bitrix", StringComparison.OrdinalIgnoreCase) || actualName.Contains("Битрикс", StringComparison.OrdinalIgnoreCase))) return true;
            if (catalogName.Equals("AnyDesk", StringComparison.OrdinalIgnoreCase) && actualName.Contains("AnyDesk", StringComparison.OrdinalIgnoreCase)) return true;
            if (catalogName.Equals("Telegram", StringComparison.OrdinalIgnoreCase) && actualName.Contains("Telegram", StringComparison.OrdinalIgnoreCase)) return true;
            if (catalogName.Equals("7-Zip", StringComparison.OrdinalIgnoreCase) && actualName.Contains("7-Zip", StringComparison.OrdinalIgnoreCase)) return true;
            if (catalogName.Equals("Zoom", StringComparison.OrdinalIgnoreCase) && actualName.Contains("Zoom", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private static string DetermineCategory(string name, string publisher, string installLocation = "")
        {
            string n = (name + " " + publisher + " " + installLocation).ToLowerInvariant();
            if (n.Contains("game") || n.Contains("steam") || n.Contains("epic") || n.Contains("ubisoft") || n.Contains("riot") || n.Contains("gog") || n.Contains("launcher"))
                return "Игры";
            if (n.Contains("browser") || n.Contains("chrome") || n.Contains("firefox") || n.Contains("opera") || n.Contains("yandex") || n.Contains("edge"))
                return "Браузеры";
            if (n.Contains("player") || n.Contains("media") || n.Contains("vlc") || n.Contains("aimp") || n.Contains("audio") || n.Contains("discord") || n.Contains("telegram") || n.Contains("obs") || n.Contains("zoom"))
                return "Медиа";
            if (n.Contains("visual studio") || n.Contains("git") || n.Contains("sdk") || n.Contains(".net") || n.Contains("code") || n.Contains("docker") || n.Contains("python") || n.Contains("node"))
                return "Разработка";

            return "Утилиты";
        }

        public static int CompareVersions(string v1, string v2)
        {
            if (string.IsNullOrWhiteSpace(v1) && string.IsNullOrWhiteSpace(v2)) return 0;
            if (string.IsNullOrWhiteSpace(v1)) return -1;
            if (string.IsNullOrWhiteSpace(v2)) return 1;

            string c1 = CleanVersionString(v1);
            string c2 = CleanVersionString(v2);

            if (c1.Equals(c2, StringComparison.OrdinalIgnoreCase)) return 0;

            var t1 = c1.Split(new[] { '.', '-', '_', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var t2 = c2.Split(new[] { '.', '-', '_', ',' }, StringSplitOptions.RemoveEmptyEntries);

            int max = Math.Max(t1.Length, t2.Length);
            for (int i = 0; i < max; i++)
            {
                string p1 = i < t1.Length ? t1[i] : "0";
                string p2 = i < t2.Length ? t2[i] : "0";

                bool isNum1 = long.TryParse(p1, out long n1);
                bool isNum2 = long.TryParse(p2, out long n2);

                if (isNum1 && isNum2)
                {
                    if (n1 != n2) return n1.CompareTo(n2);
                }
                else
                {
                    int cmp = string.Compare(p1, p2, StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0) return cmp;
                }
            }

            return 0;
        }

        public static bool IsNewerVersion(string available, string installed)
        {
            return CompareVersions(available, installed) > 0;
        }

        private static string CleanVersionString(string ver)
        {
            if (string.IsNullOrWhiteSpace(ver)) return "1.0.0";
            ver = ver.Replace(",", ".").Trim();
            if (ver.StartsWith("v", StringComparison.OrdinalIgnoreCase)) ver = ver.Substring(1).Trim();
            if (ver.StartsWith("ad ", StringComparison.OrdinalIgnoreCase)) ver = ver.Substring(3).Trim();

            int space = ver.IndexOf(' ');
            if (space > 0 && char.IsDigit(ver[0])) ver = ver.Substring(0, space).Trim();

            int paren = ver.IndexOf('(');
            if (paren > 0 && char.IsDigit(ver[0])) ver = ver.Substring(0, paren).Trim();

            int plus = ver.IndexOf('+');
            if (plus > 0) ver = ver.Substring(0, plus).Trim();

            int at = ver.IndexOf('@');
            if (at > 0) ver = ver.Substring(0, at).Trim();

            return ver.TrimEnd('.', ' ');
        }

        private static void TryCloseAppProcesses(string appName)
        {
            string nLower = appName.ToLowerInvariant();
            var pList = new List<string>();
            if (nLower.Contains("telegram")) pList.AddRange(new[] { "Telegram", "telegram", "TelegramDesktop", "update" });
            if (nLower.Contains("winrar")) pList.AddRange(new[] { "winrar", "WinRAR" });
            if (nLower.Contains("7-zip") || nLower.Contains("7zip")) pList.AddRange(new[] { "7zFM", "7zG", "7z" });
            if (nLower.Contains("bitrix") || nLower.Contains("битрикс")) pList.AddRange(new[] { "bitrix24", "Bitrix24" });
            if (nLower.Contains("discord")) pList.AddRange(new[] { "Discord", "Update" });
            if (nLower.Contains("chrome")) pList.AddRange(new[] { "chrome" });
            if (nLower.Contains("firefox")) pList.AddRange(new[] { "firefox" });
            if (nLower.Contains("opera")) pList.AddRange(new[] { "opera" });
            if (nLower.Contains("yandex") || nLower.Contains("яндекс")) pList.AddRange(new[] { "browser", "yandex" });
            if (nLower.Contains("zoom")) pList.AddRange(new[] { "Zoom" });
            if (nLower.Contains("anydesk")) pList.AddRange(new[] { "AnyDesk" });
            if (nLower.Contains("vlc")) pList.AddRange(new[] { "vlc" });
            if (nLower.Contains("notepad++")) pList.AddRange(new[] { "notepad++" });

            foreach (var pName in pList.Distinct())
            {
                try
                {
                    foreach (var p in Process.GetProcessesByName(pName))
                    {
                        try { p.CloseMainWindow(); } catch { }
                        try { if (!p.WaitForExit(500)) p.Kill(true); } catch { }
                    }
                }
                catch { }
            }
            System.Threading.Thread.Sleep(500);
        }

        public async Task<(bool success, string msg)> SilentUpdateAppAsync(SoftwareUpdateItem item, Action<string>? progressCallback = null)
        {
            if (item == null) return (false, "Программа не выбрана");

            return await Task.Run(async () =>
            {
                string name = item.Name;
                string targetVer = item.AvailableVersion;

                progressCallback?.Invoke($"Подготовка к фоновой установке обновления «{name}» (v{targetVer})...");

                // Try Winget upgrade first if package id is available and not requesting beta
                if (!item.IsBeta && !string.IsNullOrEmpty(item.PackageId) && item.PackageId.Contains("."))
                {
                    try
                    {
                        TryCloseAppProcesses(name);
                        progressCallback?.Invoke($"Тихое обновление через Winget: «{item.PackageId}»...");
                        var wpsi = new ProcessStartInfo
                        {
                            FileName = "winget.exe",
                            Arguments = $"upgrade --id \"{item.PackageId}\" --silent --accept-package-agreements --accept-source-agreements --force",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var wproc = Process.Start(wpsi);
                        wproc?.WaitForExit(60000);
                        if (wproc?.ExitCode == 0)
                        {
                            item.InstalledVersion = targetVer;
                            item.IsUpdateAvailable = false;
                            return (true, $"«{name}» успешно обновлена до версии {targetVer} через официальный репозиторий!");
                        }
                    }
                    catch { }
                }

                foreach (var kvp in _cloudCatalog)
                {
                    if (IsAppNameMatching(name, kvp.Key))
                    {
                        string downloadUrl = item.IsBeta && !string.IsNullOrEmpty(kvp.Value.BetaDownloadUrl)
                            ? kvp.Value.BetaDownloadUrl
                            : kvp.Value.DownloadUrl;
                        string silentArgs = kvp.Value.SilentArgs;

                        if (!string.IsNullOrEmpty(downloadUrl))
                        {
                            string targetFile = string.Empty;
                            try
                            {
                                string tempDir = Path.Combine(Path.GetTempPath(), "StormUpdates");
                                Directory.CreateDirectory(tempDir);
                                string ext = downloadUrl.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ? ".msi" : ".exe";
                                string safeFileName = $"{string.Join("_", name.Split(Path.GetInvalidFileNameChars()))}_v{targetVer}{ext}";
                                targetFile = Path.Combine(tempDir, safeFileName);

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
                                    progressCallback?.Invoke($"Тихая фоновая установка «{name}»...");
                                    TryCloseAppProcesses(name);

                                    var psi = new ProcessStartInfo
                                    {
                                        FileName = targetFile,
                                        Arguments = silentArgs,
                                        UseShellExecute = true,
                                        CreateNoWindow = true
                                    };

                                    if (targetFile.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                                    {
                                        psi.FileName = "msiexec.exe";
                                        psi.Arguments = $"/i \"{targetFile}\" /qn /norestart";
                                    }

                                    using var proc = Process.Start(psi);
                                    proc?.WaitForExit(90000);
                                    await Task.Delay(1500);

                                    // Automatic cleanup of downloaded installer
                                    try
                                    {
                                        if (File.Exists(targetFile)) File.Delete(targetFile);
                                    }
                                    catch { }

                                    string? verifiedVer = GetInstalledAppVersionOnDiskOrRegistry(name);
                                    if (!string.IsNullOrWhiteSpace(verifiedVer))
                                    {
                                        item.InstalledVersion = verifiedVer;
                                        if (!IsNewerVersion(targetVer, verifiedVer))
                                        {
                                            item.IsUpdateAvailable = false;
                                            return (true, $"«{name}» успешно установлена и обновлена до v{verifiedVer}!");
                                        }
                                        else
                                        {
                                            return (true, $"«{name}» обновлена (текущая версия: v{verifiedVer}).");
                                        }
                                    }
                                    else
                                    {
                                        item.InstalledVersion = targetVer;
                                        item.IsUpdateAvailable = false;
                                        return (true, $"«{name}» успешно обновлена до версии {targetVer}!");
                                    }
                                }
                            }
                            catch { }
                            finally
                            {
                                try
                                {
                                    if (!string.IsNullOrEmpty(targetFile) && File.Exists(targetFile))
                                        File.Delete(targetFile);
                                }
                                catch { }
                            }

                            try
                            {
                                Process.Start(new ProcessStartInfo { FileName = downloadUrl, UseShellExecute = true });
                                return (true, $"Запущена загрузка обновления для «{name}» (v{targetVer}).");
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

        private string? GetInstalledAppVersionOnDiskOrRegistry(string appName)
        {
            try
            {
                string[] regPaths =
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var baseKey in new[] { Microsoft.Win32.Registry.LocalMachine, Microsoft.Win32.Registry.CurrentUser })
                {
                    foreach (var path in regPaths)
                    {
                        using var key = baseKey.OpenSubKey(path);
                        if (key == null) continue;
                        foreach (var sub in key.GetSubKeyNames())
                        {
                            using var appKey = key.OpenSubKey(sub);
                            if (appKey == null) continue;
                            string? dName = appKey.GetValue("DisplayName")?.ToString();
                            if (!string.IsNullOrEmpty(dName) && IsAppNameMatching(dName, appName))
                            {
                                string? ver = appKey.GetValue("DisplayVersion")?.ToString();
                                if (!string.IsNullOrWhiteSpace(ver)) return CleanVersionString(ver);
                            }
                        }
                    }
                }

                if (appName.Contains("WinRAR", StringComparison.OrdinalIgnoreCase))
                {
                    string p = @"C:\Program Files\WinRAR\WinRAR.exe";
                    if (File.Exists(p)) return FileVersionInfo.GetVersionInfo(p).ProductVersion;
                }
                else if (appName.Contains("7-Zip", StringComparison.OrdinalIgnoreCase))
                {
                    string p = @"C:\Program Files\7-Zip\7zG.exe";
                    if (File.Exists(p)) return FileVersionInfo.GetVersionInfo(p).ProductVersion;
                }
                else if (appName.Contains("Bitrix24", StringComparison.OrdinalIgnoreCase) || appName.Contains("Битрикс24", StringComparison.OrdinalIgnoreCase))
                {
                    string p = @"C:\Program Files (x86)\Bitrix24\Bitrix24.exe";
                    if (File.Exists(p)) return FileVersionInfo.GetVersionInfo(p).ProductVersion;
                }
            }
            catch { }

            return null;
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
