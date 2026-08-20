using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace StormSystemOptimizer.Services
{
    public class TrayService
    {
        private static TrayService? _instance;
        public static TrayService Instance => _instance ??= new TrayService();

        private NativeMethods.NOTIFYICONDATA _nid;
        private bool _isCreated = false;
        private IntPtr _hwnd = IntPtr.Zero;
        private Window? _mainWindow;

        private const int TRAY_ICON_ID = 1001;
        private const int WM_TRAYICON = 0x8000 + 1;

        private TrayService() { }

        public void Initialize(Window window)
        {
            _mainWindow = window;
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                IntPtr hIcon = IntPtr.Zero;

                if (File.Exists(iconPath))
                {
                    using var icon = new Icon(iconPath, new Size(16, 16));
                    hIcon = icon.Handle;
                }

                _nid = new NativeMethods.NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf(typeof(NativeMethods.NOTIFYICONDATA)),
                    hWnd = _hwnd,
                    uID = TRAY_ICON_ID,
                    uFlags = NativeMethods.NIF_ICON | NativeMethods.NIF_TIP | NativeMethods.NIF_MESSAGE,
                    uCallbackMessage = WM_TRAYICON,
                    hIcon = hIcon,
                    szTip = "STORM SYSTEM OPTIMIZER v0.0.1"
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

        public void MinimizeToTray()
        {
            if (_hwnd != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(_hwnd, NativeMethods.SW_HIDE);
                ShowNotification("STORM OPTIMIZER в фоне", "Приложение свернуто в трей. Нажмите значок для открытия.");
            }
        }

        public void RestoreFromTray()
        {
            if (_hwnd != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(_hwnd, NativeMethods.SW_RESTORE);
                NativeMethods.SetForegroundWindow(_hwnd);
            }
        }

        public void RemoveIcon()
        {
            if (_isCreated)
            {
                NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref _nid);
                _isCreated = false;
            }
        }
    }
}
