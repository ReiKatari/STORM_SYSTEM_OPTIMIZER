using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class NetworkPage : Page
    {
        public NetworkPage()
        {
            InitializeComponent();
        }

        private async void BtnDnsCloudflare_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NetworkViewModel vm) await vm.SetDnsPresetCommand.ExecuteAsync("Cloudflare");
        }

        private async void BtnDnsGoogle_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NetworkViewModel vm) await vm.SetDnsPresetCommand.ExecuteAsync("Google");
        }

        private async void BtnDnsQuad9_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NetworkViewModel vm) await vm.SetDnsPresetCommand.ExecuteAsync("Quad9");
        }

        private async void BtnDnsAdGuard_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NetworkViewModel vm) await vm.SetDnsPresetCommand.ExecuteAsync("AdGuard");
        }

        private async void BtnDnsDhcp_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NetworkViewModel vm) await vm.SetDnsPresetCommand.ExecuteAsync("DHCP");
        }
    }
}
