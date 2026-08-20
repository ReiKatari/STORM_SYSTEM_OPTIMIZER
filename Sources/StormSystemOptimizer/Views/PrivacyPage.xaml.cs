using System;
using Microsoft.UI.Xaml.Controls;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public sealed partial class PrivacyPage : Page
    {
        public PrivacyViewModel ViewModel { get; } = new();

        public PrivacyPage()
        {
            this.InitializeComponent();
        }
    }
}
