using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StormSystemOptimizer.ViewModels;

namespace StormSystemOptimizer.Views
{
    public partial class FileUnlockerPage : Page
    {
        public FileUnlockerPage()
        {
            InitializeComponent();
        }

        private void DropZone_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                DropZone.Background = (Brush)FindResource("CardHoverBrush");
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DropZone_DragLeave(object sender, DragEventArgs e)
        {
            DropZone.Background = (Brush)FindResource("AppBackgroundBrush");
            e.Handled = true;
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            DropZone.Background = (Brush)FindResource("AppBackgroundBrush");
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    ViewModel.TargetPath = files[0];
                    _ = ViewModel.AnalyzeTargetAsync();
                }
            }
            e.Handled = true;
        }
    }
}
