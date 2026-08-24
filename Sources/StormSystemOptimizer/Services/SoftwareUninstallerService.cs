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

        private SoftwareUninstallerService() { }

        public async Task<List<InstalledAppItem>> GetInstalledAppsAsync()
        {
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

                return apps.Values.OrderBy(a => a.DisplayName).ToList();
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
                            type = "Windows Store";
                        }

                        // Calculate accurate size from install folder if estimated size is missing
                        if (sizeMb == 0 && !string.IsNullOrEmpty(location) && Directory.Exists(location))
                        {
                            try
                            {
                                long totalBytes = 0;
                                var dirInfo = new DirectoryInfo(location);
                                foreach (var file in dirInfo.EnumerateFiles("*", new System.IO.EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 2, IgnoreInaccessible = true }))
                                {
                                    totalBytes += file.Length;
                                }
                                if (totalBytes > 0)
                                {
                                    sizeMb = Math.Round(totalBytes / (1024.0 * 1024.0), 1);
                                }
                            }
                            catch { }
                        }

                        if (sizeMb == 0)
                        {
                            sizeMb = type == "Игра" ? 12400.0 : (type == "Windows Store" ? 280.0 : 150.0);
                        }

                        // Extract accurate version from main binary if DisplayVersion is missing or generic
                        string accurateVersion = ExtractAccurateVersion(location, icon, rawVersion);

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

                string safeName = CleanForSearch(app.DisplayName);
                string safePub = CleanForSearch(app.Publisher);

                if (string.IsNullOrWhiteSpace(safeName) || safeName.Length < 2) return;

                // 1. Scan filesystem folders
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string docsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string savedGames = Path.Combine(userProfile, "Saved Games");
                string tempDir = Path.GetTempPath();

                var baseDirs = new[] { appData, localAppData, programData, docsDir, savedGames, tempDir };

                foreach (var baseDir in baseDirs)
                {
                    if (!Directory.Exists(baseDir)) continue;
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(baseDir))
                        {
                            string dirName = Path.GetFileName(dir);
                            if (dirName.Contains(safeName, StringComparison.OrdinalIgnoreCase) ||
                                (!string.IsNullOrEmpty(safePub) && safePub.Length > 3 && !safePub.Equals("Microsoft Corporation", StringComparison.OrdinalIgnoreCase) && dirName.Contains(safePub, StringComparison.OrdinalIgnoreCase)))
                            {
                                if (!foundDirs.Contains(dir, StringComparer.OrdinalIgnoreCase))
                                {
                                    foundDirs.Add(dir);
                                    try
                                    {
                                        var di = new DirectoryInfo(dir);
                                        long bytes = di.EnumerateFiles("*", new System.IO.EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 3, IgnoreInaccessible = true }).Sum(f => f.Length);
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
                }

                // 2. Scan Registry Keys
                ScanRegistryForLeftovers(Registry.CurrentUser, @"Software", safeName, foundRegs);
                ScanRegistryForLeftovers(Registry.LocalMachine, @"SOFTWARE", safeName, foundRegs);
                ScanRegistryForLeftovers(Registry.LocalMachine, @"SOFTWARE\WOW6432Node", safeName, foundRegs);

                // Scan Uninstall hives for leftover uninstallation keys
                ScanUninstallHivesForLeftovers(safeName, foundRegs);

                app.FoundFolders = foundDirs;
                app.FoundRegistryKeys = foundRegs;
                app.ResidualFilesCount = foundDirs.Count;
                app.ResidualRegistryCount = foundRegs.Count;
                app.ResidualSizeMb = Math.Round(sizeMb, 1);
                app.IsScanned = true;
            });
        }

        private void ScanRegistryForLeftovers(RegistryKey root, string path, string name, List<string> found)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                if (key == null) return;
                foreach (var sub in key.GetSubKeyNames())
                {
                    if (sub.Contains(name, StringComparison.OrdinalIgnoreCase) &&
                        !sub.Equals("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                        !sub.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                    {
                        string fullPath = $@"{root.Name}\{path}\{sub}";
                        if (!found.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
                            found.Add(fullPath);
                    }
                }
            }
            catch { }
        }

        private void ScanUninstallHivesForLeftovers(string name, List<string> found)
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
                            if (dn.Contains(name, StringComparison.OrdinalIgnoreCase) || appSub.Contains(name, StringComparison.OrdinalIgnoreCase))
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
            return input.Replace("(x86)", "").Replace("(64-bit)", "").Replace("(32-bit)", "").Trim();
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
                                Directory.Delete(dir, true);
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

                    // 2. Run Standard Uninstaller or Appx removal
                    if (app.AppType == "Windows Store" || (app.UninstallString.Contains("ms-resource:", StringComparison.OrdinalIgnoreCase)))
                    {
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
                            Directory.Delete(app.InstallLocation, true);
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
                                Directory.Delete(dir, true);
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
                string search = CleanForSearch(app.DisplayName);
                foreach (var proc in Process.GetProcesses())
                {
                    try
                    {
                        if (proc.ProcessName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        {
                            proc.Kill(entireProcessTree: true);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(app.InstallLocation))
                        {
                            string? fn = proc.MainModule?.FileName;
                            if (!string.IsNullOrEmpty(fn) && fn.StartsWith(app.InstallLocation, StringComparison.OrdinalIgnoreCase))
                            {
                                proc.Kill(entireProcessTree: true);
                            }
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
            string safeName = CleanForSearch(appDisplayName);

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
                            if ((!string.IsNullOrEmpty(name) && name.Contains(safeName, StringComparison.OrdinalIgnoreCase)) ||
                                sub.Contains(safeName, StringComparison.OrdinalIgnoreCase))
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

        private void DeleteRegistryKey(string fullPath)
        {
            try
            {
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
            catch { }
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
    }
}
