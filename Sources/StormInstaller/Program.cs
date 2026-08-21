using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
        private Button btnBrowse = null!;

        private RadioButton rbStandard = null!;
        private RadioButton rbPortable = null!;
        private TextBox txtInstallPath = null!;

        private CheckBox chkDesktop = null!;
        private CheckBox chkStartMenu = null!;
        private CheckBox chkRegister = null!;
        private CheckBox chkInstallCert = null!;
        private CheckBox chkRunAfter = null!;
        private Panel headerPanel = null!;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        public InstallerForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Установка STORM SYSTEM OPTIMIZER v0.1.0";
            this.Size = new Size(620, 520);
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
                Text = "⚡ STORM SYSTEM OPTIMIZER v0.1.0",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(14, 165, 233),
                AutoSize = true,
                Location = new Point(20, 16)
            };

            lblSubtitle = new Label
            {
                Text = "Мастер установки обычной и портативной версии с цифровым сертификатом",
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
                Location = new Point(24, 95),
                Size = new Size(556, 330)
            };

            // Mode Selection
            var lblMode = new Label
            {
                Text = "Выберите тип установки программы:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(0, 0),
                AutoSize = true
            };
            bodyPanel.Controls.Add(lblMode);

            rbStandard = new RadioButton
            {
                Text = "Обычная установка (в систему, создание ярлыков, интеграция)",
                Checked = true,
                ForeColor = Color.White,
                Location = new Point(4, 26),
                AutoSize = true
            };
            rbStandard.CheckedChanged += RbMode_CheckedChanged;
            bodyPanel.Controls.Add(rbStandard);

            rbPortable = new RadioButton
            {
                Text = "Портативная версия (Portable в любую папку без записей в реестре)",
                Checked = false,
                ForeColor = Color.White,
                Location = new Point(4, 52),
                AutoSize = true
            };
            rbPortable.CheckedChanged += RbMode_CheckedChanged;
            bodyPanel.Controls.Add(rbPortable);

            // Path Selection
            var lblPath = new Label
            {
                Text = "Папка установки:",
                ForeColor = Color.FromArgb(156, 163, 175),
                Location = new Point(0, 84),
                AutoSize = true
            };
            bodyPanel.Controls.Add(lblPath);

            string defaultStandardPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "STORM SYSTEM OPTIMIZER");
            txtInstallPath = new TextBox
            {
                Text = defaultStandardPath,
                Location = new Point(4, 106),
                Size = new Size(430, 26),
                BackColor = Color.FromArgb(17, 24, 39),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            bodyPanel.Controls.Add(txtInstallPath);

            btnBrowse = new Button
            {
                Text = "Обзор...",
                Location = new Point(442, 105),
                Size = new Size(110, 28),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += BtnBrowse_Click;
            bodyPanel.Controls.Add(btnBrowse);

            // Checkboxes
            chkDesktop = new CheckBox
            {
                Text = "Создать ярлык на Рабочем столе",
                Checked = true,
                ForeColor = Color.White,
                Location = new Point(4, 142),
                AutoSize = true
            };
            bodyPanel.Controls.Add(chkDesktop);

            chkStartMenu = new CheckBox
            {
                Text = "Добавить в меню «Пуск»",
                Checked = true,
                ForeColor = Color.White,
                Location = new Point(270, 142),
                AutoSize = true
            };
            bodyPanel.Controls.Add(chkStartMenu);

            chkRegister = new CheckBox
            {
                Text = "Зарегистрировать в установленных программах Windows",
                Checked = true,
                ForeColor = Color.White,
                Location = new Point(4, 168),
                AutoSize = true
            };
            bodyPanel.Controls.Add(chkRegister);

            chkInstallCert = new CheckBox
            {
                Text = "Вшить сертификат доверия STORM (отключение SmartScreen)",
                Checked = true,
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(4, 194),
                AutoSize = true
            };
            bodyPanel.Controls.Add(chkInstallCert);

            chkRunAfter = new CheckBox
            {
                Text = "Запустить STORM SYSTEM OPTIMIZER после завершения",
                Checked = true,
                ForeColor = Color.White,
                Location = new Point(4, 220),
                AutoSize = true
            };
            bodyPanel.Controls.Add(chkRunAfter);

            // Status & Progress
            lblStatus = new Label
            {
                Text = "Нажмите «Установить» для начала распаковки и настройки...",
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(4, 252),
                Size = new Size(548, 20)
            };
            bodyPanel.Controls.Add(lblStatus);

            progressBar = new ProgressBar
            {
                Location = new Point(4, 276),
                Size = new Size(548, 22),
                Style = ProgressBarStyle.Continuous,
                Value = 0
            };
            bodyPanel.Controls.Add(progressBar);

            this.Controls.Add(bodyPanel);

            // Bottom Buttons
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            btnInstall = new Button
            {
                Text = "⚡ Установить",
                Size = new Size(140, 36),
                Location = new Point(315, 12),
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
                Location = new Point(465, 12),
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

        private void RbMode_CheckedChanged(object? sender, EventArgs e)
        {
            if (rbPortable.Checked)
            {
                txtInstallPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "STORM_SYSTEM_OPTIMIZER_Portable");
                chkDesktop.Checked = false;
                chkStartMenu.Checked = false;
                chkRegister.Checked = false;
                btnInstall.Text = "⚡ Распаковать";
            }
            else
            {
                txtInstallPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "STORM SYSTEM OPTIMIZER");
                chkDesktop.Checked = true;
                chkStartMenu.Checked = true;
                chkRegister.Checked = true;
                btnInstall.Text = "⚡ Установить";
            }
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            dlg.Description = "Выберите папку для установки STORM SYSTEM OPTIMIZER:";
            dlg.SelectedPath = txtInstallPath.Text;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtInstallPath.Text = dlg.SelectedPath;
            }
        }

        private async Task StartInstallationAsync()
        {
            btnInstall.Enabled = false;
            btnCancel.Enabled = false;
            btnBrowse.Enabled = false;

            try
            {
                string targetDir = txtInstallPath.Text.Trim();
                if (string.IsNullOrEmpty(targetDir))
                {
                    targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "STORM SYSTEM OPTIMIZER");
                }

                Directory.CreateDirectory(targetDir);

                // Terminate any running instances
                lblStatus.Text = "Завершение предыдущих процессов программы...";
                progressBar.Value = 15;
                await Task.Delay(200);

                foreach (var p in Process.GetProcessesByName("StormSystemOptimizer"))
                {
                    try { p.Kill(); p.WaitForExit(1500); } catch { }
                }

                string targetExe = Path.Combine(targetDir, "StormSystemOptimizer.exe");
                string targetCer = Path.Combine(targetDir, "STORM_Certificate.cer");
                string targetIco = Path.Combine(targetDir, "AppIcon.ico");

                if (chkInstallCert.Checked)
                {
                    lblStatus.Text = "Установка доверенного сертификата STORM Software...";
                    progressBar.Value = 40;
                    await Task.Delay(150);

                    ExtractResource("STORM_Certificate.cer", targetCer);
                    if (File.Exists(targetCer))
                    {
                        InstallCertificate(targetCer);
                    }
                }

                lblStatus.Text = "Распаковка исполняемых файлов программы (v0.1.0)...";
                progressBar.Value = 70;
                await Task.Delay(250);

                ExtractResource("StormSystemOptimizer.exe", targetExe);
                ExtractResource("AppIcon.ico", targetIco);

                // Unblock files (Remove Zone.Identifier Mark-of-the-Web)
                UnblockFile(targetExe);
                UnblockFile(targetCer);
                UnblockFile(targetIco);

                if (rbStandard.Checked)
                {
                    lblStatus.Text = "Создание системных ярлыков и регистрация в Windows...";
                    progressBar.Value = 90;
                    await Task.Delay(150);

                    CreateShortcuts(targetDir, targetExe, targetIco, chkDesktop.Checked, chkStartMenu.Checked);

                    if (chkRegister.Checked)
                    {
                        RegisterUninstall(targetDir, targetExe, targetIco);
                    }
                }

                progressBar.Value = 100;
                lblStatus.Text = rbPortable.Checked ? "Портативная версия успешно распакована!" : "Установка успешно завершена!";
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
                btnBrowse.Enabled = true;
            }
        }

        private void UnblockFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    DeleteFile(path + ":Zone.Identifier");
                }
            }
            catch { }
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

                try
                {
                    using (var lmRoot = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
                    {
                        lmRoot.Open(OpenFlags.ReadWrite);
                        lmRoot.Add(cert);
                    }
                    using (var lmPub = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine))
                    {
                        lmPub.Open(OpenFlags.ReadWrite);
                        lmPub.Add(cert);
                    }
                }
                catch { }
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

        private void CreateShortcuts(string targetDir, string targetExe, string targetIco, bool desktopShortcut, bool startMenuShortcut)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null) return;

                // Start Menu shortcut
                if (startMenuShortcut)
                {
                    string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "STORM SYSTEM OPTIMIZER.lnk");
                    dynamic shortcut = shell.CreateShortcut(startMenu);
                    shortcut.TargetPath = targetExe;
                    shortcut.WorkingDirectory = targetDir;
                    shortcut.IconLocation = targetIco + ",0";
                    shortcut.Description = "STORM SYSTEM OPTIMIZER v0.1.0";
                    shortcut.Save();
                }

                // Desktop shortcut
                if (desktopShortcut)
                {
                    string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "STORM SYSTEM OPTIMIZER.lnk");
                    dynamic deskShortcut = shell.CreateShortcut(desktop);
                    deskShortcut.TargetPath = targetExe;
                    deskShortcut.WorkingDirectory = targetDir;
                    deskShortcut.IconLocation = targetIco + ",0";
                    deskShortcut.Description = "STORM SYSTEM OPTIMIZER v0.1.0";
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
                    key.SetValue("DisplayName", "STORM SYSTEM OPTIMIZER v0.1.0");
                    key.SetValue("DisplayVersion", "0.1.0");
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
