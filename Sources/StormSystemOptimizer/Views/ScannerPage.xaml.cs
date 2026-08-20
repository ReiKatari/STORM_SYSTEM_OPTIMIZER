using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public sealed partial class ScannerPage : Page
    {
        public ScannerViewModel ViewModel { get; } = new();

        public ScannerPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (ViewModel.AllIssues.Count == 0 && !ViewModel.IsScanning)
            {
                _ = ViewModel.StartScanAsync();
            }
        }
    }
}
