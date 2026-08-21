using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class ServicesPage : Page
    {
        public ServicesPage()
        {
            InitializeComponent();
        }

        private void ChkService_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk && chk.DataContext is ServiceEntry entry && DataContext is ServicesViewModel vm)
            {
                vm.ToggleServiceCommand.Execute(entry);
            }
        }
    }
}
