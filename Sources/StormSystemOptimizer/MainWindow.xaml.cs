using System;
using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.Controls;
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

            try
            {
                var iconUri = new Uri("pack://application:,,,/Assets/AppIcon.ico", UriKind.RelativeOrAbsolute);
                this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
            }
            catch { }

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

        private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            TxtHeaderUpdate.Text = "Проверка...";
            var res = await UpdateService.Instance.CheckForUpdatesAsync();
            if (res.HasUpdate)
            {
                TxtHeaderUpdate.Text = $"v{res.LatestVersion} доступна!";
                var answer = StormMessageBox.Show($"Обнаружена новая версия STORM SYSTEM OPTIMIZER v{res.LatestVersion}!\n\nХотите загрузить и установить обновление прямо сейчас?", "Обновление доступно", MessageBoxButton.YesNo, MessageBoxImage.Information);
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
                StormMessageBox.Show($"У вас установлена самая свежая версия: v{UpdateService.CurrentVersion}", "Обновлений не найдено", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
