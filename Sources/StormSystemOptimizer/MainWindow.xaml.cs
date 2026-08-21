using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
