using System;
using Microsoft.UI.Xaml;
using StormSystemOptimizer.Services;
using StormSystemOptimizer.Themes;

namespace StormSystemOptimizer
{
    public partial class App : Application
    {
        public static MainWindow? MainWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            
            // Apply saved theme
            ThemeManager.Instance.ApplyTheme(ThemeManager.Instance.CurrentTheme, MainWindow);

            MainWindow.Activate();

            // Initialize System Tray
            TrayService.Instance.Initialize(MainWindow);
        }
    }
}
