using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class SoftwareUninstallerService
    {
        private static SoftwareUninstallerService? _instance;
        public static SoftwareUninstallerService Instance => _instance ??= new SoftwareUninstallerService();

        private static List<InstalledAppItem>? _cachedApps;
        private static DateTime _lastCacheTime;
        private static readonly object _cacheLock = new();

        public static void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _cachedApps = null;
            }
        }

        private SoftwareUninstallerService() { }

        public async Task<List<InstalledAppItem>> GetInstalledAppsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh)
            {
                lock (_cacheLock)
                {
                    if (_cachedApps != null && (DateTime.UtcNow - _lastCacheTime).TotalMinutes < 5)
                    {
                        return _cachedApps.Select(a => a.Clone()).ToList();
                    }
                }
            }

            return await Task.Run(() =>
            {
                var apps = new Dictionary<string, InstalledAppItem>(StringComparer.OrdinalIgnoreCase);

                // 1. Scan 64-bit Registry
                ScanRegistryRoot(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", apps);
                // 2. Scan 32-bit Registry (WOW6432Node)
                ScanRegistryRoot(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", apps);
                // 3. Scan Current User Registry
                ScanRegistryRoot(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall", apps);

                // 4. Scan Steam Games across all drives & libraries
                ScanSteamGames(apps);

                // 5. Scan Windows Store / UWP Apps
                ScanWindowsStoreApps(apps);

                var result = apps.Values.OrderBy(a => a.DisplayName).ToList();
                lock (_cacheLock)
                {
                    _cachedApps = result;
                    _lastCacheTime = DateTime.UtcNow;
                }
                return result;
            });
        }

        private void ScanRegistryRoot(RegistryKey root, string subKeyPath, Dictionary<string, InstalledAppItem> apps)
        {
            try
            {
                using var key = root.OpenSubKey(subKeyPath);
                if (key == null) return;

                foreach (var appSubKeyName in key.GetSubKeyNames())
                {
                    try
                    {
                        using var appKey = key.OpenSubKey(appSubKeyName);
                        if (appKey == null) continue;

                        string name = appKey.GetValue("DisplayName")?.ToString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrEmpty(name) ||
                            name.StartsWith("KB", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("Update for", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("Обновление для", StringComparison.OrdinalIgnoreCase))
                            continue;

                        int systemComponent = (int)(appKey.GetValue("SystemComponent") ?? 0);
                        if (systemComponent == 1 && !name.Contains("STORM", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string uninstall = appKey.GetValue("UninstallString")?.ToString()?.Trim() ?? string.Empty;
                        string quietUninstall = appKey.GetValue("QuietUninstallString")?.ToString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrEmpty(uninstall) && string.IsNullOrEmpty(quietUninstall))
                            continue;

                        string rawVersion = appKey.GetValue("DisplayVersion")?.ToString()?.Trim() ?? string.Empty;
                        string publisher = appKey.GetValue("Publisher")?.ToString()?.Trim() ?? string.Empty;
                        string location = appKey.GetValue("InstallLocation")?.ToString()?.Trim() ?? string.Empty;
                        string icon = appKey.GetValue("DisplayIcon")?.ToString()?.Trim() ?? string.Empty;
                        string date = appKey.GetValue("InstallDate")?.ToString()?.Trim() ?? string.Empty;

                        double sizeMb = 0;
                        var estimatedSize = appKey.GetValue("EstimatedSize");
                        if (estimatedSize is int sizeKb)
                        {
                            sizeMb = Math.Round(sizeKb / 1024.0, 1);
                        }
                        else if (estimatedSize is long sizeKbL)
                        {
                            sizeMb = Math.Round(sizeKbL / 1024.0, 1);
                        }

                        string type = "Программа";
                        if (name.Contains("Game", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Steam", StringComparison.OrdinalIgnoreCase) ||
                            location.Contains("SteamApps", StringComparison.OrdinalIgnoreCase) ||
                            location.Contains("Games", StringComparison.OrdinalIgnoreCase) ||
                            location.Contains("Epic Games", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Cyberpunk", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Grand Theft Auto", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Dota", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Witcher", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Counter-Strike", StringComparison.OrdinalIgnoreCase))
                        {
                            type = "Игра";
                        }
                        else if (uninstall.Contains("ms-resource:", StringComparison.OrdinalIgnoreCase) || location.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
                        {
                            type = "Магазин Windows";
                        }

                        if (sizeMb == 0)
                        {
                            sizeMb = type == "Игра" ? 12400.0 : (type == "Магазин Windows" || type == "Windows Store" ? 280.0 : 150.0);
                        }

                        // Fast version resolution without blocking disk reads if raw registry version exists
                        string accurateVersion = !string.IsNullOrWhiteSpace(rawVersion) 
                            ? rawVersion 
                            : ExtractAccurateVersion(location, icon, rawVersion);

                        if (!apps.ContainsKey(name))
                        {
                            apps[name] = new InstalledAppItem
                            {
                                DisplayName = name,
                                DisplayVersion = accurateVersion,
                                Publisher = string.IsNullOrEmpty(publisher) ? "Не указан" : publisher,
                                InstallLocation = location,
                                UninstallString = uninstall,
                                QuietUninstallString = quietUninstall,
                                DisplayIconPath = icon,
                                InstallDate = FormatInstallDate(date),
                                EstimatedSizeMb = sizeMb,
                                AppType = type,
                                RegistryKeyPath = $@"{root.Name}\{subKeyPath}\{appSubKeyName}",
                                IconSource = null
                            };
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static string ExtractAccurateVersion(string? location, string? icon, string fallbackVersion)
        {
            // 1. Try icon target file
            if (!string.IsNullOrWhiteSpace(icon))
            {
                try
                {
                    string target = icon.Split(',')[0].Trim('\"');
                    if (File.Exists(target) && target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        var vi = FileVersionInfo.GetVersionInfo(target);
                        string v = !string.IsNullOrWhiteSpace(vi.FileVersion) ? vi.FileVersion.Trim() : (vi.ProductVersion?.Trim() ?? string.Empty);
                        if (!string.IsNullOrWhiteSpace(v) && v != "0.0.0.0" && v != "1.0.0.0")
                        {
                            return v.Split('(')[0].Trim();
                        }
                    }
                }
                catch { }
            }

            // 2. Try main folder binary
            if (!string.IsNullOrWhiteSpace(location) && Directory.Exists(location))
            {
                string binVer = ExtractBinaryVersionFromFolder(location);
                if (!string.IsNullOrEmpty(binVer))
                {
                    return binVer;
                }
            }

            // 3. Fallback to registry version
            if (!string.IsNullOrWhiteSpace(fallbackVersion) && fallbackVersion != "Steam Edition")
            {
                return fallbackVersion;
            }

            return "1.0.0";
        }

        public static string ExtractBinaryVersionFromFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return string.Empty;

            try
            {
                var exes = Directory.GetFiles(folderPath, "*.exe", new System.IO.EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    MaxRecursionDepth = 2,
                    IgnoreInaccessible = true
                });

                foreach (var exe in exes)
                {
                    string fileName = Path.GetFileName(exe);
                    if (fileName.StartsWith("unins", StringComparison.OrdinalIgnoreCase) ||
                        fileName.StartsWith("crash", StringComparison.OrdinalIgnoreCase) ||
                        fileName.StartsWith("setup", StringComparison.OrdinalIgnoreCase) ||
                        fileName.StartsWith("vcredist", StringComparison.OrdinalIgnoreCase) ||
                        fileName.StartsWith("dxsetup", StringComparison.OrdinalIgnoreCase) ||
                        fileName.StartsWith("oalinst", StringComparison.OrdinalIgnoreCase) ||
                        fileName.StartsWith("webclient", StringComparison.OrdinalIgnoreCase) ||
                        fileName.StartsWith("bugsplat", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        var vi = FileVersionInfo.GetVersionInfo(exe);
                        string? pv = vi.ProductVersion?.Trim();
                        string? fv = vi.FileVersion?.Trim();

                        string ver = !string.IsNullOrEmpty(pv) && pv != "1.0.0.0" && pv != "0.0.0.0"
                            ? pv
                            : (!string.IsNullOrEmpty(fv) && fv != "1.0.0.0" && fv != "0.0.0.0" ? fv : string.Empty);

                        if (!string.IsNullOrEmpty(ver))
                        {
                            ver = ver.Split('(', ',')[0].Trim();
                            return ver;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return string.Empty;
        }

        private void ScanSteamGames(Dictionary<string, InstalledAppItem> apps)
        {
            try
            {
                var steamPaths = new List<string>();

                // 1. Registry Steam path
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    string? sp = key?.GetValue("SteamPath")?.ToString();
                    if (!string.IsNullOrEmpty(sp) && Directory.Exists(sp))
                    {
                        steamPaths.Add(sp);
                    }
                }

                // 2. Scan all drives for Steam libraries
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;
                    try
                    {
                        string root = drive.RootDirectory.FullName;
                        string possibleSteam = Path.Combine(root, "Steam");
                        string possibleSteamLib = Path.Combine(root, "SteamLibrary");
                        if (Directory.Exists(possibleSteam) && !steamPaths.Contains(possibleSteam, StringComparer.OrdinalIgnoreCase))
                            steamPaths.Add(possibleSteam);
                        if (Directory.Exists(possibleSteamLib) && !steamPaths.Contains(possibleSteamLib, StringComparer.OrdinalIgnoreCase))
                            steamPaths.Add(possibleSteamLib);
                    }
                    catch { }
                }

                var libraryFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Parse libraryfolders.vdf
                foreach (var sp in steamPaths)
                {
                    libraryFolders.Add(sp);
                    string vdfPath = Path.Combine(sp, "steamapps", "libraryfolders.vdf");
                    if (File.Exists(vdfPath))
                    {
                        try
                        {
                            foreach (var line in File.ReadAllLines(vdfPath))
                            {
                                if (line.Contains("\"path\""))
                                {
                                    var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 3)
                                    {
                                        string libPath = parts[parts.Length - 1].Replace(@"\\", @"\");
                                        if (Directory.Exists(libPath))
                                        {
                                            libraryFolders.Add(libPath);
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                foreach (var lib in libraryFolders)
                {
                    string steamApps = Path.Combine(lib, "steamapps");
                    if (!Directory.Exists(steamApps)) continue;

                    foreach (var manifestFile in Directory.GetFiles(steamApps, "appmanifest_*.acf"))
                    {
                        try
                        {
                            var lines = File.ReadAllLines(manifestFile);
                            string gameName = string.Empty;
                            string appid = string.Empty;
                            string installdir = string.Empty;
                            string buildid = string.Empty;
                            long sizeBytes = 0;

                            foreach (var line in lines)
                            {
                                if (line.Contains("\"name\""))
                                {
                                    var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 3) gameName = parts[parts.Length - 1];
                                }
                                else if (line.Contains("\"appid\""))
                                {
                                    var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 3) appid = parts[parts.Length - 1];
                                }
                                else if (line.Contains("\"installdir\""))
                                {
                                    var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 3) installdir = parts[parts.Length - 1];
                                }
                                else if (line.Contains("\"buildid\""))
                                {
                                    var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 3) buildid = parts[parts.Length - 1];
                                }
                                else if (line.Contains("\"SizeOnDisk\""))
                                {
                                    var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 3 && long.TryParse(parts[parts.Length - 1], out long sz))
                                        sizeBytes = sz;
                                }
                            }

                            if (string.IsNullOrEmpty(gameName) || apps.ContainsKey(gameName)) continue;

                            string fullGameDir = !string.IsNullOrEmpty(installdir)
                                ? Path.Combine(steamApps, "common", installdir)
                                : string.Empty;

                            string realVersion = string.Empty;

                            if (!string.IsNullOrEmpty(fullGameDir) && Directory.Exists(fullGameDir))
                            {
                                realVersion = ExtractBinaryVersionFromFolder(fullGameDir);
                            }

                            if (string.IsNullOrEmpty(realVersion))
                            {
                                realVersion = !string.IsNullOrEmpty(buildid) ? $"Build {buildid}" : "v1.0";
                            }

                            double sizeMb = sizeBytes > 0 ? Math.Round(sizeBytes / (1024.0 * 1024.0), 1) : 14200.0;

                            apps[gameName] = new InstalledAppItem
                            {
                                DisplayName = gameName,
                                DisplayVersion = realVersion,
                                Publisher = "Steam Games",
                                InstallLocation = fullGameDir,
                                AppType = "Игра",
                                EstimatedSizeMb = sizeMb,
                                UninstallString = $"steam://uninstall/{appid}",
                                ManifestFilePath = manifestFile
                            };
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private string FormatInstallDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate)) return "Ранее";
            if (rawDate.Length == 8 && int.TryParse(rawDate, out _))
            {
                return $"{rawDate.Substring(6, 2)}.{rawDate.Substring(4, 2)}.{rawDate.Substring(0, 4)}";
            }
            return rawDate;
        }

        public async Task ScanResidualClutterAsync(InstalledAppItem app)
        {
            await Task.Run(() =>
            {
                var foundDirs = new List<string>();
                var foundRegs = new List<string>();
                double sizeMb = 0;

                var aliases = GetAppSearchAliases(app.DisplayName, app.Publisher);
                string safePub = CleanForSearch(app.Publisher);

                if (aliases.Count == 0) return;

                // 1. Scan filesystem folders
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow");
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string docsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string savedGames = Path.Combine(userProfile, "Saved Games");
                string localPrograms = Path.Combine(localAppData, "Programs");
                string tempDir = Path.GetTempPath();
                string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                string commonFiles = Path.Combine(progFiles, "Common Files");
                string commonFilesX86 = Path.Combine(progFilesX86, "Common Files");

                var baseDirs = new List<string>
                {
                    appData, localAppData, localLow, programData, docsDir, savedGames,
                    localPrograms, tempDir, progFiles, progFilesX86, commonFiles, commonFilesX86
                };

                // Add secondary drives Program Files and Games (e.g. D:\Program Files, D:\Games, E:\Games)
                try
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady && drive.DriveType == DriveType.Fixed && !drive.RootDirectory.FullName.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
                        {
                            string dRoot = drive.RootDirectory.FullName;
                            string dProg = Path.Combine(dRoot, "Program Files");
                            string dProgX86 = Path.Combine(dRoot, "Program Files (x86)");
                            string dGames = Path.Combine(dRoot, "Games");
                            if (Directory.Exists(dProg)) baseDirs.Add(dProg);
                            if (Directory.Exists(dProgX86)) baseDirs.Add(dProgX86);
                            if (Directory.Exists(dGames)) baseDirs.Add(dGames);
                        }
                    }
                }
                catch { }

                foreach (var baseDir in baseDirs)
                {
                    if (!Directory.Exists(baseDir)) continue;
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(baseDir))
                        {
                            string dirName = Path.GetFileName(dir);
                            if (IsMatchForLeftover(dirName, aliases, safePub))
                            {
                                if (!foundDirs.Contains(dir, StringComparer.OrdinalIgnoreCase))
                                {
                                    foundDirs.Add(dir);
                                    try
                                    {
                                        var di = new DirectoryInfo(dir);
                                        long bytes = di.EnumerateFiles("*", new System.IO.EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 4, IgnoreInaccessible = true }).Sum(f => f.Length);
                                        sizeMb += bytes / (1024.0 * 1024.0);
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Check install location itself if still present
                if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation) && !foundDirs.Contains(app.InstallLocation, StringComparer.OrdinalIgnoreCase))
                {
                    foundDirs.Add(app.InstallLocation);
                    try
                    {
                        var di = new DirectoryInfo(app.InstallLocation);
                        long bytes = di.EnumerateFiles("*", new System.IO.EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 4, IgnoreInaccessible = true }).Sum(f => f.Length);
                        sizeMb += bytes / (1024.0 * 1024.0);
                    }
                    catch { }
                }

                // 2. Scan Registry Keys
                ScanRegistryForLeftovers(Registry.CurrentUser, @"Software", aliases, safePub, foundRegs);
                ScanRegistryForLeftovers(Registry.LocalMachine, @"SOFTWARE", aliases, safePub, foundRegs);
                ScanRegistryForLeftovers(Registry.LocalMachine, @"SOFTWARE\WOW6432Node", aliases, safePub, foundRegs);
                ScanRegistryForLeftovers(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services", aliases, safePub, foundRegs);
                ScanRegistryForLeftovers(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", aliases, safePub, foundRegs);
                ScanRegistryForLeftovers(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", aliases, safePub, foundRegs);
                ScanRegistryForLeftovers(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", aliases, safePub, foundRegs);

                // Scan Uninstall hives for leftover uninstallation keys
                ScanUninstallHivesForLeftovers(aliases, foundRegs);

                app.FoundFolders = foundDirs;
                app.FoundRegistryKeys = foundRegs;
                app.ResidualFilesCount = foundDirs.Count;
                app.ResidualRegistryCount = foundRegs.Count;
                app.ResidualSizeMb = Math.Round(sizeMb, 1);
                app.IsScanned = true;
            });
        }

        public async Task<(List<string> folders, List<string> registryKeys, double sizeMb)> FindResidualsDetailedAsync(InstalledAppItem app)
        {
            await ScanResidualClutterAsync(app);
            return (app.FoundFolders.ToList(), app.FoundRegistryKeys.ToList(), app.ResidualSizeMb);
        }

        public async Task<(int deletedDirs, int deletedRegs)> CleanSpecificResidualsAsync(InstalledAppItem app, IEnumerable<string> foldersToClean, IEnumerable<string> keysToClean)
        {
            return await Task.Run(() =>
            {
                int dDirs = 0;
                foreach (var dir in foldersToClean)
                {
                    try
                    {
                        if (Directory.Exists(dir))
                        {
                            ForceDeleteDirectory(dir);
                            dDirs++;
                        }
                    }
                    catch { }
                }

                int dRegs = 0;
                foreach (var reg in keysToClean)
                {
                    try
                    {
                        DeleteRegistryKey(reg);
                        dRegs++;
                    }
                    catch { }
                }

                CleanShortcuts(app.DisplayName);

                app.FoundFolders.RemoveAll(f => foldersToClean.Contains(f, StringComparer.OrdinalIgnoreCase));
                app.FoundRegistryKeys.RemoveAll(k => keysToClean.Contains(k, StringComparer.OrdinalIgnoreCase));
                app.ResidualFilesCount = app.FoundFolders.Count;
                app.ResidualRegistryCount = app.FoundRegistryKeys.Count;
                if (app.ResidualFilesCount == 0 && app.ResidualRegistryCount == 0)
                {
                    app.ResidualSizeMb = 0;
                }

                InvalidateCache();
                return (dDirs, dRegs);
            });
        }

        private List<string> GetAppSearchAliases(string displayName, string publisher)
        {
            var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(displayName)) return aliases.ToList();

            string raw = displayName.Trim();
            aliases.Add(raw);

            // Strip (x86), (64-bit), etc.
            string cleaned = System.Text.RegularExpressions.Regex.Replace(raw, @"\s*\((?:x86|x64|32-bit|64-bit|arm64)\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            aliases.Add(cleaned);

            // Strip common installer keywords
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+(?:Setup|Installer|Portable|Edition|Community|Release|Preview|Beta)$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            aliases.Add(cleaned);

            // Strip trailing versions: e.g. "LM Studio 0.4.23+1" -> "LM Studio", "Python 3.12" -> "Python"
            string withoutVer = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+(?:v|ver\.?|version)?\s*\d+(\.\d+)*(?:[-+._a-zA-Z0-9]+)?$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            if (!string.IsNullOrWhiteSpace(withoutVer) && withoutVer.Length >= 2)
            {
                aliases.Add(withoutVer);
                aliases.Add(withoutVer.Replace(" ", ""));
                aliases.Add(withoutVer.Replace(" ", "-"));
                aliases.Add(withoutVer.Replace(" ", "_"));
            }

            // Remove generic system words
            aliases.RemoveWhere(a => string.IsNullOrWhiteSpace(a) || a.Length < 2 ||
                a.Equals("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("Windows", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("System", StringComparison.OrdinalIgnoreCase));

            return aliases.ToList();
        }

        private bool IsMatchForLeftover(string dirOrKeyName, List<string> aliases, string safePub)
        {
            if (string.IsNullOrWhiteSpace(dirOrKeyName) || dirOrKeyName.Length < 2) return false;

            var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Microsoft", "Windows", "Common Files", "System32", "SysWOW64", "Temp", "Packages",
                "assembly", "WinSxS", "Desktop", "Downloads", "AppData", "Roaming", "Local",
                "LocalLow", "Default", "Public", "All Users", "Application Data", "Programs"
            };
            if (ignored.Contains(dirOrKeyName)) return false;

            foreach (var alias in aliases)
            {
                if (dirOrKeyName.Equals(alias, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (dirOrKeyName.Contains(alias, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (dirOrKeyName.Length >= 4 && alias.Contains(dirOrKeyName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            if (!string.IsNullOrEmpty(safePub) && safePub.Length > 3 &&
                !safePub.Equals("Microsoft Corporation", StringComparison.OrdinalIgnoreCase) &&
                dirOrKeyName.Contains(safePub, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private void ScanRegistryForLeftovers(RegistryKey root, string path, List<string> aliases, string safePub, List<string> found)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                if (key == null) return;
                foreach (var sub in key.GetSubKeyNames())
                {
                    if (IsMatchForLeftover(sub, aliases, safePub))
                    {
                        string fullPath = $@"{root.Name}\{path}\{sub}";
                        if (!found.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
                            found.Add(fullPath);
                    }
                }
            }
            catch { }
        }

        private void ScanUninstallHivesForLeftovers(List<string> aliases, List<string> found)
        {
            var paths = new[]
            {
                (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"),
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
                (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall")
            };

            foreach (var (root, subPath) in paths)
            {
                try
                {
                    using var key = root.OpenSubKey(subPath);
                    if (key == null) continue;
                    foreach (var appSub in key.GetSubKeyNames())
                    {
                        try
                        {
                            using var appKey = key.OpenSubKey(appSub);
                            string dn = appKey?.GetValue("DisplayName")?.ToString() ?? "";
                            if (aliases.Any(a => dn.Contains(a, StringComparison.OrdinalIgnoreCase) || appSub.Contains(a, StringComparison.OrdinalIgnoreCase)))
                            {
                                string full = $@"{root.Name}\{subPath}\{appSub}";
                                if (!found.Contains(full, StringComparer.OrdinalIgnoreCase))
                                    found.Add(full);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        private string CleanForSearch(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(input, @"\s*\((?:x86|x64|32-bit|64-bit|arm64)\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        }

        public async Task<(bool success, string message)> CleanResidualsAsync(InstalledAppItem app)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (!app.IsScanned)
                    {
                        await ScanResidualClutterAsync(app);
                    }

                    int deletedDirs = 0;
                    foreach (var dir in app.FoundFolders.ToList())
                    {
                        try
                        {
                            if (Directory.Exists(dir))
                            {
                                ForceDeleteDirectory(dir);
                                deletedDirs++;
                            }
                        }
                        catch { }
                    }

                    int deletedRegs = 0;
                    foreach (var reg in app.FoundRegistryKeys.ToList())
                    {
                        try
                        {
                            DeleteRegistryKey(reg);
                            deletedRegs++;
                        }
                        catch { }
                    }

                    CleanShortcuts(app.DisplayName);

                    app.FoundFolders.Clear();
                    app.FoundRegistryKeys.Clear();
                    app.ResidualFilesCount = 0;
                    app.ResidualRegistryCount = 0;
                    app.ResidualSizeMb = 0;
                    app.IsScanned = true;

                    return (true, $"Удаление хвостов для «{app.DisplayName}» завершено! Очищено {deletedDirs} папок и {deletedRegs} ключей реестра.");
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка очистки остаточных следов: {ex.Message}");
                }
            });
        }

        public async Task<(bool success, string message)> DeepUninstallAsync(InstalledAppItem app)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // 1. Terminate running processes belonging to target app
                    KillAppProcesses(app);

                    // 2. If this is an Orphaned Residuals item, directly perform deep residual cleanup
                    if (app.AppType == "Остатки" || app.UninstallString == "STORM_RESIDUAL_CLEAN")
                    {
                        return await CleanResidualsAsync(app);
                    }

                    // 3. Run Standard Uninstaller or Appx removal
                    if (app.AppType == "Магазин Windows" || app.AppType == "Windows Store" || (app.UninstallString.Contains("ms-resource:", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!string.IsNullOrEmpty(app.ManifestFilePath))
                        {
                            try
                            {
                                var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"Remove-AppxPackage -Package '{app.ManifestFilePath}' -ErrorAction SilentlyContinue\"")
                                {
                                    CreateNoWindow = true,
                                    UseShellExecute = false
                                };
                                using var p = Process.Start(psi);
                                p?.WaitForExit(15000);
                            }
                            catch { }
                        }
                        await RemoveBloatwareAppAsync(CleanForSearch(app.DisplayName));
                    }
                    else
                    {
                        string uninstallCmd = !string.IsNullOrEmpty(app.QuietUninstallString)
                            ? app.QuietUninstallString
                            : app.UninstallString;

                        if (!string.IsNullOrEmpty(uninstallCmd))
                        {
                            if (uninstallCmd.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo { FileName = uninstallCmd, UseShellExecute = true });
                                }
                                catch { }
                            }
                            else
                            {
                                RunUninstallProcess(uninstallCmd);
                            }
                        }
                    }

                    // 3. Delete Steam Manifest if present
                    if (!string.IsNullOrEmpty(app.ManifestFilePath) && File.Exists(app.ManifestFilePath))
                    {
                        try { File.Delete(app.ManifestFilePath); } catch { }
                    }

                    // 4. Delete Specific Registry Uninstall Key
                    if (!string.IsNullOrEmpty(app.RegistryKeyPath))
                    {
                        DeleteRegistryKey(app.RegistryKeyPath);
                    }

                    // 5. Force purge all matching Uninstall registry entries across 64-bit, 32-bit & HKCU
                    RemoveUninstallRegistryEntries(app.DisplayName);

                    // 6. Force clean target directory if still exists
                    if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation))
                    {
                        try
                        {
                            ForceDeleteDirectory(app.InstallLocation);
                        }
                        catch { }
                    }

                    // 7. Scan and delete all residuals
                    await ScanResidualClutterAsync(app);

                    int deletedDirs = 0;
                    foreach (var dir in app.FoundFolders.ToList())
                    {
                        try
                        {
                            if (Directory.Exists(dir))
                            {
                                ForceDeleteDirectory(dir);
                                deletedDirs++;
                            }
                        }
                        catch { }
                    }

                    int deletedRegs = 0;
                    foreach (var reg in app.FoundRegistryKeys.ToList())
                    {
                        try
                        {
                            DeleteRegistryKey(reg);
                            deletedRegs++;
                        }
                        catch { }
                    }

                    CleanShortcuts(app.DisplayName);

                    app.FoundFolders.Clear();
                    app.FoundRegistryKeys.Clear();
                    app.ResidualFilesCount = 0;
                    app.ResidualRegistryCount = 0;
                    app.ResidualSizeMb = 0;
                    app.IsScanned = true;

                    return (true, $"Программа «{app.DisplayName}» успешно удалена! Очищено {deletedDirs} папок и {deletedRegs} ключей реестра.");
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка при удалении «{app.DisplayName}»: {ex.Message}");
                }
            });
        }

        private void KillAppProcesses(InstalledAppItem app)
        {
            try
            {
                var aliases = GetAppSearchAliases(app.DisplayName, app.Publisher);
                var targetDirs = new List<string>();
                if (!string.IsNullOrEmpty(app.InstallLocation)) targetDirs.Add(app.InstallLocation);
                if (app.FoundFolders != null) targetDirs.AddRange(app.FoundFolders);

                foreach (var proc in Process.GetProcesses())
                {
                    try
                    {
                        string pName = proc.ProcessName;
                        if (pName.Equals("StormSystemOptimizer", StringComparison.OrdinalIgnoreCase) ||
                            pName.Equals("explorer", StringComparison.OrdinalIgnoreCase) ||
                            pName.Equals("devenv", StringComparison.OrdinalIgnoreCase))
                            continue;

                        bool kill = false;
                        foreach (var a in aliases)
                        {
                            if (a.Length >= 3 && pName.Contains(a, StringComparison.OrdinalIgnoreCase))
                            {
                                kill = true;
                                break;
                            }
                        }

                        if (!kill && targetDirs.Count > 0)
                        {
                            string? fn = null;
                            try { fn = proc.MainModule?.FileName; } catch { }
                            if (!string.IsNullOrEmpty(fn) && targetDirs.Any(td => fn.StartsWith(td, StringComparison.OrdinalIgnoreCase)))
                            {
                                kill = true;
                            }
                        }

                        if (kill)
                        {
                            proc.Kill(entireProcessTree: true);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void RunUninstallProcess(string command)
        {
            try
            {
                string file = command.Trim();
                string args = string.Empty;

                if (file.StartsWith("MsiExec.exe", StringComparison.OrdinalIgnoreCase) || file.StartsWith("msiexec", StringComparison.OrdinalIgnoreCase))
                {
                    file = "msiexec.exe";
                    int idx = command.IndexOf(' ');
                    args = idx > 0 ? command.Substring(idx + 1).Trim() : "/X";
                }
                else if (command.StartsWith("\""))
                {
                    int quoteEnd = command.IndexOf('\"', 1);
                    if (quoteEnd > 0)
                    {
                        file = command.Substring(1, quoteEnd - 1);
                        args = command.Substring(quoteEnd + 1).Trim();
                    }
                }
                else
                {
                    int spaceIdx = command.IndexOf(' ');
                    if (spaceIdx > 0)
                    {
                        string possibleFile = command.Substring(0, spaceIdx);
                        if (File.Exists(possibleFile) || !possibleFile.Contains("\\"))
                        {
                            file = possibleFile;
                            args = command.Substring(spaceIdx + 1).Trim();
                        }
                    }
                }

                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(60000);
            }
            catch
            {
                // Fallback to cmd execution
                try
                {
                    var psiCmd = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    using var procCmd = Process.Start(psiCmd);
                    procCmd?.WaitForExit(60000);
                }
                catch { }
            }
        }

        private void CleanShortcuts(string appName)
        {
            try
            {
                string safeName = CleanForSearch(appName);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string commonDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
                string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string commonStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);

                foreach (var loc in new[] { desktop, commonDesktop, startMenu, commonStartMenu })
                {
                    if (!Directory.Exists(loc)) continue;
                    try
                    {
                        foreach (var lnk in Directory.GetFiles(loc, "*.lnk", SearchOption.AllDirectories))
                        {
                            if (Path.GetFileNameWithoutExtension(lnk).Contains(safeName, StringComparison.OrdinalIgnoreCase))
                            {
                                try { File.Delete(lnk); } catch { }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public async Task<bool> RemoveBloatwareAppAsync(string appKeyword)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"Get-AppxPackage *{appKeyword}* | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(15000);
                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<(int success, int failed)> BulkUninstallAppsAsync(IEnumerable<InstalledAppItem> apps)
        {
            int s = 0, f = 0;
            foreach (var a in apps)
            {
                var (ok, _) = await DeepUninstallAsync(a);
                if (ok) s++;
                else f++;
            }
            return (s, f);
        }

        private void RemoveUninstallRegistryEntries(string appDisplayName)
        {
            if (string.IsNullOrWhiteSpace(appDisplayName)) return;
            var aliases = GetAppSearchAliases(appDisplayName, "");
            if (aliases.Count == 0) return;

            var targets = new (RegistryKey root, string path)[]
            {
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
                (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
                (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall")
            };

            foreach (var (root, path) in targets)
            {
                try
                {
                    using var key = root.OpenSubKey(path, writable: true);
                    if (key == null) continue;

                    foreach (var sub in key.GetSubKeyNames())
                    {
                        try
                        {
                            using var subKey = key.OpenSubKey(sub);
                            if (subKey == null) continue;

                            string name = subKey.GetValue("DisplayName")?.ToString()?.Trim() ?? string.Empty;
                            if (aliases.Any(a => (!string.IsNullOrEmpty(name) && name.Contains(a, StringComparison.OrdinalIgnoreCase)) ||
                                                 sub.Contains(a, StringComparison.OrdinalIgnoreCase)))
                            {
                                subKey.Dispose();
                                key.DeleteSubKeyTree(sub, false);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        private static void ForceDeleteDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return;

                // 1. Terminate any locking processes
                try
                {
                    FileUnlockerService.Instance.UnlockTargetAsync(path, true).GetAwaiter().GetResult();
                }
                catch { }

                // 2. Normalise attributes
                var di = new DirectoryInfo(path);
                foreach (var fi in di.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    try { fi.Attributes = FileAttributes.Normal; } catch { }
                }
                di.Attributes = FileAttributes.Normal;

                // 3. Try standard directory delete
                Directory.Delete(path, true);
            }
            catch
            {
                // 4. Fallback cmd rmdir
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c rmdir /s /q \"{path}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(3000);
                }
                catch { }

                // 5. If still exists, unlock each file and schedule MoveFileEx delay delete on reboot
                if (Directory.Exists(path))
                {
                    try
                    {
                        var di = new DirectoryInfo(path);
                        foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                        {
                            FileUnlockerService.Instance.UnlockAndDeleteAsync(fi.FullName).GetAwaiter().GetResult();
                        }
                        foreach (var subDir in di.EnumerateDirectories("*", SearchOption.AllDirectories).OrderByDescending(d => d.FullName.Length))
                        {
                            FileUnlockerService.Instance.UnlockAndDeleteAsync(subDir.FullName).GetAwaiter().GetResult();
                        }
                        FileUnlockerService.Instance.UnlockAndDeleteAsync(path).GetAwaiter().GetResult();
                    }
                    catch { }
                }
            }
        }

        private void DeleteRegistryKey(string fullPath)
        {
            try
            {
                if (fullPath.Contains(@"SYSTEM\CurrentControlSet\Services\", StringComparison.OrdinalIgnoreCase))
                {
                    string svcName = fullPath.Substring(fullPath.LastIndexOf('\\') + 1);
                    try
                    {
                        var psiSvc = new ProcessStartInfo("cmd.exe", $"/c sc.exe stop \"{svcName}\" & sc.exe delete \"{svcName}\"")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var pSvc = Process.Start(psiSvc);
                        pSvc?.WaitForExit(2000);
                    }
                    catch { }
                }

                if (fullPath.StartsWith("HKEY_CURRENT_USER\\", StringComparison.OrdinalIgnoreCase))
                {
                    string sub = fullPath.Substring("HKEY_CURRENT_USER\\".Length);
                    Registry.CurrentUser.DeleteSubKeyTree(sub, false);
                }
                else if (fullPath.StartsWith("HKEY_LOCAL_MACHINE\\", StringComparison.OrdinalIgnoreCase))
                {
                    string sub = fullPath.Substring("HKEY_LOCAL_MACHINE\\".Length);
                    Registry.LocalMachine.DeleteSubKeyTree(sub, false);
                }
            }
            catch
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = $"delete \"{fullPath}\" /f",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(2000);
                }
                catch { }
            }
        }
            public async System.Threading.Tasks.Task<bool> RemoveMicrosoftEdgeAsync()
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string setupPath = @"C:\Program Files (x86)\Microsoft\Edge\Application";
                    if (System.IO.Directory.Exists(setupPath))
                    {
                        var dirs = System.IO.Directory.GetDirectories(setupPath);
                        foreach (var dir in dirs)
                        {
                            string installer = System.IO.Path.Combine(dir, "Installer", "setup.exe");
                            if (System.IO.File.Exists(installer))
                            {
                                using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = installer,
                                    Arguments = "--uninstall --system-level --verbose-logging --force-uninstall",
                                    CreateNoWindow = true,
                                    UseShellExecute = false
                                });
                                p?.WaitForExit(15000);
                            }
                        }
                    }

                    string[] edgeServices = { "edgeupdate", "edgeupdatem", "MicrosoftEdgeElevationService" };
                    foreach (var s in edgeServices)
                    {
                        try
                        {
                            using var sc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "sc.exe",
                                Arguments = $"config \"{s}\" start= disabled",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            });
                            sc?.WaitForExit(2000);
                        }
                        catch { }
                    }
                    return true;
                }
                catch { return false; }
            });
        }

        public async System.Threading.Tasks.Task<bool> RemoveOneDriveAsync()
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    foreach (var proc in System.Diagnostics.Process.GetProcessesByName("OneDrive"))
                    {
                        try { proc.Kill(); } catch { }
                    }

                    string sysRoot = Environment.GetFolderPath(Environment.SpecialFolder.System);
                    string sys64 = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\SysWOW64";
                    string uninstaller = System.IO.Path.Combine(sys64, "OneDriveSetup.exe");
                    if (!System.IO.File.Exists(uninstaller))
                    {
                        uninstaller = System.IO.Path.Combine(sysRoot, "OneDriveSetup.exe");
                    }

                    if (System.IO.File.Exists(uninstaller))
                    {
                        using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = uninstaller,
                            Arguments = "/uninstall",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                        p?.WaitForExit(20000);
                    }

                    using var key = Registry.ClassesRoot.CreateSubKey(@"CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}");
                    key?.SetValue("System.IsPinnedToNameSpaceTree", 0, RegistryValueKind.DWord);

                    return true;
                }
                catch { return false; }
            });
        }

        public async System.Threading.Tasks.Task<bool> CleanComponentStoreAsync()
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = "/Online /Cleanup-Image /StartComponentCleanup /ResetBase",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    p?.WaitForExit(120000);
                    return p?.ExitCode == 0;
                }
                catch { return false; }
            });
        }

        private void ScanWindowsStoreApps(Dictionary<string, InstalledAppItem> apps)
        {
            try
            {
                string subKeyPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages";
                using var key = Registry.CurrentUser.OpenSubKey(subKeyPath);
                if (key == null) return;

                var seenDisplayNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var pkg in key.GetSubKeyNames())
                {
                    try
                    {
                        if (pkg.Contains(".split.", StringComparison.OrdinalIgnoreCase) ||
                            pkg.Contains("UndockedDevKit", StringComparison.OrdinalIgnoreCase) ||
                            pkg.Contains("CBSPreview", StringComparison.OrdinalIgnoreCase) ||
                            pkg.Contains("MicrosoftWindows.Client.", StringComparison.OrdinalIgnoreCase) ||
                            pkg.Contains("VCLibs", StringComparison.OrdinalIgnoreCase) ||
                            pkg.Contains("NET.Native", StringComparison.OrdinalIgnoreCase) ||
                            pkg.Contains("UI.Xaml", StringComparison.OrdinalIgnoreCase) ||
                            pkg.Contains("WinAppRuntime", StringComparison.OrdinalIgnoreCase))
                            continue;

                        using var pkgKey = key.OpenSubKey(pkg);
                        if (pkgKey == null) continue;

                        string rawDisplayName = pkgKey.GetValue("DisplayName")?.ToString()?.Trim() ?? string.Empty;
                        string pkgId = pkgKey.GetValue("PackageID")?.ToString()?.Trim() ?? pkg;
                        string rootFolder = pkgKey.GetValue("PackageRootFolder")?.ToString()?.Trim() ?? string.Empty;

                        if (rootFolder.StartsWith(@"C:\Windows\SystemApps", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string friendlyName = GetFriendlyStoreAppName(rawDisplayName, pkg);
                        if (string.IsNullOrWhiteSpace(friendlyName) || seenDisplayNames.Contains(friendlyName))
                            continue;

                        string version = "1.0.0.0";
                        var parts = pkg.Split('_');
                        if (parts.Length >= 2)
                        {
                            version = parts[1];
                        }

                        double sizeMb = 180.0;
                        if (!string.IsNullOrEmpty(rootFolder) && Directory.Exists(rootFolder))
                        {
                            try
                            {
                                var di = new DirectoryInfo(rootFolder);
                                long bytes = 0;
                                foreach (var f in di.EnumerateFiles("*", new System.IO.EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 2, IgnoreInaccessible = true }))
                                {
                                    bytes += f.Length;
                                }
                                if (bytes > 0) sizeMb = Math.Round(bytes / (1024.0 * 1024.0), 1);
                            }
                            catch { }
                        }

                        seenDisplayNames.Add(friendlyName);
                        if (!apps.ContainsKey(friendlyName))
                        {
                            apps[friendlyName] = new InstalledAppItem
                            {
                                DisplayName = friendlyName,
                                DisplayVersion = version,
                                Publisher = "Microsoft Store",
                                InstallLocation = rootFolder,
                                AppType = "Магазин Windows",
                                EstimatedSizeMb = sizeMb,
                                UninstallString = $"powershell.exe -NoProfile -ExecutionPolicy Bypass -Command \"Remove-AppxPackage -Package '{pkgId}' -ErrorAction SilentlyContinue\"",
                                ManifestFilePath = pkgId
                            };
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private string GetFriendlyStoreAppName(string rawDisplayName, string packageFullName)
        {
            if (!string.IsNullOrEmpty(rawDisplayName) && !rawDisplayName.StartsWith("@{") && !rawDisplayName.StartsWith("ms-resource:"))
            {
                return rawDisplayName;
            }

            string baseName = packageFullName.Split('_')[0];

            var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "A025C540.Yandex.Music", "Яндекс Музыка" },
                { "Microsoft.WindowsCalculator", "Калькулятор Windows" },
                { "Microsoft.Paint", "Paint" },
                { "Microsoft.WindowsNotepad", "Блокнот Windows" },
                { "Microsoft.WindowsCamera", "Камера Windows" },
                { "Microsoft.ScreenSketch", "Ножницы (Screen Sketch)" },
                { "Microsoft.WindowsSoundRecorder", "Запись голоса" },
                { "Microsoft.ZuneMusic", "Медиаплеер Windows (Zune)" },
                { "Microsoft.WindowsFeedbackHub", "Центр отзывов" },
                { "Microsoft.WindowsAlarms", "Часы и будильники Windows" },
                { "Microsoft.PowerAutomateDesktop", "Power Automate" },
                { "Microsoft.OutlookForWindows", "Outlook для Windows" },
                { "Microsoft.MicrosoftSolitaireCollection", "Коллекция пасьянсов (Solitaire)" },
                { "Microsoft.MicrosoftOfficeHub", "Microsoft 365 (Office)" },
                { "Microsoft.OneDriveSync", "Синхронизация OneDrive" },
                { "MSTeams", "Microsoft Teams" },
                { "Claude", "Claude" },
                { "Clipchamp.Clipchamp", "Clipchamp" },
                { "NVIDIACorp.NVIDIAControlPanel", "Панель управления NVIDIA" },
                { "WinRAR.ShellExtension", "WinRAR Shell Extension" }
            };

            if (known.TryGetValue(baseName, out var name)) return name;

            if (baseName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
            {
                return baseName.Substring("Microsoft.".Length);
            }

            return baseName;
        }

        public async Task<List<InstalledAppItem>> ScanOrphanedResidualsAsync(List<InstalledAppItem> installedApps)
        {
            return await Task.Run(() =>
            {
                var residuals = new List<InstalledAppItem>();
                var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var installedTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var app in installedApps)
                {
                    if (string.IsNullOrWhiteSpace(app.DisplayName)) continue;
                    installedTokens.Add(app.DisplayName.Trim());
                    foreach (var a in GetAppSearchAliases(app.DisplayName, app.Publisher))
                    {
                        if (a.Length >= 3) installedTokens.Add(a.Trim());
                    }
                    if (!string.IsNullOrEmpty(app.InstallLocation))
                    {
                        string folder = Path.GetFileName(app.InstallLocation.TrimEnd('\\'));
                        if (folder.Length >= 3) installedTokens.Add(folder);
                    }
                }

                var systemWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Microsoft", "Windows", "System", "Packages", "Temp", "DirectX", "CrashDumps",
                    "D3DSCache", "Comms", "IdentityCRL", "ConnectedDevicesPlatform", "Publishers",
                    "Classes", "Policies", "RegisteredApplications", "Clients", "OEM", "Storm",
                    "StormSystemOptimizer", "STORM SOFT", "Google", "NVIDIA Corporation", "NVIDIA",
                    "AMD", "Intel", "Realtek", "Steam", "Valve", "Epic Games", "Ubisoft", "GOG.com",
                    "Battle.net", "Origin", "Electronic Arts", "Application Data", "VirtualStore",
                    "History", "INetCache", "INetCookies", "NetHood", "PrintHood", "Recent", "SendTo",
                    "Start Menu", "Templates", "Programs", "Default", "All Users", "dotnet", "Pip",
                    "npm", "NuGet", "PackageManagement", "Windows PowerShell", "PowerShell", "git",
                    "Gemini", "antigravity", "WindowsApps", "Windows Defender", "Windows Defender Advanced Threat Protection",
                    "Windows Mail", "Windows NT", "Windows Sidebar", "Windows Media Player", "WindowsPowerShell",
                    "Common Files", "desktop.ini", "crypto", "assembly", "Installer", "System32", "SysWOW64",
                    "WinSxS", "Boot", "Reference Assemblies", "Microsoft.NET", "MSBuild", "Internet Explorer",
                    "ModifiableWindowsApps", "Uninstall Information"
                };

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow");
                string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string localPrograms = Path.Combine(localAppData, "Programs");
                string userDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string savedGames = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games");
                string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                var candidateLocations = new List<string>
                {
                    appData, localAppData, localLow, progData, localPrograms, userDocs, savedGames, progFiles, progFilesX86
                };

                try
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady && drive.DriveType == DriveType.Fixed && !drive.RootDirectory.FullName.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
                        {
                            string dRoot = drive.RootDirectory.FullName;
                            string dProg = Path.Combine(dRoot, "Program Files");
                            string dProgX86 = Path.Combine(dRoot, "Program Files (x86)");
                            string dGames = Path.Combine(dRoot, "Games");
                            if (Directory.Exists(dProg)) candidateLocations.Add(dProg);
                            if (Directory.Exists(dProgX86)) candidateLocations.Add(dProgX86);
                            if (Directory.Exists(dGames)) candidateLocations.Add(dGames);
                        }
                    }
                }
                catch { }

                var candidateFolders = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (var baseDir in candidateLocations)
                {
                    if (!Directory.Exists(baseDir)) continue;
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(baseDir))
                        {
                            string dirName = Path.GetFileName(dir);
                            if (string.IsNullOrWhiteSpace(dirName) || systemWhitelist.Contains(dirName))
                                continue;

                            bool isInstalled = installedTokens.Any(token =>
                                dirName.Equals(token, StringComparison.OrdinalIgnoreCase) ||
                                (token.Length >= 4 && dirName.Contains(token, StringComparison.OrdinalIgnoreCase)) ||
                                (dirName.Length >= 4 && token.Contains(dirName, StringComparison.OrdinalIgnoreCase)));

                            if (!isInstalled)
                            {
                                if (!candidateFolders.ContainsKey(dirName))
                                {
                                    candidateFolders[dirName] = new List<string>();
                                }
                                if (!candidateFolders[dirName].Contains(dir, StringComparer.OrdinalIgnoreCase))
                                {
                                    candidateFolders[dirName].Add(dir);
                                }
                            }
                        }
                    }
                    catch { }
                }

                foreach (var (appName, folders) in candidateFolders)
                {
                    if (seenNames.Contains(appName)) continue;

                    long totalBytes = 0;
                    int totalFiles = 0;

                    foreach (var folder in folders)
                    {
                        try
                        {
                            var di = new DirectoryInfo(folder);
                            foreach (var f in di.EnumerateFiles("*", new System.IO.EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 3, IgnoreInaccessible = true }))
                            {
                                totalFiles++;
                                totalBytes += f.Length;
                                if (totalFiles > 1000) break;
                            }
                        }
                        catch { }
                    }

                    if (totalFiles == 0 && folders.Count == 0) continue;

                    var foundRegs = new List<string>();
                    var aliases = GetAppSearchAliases(appName, "");
                    ScanRegistryForLeftovers(Registry.CurrentUser, @"Software", aliases, appName, foundRegs);
                    ScanRegistryForLeftovers(Registry.LocalMachine, @"SOFTWARE", aliases, appName, foundRegs);
                    ScanRegistryForLeftovers(Registry.LocalMachine, @"SOFTWARE\WOW6432Node", aliases, appName, foundRegs);

                    double sizeMb = Math.Round(totalBytes / (1024.0 * 1024.0), 1);
                    if (sizeMb == 0 && totalFiles > 0) sizeMb = 0.5;

                    seenNames.Add(appName);
                    residuals.Add(new InstalledAppItem
                    {
                        DisplayName = appName,
                        DisplayVersion = "Остаточные файлы и реестр",
                        Publisher = "Ранее удаленная программа",
                        InstallLocation = folders.FirstOrDefault() ?? string.Empty,
                        AppType = "Остатки",
                        EstimatedSizeMb = sizeMb,
                        FoundFolders = folders,
                        FoundRegistryKeys = foundRegs,
                        ResidualFilesCount = totalFiles,
                        ResidualRegistryCount = foundRegs.Count,
                        ResidualSizeMb = sizeMb,
                        IsScanned = true,
                        UninstallString = "STORM_RESIDUAL_CLEAN"
                    });
                }

                return residuals.OrderByDescending(r => r.EstimatedSizeMb).ToList();
            });
        }
    }
}
