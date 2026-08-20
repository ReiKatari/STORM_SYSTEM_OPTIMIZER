using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Themes;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; } = new();

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private void BtnThemeDark_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormDark, App.MainWindow);
        }

        private void BtnThemeNight_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormNight, App.MainWindow);
        }

        private void BtnThemeDay_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormDay, App.MainWindow);
        }

        private void BtnThemeMidnight_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormMidnight, App.MainWindow);
        }
    }
}
