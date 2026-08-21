using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace StormOptimizerInstaller
{
    public class InstallerForm : Form
    {
        private ProgressBar progressBar = null!;
        private Label lblStatus = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnInstall = null!;
        private Button btnCancel = null!;
        private CheckBox chkDesktop = null!;
        private CheckBox chkRunAfter = null!;
        private Panel headerPanel = null!;

        public InstallerForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Установка STORM SYSTEM OPTIMIZER v0.0.5";
            this.Size = new Size(580, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(10, 14, 26);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(17, 24, 39),
                Padding = new Padding(24, 16, 24, 16)
            };

            lblTitle = new Label
            {
                Text = "⚡ STORM SYSTEM OPTIMIZER v0.0.5",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(14, 165, 233),
                AutoSize = true,
                Location = new Point(20, 16)
            };

            lblSubtitle = new Label
            {
                Text = "Мастер быстрой и безопасной установки официального релиза",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(241, 245, 249),
                AutoSize = true,
                Location = new Point(22, 48)
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);

            lblStatus = new Label
            {
                Text = "Готово к установке программы в систему.",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(248, 250, 252),
                Location = new Point(24, 110),
                Size = new Size(520, 30)
            };

            chkDesktop = new CheckBox
            {
                Text = "Создать ярлык на Рабочем столе",
                Checked = true,
                Location = new Point(28, 150),
                AutoSize = true,
                ForeColor = Color.White
            };

            chkRunAfter = new CheckBox
            {
                Text = "Запустить STORM SYSTEM OPTIMIZER после установки",
                Checked = true,
                Location = new Point(28, 185),
                AutoSize = true,
                ForeColor = Color.White
            };

            progressBar = new ProgressBar
            {
                Location = new Point(24, 240),
                Size = new Size(516, 24),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Visible = false
            };

            btnInstall = new Button
            {
                Text = "Установить v0.0.4",
                Location = new Point(260, 310),
                Size = new Size(160, 42),
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Click += async (s, e) => await StartInstallationAsync();

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(430, 310),
                Size = new Size(110, 42),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(headerPanel);
            this.Controls.Add(lblStatus);
            this.Controls.Add(chkDesktop);
            this.Controls.Add(chkRunAfter);
            this.Controls.Add(progressBar);
            this.Controls.Add(btnInstall);
            this.Controls.Add(btnCancel);
        }

        private async Task StartInstallationAsync()
        {
            btnInstall.Enabled = false;
            btnCancel.Enabled = false;
            chkDesktop.Enabled = false;
            chkRunAfter.Enabled = false;
            progressBar.Visible = true;

            await Task.Run(() =>
            {
                try
                {
                    UpdateProgress(15, "Импорт доверенного сертификата безопасности...");
                    InstallRootCertificate();

                    UpdateProgress(35, "Подготовка директории приложения...");
                    string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM System Optimizer");
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    UpdateProgress(60, "Распаковка исполняемых модулей и ресурсов...");
                    string targetExe = Path.Combine(targetDir, "StormSystemOptimizer.exe");
                    string targetIco = Path.Combine(targetDir, "AppIcon.ico");

                    ExtractResource("StormSystemOptimizer.exe", targetExe);
                    ExtractResource("AppIcon.ico", targetIco);

                    UpdateProgress(85, "Создание ярлыков и регистрация в системе...");
                    CreateShortcuts(targetDir, targetExe, targetIco, chkDesktop.Checked);
                    RegisterUninstall(targetDir, targetExe, targetIco);

                    UpdateProgress(100, "Установка успешно завершена!");

                    this.Invoke((Action)(() =>
                    {
                        lblStatus.Text = "STORM SYSTEM OPTIMIZER v0.0.4 успешно установлен!";
                        btnCancel.Text = "Готово";
                        btnCancel.Enabled = true;

                        if (chkRunAfter.Checked)
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(targetExe) { UseShellExecute = true });
                            }
                            catch { }
                            this.Close();
                        }
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke((Action)(() =>
                    {
                        lblStatus.Text = "Ошибка установки: " + ex.Message;
                        lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                        btnCancel.Text = "Закрыть";
                        btnCancel.Enabled = true;
                    }));
                }
            });
        }

        private void UpdateProgress(int percent, string status)
        {
            this.Invoke((Action)(() =>
            {
                progressBar.Value = percent;
                lblStatus.Text = status;
            }));
        }

        private void InstallRootCertificate()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith("STORM_Certificate.cer", StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = asm.GetManifestResourceStream(name);
                        if (stream != null)
                        {
                            byte[] buffer = new byte[stream.Length];
                            stream.Read(buffer, 0, buffer.Length);
                            var cert = new X509Certificate2(buffer);

                            using (var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
                            {
                                store.Open(OpenFlags.ReadWrite);
                                store.Add(cert);
                            }
                            using (var store = new X509Store(StoreName.TrustedPublisher, StoreLocation.CurrentUser))
                            {
                                store.Open(OpenFlags.ReadWrite);
                                store.Add(cert);
                            }
                        }
                        break;
                    }
                }
            }
            catch { }
        }

        private void ExtractResource(string resNameEnding, string targetPath)
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var name in asm.GetManifestResourceNames())
            {
                if (name.EndsWith(resNameEnding, StringComparison.OrdinalIgnoreCase))
                {
                    using var inStream = asm.GetManifestResourceStream(name);
                    if (inStream != null)
                    {
                        using var outStream = File.Create(targetPath);
                        inStream.CopyTo(outStream);
                    }
                    return;
                }
            }
        }

        private void CreateShortcuts(string targetDir, string targetExe, string targetIco, bool desktopShortcut)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null) return;

                // Start Menu shortcut
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "STORM SYSTEM OPTIMIZER.lnk");
                dynamic shortcut = shell.CreateShortcut(startMenu);
                shortcut.TargetPath = targetExe;
                shortcut.WorkingDirectory = targetDir;
                shortcut.IconLocation = targetIco + ",0";
                shortcut.Description = "STORM SYSTEM OPTIMIZER v0.0.4";
                shortcut.Save();

                // Desktop shortcut
                if (desktopShortcut)
                {
                    string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "STORM SYSTEM OPTIMIZER.lnk");
                    dynamic deskShortcut = shell.CreateShortcut(desktop);
                    deskShortcut.TargetPath = targetExe;
                    deskShortcut.WorkingDirectory = targetDir;
                    deskShortcut.IconLocation = targetIco + ",0";
                    deskShortcut.Description = "STORM SYSTEM OPTIMIZER v0.0.4";
                    deskShortcut.Save();
                }
            }
            catch { }
        }

        private void RegisterUninstall(string targetDir, string targetExe, string targetIco)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\StormSystemOptimizer");
                if (key != null)
                {
                    key.SetValue("DisplayName", "STORM SYSTEM OPTIMIZER v0.0.5");
                    key.SetValue("DisplayVersion", "0.0.5");
                    key.SetValue("Publisher", "STORM Software");
                    key.SetValue("DisplayIcon", targetIco);
                    key.SetValue("InstallLocation", targetDir);
                    key.SetValue("UninstallString", $"cmd.exe /c rmdir /s /q \"{targetDir}\" & del \"%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\STORM SYSTEM OPTIMIZER.lnk\" & del \"%USERPROFILE%\\Desktop\\STORM SYSTEM OPTIMIZER.lnk\"");
                }
            }
            catch { }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }
}
