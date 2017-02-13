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
using System.Windows.Controls.Primitives;
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
    public class TimelineItem : ContentControl
    {
        public TimelineItem()
        {
            PreviewMouseLeftButtonDown += (sender, args) =>
            {
                //Mouse.Capture(null);
                //args.Handled = true;
                IsSelected = true;
            };
        }

        #region Indexプロパティ

        public static readonly DependencyProperty IndexProperty =
            DependencyProperty.Register("Index", typeof(int), typeof(TimelineItem),
                                        new FrameworkPropertyMetadata(default(int),
                                                                      FrameworkPropertyMetadataOptions
                                                                          .AffectsParentArrange));

        public int Index
        {
            get { return (int) GetValue(IndexProperty); }
            set { SetValue(IndexProperty, value); }
        }

        #endregion

        #region IsSelectedプロパティ

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(TimelineItem),
                                        new FrameworkPropertyMetadata(default(bool),
                                                                      FrameworkPropertyMetadataOptions
                                                                          .AffectsArrange));

        public bool IsSelected
        {
            get { return (bool) GetValue(IsSelectedProperty); }
            set
            {
                SetValue(IsSelectedProperty, value);
                SelectionChanged?.Invoke(value);
            }
        }

        public delegate void SelectionChangedDelegate(bool isSelected);

        public event SelectionChangedDelegate SelectionChanged;

        public void ReverseSelect()
        {
            IsSelected = !IsSelected;
        }

        #endregion
    }

    /// <summary>
    /// TimelineControl.xaml の相互作用ロジック
    /// </summary>
    public class TimelineControlBase : MultiSelector {}

    public partial class TimelineControl : TimelineControlBase
    {
        public TimelineControl()
        {
            InitializeComponent();
            CanSelectMultipleItems = true;
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);
            RenderItems();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        private List<TimelineItem> Children { get; } = new List<TimelineItem>();

        private List<TimelineItem> _selectedItems = new List<TimelineItem>();

        protected override int VisualChildrenCount => Children.Count;

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

        #region Alignment変更通知プロパティ

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

            foreach (var item in ItemsSource)
            {
                var controlItem = new TimelineItem
                {
                    Style = ItemContainerStyle,
                    DataContext = item
                };
                AddVisualChild(controlItem);
                Children.Add(controlItem);
                controlItem.SelectionChanged += selected =>
                {
                    BeginUpdateSelectedItems();
                    if (selected)
                    {
                        SelectedItems.Add(controlItem);
                    }
                    else
                    {
                        SelectedItems.Remove(controlItem);
                    }
                    EndUpdateSelectedItems();
                };
            }
            InvalidateMeasure();
        }

        public new void SelectAll()
        {
            foreach (var timelineItem in Children)
            {
                timelineItem.IsSelected = true;
            }
        }

        public new void UnselectAll()
        {
            var list = SelectedItems.Cast<TimelineItem>().ToList();
            foreach (var timelineItem in list)
            {
                timelineItem.IsSelected = false;
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
                var location = new Point(child.Index, Margin.Top);
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

        #region 測定、配置

        protected override Size MeasureOverride(Size availableSize)
        {
            var indexWidth = 0.0;
            var height = 0.0;
            var index = 0;
            FrameworkElement lastIndexChild = null;
            foreach (var child in Children)
            {
                child.Measure(availableSize);
                indexWidth = Math.Max(indexWidth, child.DesiredSize.Width);
                height = Math.Max(height, child.DesiredSize.Height);
                var i = child.Index;
                if (index < i)
                {
                    lastIndexChild = child;
                    index = i;
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
