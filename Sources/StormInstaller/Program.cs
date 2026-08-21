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
            this.Text = "Установка STORM SYSTEM OPTIMIZER v0.0.7";
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
                Text = "⚡ STORM SYSTEM OPTIMIZER v0.0.7",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(14, 165, 233),
                AutoSize = true,
                Location = new Point(20, 16)
            };

            lblSubtitle = new Label
            {
                Text = "Мастер быстрой и безопасной установки официального релиза",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize = true,
                Location = new Point(22, 48)
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);
            this.Controls.Add(headerPanel);

            var bodyPanel = new Panel
            {
                Location = new Point(24, 105),
                Size = new Size(516, 210)
            };

            var lblDesc = new Label
            {
                Text = "Программа будет установлена в вашу персональную директорию:\n%LOCALAPPDATA%\\STORM SYSTEM OPTIMIZER\n\nВсе сертификаты доверия и компоненты оптимизации вшиты в установщик.",
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(0, 0),
                Size = new Size(516, 65)
            };
            bodyPanel.Controls.Add(lblDesc);

            chkDesktop = new CheckBox
            {
                Text = "Создать ярлык на Рабочем столе",
                Checked = true,
                ForeColor = Color.White,
                Location = new Point(4, 75),
                AutoSize = true
            };
            bodyPanel.Controls.Add(chkDesktop);

            chkRunAfter = new CheckBox
            {
                Text = "Запустить STORM SYSTEM OPTIMIZER после завершения установки",
                Checked = true,
                ForeColor = Color.White,
                Location = new Point(4, 105),
                AutoSize = true
            };
            bodyPanel.Controls.Add(chkRunAfter);

            lblStatus = new Label
            {
                Text = "Нажмите «Установить» для начала распаковки и настройки...",
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(4, 145),
                Size = new Size(508, 20)
            };
            bodyPanel.Controls.Add(lblStatus);

            progressBar = new ProgressBar
            {
                Location = new Point(4, 172),
                Size = new Size(508, 22),
                Style = ProgressBarStyle.Continuous,
                Value = 0
            };
            bodyPanel.Controls.Add(progressBar);

            this.Controls.Add(bodyPanel);

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            btnInstall = new Button
            {
                Text = "⚡ Установить",
                Size = new Size(130, 36),
                Location = new Point(285, 12),
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Click += async (s, e) => await StartInstallationAsync();

            btnCancel = new Button
            {
                Text = "Отмена",
                Size = new Size(100, 36),
                Location = new Point(425, 12),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(203, 213, 225),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();

            bottomPanel.Controls.Add(btnInstall);
            bottomPanel.Controls.Add(btnCancel);
            this.Controls.Add(bottomPanel);
        }

        private async Task StartInstallationAsync()
        {
            btnInstall.Enabled = false;
            btnCancel.Enabled = false;
            chkDesktop.Enabled = false;
            chkRunAfter.Enabled = false;

            string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STORM SYSTEM OPTIMIZER");
            string targetExe = Path.Combine(targetDir, "StormSystemOptimizer.exe");
            string targetIco = Path.Combine(targetDir, "AppIcon.ico");
            string targetCer = Path.Combine(targetDir, "STORM_Certificate.cer");

            try
            {
                lblStatus.Text = "Подготовка целевой директории...";
                progressBar.Value = 15;
                await Task.Delay(200);

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                lblStatus.Text = "Установка корневого доверенного сертификата STORM Software...";
                progressBar.Value = 35;
                await Task.Delay(200);

                ExtractResource("STORM_Certificate.cer", targetCer);
                if (File.Exists(targetCer))
                {
                    InstallCertificate(targetCer);
                }

                lblStatus.Text = "Распаковка исполняемых файлов программы (v0.0.6)...";
                progressBar.Value = 65;
                await Task.Delay(300);

                ExtractResource("StormSystemOptimizer.exe", targetExe);
                ExtractResource("AppIcon.ico", targetIco);

                lblStatus.Text = "Создание системных ярлыков и регистрация в Windows...";
                progressBar.Value = 85;
                await Task.Delay(200);

                CreateShortcuts(targetDir, targetExe, targetIco, chkDesktop.Checked);
                RegisterUninstall(targetDir, targetExe, targetIco);

                progressBar.Value = 100;
                lblStatus.Text = "Установка успешно завершена!";
                lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                await Task.Delay(500);

                if (chkRunAfter.Checked && File.Exists(targetExe))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = targetExe,
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    });
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка во время установки:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnInstall.Enabled = true;
                btnCancel.Enabled = true;
            }
        }

        private void InstallCertificate(string cerPath)
        {
            try
            {
                var cert = new X509Certificate2(cerPath);
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
                shortcut.Description = "STORM SYSTEM OPTIMIZER v0.0.6";
                shortcut.Save();

                // Desktop shortcut
                if (desktopShortcut)
                {
                    string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "STORM SYSTEM OPTIMIZER.lnk");
                    dynamic deskShortcut = shell.CreateShortcut(desktop);
                    deskShortcut.TargetPath = targetExe;
                    deskShortcut.WorkingDirectory = targetDir;
                    deskShortcut.IconLocation = targetIco + ",0";
                    deskShortcut.Description = "STORM SYSTEM OPTIMIZER v0.0.6";
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
                    key.SetValue("DisplayName", "STORM SYSTEM OPTIMIZER v0.0.6");
                    key.SetValue("DisplayVersion", "0.0.6");
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
