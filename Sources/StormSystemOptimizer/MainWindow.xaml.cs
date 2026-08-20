using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Themes;
using StormSystemOptimizer.Views;
using Windows.Graphics;

namespace StormSystemOptimizer
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow? _appWindow;

        public MainWindow()
        {
            this.InitializeComponent();

            ConfigureWindow();

            // Default Page
            NavView.SelectedItem = NavView.MenuItems[0];

            UpdateThemeButtonLabel(ThemeManager.Instance.CurrentTheme);
            ThemeManager.Instance.ThemeChanged += (s, t) => UpdateThemeButtonLabel(t);
        }

        private void ConfigureWindow()
        {
            try
            {
                IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                _appWindow = AppWindow.GetFromWindowId(windowId);

                if (_appWindow != null)
                {
                    // Title Bar Configuration
                    ExtendsContentIntoTitleBar = true;
                    SetTitleBar(AppTitleBar);

                    // Resize and Center
                    int width = 1180;
                    int height = 780;
                    _appWindow.Resize(new SizeInt32(width, height));

                    var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                    if (displayArea != null)
                    {
                        var centeredPos = new PointInt32(
                            (displayArea.WorkArea.Width - width) / 2,
                            (displayArea.WorkArea.Height - height) / 2
                        );
                        _appWindow.Move(centeredPos);
                    }

                    // Set App Icon on TitleBar
                    string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                    if (System.IO.File.Exists(iconPath))
                    {
                        _appWindow.SetIcon(iconPath);
                    }
                }
            }
            catch { }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                Type? pageType = tag switch
                {
                    "Dashboard" => typeof(DashboardPage),
                    "Scanner" => typeof(ScannerPage),
                    "Startup" => typeof(StartupPage),
                    "Services" => typeof(ServicesPage),
                    "Network" => typeof(NetworkPage),
                    "Privacy" => typeof(PrivacyPage),
                    "SystemTools" => typeof(SystemToolsPage),
                    "Settings" => typeof(SettingsPage),
                    _ => typeof(DashboardPage)
                };

                if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
                {
                    ContentFrame.Navigate(pageType);
                }
            }
        }

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            var current = ThemeManager.Instance.CurrentTheme;
            var next = current switch
            {
                ThemeType.StormDark => ThemeType.StormNight,
                ThemeType.StormNight => ThemeType.StormDay,
                ThemeType.StormDay => ThemeType.StormMidnight,
                ThemeType.StormMidnight => ThemeType.StormDark,
                _ => ThemeType.StormDark
            };

            ThemeManager.Instance.ApplyTheme(next, this);
        }

        private void UpdateThemeButtonLabel(ThemeType theme)
        {
            TxtCurrentTheme.Text = theme switch
            {
                ThemeType.StormDark => "STORM DARK",
                ThemeType.StormNight => "STORM NIGHT",
                ThemeType.StormDay => "STORM DAY",
                ThemeType.StormMidnight => "STORM MIDNIGHT",
                _ => "STORM DARK"
            };
        }
    }
}
