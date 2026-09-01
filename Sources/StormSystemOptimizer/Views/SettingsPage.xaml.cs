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

        private void BtnThemeMatrix_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormMatrix, Application.Current.MainWindow);
        }

        private void BtnThemeCyberpunk_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormCyberpunk, Application.Current.MainWindow);
        }

        private void BtnThemeFantasy_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormFantasy, Application.Current.MainWindow);
        }

        private void BtnThemeWarhammer_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme(ThemeType.StormWarhammer, Application.Current.MainWindow);
        }

        private void BtnLangRu_Click(object sender, RoutedEventArgs e)
        {
            Services.LocalizationService.Instance.CurrentLanguage = "ru";
        }

        private void BtnLangEn_Click(object sender, RoutedEventArgs e)
        {
            Services.LocalizationService.Instance.CurrentLanguage = "en";
        }

        private void BtnLangDe_Click(object sender, RoutedEventArgs e)
        {
            Services.LocalizationService.Instance.CurrentLanguage = "de";
        }

        private void BtnLangFr_Click(object sender, RoutedEventArgs e)
        {
            Services.LocalizationService.Instance.CurrentLanguage = "fr";
        }

        private void BtnLangZh_Click(object sender, RoutedEventArgs e)
        {
            Services.LocalizationService.Instance.CurrentLanguage = "zh";
        }

        private void BtnLangJa_Click(object sender, RoutedEventArgs e)
        {
            Services.LocalizationService.Instance.CurrentLanguage = "ja";
        }
    }
}
