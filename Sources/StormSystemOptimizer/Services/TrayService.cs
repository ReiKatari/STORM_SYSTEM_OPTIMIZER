using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StormSystemOptimizer.Services
{
    public class TrayService
    {
        private static TrayService? _instance;
        public static TrayService Instance => _instance ??= new TrayService();

        private NativeMethods.NOTIFYICONDATA _nid;
        private bool _isCreated = false;
        private IntPtr _hwnd = IntPtr.Zero;
        private Icon? _trayIcon;

        private const int TRAY_ICON_ID = 1001;
        public const int WM_TRAYICON = 0x8000 + 1;

        // Win32 context menu constants
        private const uint MF_STRING = 0x00000000;
        private const uint MF_SEPARATOR = 0x00000800;
        private const uint MF_GRAYED = 0x00000001;
        private const uint TPM_RETURNCMD = 0x0100;
        private const uint TPM_NONOTIFY = 0x0080;
        private const uint TPM_RIGHTBUTTON = 0x0002;
        private const uint TPM_LEFTALIGN = 0x0000;

        private const int CMD_OPEN = 1;
        private const int CMD_EXIT = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private TrayService() { }

        public void Initialize(Window window)
        {
            try
            {
                var helper = new WindowInteropHelper(window);
                _hwnd = helper.Handle;

                IntPtr hIcon = IntPtr.Zero;

                // 1. Try loading from WPF Pack URI resource stream (works inside Single-File bundle)
                try
                {
                    var iconUri = new Uri("pack://application:,,,/Assets/AppIcon.ico", UriKind.RelativeOrAbsolute);
                    var streamInfo = Application.GetResourceStream(iconUri);
                    if (streamInfo != null)
                    {
                        using var s = streamInfo.Stream;
                        _trayIcon = new System.Drawing.Icon(s, new System.Drawing.Size(16, 16));
                        hIcon = _trayIcon.Handle;
                    }
                }
                catch { }

                // 2. Fallback: Extract associated icon from running main module executable
                if (hIcon == IntPtr.Zero)
                {
                    try
                    {
                        string? mainExe = Process.GetCurrentProcess().MainModule?.FileName;
                        if (!string.IsNullOrEmpty(mainExe) && File.Exists(mainExe))
                        {
                            var extracted = System.Drawing.Icon.ExtractAssociatedIcon(mainExe);
                            if (extracted != null)
                            {
                                _trayIcon = new System.Drawing.Icon(extracted, new System.Drawing.Size(16, 16));
                                hIcon = _trayIcon.Handle;
                            }
                        }
                    }
                    catch { }
                }

                // 3. Fallback: File on disk in AppDomain BaseDirectory
                if (hIcon == IntPtr.Zero)
                {
                    try
                    {
                        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AppIcon.ico");
                        if (File.Exists(iconPath))
                        {
                            _trayIcon = new System.Drawing.Icon(iconPath, new System.Drawing.Size(16, 16));
                            hIcon = _trayIcon.Handle;
                        }
                    }
                    catch { }
                }

                _nid = new NativeMethods.NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf(typeof(NativeMethods.NOTIFYICONDATA)),
                    hWnd = _hwnd,
                    uID = TRAY_ICON_ID,
                    uFlags = NativeMethods.NIF_ICON | NativeMethods.NIF_TIP | NativeMethods.NIF_MESSAGE,
                    uCallbackMessage = WM_TRAYICON,
                    hIcon = hIcon,
                    szTip = "STORM SYSTEM OPTIMIZER 2.0.7"
                };

                _isCreated = NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref _nid);
            }
            catch { }
        }

        public void ShowNotification(string title, string message)
        {
            if (!_isCreated) return;
            try
            {
                _nid.uFlags |= NativeMethods.NIF_INFO;
                _nid.szInfoTitle = title;
                _nid.szInfo = message;
                _nid.dwInfoFlags = NativeMethods.NIIF_INFO;
                NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_MODIFY, ref _nid);
            }
            catch { }
        }

        public void MinimizeToTray(Window window)
        {
            try
            {
                window.Hide();
                ShowNotification("STORM SYSTEM OPTIMIZER в фоне", "Приложение свернуто в системный трей. Нажмите правой кнопкой для меню.");
            }
            catch { }
        }

        public void RestoreFromTray(Window window)
        {
            try
            {
                window.Show();
                window.WindowState = WindowState.Normal;
                window.Activate();
                NativeMethods.SetForegroundWindow(new WindowInteropHelper(window).Handle);
            }
            catch { }
        }

        public void HandleTrayClick(Window window, int message)
        {
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_RBUTTONDOWN = 0x0204;
            const int WM_LBUTTONDBLCLK = 0x0203;

            if (message == WM_LBUTTONDOWN || message == WM_LBUTTONDBLCLK)
            {
                RestoreFromTray(window);
            }
            else if (message == WM_RBUTTONDOWN)
            {
                ShowTrayContextMenu(window);
            }
        }

        private const int CMD_CLEAN_RAM = 3;
        private const int CMD_GAME_BOOST = 4;
        private const int CMD_RESTORE_POINT = 5;

        private void ShowTrayContextMenu(Window window)
        {
            try
            {
                IntPtr hMenu = CreatePopupMenu();
                if (hMenu == IntPtr.Zero) return;

                AppendMenu(hMenu, MF_STRING, CMD_OPEN, "Открыть STORM SYSTEM OPTIMIZER");
                AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
                AppendMenu(hMenu, MF_STRING, CMD_CLEAN_RAM, "Очистить оперативную память (RAM)");
                AppendMenu(hMenu, MF_STRING, CMD_GAME_BOOST, "Активировать игровой режим (Game Boost)");
                AppendMenu(hMenu, MF_STRING, CMD_RESTORE_POINT, "Создать точку восстановления системы");
                AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
                AppendMenu(hMenu, MF_STRING, CMD_EXIT, "Закрыть приложение (полный выход)");

                GetCursorPos(out POINT pt);

                NativeMethods.SetForegroundWindow(_hwnd);
                int cmd = TrackPopupMenu(hMenu, TPM_RETURNCMD | TPM_NONOTIFY | TPM_LEFTALIGN | TPM_RIGHTBUTTON,
                    pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);

                DestroyMenu(hMenu);

                switch (cmd)
                {
                    case CMD_OPEN:
                        RestoreFromTray(window);
                        break;
                    case CMD_CLEAN_RAM:
                        _ = System.Threading.Tasks.Task.Run(() =>
                        {
                            MemoryOptimizerService.Instance.PurgeStandbyList();
                            MemoryOptimizerService.Instance.PurgeWorkingSets();
                        });
                        ShowNotification("Очистка RAM", "Рабочие наборы и Standby List успешно очищены.");
                        break;
                    case CMD_GAME_BOOST:
                        _ = System.Threading.Tasks.Task.Run(() =>
                        {
                            GameBoostService.Instance.ActivateGameBoost();
                        });
                        ShowNotification("Game Boost", "Игровой профиль наивысшего приоритета активирован.");
                        break;
                    case CMD_RESTORE_POINT:
                        _ = BackupVaultService.Instance.CreateRestorePointAsync("STORM_TRAY_RESTOREPOINT");
                        break;
                    case CMD_EXIT:
                        if (window is MainWindow mw)
                        {
                            mw.PerformRealExit();
                        }
                        else
                        {
                            RemoveIcon();
                            Application.Current.Shutdown();
                        }
                        break;
                }
            }
            catch { }
        }

        public void RemoveIcon()
        {
            if (_isCreated)
            {
                NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref _nid);
                _isCreated = false;
            }
            _trayIcon?.Dispose();
        }
    }
}
