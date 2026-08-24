using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace StormUniversal.Installer
{
    public class InstallerForm : Form
    {
        private ProgressBar progressBar = null!;
        private Label lblStatus = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnInstall = null!;
        private Button btnCancel = null!;
        private PictureBox picHeaderLogo = null!;
        private Panel headerPanel = null!;

        private const string AppVersion = "1.1.0";
        private const string AppDisplayName = "STORM SYSTEM OPTIMIZER";
        private const string AppFolderName = "STORM SYSTEM OPTIMIZER";
        private const string ExeName = "StormSystemOptimizer.exe";
        private const string LauncherExeName = "StormLauncher.exe";
        private const string IcoName = "AppIcon.ico";

        private RadioButton rbStandard = null!;
        private RadioButton rbPortable = null!;
        private TextBox txtInstallPath = null!;
        private Button btnBrowse = null!;

        private CheckBox chkDesktop = null!;
        private CheckBox chkStartMenu = null!;
        private CheckBox chkRegister = null!;
        private CheckBox chkInstallCert = null!;
        private CheckBox chkRunAfter = null!;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        public InstallerForm()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith("AppIcon.ico", StringComparison.OrdinalIgnoreCase) || name.EndsWith("app.ico", StringComparison.OrdinalIgnoreCase))
                    {
                        using var s = asm.GetManifestResourceStream(name);
                        if (s != null)
                        {
                            this.Icon = new Icon(s);
                            break;
                        }
                    }
                }
                if (this.Icon == null && !string.IsNullOrEmpty(Application.ExecutablePath) && File.Exists(Application.ExecutablePath))
                {
                    this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch { }
            InitializeComponent();
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void InitializeComponent()
        {
            this.Text = $"{AppDisplayName} вЂ” STORM INSTALLER";
            this.Size = new Size(640, 540);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(11, 15, 25);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // 1. Dark Stylized Header
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = Color.FromArgb(17, 24, 39),
                Padding = new Padding(22, 14, 22, 14)
            };
            headerPanel.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(14, 165, 233), 2f);
                e.Graphics.DrawLine(p, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            lblTitle = new Label
            {
                Text = AppDisplayName,
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(14, 165, 233),
                AutoSize = true,
                Location = new Point(22, 16)
            };

            lblSubtitle = new Label
            {
                Text = $"РњР°СЃС‚РµСЂ СѓСЃС‚Р°РЅРѕРІРєРё вЂў Р’РµСЂСЃРёСЏ {AppVersion} вЂў STORM TEAM",
                Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize = true,
                Location = new Point(24, 49)
            };

            // Top-Right Header Icon Container Badge
            var logoContainer = new Panel
            {
                Location = new Point(546, 12),
                Size = new Size(62, 62),
                BackColor = Color.Transparent
            };
            logoContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = GetRoundedRectPath(new Rectangle(0, 0, 61, 61), 10);
                using var brush = new SolidBrush(Color.FromArgb(20, 10, 15));
                using var pen = new Pen(Color.FromArgb(225, 29, 72), 1.5f); // Crimson glow border
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            };

            picHeaderLogo = new PictureBox
            {
                Location = new Point(5, 5),
                Size = new Size(52, 52),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            Image? logoImg = null;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith("header_badge.png", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("badge_logo.png", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase))
                    {
                        using var s = asm.GetManifestResourceStream(name);
                        if (s != null)
                        {
                            logoImg = Image.FromStream(s);
                            break;
                        }
                    }
                }
            }
            catch { }

            if (logoImg != null)
            {
                picHeaderLogo.Image = logoImg;
            }
            else if (this.Icon != null)
            {
                picHeaderLogo.Image = this.Icon.ToBitmap();
            }

            logoContainer.Controls.Add(picHeaderLogo);

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);
            headerPanel.Controls.Add(logoContainer);
            this.Controls.Add(headerPanel);

            // 2. Body Panel
            var bodyPanel = new Panel
            {
                Location = new Point(24, 98),
                Size = new Size(576, 350)
            };

            var lblMode = new Label
            {
                Text = "Р’С‹Р±РµСЂРёС‚Рµ С‚РёРї СѓСЃС‚Р°РЅРѕРІРєРё РїСЂРѕРіСЂР°РјРјС‹:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(0, 0),
                AutoSize = true
            };
            bodyPanel.Controls.Add(lblMode);

            rbStandard = new RadioButton
            {
                Text = "РЎС‚Р°РЅРґР°СЂС‚РЅР°СЏ СѓСЃС‚Р°РЅРѕРІРєР° РІ Program Files (СЂРµРєРѕРјРµРЅРґСѓРµС‚СЃСЏ)",
                Checked = true,
                Location = new Point(10, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.White
            };
            rbStandard.CheckedChanged += Mode_CheckedChanged;
            bodyPanel.Controls.Add(rbStandard);

            rbPortable = new RadioButton
            {
                Text = "РџРѕСЂС‚Р°С‚РёРІРЅР°СЏ РІРµСЂСЃРёСЏ (РІ РІС‹Р±СЂР°РЅРЅСѓСЋ РІР°РјРё РїР°РїРєСѓ, Р±РµР· СЂРµРµСЃС‚СЂР°)",
                Checked = false,
                Location = new Point(10, 50),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.White
            };
            rbPortable.CheckedChanged += Mode_CheckedChanged;
            bodyPanel.Controls.Add(rbPortable);

            var lblPath = new Label
            {
                Text = "РџР°РїРєР° РЅР°Р·РЅР°С‡РµРЅРёСЏ:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(0, 82),
                AutoSize = true
            };
            bodyPanel.Controls.Add(lblPath);

            txtInstallPath = new TextBox
            {
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppFolderName),
                Location = new Point(5, 105),
                Size = new Size(460, 26),
                BackColor = Color.FromArgb(17, 24, 39),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            bodyPanel.Controls.Add(txtInstallPath);

            btnBrowse = new Button
            {
                Text = "РћР±Р·РѕСЂ...",
                Location = new Point(475, 104),
                Size = new Size(95, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(14, 165, 233),
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(14, 165, 233);
            btnBrowse.Click += BtnBrowse_Click;
            bodyPanel.Controls.Add(btnBrowse);

            var lblOptions = new Label
            {
                Text = "Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅС‹Рµ РїР°СЂР°РјРµС‚СЂС‹ Р±РµР·РѕРїР°СЃРЅРѕСЃС‚Рё Рё РёРЅС‚РµРіСЂР°С†РёРё:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(0, 142),
                AutoSize = true
            };
            bodyPanel.Controls.Add(lblOptions);

            chkDesktop = new CheckBox
            {
                Text = "РЎРѕР·РґР°С‚СЊ СЏСЂР»С‹Рє РЅР° Р Р°Р±РѕС‡РµРј СЃС‚РѕР»Рµ",
                Checked = true,
                Location = new Point(10, 166),
                AutoSize = true,
                ForeColor = Color.White
            };
            bodyPanel.Controls.Add(chkDesktop);

            chkStartMenu = new CheckBox
            {
                Text = "РЎРѕР·РґР°С‚СЊ СЏСЂР»С‹Рє РІ РјРµРЅСЋ В«РџСѓСЃРєВ»",
                Checked = true,
                Location = new Point(10, 191),
                AutoSize = true,
                ForeColor = Color.White
            };
            bodyPanel.Controls.Add(chkStartMenu);

            chkInstallCert = new CheckBox
            {
                Text = "Р—Р°СЂРµРіРёСЃС‚СЂРёСЂРѕРІР°С‚СЊ СЃРµСЂС‚РёС„РёРєР°С‚ STORM TEAM (Р·Р°С‰РёС‚Р° РѕС‚ SmartScreen / SAC)",
                Checked = true,
                Location = new Point(10, 216),
                AutoSize = true,
                ForeColor = Color.FromArgb(52, 211, 153)
            };
            bodyPanel.Controls.Add(chkInstallCert);

            chkRegister = new CheckBox
            {
                Text = "Р—Р°СЂРµРіРёСЃС‚СЂРёСЂРѕРІР°С‚СЊ РІ СЃРїРёСЃРєРµ В«РЈСЃС‚Р°РЅРѕРІРєР° Рё СѓРґР°Р»РµРЅРёРµ РїСЂРѕРіСЂР°РјРјВ»",
                Checked = true,
                Location = new Point(10, 241),
                AutoSize = true,
                ForeColor = Color.White
            };
            bodyPanel.Controls.Add(chkRegister);

            chkRunAfter = new CheckBox
            {
                Text = $"Р—Р°РїСѓСЃС‚РёС‚СЊ {AppDisplayName} СЃСЂР°Р·Сѓ РїРѕСЃР»Рµ СѓСЃС‚Р°РЅРѕРІРєРё",
                Checked = true,
                Location = new Point(10, 266),
                AutoSize = true,
                ForeColor = Color.FromArgb(14, 165, 233)
            };
            bodyPanel.Controls.Add(chkRunAfter);

            progressBar = new ProgressBar
            {
                Location = new Point(5, 296),
                Size = new Size(565, 12),
                Style = ProgressBarStyle.Continuous,
                Value = 0,
                Visible = false
            };
            bodyPanel.Controls.Add(progressBar);

            lblStatus = new Label
            {
                Text = "",
                Location = new Point(5, 312),
                Size = new Size(565, 20),
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Visible = false
            };
            bodyPanel.Controls.Add(lblStatus);

            this.Controls.Add(bodyPanel);

            // 3. Bottom Panel
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(17, 24, 39),
                Padding = new Padding(24, 12, 24, 12)
            };

            btnCancel = new Button
            {
                Text = "РћС‚РјРµРЅР°",
                Size = new Size(110, 36),
                Location = new Point(365, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(226, 232, 240),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnCancel.Click += (s, e) => this.Close();
            bottomPanel.Controls.Add(btnCancel);

            btnInstall = new Button
            {
                Text = "рџ“¦  РЈСЃС‚Р°РЅРѕРІРёС‚СЊ",
                Size = new Size(135, 36),
                Location = new Point(485, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderColor = Color.FromArgb(56, 189, 248);
            btnInstall.Click += BtnInstall_Click;
            bottomPanel.Controls.Add(btnInstall);

            this.Controls.Add(bottomPanel);
        }

        private void Mode_CheckedChanged(object? sender, EventArgs e)
        {
            if (rbPortable.Checked)
            {
                txtInstallPath.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppFolderName}_Portable");
                chkDesktop.Checked = false;
                chkDesktop.Enabled = false;
                chkStartMenu.Checked = false;
                chkStartMenu.Enabled = false;
                chkRegister.Checked = false;
                chkRegister.Enabled = false;
                btnInstall.Text = "рџ“¦  Р Р°СЃРїР°РєРѕРІР°С‚СЊ";
            }
            else
            {
                txtInstallPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppFolderName);
                chkDesktop.Checked = true;
                chkDesktop.Enabled = true;
                chkStartMenu.Checked = true;
                chkStartMenu.Enabled = true;
                chkRegister.Checked = true;
                chkRegister.Enabled = true;
                btnInstall.Text = "рџ“¦  РЈСЃС‚Р°РЅРѕРІРёС‚СЊ";
            }
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            fbd.Description = $"Р’С‹Р±РµСЂРёС‚Рµ РїР°РїРєСѓ РґР»СЏ СѓСЃС‚Р°РЅРѕРІРєРё {AppDisplayName}:";
            fbd.UseDescriptionForTitle = true;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtInstallPath.Text = fbd.SelectedPath;
            }
        }

        private async void BtnInstall_Click(object? sender, EventArgs e)
        {
            progressBar.Visible = true;
            lblStatus.Visible = true;
            await StartInstallationAsync();
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
                    targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppFolderName);
                }

                Directory.CreateDirectory(targetDir);

                // Terminate any running instances
                lblStatus.Text = "Р—Р°РІРµСЂС€РµРЅРёРµ РїСЂРµРґС‹РґСѓС‰РёС… РїСЂРѕС†РµСЃСЃРѕРІ РїСЂРѕРіСЂР°РјРјС‹...";
                progressBar.Value = 10;
                await Task.Delay(150);

                string[] procNames = { "StormSystemOptimizer", "StormLauncher", "STORM_SYSTEM_OPTIMIZER" };
                foreach (var pName in procNames)
                {
                    foreach (var p in Process.GetProcessesByName(pName))
                    {
                        try { p.Kill(); p.WaitForExit(1500); } catch { }
                    }
                }

                string targetExe = Path.Combine(targetDir, ExeName);
                string targetLauncher = Path.Combine(targetDir, LauncherExeName);
                string targetCer = Path.Combine(targetDir, "STORM_Certificate.cer");
                string targetIco = Path.Combine(targetDir, IcoName);
                string targetLogo = Path.Combine(targetDir, "logo.png");

                if (chkInstallCert.Checked)
                {
                    lblStatus.Text = "Р РµРіРёСЃС‚СЂР°С†РёСЏ РґРѕРІРµСЂРµРЅРЅРѕРіРѕ СЃРµСЂС‚РёС„РёРєР°С‚Р° (Root & Publisher)...";
                    progressBar.Value = 25;
                    await Task.Delay(150);

                    ExtractResource("STORM_Certificate.cer", targetCer);
                    if (File.Exists(targetCer))
                    {
                        InstallCertificateSilently(targetCer);
                    }
                }

                lblStatus.Text = $"Р Р°СЃРїР°РєРѕРІРєР° РїР°РєРµС‚Р° {AppDisplayName} (v{AppVersion})...";
                progressBar.Value = 40;
                await Task.Delay(100);

                // Extract primary executables
                ExtractResource(ExeName, targetExe);
                ExtractResource(LauncherExeName, targetLauncher);
                ExtractResource(IcoName, targetIco);
                ExtractResource("logo.png", targetLogo);
                ExtractResource("STORM_Certificate.cer", targetCer);

                progressBar.Value = 75;
                lblStatus.Text = "РЎРЅСЏС‚РёРµ РјРµС‚РѕРє Р±Р»РѕРєРёСЂРѕРІРєРё Рё РѕРїС‚РёРјРёР·Р°С†РёСЏ Р±РµР·РѕРїР°СЃРЅРѕСЃС‚Рё...";
                await Task.Delay(150);

                UnblockFile(targetExe);
                UnblockFile(targetLauncher);
                UnblockFile(targetCer);
                UnblockFile(targetIco);
                UnblockFile(targetLogo);
                UnblockEntireDirectory(targetDir);

                if (rbStandard.Checked)
                {
                    lblStatus.Text = "РЎРѕР·РґР°РЅРёРµ СЃРёСЃС‚РµРјРЅС‹С… СЏСЂР»С‹РєРѕРІ Рё СЂРµРіРёСЃС‚СЂР°С†РёСЏ РІ Windows...";
                    progressBar.Value = 88;
                    await Task.Delay(150);

                    string runTarget = File.Exists(targetLauncher) ? targetLauncher : targetExe;
                    CreateShortcuts(targetDir, runTarget, targetIco, chkDesktop.Checked, chkStartMenu.Checked);

                    if (chkRegister.Checked)
                    {
                        RegisterUninstall(targetDir, runTarget, targetIco);
                    }
                }

                progressBar.Value = 100;
                lblStatus.Text = rbPortable.Checked ? "РџРѕСЂС‚Р°С‚РёРІРЅР°СЏ РІРµСЂСЃРёСЏ СѓСЃРїРµС€РЅРѕ СЂР°СЃРїР°РєРѕРІР°РЅР° Рё СЂР°Р·Р±Р»РѕРєРёСЂРѕРІР°РЅР°!" : "РЈСЃС‚Р°РЅРѕРІРєР° СѓСЃРїРµС€РЅРѕ Р·Р°РІРµСЂС€РµРЅР°! РЎРёСЃС‚РµРјР° РїРѕР»РЅРѕСЃС‚СЊСЋ РіРѕС‚РѕРІР°.";
                lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                await Task.Delay(500);

                if (chkRunAfter.Checked)
                {
                    TryLaunchApplication(targetLauncher, targetExe, targetDir);
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"РћС€РёР±РєР° РІРѕ РІСЂРµРјСЏ СѓСЃС‚Р°РЅРѕРІРєРё:\n{ex.Message}", "РћС€РёР±РєР° СѓСЃС‚Р°РЅРѕРІРєРё", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnInstall.Enabled = true;
                btnCancel.Enabled = true;
                btnBrowse.Enabled = true;
            }
        }

        private static void TryLaunchApplication(string launcherPath, string directExePath, string workingDir)
        {
            try
            {
                if (File.Exists(launcherPath))
                {
                    UnblockFile(launcherPath);
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = launcherPath,
                            WorkingDirectory = workingDir,
                            UseShellExecute = true
                        });
                        return;
                    }
                    catch { }
                }

                if (File.Exists(directExePath))
                {
                    UnblockFile(directExePath);
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = directExePath,
                            WorkingDirectory = workingDir,
                            UseShellExecute = true
                        });
                        return;
                    }
                    catch { }

                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{directExePath}\"",
                            UseShellExecute = false
                        });
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void UnblockFile(string path)
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

        public static void UnblockEntireDirectory(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return;
                foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                {
                    UnblockFile(file);
                }
            }
            catch { }
        }

        public static void InstallCertificateSilently(string cerPath)
        {
            try
            {
                if (!File.Exists(cerPath)) return;

                // 1. Direct certutil commands
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
                    p1?.WaitForExit(5000);

                    var psiPub = new ProcessStartInfo
                    {
                        FileName = "certutil.exe",
                        Arguments = $"-addstore -f \"TrustedPublisher\" \"{cerPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using var p2 = Process.Start(psiPub);
                    p2?.WaitForExit(5000);

                    var psiAuth = new ProcessStartInfo
                    {
                        FileName = "certutil.exe",
                        Arguments = $"-addstore -f \"AuthRoot\" \"{cerPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using var p3 = Process.Start(psiAuth);
                    p3?.WaitForExit(5000);
                }
                catch { }

                // 2. .NET X509Store
                try
                {
                    var cert = new X509Certificate2(cerPath);
                    foreach (var loc in new[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser })
                    {
                        foreach (var name in new[] { StoreName.Root, StoreName.TrustedPublisher, StoreName.AuthRoot, StoreName.CertificateAuthority })
                        {
                            try
                            {
                                using var store = new X509Store(name, loc);
                                store.Open(OpenFlags.ReadWrite);
                                store.Add(cert);
                                store.Close();
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                // 3. Relax SAC/SmartScreen block policies for installed app
                try
                {
                    using var keyCi = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\CI\Policy");
                    keyCi?.SetValue("VerifiedAndReputablePolicyState", 0, RegistryValueKind.DWord);
                    keyCi?.SetValue("SAC_PreviousState", 0, RegistryValueKind.DWord);

                    using var keySs = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer");
                    keySs?.SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);

                    using var keyDef = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\SmartScreen");
                    keyDef?.SetValue("ConfigureAppInstallControlEnabled", 0, RegistryValueKind.DWord);
                }
                catch { }
            }
            catch { }
        }

        private void ExtractResource(string resNameEnding, string targetPath)
        {
            try
            {
                string? dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
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
            catch { }
        }

        private void CreateShortcuts(string targetDir, string targetExe, string targetIco, bool desktopShortcut, bool startMenuShortcut)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null) return;

                if (startMenuShortcut)
                {
                    string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), $"{AppDisplayName}.lnk");
                    dynamic shortcut = shell.CreateShortcut(startMenu);
                    shortcut.TargetPath = targetExe;
                    shortcut.WorkingDirectory = targetDir;
                    shortcut.IconLocation = (File.Exists(targetIco) ? targetIco : targetExe) + ",0";
                    shortcut.Description = AppDisplayName;
                    shortcut.Save();
                }

                if (desktopShortcut)
                {
                    string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{AppDisplayName}.lnk");
                    dynamic deskShortcut = shell.CreateShortcut(desktop);
                    deskShortcut.TargetPath = targetExe;
                    deskShortcut.WorkingDirectory = targetDir;
                    deskShortcut.IconLocation = (File.Exists(targetIco) ? targetIco : targetExe) + ",0";
                    deskShortcut.Description = AppDisplayName;
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
                    key.SetValue("DisplayName", AppDisplayName);
                    key.SetValue("DisplayVersion", AppVersion);
                    key.SetValue("Publisher", "STORM TEAM");
                    key.SetValue("DisplayIcon", File.Exists(targetIco) ? targetIco : targetExe);
                    key.SetValue("InstallLocation", targetDir);
                    key.SetValue("UninstallString", $"cmd.exe /c rmdir /s /q \"{targetDir}\" & del \"%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\*STORM SYSTEM OPTIMIZER*.lnk\" & del \"%USERPROFILE%\\Desktop\\*STORM SYSTEM OPTIMIZER*.lnk\"");
                }
            }
            catch { }
        }

        [STAThread]
        public static void Main()
        {
            try
            {
                string selfExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(selfExe))
                {
                    UnblockFile(selfExe);
                }
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }
}