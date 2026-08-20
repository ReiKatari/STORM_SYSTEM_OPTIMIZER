using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class ServicesPage : Page
    {
        public ServicesPage()
        {
            InitializeComponent();
        }

        private async void BtnProfileBalanced_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ServicesViewModel vm)
            {
                await vm.ApplyProfileCommand.ExecuteAsync("Balanced");
            }
        }

        private async void BtnProfileGaming_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ServicesViewModel vm)
            {
                await vm.ApplyProfileCommand.ExecuteAsync("Gaming");
            }
        }

        private async void BtnProfileDefault_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ServicesViewModel vm)
            {
                await vm.ApplyProfileCommand.ExecuteAsync("Default");
            }
        }
    }
}
