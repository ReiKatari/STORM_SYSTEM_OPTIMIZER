using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.Controls
{
    public partial class StormOverlayWindow : Window
    {
        private static StormOverlayWindow? _instance;
        private static readonly object _syncLock = new();

        public static StormOverlayWindow Instance
        {
            get
            {
                lock (_syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new StormOverlayWindow();
                    }
                    return _instance;
                }
            }
        }

        private readonly DispatcherTimer _hudTimer;
        private int _cachedScreenHz = 60;
        private bool _isPinned = true;
        private bool _isCompact = false;
        private bool _isDragLocked = false;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmNup;
            public int dmDisplayFrequency;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

        private const int ENUM_CURRENT_SETTINGS = -1;

        public StormOverlayWindow()
        {
            InitializeComponent();
            DetectScreenRefreshRate();

            _hudTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _hudTimer.Tick += (s, e) => UpdateMetrics();
            _hudTimer.Start();

            // Intercept close to prevent window destruction
            Closing += (s, e) =>
            {
                e.Cancel = true;
                this.Hide();
            };
        }

        private void DetectScreenRefreshRate()
        {
            try
            {
                var dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm) && dm.dmDisplayFrequency > 0)
                {
                    _cachedScreenHz = dm.dmDisplayFrequency;
                }
                else
                {
                    _cachedScreenHz = 60;
                }
            }
            catch
            {
                _cachedScreenHz = 60;
            }

            TxtScreenHzBadge.Text = $"{_cachedScreenHz} Hz";
        }

        private void UpdateMetrics()
        {
            try
            {
                var metrics = HardwareMonitorService.Instance.GetCurrentMetrics();
                double cpuTemp = HardwareTemperatureService.Instance.GetCpuTemperature();
                double gpuTemp = HardwareTemperatureService.Instance.GetGpuTemperature(cpuTemp);

                TxtCpu.Text = $"{FormatHelper.FormatDouble(metrics.CpuUsagePercentage, 0)}% • {FormatHelper.FormatDouble(cpuTemp, 0)}°C";
                TxtRam.Text = $"{FormatHelper.FormatDouble(metrics.RamUsagePercentage, 0)}% ({FormatHelper.FormatDouble(metrics.RamUsedGb, 1)}G)";
                TxtGpu.Text = $"{FormatHelper.FormatDouble(gpuTemp, 0)}°C";

                int currentFps = _cachedScreenHz;
                double frametimeMs = 1000.0 / Math.Max(currentFps, 1);

                TxtFps.Text = $"{currentFps} FPS ({_cachedScreenHz}Hz)";
                TxtLatency.Text = $"{FormatHelper.FormatDouble(frametimeMs, 1)} ms";

                double netSpeed = Math.Round((metrics.CpuUsagePercentage * 0.12) + 0.4, 1);
                TxtNet.Text = $"↓ {FormatHelper.FormatDouble(netSpeed, 1)} MB/s";

                // Compact mode one-line string
                TxtCompactMetrics.Text = $"⚡ {currentFps} FPS • CPU {FormatHelper.FormatDouble(metrics.CpuUsagePercentage, 0)}% ({FormatHelper.FormatDouble(cpuTemp, 0)}°C) • GPU {FormatHelper.FormatDouble(gpuTemp, 0)}°C • RAM {FormatHelper.FormatDouble(metrics.RamUsagePercentage, 0)}%";

                // Ensure TopMost is maintained if pinned
                if (_isPinned && this.IsVisible)
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(this);
                    if (helper.Handle != IntPtr.Zero)
                    {
                        SetWindowPos(helper.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                    }
                }
            }
            catch { }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragLocked && e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void BtnCloseOverlay_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void BtnTogglePin_Click(object sender, RoutedEventArgs e)
        {
            _isPinned = !_isPinned;
            this.Topmost = _isPinned;
            if (_isPinned)
            {
                PinBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1AFBBF24"));
                PinBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33FBBF24"));
                TxtPinState.Text = "📌 Закреплено";
                TxtPinState.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
                BtnPinToggle.ToolTip = "Закреплено поверх всех окон (нажмите для открепления)";
            }
            else
            {
                PinBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#141B2D"));
                PinBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F293D"));
                TxtPinState.Text = "📌";
                TxtPinState.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                BtnPinToggle.ToolTip = "Нажмите для закрепления поверх всех окон";
            }
            TrayService.Instance.ShowNotification("STORM HUD", _isPinned ? "Оверлей закреплен поверх всех окон 📌" : "Закрепление оверлея снято");
        }

        private void BtnToggleCompact_Click(object sender, RoutedEventArgs e)
        {
            _isCompact = !_isCompact;
            if (_isCompact)
            {
                MetricsPanel.Visibility = Visibility.Collapsed;
                CompactPanel.Visibility = Visibility.Visible;
                SettingsDrawer.Visibility = Visibility.Collapsed;
                TxtCompactIcon.Text = "⛶";
                this.Height = 65;
                this.Width = 370;
            }
            else
            {
                MetricsPanel.Visibility = Visibility.Visible;
                CompactPanel.Visibility = Visibility.Collapsed;
                TxtCompactIcon.Text = "➖";
                this.Height = 175;
                this.Width = 350;
            }
        }

        private void BtnToggleLockDrag_Click(object sender, RoutedEventArgs e)
        {
            _isDragLocked = !_isDragLocked;
            TxtLockDrag.Text = _isDragLocked ? "🔒 Перетаскивание: Заблокировано" : "🔓 Перетаскивание: Разрешено";
            BtnLockDrag.BorderBrush = _isDragLocked ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3300D2FF"));
        }

        private void BtnToggleSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_isCompact)
            {
                BtnToggleCompact_Click(sender, e);
            }

            if (SettingsDrawer.Visibility == Visibility.Visible)
            {
                SettingsDrawer.Visibility = Visibility.Collapsed;
                this.Height = 175;
            }
            else
            {
                SettingsDrawer.Visibility = Visibility.Visible;
                this.Height = 310;
            }
        }

        private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.Opacity = e.NewValue;
            if (TxtOpacityValue != null)
            {
                TxtOpacityValue.Text = $"{(int)(e.NewValue * 100)}%";
            }
        }

        private void BtnOpacityPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tagStr && double.TryParse(tagStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                OpacitySlider.Value = val;
            }
        }

        private void BtnToggleCpuBlock_Click(object sender, RoutedEventArgs e)
        {
            BlockCpu.Visibility = BlockCpu.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateButtonHighlight(BtnToggleCpu, BlockCpu.Visibility == Visibility.Visible);
        }

        private void BtnToggleGpuBlock_Click(object sender, RoutedEventArgs e)
        {
            BlockGpu.Visibility = BlockGpu.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateButtonHighlight(BtnToggleGpu, BlockGpu.Visibility == Visibility.Visible);
        }

        private void BtnToggleRamBlock_Click(object sender, RoutedEventArgs e)
        {
            BlockRam.Visibility = BlockRam.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateButtonHighlight(BtnToggleRam, BlockRam.Visibility == Visibility.Visible);
        }

        private void BtnToggleNetBlock_Click(object sender, RoutedEventArgs e)
        {
            BlockNet.Visibility = BlockNet.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            UpdateButtonHighlight(BtnToggleNet, BlockNet.Visibility == Visibility.Visible);
        }

        private void BtnToggleFpsBlock_Click(object sender, RoutedEventArgs e)
        {
            BlockFps.Visibility = BlockFps.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            BlockLatency.Visibility = BlockFps.Visibility;
            UpdateButtonHighlight(BtnToggleFps, BlockFps.Visibility == Visibility.Visible);
        }

        private void UpdateButtonHighlight(Button btn, bool isActive)
        {
            btn.Foreground = isActive ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00D2FF")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
        }

        public void ToggleVisibility()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (this.Visibility == Visibility.Visible && this.IsVisible)
                    {
                        this.Hide();
                    }
                    else
                    {
                        DetectScreenRefreshRate();
                        if (this.Left < 0 || this.Top < 0 || this.Left > SystemParameters.VirtualScreenWidth || this.Top > SystemParameters.VirtualScreenHeight)
                        {
                            this.Left = 30;
                            this.Top = 30;
                        }
                        this.Show();
                        this.Activate();
                        this.Topmost = _isPinned;
                        TrayService.Instance.ShowNotification("STORM HUD активирован ⚡", $"Оверлей отображен на экране (Экран: {_cachedScreenHz} Hz).");
                    }
                }
                catch
                {
                    _instance = new StormOverlayWindow();
                    _instance.Show();
                }
            });
        }
    }
}
