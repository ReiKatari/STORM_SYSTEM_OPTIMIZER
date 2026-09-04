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
