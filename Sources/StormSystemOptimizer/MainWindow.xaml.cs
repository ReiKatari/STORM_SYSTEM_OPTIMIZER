using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using StormSystemOptimizer.Controls;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;
using StormSystemOptimizer.Themes;
using StormSystemOptimizer.Views;

namespace StormSystemOptimizer
{
    public partial class MainWindow : Window
    {
        private const int HOTKEY_ID_HUD = 9001;
        private HwndSource? _hwndSource;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                var iconUri = new Uri("pack://application:,,,/Assets/AppIcon.ico", UriKind.RelativeOrAbsolute);
                var streamInfo = Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    using var s = streamInfo.Stream;
                    this.Icon = BitmapFrame.Create(s);
                }
            }
            catch { }

            Loaded += async (s, e) =>
            {
                MainContentFrame.Navigate(new DashboardPage());
                TrayService.Instance.Initialize(this);

                // Register Global Hotkey Ctrl+Shift+O for Gaming HUD Overlay
                try
                {
                    var helper = new WindowInteropHelper(this);
                    _hwndSource = HwndSource.FromHwnd(helper.Handle);
                    _hwndSource?.AddHook(HwndHook);

                    // VK_O = 0x4F, MOD_CONTROL | MOD_SHIFT
                    NativeMethods.RegisterHotKey(helper.Handle, HOTKEY_ID_HUD, NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT, 0x4F);
                }
                catch { }

                // Auto-check for updates in background
                var updateRes = await UpdateService.Instance.CheckForUpdatesAsync();
                if (updateRes.HasUpdate)
                {
                    TxtHeaderUpdate.Text = $"Новая v{updateRes.LatestVersion}!";
                    TxtHeaderUpdate.Foreground = (System.Windows.Media.Brush)FindResource("AccentSecondaryBrush");
                    TrayService.Instance.ShowNotification("Доступно обновление!", $"Найдена версия STORM OPTIMIZER v{updateRes.LatestVersion}. Нажмите для обновления.");
                }
            };

            Closed += (s, e) =>
            {
                try
                {
                    var helper = new WindowInteropHelper(this);
                    NativeMethods.UnregisterHotKey(helper.Handle, HOTKEY_ID_HUD);
                }
                catch { }
            };
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_HUD)
                {
                    StormOverlayWindow.Instance.ToggleVisibility();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                Page? page = tag switch
                {
                    "Dashboard" => new DashboardPage(),
                    "Processes" => new ProcessesPage(),
                    "Disks" => new DisksPage(),
                    "Benchmarks" => new BenchmarksPage(),
                    "Scanner" => new ScannerPage(),
                    "Startup" => new StartupPage(),
                    "Services" => new ServicesPage(),
                    "Network" => new NetworkPage(),
                    "Privacy" => new PrivacyPage(),
                    "SystemTools" => new SystemToolsPage(),
                    "Settings" => new SettingsPage(),
                    _ => new DashboardPage()
                };

                if (page != null)
                {
                    MainContentFrame.Navigate(page);
                }
            }
        }

        private void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Navigate(new SettingsPage());
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            TrayService.Instance.RemoveIcon();
            Application.Current.Shutdown();
        }
    }
}
