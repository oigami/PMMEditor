using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PMMEditor.Views
{
    /// <summary>
    /// TimelinePanel.xaml の相互作用ロジック
    /// </summary>
    public class TimelinePanel : Panel
    {
        #region 添付プロパティ

        #region Index変更通知プロパティ

        public static int GetIndex(DependencyObject obj)
        {
            return (int) obj.GetValue(IndexProperty);
        }

        public static void SetIndex(DependencyObject obj, int val)
        {
            obj.SetValue(IndexProperty, val);
        }

        public static readonly DependencyProperty IndexProperty =
            DependencyProperty.RegisterAttached("Index",
                                                typeof(int),
                                                typeof(TimelinePanel),
                                                new FrameworkPropertyMetadata(0,
                                                                              FrameworkPropertyMetadataOptions
                                                                                  .AffectsArrange));

        #endregion

        #endregion

        #region プロパティ

        #region IndexWidth変更通知プロパティ

        public double IndexWidth
        {
            get { return (double) GetValue(IndexWidthProperty); }
            set { SetValue(IndexWidthProperty, value); }
        }

        public static readonly DependencyProperty IndexWidthProperty =
            DependencyProperty.Register("IndexWidth",
                                        typeof(double),
                                        typeof(TimelinePanel),
                                        new FrameworkPropertyMetadata(double.NaN,
                                                                      FrameworkPropertyMetadataOptions
                                                                          .AffectsArrange));

        #endregion

        #region MaxIndex変更通知プロパティ

        public double MaxIndex
        {
            get { return (double) GetValue(MaxIndexProperty); }
            set { SetValue(MaxIndexProperty, value); }
        }

        public static readonly DependencyProperty MaxIndexProperty =
            DependencyProperty.Register("MaxIndex",
                                        typeof(double),
                                        typeof(TimelinePanel),
                                        new FrameworkPropertyMetadata(double.NaN,
                                                                      FrameworkPropertyMetadataOptions
                                                                          .AffectsMeasure));

        #endregion

        #region Alignment変更通知プロパティ

        public AlignmentX Alignment
        {
            get { return (AlignmentX) GetValue(AlignmentProperty); }
            set { SetValue(AlignmentProperty, value); }
        }

        public static readonly DependencyProperty AlignmentProperty =
            DependencyProperty.Register(nameof(Alignment),
                                        typeof(AlignmentX),
                                        typeof(TimelinePanel),
                                        new FrameworkPropertyMetadata(AlignmentX.Right,
                                                                      FrameworkPropertyMetadataOptions
                                                                          .AffectsMeasure));

        #endregion

        #endregion

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return null;
        }

        #region 測定、配置

        private double GetPosition(double x, double indexWidth, UIElement child)
        {
            x *= indexWidth;
            x += Margin.Left;
            if (child != null)
            {
                switch (Alignment)
                {
                    case AlignmentX.Center:
                        x -= child.DesiredSize.Width / 2;
                        x += indexWidth / 2;
                        break;
                    case AlignmentX.Left:
                        x -= child.DesiredSize.Width;
                        x += indexWidth / 2;
                        break;
                }
            }
            return x;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in Children)
            {
                var location = new Point(GetIndex(child), Margin.Top);
                var width = double.IsNaN(IndexWidth) ? child.DesiredSize.Width : IndexWidth;
                location.X = GetPosition(location.X, width, child);

                child.Arrange(new Rect(location, child.DesiredSize));
            }

            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var indexWidth = 0.0;
            var height = 0.0;
            var index = 0;
            UIElement lastIndexChild = null;
            foreach (UIElement child in Children)
            {
                child.Measure(availableSize);
                indexWidth = Math.Max(indexWidth, child.DesiredSize.Width);
                height = Math.Max(height, child.DesiredSize.Height);
                var childIndex = GetIndex(child);
                if (index <= childIndex)
                {
                    lastIndexChild = child;
                    index = childIndex;
                }
            }

            if (double.IsNaN(IndexWidth) == false)
            {
                indexWidth = IndexWidth;
            }

            var size = new Size
            {
                Width = Math.Min(availableSize.Width,
                                 GetPosition(double.IsNaN(MaxIndex) ? index : MaxIndex, indexWidth,
                                             lastIndexChild) + lastIndexChild?.DesiredSize.Width ?? 0
                                 + Margin.Right),
                Height =
                    Math.Min(height + Margin.Top + Margin.Bottom, availableSize.Height)
            };
            return size;
        }

        #endregion
    }
}
