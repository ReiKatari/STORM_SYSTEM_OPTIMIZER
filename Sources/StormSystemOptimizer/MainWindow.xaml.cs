using System;
using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;
using StormSystemOptimizer.Themes;
using StormSystemOptimizer.Views;

namespace StormSystemOptimizer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ThemeManager.Instance.ThemeChanged += (s, t) => UpdateThemeButtonLabel(t);
            UpdateThemeButtonLabel(ThemeManager.Instance.CurrentTheme);

            Loaded += (s, e) =>
            {
                MainContentFrame.Navigate(new DashboardPage());
                TrayService.Instance.Initialize(this);
            };
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                Page? page = tag switch
                {
                    "Dashboard" => new DashboardPage(),
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

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
