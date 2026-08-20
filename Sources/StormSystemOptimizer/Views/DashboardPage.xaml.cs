using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.Services;

namespace StormSystemOptimizer.Views
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
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
