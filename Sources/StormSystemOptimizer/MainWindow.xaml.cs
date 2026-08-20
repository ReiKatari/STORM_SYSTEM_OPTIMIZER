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

            Loaded += async (s, e) =>
            {
                MainContentFrame.Navigate(new DashboardPage());
                TrayService.Instance.Initialize(this);

                // Auto-check for updates in background
                var updateRes = await UpdateService.Instance.CheckForUpdatesAsync();
                if (updateRes.HasUpdate)
                {
                    TxtHeaderUpdate.Text = $"Новая v{updateRes.LatestVersion}!";
                    TxtHeaderUpdate.Foreground = (System.Windows.Media.Brush)FindResource("AccentSecondaryBrush");
                    TrayService.Instance.ShowNotification("Доступно обновление!", $"Найдена версия STORM OPTIMIZER v{updateRes.LatestVersion}. Нажмите для обновления.");
                }
            };
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                Page? page = tag switch
                {
                    "Dashboard" => new DashboardPage(),
                    "Processes" => new ProcessesPage(),
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

        private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            TxtHeaderUpdate.Text = "Проверка...";
            var res = await UpdateService.Instance.CheckForUpdatesAsync();
            if (res.HasUpdate)
            {
                TxtHeaderUpdate.Text = $"v{res.LatestVersion} доступна!";
                var answer = MessageBox.Show($"Обнаружена новая версия STORM SYSTEM OPTIMIZER v{res.LatestVersion}!\n\nХотите загрузить и установить обновление прямо сейчас?", "Обновление доступно", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (answer == MessageBoxResult.Yes)
                {
                    if (!string.IsNullOrEmpty(res.DownloadUrl))
                    {
                        TxtHeaderUpdate.Text = "Скачивание...";
                        await UpdateService.Instance.DownloadAndApplyUpdateAsync(res.DownloadUrl);
                    }
                    else
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(res.ReleasePageUrl) { UseShellExecute = true });
                    }
                }
            }
            else
            {
                TxtHeaderUpdate.Text = "Актуально";
                MessageBox.Show($"У вас установлена самая свежая версия: v{UpdateService.CurrentVersion}", "Обновлений не найдено", MessageBoxButton.OK, MessageBoxImage.Information);
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
