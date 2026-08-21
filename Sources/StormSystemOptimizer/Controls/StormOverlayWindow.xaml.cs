using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.Controls
{
    public partial class StormOverlayWindow : Window
    {
        private static StormOverlayWindow? _instance;
        public static StormOverlayWindow Instance => _instance ??= new StormOverlayWindow();

        private DispatcherTimer _hudTimer;

        public StormOverlayWindow()
        {
            InitializeComponent();

            _hudTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _hudTimer.Tick += (s, e) => UpdateMetrics();
            _hudTimer.Start();
        }

        private void UpdateMetrics()
        {
            try
            {
                var metrics = HardwareMonitorService.Instance.GetCurrentMetrics();
                double cpuTemp = HardwareTemperatureService.Instance.GetCpuTemperature();
                double gpuTemp = HardwareTemperatureService.Instance.GetGpuTemperature(cpuTemp);

                TxtCpu.Text = $"{metrics.CpuUsagePercentage:F0}% • {cpuTemp:F0}°C";
                TxtRam.Text = $"{metrics.RamUsagePercentage:F0}%";
                TxtGpu.Text = $"{gpuTemp:F0}°C";

                // Simulated active app FPS / Refresh rate
                TxtFps.Text = GameBoostService.Instance.IsGameBoostActive ? "165" : "144";
            }
            catch { }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        public void ToggleVisibility()
        {
            if (this.IsVisible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }
    }
}
