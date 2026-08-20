using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StormSystemOptimizer.Controls
{
    public partial class StormMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public StormMessageBox()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }

        public static MessageBoxResult Show(string message, string title = "STORM SYSTEM OPTIMIZER", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            var msgBox = new StormMessageBox();
            msgBox.TxtTitle.Text = title;
            msgBox.TxtMessage.Text = message;

            // Set Icon
            msgBox.TxtIcon.Text = icon switch
            {
                MessageBoxImage.Information => "ℹ️",
                MessageBoxImage.Warning => "⚠️",
                MessageBoxImage.Question => "❓",
                MessageBoxImage.Error => "❌",
                _ => "⚡"
            };

            // Setup Buttons
            msgBox.PnlButtons.Children.Clear();

            switch (buttons)
            {
                case MessageBoxButton.OK:
                    msgBox.AddButton("OK", MessageBoxResult.OK, isPrimary: true);
                    break;
                case MessageBoxButton.OKCancel:
                    msgBox.AddButton("Отмена", MessageBoxResult.Cancel, isPrimary: false);
                    msgBox.AddButton("OK", MessageBoxResult.OK, isPrimary: true);
                    break;
                case MessageBoxButton.YesNo:
                    msgBox.AddButton("Нет", MessageBoxResult.No, isPrimary: false);
                    msgBox.AddButton("Да", MessageBoxResult.Yes, isPrimary: true);
                    break;
                case MessageBoxButton.YesNoCancel:
                    msgBox.AddButton("Отмена", MessageBoxResult.Cancel, isPrimary: false);
                    msgBox.AddButton("Нет", MessageBoxResult.No, isPrimary: false);
                    msgBox.AddButton("Да", MessageBoxResult.Yes, isPrimary: true);
                    break;
            }

            if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                msgBox.Owner = Application.Current.MainWindow;
            }

            msgBox.ShowDialog();
            return msgBox.Result;
        }

        private void AddButton(string text, MessageBoxResult result, bool isPrimary)
        {
            var btn = new Button
            {
                Content = text,
                MinWidth = 85,
                Height = 32,
                Margin = new Thickness(8, 0, 0, 0),
                Cursor = Cursors.Hand,
                FontWeight = isPrimary ? FontWeights.Bold : FontWeights.Normal,
                FontSize = 12
            };

            if (isPrimary)
            {
                btn.Background = (Brush)FindResource("AccentPrimaryBrush");
                btn.Foreground = (Brush)FindResource("AppBackgroundBrush");
            }
            else
            {
                btn.Background = (Brush)FindResource("CardBackgroundBrush");
                btn.Foreground = (Brush)FindResource("TextPrimaryBrush");
                btn.BorderBrush = (Brush)FindResource("CardBorderBrush");
                btn.BorderThickness = new Thickness(1);
            }

            var template = new ControlTemplate(typeof(Button));
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            factory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { Source = btn });
            factory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { Source = btn });
            factory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { Source = btn });
            factory.SetValue(Border.PaddingProperty, new Thickness(14, 6, 14, 6));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(contentPresenter);

            template.VisualTree = factory;
            btn.Template = template;

            btn.Click += (s, e) =>
            {
                Result = result;
                Close();
            };

            PnlButtons.Children.Add(btn);
        }
    }
}
