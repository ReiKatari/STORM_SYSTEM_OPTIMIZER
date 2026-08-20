using System;
using Microsoft.UI.Xaml.Controls;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public sealed partial class SystemToolsPage : Page
    {
        public SystemToolsViewModel ViewModel { get; } = new();

        public SystemToolsPage()
        {
            this.InitializeComponent();
        }
    }
}
