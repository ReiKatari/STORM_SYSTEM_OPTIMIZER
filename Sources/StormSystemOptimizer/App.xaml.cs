using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using StormSystemOptimizer.Themes;

namespace StormSystemOptimizer
{
    public partial class App : Application
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Self-healing: Unblock self from Mark of the Web
            try
            {
                string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    DeleteFile(exePath + ":Zone.Identifier");
                }
            }
            catch { }

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
