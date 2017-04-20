using SharpDX.Direct3D;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Media;
using Reactive.Bindings.Extensions;
using SharpDX;
using SharpDX.Mathematics.Interop;
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
            if (!Resources.TryGetValue(key, out T res))
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
                T res = Resources[key];
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
                string key = factory.Key;

                if (Resources.TryGetValue(key, out T resOld))
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
            Window window = Window.GetWindow(AssociatedObject);
            if (window != null)
            {
                window.Closed -= WindowClosedHandler;
            }
        }

        private void UserControlLoadedHandler(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(AssociatedObject);
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
        private CompositeDisposable _d3DRenderTargetCompositeDisposable = new CompositeDisposable();
        protected readonly CompositeDisposable AllCompositeDisposable = new CompositeDisposable();

        #region Device

        public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(
            nameof(Device), typeof(Device), typeof(SharpDxControl),
            new PropertyMetadata(default(Device), (_, __) => ((SharpDxControl) _).DevicePropertyChangedCallback(__)));

        private void DevicePropertyChangedCallback(DependencyPropertyChangedEventArgs args)
        {
            CreateAndBindTarget();
        }

        public Device Device
        {
            get { return (Device) GetValue(DeviceProperty); }
            set { SetValue(DeviceProperty, value); }
        }

        #endregion

        #region D3DSize

        private static void D3DSizePropertyChangedCallback(DependencyObject _, DependencyPropertyChangedEventArgs args)
        {
            ((SharpDxControl) _).CreateAndBindTarget();
        }

        #region D3DWidth

        public static readonly DependencyProperty D3DWidthProperty = DependencyProperty.Register(
            nameof(D3DWidth), typeof(double), typeof(SharpDxControl),
            new PropertyMetadata(double.NaN, D3DSizePropertyChangedCallback));


        public double D3DWidth
        {
            get { return (double) GetValue(D3DWidthProperty); }
            set { SetValue(D3DWidthProperty, value); }
        }

        #endregion

        #region D3DHeight

        public static readonly DependencyProperty D3DHeightProperty = DependencyProperty.Register(
            nameof(D3DHeight), typeof(double), typeof(SharpDxControl),
            new PropertyMetadata(double.NaN, D3DSizePropertyChangedCallback));

        public double D3DHeight
        {
            get { return (double) GetValue(D3DHeightProperty); }
            set { SetValue(D3DHeightProperty, value); }
        }

        #endregion

        #endregion

        public RenderTarget D2DRenderTarget;

        private Texture2D _renderTargetTexture;
        private D3D11Image _d3DSurface;
        private Factory _d2DFactory;
        private DepthStencilView _depthStencilView;
        private DepthStencilState _depthStencilState;
        private RenderTargetView _renderTarget;
        private float _actualWidth;
        private float _actualheight;


        private readonly Stopwatch _renderTimer = new Stopwatch();

        private bool _isInitialized;

        protected readonly ResourceManager<Brush> BrushManager = new ResourceManager<Brush>();
        private Query _query;
        private Texture2D _sharedTargetTexture;

        protected static bool IsInDesignMode
            => (bool) DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue;

        protected SharpDxControl()
        {
            if (!IsInDesignMode)
            {
                Loaded += OnLoadedEventHandler;
                AllCompositeDisposable.Add(Disposable.Create(() => Loaded -= OnLoadedEventHandler));
                Unloaded += OnUnloadedEventHandler;
                AllCompositeDisposable.Add(Disposable.Create(() => Unloaded -= OnUnloadedEventHandler));
                StartD3D();
            }
            Stretch = Stretch.Fill;
        }

        private void OnUnloadedEventHandler(object _, RoutedEventArgs __)
        {
            StopRendering();
        }

        private void OnLoadedEventHandler(object _, RoutedEventArgs __)
        {
            CreateAndBindTarget();
            StartRendering();
        }

        protected abstract void Render();
        protected abstract void Render2D();

        protected virtual void ResetRenderTarget() { }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!_renderTimer.IsRunning || !IsLoaded || !_isInitialized)
            {
                return;
            }

            CallRender();
            _d3DSurface.InvalidateD3DImage();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (IsLoaded && !double.IsNaN(D3DWidth) && !double.IsNaN(D3DHeight))
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
            _d3DSurface = new D3D11Image().AddTo(AllCompositeDisposable);

            _d3DSurface.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;
            AllCompositeDisposable.Add(
                Disposable.Create(
                    () => _d3DSurface.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged));


            _d2DFactory = new Factory().AddTo(AllCompositeDisposable);
            Source = _d3DSurface;

            CreateAndBindTarget();
            _isInitialized = true;
        }

        private void CreateAndBindTarget()
        {
            if (D3D9Instance.Instance == null)
            {
                return;
            }

            Device device = Device;
            if (device == null)
            {
                return;
            }
            if (IsInDesignMode)
            {
                return;
            }

            _query = new Query(device, new QueryDescription
            {
                Type = QueryType.Event
            });

            int width = Math.Max((int) ActualWidth, 100);
            int height = Math.Max((int) ActualHeight, 100);
            if (!double.IsNaN(D3DWidth))
            {
                width = (int) D3DWidth;
            }
            if (!double.IsNaN(D3DHeight))
            {
                height = (int) D3DHeight;
            }
            if (_renderTargetTexture != null &&
                _renderTargetTexture.Description.Width == width &&
                _renderTargetTexture.Description.Height == height)
            {
                return;
            }

            _actualWidth = width;
            _actualheight = height;

            _d3DSurface.ClearRenderTarget();
            _d3DRenderTargetCompositeDisposable.Dispose();
            _d3DRenderTargetCompositeDisposable = new CompositeDisposable();

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

            _renderTargetTexture = new Texture2D(device, renderDesc).AddTo(_d3DRenderTargetCompositeDisposable);
            _sharedTargetTexture = new Texture2D(device, renderDesc).AddTo(_d3DRenderTargetCompositeDisposable);
            Surface surface = _renderTargetTexture.QueryInterface<Surface>();

            var rtp = new RenderTargetProperties(new PixelFormat(Format.Unknown, AlphaMode.Premultiplied));
            D2DRenderTarget = new RenderTarget(_d2DFactory, surface, rtp).AddTo(_d3DRenderTargetCompositeDisposable);
            BrushManager.UpdateRenderTarget(D2DRenderTarget);

            _d3DSurface.SetRenderTarget(_sharedTargetTexture);

            // 深度バッファ
            var zBufferTextureDescription = new Texture2DDescription
            {
                Format = Format.D32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            using (var zBufferTexture = new Texture2D(device, zBufferTextureDescription))
            {
                _depthStencilView =
                    new DepthStencilView(device, zBufferTexture).AddTo(_d3DRenderTargetCompositeDisposable);
            }

            _depthStencilState
                = new DepthStencilState(device, new DepthStencilStateDescription
                {
                    IsDepthEnabled = true,
                    DepthComparison = Comparison.Less,
                    IsStencilEnabled = false,
                    DepthWriteMask = DepthWriteMask.All
                }).AddTo(_d3DRenderTargetCompositeDisposable);
            _renderTarget = new RenderTargetView(device, _renderTargetTexture).AddTo(_d3DRenderTargetCompositeDisposable);

            device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height);

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

            SharpDX.Direct3D11.DeviceContext context = Device.ImmediateContext;

            // 速度維持のために一つ前のフレームを描画する
            context.Begin(_query);
            context.ResolveSubresource(_renderTargetTexture, 0, _sharedTargetTexture, 0, Format.B8G8R8A8_UNorm);
            context.End(_query);
            context.Flush();

            context.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(_renderTarget, new RawColor4(0, 0, 0, 0));
            SetRenderTarget(context);

            Render();
            D2DRenderTarget.BeginDraw();
            Render2D();
            D2DRenderTarget.EndDraw();

            while (true)
            {
                DataStream data = context.GetData(_query, AsynchronousFlags.DoNotFlush);
                int isEnd = data.Read<int>();
                if (isEnd == 1)
                {
                    break;
                }
            }

        }

        protected void SetRenderTarget(SharpDX.Direct3D11.DeviceContext context)
        {
            context.Rasterizer.SetViewport(0, 0, _actualWidth, _actualheight);
            context.OutputMerger.SetDepthStencilState(_depthStencilState);
            context.OutputMerger.SetRenderTargets(_depthStencilView, _renderTarget);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Source = null;
                _d3DRenderTargetCompositeDisposable.Dispose();
                AllCompositeDisposable.Dispose();
            }
        }


        public void Dispose()
        {
            Dispose(true);
        }
    }
}
