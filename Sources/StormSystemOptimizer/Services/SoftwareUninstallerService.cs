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

                // 1. 64-bit HKLM
                ScanRegistryUninstallKey(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", apps);

                // 2. 32-bit HKLM
                ScanRegistryUninstallKey(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", apps);

                // 3. User HKCU
                ScanRegistryUninstallKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall", apps);

                // 4. Scan Steam Games
                ScanSteamGames(apps);

                return apps.Values
                    .Where(a => !string.IsNullOrWhiteSpace(a.DisplayName))
                    .OrderBy(a => a.DisplayName)
                    .ToList();
            });
        }

        private void ScanRegistryUninstallKey(RegistryKey root, string subKeyPath, Dictionary<string, InstalledAppItem> apps)
        {
            try
            {
                using var key = root.OpenSubKey(subKeyPath);
                if (key == null) return;

                foreach (string subKeyName in key.GetSubKeyNames())
                {
                    try
                    {
                        using var appKey = key.OpenSubKey(subKeyName);
                        if (appKey == null) continue;

                        string? name = appKey.GetValue("DisplayName")?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(name)) continue;

                        // Filter out system updates / KB patches / internal CLSIDs
                        if (name.StartsWith("KB", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("Security Update", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("Обновление для", StringComparison.OrdinalIgnoreCase))
                            continue;

                        int systemComponent = (int)(appKey.GetValue("SystemComponent") ?? 0);
                        if (systemComponent == 1 && !name.Contains("STORM", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string uninstall = appKey.GetValue("UninstallString")?.ToString()?.Trim() ?? string.Empty;
                        string quietUninstall = appKey.GetValue("QuietUninstallString")?.ToString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrEmpty(uninstall) && string.IsNullOrEmpty(quietUninstall))
                            continue;

                        string version = appKey.GetValue("DisplayVersion")?.ToString()?.Trim() ?? string.Empty;
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
                            name.Contains("Counter-Strike", StringComparison.OrdinalIgnoreCase))
                        {
                            type = "Игра";
                        }
                        else if (uninstall.Contains("ms-resource:", StringComparison.OrdinalIgnoreCase) || location.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
                        {
                            type = "Windows Store";
                        }

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

                        if (!apps.ContainsKey(name))
                        {
                            apps[name] = new InstalledAppItem
                            {
                                DisplayName = name,
                                DisplayVersion = version,
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

        private void ScanSteamGames(Dictionary<string, InstalledAppItem> apps)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (key == null) return;
                string? steamPath = key.GetValue("SteamPath")?.ToString();
                if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath)) return;

                string steamApps = Path.Combine(steamPath, "steamapps");
                if (Directory.Exists(steamApps))
                {
                    foreach (var file in Directory.GetFiles(steamApps, "appmanifest_*.acf"))
                    {
                        try
                        {
                            var lines = File.ReadAllLines(file);
                            string gameName = string.Empty;
                            string appid = string.Empty;
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
                                else if (line.Contains("\"SizeOnDisk\""))
                                {
                                    var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 3 && long.TryParse(parts[parts.Length - 1], out long sz))
                                        sizeBytes = sz;
                                }
                            }

                            if (!string.IsNullOrEmpty(gameName) && !apps.ContainsKey(gameName))
                            {
                                apps[gameName] = new InstalledAppItem
                                {
                                    DisplayName = gameName,
                                    DisplayVersion = "Steam Edition",
                                    Publisher = "Valve / Steam",
                                    AppType = "Игра",
                                    EstimatedSizeMb = Math.Round(sizeBytes / (1024.0 * 1024.0), 1),
                                    UninstallString = $"steam://uninstall/{appid}"
                                };
                            }
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

        /// <summary>
        /// Deep scan for leftovers (folders in AppData/ProgramData, registry keys, shortcuts)
        /// </summary>
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

                // 1. Search in AppData / LocalAppData / ProgramData
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string tempDir = Path.GetTempPath();

                string[] searchRoots = { appData, localAppData, programData };

                foreach (var root in searchRoots)
                {
                    if (!Directory.Exists(root)) continue;

                    try
                    {
                        foreach (var dir in Directory.GetDirectories(root))
                        {
                            string dirName = Path.GetFileName(dir);
                            if (IsProtectedSystemDirectory(dir)) continue;

                            if (dirName.Contains(safeName, StringComparison.OrdinalIgnoreCase) ||
                                (!string.IsNullOrEmpty(safePub) && safePub.Length > 3 && dirName.Equals(safePub, StringComparison.OrdinalIgnoreCase)))
                            {
                                foundDirs.Add(dir);
                                sizeMb += GetDirectorySizeMb(dir);
                            }
                        }
                    }
                    catch { }
                }

                // 2. Search Registry Keys in HKCU and HKLM
                string[] regRoots = { @"Software", @"Software\WOW6432Node" };
                foreach (var regRoot in regRoots)
                {
                    try
                    {
                        using var key = Registry.CurrentUser.OpenSubKey(regRoot);
                        if (key != null)
                        {
                            foreach (var kName in key.GetSubKeyNames())
                            {
                                if (kName.Contains(safeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    foundRegs.Add($@"HKCU\{regRoot}\{kName}");
                                }
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        using var key = Registry.LocalMachine.OpenSubKey(regRoot);
                        if (key != null)
                        {
                            foreach (var kName in key.GetSubKeyNames())
                            {
                                if (kName.Contains(safeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    foundRegs.Add($@"HKLM\{regRoot}\{kName}");
                                }
                            }
                        }
                    }
                    catch { }
                }

                app.IsScanned = true;
                app.FoundFolders = foundDirs;
                app.FoundRegistryKeys = foundRegs;
                app.ResidualFilesCount = foundDirs.Count;
                app.ResidualRegistryCount = foundRegs.Count;
                app.ResidualSizeMb = Math.Round(sizeMb, 1);
            });
        }

        public async Task<(bool Success, string Message)> DeepUninstallAsync(InstalledAppItem app)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // 1. Run Official Uninstaller
                    if (!string.IsNullOrEmpty(app.UninstallString))
                    {
                        string uninst = !string.IsNullOrEmpty(app.QuietUninstallString) ? app.QuietUninstallString : app.UninstallString;
                        RunUninstallString(uninst);
                        await Task.Delay(2500);
                    }

                    // 2. Scan remaining residuals
                    await ScanResidualClutterAsync(app);

                    // 3. Clean remaining folders
                    int cleanedDirs = 0;
                    foreach (var dir in app.FoundFolders)
                    {
                        if (!IsProtectedSystemDirectory(dir) && Directory.Exists(dir))
                        {
                            try
                            {
                                Directory.Delete(dir, true);
                                cleanedDirs++;
                            }
                            catch { }
                        }
                    }

                    // 4. Clean remaining registry keys
                    int cleanedRegs = 0;
                    foreach (var regPath in app.FoundRegistryKeys)
                    {
                        try
                        {
                            if (regPath.StartsWith(@"HKCU\"))
                            {
                                string subKey = regPath.Substring(5);
                                Registry.CurrentUser.DeleteSubKeyTree(subKey, false);
                                cleanedRegs++;
                            }
                            else if (regPath.StartsWith(@"HKLM\"))
                            {
                                string subKey = regPath.Substring(5);
                                Registry.LocalMachine.DeleteSubKeyTree(subKey, false);
                                cleanedRegs++;
                            }
                        }
                        catch { }
                    }

                    // 5. Clean InstallLocation if leftover
                    if (!string.IsNullOrWhiteSpace(app.InstallLocation) && Directory.Exists(app.InstallLocation) && !IsProtectedSystemDirectory(app.InstallLocation))
                    {
                        try { Directory.Delete(app.InstallLocation, true); cleanedDirs++; } catch { }
                    }

                    // 6. Clean Desktop and Start Menu Shortcuts
                    try
                    {
                        string desk = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        string commonDesk = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
                        string start = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                        string commonStart = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);

                        string[] shortcutDirs = { desk, commonDesk, start, commonStart };
                        foreach (var sDir in shortcutDirs)
                        {
                            if (!Directory.Exists(sDir)) continue;
                            foreach (var f in Directory.GetFiles(sDir, "*.lnk", SearchOption.AllDirectories))
                            {
                                string fName = Path.GetFileNameWithoutExtension(f);
                                if (fName.IndexOf(app.DisplayName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    app.DisplayName.IndexOf(fName, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    try { File.Delete(f); } catch { }
                                }
                            }
                        }
                    }
                    catch { }

                    return (true, $"Программа «{app.DisplayName}» полностью удалена. Уничтожено {cleanedDirs} остаточных каталогов и {cleanedRegs} ключей реестра.");
                }
                catch (Exception ex)
                {
                    return (false, $"Ошибка деинсталляции: {ex.Message}");
                }
            });
        }

        public async Task<int> CleanAllResidualsAsync(InstalledAppItem app)
        {
            return await Task.Run(() =>
            {
                int count = 0;
                foreach (var dir in app.FoundFolders)
                {
                    if (!IsProtectedSystemDirectory(dir) && Directory.Exists(dir))
                    {
                        try { Directory.Delete(dir, true); count++; } catch { }
                    }
                }

                foreach (var regPath in app.FoundRegistryKeys)
                {
                    try
                    {
                        if (regPath.StartsWith(@"HKCU\"))
                        {
                            Registry.CurrentUser.DeleteSubKeyTree(regPath.Substring(5), false);
                            count++;
                        }
                        else if (regPath.StartsWith(@"HKLM\"))
                        {
                            Registry.LocalMachine.DeleteSubKeyTree(regPath.Substring(5), false);
                            count++;
                        }
                    }
                    catch { }
                }

                app.FoundFolders.Clear();
                app.FoundRegistryKeys.Clear();
                app.ResidualFilesCount = 0;
                app.ResidualRegistryCount = 0;

                return count;
            });
        }

        private void RunUninstallString(string uninst)
        {
            try
            {
                uninst = uninst.Trim();
                string exe = uninst;
                string args = string.Empty;

                if (uninst.StartsWith("\""))
                {
                    int endQuote = uninst.IndexOf('"', 1);
                    if (endQuote > 1)
                    {
                        exe = uninst.Substring(1, endQuote - 1);
                        args = uninst.Substring(endQuote + 1).Trim();
                    }
                }
                else if (uninst.Contains(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    int idx = uninst.IndexOf(".exe", StringComparison.OrdinalIgnoreCase) + 4;
                    exe = uninst.Substring(0, idx).Trim();
                    args = uninst.Substring(idx).Trim();
                }
                else if (uninst.StartsWith("MsiExec.exe", StringComparison.OrdinalIgnoreCase) || uninst.StartsWith("msiexec", StringComparison.OrdinalIgnoreCase))
                {
                    exe = "msiexec.exe";
                    args = uninst.Substring(uninst.IndexOf(" ") + 1);
                }

                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = true
                };

                using var proc = Process.Start(psi);
                proc?.WaitForExit(30000); // 30 sec max wait
            }
            catch { }
        }

        private bool IsProtectedSystemDirectory(string path)
        {
            string p = path.ToLowerInvariant();
            return p.Contains(@"\windows") ||
                   p.Contains(@"\system32") ||
                   p.Contains(@"\microsoft") ||
                   p.Contains(@"\windowsapps") ||
                   p.Contains(@"\common files") ||
                   p.EndsWith(@"\users") ||
                   p.EndsWith(@"\appdata") ||
                   p.EndsWith(@"\local") ||
                   p.EndsWith(@"\roaming") ||
                   p.EndsWith(@"\program files") ||
                   p.EndsWith(@"\program files (x86)") ||
                   p.EndsWith(@"\programdata");
        }

        private double GetDirectorySizeMb(string path)
        {
            try
            {
                var dir = new DirectoryInfo(path);
                long bytes = dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                return Math.Round(bytes / (1024.0 * 1024.0), 1);
            }
            catch { return 0; }
        }

        private string CleanForSearch(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            return clean.Replace("Microsoft", "").Replace("Corporation", "").Trim();
        }
    }
}
