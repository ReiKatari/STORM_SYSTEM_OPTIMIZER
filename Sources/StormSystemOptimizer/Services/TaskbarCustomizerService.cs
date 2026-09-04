using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class TaskbarCustomizerService
    {
        private static TaskbarCustomizerService? _instance;
        public static TaskbarCustomizerService Instance => _instance ??= new TaskbarCustomizerService();

        private const string ExplorerAdvancedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        private const string ExplorerSearchKey = @"Software\Microsoft\Windows\CurrentVersion\Search";

        private TaskbarCustomizerService() { }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            UIntPtr wParam,
            string lParam,
            uint fuFlags,
            uint uTimeout,
            out UIntPtr lpdwResult);

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        public void NotifyExplorerSettingChange(string section = "TraySettings")
        {
            try
            {
                const int HWND_BROADCAST = 0xffff;
                const uint WM_SETTINGCHANGE = 0x001A;
                const uint SMTO_ABORTIFHUNG = 0x0002;
                SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero, section, SMTO_ABORTIFHUNG, 2000, out _);
                SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero, "Policy", SMTO_ABORTIFHUNG, 2000, out _);
                SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); // SHCNE_ASSOCCHANGED
            }
            catch { }
        }

        public int GetTaskbarAlignment()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ExplorerAdvancedKey);
                if (key != null)
                {
                    object? val = key.GetValue("TaskbarAl");
                    if (val is int i) return i; // 0 = Left, 1 = Center
                }
            }
            catch { }
            return 1; // Default Center in Windows 11
        }

        public bool SetTaskbarAlignment(int alignment)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(ExplorerAdvancedKey);
                key?.SetValue("TaskbarAl", alignment, RegistryValueKind.DWord);
                NotifyExplorerSettingChange();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int GetTaskbarSize()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ExplorerAdvancedKey);
                if (key != null)
                {
                    object? val = key.GetValue("TaskbarSi");
                    if (val is int i) return i; // 0 = Small, 1 = Medium, 2 = Large
                }
            }
            catch { }
            return 1;
        }

        public bool SetTaskbarSize(int size)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(ExplorerAdvancedKey);
                key?.SetValue("TaskbarSi", size, RegistryValueKind.DWord);
                NotifyExplorerSettingChange();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int GetTaskbarGrouping()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ExplorerAdvancedKey);
                if (key != null)
                {
                    object? val = key.GetValue("TaskbarGlomLevel");
                    if (val is int i) return i; // 0 = Always, 1 = When full, 2 = Never
                }
            }
            catch { }
            return 0;
        }

        public bool SetTaskbarGrouping(int glomLevel)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(ExplorerAdvancedKey);
                key?.SetValue("TaskbarGlomLevel", glomLevel, RegistryValueKind.DWord);
                NotifyExplorerSettingChange();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int GetSearchBoxMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ExplorerSearchKey);
                if (key != null)
                {
                    object? val = key.GetValue("SearchboxTaskbarMode");
                    if (val is int i) return i; // 0 = Hidden, 1 = Icon only, 2 = Box, 3 = Button
                }
            }
            catch { }
            return 1;
        }

        public bool SetSearchBoxMode(int mode)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(ExplorerSearchKey);
                key?.SetValue("SearchboxTaskbarMode", mode, RegistryValueKind.DWord);
                NotifyExplorerSettingChange();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool GetTaskViewButton()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ExplorerAdvancedKey);
                if (key != null)
                {
                    object? val = key.GetValue("ShowTaskViewButton");
                    if (val is int i) return i != 0;
                }
            }
            catch { }
            return true;
        }

        public bool SetTaskViewButton(bool show)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(ExplorerAdvancedKey);
                key?.SetValue("ShowTaskViewButton", show ? 1 : 0, RegistryValueKind.DWord);
                NotifyExplorerSettingChange();
                return true;
            }
            catch { return false; }
        }

        public bool GetWidgetsButton()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ExplorerAdvancedKey);
                if (key != null)
                {
                    object? val = key.GetValue("TaskbarDa");
                    if (val is int i) return i != 0;
                }
            }
            catch { }
            return true;
        }

        public bool SetWidgetsButton(bool show)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(ExplorerAdvancedKey);
                key?.SetValue("TaskbarDa", show ? 1 : 0, RegistryValueKind.DWord);
                NotifyExplorerSettingChange();
                return true;
            }
            catch { return false; }
        }

        public bool GetShowSecondsInClock()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ExplorerAdvancedKey);
                if (key != null)
                {
                    object? val = key.GetValue("ShowSecondsInSystemClock");
                    if (val is int i) return i == 1;
                }
            }
            catch { }
            return false;
        }

        public bool SetShowSecondsInClock(bool show)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(ExplorerAdvancedKey);
                key?.SetValue("ShowSecondsInSystemClock", show ? 1 : 0, RegistryValueKind.DWord);
                NotifyExplorerSettingChange();
                return true;
            }
            catch { return false; }
        }

        public bool GetHideRecommendedStart()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ExplorerAdvancedKey);
                if (key != null)
                {
                    object? val = key.GetValue("Start_IrisRecommendations");
                    if (val is int i) return i == 0;
                }
            }
            catch { }
            return false;
        }

        public bool SetHideRecommendedStart(bool hide)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(ExplorerAdvancedKey);
                key?.SetValue("Start_IrisRecommendations", hide ? 0 : 1, RegistryValueKind.DWord);
                NotifyExplorerSettingChange();
                return true;
            }
            catch { return false; }
        }

        public bool GetLockTaskbar()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ExplorerAdvancedKey);
                if (key != null)
                {
                    object? val = key.GetValue("TaskbarSizeMove");
                    if (val is int i) return i == 0;
                }
            }
            catch { }
            return true;
        }

        public bool SetLockTaskbar(bool locked)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(ExplorerAdvancedKey);
                key?.SetValue("TaskbarSizeMove", locked ? 0 : 1, RegistryValueKind.DWord);
                NotifyExplorerSettingChange();
                return true;
            }
            catch { return false; }
        }

        public bool GetAutoHideTaskbar()
        {
            try
            {
                var data = new NativeMethods.APPBARDATA
                {
                    cbSize = Marshal.SizeOf(typeof(NativeMethods.APPBARDATA)),
                    hWnd = NativeMethods.FindWindow("Shell_TrayWnd", null)
                };
                uint state = (uint)NativeMethods.SHAppBarMessage(NativeMethods.ABM_GETSTATE, ref data);
                if (state != 0)
                {
                    return (state & NativeMethods.ABS_AUTOHIDE) != 0;
                }

                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3");
                if (key != null && key.GetValue("Settings") is byte[] bytes && bytes.Length > 8)
                {
                    return (bytes[8] & 0x01) != 0;
                }
            }
            catch { }
            return false;
        }

        public bool SetAutoHideTaskbar(bool autoHide)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3", true);
                if (key != null && key.GetValue("Settings") is byte[] bytes && bytes.Length > 8)
                {
                    if (autoHide) bytes[8] |= 0x01;
                    else bytes[8] &= unchecked((byte)~0x01);
                    key.SetValue("Settings", bytes, RegistryValueKind.Binary);
                }

                var data = new NativeMethods.APPBARDATA
                {
                    cbSize = Marshal.SizeOf(typeof(NativeMethods.APPBARDATA)),
                    hWnd = NativeMethods.FindWindow("Shell_TrayWnd", null),
                    lParam = autoHide ? NativeMethods.ABS_AUTOHIDE : NativeMethods.ABS_ALWAYSONTOP
                };
                NativeMethods.SHAppBarMessage(NativeMethods.ABM_SETSTATE, ref data);
                NotifyExplorerSettingChange();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ApplyTaskbarAcrylicBlur(int alpha = 180, int red = 10, int green = 11, int blue = 16)
        {
            try
            {
                IntPtr taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
                if (taskbar != IntPtr.Zero)
                {
                    SetWindowBlur(taskbar, NativeMethods.AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND, alpha, red, green, blue);
                }

                // Apply to secondary monitors
                IntPtr secTaskbar = NativeMethods.FindWindow("Shell_SecondaryTrayWnd", null);
                while (secTaskbar != IntPtr.Zero)
                {
                    SetWindowBlur(secTaskbar, NativeMethods.AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND, alpha, red, green, blue);
                    secTaskbar = NativeMethods.FindWindowEx(IntPtr.Zero, secTaskbar, "Shell_SecondaryTrayWnd", null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ApplyTaskbarTransparent()
        {
            try
            {
                IntPtr taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
                if (taskbar != IntPtr.Zero)
                {
                    SetWindowBlur(taskbar, NativeMethods.AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT, 0, 0, 0, 0);
                }

                IntPtr secTaskbar = NativeMethods.FindWindow("Shell_SecondaryTrayWnd", null);
                while (secTaskbar != IntPtr.Zero)
                {
                    SetWindowBlur(secTaskbar, NativeMethods.AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT, 0, 0, 0, 0);
                    secTaskbar = NativeMethods.FindWindowEx(IntPtr.Zero, secTaskbar, "Shell_SecondaryTrayWnd", null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ResetTaskbarStyle()
        {
            try
            {
                IntPtr taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
                if (taskbar != IntPtr.Zero)
                {
                    SetWindowBlur(taskbar, NativeMethods.AccentState.ACCENT_NORMAL, 0, 0, 0, 0);
                }

                IntPtr secTaskbar = NativeMethods.FindWindow("Shell_SecondaryTrayWnd", null);
                while (secTaskbar != IntPtr.Zero)
                {
                    SetWindowBlur(secTaskbar, NativeMethods.AccentState.ACCENT_NORMAL, 0, 0, 0, 0);
                    secTaskbar = NativeMethods.FindWindowEx(IntPtr.Zero, secTaskbar, "Shell_SecondaryTrayWnd", null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SetWindowBlur(IntPtr hwnd, NativeMethods.AccentState state, int alpha, int r, int g, int b)
        {
            int gradientColor = (alpha << 24) | (b << 16) | (g << 8) | r;

            var accent = new NativeMethods.AccentPolicy
            {
                AccentState = state,
                AccentFlags = 2,
                GradientColor = gradientColor,
                AnimationId = 0
            };

            int accentStructSize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new NativeMethods.WindowCompositionAttributeData
                {
                    Attribute = NativeMethods.WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = accentPtr,
                    SizeOfData = accentStructSize
                };

                NativeMethods.SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        public bool SetCustomStartIcon(string iconPath)
        {
            try
            {
                if (!File.Exists(iconPath)) return false;

                string destDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "StormSystemOptimizer",
                    "StartIcons"
                );
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                string destPath = Path.Combine(destDir, "start_button" + Path.GetExtension(iconPath));
                File.Copy(iconPath, destPath, true);

                // Register in Explorer branding
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                key?.SetValue("CustomStartIcon", destPath);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool CreateQuickToolbarFolder(string folderName, string targetFolderPath)
        {
            try
            {
                if (!Directory.Exists(targetFolderPath)) return false;

                string quickLaunch = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Internet Explorer", "Quick Launch", "User Pinned", "TaskBar"
                );

                if (!Directory.Exists(quickLaunch))
                {
                    Directory.CreateDirectory(quickLaunch);
                }

                string shortcutTarget = Path.Combine(quickLaunch, $"{folderName}.lnk");
                CreateShortcut(targetFolderPath, shortcutTarget, folderName);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void CreateShortcut(string targetPath, string shortcutPath, string description)
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.Description = description;
            shortcut.Save();
        }

        public void RestartExplorer()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("explorer"))
                {
                    try { p.Kill(); p.WaitForExit(1500); } catch { }
                }
                Process.Start("explorer.exe");
            }
            catch { }
        }
    }
}
