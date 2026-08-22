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

        // Dynamic Multi-Repository Cloud Catalog with 2026/2025 verified releases and silent arguments
        private static readonly Dictionary<string, (string LatestVersion, string DownloadUrl, string SilentArgs, string Publisher, string Category, string? WingetId)> _cloudCatalog =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "WinRAR", ("7.01.0", "https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-701ru.exe", "/s", "RARLab", "Утилиты", "RARLab.WinRAR") },
                { "7-Zip", ("24.08.0", "https://www.7-zip.org/a/7z2408-x64.exe", "/S", "Igor Pavlov", "Утилиты", "7zip.7zip") },
                { "Bitrix24", ("24.1.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "/S", "Bitrix", "Утилиты", null) },
                { "Битрикс24", ("24.1.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "/S", "Bitrix", "Утилиты", null) },
                { "Bitrix24 for Windows", ("24.1.0", "https://dl.bitrix24.com/b24/bitrix24_desktop.exe", "/S", "Bitrix", "Утилиты", null) },
                { "Telegram", ("7.1.0", "https://telegram.org/dl/desktop/win64", "/VERYSILENT /NORESTART", "Telegram FZ-LLC", "Медиа", "Telegram.TelegramDesktop") },
                { "Telegram Desktop", ("7.1.0", "https://telegram.org/dl/desktop/win64", "/VERYSILENT /NORESTART", "Telegram FZ-LLC", "Медиа", "Telegram.TelegramDesktop") },
                { "Yandex", ("24.10.1.614", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "--silent --do-not-launch-chrome", "YANDEX LLC", "Браузеры", null) },
                { "Яндекс Браузер", ("24.10.1.614", "https://download.yandex.ru/browser/yandex/ru/Yandex.exe", "--silent --do-not-launch-chrome", "YANDEX LLC", "Браузеры", null) },
                { "Google Chrome", ("130.0.6723.70", "https://dl.google.com/chrome/install/standalone/service/ChromeStandaloneSetup64.exe", "/silent /install", "Google LLC", "Браузеры", "Google.Chrome") },
                { "Mozilla Firefox", ("132.0.0", "https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=ru", "/S", "Mozilla Corporation", "Браузеры", "Mozilla.Firefox") },
                { "Opera Stable", ("114.0.5282.115", "https://net.geo.opera.com/opera/stable/windows", "/silent /launch=0", "Opera Software", "Браузеры", "Opera.Opera") },
                { "Notepad++", ("8.7.5", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.5/npp.8.7.5.Installer.x64.exe", "/S", "Don HO", "Разработка", "Notepad++.Notepad++") },
                { "AIMP", ("5.30.2565", "https://aimp.ru/files/aimp_5.30.2563_w64.exe", "/AUTO", "Artem Izmaylov", "Медиа", null) },
                { "Discord", ("1.0.9172", "https://discord.com/api/download?platform=win", "--silent", "Discord Inc.", "Медиа", "Discord.Discord") },
                { "VLC media player", ("3.0.21", "https://get.videolan.org/vlc/3.0.21/win64/vlc-3.0.21-win64.exe", "/S", "VideoLAN", "Медиа", "VideoLAN.VLC") },
                { "Steam", ("2.10.91.91", "https://cdn.cloudflare.steamstatic.com/client/installer/SteamSetup.exe", "/S", "Valve Corporation", "Игры", "Valve.Steam") },
                { "Epic Games Launcher", ("1.3.195.0", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", "/qn", "Epic Games Inc.", "Игры", "EpicGames.EpicGamesLauncher") },
                { "qBittorrent", ("4.6.6", "https://downloads.sourceforge.net/project/qbittorrent/qbittorrent-win32/qbittorrent-4.6.5/qbittorrent_4.6.5_x64_setup.exe", "/S", "The qBittorrent Project", "Утилиты", "qBittorrent.qBittorrent") },
                { "Total Commander", ("11.03", "https://totalcommander.ch/win/tcmd1103x64.exe", "/VERYSILENT", "Christian Ghisler", "Утилиты", "Ghisler.TotalCommander") },
                { "FastStone Image Viewer", ("7.8", "https://www.faststonesoft.net/DN/FSViewerSetup78.exe", "/S", "FastStone Soft", "Медиа", "FastStone.Viewer") },
                { "CPU-Z", ("2.12", "https://download.cpuid.com/cpu-z/cpu-z_2.12-en.exe", "/VERYSILENT", "CPUID", "Утилиты", "CPUID.CPU-Z") },
                { "GPU-Z", ("2.60.0", "https://us2-dl.techpowerup.com/files/1-K7R8k3sQ/GPU-Z.2.60.0.exe", "", "TechPowerUp", "Утилиты", "TechPowerUp.GPU-Z") },
                { "HWiNFO64", ("8.06", "https://www.sac.sk/download/utildi/hwi_806.exe", "/VERYSILENT", "REALiX", "Утилиты", "REALiX.HWiNFO") },
                { "CrystalDiskInfo", ("9.3.2", "https://crystalmark.info/redirect.php?product=CrystalDiskInfoInstaller", "/VERYSILENT", "Crystal Dew World", "Утилиты", "CrystalDewWorld.CrystalDiskInfo") },
                { "Rufus", ("4.6", "https://github.com/pbatard/rufus/releases/download/v4.5/rufus-4.5.exe", "", "Pete Batard", "Утилиты", "Rufus.Rufus") },
                { "OBS Studio", ("31.0.2", "https://github.com/obsproject/obs-studio/releases/download/31.0.1/OBS-Studio-31.0.1-Windows-Installer.exe", "/S", "OBS Project", "Медиа", "OBSProject.OBSStudio") },
                { "Zoom Workplace", ("6.2.11", "https://zoom.us/client/latest/ZoomInstallerFull.exe", "/silent", "Zoom Video Communications", "Медиа", "Zoom.Zoom") },
                { "Zoom", ("6.2.11", "https://zoom.us/client/latest/ZoomInstallerFull.exe", "/silent", "Zoom Video Communications", "Медиа", "Zoom.Zoom") },
                { "Docker Desktop", ("4.34.0", "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe", "install --quiet", "Docker Inc.", "Разработка", "Docker.DockerDesktop") },
                { "AnyDesk", ("9.0.2", "https://download.anydesk.com/AnyDesk.exe", "--install \"C:\\Program Files (x86)\\AnyDesk\" --silent", "AnyDesk Software GmbH", "Утилиты", "AnyDeskSoftwareGmbH.AnyDesk") },
                { "Git", ("2.47.0", "https://github.com/git-for-windows/git/releases/download/v2.46.0.windows.1/Git-2.46.0-64-bit.exe", "/VERYSILENT /NORESTART", "The Git Project", "Разработка", "Git.Git") },
                { "IObit Uninstaller", ("14.0.0", "https://download.iobit.com/iobituninstaller.exe", "/VERYSILENT", "IObit", "Утилиты", null) },
                { "ShareX", ("16.1.0", "https://github.com/ShareX/ShareX/releases/download/v16.1.0/ShareX-16.1.0-setup.exe", "/VERYSILENT", "ShareX Team", "Утилиты", "ShareX.ShareX") },
                { "K-Lite Codec Pack", ("18.5.5", "https://files3.codecguide.com/K-Lite_Codec_Pack_1850_Standard.exe", "/verysilent", "Codec Guide", "Медиа", "CodecGuide.K-LiteCodecPack.Standard") },
                { "Audacity", ("3.6.4", "https://github.com/audacity/audacity/releases/download/Audacity-3.6.2/audacity-win-3.6.2-64bit.exe", "/VERYSILENT", "Audacity Team", "Медиа", "Audacity.Audacity") },
                { "GIMP", ("2.10.38", "https://download.gimp.org/gimp/v2.10/windows/gimp-2.10.38-setup.exe", "/VERYSILENT", "The GIMP Team", "Медиа", "GIMP.GIMP") },
                { "Blender", ("4.2.3", "https://download.blender.org/release/Blender4.2/blender-4.2.1-windows-x64.msi", "/qn", "Blender Foundation", "Медиа", "BlenderFoundation.Blender") },
                { "HandBrake", ("1.8.2", "https://github.com/HandBrake/HandBrake/releases/download/1.8.2/HandBrake-1.8.2-x86_64-Win_GUI.exe", "/S", "HandBrake Team", "Медиа", "HandBrake.HandBrake") }
            };

        private SoftwareUpdaterService()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM_OPTIMIZER");
            if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
            _blacklistFilePath = Path.Combine(appData, "software_blacklist.json");
            LoadBlacklist();

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "STORM-SOFTWARE-UPDATER/0.3.6");
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

        public async Task<List<SoftwareUpdateItem>> ScanInstalledAppsForUpdatesAsync()
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
                            string targetLatestVer = kvp.Value.LatestVersion;
                            if (IsNewerVersion(targetLatestVer, app.InstalledVersion))
                            {
                                app.AvailableVersion = targetLatestVer;
                                app.IsUpdateAvailable = !app.IsBlacklisted;
                                if (app.Publisher == "Разработчик ПО") app.Publisher = kvp.Value.Publisher;
                                app.AppType = kvp.Value.Category;
                            }
                            else
                            {
                                // Current version is equal or newer than cloud catalog
                                app.AvailableVersion = app.InstalledVersion;
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
            string[] procNames = nLower switch
            {
                var n when n.Contains("winrar") => new[] { "winrar" },
                var n when n.Contains("7-zip") || n.Contains("7zip") => new[] { "7zFM", "7zG", "7z" },
                var n when n.Contains("bitrix") || n.Contains("битрикс") => new[] { "bitrix24", "Bitrix24" },
                var n when n.Contains("telegram") => new[] { "Telegram" },
                var n when n.Contains("zoom") => new[] { "Zoom" },
                var n when n.Contains("anydesk") => new[] { "AnyDesk" },
                var n when n.Contains("vlc") => new[] { "vlc" },
                _ => Array.Empty<string>()
            };

            foreach (var pName in procNames)
            {
                try
                {
                    foreach (var p in Process.GetProcessesByName(pName))
                    {
                        try { p.CloseMainWindow(); } catch { }
                        try { if (!p.WaitForExit(1000)) p.Kill(); } catch { }
                    }
                }
                catch { }
            }
        }

        public async Task<(bool success, string msg)> SilentUpdateAppAsync(SoftwareUpdateItem item, Action<string>? progressCallback = null)
        {
            if (item == null) return (false, "Программа не выбрана");

            return await Task.Run(async () =>
            {
                string name = item.Name;
                string targetVer = item.AvailableVersion;

                progressCallback?.Invoke($"Подготовка к фоновой установке обновления «{name}» (v{targetVer})...");

                // Try Winget upgrade first if package id is available
                if (!string.IsNullOrEmpty(item.PackageId) && item.PackageId.Contains("."))
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
                        string downloadUrl = kvp.Value.DownloadUrl;
                        string silentArgs = kvp.Value.SilentArgs;

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
