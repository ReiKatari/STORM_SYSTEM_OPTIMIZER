using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Themes;
using StormSystemOptimizer.Views;

namespace StormSystemOptimizer
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Set TitleBar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // Default Page
            NavView.SelectedItem = NavView.MenuItems[0];

            UpdateThemeButtonLabel(ThemeManager.Instance.CurrentTheme);
            ThemeManager.Instance.ThemeChanged += (s, t) => UpdateThemeButtonLabel(t);
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
            // Cycle between: StormDark -> StormNight -> StormDay -> StormMidnight
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
