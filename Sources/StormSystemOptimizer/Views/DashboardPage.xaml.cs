using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StormSystemOptimizer.Services;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; } = new();

        public DashboardPage()
        {
            this.InitializeComponent();
        }

        private void BtnOpenScanner_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ScannerPage));
        }

        private void BtnDnsFlush_Click(object sender, RoutedEventArgs e)
        {
            NetworkOptimizerService.Instance.FlushDnsCache();
            TrayService.Instance.ShowNotification("DNS очищен", "Системный кэш сопоставителя DNS успешно сброшен.");
        }

        private void BtnTrimSsd_Click(object sender, RoutedEventArgs e)
        {
            _ = SystemToolsService.Instance.RunSsdTrimAsync("C:");
            TrayService.Instance.ShowNotification("SSD TRIM", "Команда оптимизации SSD диска C: запущена.");
        }

        private void BtnPowerPlan_Click(object sender, RoutedEventArgs e)
        {
            SystemToolsService.Instance.ActivateUltimatePerformancePlan();
            TrayService.Instance.ShowNotification("Электропитание", "Активирован план «Максимальная производительность».");
        }
    }
}
