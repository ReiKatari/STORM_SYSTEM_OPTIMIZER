using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class DriverUpdaterPage : Page
    {
        public DriverUpdaterPage()
        {
            InitializeComponent();
        }

        private void CatAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DriverUpdaterViewModel vm) vm.SetCategory("Все");
        }

        private void CatGpu_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DriverUpdaterViewModel vm) vm.SetCategory("Видеокарта");
        }

        private void CatCpu_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DriverUpdaterViewModel vm) vm.SetCategory("Процессор");
        }

        private void CatMobo_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DriverUpdaterViewModel vm) vm.SetCategory("Материнская плата");
        }

        private void CatNet_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DriverUpdaterViewModel vm) vm.SetCategory("Сеть");
        }

        private void CatAudio_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DriverUpdaterViewModel vm) vm.SetCategory("Звук");
        }

        private void CatDisk_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DriverUpdaterViewModel vm) vm.SetCategory("Накопители");
        }

        private void CatChipset_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DriverUpdaterViewModel vm) vm.SetCategory("Чипсет & USB");
        }
    }
}
