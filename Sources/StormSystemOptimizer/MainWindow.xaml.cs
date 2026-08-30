using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using StormSystemOptimizer.Controls;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;
using StormSystemOptimizer.Themes;
using StormSystemOptimizer.ViewModels;
using StormSystemOptimizer.Views;

namespace StormSystemOptimizer
{
    public partial class MainWindow : Window
    {
        private const int HOTKEY_ID_HUD = 9001;
        private HwndSource? _hwndSource;

        /// <summary>
        /// When true the app is fully closing (via tray "Выход"). 
        /// When false the X button minimizes to tray instead of closing.
        /// </summary>
        private bool _isRealExit = false;

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

            // Initialize and cache QuickMaintenance immediately
            var quickMaint = new QuickMaintenancePage();
            _pageCache["QuickMaintenance"] = quickMaint;
            MainContentFrame.Navigate(quickMaint);

            Loaded += async (s, e) =>
            {
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
                    TrayService.Instance.ShowNotification("Доступно обновление!", $"Найдена версия STORM SYSTEM OPTIMIZER v{updateRes.LatestVersion}. Нажмите для обновления.");
                }

                if (!string.IsNullOrEmpty(App.UnlockInitialPath))
                {
                    var unlockPage = new FileUnlockerPage();
                    if (unlockPage.DataContext is FileUnlockerViewModel uvm)
                    {
                        uvm.LoadPathFromCommandLine(App.UnlockInitialPath);
                    }
                    _pageCache["FileUnlocker"] = unlockPage;
                    NavRadioFileUnlocker.IsChecked = true;
                    MainContentFrame.Navigate(unlockPage);
                }
            };

            Closing += (s, e) =>
            {
                if (!_isRealExit)
                {
                    // Intercept close → minimize to tray instead
                    e.Cancel = true;
                    TrayService.Instance.MinimizeToTray(this);
                    return;
                }

                // Real exit: cleanup
                try
                {
                    var helper = new WindowInteropHelper(this);
                    NativeMethods.UnregisterHotKey(helper.Handle, HOTKEY_ID_HUD);
                }
                catch { }

                TrayService.Instance.RemoveIcon();
            };
        }

        /// <summary>
        /// Called from TrayService when user selects "Выход" in tray context menu.
        /// Forces a real shutdown, killing child processes.
        /// </summary>
        public void PerformRealExit()
        {
            _isRealExit = true;

            // Gracefully stop background services
            try { SmartDaemonService.Instance.Stop(); } catch { }

            // Kill any child or related processes (e.g. nvidia-smi, powershell subprocesses)
            try
            {
                int myPid = Environment.ProcessId;
                foreach (var proc in Process.GetProcessesByName("StormSystemOptimizer"))
                {
                    if (proc.Id != myPid)
                    {
                        try { proc.Kill(entireProcessTree: true); } catch { }
                    }
                }
            }
            catch { }

            TrayService.Instance.RemoveIcon();
            Application.Current.Shutdown();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle tray icon messages
            if (msg == TrayService.WM_TRAYICON)
            {
                int lp = lParam.ToInt32();
                if (lp == 0x0201 || lp == 0x0204) // WM_LBUTTONDOWN or WM_RBUTTONDOWN
                {
                    TrayService.Instance.HandleTrayClick(this, lp);
                    handled = true;
                    return IntPtr.Zero;
                }
            }

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

        private readonly System.Collections.Generic.Dictionary<string, Page> _pageCache = new();

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                NavigateToTag(tag);
            }
        }

        public void NavigateToTag(string tag)
        {
            if (MainContentFrame == null || string.IsNullOrEmpty(tag)) return;

            try
            {
                if (!_pageCache.TryGetValue(tag, out var page) || page == null)
                {
                    page = CreatePageForTag(tag);
                    _pageCache[tag] = page;
                }

                MainContentFrame.Navigate(page);
                SyncSidebarRadio(tag);

                if (page is DisksPage dp && dp.DataContext is DisksViewModel dvm)
                {
                    _ = dvm.LoadDrivesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть раздел: {ex.Message}", "Навигация", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private Page CreatePageForTag(string tag)
        {
            return tag switch
            {
                "QuickMaintenance" => new QuickMaintenancePage(),
                "Dashboard" => new DashboardPage(),
                "Processes" => new ProcessesPage(),
                "Disks" => new DisksPage(),
                "Benchmarks" => new BenchmarksPage(),
                "Scanner" => new ScannerPage(),
                "Startup" => new StartupPage(),
                "Services" => new ServicesPage(),
                "Network" => new NetworkPage(),
                "Privacy" => new PrivacyPage(),
                "Uninstaller" => new UninstallerPage(),
                "FolderProtection" => new FolderProtectionPage(),
                "Drivers" => new DriverUpdaterPage(),
                "SoftwareUpdater" => new SoftwareUpdaterPage(),
                "SystemInfo" => new SystemInfoPage(),
                "BiosOptimizer" => new BiosOptimizerPage(),
                "SystemTools" => new SystemToolsPage(),
                "PowerTuning" => new PowerTuningPage(),
                "InputLag" => new InputLagPage(),
                "ContextMenu" => new ContextMenuPage(),
                "BackupVault" => new BackupVaultPage(),
                "ExplorerTweaks" => new ExplorerTweaksPage(),
                "BrowserTurbo" => new BrowserTurboPage(),
                "GameLaunchers" => new GameLaunchersPage(),
                "DefenderTweaker" => new DefenderTweakerPage(),
                "MemoryMaster" => new MemoryMasterPage(),
                "AudioLatency" => new AudioLatencyPage(),
                "UsbPolling" => new UsbPollingPage(),
                "UpdateComponent" => new UpdateComponentPage(),
                "VisualPerformance" => new VisualPerformancePage(),
                "BootProfiler" => new BootProfilerPage(),
                "FileUnlocker" => new FileUnlockerPage(),
                "InterruptAffinity" => new InterruptAffinityPage(),
                "OfficeDeployer" => new OfficeDeployerPage(),
                "DatabaseOptimizer" => new DatabaseOptimizerPage(),
                "SecurityAudit" => new SecurityAuditPage(),
                "Settings" => new SettingsPage(),
                _ => new DashboardPage()
            };
        }

        private void SyncSidebarRadio(string tag)
        {
            try
            {
                foreach (var rb in FindVisualChildren<RadioButton>(this))
                {
                    if (rb.Tag is string rTag && rTag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                    {
                        rb.IsChecked = true;
                        break;
                    }
                }
            }
            catch { }
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject depObj) where T : System.Windows.DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        private void TxtGlobalSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = TxtGlobalSearch.Text.Trim();
            TxtSearchWatermark.Visibility = string.IsNullOrEmpty(q) ? Visibility.Visible : Visibility.Collapsed;
            BtnClearSearch.Visibility = string.IsNullOrEmpty(q) ? Visibility.Collapsed : Visibility.Visible;

            if (string.IsNullOrWhiteSpace(q))
            {
                PopupSearchResults.IsOpen = false;
                return;
            }

            var results = SearchIndexService.Instance.Search(q);
            ListSearchResults.ItemsSource = results;
            PopupSearchResults.IsOpen = results.Count > 0;
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            TxtGlobalSearch.Text = string.Empty;
            PopupSearchResults.IsOpen = false;
        }

        private void TxtGlobalSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                TxtGlobalSearch.Text = string.Empty;
                PopupSearchResults.IsOpen = false;
            }
        }

        private void ListSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListSearchResults.SelectedItem is SearchResultItem item)
            {
                PopupSearchResults.IsOpen = false;
                TxtGlobalSearch.Text = string.Empty;
                NavigateToTag(item.TargetTag);
            }
        }

        private void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            NavigateToTag("Settings");
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
            Close();
        }
    }
}
