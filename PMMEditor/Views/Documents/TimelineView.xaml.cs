using Livet;
using System;
using System.Collections;
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
    public class KeyFrameMoveEventArgs
    {
        public KeyFrameMoveEventArgs(IEnumerable<TimelineControl> controls, IEnumerable<IEnumerable<TimelineItem>> selectedItems,
                                     int diffFrame)
        {
            Controls = controls;
            SelectedItems = selectedItems;
            DiffFrame = diffFrame;
        }

        public int DiffFrame { get; }

        public IEnumerable<TimelineControl> Controls { get; }

        public IEnumerable<IEnumerable<TimelineItem>> SelectedItems { get; }
    }


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
                    new Rect(new Point(Canvas.GetLeft(SelectRangeControl), Canvas.GetTop(SelectRangeControl)),
                             new Size(SelectRangeControl.Width, SelectRangeControl.Height)));
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
            Canvas.SetLeft(SelectRangeControl, e.HorizontalOffset);
            Canvas.SetTop(SelectRangeControl, e.VerticalOffset);
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
            SelectRangeControl.Width = Math.Abs(e.HorizontalChange);
            SelectRangeControl.Height = Math.Abs(e.VerticalChange);
            Canvas.SetLeft(SelectRangeControl, Math.Min(e.HorizontalChange + _rangeStartPoint.X, _rangeStartPoint.X));
            Canvas.SetTop(SelectRangeControl, Math.Min(e.VerticalChange + _rangeStartPoint.Y, _rangeStartPoint.Y));

            RangeHitTest();
        }

        private void BackgroundSelectRange_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            SelectRangeControl.Width = 0;
            SelectRangeControl.Height = 0;
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

        private KeyFrameMoveEventArgs CreateKeyMoveEventArgs(int diff)
        {
            IEnumerable<TimelineControl> timeline = GetTimeline();
            IEnumerable<IEnumerable<TimelineItem>> selectedItems =
                timeline.Select(i =>i.SelectedItems.Cast<TimelineItem>());
            return new KeyFrameMoveEventArgs(timeline, selectedItems, diff);
        }

        private void OnKeyFrameMoveStarted(object sender, DragStartedEventArgs e)
        {
            KeyFrameMoveStartedCommand?.Execute(CreateKeyMoveEventArgs(0));
        }

        private void OnKeyFrameMoveDelta(object sender, DragDeltaEventArgs e)
        {
            int diff = (int) e.HorizontalChange / 14;
            if (diff == 0)
            {
                return;
            }
            if (KeyFrameMoveDeltaCommand != null)
            {
                KeyFrameMoveDeltaCommand.Execute(CreateKeyMoveEventArgs(diff));
                return;
            }
            // 負のときはキーフレームが負にならないように事前に確認する
            if (diff < 0)
            {
                foreach (var item1 in GetSelectedTimelineItem())
                {
                    if (item1.Index + diff < 0)
                    {
                        return;
                    }
                }
            }
            foreach (var item1 in GetSelectedTimelineItem())
            {
                item1.Index += diff;
            }
        }

        private void OnKeyFrameMoveCompleted(object sender, DragCompletedEventArgs e)
        {
            KeyFrameMoveCompletedCommand?.Execute(CreateKeyMoveEventArgs(0));
        }

        #endregion

        public ICommand KeyFrameMoveStartedCommand
        {
            get { return (ICommand) GetValue(KeyFrameMoveStartedCommandProperty); }
            set { SetValue(KeyFrameMoveStartedCommandProperty, value); }
        }

        public static readonly DependencyProperty KeyFrameMoveStartedCommandProperty =
            DependencyProperty.Register(nameof(KeyFrameMoveStartedCommand),
                                        typeof(ICommand),
                                        typeof(TimelineView),
                                        new PropertyMetadata(null));

        public ICommand KeyFrameMoveDeltaCommand
        {
            get { return (ICommand) GetValue(KeyFrameMoveDeltaCommandProperty); }
            set { SetValue(KeyFrameMoveDeltaCommandProperty, value); }
        }

        public static readonly DependencyProperty KeyFrameMoveDeltaCommandProperty =
            DependencyProperty.Register(nameof(KeyFrameMoveDeltaCommand),
                                        typeof(ICommand),
                                        typeof(TimelineView),
                                        new PropertyMetadata(null));

        public ICommand KeyFrameMoveCompletedCommand
        {
            get { return (ICommand) GetValue(KeyFrameMoveCompletedCommandProperty); }
            set { SetValue(KeyFrameMoveCompletedCommandProperty, value); }
        }

        public static readonly DependencyProperty KeyFrameMoveCompletedCommandProperty =
            DependencyProperty.Register(nameof(KeyFrameMoveCompletedCommand),
                                        typeof(ICommand),
                                        typeof(TimelineView),
                                        new PropertyMetadata(null));


        /*
         * Style変更用プロパティ
         */

        #region Visual変更通知プロパティ

        public Style TimelineGridStyle
        {
            get { return (Style) GetValue(TimelineGridStyleProperty); }
            set { SetValue(TimelineGridStyleProperty, value); }
        }

        public static readonly DependencyProperty TimelineGridStyleProperty =
            DependencyProperty.Register("TimelineGridStyle",
                                        typeof(Style),
                                        typeof(TimelineView),
                                        new FrameworkPropertyMetadata(null,
                                                                      FrameworkPropertyMetadataOptions
                                                                          .AffectsArrange));

        #endregion

        #region SelectRange変更通知プロパティ

        public DataTemplate SelectRangeTemplate
        {
            get { return (DataTemplate) GetValue(SelectRangeTemplateProperty); }
            set { SetValue(SelectRangeTemplateProperty, value); }
        }

        public static readonly DependencyProperty SelectRangeTemplateProperty =
            DependencyProperty.Register("SelectRangeTemplate",
                                        typeof(DataTemplate),
                                        typeof(TimelineView),
                                        new FrameworkPropertyMetadata(null,
                                                                      FrameworkPropertyMetadataOptions
                                                                          .AffectsArrange));

        #endregion

        #region SelectedContainerStyle変更通知プロパティ

        public ControlTemplate SelectedContainerTemplate
        {
            get { return (ControlTemplate) GetValue(SelectedContainerTemplateProperty); }
            set { SetValue(SelectedContainerTemplateProperty, value); }
        }

        public static readonly DependencyProperty SelectedContainerTemplateProperty =
            DependencyProperty.Register("SelectedContainerTemplate",
                                        typeof(ControlTemplate),
                                        typeof(TimelineView),
                                        new FrameworkPropertyMetadata(null,
                                                                      FrameworkPropertyMetadataOptions
                                                                          .AffectsArrange));

        #endregion

        #region SelectedContainerStyle変更通知プロパティ

        public ControlTemplate UnselectedContainerTemplate
        {
            get { return (ControlTemplate) GetValue(UnselectedContainerTemplateProperty); }
            set { SetValue(UnselectedContainerTemplateProperty, value); }
        }

        public static readonly DependencyProperty UnselectedContainerTemplateProperty =
            DependencyProperty.Register("UnselectedContainerTemplate",
                                        typeof(ControlTemplate),
                                        typeof(TimelineView),
                                        new FrameworkPropertyMetadata(null,
                                                                      FrameworkPropertyMetadataOptions
                                                                          .AffectsArrange));

        #endregion
    }
}
