using System;
using Microsoft.UI.Xaml.Controls;

namespace StormSystemOptimizer.Controls
{
    public sealed partial class StormDialog : ContentDialog
    {
        public StormDialog(string title, string message, string iconGlyph = "\uE7E8")
        {
            this.InitializeComponent();
            DlgTitle.Text = title;
            DlgMessage.Text = message;
            DlgIcon.Glyph = iconGlyph;
            this.XamlRoot = App.MainWindow?.Content.XamlRoot;
        }
    }
}
