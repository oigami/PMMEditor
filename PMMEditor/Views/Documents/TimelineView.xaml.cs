using Livet;
using System;
using System.Collections.Generic;
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

        readonly List<TimelineItem> _rangeSelectedElements = new List<TimelineItem>();
        private Point _rangeStartPoint;

        private void RangeHitTest()
        {
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
                    }
                    return HitTestFilterBehavior.Continue;
                });

            VisualTreeHelper.HitTest(
                                     TimelineViewer, filterCallback, resultCallback, hitTestParams);
        }

        private void BackgroundSelectRange_OnDragStarted(object sender, DragStartedEventArgs e)
        {
            Canvas.SetLeft(RangeSelectBorder, e.HorizontalOffset);
            Canvas.SetTop(RangeSelectBorder, e.VerticalOffset);
            RangeSelectBorder.BorderThickness = new Thickness(1);
            if (Keyboard.IsKeyDown(Key.LeftShift) == false && Keyboard.IsKeyDown(Key.RightShift) == false)
            {
                foreach (var item in _rangeSelectedElements)
                {
                    item.IsSelected = false;
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
    }
}
