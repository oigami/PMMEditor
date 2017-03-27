using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using PMMEditor.ViewModels.Graphics;
using SharpDX.Direct3D11;

namespace PMMEditor.Views.Graphics
{
    public class MmdModelItemsControl : ContentControl
    {
        #region ItemsSource

        public IEnumerable<IRenderer> ItemsSource
        {
            get { return (IEnumerable<IRenderer>) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), typeof(IEnumerable<IRenderer>),
                typeof(MmdModelItemsControl),
                new PropertyMetadata(null, (_, __) => ((MmdModelItemsControl) _).OnItemsSourceChanged(__)));

        #endregion

        #region Device

        public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(
            "Device", typeof(Device), typeof(MmdModelItemsControl),
            new PropertyMetadata(default(Device),
                                 (_, __) => ((MmdModelItemsControl) _).OnDeviceChangedCallback(__)));

        private void OnDeviceChangedCallback(DependencyPropertyChangedEventArgs args)
        {
            if (_renderer != null)
            {
                _renderer.Device = (Device) args.NewValue;
            }
        }

        public Device Device
        {
            get { return (Device) GetValue(DeviceProperty); }
            set { SetValue(DeviceProperty, value); }
        }

        #endregion

        #region ItemsPanel

        public static readonly DependencyProperty ItemsPanelProperty = DependencyProperty.Register(
            nameof(ItemsPanel), typeof(ControlTemplate), typeof(MmdModelItemsControl),
            new PropertyMetadata(default(ControlTemplate),
                                 (_, __) => ((MmdModelItemsControl) _).ItemsPanelChangedCallback(__)));

        private void ItemsPanelChangedCallback(DependencyPropertyChangedEventArgs args)
        {
            if (ItemsPanel == null)
            {
                Content = _renderer = new MainRenderer();
            }
            else
            {
                Content = _renderer = (RendererPanel) ItemsPanel.LoadContent();
            }
            _renderer.Device = Device;
            if (ItemsSource != null)
            {
                foreach (var renderer in ItemsSource)
                {
                    _renderer.Children.Add(renderer);
                }
            }
        }

        public ControlTemplate ItemsPanel
        {
            get { return (ControlTemplate) GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        #endregion

        private void OnItemsSourceChanged(DependencyPropertyChangedEventArgs args)
        {
            {
                if (args.OldValue is INotifyCollectionChanged oldChanged)
                {
                    oldChanged.CollectionChanged -= ChangedOnCollectionChanged;
                }

                _renderer?.Children?.Clear();
            }


            if (args.NewValue is INotifyCollectionChanged changed)
            {
                changed.CollectionChanged += ChangedOnCollectionChanged;
            }
            if (_renderer != null)
            {
                foreach (var item in (IEnumerable<IRenderer>) args.NewValue)
                {
                    _renderer.Children.Add(item);
                }
            }
        }

        private void ChangedOnCollectionChanged(
            object sender, NotifyCollectionChangedEventArgs args)
        {
            if (_renderer == null)
            {
                return;
            }
            int index = args.NewStartingIndex;
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (IRenderer argsNewItem in args.NewItems)
                    {
                        _renderer.Children.Insert(index++, argsNewItem);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (IRenderer item in args.OldItems)
                    {
                        _renderer.Children.Remove(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _renderer.Children.Clear();
                    foreach (var item in ItemsSource)
                    {
                        _renderer.Children.Add(item);
                    }
                    break;
                default:
                    // TODO: Replace, Moveの処理
                    throw new NotImplementedException("Replace, Moveの処理");
            }
        }

        private RendererPanel _renderer = new MainRenderer();

        public MmdModelItemsControl()
        {
            Content = _renderer;
        }
    }
}
