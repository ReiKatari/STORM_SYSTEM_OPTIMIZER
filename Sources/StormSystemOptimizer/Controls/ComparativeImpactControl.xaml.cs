using System.Windows;
using System.Windows.Controls;

namespace StormSystemOptimizer.Controls
{
    public partial class ComparativeImpactControl : UserControl
    {
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(ComparativeImpactControl),
                new PropertyMetadata("⏱️", (d, e) => ((ComparativeImpactControl)d).TxtIcon.Text = (string)e.NewValue));

        public static readonly DependencyProperty MetricTitleProperty =
            DependencyProperty.Register(nameof(MetricTitle), typeof(string), typeof(ComparativeImpactControl),
                new PropertyMetadata("Время старта", (d, e) => ((ComparativeImpactControl)d).TxtMetricTitle.Text = (string)e.NewValue));

        public static readonly DependencyProperty MetricSubtitleProperty =
            DependencyProperty.Register(nameof(MetricSubtitle), typeof(string), typeof(ComparativeImpactControl),
                new PropertyMetadata("Оптимизация", (d, e) => ((ComparativeImpactControl)d).TxtMetricSub.Text = (string)e.NewValue));

        public static readonly DependencyProperty BeforeValueProperty =
            DependencyProperty.Register(nameof(BeforeValue), typeof(string), typeof(ComparativeImpactControl),
                new PropertyMetadata("42 сек", (d, e) => ((ComparativeImpactControl)d).TxtBefore.Text = (string)e.NewValue));

        public static readonly DependencyProperty AfterValueProperty =
            DependencyProperty.Register(nameof(AfterValue), typeof(string), typeof(ComparativeImpactControl),
                new PropertyMetadata("18 сек", (d, e) => ((ComparativeImpactControl)d).TxtAfter.Text = (string)e.NewValue));

        public static readonly DependencyProperty BadgeTextProperty =
            DependencyProperty.Register(nameof(BadgeText), typeof(string), typeof(ComparativeImpactControl),
                new PropertyMetadata("-57% быстрее", (d, e) => ((ComparativeImpactControl)d).TxtBadge.Text = (string)e.NewValue));

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public string MetricTitle
        {
            get => (string)GetValue(MetricTitleProperty);
            set => SetValue(MetricTitleProperty, value);
        }

        public string MetricSubtitle
        {
            get => (string)GetValue(MetricSubtitleProperty);
            set => SetValue(MetricSubtitleProperty, value);
        }

        public string BeforeValue
        {
            get => (string)GetValue(BeforeValueProperty);
            set => SetValue(BeforeValueProperty, value);
        }

        public string AfterValue
        {
            get => (string)GetValue(AfterValueProperty);
            set => SetValue(AfterValueProperty, value);
        }

        public string BadgeText
        {
            get => (string)GetValue(BadgeTextProperty);
            set => SetValue(BadgeTextProperty, value);
        }

        public ComparativeImpactControl()
        {
            InitializeComponent();
        }
    }
}
