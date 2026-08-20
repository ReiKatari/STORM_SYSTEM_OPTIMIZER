using System;
using Microsoft.UI.Xaml.Controls;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public sealed partial class NetworkPage : Page
    {
        public NetworkViewModel ViewModel { get; } = new();

        public NetworkPage()
        {
            this.InitializeComponent();
        }
    }
}
