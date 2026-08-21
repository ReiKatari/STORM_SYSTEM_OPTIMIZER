using System.IO;
using System.Windows;
using System.Windows.Controls;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class FolderProtectionPage : Page
    {
        public FolderProtectionPage()
        {
            InitializeComponent();
        }

        private void Page_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string target = files[0];
                    if (Directory.Exists(target))
                    {
                        if (DataContext is FolderProtectionViewModel vm)
                        {
                            vm.SelectedFolderPath = target;
                        }
                    }
                }
            }
        }
    }
}
