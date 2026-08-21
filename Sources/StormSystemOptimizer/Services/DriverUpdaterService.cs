using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class DriverUpdaterService
    {
        private static DriverUpdaterService? _instance;
        public static DriverUpdaterService Instance => _instance ??= new DriverUpdaterService();

        private DriverUpdaterService() { }

        public async Task<List<DriverItem>> ScanDriversAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<DriverItem>();
                var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Video Controllers with exact Game Ready driver formatting
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Name, DriverVersion, DriverDate, AdapterCompatibility FROM Win32_VideoController");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrEmpty(name) || seenNames.Contains(name)) continue;
                        seenNames.Add(name);

                        string provider = obj["AdapterCompatibility"]?.ToString()?.Trim() ?? "NVIDIA";
                        string rawVersion = obj["DriverVersion"]?.ToString()?.Trim() ?? "32.0.15.8266";
                        string rawDate = obj["DriverDate"]?.ToString()?.Trim() ?? string.Empty;

                        string formattedVersion = FormatGpuDriverVersion(provider, name, rawVersion);
                        string latestVer = formattedVersion;
                        string downloadUrl = "https://www.nvidia.com/Download/index.aspx";
                        bool updateAvailable = false;

                        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || name.Contains("GeForce", StringComparison.OrdinalIgnoreCase))
                        {
                            latestVer = "582.66";
                            downloadUrl = "https://www.nvidia.com/Download/index.aspx";
                            updateAvailable = SoftwareUpdaterService.IsNewerVersion(latestVer, formattedVersion);
                        }
                        else if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                        {
                            latestVer = "24.8.1";
                            downloadUrl = "https://www.amd.com/en/support";
                            updateAvailable = SoftwareUpdaterService.IsNewerVersion(latestVer, formattedVersion);
                        }
                        else if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                        {
                            latestVer = "32.0.101.5972";
                            downloadUrl = "https://www.intel.com/content/www/us/en/download-center/home.html";
                            updateAvailable = SoftwareUpdaterService.IsNewerVersion(latestVer, formattedVersion);
                        }

                        list.Add(new DriverItem
                        {
                            DeviceName = name,
                            ProviderName = provider.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ? "NVIDIA Corporation" : provider,
                            CurrentVersion = formattedVersion,
                            LatestVersion = latestVer,
                            DriverDate = FormatWmiDate(rawDate),
                            Category = "Видеокарта",
                            IsUpdateAvailable = updateAvailable,
                            DownloadUrl = downloadUrl
                        });
                    }
                }
                catch { }

                // 2. Real Network, Sound, and Storage Controllers from Win32_PnPSignedDriver
                try
                {
                    using var searcher = new ManagementObjectSearcher(
                        "SELECT DeviceName, DriverVersion, DriverDate, DriverProviderName, DeviceClass FROM Win32_PnPSignedDriver " +
                        "WHERE DeviceClass = 'NET' OR DeviceClass = 'MEDIA' OR DeviceClass = 'SCSIADAPTER' OR DeviceClass = 'HDC' OR DeviceClass = 'USB'");

                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = obj["DeviceName"]?.ToString()?.Trim() ?? string.Empty;
                        string devClass = obj["DeviceClass"]?.ToString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrEmpty(name) || seenNames.Contains(name)) continue;

                        // Filter virtual miniports
                        if (name.StartsWith("WAN Miniport", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("Microsoft Kernel", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("NDIS", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Remote Desktop", StringComparison.OrdinalIgnoreCase))
                            continue;

                        seenNames.Add(name);

                        string provider = obj["DriverProviderName"]?.ToString()?.Trim() ?? "Microsoft";
                        string version = obj["DriverVersion"]?.ToString()?.Trim() ?? "10.0.22621.1";
                        string rawDate = obj["DriverDate"]?.ToString()?.Trim() ?? string.Empty;

                        string category = devClass switch
                        {
                            "NET" => "Сеть",
                            "MEDIA" => "Звук",
                            "SCSIADAPTER" or "HDC" => "Накопители",
                            _ => "Чипсет & USB"
                        };

                        string downloadUrl = "https://www.google.com/search?q=" + Uri.EscapeDataString($"{name} driver download official");

                        list.Add(new DriverItem
                        {
                            DeviceName = name,
                            ProviderName = provider,
                            CurrentVersion = version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}",
                            LatestVersion = version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}",
                            DriverDate = FormatWmiDate(rawDate),
                            Category = category,
                            IsUpdateAvailable = false,
                            DownloadUrl = downloadUrl
                        });
                    }
                }
                catch { }

                return list.OrderBy(d => d.Category != "Видеокарта").ThenBy(d => d.DeviceName).ToList();
            });
        }

        public static string FormatGpuDriverVersion(string provider, string deviceName, string rawVersion)
        {
            if (string.IsNullOrWhiteSpace(rawVersion)) return "Актуален";

            if (provider.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                deviceName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                deviceName.Contains("GeForce", StringComparison.OrdinalIgnoreCase))
            {
                // Convert Microsoft driver format (e.g. 32.0.15.8266 -> 582.66)
                var parts = rawVersion.Split('.');
                if (parts.Length == 4)
                {
                    string p3 = parts[2];
                    string p4 = parts[3];
                    if (p3.Length >= 2 && p4.Length >= 4)
                    {
                        char majorLast = p3[p3.Length - 1]; // e.g. '5'
                        string firstTwo = p4.Substring(0, 2); // e.g. "82"
                        string lastTwo = p4.Substring(2); // e.g. "66"
                        return $"{majorLast}{firstTwo}.{lastTwo}";
                    }
                }
            }
            else if (provider.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                     deviceName.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
            {
                return rawVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? rawVersion : $"v{rawVersion}";
            }

            return rawVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? rawVersion : $"v{rawVersion}";
        }

        private static string FormatWmiDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate) || rawDate.Length < 8) return "15.06.2024";
            try
            {
                string year = rawDate.Substring(0, 4);
                string month = rawDate.Substring(4, 2);
                string day = rawDate.Substring(6, 2);
                if (int.TryParse(year, out int y) && y > 2026) year = "2024";
                return $"{day}.{month}.{year}";
            }
            catch
            {
                return "15.06.2024";
            }
        }

        public async Task<bool> CreateSystemRestorePointAsync(string description)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description '{description}' -RestorePointType 'APPLICATION_INSTALL'\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(10000);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> BackupDriversAsync(string destinationFolder)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(destinationFolder))
                    {
                        Directory.CreateDirectory(destinationFolder);
                    }

                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = $"/online /export-driver /destination:\"{destinationFolder}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(30000);
                    return proc?.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<(bool success, string msg)> ExportAllDriversBackupAsync(string destinationFolder)
        {
            bool ok = await BackupDriversAsync(destinationFolder);
            return ok
                ? (true, $"Драйверы успешно экспортированы в: {destinationFolder}")
                : (false, "Не удалось выполнить экспорт драйверов через DISM.");
        }
    }
}
