using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;

namespace PMMEditor.Views.Behaviors
{
    /*
     * プログラミングな日々: WPFのScrollViewerやScrollBarのスクロール位置を同期させる
     * https://days-of-programming.blogspot.jp/2015/01/wpfscrollviewerscrollbar.html
     */
    public class ScrollSyncronizingBehavior : Behavior<Control>
    {
        static Dictionary<string, List<Control>> SyncGroups = new Dictionary<string, List<Control>>();

        protected override void OnAttached()
        {
            base.OnAttached();

            AddSyncGroup(ScrollGroup);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            RemoveSyncGroup(ScrollGroup);
        }

        /// <summary>
        /// スクロールグループ
        /// </summary>
        public string ScrollGroup
        {
            get { return (string)this.GetValue(ScrollGroupProperty); }
            set { this.SetValue(ScrollGroupProperty, value); }
        }
        private static readonly DependencyProperty ScrollGroupProperty = DependencyProperty.Register(
            "ScrollGroup", typeof(string), typeof(ScrollSyncronizingBehavior), new FrameworkPropertyMetadata((d, e) => {
                ScrollSyncronizingBehavior me = (ScrollSyncronizingBehavior)d;

                me.RemoveSyncGroup((string)e.OldValue);
                me.AddSyncGroup((string)e.NewValue);
            })
        );

        /// <summary>
        /// スクロールの向き
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)this.GetValue(OrientationProperty); }
            set { this.SetValue(OrientationProperty, value); }
        }
        private static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(ScrollSyncronizingBehavior), new FrameworkPropertyMetadata()
        );

        /// <summary>
        /// 同期グループに追加するメソッド
        /// </summary>
        /// <param name="GroupName">グループ名</param>
        /// <returns>成功したかどうか</returns>
        bool AddSyncGroup(string GroupName)
        {
            if (!string.IsNullOrEmpty(ScrollGroup) && (this.AssociatedObject is ScrollViewer || this.AssociatedObject is ScrollBar))
            {
                if (!SyncGroups.ContainsKey(GroupName))
                    SyncGroups.Add(GroupName, new List<Control>());
                SyncGroups[GroupName].Add(this.AssociatedObject);

                ScrollViewer sv = this.AssociatedObject as ScrollViewer;
                ScrollBar sb = this.AssociatedObject as ScrollBar;

                if (sv != null)
                    sv.ScrollChanged += ScrollViewerScrolled;
                if (sb != null)
                    sb.ValueChanged += ScrollBarScrolled;

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// 同期グループから削除するメソッド
        /// </summary>
        /// <param name="GroupName">グループ名</param>
        /// <returns>成功したかどうか</returns>
        bool RemoveSyncGroup(string GroupName)
        {
            if (!string.IsNullOrEmpty(ScrollGroup) && (this.AssociatedObject is ScrollViewer || this.AssociatedObject is ScrollBar))
            {
                ScrollViewer sv = this.AssociatedObject as ScrollViewer;
                ScrollBar sb = this.AssociatedObject as ScrollBar;

                if (sv != null)
                    sv.ScrollChanged -= ScrollViewerScrolled;
                if (sb != null)
                    sb.ValueChanged -= ScrollBarScrolled;

                SyncGroups[GroupName].Remove(this.AssociatedObject);
                if (SyncGroups[GroupName].Count == 0)
                    SyncGroups.Remove(GroupName);

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// ScrollViewerの場合の変更通知イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ScrollViewerScrolled(object sender, ScrollChangedEventArgs e)
        {
            UpdateScrollValue(sender, Orientation == Orientation.Horizontal ? e.HorizontalOffset : e.VerticalOffset);
        }

        /// <summary>
        /// ScrollBarの場合の変更通知イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ScrollBarScrolled(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateScrollValue(sender, e.NewValue);
        }

        /// <summary>
        /// スクロール値を設定するメソッド
        /// </summary>
        /// <param name="sender">スクロール値を更新してきたコントロール</param>
        /// <param name="NewValue">新しいスクロール値</param>
        void UpdateScrollValue(object sender, double NewValue)
        {
            IEnumerable<Control> others = SyncGroups[ScrollGroup].Where(p => p != sender);

            foreach (ScrollBar sb in others.OfType<ScrollBar>().Where(p => p.Orientation == Orientation))
                sb.Value = NewValue;
            foreach (ScrollViewer sv in others.OfType<ScrollViewer>())
            {
                if (Orientation == Orientation.Horizontal)
                    sv.ScrollToHorizontalOffset(NewValue);
                else
                    sv.ScrollToVerticalOffset(NewValue);
            }
        }
    }
}
