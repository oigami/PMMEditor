using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

    public class ScrollSyncronizingBehavior : BehaviorBase<Control>
    {
        private static readonly Dictionary<string, List<Control>> SyncGroups = new Dictionary<string, List<Control>>();

        private void AddEvent<T>(DependencyProperty prop, EventHandler handler)
        {
            DependencyPropertyDescriptor.FromProperty(prop, typeof(T))
                                        .AddValueChanged(AssociatedObject, handler);
        }

        private void RemoveEvent<T>(DependencyProperty prop, EventHandler handler)
        {
            DependencyPropertyDescriptor.FromProperty(prop, typeof(T))
                                        .RemoveValueChanged(AssociatedObject, handler);
        }

        protected override void OnSetup()
        {
            AddSyncGroup(ScrollGroup);
        }

        protected override void OnCleanup()
        {
            RemoveSyncGroup(ScrollGroup);
        }

        /// <summary>
        /// スクロールグループ
        /// </summary>
        public string ScrollGroup
        {
            get { return (string) GetValue(ScrollGroupProperty); }
            set { SetValue(ScrollGroupProperty, value); }
        }

        private static readonly DependencyProperty ScrollGroupProperty
            = DependencyProperty.Register("ScrollGroup",
                                          typeof(string),
                                          typeof(ScrollSyncronizingBehavior),
                                          new FrameworkPropertyMetadata((d, e) =>
                                          {
                                              var me = (ScrollSyncronizingBehavior) d;

                                              me.RemoveSyncGroup((string) e.OldValue);
                                              me.AddSyncGroup((string) e.NewValue);
                                          })
                );

        /// <summary>
        /// スクロールの向き
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        private static readonly DependencyProperty OrientationProperty
            = DependencyProperty.Register("Orientation",
                                          typeof(Orientation),
                                          typeof(ScrollSyncronizingBehavior),
                                          new FrameworkPropertyMetadata()
                );

        /// <summary>
        /// 同期グループに追加するメソッド
        /// </summary>
        /// <param name = "groupName"> グループ名 </param>
        /// <returns> 成功したかどうか </returns>
        private bool AddSyncGroup(string groupName)
        {
            if (string.IsNullOrEmpty(ScrollGroup))
            {
                return false;
            }
            if (SyncGroups.ContainsKey(groupName) == false)
            {
                SyncGroups.Add(groupName, new List<Control>());
            }
            SyncGroups[groupName].Add(AssociatedObject);

            var sv = AssociatedObject as ScrollViewer;
            var sb = AssociatedObject as ScrollBar;

            if (sv != null)
            {
                AddEvent<ScrollViewer>(ScrollViewer.HorizontalOffsetProperty, ScrollViewerScrolled);
                AddEvent<ScrollViewer>(ScrollViewer.VerticalOffsetProperty, ScrollViewerScrolled);
            }
            else if (sb != null)
            {
                AddEvent<ScrollBar>(RangeBase.ValueProperty, ScrollBarScrolled);
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 同期グループから削除するメソッド
        /// </summary>
        /// <param name = "groupName"> グループ名 </param>
        /// <returns> 成功したかどうか </returns>
        private bool RemoveSyncGroup(string groupName)
        {
            if (string.IsNullOrEmpty(ScrollGroup))
            {
                return false;
            }
            var sv = AssociatedObject as ScrollViewer;
            var sb = AssociatedObject as ScrollBar;

            if (sv != null)
            {
                RemoveEvent<ScrollViewer>(ScrollViewer.HorizontalOffsetProperty, ScrollViewerScrolled);
                RemoveEvent<ScrollViewer>(ScrollViewer.VerticalOffsetProperty, ScrollViewerScrolled);
            }
            else if (sb != null)
            {
                RemoveEvent<ScrollBar>(RangeBase.ValueProperty, ScrollBarScrolled);
            }
            else
            {
                return false;
            }

            SyncGroups[groupName].Remove(AssociatedObject);
            if (SyncGroups[groupName].Count == 0)
            {
                SyncGroups.Remove(groupName);
            }

            return true;
        }

        /// <summary>
        /// ScrollViewerの場合の変更通知イベントハンドラ
        /// </summary>
        /// <param name = "sender"> </param>
        /// <param name = "e"> </param>
        private void ScrollViewerScrolled(object sender, EventArgs e)
        {
            var sv = (ScrollViewer) sender;
            UpdateScrollValue(sender, Orientation == Orientation.Horizontal ? sv.HorizontalOffset : sv.VerticalOffset);
        }


        /// <summary>
        /// ScrollBarの場合の変更通知イベントハンドラ
        /// </summary>
        /// <param name = "sender"> </param>
        /// <param name = "e"> </param>
        private void ScrollBarScrolled(object sender, EventArgs e)
        {
            UpdateScrollValue(sender, ((ScrollBar) sender).Value);
        }

        /// <summary>
        /// スクロール値を設定するメソッド
        /// </summary>
        /// <param name = "sender"> スクロール値を更新してきたコントロール </param>
        /// <param name = "newValue"> 新しいスクロール値 </param>
        private void UpdateScrollValue(object sender, double newValue)
        {
            IEnumerable<Control> others = SyncGroups[ScrollGroup].Where(p => !Equals(p, sender));

            foreach (var sb in others.OfType<ScrollBar>().Where(p => p.Orientation == Orientation))
            {
                sb.Value = newValue;
            }
            foreach (var sv in others.OfType<ScrollViewer>())
            {
                if (Orientation == Orientation.Horizontal)
                {
                    sv.ScrollToHorizontalOffset(newValue);
                }
                else
                {
                    sv.ScrollToVerticalOffset(newValue);
                }
            }
        }
    }
}
