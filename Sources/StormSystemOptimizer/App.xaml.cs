using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using StormSystemOptimizer.Themes;

namespace StormSystemOptimizer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LogCrash(args.ExceptionObject as Exception);
            };

            DispatcherUnhandledException += (s, args) =>
            {
                LogCrash(args.Exception);
                args.Handled = true;
            };

            // Apply saved theme
            ThemeManager.Instance.ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }

        private void LogCrash(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StormSystemOptimizer");
                Directory.CreateDirectory(logDir);
                File.AppendAllText(Path.Combine(logDir, "crash.log"), $"[{DateTime.Now}] {ex}\n\n");
            }
            catch { }
        }
    }
}
