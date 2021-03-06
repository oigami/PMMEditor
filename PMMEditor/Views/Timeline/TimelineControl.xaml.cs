﻿using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PMMEditor.Views.Timeline
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
                                                                          .AffectsParentArrange,
                                                                      (o, e) =>
                                                                          TimelinePanel.SetIndex(o, (int) e.NewValue)));

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
                SelectionChanged?.Invoke(this, value);
            }
        }

        public delegate void SelectionChangedDelegate(object sender, bool isSelected);

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
    public class TimelineControlBase : MultiSelector { }

    public partial class TimelineControl
    {
        public TimelineControl()
        {
            InitializeComponent();
            CanSelectMultipleItems = true;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            var res = new TimelineItem();
            res.SelectionChanged += (s, selected) =>
            {
                BeginUpdateSelectedItems();
                if (selected)
                {
                    SelectedItems.Add(res);
                }
                else
                {
                    SelectedItems.Remove(res);
                }
                EndUpdateSelectedItems();
            };
            return res;
        }

        new public void UnselectAll()
        {
            foreach (var timelineItem in SelectedItems.Cast<TimelineItem>().ToList())
            {
                timelineItem.IsSelected = false;
            }
        }
    }
}
