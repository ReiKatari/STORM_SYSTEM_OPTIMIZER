using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public sealed partial class StartupPage : Page
    {
        public StartupViewModel ViewModel { get; } = new();

        public StartupPage()
        {
            this.InitializeComponent();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadStartupApps();
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch ts && ts.DataContext is StartupEntry entry)
            {
                ViewModel.ToggleEntry(entry);
            }
        }
    }
}
