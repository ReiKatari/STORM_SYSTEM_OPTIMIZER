using System;
using System.IO;
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
            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StormSystemOptimizer", "crash.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);
                File.AppendAllText(logFile, $"[{DateTime.Now}] Exception: {e.Message}\n{e.Exception}\n\n");
            }
            catch { }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();

            try
            {
                ThemeManager.Instance.ApplyTheme(ThemeManager.Instance.CurrentTheme, MainWindow);
                TrayService.Instance.Initialize(MainWindow);
            }
            catch { }
        }
    }
}
