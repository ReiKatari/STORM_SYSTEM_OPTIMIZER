using System.Windows.Controls;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class DisksPage : Page
    {
        public DisksPage()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is DisksViewModel vm)
                {
                    await vm.LoadDrivesAsync();
                }
            };
        }
    }
}
