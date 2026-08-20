using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class SystemToolsPage : Page
    {
        public SystemToolsPage()
        {
            InitializeComponent();
        }

        private void BtnSnapin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tool && DataContext is SystemToolsViewModel vm)
            {
                vm.LaunchSnapin(tool);
            }
        }
    }
}
