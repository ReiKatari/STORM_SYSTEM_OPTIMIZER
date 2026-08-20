using System;
using System.Collections.Generic;
using System.IO;
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

            // 1. HKCU Run
            ReadRegistryRunKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU: Реестр", list);

            // 2. HKLM Run
            ReadRegistryRunKey(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKLM: Реестр", list);

            // 3. User Startup Folder
            string userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            ReadStartupFolder(userStartup, "Папка Автозагрузка (User)", list);

            // 4. Common Startup Folder
            string commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
            ReadStartupFolder(commonStartup, "Папка Автозагрузка (All)", list);

            return list;
        }

        private void ReadRegistryRunKey(RegistryKey root, string keyPath, string location, List<StartupEntry> list)
        {
            try
            {
                using var key = root.OpenSubKey(keyPath, false);
                if (key == null) return;

                foreach (string name in key.GetValueNames())
                {
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

        private void ReadStartupFolder(string folderPath, string location, List<StartupEntry> list)
        {
            try
            {
                if (!Directory.Exists(folderPath)) return;
                var files = Directory.GetFiles(folderPath, "*.lnk");
                foreach (var f in files)
                {
                    string name = Path.GetFileNameWithoutExtension(f);
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
                                mainKey.DeleteValue(entry.Name);
                                entry.IsEnabled = false;
                                return true;
                            }
                        }
                    }
                    else
                    {
                        using var mainKey = root.CreateSubKey(entry.RegistryPath, true);
                        using var backupKey = root.OpenSubKey(backupKeyPath, true);
                        if (mainKey != null && backupKey != null)
                        {
                            object? val = backupKey.GetValue(entry.Name);
                            if (val != null)
                            {
                                mainKey.SetValue(entry.Name, val);
                                backupKey.DeleteValue(entry.Name);
                                entry.IsEnabled = true;
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private string DetectPublisher(string command)
        {
            if (string.IsNullOrEmpty(command)) return "Неизвестно";
            string lower = command.ToLowerInvariant();
            if (lower.Contains("microsoft") || lower.Contains("windows")) return "Microsoft Corporation";
            if (lower.Contains("nvidia")) return "NVIDIA Corporation";
            if (lower.Contains("amd") || lower.Contains("radeon")) return "Advanced Micro Devices";
            if (lower.Contains("intel")) return "Intel Corporation";
            if (lower.Contains("discord")) return "Discord Inc.";
            if (lower.Contains("telegram")) return "Telegram FZ-LLC";
            if (lower.Contains("spotify")) return "Spotify AB";
            if (lower.Contains("steam") || lower.Contains("valve")) return "Valve Corporation";
            if (lower.Contains("epic")) return "Epic Games";
            return "Сторонний разработчик";
        }

        private string DetermineImpact(string name, string command)
        {
            string s = (name + " " + command).ToLowerInvariant();
            if (s.Contains("discord") || s.Contains("steam") || s.Contains("epic") || s.Contains("spotify") || s.Contains("chrome") || s.Contains("onedrive"))
                return "Высокое";
            if (s.Contains("nvidia") || s.Contains("amd") || s.Contains("realtek") || s.Contains("intel") || s.Contains("telegram"))
                return "Среднее";
            return "Низкое";
        }
    }
}
