using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class UninstallerPage : Page
    {
        public UninstallerPage()
        {
            InitializeComponent();
        }

        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UninstallerViewModel vm) vm.SetCategory("Все");
        }

        private void FilterGames_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UninstallerViewModel vm) vm.SetCategory("Игры");
        }

        private void FilterApps_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UninstallerViewModel vm) vm.SetCategory("Программы");
        }

        private void FilterStore_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UninstallerViewModel vm) vm.SetCategory("Windows Store");
        }
    }
}
