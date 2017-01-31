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
        #region Leftプロパティ

        public static double GetIndex(DependencyObject obj)
        {
            return (double) obj.GetValue(IndexProperty);
        }

        public static void SetIndex(DependencyObject obj, double value)
        {
            obj.SetValue(IndexProperty, value);
        }

        public static readonly DependencyProperty IndexProperty =
            DependencyProperty.RegisterAttached("Index", typeof(double), typeof(TimelineControl),
                                                new FrameworkPropertyMetadata(default(double),
                                                                              FrameworkPropertyMetadataOptions
                                                                                  .AffectsArrange
                                                                              | FrameworkPropertyMetadataOptions
                                                                                  .AffectsMeasure));

        #endregion

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        private List<FrameworkElement> Children { get; } = new List<FrameworkElement>();

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource",
                                        typeof(IEnumerable),
                                        typeof(TimelineControl),
                                        new PropertyMetadata(null, ItemsSourceChanged));

        private static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimelineControl) d).RenderItems();
        }

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
            var itemTemplate = ItemTemplate;
            if (itemTemplate == null)
            {
                ResourceDictionary rd = new ResourceDictionary
                {
                    Source =
                        new Uri("pack://application:,,,/PmmEditor;component/Views/Panes/TimelineResource.xaml",
                                UriKind.Absolute)
                };
                itemTemplate = rd["DefaultTimelineItemTemplate"] as DataTemplate;

                if (itemTemplate == null)
                {
                    return;
                }
            }
            foreach (var item in ItemsSource)
            {
                var elem = itemTemplate.LoadContent() as FrameworkElement;
                elem.DataContext = item;
                elem.Style = ItemContainerStyle;
                Children.Add(elem);
            }
        }

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

        #region ItemTemplateプロパティ

        public DataTemplate DefaultItemTemplate
        {
            get { return (DataTemplate) GetValue(DefaultItemTemplateProperty); }
            set { SetValue(DefaultItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty DefaultItemTemplateProperty =
            DependencyProperty.Register("DefaultItemTemplate", typeof(DataTemplate), typeof(TimelineControl),
                                        new PropertyMetadata(null));

        #endregion

        protected override int VisualChildrenCount => Children.Count;

        public Style ItemContainerStyle { get; set; }

        protected override Visual GetVisualChild(int index)
        {
            return Children[index];
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var child in Children)
            {
                var location = new Point(GetIndex(child), Margin.Top);
                location.X *= child.DesiredSize.Width * 2;
                location.X += Margin.Left;
                child.Arrange(new Rect(location, child.DesiredSize));
            }

            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children)
            {
                child.Measure(availableSize);
            }

            return base.MeasureOverride(availableSize);
        }
    }
}
