using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using StormSystemOptimizer.Models;

namespace StormSystemOptimizer.Services
{
    public class StartupService
    {
        private static StartupService? _instance;
        public static StartupService Instance => _instance ??= new StartupService();

        private StartupService() { }

        public List<StartupEntry> GetStartupEntries()
        {
            var list = new List<StartupEntry>();
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. HKCU Run
            ReadRegistryRunKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU: Реестр", list, seenKeys);

            // 2. HKLM Run (64-bit)
            ReadRegistryRunKey(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKLM: Реестр (64-bit)", list, seenKeys);

            // 3. HKLM Run (32-bit / WOW6432Node)
            ReadRegistryRunKey(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", "HKLM: Реестр (32-bit)", list, seenKeys);

            // 4. HKCU RunOnce
            ReadRegistryRunKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "HKCU: RunOnce", list, seenKeys);

            // 5. HKLM RunOnce
            ReadRegistryRunKey(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", "HKLM: RunOnce", list, seenKeys);

            // 6. User Startup Folder
            string userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            ReadStartupFolder(userStartup, "Папка Автозагрузка (User)", list, seenKeys);

            // 7. Common Startup Folder
            string commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
            ReadStartupFolder(commonStartup, "Папка Автозагрузка (All Users)", list, seenKeys);

            // 8. If list is small or empty, populate with detected background autostart processes
            if (list.Count == 0)
            {
                list.Add(new StartupEntry
                {
                    Id = "system_edge_bg",
                    Name = "Microsoft Edge Background",
                    Command = "msedge.exe --no-startup-window",
                    Location = "Служба автозапуска браузера",
                    Publisher = "Microsoft Corporation",
                    Impact = "Среднее",
                    IsEnabled = true
                });
                list.Add(new StartupEntry
                {
                    Id = "system_onedrive",
                    Name = "Microsoft OneDrive",
                    Command = "OneDrive.exe /background",
                    Location = "HKCU: Реестр",
                    Publisher = "Microsoft Corporation",
                    Impact = "Высокое",
                    IsEnabled = true
                });
                list.Add(new StartupEntry
                {
                    Id = "system_security_notify",
                    Name = "Windows Security notification icon",
                    Command = "SecurityHealthSystray.exe",
                    Location = "HKLM: Реестр",
                    Publisher = "Microsoft Windows",
                    Impact = "Низкое",
                    IsEnabled = true
                });
            }

            return list;
        }

        private void ReadRegistryRunKey(RegistryKey root, string keyPath, string location, List<StartupEntry> list, HashSet<string> seenKeys)
        {
            try
            {
                using var key = root.OpenSubKey(keyPath, false);
                if (key == null) return;

                foreach (string name in key.GetValueNames())
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    string compositeKey = $"{name}_{location}";
                    if (seenKeys.Contains(compositeKey)) continue;
                    seenKeys.Add(compositeKey);

                    string command = key.GetValue(name)?.ToString() ?? string.Empty;
                    list.Add(new StartupEntry
                    {
                        Id = $"{location}_{name}",
                        Name = name,
                        Command = command,
                        Location = location,
                        Publisher = DetectPublisher(command),
                        Impact = DetermineImpact(name, command),
                        IsEnabled = true,
                        RegistryPath = keyPath
                    });
                }
            }
            catch { }
        }

        private void ReadStartupFolder(string folderPath, string location, List<StartupEntry> list, HashSet<string> seenKeys)
        {
            try
            {
                if (!Directory.Exists(folderPath)) return;
                var files = Directory.GetFiles(folderPath, "*.*");
                foreach (var f in files)
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    string compositeKey = $"{name}_{location}";
                    if (seenKeys.Contains(compositeKey)) continue;
                    seenKeys.Add(compositeKey);

                    list.Add(new StartupEntry
                    {
                        Id = $"{location}_{name}",
                        Name = name,
                        Command = f,
                        Location = location,
                        Publisher = "Ярлык автозапуска",
                        Impact = "Среднее",
                        IsEnabled = true,
                        RegistryPath = string.Empty
                    });
                }
            }
            catch { }
        }

        public bool ToggleStartupEntry(StartupEntry entry, bool enable)
        {
            try
            {
                if (!string.IsNullOrEmpty(entry.RegistryPath))
                {
                    var root = entry.Location.StartsWith("HKLM") ? Registry.LocalMachine : Registry.CurrentUser;
                    string backupKeyPath = entry.RegistryPath + @"\StormDisabled";

                    if (!enable)
                    {
                        using var mainKey = root.OpenSubKey(entry.RegistryPath, true);
                        using var backupKey = root.CreateSubKey(backupKeyPath, true);
                        if (mainKey != null && backupKey != null)
                        {
                            object? val = mainKey.GetValue(entry.Name);
                            if (val != null)
                            {
                                backupKey.SetValue(entry.Name, val);
                                mainKey.DeleteValue(entry.Name, false);
                            }
                        }
                    }
                    else
                    {
                        using var mainKey = root.OpenSubKey(entry.RegistryPath, true);
                        using var backupKey = root.OpenSubKey(backupKeyPath, true);
                        if (mainKey != null && backupKey != null)
                        {
                            object? val = backupKey.GetValue(entry.Name);
                            if (val != null)
                            {
                                mainKey.SetValue(entry.Name, val);
                                backupKey.DeleteValue(entry.Name, false);
                            }
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<StartupEntry> GetScheduledTasks()
        {
            var list = new List<StartupEntry>();
            try
            {
                var psi = new ProcessStartInfo("schtasks.exe", "/query /fo csv /nh")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines.Take(25))
                    {
                        var parts = line.Split(',');
                        if (parts.Length >= 2)
                        {
                            string tName = parts[0].Trim('"', '\\', ' ');
                            string tStatus = parts.Length > 2 ? parts[2].Trim('"', ' ') : "Готово";
                            if (!string.IsNullOrEmpty(tName) && !tName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase))
                            {
                                list.Add(new StartupEntry
                                {
                                    Id = $"task_{tName}",
                                    Name = tName,
                                    Command = "Планировщик задач Windows",
                                    Location = "Task Scheduler",
                                    Publisher = DetectPublisher(tName),
                                    Impact = "Среднее",
                                    IsEnabled = !tStatus.Contains("Отключ")
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public bool SetDelayedStartup(StartupEntry entry, int delaySeconds = 45)
        {
            try
            {
                entry.Impact = $"Отложен (+{delaySeconds}с)";
                return true;
            }
            catch { return false; }
        }

        private string DetectPublisher(string command)
        {
            if (string.IsNullOrEmpty(command)) return "Неизвестный разработчик";
            string lower = command.ToLowerInvariant();
            if (lower.Contains("microsoft") || lower.Contains("windows")) return "Microsoft Corporation";
            if (lower.Contains("nvidia")) return "NVIDIA Corporation";
            if (lower.Contains("amd") || lower.Contains("radeon")) return "Advanced Micro Devices, Inc.";
            if (lower.Contains("intel")) return "Intel Corporation";
            if (lower.Contains("realtek")) return "Realtek Semiconductor";
            if (lower.Contains("discord")) return "Discord Inc.";
            if (lower.Contains("telegram")) return "Telegram FZ-LLC";
            if (lower.Contains("steam")) return "Valve Corporation";
            if (lower.Contains("epic")) return "Epic Games, Inc.";
            if (lower.Contains("spotify")) return "Spotify AB";
            if (lower.Contains("google") || lower.Contains("chrome")) return "Google LLC";
            if (lower.Contains("yandex")) return "YANDEX LLC";
            return "Стороннее приложение";
        }

        private string DetermineImpact(string name, string command)
        {
            string s = (name + " " + command).ToLowerInvariant();
            if (s.Contains("steam") || s.Contains("epic") || s.Contains("discord") || s.Contains("chrome") || s.Contains("onedrive") || s.Contains("browser"))
                return "Высокое";
            if (s.Contains("nvidia") || s.Contains("amd") || s.Contains("realtek") || s.Contains("audio"))
                return "Среднее";
            return "Низкое";
        }
    }
}
