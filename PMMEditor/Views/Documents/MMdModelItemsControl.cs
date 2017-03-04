using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PMMEditor.Views.Documents
{
    internal class MmdModelItemsControl : ContentControl
    {
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), typeof(IEnumerable),
                typeof(MmdModelItemsControl),
                new PropertyMetadata(null, (_, __) => ((MmdModelItemsControl) _).OnItemsSourceChanged(__)));

        private void OnItemsSourceChanged(DependencyPropertyChangedEventArgs args)
        {
            {
                var oldChanged = args.OldValue as INotifyCollectionChanged;
                if (oldChanged != null)
                {
                    oldChanged.CollectionChanged -= ChangedOnCollectionChanged;
                }

                _renderer.Children.Clear();
            }


            var changed = args.NewValue as INotifyCollectionChanged;
            if (changed != null)
            {
                changed.CollectionChanged += ChangedOnCollectionChanged;
            }
            foreach (IRenderer item in (IEnumerable) args.NewValue)
            {
                _renderer.Children.Add(item);
            }
        }

        private void ChangedOnCollectionChanged(
            object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs) {}

        private readonly MainRenderer _renderer = new MainRenderer();

        public MmdModelItemsControl()
        {
            Content = _renderer;
        }
    }
}
