using SharpDX.Direct3D;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Media;
using PMMEditor.Models.Graphics;
using PMMEditor.Models.Thread;
using Reactive.Bindings.Extensions;
using SharpDX;
using SharpDX.Mathematics.Interop;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Brush = SharpDX.Direct2D1.Brush;
using Device = SharpDX.Direct3D11.Device;
using DeviceContext = SharpDX.Direct3D11.DeviceContext;
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

    public class SharpDxControl : Image, IDisposable
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

        #region RenderTextureQueue

        public static readonly DependencyProperty RenderTextureQueueProperty = DependencyProperty.Register(
            nameof(RenderTextureQueue), typeof(RenderTextureQueue), typeof(SharpDxControl),
            new PropertyMetadata(default(RenderTextureQueue)));

        public RenderTextureQueue RenderTextureQueue
        {
            get => (RenderTextureQueue) GetValue(RenderTextureQueueProperty);
            set => SetValue(RenderTextureQueueProperty, value);
        }

        #endregion

        private D3D11Image _d3DSurface;
        private float _actualWidth;
        private float _actualheight;


        private readonly Stopwatch _renderTimer = new Stopwatch();

        private bool _isInitialized;

        private Query _query;
        private Texture2D _sharedTargetTexture;

        /// <summary>
        /// レンダリング時に同時にdeviceによるリソース作成が出来るかどうか
        /// </summary>
        private bool _useConcurrentCreates = true;

        /// <summary>
        /// 遅延コンテキストが使えるかどうか
        /// </summary>
        private bool _useDefferdContext;

        protected static bool IsInDesignMode
            => (bool) DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue;

        public SharpDxControl()
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

            device.CheckThreadingSupport(out _useConcurrentCreates, out _useDefferdContext);

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
            if (RenderTextureQueue != null)
            {
                RenderTextureQueue.Texture2DDescription = renderDesc;
            }
            _sharedTargetTexture = new Texture2D(device, renderDesc).AddTo(_d3DRenderTargetCompositeDisposable);

            _d3DSurface.SetRenderTarget(_sharedTargetTexture);

            device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height);
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

        private void Rendering()
        {
            ECS.RenderTexture queue = RenderTextureQueue?.DequeueTexture2D();
            if (queue == null)
            {
                return;
            }

            DeviceContext context = GraphicsModel.Device.ImmediateContext;
            lock (context)
            {
                context.Begin(_query);
                context.ResolveSubresource(queue.ColorTexture, 0, _sharedTargetTexture, 0, Format.B8G8R8A8_UNorm);
                //context.Flush();
                context.End(_query);

                while (true)
                {
                    using (DataStream data = context.GetData(_query, AsynchronousFlags.DoNotFlush))
                    {
                        int isEnd = data.Read<int>();
                        if (isEnd == 1)
                        {
                            break;
                        }
                    }
                }
            }

            RenderTextureQueue.EnqueueTexture2D(queue);
        }

        private void CallRender()
        {
            if (_useConcurrentCreates)
            {
                Rendering();
            }
            else
            {
                lock (GraphicsModel.SyncObject)
                {
                    Rendering();
                }
            }
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
