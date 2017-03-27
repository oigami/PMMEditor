using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PMMEditor.ViewModels.Documents;
using PMMEditor.Views.Behaviors;

namespace PMMEditor.Views.Timeline
{
    /// <summary>
    /// TimelinePanel.xaml の相互作用ロジック
    /// </summary>
    public class TimelinePanel : Panel, IVirtualizingPanel
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
                                                                                  .AffectsParentArrange));

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

        #region SelectedItemBrush変更通知プロパティ

        public FrameworkElement SelectedItemVisual
        {
            get { return (FrameworkElement) GetValue(SelectedItemVisualProperty); }
            set { SetValue(SelectedItemVisualProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemVisualProperty =
            DependencyProperty.Register(
                nameof(SelectedItemVisual),
                typeof(FrameworkElement),
                typeof(TimelinePanel),
                new FrameworkPropertyMetadata(
                    null,
                    (s, args) =>
                    {
                        var panel = (TimelinePanel) s;
                        panel.OnVisualChanged((FrameworkElement) args.NewValue,
                                              panel.Visual_Initialized);
                    }));

        #endregion

        #region UnselectedItemBrush変更通知プロパティ

        public FrameworkElement UnselectedItemVisual
        {
            get { return (FrameworkElement) GetValue(UnselectedItemVisualProperty); }
            set { SetValue(UnselectedItemVisualProperty, value); }
        }

        public static readonly DependencyProperty UnselectedItemVisualProperty =
            DependencyProperty.Register(
                nameof(UnselectedItemVisual),
                typeof(FrameworkElement),
                typeof(TimelinePanel),
                new FrameworkPropertyMetadata(
                    null,
                    (s, args) =>
                    {
                        var panel = (TimelinePanel) s;
                        panel.OnVisualChanged((FrameworkElement) args.NewValue,
                                              panel.UnselectedVisual_Initialized);
                    }));

        #endregion

        #region UnselectedItemBrush変更通知プロパティ

        public Rect Viewport
        {
            get { return (Rect) GetValue(ViewportProperty); }
            set { SetValue(ViewportProperty, value); }
        }

        public static readonly DependencyProperty ViewportProperty =
            DependencyProperty.Register(
                nameof(Viewport),
                typeof(Rect),
                typeof(TimelinePanel),
                new FrameworkPropertyMetadata(
                    new Rect(0, 0, double.PositiveInfinity, double.PositiveInfinity),
                    (s, args) => ((TimelinePanel) s).OnViewportChanged()));


        private void OnViewportChanged()
        {
            if (_renderedRect.Right < Viewport.Right)
            {
                InvalidateVisual();
            }
            else if (Viewport.Left < _renderedRect.Left)
            {
                InvalidateVisual();
            }
        }

        private Rect _renderedRect;

        #endregion

        struct RenderObject
        {
            public Brush Brush { get; set; }

            public Size Size { get; set; }
        }

        private RenderObject _selectedObject;
        private RenderObject _unselectedObject;

        private void OnVisualChanged(FrameworkElement visual, EventHandler handler)
        {
            visual.Initialized += handler;
        }

        private ImageBrush VisualRender(FrameworkElement visual)
        {
            var size = new Size(double.PositiveInfinity, double.PositiveInfinity);
            visual.Measure(size);
            visual.Arrange(new Rect(visual.DesiredSize));

            var renderBitmap = new RenderTargetBitmap((int) visual.ActualWidth,
                                                      (int) visual.ActualHeight,
                                                      96.0,
                                                      96.0,
                                                      PixelFormats.Pbgra32);
            renderBitmap.Render(visual);
            renderBitmap.Freeze();
            var brush = new ImageBrush(renderBitmap);
            brush.Freeze();
            return brush;
        }

        private void Visual_Initialized(object sender, EventArgs e)
        {
            var visual = (FrameworkElement) sender;
            visual.Initialized -= Visual_Initialized;
            _selectedObject.Brush = VisualRender(visual);

            _selectedObject.Size = new Size(SelectedItemVisual.ActualWidth, SelectedItemVisual.ActualHeight);
        }

        private void UnselectedVisual_Initialized(object sender, EventArgs e)
        {
            var visual = (FrameworkElement) sender;
            visual.Initialized -= UnselectedVisual_Initialized;
            _unselectedObject.Brush = VisualRender(visual);
            _unselectedObject.Size = new Size(UnselectedItemVisual.ActualWidth, UnselectedItemVisual.ActualHeight);
        }

        public IList<TimelineFrameData> KeyChildren
        {
            get { return (IList<TimelineFrameData>) GetValue(KeyChildrenProperty); }
            set { SetValue(KeyChildrenProperty, value); }
        }

        public static readonly DependencyProperty KeyChildrenProperty =
            DependencyProperty.Register(
                nameof(KeyChildren),
                typeof(IList<TimelineFrameData>),
                typeof(TimelinePanel),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return null;
        }

        public int LowerBound(IList<TimelineFrameData> data, double value)
        {
            int l = 0, r = data.Count;
            var width = double.IsNaN(IndexWidth) ? SelectedItemVisual.DesiredSize.Width : IndexWidth;

            while (r - l > 0)
            {
                int mid = (r - l) / 2 + l;
                double x = GetPosition(data[mid].FrameNumber, width, SelectedItemVisual);
                if (value <= x)
                {
                    r = mid;
                }
                else
                {
                    l = mid + 1;
                }
            }
            return r;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (SelectedItemVisual == null || UnselectedItemVisual == null || KeyChildren == null)
            {
                return;
            }
            var width = double.IsNaN(IndexWidth) ? SelectedItemVisual.DesiredSize.Width : IndexWidth;


            _renderedRect.X = Math.Max(0, Viewport.Left - Viewport.Width);
            _renderedRect.Width = Viewport.Right + Viewport.Width;

            for (int i = LowerBound(KeyChildren, _renderedRect.Left); i < KeyChildren.Count; i++)
            {
                var data = KeyChildren[i];
                var obj = data.IsSelected ? _selectedObject : _unselectedObject;
                double x = GetPosition(data.FrameNumber, width, SelectedItemVisual);
                if (_renderedRect.Right < x)
                {
                    break;
                }
                dc.DrawRectangle(obj.Brush, null,
                                 new Rect(new Point(x, 0), obj.Size));
            }
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
            if (SelectedItemVisual != null && UnselectedItemVisual != null)
            {
                return new Size
                {
                    Width =
                        Math.Max(SelectedItemVisual.DesiredSize.Width, UnselectedItemVisual.DesiredSize.Width) * 1000,
                    Height =
                        Math.Max(SelectedItemVisual.DesiredSize.Height, UnselectedItemVisual.DesiredSize.Height)
                };
            }
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

            return new Size
            {
                Width = Math.Min(availableSize.Width,
                                 GetPosition(double.IsNaN(MaxIndex) ? index : MaxIndex, indexWidth,
                                             lastIndexChild) + lastIndexChild?.DesiredSize.Width ?? 0
                                 + Margin.Right),
                Height =
                    Math.Min(height + Margin.Top + Margin.Bottom, availableSize.Height)
            };
        }

        #endregion
    }
}
