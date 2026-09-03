using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace StormSystemOptimizer.Services
{
    public enum ExtensionRiskLevel
    {
        Safe,
        Suspicious,
        Dangerous
    }

    public class BrowserExtensionItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BrowserName { get; set; } = string.Empty;
        public string BrowserEmoji { get; set; } = "🌐";
        public string ProfileName { get; set; } = "Default";
        public string ExtensionDirectory { get; set; } = string.Empty;
        public string VersionDirectory { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public ExtensionRiskLevel RiskLevel { get; set; } = ExtensionRiskLevel.Safe;
        public string RiskBadge => RiskLevel switch
        {
            ExtensionRiskLevel.Dangerous => "Опасное",
            ExtensionRiskLevel.Suspicious => "Подозрительное",
            _ => "Безопасное"
        };
        public string RiskBadgeColor => RiskLevel switch
        {
            ExtensionRiskLevel.Dangerous => "#EF4444",
            ExtensionRiskLevel.Suspicious => "#F59E0B",
            _ => "#10B981"
        };
        public string RiskBgColor => RiskLevel switch
        {
            ExtensionRiskLevel.Dangerous => "#26EF4444",
            ExtensionRiskLevel.Suspicious => "#26F59E0B",
            _ => "#1A10B981"
        };
        public string PermissionsSummary { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
    }

    public class BrowserExtensionsService
    {
        private static BrowserExtensionsService? _instance;
        public static BrowserExtensionsService Instance => _instance ??= new BrowserExtensionsService();

        private readonly string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private readonly string _appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public async Task<List<BrowserExtensionItem>> ScanAllExtensionsAsync()
        {
            return await Task.Run(() =>
            {
                var result = new List<BrowserExtensionItem>();

                // 1. Google Chrome
                ScanChromiumBrowser(result, "Google Chrome", "🌐", Path.Combine(_localAppData, @"Google\Chrome\User Data"));

                // 2. Microsoft Edge
                ScanChromiumBrowser(result, "Microsoft Edge", "🌊", Path.Combine(_localAppData, @"Microsoft\Edge\User Data"));

                // 3. Яндекс Браузер
                ScanChromiumBrowser(result, "Яндекс Браузер", "🔴", Path.Combine(_localAppData, @"Yandex\YandexBrowser\User Data"));

                // 4. Brave Browser
                ScanChromiumBrowser(result, "Brave Browser", "🦁", Path.Combine(_localAppData, @"BraveSoftware\Brave-Browser\User Data"));

                // 5. Opera & Opera GX
                ScanOperaBrowser(result, "Opera", "⭕", Path.Combine(_appData, @"Opera Software\Opera Stable\Extensions"));
                ScanOperaBrowser(result, "Opera GX", "🎮", Path.Combine(_appData, @"Opera Software\Opera GX Stable\Extensions"));

                // 6. Mozilla Firefox
                ScanFirefoxBrowser(result, "Mozilla Firefox", "🦊", Path.Combine(_appData, @"Mozilla\Firefox\Profiles"));

                return result;
            });
        }

        private void ScanChromiumBrowser(List<BrowserExtensionItem> list, string browserName, string emoji, string userDataPath)
        {
            if (!Directory.Exists(userDataPath)) return;

            try
            {
                // Profiles: "Default", "Profile 1", "Profile 2", etc.
                var profileDirs = Directory.GetDirectories(userDataPath, "*", SearchOption.TopDirectoryOnly)
                    .Where(d =>
                    {
                        string name = Path.GetFileName(d);
                        return name.Equals("Default", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase);
                    });

                foreach (var pDir in profileDirs)
                {
                    string profileName = Path.GetFileName(pDir);
                    string extDir = Path.Combine(pDir, "Extensions");
                    if (!Directory.Exists(extDir)) continue;

                    foreach (var extIdDir in Directory.GetDirectories(extDir))
                    {
                        string extId = Path.GetFileName(extIdDir);
                        if (extId.Equals("Temp", StringComparison.OrdinalIgnoreCase)) continue;

                        // Inside extIdDir, there are version directories, e.g. "1.0.0_0"
                        var verDirs = Directory.GetDirectories(extIdDir);
                        if (verDirs.Length == 0) continue;

                        string latestVerDir = verDirs.OrderByDescending(d => d).First();
                        string manifestPath = Path.Combine(latestVerDir, "manifest.json");
                        if (!File.Exists(manifestPath)) continue;

                        var item = ParseChromiumManifest(manifestPath, extId, browserName, emoji, profileName, extIdDir, latestVerDir);
                        if (item != null)
                        {
                            list.Add(item);
                        }
                    }
                }
            }
            catch { }
        }

        private void ScanOperaBrowser(List<BrowserExtensionItem> list, string browserName, string emoji, string extensionsPath)
        {
            if (!Directory.Exists(extensionsPath)) return;

            try
            {
                foreach (var extIdDir in Directory.GetDirectories(extensionsPath))
                {
                    string extId = Path.GetFileName(extIdDir);
                    var verDirs = Directory.GetDirectories(extIdDir);
                    if (verDirs.Length == 0) continue;

                    string latestVerDir = verDirs.OrderByDescending(d => d).First();
                    string manifestPath = Path.Combine(latestVerDir, "manifest.json");
                    if (!File.Exists(manifestPath)) continue;

                    var item = ParseChromiumManifest(manifestPath, extId, browserName, emoji, "Default", extIdDir, latestVerDir);
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }
            }
            catch { }
        }

        private BrowserExtensionItem? ParseChromiumManifest(string manifestPath, string id, string browserName, string emoji, string profile, string extDir, string verDir)
        {
            try
            {
                string json = File.ReadAllText(manifestPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string rawName = root.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;
                string rawDesc = root.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                string version = root.TryGetProperty("version", out var v) ? v.GetString() ?? "1.0" : "1.0";

                string name = ResolveChromiumLocaleString(rawName, verDir);
                string desc = ResolveChromiumLocaleString(rawDesc, verDir);

                // Permissions
                var permissions = new List<string>();
                if (root.TryGetProperty("permissions", out var permElem) && permElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in permElem.EnumerateArray())
                    {
                        if (p.ValueKind == JsonValueKind.String)
                        {
                            string s = p.GetString() ?? "";
                            if (!string.IsNullOrEmpty(s)) permissions.Add(s);
                        }
                    }
                }

                if (root.TryGetProperty("host_permissions", out var hostElem) && hostElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in hostElem.EnumerateArray())
                    {
                        if (p.ValueKind == JsonValueKind.String)
                        {
                            string s = p.GetString() ?? "";
                            if (!string.IsNullOrEmpty(s)) permissions.Add(s);
                        }
                    }
                }

                // Analyze risk
                var risk = ExtensionRiskLevel.Safe;
                bool hasAllUrls = permissions.Any(p => p.Contains("<all_urls>") || p.Contains("*://*/*"));
                bool hasDangerousApi = permissions.Any(p => p.Equals("webRequestBlocking", StringComparison.OrdinalIgnoreCase) ||
                                                           p.Equals("debugger", StringComparison.OrdinalIgnoreCase) ||
                                                           p.Equals("proxy", StringComparison.OrdinalIgnoreCase));
                bool hasTrackingApi = permissions.Any(p => p.Equals("cookies", StringComparison.OrdinalIgnoreCase) ||
                                                          p.Equals("webRequest", StringComparison.OrdinalIgnoreCase) ||
                                                          p.Equals("management", StringComparison.OrdinalIgnoreCase));

                if (hasDangerousApi || (hasAllUrls && hasTrackingApi))
                {
                    risk = ExtensionRiskLevel.Dangerous;
                }
                else if (hasAllUrls || hasTrackingApi)
                {
                    risk = ExtensionRiskLevel.Suspicious;
                }

                bool isEnabled = !extDir.EndsWith("_DISABLED", StringComparison.OrdinalIgnoreCase);

                return new BrowserExtensionItem
                {
                    Id = id,
                    Name = string.IsNullOrWhiteSpace(name) ? id : name,
                    Version = version,
                    Description = desc,
                    BrowserName = browserName,
                    BrowserEmoji = emoji,
                    ProfileName = profile,
                    ExtensionDirectory = extDir,
                    VersionDirectory = verDir,
                    IsEnabled = isEnabled,
                    RiskLevel = risk,
                    Permissions = permissions,
                    PermissionsSummary = permissions.Count > 0 ? string.Join(", ", permissions.Take(5)) + (permissions.Count > 5 ? $" (+{permissions.Count - 5})" : "") : "Нет специальных разрешений"
                };
            }
            catch
            {
                return null;
            }
        }

        private string ResolveChromiumLocaleString(string text, string verDir)
        {
            if (string.IsNullOrEmpty(text) || !text.StartsWith("__MSG_") || !text.EndsWith("__"))
                return text;

            string key = text.Substring(6, text.Length - 8);

            // Try ru first, then en
            string[] langOrder = { "ru", "en", "en_US", "en_GB" };
            foreach (var lang in langOrder)
            {
                string msgFile = Path.Combine(verDir, "_locales", lang, "messages.json");
                if (File.Exists(msgFile))
                {
                    try
                    {
                        string json = File.ReadAllText(msgFile);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty(key, out var keyElem))
                        {
                            if (keyElem.TryGetProperty("message", out var val))
                            {
                                return val.GetString() ?? text;
                            }
                        }
                    }
                    catch { }
                }
            }

            return key; // Fallback to key name
        }

        private void ScanFirefoxBrowser(List<BrowserExtensionItem> list, string browserName, string emoji, string profilesPath)
        {
            if (!Directory.Exists(profilesPath)) return;

            try
            {
                foreach (var pDir in Directory.GetDirectories(profilesPath))
                {
                    string profileName = Path.GetFileName(pDir);
                    string extJsonPath = Path.Combine(pDir, "extensions.json");
                    if (!File.Exists(extJsonPath)) continue;

                    string json = File.ReadAllText(extJsonPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("addons", out var addons) && addons.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var addon in addons.EnumerateArray())
                        {
                            string type = addon.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                            if (!type.Equals("extension", StringComparison.OrdinalIgnoreCase)) continue;

                            string id = addon.TryGetProperty("id", out var i) ? i.GetString() ?? "" : "";
                            string version = addon.TryGetProperty("version", out var v) ? v.GetString() ?? "1.0" : "1.0";
                            bool isEnabled = addon.TryGetProperty("active", out var a) && a.GetBoolean();

                            string name = id;
                            if (addon.TryGetProperty("defaultLocale", out var loc) && loc.TryGetProperty("name", out var n))
                            {
                                name = n.GetString() ?? id;
                            }

                            string desc = "";
                            if (addon.TryGetProperty("defaultLocale", out var loc2) && loc2.TryGetProperty("description", out var d))
                            {
                                desc = d.GetString() ?? "";
                            }

                            var item = new BrowserExtensionItem
                            {
                                Id = id,
                                Name = name,
                                Version = version,
                                Description = desc,
                                BrowserName = browserName,
                                BrowserEmoji = emoji,
                                ProfileName = profileName,
                                ExtensionDirectory = Path.Combine(pDir, "extensions"),
                                IsEnabled = isEnabled,
                                RiskLevel = ExtensionRiskLevel.Safe,
                                PermissionsSummary = "Стандартные разрешения Firefox"
                            };

                            list.Add(item);
                        }
                    }
                }
            }
            catch { }
        }

        public async Task<bool> ToggleExtensionStateAsync(BrowserExtensionItem item)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(item.ExtensionDirectory) || !Directory.Exists(item.ExtensionDirectory))
                        return false;

                    if (item.IsEnabled)
                    {
                        // Disable by appending _DISABLED
                        string disabledDir = item.ExtensionDirectory + "_DISABLED";
                        if (!Directory.Exists(disabledDir))
                        {
                            Directory.Move(item.ExtensionDirectory, disabledDir);
                            item.ExtensionDirectory = disabledDir;
                            item.IsEnabled = false;
                            return true;
                        }
                    }
                    else
                    {
                        // Enable by stripping _DISABLED
                        if (item.ExtensionDirectory.EndsWith("_DISABLED", StringComparison.OrdinalIgnoreCase))
                        {
                            string enabledDir = item.ExtensionDirectory.Substring(0, item.ExtensionDirectory.Length - 9);
                            if (!Directory.Exists(enabledDir))
                            {
                                Directory.Move(item.ExtensionDirectory, enabledDir);
                                item.ExtensionDirectory = enabledDir;
                                item.IsEnabled = true;
                                return true;
                            }
                        }
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BrowserExtensionsService] Toggle error: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> RemoveExtensionAsync(BrowserExtensionItem item)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(item.ExtensionDirectory) && Directory.Exists(item.ExtensionDirectory))
                    {
                        Directory.Delete(item.ExtensionDirectory, true);
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BrowserExtensionsService] Delete error: {ex.Message}");
                    return false;
                }
            });
        }
    }
}
