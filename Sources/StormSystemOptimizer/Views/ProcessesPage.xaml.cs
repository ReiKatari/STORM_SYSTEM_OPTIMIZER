using System.Windows;
using System.Windows.Controls;
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

        private void BtnFilterAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProcessesViewModel vm) vm.SetFilter("Все процессы");
        }

        private void BtnFilterSafe_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProcessesViewModel vm) vm.SetFilter("Безопасно завершить");
        }

        private void BtnFilterUser_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProcessesViewModel vm) vm.SetFilter("Пользовательские");
        }

        private void BtnFilterSystem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProcessesViewModel vm) vm.SetFilter("Системные Windows");
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
