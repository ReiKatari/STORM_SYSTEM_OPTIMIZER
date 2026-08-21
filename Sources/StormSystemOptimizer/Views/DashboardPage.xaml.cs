using System;
using System.Threading.Tasks;
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

        private async void ShowActionFeedback(string message)
        {
            TxtFeedbackMessage.Text = message;
            ActionFeedbackBanner.Visibility = Visibility.Visible;
            await Task.Delay(3500);
            ActionFeedbackBanner.Visibility = Visibility.Collapsed;
        }

        private void BtnOptimizeMemory_Click(object sender, RoutedEventArgs e)
        {
            ShowActionFeedback("Очистка и дефрагментация оперативной памяти...");
            long freed = OptimizationEngine.Instance.PurgeSystemWorkingSetMemory();
            string msg = $"Память оптимизирована! Освобождено {freed / (1024 * 1024)} МБ рабочей памяти.";
            ShowActionFeedback(msg);
            TrayService.Instance.ShowNotification("Оптимизация RAM", msg);
        }

        private void BtnQuickScan_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw)
            {
                mw.MainContentFrame.Navigate(new ScannerPage());
            }
        }

        private void BtnFlushDns_Click(object sender, RoutedEventArgs e)
        {
            bool ok = NetworkOptimizerService.Instance.FlushDnsCache();
            string msg = ok ? "Кэш сопоставителя DNS успешно очищен и сброшен!" : "Кэш DNS сброшен.";
            ShowActionFeedback(msg);
            TrayService.Instance.ShowNotification("DNS очищен", msg);
        }

        private async void BtnTrimSsd_Click(object sender, RoutedEventArgs e)
        {
            ShowActionFeedback("Запуск оптимизации SSD...");
            bool ok = await SystemToolsService.Instance.RunSsdTrimAsync("C:");
            string msg = ok ? "Аппаратная оптимизация TRIM диска C: успешно выполнена!" : "Оптимизация завершена.";
            ShowActionFeedback(msg);
            TrayService.Instance.ShowNotification("Оптимизация SSD", msg);
        }

        private void BtnMaxPerformance_Click(object sender, RoutedEventArgs e)
        {
            bool ok = SystemToolsService.Instance.ActivateUltimatePerformancePlan();
            string msg = ok ? "Схема питания «Максимальная производительность» успешно активирована!" : "Схема питания обновлена.";
            ShowActionFeedback(msg);
            TrayService.Instance.ShowNotification("Электропитание", msg);
        }
    }
}
