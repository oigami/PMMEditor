using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Windows;
using System.Windows.Input;
using PMMEditor.Models;
using PMMEditor.Views.Graphics;
using Reactive.Bindings.Extensions;
using SharpDX;
using Point = System.Windows.Point;

namespace PMMEditor.Views.Behaviors
{
    internal class DefaultCameraMouseControlBehavior : BehaviorBase<RendererPanel>
    {
        private CompositeDisposable _compositeDisposable;

        protected override void OnSetup()
        {
            _compositeDisposable?.Dispose();
            _compositeDisposable = new CompositeDisposable();
            AssociatedObject.MouseDown += AssociatedObject_MouseDown;
            AssociatedObject.MouseMove += AssociatedObject_MouseMove;
            AssociatedObject.MouseUp += AssociatedObject_MouseUp;
            AssociatedObject.MouseWheel += AssociatedObject_MouseWheel;
        }

        protected override void OnCleanup()
        {
            _compositeDisposable.Dispose();
            AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
            AssociatedObject.MouseDown += AssociatedObject_MouseDown;
            AssociatedObject.MouseUp += AssociatedObject_MouseUp;
            AssociatedObject.MouseWheel -= AssociatedObject_MouseWheel;
        }

        private void AssociatedObject_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
        }

        private void AssociatedObject_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(AssociatedObject);
            _startedPoint = e.GetPosition(AssociatedObject);
        }

        private Point _startedPoint;

        private void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
        {
            if (CameraControl == null)
            {
                return;
            }

            Point pos = e.GetPosition(AssociatedObject);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                var rot = new Vector3((float) (pos.Y - _startedPoint.Y),
                                      (float) (pos.X - _startedPoint.X),
                                      0.0f);
                rot /= 300.0f;
                CameraControl.AddRotate(-rot);
                AssociatedObject.View = CameraControl.View;
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                var lookat = new Vector2(-(float) (pos.X - _startedPoint.X),
                                         (float) (pos.Y - _startedPoint.Y));
                lookat /= 10.0f;
                CameraControl.Transform(lookat);
                AssociatedObject.View = CameraControl.View;
            }
            _startedPoint = pos;
        }

        private void AssociatedObject_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (CameraControl == null)
            {
                return;
            }

            CameraControl.Distance -= e.Delta / 100.0f;
            AssociatedObject.View = CameraControl.View;
        }

        public CameraControlModel CameraControl
        {
            get { return (CameraControlModel) GetValue(_cameraControlProperty); }
            set { SetValue(_cameraControlProperty, value); }
        }

        private static readonly DependencyProperty _cameraControlProperty
            = DependencyProperty.Register(nameof(CameraControl),
                                          typeof(CameraControlModel),
                                          typeof(DefaultCameraMouseControlBehavior),
                                          new FrameworkPropertyMetadata((d, e) =>
                                          {
                                              if (e.NewValue == null)
                                              {
                                                  return;
                                              }

                                              var me = (DefaultCameraMouseControlBehavior) d;
                                              me.OnCameraControlChanged();
                                          })
                );

        private void OnCameraControlChanged()
        {
            if (AssociatedObject == null)
            {
                return;
            }

            AssociatedObject.View = CameraControl.View;
            AssociatedObject.Projection = CameraControl.CreateProjection();
            CameraControl.ObserveProperty(_ => _.View).Subscribe(view => AssociatedObject.View = view)
                         .AddTo(_compositeDisposable);
        }
    }
}
