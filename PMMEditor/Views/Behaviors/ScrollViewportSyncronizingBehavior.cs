using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using Reactive.Bindings.Extensions;

namespace PMMEditor.Views.Behaviors
{
    public interface IVirtualizingPanel
    {
        Rect Viewport { get; set; }
    }

    public class ScrollViewportSyncronizingBehavior : BehaviorBase<FrameworkElement>
    {
        private static readonly Dictionary<string, List<IVirtualizingPanel>> _syncGroups =
            new Dictionary<string, List<IVirtualizingPanel>>();

        private static readonly Dictionary<string, ScrollViewer> _scrollViewers = new Dictionary<string, ScrollViewer>();

        private void AddEvent(DependencyProperty prop)
        {
            DependencyPropertyDescriptor.FromProperty(prop, typeof(ScrollViewer))
                                        .AddValueChanged(AssociatedObject, OnViewportChanged);
        }

        private void RemoveEvent(DependencyProperty prop)
        {
            DependencyPropertyDescriptor.FromProperty(prop, typeof(ScrollViewer))
                                        .RemoveValueChanged(AssociatedObject, OnViewportChanged);
        }

        private void OnViewportChanged(object sender, EventArgs e)
        {
            var sv = (ScrollViewer) sender;

            foreach (var panel in _syncGroups[""])
            {
                panel.Viewport = new Rect(sv.HorizontalOffset,
                                          sv.VerticalOffset,
                                          sv.ViewportWidth,
                                          sv.ViewportHeight);
            }
        }

        protected override void OnSetup()
        {
            if (!_syncGroups.ContainsKey(""))
            {
                _syncGroups.Add("", new List<IVirtualizingPanel>());
            }
            if (AssociatedObject is ScrollViewer sv)
            {
                _scrollViewers[""] = sv;
                AddEvent(ScrollViewer.HorizontalOffsetProperty);
                AddEvent(ScrollViewer.VerticalOffsetProperty);
                AddEvent(ScrollViewer.ViewportHeightProperty);
                AddEvent(ScrollViewer.ViewportWidthProperty);
            }
            else if (AssociatedObject is IVirtualizingPanel panel)
            {
                _syncGroups[""].Add(panel);
                if (_scrollViewers[""] == null)
                {
                    return;
                }
                sv = _scrollViewers[""];
                panel.Viewport = new Rect(sv.HorizontalOffset,
                                          sv.VerticalOffset,
                                          sv.ViewportWidth,
                                          sv.ViewportHeight);
            }
        }

        protected override void OnCleanup()
        {
            if (AssociatedObject is ScrollViewer sv)
            {
                RemoveEvent(ScrollViewer.HorizontalOffsetProperty);
                RemoveEvent(ScrollViewer.VerticalOffsetProperty);
                RemoveEvent(ScrollViewer.ViewportHeightProperty);
                RemoveEvent(ScrollViewer.ViewportWidthProperty);
            }
            else
            {
                _syncGroups[""].Remove((IVirtualizingPanel) AssociatedObject);
            }
        }
    }
}
