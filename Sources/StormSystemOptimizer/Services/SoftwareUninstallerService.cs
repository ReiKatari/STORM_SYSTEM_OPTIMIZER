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
                                UninstallString = $"steam://uninstall/{appid}"
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

                if (string.IsNullOrWhiteSpace(safeName) || safeName.Length < 3) return;

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string tempDir = Path.GetTempPath();

                foreach (var baseDir in new[] { appData, localAppData, programData, tempDir })
                {
                    if (!Directory.Exists(baseDir)) continue;
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(baseDir))
                        {
                            string dirName = Path.GetFileName(dir);
                            if (dirName.Contains(safeName, StringComparison.OrdinalIgnoreCase) ||
                                (!string.IsNullOrEmpty(safePub) && safePub.Length > 3 && dirName.Contains(safePub, StringComparison.OrdinalIgnoreCase)))
                            {
                                foundDirs.Add(dir);
                                try
                                {
                                    var di = new DirectoryInfo(dir);
                                    long bytes = di.EnumerateFiles("*", new System.IO.EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 2, IgnoreInaccessible = true }).Sum(f => f.Length);
                                    sizeMb += bytes / (1024.0 * 1024.0);
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }

                // Scan Registry Keys
                ScanRegistryForLeftovers(Registry.CurrentUser, @"Software", safeName, foundRegs);
                ScanRegistryForLeftovers(Registry.LocalMachine, @"SOFTWARE", safeName, foundRegs);
                ScanRegistryForLeftovers(Registry.LocalMachine, @"SOFTWARE\WOW6432Node", safeName, foundRegs);

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
                    if (sub.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        found.Add($@"{root.Name}\{path}\{sub}");
                    }
                }
            }
            catch { }
        }

        private string CleanForSearch(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return input.Replace("(x86)", "").Replace("(64-bit)", "").Replace("(32-bit)", "").Trim();
        }

        public async Task<(bool success, string message)> DeepUninstallAsync(InstalledAppItem app)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // 1. Run Standard Uninstaller
                    string uninstallCmd = !string.IsNullOrEmpty(app.QuietUninstallString)
                        ? app.QuietUninstallString
                        : app.UninstallString;

                    if (!string.IsNullOrEmpty(uninstallCmd))
                    {
                        if (uninstallCmd.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
                        {
                            Process.Start(new ProcessStartInfo { FileName = uninstallCmd, UseShellExecute = true });
                        }
                        else
                        {
                            RunUninstallProcess(uninstallCmd);
                        }
                    }

                    // 2. Scan and clean residuals automatically
                    await ScanResidualClutterAsync(app);

                    int deletedDirs = 0;
                    foreach (var dir in app.FoundFolders)
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
                    foreach (var reg in app.FoundRegistryKeys)
                    {
                        try
                        {
                            DeleteRegistryKey(reg);
                            deletedRegs++;
                        }
                        catch { }
                    }

                    return (true, $"Деинсталляция «{app.DisplayName}» завершена! Очищено {deletedDirs} папок и {deletedRegs} ключей реестра.");
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка деинсталляции: {ex.Message}");
                }
            });
        }

        private void RunUninstallProcess(string command)
        {
            try
            {
                string file = command;
                string args = string.Empty;

                if (command.StartsWith("\""))
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
                    if (spaceIdx > 0 && File.Exists(command.Substring(0, spaceIdx)))
                    {
                        file = command.Substring(0, spaceIdx);
                        args = command.Substring(spaceIdx + 1).Trim();
                    }
                }

                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    UseShellExecute = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(30000);
            }
            catch { }
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
    }
}
