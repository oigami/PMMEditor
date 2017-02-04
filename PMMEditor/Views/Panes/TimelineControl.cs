using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PMMEditor.Views.Panes
{
    /// <summary>
    /// TimelineControl.xaml の相互作用ロジック
    /// </summary>
    public class TimelineControl : ContentControl
    {
        #region Indexプロパティ

        public static int GetIndex(DependencyObject obj)
        {
            return (int) obj.GetValue(IndexProperty);
        }

        public static void SetIndex(DependencyObject obj, int value)
        {
            obj.SetValue(IndexProperty, value);
        }

        public static readonly DependencyProperty IndexProperty =
            DependencyProperty.RegisterAttached("Index", typeof(int), typeof(TimelineControl),
                                                new FrameworkPropertyMetadata(default(int),
                                                                              FrameworkPropertyMetadataOptions
                                                                                  .AffectsArrange));

        #endregion

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        private List<FrameworkElement> Children { get; } = new List<FrameworkElement>();

        #region ItemsSourceプロパティ

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource",
                                        typeof(IEnumerable),
                                        typeof(TimelineControl),
                                        new FrameworkPropertyMetadata(null,
                                                                      FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                                      ItemsSourceChanged));

        private static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimelineControl) d).RenderItems();
        }

        #endregion

        #region ItemTemplateプロパティ

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate) GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(TimelineControl),
                                        new PropertyMetadata(null));

        #endregion

        protected override int VisualChildrenCount => Children.Count;

        public Style ItemContainerStyle { private get; set; }

        #region IndexWidth変更通知プロパティ

        public double IndexWidth
        {
            get { return (double) GetValue(IndexWidthProperty); }
            set { SetValue(IndexWidthProperty, value); }
        }

        public static readonly DependencyProperty IndexWidthProperty =
            DependencyProperty.Register("IndexWidth",
                                        typeof(double),
                                        typeof(TimelineControl),
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
                                        typeof(TimelineControl),
                                        new FrameworkPropertyMetadata(double.NaN,
                                                                      FrameworkPropertyMetadataOptions
                                                                          .AffectsMeasure));

        #endregion

        #region MaxIndex変更通知プロパティ

        public AlignmentX Alignment
        {
            get { return (AlignmentX) GetValue(AlignmentProperty); }
            set { SetValue(AlignmentProperty, value); }
        }

        public static readonly DependencyProperty AlignmentProperty =
            DependencyProperty.Register(nameof(Alignment),
                                        typeof(AlignmentX),
                                        typeof(TimelineControl),
                                        new FrameworkPropertyMetadata(AlignmentX.Right,
                                                                      FrameworkPropertyMetadataOptions
                                                                          .AffectsMeasure));

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            RenderItems();
        }

        private void RenderItems()
        {
            foreach (var item in Children)
            {
                RemoveVisualChild(item);
            }
            Children.Clear();

            if (ItemsSource == null)
            {
                return;
            }

            var itemTemplate = ItemTemplate ?? new ResourceDictionary
            {
                Source =
                    new Uri("pack://application:,,,/PmmEditor;component/Views/Panes/TimelineResource.xaml",
                            UriKind.Absolute)
            }["DefaultTimelineItemTemplate"] as DataTemplate;

            if (itemTemplate == null)
            {
                return;
            }

            foreach (var item in ItemsSource)
            {
                var elem = itemTemplate.LoadContent() as FrameworkElement;
                elem.DataContext = item;
                elem.Style = ItemContainerStyle;
                Children.Add(elem);
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            return Children[index];
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var child in Children)
            {
                var location = new Point(GetIndex(child), Margin.Top);
                var width = double.IsNaN(IndexWidth) ? child.DesiredSize.Width : IndexWidth;
                location.X = GetPosition(location.X, width, child);

                child.Arrange(new Rect(location, child.DesiredSize));
            }

            return finalSize;
        }

        private double GetPosition(double x, double indexWidth, FrameworkElement child)
        {
            indexWidth += Padding.Left + Padding.Right;

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
            x -= (Padding.Right - Padding.Left) / 2;
            return x;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var width = 0.0;
            var height = 0.0;
            var index = 0;

            foreach (var child in Children)
            {
                child.Measure(availableSize);
                width = Math.Max(width, child.DesiredSize.Width);
                height = Math.Max(height, child.DesiredSize.Height);
                index = Math.Max(index, (int) GetIndex(child));
            }

            if (double.IsNaN(IndexWidth) == false)
            {
                width = IndexWidth;
            }

            var size = new Size
            {
                Width = Math.Min(availableSize.Width,
                                 GetPosition(double.IsNaN(MaxIndex) ? index : MaxIndex, width,
                                             Children.Count == 0 ? null : Children[0]) + width
                                 + Margin.Right),
                Height =
                    Math.Min(height + Margin.Top + Margin.Bottom, availableSize.Height)
            };
            return size;
        }
    }
}
