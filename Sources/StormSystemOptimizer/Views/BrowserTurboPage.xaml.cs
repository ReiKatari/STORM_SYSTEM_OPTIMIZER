using System.Windows.Controls;

namespace StormSystemOptimizer.Views
{
    public partial class BrowserTurboPage : Page
    {
        public BrowserTurboPage()
        {
            InitializeComponent();
        }

        private void OpenExtensions_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mw)
            {
                mw.NavigateToTag("BrowserExtensions");
            }
        }
    }
}
