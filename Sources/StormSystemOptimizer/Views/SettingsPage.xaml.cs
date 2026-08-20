using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Themes;

namespace StormSystemOptimizer.Views
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void BtnThemeDark_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormDark, Application.Current.MainWindow);
        }

        private void BtnThemeNight_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormNight, Application.Current.MainWindow);
        }

        private void BtnThemeDay_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormDay, Application.Current.MainWindow);
        }

        private void BtnThemeMidnight_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormMidnight, Application.Current.MainWindow);
        }
    }
}
