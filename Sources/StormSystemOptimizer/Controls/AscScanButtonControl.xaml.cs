using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace StormSystemOptimizer.Controls
{
    public partial class AscScanButtonControl : UserControl
    {
        public static readonly DependencyProperty IsScanningProperty =
            DependencyProperty.Register(nameof(IsScanning), typeof(bool), typeof(AscScanButtonControl),
                new PropertyMetadata(false, OnScanningChanged));

        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register(nameof(Progress), typeof(int), typeof(AscScanButtonControl),
                new PropertyMetadata(0, OnProgressChanged));

        public static readonly DependencyProperty StatusStepTextProperty =
            DependencyProperty.Register(nameof(StatusStepText), typeof(string), typeof(AscScanButtonControl),
                new PropertyMetadata("Анализ...", OnStatusStepChanged));

        public static readonly DependencyProperty IssuesCountProperty =
            DependencyProperty.Register(nameof(IssuesCount), typeof(int), typeof(AscScanButtonControl),
                new PropertyMetadata(0, OnIssuesCountChanged));

        public static readonly DependencyProperty ScanCommandProperty =
            DependencyProperty.Register(nameof(ScanCommand), typeof(ICommand), typeof(AscScanButtonControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty FixCommandProperty =
            DependencyProperty.Register(nameof(FixCommand), typeof(ICommand), typeof(AscScanButtonControl),
                new PropertyMetadata(null));

        public bool IsScanning
        {
            get => (bool)GetValue(IsScanningProperty);
            set => SetValue(IsScanningProperty, value);
        }

        public int Progress
        {
            get => (int)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public string StatusStepText
        {
            get => (string)GetValue(StatusStepTextProperty);
            set => SetValue(StatusStepTextProperty, value);
        }

        public int IssuesCount
        {
            get => (int)GetValue(IssuesCountProperty);
            set => SetValue(IssuesCountProperty, value);
        }

        public ICommand? ScanCommand
        {
            get => (ICommand?)GetValue(ScanCommandProperty);
            set => SetValue(ScanCommandProperty, value);
        }

        public ICommand? FixCommand
        {
            get => (ICommand?)GetValue(FixCommandProperty);
            set => SetValue(FixCommandProperty, value);
        }

        private Storyboard? _breathingStoryboard;
        private Storyboard? _rotateStoryboard;

        public AscScanButtonControl()
        {
            InitializeComponent();
            Loaded += AscScanButtonControl_Loaded;
        }

        private void AscScanButtonControl_Loaded(object sender, RoutedEventArgs e)
        {
            _breathingStoryboard = Resources["BreathingGlowStoryboard"] as Storyboard;
            _rotateStoryboard = Resources["RadarRotateStoryboard"] as Storyboard;

            _breathingStoryboard?.Begin(this, true);
            UpdateVisualState();
        }

        private static void OnScanningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AscScanButtonControl ctrl)
            {
                ctrl.UpdateVisualState();
            }
        }

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AscScanButtonControl ctrl)
            {
                ctrl.TxtScanPercent.Text = $"{e.NewValue}%";
            }
        }

        private static void OnStatusStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AscScanButtonControl ctrl && e.NewValue is string s)
            {
                ctrl.TxtScanStep.Text = s;
            }
        }

        private static void OnIssuesCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AscScanButtonControl ctrl)
            {
                int count = (int)e.NewValue;
                ctrl.TxtIssuesFoundBadge.Text = $"{count} {GetIssuesDeclension(count)}";
                ctrl.UpdateVisualState();
            }
        }

        private void UpdateVisualState()
        {
            if (IsScanning)
            {
                StateIdle.Visibility = Visibility.Collapsed;
                StateScanning.Visibility = Visibility.Visible;
                StateCompleted.Visibility = Visibility.Collapsed;
                RadarSweepContainer.Visibility = Visibility.Visible;

                _rotateStoryboard?.Begin(this, true);
            }
            else if (IssuesCount > 0)
            {
                StateIdle.Visibility = Visibility.Collapsed;
                StateScanning.Visibility = Visibility.Collapsed;
                StateCompleted.Visibility = Visibility.Visible;
                RadarSweepContainer.Visibility = Visibility.Collapsed;

                _rotateStoryboard?.Stop(this);
            }
            else
            {
                StateIdle.Visibility = Visibility.Visible;
                StateScanning.Visibility = Visibility.Collapsed;
                StateCompleted.Visibility = Visibility.Collapsed;
                RadarSweepContainer.Visibility = Visibility.Collapsed;

                _rotateStoryboard?.Stop(this);
            }
        }

        private void BtnAscScan_Click(object sender, RoutedEventArgs e)
        {
            if (IsScanning) return;

            if (IssuesCount > 0 && FixCommand != null && FixCommand.CanExecute(null))
            {
                FixCommand.Execute(null);
            }
            else if (ScanCommand != null && ScanCommand.CanExecute(null))
            {
                ScanCommand.Execute(null);
            }
        }

        private static string GetIssuesDeclension(int number)
        {
            int n = Math.Abs(number) % 100;
            int n1 = n % 10;
            if (n > 10 && n < 20) return "проблем";
            if (n1 > 1 && n1 < 5) return "проблемы";
            if (n1 == 1) return "проблема";
            return "проблем";
        }
    }
}
