using Livet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PMMEditor.Views.Panes;

namespace PMMEditor.Views.Documents
{
    /// <summary>
    /// TimelineView.xaml の相互作用ロジック
    /// </summary>
    public partial class TimelineView : UserControl
    {
        public TimelineView()
        {
            InitializeComponent();
        }

        private readonly List<TimelineItem> _rangeSelectedElements = new List<TimelineItem>();

        private void RangeHitTest()
        {
            foreach (var item in _rangeSelectedElements)
            {
                item.IsSelected = false;
            }
            var rect =
                new RectangleGeometry(
                    new Rect(new Point(Canvas.GetLeft(RangeSelectBorder), Canvas.GetTop(RangeSelectBorder)),
                             new Size(RangeSelectBorder.Width, RangeSelectBorder.Height)));
            var hitTestParams = new GeometryHitTestParameters(rect);

            var resultCallback = new HitTestResultCallback(
                result => HitTestResultBehavior.Continue);

            var filterCallback = new HitTestFilterCallback(
                element =>
                {
                    var item = element as TimelineItem;
                    if (item != null)
                    {
                        _rangeSelectedElements.Add(item);
                        item.IsSelected = true;
                        return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                    }
                    return HitTestFilterBehavior.Continue;
                });

            VisualTreeHelper.HitTest(TimelineViewer, filterCallback, resultCallback, hitTestParams);
        }

        #region SelectRange

        private Point _rangeStartPoint;

        private void BackgroundSelectRange_OnDragStarted(object sender, DragStartedEventArgs e)
        {
            Canvas.SetLeft(RangeSelectBorder, e.HorizontalOffset);
            Canvas.SetTop(RangeSelectBorder, e.VerticalOffset);
            RangeSelectBorder.BorderThickness = new Thickness(1);
            if (Keyboard.IsKeyDown(Key.LeftShift) == false && Keyboard.IsKeyDown(Key.RightShift) == false)
            {
                foreach (var item in GetTimeline())
                {
                    item.UnselectAll();
                }
            }
            _rangeSelectedElements.Clear();
            _rangeStartPoint = new Point(e.HorizontalOffset, e.VerticalOffset);
        }

        private void BackgroundSelectRange_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            RangeSelectBorder.Width = Math.Abs(e.HorizontalChange);
            RangeSelectBorder.Height = Math.Abs(e.VerticalChange);
            Canvas.SetLeft(RangeSelectBorder, Math.Min(e.HorizontalChange + _rangeStartPoint.X, _rangeStartPoint.X));
            Canvas.SetTop(RangeSelectBorder, Math.Min(e.VerticalChange + _rangeStartPoint.Y, _rangeStartPoint.Y));

            RangeHitTest();
        }

        private void BackgroundSelectRange_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            RangeSelectBorder.Width = 0;
            RangeSelectBorder.Height = 0;
        }

        #endregion

        IEnumerable<TimelineControl> GetTimeline()
        {
            foreach (var o in TimelineViewer.Items)
            {
                var c = (ContentPresenter) TimelineViewer.ItemContainerGenerator.ContainerFromItem(o);
                yield return c.ContentTemplate.FindName("TimelineControl", c) as TimelineControl;
            }
        }

        IEnumerable<TimelineItem> GetSelectedTimelineItem()
        {
            foreach (var item in GetTimeline())
            {
                if (item == null || item.IsArrangeValid == false)
                {
                    continue;
                }

                foreach (var selectedItem in item.SelectedItems)
                {
                    yield return (TimelineItem) selectedItem;
                }
            }
        }

        #region KeyFrameMove

        private void KeyFrameMoveStarted(object sender, DragStartedEventArgs e) {}

        private void KeyFrameMoveDelta(object sender, DragDeltaEventArgs e)
        {
            int diff = (int) e.HorizontalChange / 14;
            foreach (var item1 in GetSelectedTimelineItem())
            {
                item1.Index += diff;
            }
        }

        private void KeyFrameMoveCompleted(object sender, DragCompletedEventArgs e) {}

        #endregion
    }
}
