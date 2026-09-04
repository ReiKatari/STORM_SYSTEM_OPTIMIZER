using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public partial class IconThemeItem : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _author = "STORM SOFT";

        [ObservableProperty]
        private string _format = "Shell-пак (.ICO)";

        [ObservableProperty]
        private string _category = "Киберпанк";

        [ObservableProperty]
        private string _previewUrl = string.Empty;

        [ObservableProperty]
        private int _iconCount = 320;

        [ObservableProperty]
        private string _previewGeometryKey = "GeoFolder";

        [ObservableProperty]
        private string _accentColor = "#00D2FF";

        [ObservableProperty]
        private string _compatibilityBadge = "Windows 10 / 11 64-bit";

        [ObservableProperty]
        private string _statusBadge = "Официальный стиль STORM SOFT";

        [ObservableProperty]
        private bool _isApplied = false;
    }

    public partial class StormIconEntry : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _category = string.Empty;

        [ObservableProperty]
        private string _targetSystemName = string.Empty;

        [ObservableProperty]
        private string _geometryKey = "GeoFolder";

        [ObservableProperty]
        private bool _isSelected = true;
    }

    public class IconThemeService
    {
        private static IconThemeService? _instance;
        public static IconThemeService Instance => _instance ??= new IconThemeService();

        private readonly string _iconsDir;

        private IconThemeService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _iconsDir = Path.Combine(appData, "StormSystemOptimizer", "IconThemes");
            if (!Directory.Exists(_iconsDir))
            {
                Directory.CreateDirectory(_iconsDir);
            }
        }

        public string GetActiveThemeName()
        {
            try
            {
                string path = Path.Combine(_iconsDir, "active_theme.txt");
                if (File.Exists(path))
                {
                    string txt = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrEmpty(txt)) return txt;
                }
            }
            catch { }
            return IsCustomThemeApplied() ? "STORM Cyber Glow" : "Стандартные значки Windows (Default)";
        }

        public void SetActiveThemeName(string themeName)
        {
            try
            {
                string path = Path.Combine(_iconsDir, "active_theme.txt");
                File.WriteAllText(path, themeName);
            }
            catch { }
        }

        public bool IsCustomThemeApplied()
        {
            try
            {
                using var hklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons");
                if (hklmKey != null && hklmKey.ValueCount > 0) return true;

                using var hkcuKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons");
                if (hkcuKey != null && hkcuKey.ValueCount > 0) return true;

                using var clsidKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{20D04FE0-3AEA-1069-A2D8-08002B30309D}\DefaultIcon");
                if (clsidKey != null && clsidKey.GetValue("") != null) return true;
            }
            catch { }
            return false;
        }

        public List<IconThemeItem> GetCuratedIconThemes()
        {
            bool isCustomApplied = IsCustomThemeApplied();
            string activeTheme = GetActiveThemeName();

            var themes = new List<IconThemeItem>
            {
                new() {
                    Title = "STORM Cyber Glow",
                    Description = "Фирменный неоновый пак STORM SOFT для Windows 10 и 11. Включает векторные значки папок, дисков, компьютера и корзины в неоново-бирюзовой гамме.",
                    Author = "STORM SOFT",
                    Format = "Векторный Shell-пак (.ICO)",
                    Category = "STORM Dark",
                    PreviewGeometryKey = "GeoFolder",
                    AccentColor = "#00D2FF",
                    IconCount = 320,
                    CompatibilityBadge = "Windows 10 / 11 64-bit",
                    StatusBadge = "Официальный стиль STORM SOFT",
                    IsApplied = isCustomApplied && activeTheme.Contains("STORM Cyber Glow")
                },
                new() {
                    Title = "Windows 11 Fluent Dark",
                    Description = "Современный глубокий тёмный стиль Windows 11 с акриловыми синими градиентами папок, системных библиотек, накопителей и служебных утилит.",
                    Author = "Fluent Team",
                    Format = "Fluent Shell-пак (.ICO)",
                    Category = "Fluent Design",
                    PreviewGeometryKey = "GeoExplorer",
                    AccentColor = "#38BDF8",
                    IconCount = 240,
                    CompatibilityBadge = "Windows 10 / 11 64-bit",
                    StatusBadge = "Акриловый стиль Fluent",
                    IsApplied = isCustomApplied && activeTheme.Contains("Fluent")
                },
                new() {
                    Title = "Cyberpunk Neon City 2077",
                    Description = "Футуристический набор значков в стилистике Найт-Сити с яркими золотисто-жёлтыми контурами, кибер-папками и голографическими дисками.",
                    Author = "NightCity Modders",
                    Format = "Cyberpunk Shell-пак (.ICO)",
                    Category = "Киберпанк",
                    PreviewGeometryKey = "GeoGamepad",
                    AccentColor = "#FACC15",
                    IconCount = 180,
                    CompatibilityBadge = "Windows 10 / 11 64-bit",
                    StatusBadge = "Неоновый кибер-стиль",
                    IsApplied = isCustomApplied && activeTheme.Contains("Cyberpunk")
                },
                new() {
                    Title = "Minimalist Monochrome Pro",
                    Description = "Строгий ультраминималистичный набор значков в платиново-серебристых тонах. Идеален для чистых тёмных рабочих столов без отвлекающих цветов.",
                    Author = "DesignStudio Lab",
                    Format = "Monochrome Shell-пак (.ICO)",
                    Category = "Минимализм",
                    PreviewGeometryKey = "GeoComponent",
                    AccentColor = "#E2E8F0",
                    IconCount = 150,
                    CompatibilityBadge = "Windows 10 / 11 64-bit",
                    StatusBadge = "Платиновый минимализм",
                    IsApplied = isCustomApplied && activeTheme.Contains("Monochrome")
                },
                new() {
                    Title = "macOS Tahoe Dark Glass",
                    Description = "Элегантный темный стекломорфизм с аметистовыми акцентами, мягкими закруглениями и глубокими тенями в стиле современных интерфейсов Apple.",
                    Author = "Cupertino Dark Team",
                    Format = "Glass Shell-пак (.ICO)",
                    Category = "Стекломорфизм",
                    PreviewGeometryKey = "GeoVisual",
                    AccentColor = "#A855F7",
                    IconCount = 210,
                    CompatibilityBadge = "Windows 10 / 11 64-bit",
                    StatusBadge = "Аметистовое стекло",
                    IsApplied = isCustomApplied && activeTheme.Contains("macOS")
                },
                new() {
                    Title = "Стандартные значки Windows (Default)",
                    Description = "Оригинальные заводские значки проводника, дисков, папок и корзины Microsoft Windows 10/11. Полный сброс всех пользовательских модификаций реестра.",
                    Author = "Microsoft Corporation",
                    Format = "Оригинальные библиотеки Windows",
                    Category = "По умолчанию",
                    PreviewGeometryKey = "GeoDevice",
                    AccentColor = "#94A3B8",
                    IconCount = 0,
                    CompatibilityBadge = "Все версии Windows",
                    StatusBadge = "Заводской вид Windows",
                    IsApplied = !isCustomApplied || activeTheme.Contains("Default") || activeTheme.Contains("Стандартные")
                }
            };

            return themes;
        }

        public async Task<bool> ApplyCuratedThemeAsync(string themeTitle)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (themeTitle.Contains("Default") || themeTitle.Contains("Стандартные"))
                    {
                        return ResetSystemIconsToDefault();
                    }

                    string safeName = string.Join("_", themeTitle.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                    string themeDir = Path.Combine(_iconsDir, safeName);
                    if (!Directory.Exists(themeDir))
                    {
                        Directory.CreateDirectory(themeDir);
                    }

                    // Generate distinct icons for each system element
                    string folderIco = Path.Combine(themeDir, "folder.ico");
                    string folderOpenIco = Path.Combine(themeDir, "folder_open.ico");
                    string pcIco = Path.Combine(themeDir, "thispc.ico");
                    string trashEmptyIco = Path.Combine(themeDir, "trash_empty.ico");
                    string trashFullIco = Path.Combine(themeDir, "trash_full.ico");
                    string userIco = Path.Combine(themeDir, "user.ico");
                    string netIco = Path.Combine(themeDir, "network.ico");
                    string driveIco = Path.Combine(themeDir, "drive.ico");

                    IconGenerator.SaveGeometryKeyToIcoFile("GeoFolder", folderIco, themeTitle);
                    IconGenerator.SaveGeometryKeyToIcoFile("GeoFolderOpen", folderOpenIco, themeTitle);
                    IconGenerator.SaveGeometryKeyToIcoFile("GeoThisPC", pcIco, themeTitle);
                    IconGenerator.SaveGeometryKeyToIcoFile("GeoRecycleBinEmpty", trashEmptyIco, themeTitle);
                    IconGenerator.SaveGeometryKeyToIcoFile("GeoRecycleBinFull", trashFullIco, themeTitle);
                    IconGenerator.SaveGeometryKeyToIcoFile("GeoUserProfile", userIco, themeTitle);
                    IconGenerator.SaveGeometryKeyToIcoFile("GeoNetwork", netIco, themeTitle);
                    IconGenerator.SaveGeometryKeyToIcoFile("GeoLocalDrive", driveIco, themeTitle);

                    // Apply to Shell Icons (3 = folder closed, 4 = folder open, 9 = drive)
                    SetShellIcon("3", folderIco);
                    SetShellIcon("4", folderOpenIco);
                    SetShellIcon("9", driveIco);

                    // Apply to CLSID desktop icons
                    SetClsidDefaultIcon(@"{20D04FE0-3AEA-1069-A2D8-08002B30309D}", pcIco);
                    SetClsidDefaultIconValue(@"{645FF040-5081-101B-9F08-00AA002F954E}", "empty", trashEmptyIco);
                    SetClsidDefaultIconValue(@"{645FF040-5081-101B-9F08-00AA002F954E}", "full", trashFullIco);
                    SetClsidDefaultIcon(@"{59031a47-0728-4441-b571-3115503794b1}", userIco);
                    SetClsidDefaultIcon(@"{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", netIco);

                    SetActiveThemeName(themeTitle);
                    NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public bool ApplyIconToFolder(string folderPath, string geometryKey, string themeName, string? customAccentHex = null)
        {
            try
            {
                if (!Directory.Exists(folderPath)) return false;

                string icoPath = Path.Combine(folderPath, "custom_folder_icon.ico");
                if (File.Exists(icoPath))
                {
                    try { File.SetAttributes(icoPath, FileAttributes.Normal); } catch { }
                }

                IconGenerator.SaveGeometryKeyToIcoFile(geometryKey, icoPath, themeName, customAccentHex);
                try { File.SetAttributes(icoPath, FileAttributes.Hidden | FileAttributes.System); } catch { }

                string iniPath = Path.Combine(folderPath, "desktop.ini");
                if (File.Exists(iniPath))
                {
                    try { File.SetAttributes(iniPath, FileAttributes.Normal); } catch { }
                }

                string iniContent = "[.ShellClassInfo]\r\nIconResource=custom_folder_icon.ico,0\r\n[ViewState]\r\nMode=\r\nVid=\r\nFolderType=Generic\r\n";
                File.WriteAllText(iniPath, iniContent, System.Text.Encoding.Default);
                try { File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System); } catch { }

                // Windows Explorer requires ReadOnly on folder to read desktop.ini
                var folderAttr = File.GetAttributes(folderPath);
                File.SetAttributes(folderPath, folderAttr | FileAttributes.ReadOnly);

                NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ApplyIconToShortcut(string shortcutPath, string geometryKey, string themeName, string? customAccentHex = null)
        {
            try
            {
                if (!File.Exists(shortcutPath) || !shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) return false;

                string shortcutIconsDir = Path.Combine(_iconsDir, "ShortcutIcons");
                if (!Directory.Exists(shortcutIconsDir)) Directory.CreateDirectory(shortcutIconsDir);

                string icoPath = Path.Combine(shortcutIconsDir, $"sc_{Math.Abs(shortcutPath.GetHashCode())}_{geometryKey}.ico");
                IconGenerator.SaveGeometryKeyToIcoFile(geometryKey, icoPath, themeName, customAccentHex);

                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic? shell = Activator.CreateInstance(shellType);
                    if (shell != null)
                    {
                        dynamic shortcut = shell.CreateShortcut(shortcutPath);
                        shortcut.IconLocation = $"{icoPath},0";
                        shortcut.Save();
                        NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool ExportIconToFile(string outputPath, string geometryKey, string themeName, string? customAccentHex = null)
        {
            try
            {
                IconGenerator.SaveGeometryKeyToIcoFile(geometryKey, outputPath, themeName, customAccentHex);
                return File.Exists(outputPath);
            }
            catch
            {
                return false;
            }
        }

        public bool SetSystemIcon(string target, string iconPath)
        {
            try
            {
                if (!File.Exists(iconPath)) return false;

                switch (target)
                {
                    case "ThisPC":
                    case "Этот компьютер":
                        SetClsidDefaultIcon(@"{20D04FE0-3AEA-1069-A2D8-08002B30309D}", iconPath);
                        break;
                    case "RecycleBinEmpty":
                    case "Корзина (пустая)":
                        SetClsidDefaultIconValue(@"{645FF040-5081-101B-9F08-00AA002F954E}", "empty", iconPath);
                        break;
                    case "RecycleBinFull":
                    case "Корзина (полная)":
                        SetClsidDefaultIconValue(@"{645FF040-5081-101B-9F08-00AA002F954E}", "full", iconPath);
                        break;
                    case "UserFolder":
                    case "Папка пользователя":
                        SetClsidDefaultIcon(@"{59031a47-0728-4441-b571-3115503794b1}", iconPath);
                        break;
                    case "Network":
                    case "Сеть":
                        SetClsidDefaultIcon(@"{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", iconPath);
                        break;
                    case "Folders":
                    case "Папки":
                    case "Системная папка":
                        SetShellIcon("3", iconPath);
                        SetShellIcon("4", iconPath);
                        break;
                    case "Drives":
                    case "Диски":
                    case "Локальный диск":
                        SetShellIcon("9", iconPath);
                        break;
                }

                NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ResetSystemIconsToDefault()
        {
            try
            {
                try
                {
                    using var hklm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                    using var key = hklm64.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", true);
                    if (key != null)
                    {
                        foreach (var val in key.GetValueNames())
                        {
                            try { key.DeleteValue(val); } catch { }
                        }
                    }
                }
                catch { }

                try
                {
                    using var hkcu64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
                    using var key = hkcu64.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", true);
                    if (key != null)
                    {
                        foreach (var val in key.GetValueNames())
                        {
                            try { key.DeleteValue(val); } catch { }
                        }
                    }
                }
                catch { }

                ResetClsidDefaultIcon(@"{20D04FE0-3AEA-1069-A2D8-08002B30309D}");
                ResetClsidDefaultIcon(@"{645FF040-5081-101B-9F08-00AA002F954E}");
                ResetClsidDefaultIcon(@"{59031a47-0728-4441-b571-3115503794b1}");
                ResetClsidDefaultIcon(@"{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}");

                SetActiveThemeName("Стандартные значки Windows (Default)");
                NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RebuildIconCacheAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    foreach (var proc in Process.GetProcessesByName("explorer"))
                    {
                        try { proc.Kill(); proc.WaitForExit(2000); } catch { }
                    }

                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string iconCacheDb = Path.Combine(localAppData, "IconCache.db");
                    if (File.Exists(iconCacheDb))
                    {
                        try { File.Delete(iconCacheDb); } catch { }
                    }

                    string expCacheDir = Path.Combine(localAppData, "Microsoft", "Windows", "Explorer");
                    if (Directory.Exists(expCacheDir))
                    {
                        foreach (var f in Directory.GetFiles(expCacheDir, "iconcache_*.db"))
                        {
                            try { File.Delete(f); } catch { }
                        }
                        foreach (var f in Directory.GetFiles(expCacheDir, "thumbcache_*.db"))
                        {
                            try { File.Delete(f); } catch { }
                        }
                    }

                    try { Process.Start("explorer.exe"); } catch { }
                    NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                    return true;
                }
                catch
                {
                    if (Process.GetProcessesByName("explorer").Length == 0)
                    {
                        try { Process.Start("explorer.exe"); } catch { }
                    }
                    return false;
                }
            });
        }

        private static void SetClsidDefaultIcon(string clsid, string iconPath)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{clsid}\DefaultIcon");
                key?.SetValue("", $"{iconPath},0");
            }
            catch { }
        }

        private static void SetClsidDefaultIconValue(string clsid, string valueName, string iconPath)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{clsid}\DefaultIcon");
                key?.SetValue(valueName, $"{iconPath},0");
                key?.SetValue("", $"{iconPath},0");
            }
            catch { }
        }

        private static void ResetClsidDefaultIcon(string clsid)
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\CLSID\{clsid}\DefaultIcon", false);
            }
            catch { }
        }

        private static void SetShellIcon(string index, string iconPath)
        {
            try
            {
                using var hklm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var key = hklm64.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons");
                key?.SetValue(index, $"{iconPath},0");
            }
            catch { }

            try
            {
                using var hkcu64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
                using var key = hkcu64.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons");
                key?.SetValue(index, $"{iconPath},0");
            }
            catch { }
        }

        public async Task<bool> InstallIconPackageArchiveAsync(string packageFilePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(packageFilePath)) return false;

                    string ext = Path.GetExtension(packageFilePath).ToLowerInvariant();
                    string themeName = Path.GetFileNameWithoutExtension(packageFilePath);
                    string destDir = Path.Combine(_iconsDir, themeName);

                    if (Directory.Exists(destDir))
                    {
                        Directory.Delete(destDir, true);
                    }
                    Directory.CreateDirectory(destDir);

                    if (ext == ".zip" || ext == ".iconpack" || ext == ".ip" || ext == ".7tsp")
                    {
                        try
                        {
                            ZipFile.ExtractToDirectory(packageFilePath, destDir, true);
                        }
                        catch
                        {
                            File.Copy(packageFilePath, Path.Combine(destDir, Path.GetFileName(packageFilePath)), true);
                        }
                    }
                    else if (ext == ".ico" || ext == ".png")
                    {
                        File.Copy(packageFilePath, Path.Combine(destDir, Path.GetFileName(packageFilePath)), true);
                    }

                    var icoFiles = Directory.GetFiles(destDir, "*.ico", SearchOption.AllDirectories);
                    if (icoFiles.Length > 0)
                    {
                        var folderIco = icoFiles.FirstOrDefault(f => f.Contains("folder", StringComparison.OrdinalIgnoreCase)) ?? icoFiles[0];
                        var pcIco = icoFiles.FirstOrDefault(f => f.Contains("computer", StringComparison.OrdinalIgnoreCase) || f.Contains("thispc", StringComparison.OrdinalIgnoreCase)) ?? icoFiles[0];
                        var trashIco = icoFiles.FirstOrDefault(f => f.Contains("trash", StringComparison.OrdinalIgnoreCase) || f.Contains("recycle", StringComparison.OrdinalIgnoreCase)) ?? icoFiles[0];

                        SetSystemIcon("Folders", folderIco);
                        SetSystemIcon("ThisPC", pcIco);
                        SetSystemIcon("RecycleBinEmpty", trashIco);
                        SetSystemIcon("RecycleBinFull", trashIco);
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public List<StormIconEntry> GetStormCyberGlowCatalog()
        {
            var list = new List<StormIconEntry>(320);

            // 1. Система (40)
            string[] sysIcons = {
                "Этот компьютер", "Корзина (пустая)", "Корзина (полная)", "Папка пользователя", "Сеть",
                "Панель управления", "Параметры Windows", "Диспетчер задач", "Службы Windows", "Редактор реестра",
                "Командная строка", "PowerShell", "Терминал Windows", "Защитник Windows", "Центр обновления",
                "Диспетчер устройств", "Управление дисками", "Просмотр событий", "Планировщик заданий", "Монитор ресурсов",
                "Очистка диска", "Дефрагментация", "Свойства системы", "Сетевые подключения", "Брандмауэр",
                "Электропитание", "Шрифты системы", "Звуковые устройства", "Клавиатура и мышь", "Специальные возможности",
                "Архивация и восстановление", "Точка восстановления", "Конфигурация системы", "Сведения о системе", "Диагностика памяти",
                "Редактор локальной политики", "Управление печатью", "Общие папки", "Локальные пользователи", "Среда восстановления"
            };
            string[] sysGeos = {
                "GeoThisPC", "GeoRecycleBinEmpty", "GeoRecycleBinFull", "GeoUserProfile", "GeoNetwork",
                "GeoSettings", "GeoSettings", "GeoProcesses", "GeoServices", "GeoKey",
                "GeoTerminal", "GeoTerminal", "GeoTerminal", "GeoShield", "GeoUpdate",
                "GeoDevice", "GeoDisks", "GeoLog", "GeoTask", "GeoSpeedTest",
                "GeoClean", "GeoDisks", "GeoSystemInfo", "GeoNetwork", "GeoFirewall",
                "GeoPower", "GeoComponent", "GeoAudio", "GeoDevice", "GeoVisual",
                "GeoStartup", "GeoShield", "GeoSettings", "GeoSystemInfo", "GeoRam",
                "GeoKey", "GeoDevice", "GeoFolder", "GeoUserProfile", "GeoBios"
            };
            for (int i = 0; i < sysIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = sysIcons[i],
                    Category = "Система",
                    GeometryKey = sysGeos[i % sysGeos.Length],
                    TargetSystemName = $"Sys_{i+1}",
                    IsSelected = true
                });
            }

            // 2. Папки и Диски (40)
            string[] folderIcons = {
                "Системная папка", "Открытая папка", "Документы", "Загрузки", "Изображения",
                "Музыка", "Видео", "Рабочий стол", "Избранное", "Облако OneDrive",
                "Облако Яндекс Диск", "Облако Google Drive", "Локальный диск C:", "Локальный диск D:", "Локальный диск E:",
                "Локальный диск F:", "Съемный USB накопитель", "Внешний жесткий диск", "Оптический привод DVD/BD", "Сетевой накопитель NAS",
                "Папка Игры", "Папка Программы", "Папка Проекты", "Папка Архив", "Папка Временные файлы",
                "Папка Безопасность", "Папка Мультимедиа", "Папка Исходный код", "Папка Базы данных", "Папка Скрипты",
                "Папка Шрифты", "Папка Кэш", "Папка Бэкапы", "Папка Загрузки браузера", "Папка Скриншоты",
                "Папка Записи видео", "Папка Документы работы", "Папка Личное", "Папка Шаблоны", "Папка Корзина проекта"
            };
            string[] folderGeos = {
                "GeoFolder", "GeoFolderOpen", "GeoOffice", "GeoAppUpdate", "GeoBrush",
                "GeoAudio", "GeoVisual", "GeoMonitor", "GeoStar", "GeoNetwork",
                "GeoNetwork", "GeoNetwork", "GeoLocalDrive", "GeoLocalDrive", "GeoLocalDrive",
                "GeoLocalDrive", "GeoUsb", "GeoDisks", "GeoDisks", "GeoNetwork",
                "GeoGamepad", "GeoApps", "GeoTerminal", "GeoFolderLock", "GeoClean",
                "GeoShield", "GeoAudio", "GeoTerminal", "GeoDatabase", "GeoTerminal",
                "GeoComponent", "GeoClean", "GeoShield", "GeoBrowser", "GeoBrush",
                "GeoVisual", "GeoOffice", "GeoUserProfile", "GeoCopy", "GeoRecycleBinEmpty"
            };
            for (int i = 0; i < folderIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = folderIcons[i],
                    Category = "Папки и Диски",
                    GeometryKey = folderGeos[i % folderGeos.Length],
                    TargetSystemName = $"Folder_{i+1}",
                    IsSelected = true
                });
            }

            // 3. Браузеры (40)
            string[] browserIcons = {
                "Google Chrome", "Mozilla Firefox", "Microsoft Edge", "Yandex Browser", "Opera One",
                "Opera GX Gaming", "Brave Browser", "Vivaldi", "Tor Browser", "Chromium",
                "Waterfox", "LibreWolf", "Pale Moon", "Midori", "Falkon",
                "Maxthon", "Sidekick", "Arc Browser", "DuckDuckGo Browser", "Epic Privacy Browser",
                "Avast Secure Browser", "CCleaner Browser", "Sleipnir", "Ghostery Dawn", "Ungoogled Chromium",
                "Floorp", "Mullvad Browser", "Thorium", "Zen Browser", "Orion Browser",
                "SeaMonkey", "Comodo Dragon", "SRWare Iron", "Cent Browser", "Slimjet",
                "Iridium Browser", "Otter Browser", "Puffin Secure", "Bonsai Browser", "Min Minimal Browser"
            };
            string[] browserGeos = {
                "GeoBrowser", "GeoFirewall", "GeoNetwork", "GeoRocket", "GeoSpeedTest",
                "GeoGamepad", "GeoShield", "GeoVisual", "GeoKey", "GeoBrowser",
                "GeoSpeedTest", "GeoShield", "GeoStar", "GeoComponent", "GeoRadar",
                "GeoRocket", "GeoApps", "GeoVisual", "GeoShield", "GeoPrivacy",
                "GeoShield", "GeoClean", "GeoBrowser", "GeoPrivacy", "GeoBrowser",
                "GeoRocket", "GeoShield", "GeoLightning", "GeoVisual", "GeoStar",
                "GeoNetwork", "GeoShield", "GeoComponent", "GeoSpeedTest", "GeoRocket",
                "GeoPrivacy", "GeoBrowser", "GeoShield", "GeoApps", "GeoComponent"
            };
            for (int i = 0; i < browserIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = browserIcons[i],
                    Category = "Браузеры",
                    GeometryKey = browserGeos[i % browserGeos.Length],
                    TargetSystemName = $"Browser_{i+1}",
                    IsSelected = true
                });
            }

            // 4. Игры (40)
            string[] gameIcons = {
                "Steam", "Epic Games Store", "Battle.net", "GOG Galaxy", "EA App",
                "Ubisoft Connect", "Xbox App", "Discord", "RetroArch", "Citron Switch",
                "Eden Switch", "Yuzu Emulator", "Ryujinx", "MelonDS", "DeSmuME",
                "PCSX2", "RPCS3", "PPSSPP", "Dolphin Emulator", "DuckStation",
                "Cemu Wii U", "VBA-M", "MAME Arcade", "Flycast", "Xenia Xbox 360",
                "Vita3K", "shadPS4", "Minecraft", "Counter-Strike 2", "Dota 2",
                "Cyberpunk 2077", "The Witcher 3", "GTA V", "Valorant", "Genshin Impact",
                "Roblox", "Apex Legends", "Call of Duty", "Overwatch 2", "Warhammer 40K Space Marine"
            };
            string[] gameGeos = {
                "GeoGamepad", "GeoRocket", "GeoShield", "GeoStar", "GeoLightning",
                "GeoRadar", "GeoGameBoost", "GeoAudio", "GeoGame", "GeoGamepad",
                "GeoGamepad", "GeoGame", "GeoGameBoost", "GeoDevice", "GeoDevice",
                "GeoGpu", "GeoCpu", "GeoDevice", "GeoVisual", "GeoGpu",
                "GeoMonitor", "GeoGame", "GeoLaunchers", "GeoRocket", "GeoGameBoost",
                "GeoDevice", "GeoGpu", "GeoComponent", "GeoSpeedTest", "GeoShield",
                "GeoLightning", "GeoStar", "GeoFirewall", "GeoRadar", "GeoVisual",
                "GeoComponent", "GeoSpeedTest", "GeoFirewall", "GeoGameBoost", "GeoSkull"
            };
            for (int i = 0; i < gameIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = gameIcons[i],
                    Category = "Игры",
                    GeometryKey = gameGeos[i % gameGeos.Length],
                    TargetSystemName = $"Game_{i+1}",
                    IsSelected = true
                });
            }

            // 5. Разработка (40)
            string[] devIcons = {
                "Visual Studio 2022", "Visual Studio Code", "Git", "GitHub Desktop", "GitLab",
                "Docker Desktop", "JetBrains Rider", "PyCharm", "IntelliJ IDEA", "WebStorm",
                "CLion", "Android Studio", "Unity Editor", "Unreal Engine 5", "Godot Engine",
                "Node.js", "Python Runtime", ".NET SDK", "Rust Cargo", "Go Language",
                "C++ Toolchain", "Postman", "Insomnia", "DBeaver", "Navicat",
                "HeidiSQL", "SQLite Studio", "Wireshark", "Fiddler", "Sublime Text",
                "Notepad++ Dev", "Neovim", "Vim", "Emacs", "Windows Terminal Dev",
                "Kubernetes", "Redis Desktop", "RabbitMQ", "Kafka Manager", "Nginx Server"
            };
            string[] devGeos = {
                "GeoTerminal", "GeoComponent", "GeoNetwork", "GeoLaunchers", "GeoStar",
                "GeoDisks", "GeoCpu", "GeoTerminal", "GeoLightning", "GeoVisual",
                "GeoCpu", "GeoDevice", "GeoGamepad", "GeoGameBoost", "GeoRocket",
                "GeoTerminal", "GeoTerminal", "GeoComponent", "GeoSpeedTest", "GeoTerminal",
                "GeoCpu", "GeoNetwork", "GeoRocket", "GeoDatabase", "GeoDatabase",
                "GeoDatabase", "GeoDatabase", "GeoRadar", "GeoNetwork", "GeoLog",
                "GeoLog", "GeoTerminal", "GeoTerminal", "GeoTerminal", "GeoTerminal",
                "GeoDisks", "GeoRam", "GeoServices", "GeoInterrupts", "GeoSettings"
            };
            for (int i = 0; i < devIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = devIcons[i],
                    Category = "Разработка",
                    GeometryKey = devGeos[i % devGeos.Length],
                    TargetSystemName = $"Dev_{i+1}",
                    IsSelected = true
                });
            }

            // 6. Мультимедиа (40)
            string[] mediaIcons = {
                "Spotify", "Yandex Music", "VLC Media Player", "AIMP Player", "MPC-HC",
                "PotPlayer", "OBS Studio", "Streamlabs", "Audacity", "FL Studio",
                "Ableton Live", "Adobe Photoshop", "Adobe Illustrator", "Adobe Premiere Pro", "Adobe After Effects",
                "Blender 3D", "DaVinci Resolve", "Foobar2000", "Winamp Modern", "CorelDRAW",
                "Paint.NET", "GIMP", "Krita", "Inkscape", "Cinema 4D",
                "Maya", "3ds Max", "ZBrush", "Substance Painter", "Reaper DAW",
                "Cubase", "HandBrake", "Format Factory", "FFmpeg CLI", "MusicBee",
                "Lightroom", "Vegas Pro", "Camtasia", "Shotcut", "Kdenlive"
            };
            string[] mediaGeos = {
                "GeoAudio", "GeoAudio", "GeoVisual", "GeoAudio", "GeoVisual",
                "GeoVisual", "GeoMonitor", "GeoRadar", "GeoAudio", "GeoAudio",
                "GeoAudio", "GeoBrush", "GeoBrush", "GeoVisual", "GeoVisual",
                "GeoComponent", "GeoVisual", "GeoAudio", "GeoAudio", "GeoBrush",
                "GeoBrush", "GeoBrush", "GeoBrush", "GeoBrush", "GeoComponent",
                "GeoVisual", "GeoComponent", "GeoBrush", "GeoBrush", "GeoAudio",
                "GeoAudio", "GeoUpdate", "GeoUpdate", "GeoTerminal", "GeoAudio",
                "GeoVisual", "GeoVisual", "GeoMonitor", "GeoVisual", "GeoVisual"
            };
            for (int i = 0; i < mediaIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = mediaIcons[i],
                    Category = "Мультимедиа",
                    GeometryKey = mediaGeos[i % mediaGeos.Length],
                    TargetSystemName = $"Media_{i+1}",
                    IsSelected = true
                });
            }

            // 7. Утилиты (40)
            string[] utilIcons = {
                "STORM SYSTEM OPTIMIZER", "STORM GAME SYSTEM", "STORM INSTALLER", "7-Zip Archiver", "WinRAR",
                "Process Hacker", "HWMonitor", "CPU-Z", "GPU-Z", "MSI Afterburner",
                "CrystalDiskInfo", "CrystalDiskMark", "Rufus", "BleachBit", "Everything Search",
                "Notepad++", "HWiNFO64", "Autoruns Sysinternals", "Process Explorer", "TCPView",
                "TreeSize Free", "SpaceSniffer", "Revo Uninstaller", "Geek Uninstaller", "AIDA64 Extreme",
                "FurMark", "OCCT", "Prime95", "MemTest86", "Victoria HDD",
                "QuickCPU", "CapFrameX", "RTSS Rivatuner", "Bulk Rename Utility", "FastStone Capture",
                "ShareX", "KeePassXC", "Bitwarden", "AnyDesk", "TeamViewer"
            };
            string[] utilGeos = {
                "GeoDashboard", "GeoGameBoost", "GeoAppUpdate", "GeoFolderLock", "GeoLock",
                "GeoProcesses", "GeoThermometer", "GeoCpu", "GeoGpu", "GeoSpeedTest",
                "GeoDisks", "GeoSpeedTest", "GeoUsb", "GeoClean", "GeoSearch",
                "GeoLog", "GeoSystemInfo", "GeoStartup", "GeoTask", "GeoRadar",
                "GeoDisks", "GeoDisks", "GeoUninstaller", "GeoGarbage", "GeoBenchmarks",
                "GeoFirewall", "GeoCpu", "GeoBenchmarks", "GeoRam", "GeoDisks",
                "GeoCpu", "GeoSpeedTest", "GeoFan", "GeoCopy", "GeoVisual",
                "GeoVisual", "GeoKey", "GeoShield", "GeoNetwork", "GeoMonitor"
            };
            for (int i = 0; i < utilIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = utilIcons[i],
                    Category = "Утилиты",
                    GeometryKey = utilGeos[i % utilGeos.Length],
                    TargetSystemName = $"Util_{i+1}",
                    IsSelected = true
                });
            }

            // 8. Типы файлов (40)
            string[] fileTypeIcons = {
                "Исполняемый файл (.exe)", "Библиотека (.dll)", "Архив ZIP (.zip)", "Архив RAR (.rar)", "Архив 7-Zip (.7z)",
                "Образ диска (.iso)", "Документ PDF (.pdf)", "Документ Word (.docx)", "Таблица Excel (.xlsx)", "Презентация (.pptx)",
                "Текстовый файл (.txt)", "Файл JSON (.json)", "Файл XML (.xml)", "Аудио MP3 (.mp3)", "Аудио FLAC (.flac)",
                "Аудио WAV (.wav)", "Видео MP4 (.mp4)", "Видео MKV (.mkv)", "Изображение PNG (.png)", "Изображение JPG (.jpg)",
                "Иконка (.ico)", "Вектор SVG (.svg)", "Исходный код C# (.cs)", "Исходный код C++ (.cpp)", "Заголовок C++ (.h)",
                "Исходный код Python (.py)", "Скрипт JavaScript (.js)", "Скрипт TypeScript (.ts)", "Стиль CSS (.css)", "Страница HTML (.html)",
                "Разметка Markdown (.md)", "База данных SQLite (.db)", "Скрипт SQL (.sql)", "Конфигурация YAML (.yaml)", "Конфигурация TOML (.toml)",
                "Пакетный файл (.bat)", "Скрипт PowerShell (.ps1)", "Файл реестра (.reg)", "Файл шрифта (.ttf)", "Файл шрифта (.otf)"
            };
            string[] fileTypeGeos = {
                "GeoTerminal", "GeoComponent", "GeoFolderLock", "GeoFolderLock", "GeoFolderLock",
                "GeoDisks", "GeoOffice", "GeoOffice", "GeoOffice", "GeoOffice",
                "GeoLog", "GeoDatabase", "GeoLog", "GeoAudio", "GeoAudio",
                "GeoAudio", "GeoVisual", "GeoVisual", "GeoBrush", "GeoBrush",
                "GeoBrush", "GeoBrush", "GeoTerminal", "GeoTerminal", "GeoTerminal",
                "GeoTerminal", "GeoTerminal", "GeoTerminal", "GeoBrush", "GeoBrowser",
                "GeoLog", "GeoDatabase", "GeoDatabase", "GeoSettings", "GeoSettings",
                "GeoTerminal", "GeoTerminal", "GeoKey", "GeoComponent", "GeoComponent"
            };
            for (int i = 0; i < fileTypeIcons.Length; i++)
            {
                list.Add(new StormIconEntry {
                    Name = fileTypeIcons[i],
                    Category = "Типы файлов",
                    GeometryKey = fileTypeGeos[i % fileTypeGeos.Length],
                    TargetSystemName = $"File_{i+1}",
                    IsSelected = true
                });
            }

            return list;
        }
    }
}
