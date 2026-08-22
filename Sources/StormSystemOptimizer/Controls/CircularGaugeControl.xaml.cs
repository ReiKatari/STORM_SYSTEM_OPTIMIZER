using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StormSystemOptimizer.Controls
{
    public partial class CircularGaugeControl : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(CircularGaugeControl),
                new PropertyMetadata(0.0, OnPropertyChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(CircularGaugeControl),
                new PropertyMetadata(100.0, OnPropertyChanged));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(nameof(Unit), typeof(string), typeof(CircularGaugeControl),
                new PropertyMetadata("%", OnPropertyChanged));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(CircularGaugeControl),
                new PropertyMetadata("LOAD", OnPropertyChanged));

        public static readonly DependencyProperty ArcBrushProperty =
            DependencyProperty.Register(nameof(ArcBrush), typeof(Brush), typeof(CircularGaugeControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 0, 210, 255))));

        public static readonly DependencyProperty TrackBrushProperty =
            DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(CircularGaugeControl),
                new PropertyMetadata(null, OnPropertyChanged));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(CircularGaugeControl),
                new PropertyMetadata(9.0, OnPropertyChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public Brush ArcBrush
        {
            get => (Brush)GetValue(ArcBrushProperty);
            set => SetValue(ArcBrushProperty, value);
        }

        public Brush TrackBrush
        {
            get => (Brush)GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        private const double StartAngle = 135.0; // bottom left
        private const double SweepAngle = 270.0; // sweeps around top to bottom right
        private const double CenterX = 70.0;
        private const double CenterY = 70.0;
        private const double Radius = 54.0;

        public CircularGaugeControl()
        {
            InitializeComponent();
            Loaded += (s, e) => Redraw();
            SizeChanged += (s, e) => Redraw();
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularGaugeControl control)
            {
                control.Redraw();
            }
        }

        private void Redraw()
        {
            if (TrackPath == null || ProgressPath == null) return;

            TxtValue.Text = $"{Math.Round(Value):F0}";
            TxtUnit.Text = Unit;
            TxtTitle.Text = Title;

            // 1. Draw Track Arc (Full sweep)
            TrackPath.Data = CreateArcGeometry(StartAngle, StartAngle + SweepAngle, Radius);

            // 2. Draw Progress Arc
            double pct = Maximum > 0 ? Math.Clamp(Value / Maximum, 0.0, 1.0) : 0.0;
            double currentSweep = SweepAngle * pct;

            if (currentSweep <= 0.5)
            {
                ProgressPath.Data = null;
                EndpointMarker.Visibility = Visibility.Collapsed;
            }
            else
            {
                double endAngle = StartAngle + currentSweep;
                ProgressPath.Data = CreateArcGeometry(StartAngle, endAngle, Radius);

                // Position endpoint marker dot
                double rad = (endAngle * Math.PI) / 180.0;
                double markerX = CenterX + (Radius * Math.Cos(rad)) - (EndpointMarker.Width / 2.0);
                double markerY = CenterY + (Radius * Math.Sin(rad)) - (EndpointMarker.Height / 2.0);

                Canvas.SetLeft(EndpointMarker, markerX);
                Canvas.SetTop(EndpointMarker, markerY);
                EndpointMarker.Visibility = Visibility.Visible;
            }
        }

        private static PathGeometry CreateArcGeometry(double startAngleDeg, double endAngleDeg, double radius)
        {
            double startRad = (startAngleDeg * Math.PI) / 180.0;
            double endRad = (endAngleDeg * Math.PI) / 180.0;

            Point startPoint = new Point(
                CenterX + (radius * Math.Cos(startRad)),
                CenterY + (radius * Math.Sin(startRad)));

            Point endPoint = new Point(
                CenterX + (radius * Math.Cos(endRad)),
                CenterY + (radius * Math.Sin(endRad)));

            bool isLargeArc = Math.Abs(endAngleDeg - startAngleDeg) > 180.0;

            var figure = new PathFigure
            {
                StartPoint = startPoint,
                IsClosed = false,
                IsFilled = false
            };

            var arcSegment = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = isLargeArc
            };

            figure.Segments.Add(arcSegment);

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }
    }
}
