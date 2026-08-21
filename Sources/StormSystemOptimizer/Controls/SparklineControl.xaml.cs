using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StormSystemOptimizer.Controls
{
    public partial class SparklineControl : UserControl
    {
        public static readonly DependencyProperty StrokeBrushProperty =
            DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(SparklineControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 0, 210, 255))));

        public static readonly DependencyProperty FillBrushProperty =
            DependencyProperty.Register(nameof(FillBrush), typeof(Brush), typeof(SparklineControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(50, 0, 210, 255))));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(SparklineControl),
                new PropertyMetadata(1.8, OnPropertyChanged));

        public static readonly DependencyProperty DataPointsProperty =
            DependencyProperty.Register(nameof(DataPoints), typeof(IEnumerable<double>), typeof(SparklineControl),
                new PropertyMetadata(null, OnDataPointsChanged));

        public Brush StrokeBrush
        {
            get => (Brush)GetValue(StrokeBrushProperty);
            set => SetValue(StrokeBrushProperty, value);
        }

        public Brush FillBrush
        {
            get => (Brush)GetValue(FillBrushProperty);
            set => SetValue(FillBrushProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public IEnumerable<double> DataPoints
        {
            get => (IEnumerable<double>)GetValue(DataPointsProperty);
            set => SetValue(DataPointsProperty, value);
        }

        public SparklineControl()
        {
            InitializeComponent();
            SizeChanged += (s, e) => Redraw();
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SparklineControl control) control.Redraw();
        }

        private static void OnDataPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SparklineControl control)
            {
                if (e.OldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= control.OnCollectionChanged;
                }
                if (e.NewValue is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += control.OnCollectionChanged;
                }
                control.Redraw();
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Redraw();
        }

        public void Redraw()
        {
            if (LinePath == null || AreaPath == null) return;
            if (ActualWidth <= 0 || ActualHeight <= 0) return;

            var points = DataPoints?.ToList();
            if (points == null || points.Count < 2)
            {
                LinePath.Data = null;
                AreaPath.Data = null;
                return;
            }

            double w = ActualWidth;
            double h = ActualHeight;
            double padding = 2.0;
            double maxVal = 100.0;
            double minVal = 0.0;

            double stepX = (w - (padding * 2)) / (points.Count - 1);

            var screenPoints = new List<Point>();
            for (int i = 0; i < points.Count; i++)
            {
                double val = Math.Clamp(points[i], minVal, maxVal);
                double normY = (val - minVal) / Math.Max(1.0, maxVal - minVal);
                double x = padding + (i * stepX);
                double y = (h - padding) - (normY * (h - (padding * 2)));
                screenPoints.Add(new Point(x, y));
            }

            // Build Bezier Curve
            var lineGeometry = new PathGeometry();
            var areaGeometry = new PathGeometry();

            var lineFigure = new PathFigure
            {
                StartPoint = screenPoints[0],
                IsClosed = false,
                IsFilled = false
            };

            var areaFigure = new PathFigure
            {
                StartPoint = new Point(screenPoints[0].X, h),
                IsClosed = true,
                IsFilled = true
            };
            areaFigure.Segments.Add(new LineSegment(screenPoints[0], true));

            for (int i = 0; i < screenPoints.Count - 1; i++)
            {
                Point p0 = i > 0 ? screenPoints[i - 1] : screenPoints[i];
                Point p1 = screenPoints[i];
                Point p2 = screenPoints[i + 1];
                Point p3 = i < screenPoints.Count - 2 ? screenPoints[i + 2] : p2;

                Point cp1 = new Point(p1.X + (p2.X - p0.X) / 6.0, p1.Y + (p2.Y - p0.Y) / 6.0);
                Point cp2 = new Point(p2.X - (p3.X - p1.X) / 6.0, p2.Y - (p3.Y - p1.Y) / 6.0);

                var segment = new BezierSegment(cp1, cp2, p2, true);
                lineFigure.Segments.Add(segment);
                areaFigure.Segments.Add(segment);
            }

            areaFigure.Segments.Add(new LineSegment(new Point(screenPoints.Last().X, h), true));

            lineGeometry.Figures.Add(lineFigure);
            areaGeometry.Figures.Add(areaFigure);

            LinePath.Data = lineGeometry;
            AreaPath.Data = areaGeometry;
        }
    }
}
