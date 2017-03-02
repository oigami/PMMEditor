using SharpDX.Direct3D;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Media;
using SharpDX;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Brush = SharpDX.Direct2D1.Brush;
using Device = SharpDX.Direct3D11.Device;
using Factory = SharpDX.Direct2D1.Factory;
using Image = System.Windows.Controls.Image;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

namespace PMMEditor.SharpDxControl
{
    public class ResourceManager<T> where T : class
    {
        public Dictionary<string, T> Resources { get; } = new Dictionary<string, T>();

        public T this[string key] => Resources[key];

        private readonly Dictionary<string, Func<RenderTarget, T>> _factorys =
            new Dictionary<string, Func<RenderTarget, T>>();

        private RenderTarget _renderTarget;

        public void UpdateRenderTarget(RenderTarget renderTarget)
        {
            if (_renderTarget == renderTarget)
            {
                return;
            }
            _renderTarget = renderTarget;
            UpdateResources();
        }

        public void Add(string key, Func<RenderTarget, T> factory)
        {
            Remove(key);

            _factorys.Add(key, factory);
            Resources.Add(key, _renderTarget == null ? null : factory(_renderTarget));
        }

        public bool Remove(string key)
        {
            T res;
            if (!Resources.TryGetValue(key, out res))
            {
                return false;
            }
            SafeDispose(ref res);
            _factorys.Remove(key);
            Resources.Remove(key);
            return true;
        }

        public void Clear()
        {
            foreach (var key in Resources.Keys)
            {
                var res = Resources[key];
                SafeDispose(ref res);
            }
            _factorys.Clear();
            Resources.Clear();
        }

        private static void SafeDispose(ref T o)
        {
            var dis = o as IDisposable;
            Utilities.Dispose(ref dis);
            o = null;
        }

        private void UpdateResources()
        {
            if (_renderTarget == null)
            {
                return;
            }

            foreach (var factory in _factorys)
            {
                var key = factory.Key;

                T resOld;
                if (Resources.TryGetValue(key, out resOld))
                {
                    SafeDispose(ref resOld);
                    Resources.Remove(key);
                }

                Resources.Add(key, factory.Value(_renderTarget));
            }
        }
    }

    public sealed class SharpDxControlUnloadingEventBehavior : Behavior<SharpDxControl>
    {
        protected override void OnAttached()
        {
            AssociatedObject.Loaded += UserControlLoadedHandler;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= UserControlLoadedHandler;
            var window = Window.GetWindow(AssociatedObject);
            if (window != null)
            {
                window.Closed -= WindowClosedHandler;
            }
        }

        private void UserControlLoadedHandler(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(AssociatedObject);
            if (window == null)
            {
                throw new Exception(
                    $"The UserControl {AssociatedObject.GetType().Name} is not contained within a Window. The SharpDxControlUnloadingEventBehavior cannot be used.");
            }

            window.Closed += WindowClosedHandler;
        }

        private void WindowClosedHandler(object sender, EventArgs e)
        {
            OnUserControlClosing();
        }

        private void OnUserControlClosing()
        {
            AssociatedObject.Dispose();
        }
    }

    public abstract class SharpDxControl : Image, IDisposable
    {
        public Device Device;
        public RenderTarget D2DRenderTarget;

        private Texture2D _renderTarget;
        private D3D11Image _d3DSurface;
        private Factory _d2DFactory;

        private readonly Stopwatch _renderTimer = new Stopwatch();

        protected readonly ResourceManager<Brush> BrushManager = new ResourceManager<Brush>();

        protected SharpDxControl()
        {
            if (!(bool) DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue)
            {
                StartD3D();
                Loaded += (_, __) =>
                {
                    CreateAndBindTarget();
                    StartRendering();
                };
                Unloaded += (_, __) => StopRendering();
            }
            Stretch = Stretch.Fill;
        }

        ~SharpDxControl()
        {
            Dispose(false);
        }

        protected abstract void Render();
        protected virtual void ResetRenderTarget() {}

        private void OnRendering(object sender, EventArgs e)
        {
            if (!_renderTimer.IsRunning || !IsLoaded)
            {
                return;
            }

            CallRender();
            _d3DSurface.InvalidateD3DImage();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (IsLoaded)
            {
                CreateAndBindTarget();
            }
            base.OnRenderSizeChanged(sizeInfo);
        }

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_d3DSurface.IsFrontBufferAvailable)
            {
                StartRendering();
            }
            else
            {
                StopRendering();
            }
        }

        private void StartD3D()
        {
            Device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

            _d3DSurface = new D3D11Image();
            _d3DSurface.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

            _d2DFactory = new Factory();

            Source = _d3DSurface;
        }

        private void CreateAndBindTarget()
        {
            _d3DSurface.ClearRenderTarget();

            Utilities.Dispose(ref D2DRenderTarget);
            Utilities.Dispose(ref _renderTarget);

            var width = Math.Max((int) ActualWidth, 100);
            var height = Math.Max((int) ActualHeight, 100);

            var renderDesc = new Texture2DDescription
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.Shared,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };

            _renderTarget = new Texture2D(Device, renderDesc);

            var surface = _renderTarget.QueryInterface<Surface>();

            var rtp = new RenderTargetProperties(new PixelFormat(Format.Unknown, AlphaMode.Premultiplied));
            D2DRenderTarget = new RenderTarget(_d2DFactory, surface, rtp);
            BrushManager.UpdateRenderTarget(D2DRenderTarget);

            _d3DSurface.SetRenderTarget(_renderTarget);
            Device.ImmediateContext.OutputMerger.SetRenderTargets(new RenderTargetView(Device, _renderTarget));
            Device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height);

            ResetRenderTarget();
        }


        private void StartRendering()
        {
            if (_renderTimer.IsRunning)
            {
                return;
            }

            CompositionTarget.Rendering += OnRendering;
            _renderTimer.Start();
        }

        private void StopRendering()
        {
            if (!_renderTimer.IsRunning)
            {
                return;
            }

            _renderTimer.Stop();
            CompositionTarget.Rendering -= OnRendering;
        }

        private void CallRender()
        {
            if (D2DRenderTarget == null)
            {
                return;
            }

            D2DRenderTarget.BeginDraw();
            Render();
            D2DRenderTarget.EndDraw();

            Device.ImmediateContext.Flush();
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _d3DSurface.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
                Source = null;
                Device?.Dispose();
                D2DRenderTarget?.Dispose();
                _renderTarget?.Dispose();
                _d3DSurface?.Dispose();
                _d2DFactory?.Dispose();
            }
        }

        /// <summary> Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
