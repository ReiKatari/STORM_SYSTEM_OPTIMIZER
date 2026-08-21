using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class SoftwareUpdaterPage : Page
    {
        public SoftwareUpdaterPage()
        {
            InitializeComponent();
        }

        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SoftwareUpdaterViewModel vm) vm.SetFilter("Все");
        }

        private void FilterUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SoftwareUpdaterViewModel vm) vm.SetFilter("Updates");
        }

        private void FilterActual_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SoftwareUpdaterViewModel vm) vm.SetFilter("Actual");
        }

        private void FilterBlacklist_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SoftwareUpdaterViewModel vm) vm.SetFilter("Blacklist");
        }
    }
}
