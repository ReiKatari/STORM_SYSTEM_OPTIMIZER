using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StormSystemOptimizer.Models;
using StormSystemOptimizer.Services;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class ServicesPage : Page
    {
        public ServicesPage()
        {
            InitializeComponent();
        }

        private void HighlightActiveProfileButton(Button activeButton)
        {
            var defaultBg = (Brush)FindResource("CardBackgroundBrush");
            var defaultBorder = (Brush)FindResource("CardBorderBrush");
            var defaultFg = (Brush)FindResource("TextPrimaryBrush");

            var activeBg = (Brush)FindResource("AccentGlowBrush");
            var activeBorder = (Brush)FindResource("AccentPrimaryBrush");
            var activeFg = (Brush)FindResource("AccentPrimaryBrush");

            BtnProfileBalanced.Background = defaultBg;
            BtnProfileBalanced.BorderBrush = defaultBorder;
            BtnProfileBalanced.Foreground = defaultFg;
            BtnProfileBalanced.FontWeight = FontWeights.SemiBold;

            BtnProfileGaming.Background = defaultBg;
            BtnProfileGaming.BorderBrush = defaultBorder;
            BtnProfileGaming.Foreground = defaultFg;
            BtnProfileGaming.FontWeight = FontWeights.SemiBold;

            BtnProfileExtreme.Background = defaultBg;
            BtnProfileExtreme.BorderBrush = defaultBorder;
            BtnProfileExtreme.Foreground = defaultFg;
            BtnProfileExtreme.FontWeight = FontWeights.SemiBold;

            BtnProfileDefault.Background = defaultBg;
            BtnProfileDefault.BorderBrush = defaultBorder;
            BtnProfileDefault.Foreground = defaultFg;
            BtnProfileDefault.FontWeight = FontWeights.SemiBold;

            activeButton.Background = activeBg;
            activeButton.BorderBrush = activeBorder;
            activeButton.Foreground = activeFg;
            activeButton.FontWeight = FontWeights.Bold;
        }

        private async void BtnProfileBalanced_Click(object sender, RoutedEventArgs e)
        {
            HighlightActiveProfileButton(BtnProfileBalanced);
            if (DataContext is ServicesViewModel vm)
            {
                await vm.ApplyProfileCommand.ExecuteAsync("Balanced");
            }
        }

        private async void BtnProfileGaming_Click(object sender, RoutedEventArgs e)
        {
            HighlightActiveProfileButton(BtnProfileGaming);
            if (DataContext is ServicesViewModel vm)
            {
                await vm.ApplyProfileCommand.ExecuteAsync("Gaming");
            }
        }

        private async void BtnProfileExtreme_Click(object sender, RoutedEventArgs e)
        {
            HighlightActiveProfileButton(BtnProfileExtreme);
            if (DataContext is ServicesViewModel vm)
            {
                await vm.ApplyProfileCommand.ExecuteAsync("Extreme");
            }
        }

        private async void BtnProfileDefault_Click(object sender, RoutedEventArgs e)
        {
            HighlightActiveProfileButton(BtnProfileDefault);
            if (DataContext is ServicesViewModel vm)
            {
                await vm.ApplyProfileCommand.ExecuteAsync("Default");
            }
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
