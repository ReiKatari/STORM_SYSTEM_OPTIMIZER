using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public sealed partial class ServicesPage : Page
    {
        public ServicesViewModel ViewModel { get; } = new();

        public ServicesPage()
        {
            this.InitializeComponent();
        }

        private void BtnPresetBalanced_Click(object sender, RoutedEventArgs e) => ViewModel.ApplyPresetCommand.Execute("Balanced");
        private void BtnPresetGaming_Click(object sender, RoutedEventArgs e) => ViewModel.ApplyPresetCommand.Execute("Gaming");
        private void BtnPresetExtreme_Click(object sender, RoutedEventArgs e) => ViewModel.ApplyPresetCommand.Execute("Extreme");
        private void BtnPresetDefault_Click(object sender, RoutedEventArgs e) => ViewModel.ApplyPresetCommand.Execute("Default");

        private void BtnToggleService_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ServiceEntry service)
            {
                ViewModel.ToggleService(service);
            }
        }
    }
}
