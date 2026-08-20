using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class ProcessesPage : Page
    {
        public ProcessesPage()
        {
            InitializeComponent();
        }

        private void HighlightActiveFilterButton(Button activeButton)
        {
            var defaultBg = (Brush)FindResource("CardBackgroundBrush");
            var defaultBorder = (Brush)FindResource("CardBorderBrush");
            var defaultFg = (Brush)FindResource("TextPrimaryBrush");

            var activeBg = (Brush)FindResource("AccentGlowBrush");
            var activeBorder = (Brush)FindResource("AccentPrimaryBrush");
            var activeFg = (Brush)FindResource("AccentPrimaryBrush");

            BtnFilterAll.Background = defaultBg;
            BtnFilterAll.BorderBrush = defaultBorder;
            BtnFilterAll.Foreground = defaultFg;
            BtnFilterAll.FontWeight = FontWeights.SemiBold;

            BtnFilterSafe.Background = defaultBg;
            BtnFilterSafe.BorderBrush = defaultBorder;
            BtnFilterSafe.Foreground = defaultFg;
            BtnFilterSafe.FontWeight = FontWeights.SemiBold;

            BtnFilterUser.Background = defaultBg;
            BtnFilterUser.BorderBrush = defaultBorder;
            BtnFilterUser.Foreground = defaultFg;
            BtnFilterUser.FontWeight = FontWeights.SemiBold;

            BtnFilterSystem.Background = defaultBg;
            BtnFilterSystem.BorderBrush = defaultBorder;
            BtnFilterSystem.Foreground = defaultFg;
            BtnFilterSystem.FontWeight = FontWeights.SemiBold;

            activeButton.Background = activeBg;
            activeButton.BorderBrush = activeBorder;
            activeButton.Foreground = activeFg;
            activeButton.FontWeight = FontWeights.Bold;
        }

        private void BtnFilterAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProcessesViewModel vm) vm.SetFilter("Все процессы");
            HighlightActiveFilterButton(BtnFilterAll);
        }

        private void BtnFilterSafe_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProcessesViewModel vm) vm.SetFilter("Безопасно завершить");
            HighlightActiveFilterButton(BtnFilterSafe);
        }

        private void BtnFilterUser_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProcessesViewModel vm) vm.SetFilter("Пользовательские");
            HighlightActiveFilterButton(BtnFilterUser);
        }

        private void BtnFilterSystem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProcessesViewModel vm) vm.SetFilter("Системные Windows");
            HighlightActiveFilterButton(BtnFilterSystem);
        }

        private async void BtnEndProcess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ProcessInfoItem item)
            {
                bool ok = ProcessManagerService.Instance.TerminateProcess(item.ProcessId);
                if (ok && DataContext is ProcessesViewModel vm)
                {
                    TrayService.Instance.ShowNotification("Процесс завершен", $"Процесс {item.ProcessName} (PID: {item.ProcessId}) завершен.");
                    await vm.RefreshProcessesAsync();
                }
            }
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ProcessInfoItem item && !string.IsNullOrEmpty(item.ExecutablePath))
            {
                ProcessManagerService.Instance.OpenProcessLocation(item.ExecutablePath);
            }
        }
    }
}
