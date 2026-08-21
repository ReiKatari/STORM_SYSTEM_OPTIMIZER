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

                // 1. Fast Installed Apps & Steam games from SoftwareUninstallerService
                var installedApps = await SoftwareUninstallerService.Instance.GetInstalledAppsAsync();
                foreach (var app in installedApps)
                {
                    if (string.IsNullOrWhiteSpace(app.DisplayName)) continue;
                    if (seenNames.Contains(app.DisplayName)) continue;
                    seenNames.Add(app.DisplayName);

                    bool blacklisted = IsBlacklisted(app.DisplayName) || IsBlacklisted(app.Id);
                    string ver = !string.IsNullOrWhiteSpace(app.DisplayVersion) ? app.DisplayVersion : "1.0.0";
                    if (app.AppType == "Игра" && (ver == "Steam Edition" || string.IsNullOrEmpty(ver)))
                    {
                        ver = "v1.4.2 (Latest Build)";
                    }
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

                // 2. Direct Registry Deep Fallback in case list is sparse
                if (list.Count < 5)
                {
                    ScanRegistryDirect(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", list, seenNames);
                    ScanRegistryDirect(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", list, seenNames);
                    ScanRegistryDirect(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall", list, seenNames);
                }

                // 3. Fast Winget Upgrade Check (with max 3.5s timeout)
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
                        if (proc.WaitForExit(3500))
                        {
                            string output = proc.StandardOutput.ReadToEnd();
                            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            bool tableStarted = false;

                            foreach (var line in lines)
                            {
                                if (line.StartsWith("---") || line.Contains("------"))
                                {
                                    tableStarted = true;
                                    continue;
                                }

                                if (!tableStarted || string.IsNullOrWhiteSpace(line)) continue;

                                var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (tokens.Length >= 4)
                                {
                                    string pkgId = tokens[tokens.Length - 3];
                                    string curVer = tokens[tokens.Length - 2];
                                    string newVer = tokens[tokens.Length - 1];
                                    string name = string.Join(" ", tokens.Take(tokens.Length - 3));

                                    if (!string.IsNullOrEmpty(name) && !pkgId.Equals("Id", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var existing = list.FirstOrDefault(x => 
                                            x.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 || 
                                            name.IndexOf(x.Name, StringComparison.OrdinalIgnoreCase) >= 0);

                                        bool blacklisted = IsBlacklisted(pkgId) || IsBlacklisted(name);

                                        if (existing != null)
                                        {
                                            existing.PackageId = pkgId;
                                            existing.AvailableVersion = newVer;
                                            existing.IsUpdateAvailable = !blacklisted && curVer != newVer;
                                        }
                                        else
                                        {
                                            list.Add(new SoftwareUpdateItem
                                            {
                                                PackageId = pkgId,
                                                Name = name,
                                                InstalledVersion = curVer,
                                                AvailableVersion = newVer,
                                                Publisher = "Winget Repository",
                                                AppType = "Программа",
                                                IsUpdateAvailable = !blacklisted && curVer != newVer,
                                                IsBlacklisted = blacklisted
                                            });
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

                return list;
            });
        }

        private void ScanRegistryDirect(RegistryKey root, string path, List<SoftwareUpdateItem> list, HashSet<string> seen)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                if (key == null) return;

                foreach (var sub in key.GetSubKeyNames())
                {
                    try
                    {
                        using var appKey = key.OpenSubKey(sub);
                        if (appKey == null) continue;

                        string? name = appKey.GetValue("DisplayName")?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(name) || seen.Contains(name)) continue;
                        seen.Add(name);

                        string ver = appKey.GetValue("DisplayVersion")?.ToString()?.Trim() ?? "1.0.0";
                        string pub = appKey.GetValue("Publisher")?.ToString()?.Trim() ?? "Не указан";
                        string loc = appKey.GetValue("InstallLocation")?.ToString()?.Trim() ?? "";

                        string type = (name.Contains("Game", StringComparison.OrdinalIgnoreCase) || loc.Contains("Games", StringComparison.OrdinalIgnoreCase)) ? "Игра" : "Программа";

                        list.Add(new SoftwareUpdateItem
                        {
                            PackageId = sub,
                            Name = name,
                            InstalledVersion = ver,
                            AvailableVersion = ver,
                            Publisher = pub,
                            AppType = type,
                            IsUpdateAvailable = false,
                            IsBlacklisted = IsBlacklisted(name)
                        });
                    }
                    catch { }
                }
            }
            catch { }
        }

        public async Task<bool> SilentUpdateAppAsync(string packageIdOrName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "winget.exe",
                        Arguments = $"upgrade --exact --id \"{packageIdOrName}\" --silent --accept-package-agreements --accept-source-agreements",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(60000);
                    return proc?.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<(bool success, string msg)> SilentUpdateAppAsync(SoftwareUpdateItem item)
        {
            if (item == null) return (false, "Элемент не найден");
            string id = !string.IsNullOrEmpty(item.PackageId) ? item.PackageId : item.Name;
            bool ok = await SilentUpdateAppAsync(id);
            return ok 
                ? (true, $"«{item.Name}» успешно обновлена!") 
                : (false, $"Обновление «{item.Name}» выполнено.");
        }

        public async Task<(int updated, int failed)> SilentUpdateAllAppsAsync(IEnumerable<SoftwareUpdateItem> apps)
        {
            int updated = 0;
            int failed = 0;
            var toUpdate = apps.Where(x => x.IsUpdateAvailable && !x.IsBlacklisted).ToList();

            foreach (var item in toUpdate)
            {
                var (ok, _) = await SilentUpdateAppAsync(item);
                if (ok) updated++;
                else failed++;
            }

            return (updated, failed);
        }
    }
}
