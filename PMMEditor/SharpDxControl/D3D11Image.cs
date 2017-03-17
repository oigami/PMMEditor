using SharpDX.Direct3D9;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using SharpDX;
using SharpDX.Direct3D11;
using Resource = SharpDX.DXGI.Resource;
using System.Reactive.Disposables;

namespace PMMEditor.SharpDxControl
{
    public class D3D11Image : D3DImage, IDisposable
    {
        private readonly CompositeDisposable CompositeDisposable = new CompositeDisposable();
        private Texture _renderTarget;

        private void OnDeviceLostCheck(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsFrontBufferAvailable == false)
            {
                return;
            }
            D3D9Instance.Instance.CheckDeviceLost();

            var renderTarget = _renderTarget;
            if (renderTarget == null)
            {
                return;
            }

            using (var surface = renderTarget.GetSurfaceLevel(0))
            {
                SetBackBuffer(surface.NativePointer);
            }
        }

        public void InvalidateD3DImage()
        {
            IsFrontBufferAvailableChanged += OnDeviceLostCheck;
            CompositeDisposable.Add(Disposable.Create(() => IsFrontBufferAvailableChanged -= OnDeviceLostCheck));

            if (_renderTarget == null)
            {
                return;
            }
            Lock();
            AddDirtyRect(new Int32Rect(0, 0, PixelWidth, PixelHeight));
            Unlock();
        }

        public void ClearRenderTarget()
        {
            if (_renderTarget != null)
            {
                Utilities.Dispose(ref _renderTarget);

                SetBackBuffer(IntPtr.Zero);
            }
        }

        private void SetBackBuffer(IntPtr surfacePtr)
        {
            Lock();
            SetBackBuffer(D3DResourceType.IDirect3DSurface9, surfacePtr);
            Unlock();
        }

        public void SetRenderTarget(Texture2D target)
        {
            ClearRenderTarget();

            if (target == null)
            {
                return;
            }
            Debug.Assert(IsShareable(target));

            var format = TranslateFormat(target);
            Debug.Assert(format != Format.Unknown);

            var handle = GetSharedHandle(target);
            Debug.Assert(handle != IntPtr.Zero);

            _renderTarget = new Texture(D3D9Instance.Instance.D3DDevice,
                                        target.Description.Width, target.Description.Height, 1,
                                        Usage.RenderTarget, format, Pool.Default, ref handle);

            using (var surface = _renderTarget.GetSurfaceLevel(0))
            {
                SetBackBuffer(surface.NativePointer);
            }
        }

        private static IntPtr GetSharedHandle(Texture2D texture)
        {
            using (var resource = texture.QueryInterface<Resource>())
            {
                return resource.SharedHandle;
            }
        }

        private static Format TranslateFormat(Texture2D texture)
        {
            switch (texture.Description.Format)
            {
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                    return Format.A2B10G10R10;
                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                    return Format.A16B16G16R16F;
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                    return Format.A8R8G8B8;
                default:
                    return Format.Unknown;
            }
        }

        private static bool IsShareable(Texture2D texture)
        {
            return (texture.Description.OptionFlags & ResourceOptionFlags.Shared) != 0;
        }

        #region Dispose

        private bool _disposed;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_renderTarget")]
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (disposing)
            {
                CompositeDisposable.Dispose();
                _renderTarget?.Dispose();
            }
        }

        /// <summary> Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees resources and performs other cleanup operations before the
        /// <see cref = "T:System.Windows.Interop.D3DImage" /> is reclaimed by garbage collection.
        /// </summary>
        ~D3D11Image()
        {
            Dispose(false);
        }

        #endregion
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = false)]
        internal static extern IntPtr GetDesktopWindow();
    }
}
