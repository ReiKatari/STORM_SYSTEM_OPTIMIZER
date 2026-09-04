using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
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
        private string _format = "ICO / PNG";

        [ObservableProperty]
        private string _category = "Киберпанк";

        [ObservableProperty]
        private string _previewUrl = string.Empty;

        [ObservableProperty]
        private int _iconCount = 240;

        [ObservableProperty]
        private double _rating = 4.9;

        [ObservableProperty]
        private bool _isApplied = false;
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

        public List<IconThemeItem> GetCuratedIconThemes()
        {
            return new List<IconThemeItem>
            {
                new() {
                    Title = "STORM Cyber Glow",
                    Description = "Фирменный неоновый пак STORM SOFT с неоновыми контурами и объемными 3D градиентами",
                    Author = "STORM SOFT",
                    Format = "7tsp / IconPackager",
                    Category = "STORM Dark",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 320,
                    Rating = 5.0,
                    IsApplied = true
                },
                new() {
                    Title = "Fluent Dark Minimal",
                    Description = "Современный строгий дизайн в стиле Windows 11 Fluent с матовыми темными акцентами",
                    Author = "Microsoft Fluent Team",
                    Format = "7tsp / ICO",
                    Category = "Минимализм",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 450,
                    Rating = 4.9
                },
                new() {
                    Title = "Lumicons Neomorphism 3D",
                    Description = "Объемные неоморфные значки с глубокими мягкими тенями и парящими элементами",
                    Author = "LumiStudio",
                    Format = "IconPackager (.ip)",
                    Category = "3D Объем",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 210,
                    Rating = 4.8
                },
                new() {
                    Title = "Imperial Gothic 40K",
                    Author = "TitanForge",
                    Description = "Готические золотые значки, пергаменты и аугментированные шестерни механикус",
                    Format = "iPack / 7tsp",
                    Category = "Игры и Арт",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 180,
                    Rating = 4.9
                },
                new() {
                    Title = "Retro Windows 98 Nostalgia",
                    Description = "Аутентичные пиксельные значки классической эры Windows 95 и 98 в высоком разрешении",
                    Author = "RetroForge",
                    Format = "ICO / PNG",
                    Category = "Ретро",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 160,
                    Rating = 4.7
                },
                new() {
                    Title = "MacOS Monterey Glass",
                    Description = "Стеклянные закругленные сквиркл-значки с кристальной прозрачностью",
                    Author = "Cupertino Designers",
                    Format = "IconPackager (.iconpack)",
                    Category = "Минимализм",
                    PreviewUrl = "pack://application:,,,/Assets/AppIcon.ico",
                    IconCount = 380,
                    Rating = 4.9
                }
            };
        }

        public async Task<bool> RebuildIconCacheAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 1. Kill explorer
                    foreach (var p in Process.GetProcessesByName("explorer"))
                    {
                        try { p.Kill(); p.WaitForExit(1500); } catch { }
                    }

                    // 2. Delete IconCache.db in %LOCALAPPDATA%
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string legacyCache = Path.Combine(localAppData, "IconCache.db");
                    if (File.Exists(legacyCache))
                    {
                        try { File.Delete(legacyCache); } catch { }
                    }

                    // 3. Delete modern icon and thumb caches
                    string explorerCacheDir = Path.Combine(localAppData, "Microsoft", "Windows", "Explorer");
                    if (Directory.Exists(explorerCacheDir))
                    {
                        var files = Directory.GetFiles(explorerCacheDir, "iconcache*.db")
                            .Concat(Directory.GetFiles(explorerCacheDir, "thumbcache*.db"));

                        foreach (var f in files)
                        {
                            try { File.Delete(f); } catch { }
                        }
                    }

                    // 4. Run ie4uinit.exe -show
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "ie4uinit.exe",
                            Arguments = "-show",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var proc = Process.Start(psi);
                        proc?.WaitForExit(2000);
                    }
                    catch { }

                    // 5. Restart Explorer
                    Process.Start("explorer.exe");

                    // 6. Notify Shell
                    NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);

                    return true;
                }
                catch
                {
                    // Always ensure explorer is running
                    if (Process.GetProcessesByName("explorer").Length == 0)
                    {
                        try { Process.Start("explorer.exe"); } catch { }
                    }
                    return false;
                }
            });
        }

        public bool SetSystemIcon(string target, string iconPath)
        {
            try
            {
                if (!File.Exists(iconPath)) return false;

                switch (target)
                {
                    case "ThisPC":
                        SetClsidDefaultIcon(@"{20D04FE0-3AEA-1069-A2D8-08002B30309D}", iconPath);
                        break;
                    case "RecycleBinEmpty":
                        SetClsidDefaultIconValue(@"{645FF040-5081-101B-9F08-00AA002F954E}", "empty", iconPath);
                        break;
                    case "RecycleBinFull":
                        SetClsidDefaultIconValue(@"{645FF040-5081-101B-9F08-00AA002F954E}", "full", iconPath);
                        break;
                    case "UserFolder":
                        SetClsidDefaultIcon(@"{59031a47-0728-4441-b571-3115503794b1}", iconPath);
                        break;
                    case "Network":
                        SetClsidDefaultIcon(@"{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", iconPath);
                        break;
                    case "Folders":
                        SetShellIcon("3", iconPath);
                        break;
                    case "Drives":
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
                // Delete Shell Icons subkey
                Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", false);

                // Reset CLSIDs
                ResetClsidDefaultIcon(@"{20D04FE0-3AEA-1069-A2D8-08002B30309D}");
                ResetClsidDefaultIcon(@"{645FF040-5081-101B-9F08-00AA002F954E}");
                ResetClsidDefaultIcon(@"{59031a47-0728-4441-b571-3115503794b1}");
                ResetClsidDefaultIcon(@"{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}");

                NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SetClsidDefaultIcon(string clsid, string iconPath)
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{clsid}\DefaultIcon");
            key?.SetValue("", $"{iconPath},0");
        }

        private static void SetClsidDefaultIconValue(string clsid, string valName, string iconPath)
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{clsid}\DefaultIcon");
            key?.SetValue(valName, $"{iconPath},0");
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
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons");
            key?.SetValue(index, $"{iconPath},0");
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
                            // If zip fails (e.g. 7z format), try copying raw file
                            File.Copy(packageFilePath, Path.Combine(destDir, Path.GetFileName(packageFilePath)), true);
                        }
                    }
                    else if (ext == ".ico" || ext == ".png")
                    {
                        File.Copy(packageFilePath, Path.Combine(destDir, Path.GetFileName(packageFilePath)), true);
                    }

                    // Look for icons inside extracted tree and map best matches
                    var icoFiles = Directory.GetFiles(destDir, "*.ico", SearchOption.AllDirectories);
                    if (icoFiles.Length > 0)
                    {
                        // Match folder, drive, recycle, pc
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
    }
}
