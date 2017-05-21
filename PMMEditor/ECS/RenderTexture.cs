using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings.Extensions;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Format = SharpDX.DXGI.Format;

namespace PMMEditor.ECS
{
    public class RenderTexture : IDisposable
    {
        public static RenderTexture Active { get; set; }

        public RenderTargetView ColorBuffer
        {
            get
            {
                Create();
                return _colorBuffer;
            }
            private set => _colorBuffer = value;
        }

        public Texture2D ColorTexture { get; private set; }

        public DepthStencilView DepthBuffer
        {
            get
            {
                Create();
                return _depthBuffer;
            }
            private set => _depthBuffer = value;
        }

        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                _isCreated = false;
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                _isCreated = false;
            }
        }

        public int Depth
        {
            get => _depth;
            set
            {
                _depth = value;
                _isCreated = false;
            }
        }

        public Format Format
        {
            get => _format;
            set
            {
                _format = value;
                _isCreated = false;
            }
        }

        public bool GenerateMips
        {
            get => _generateMips;
            set
            {
                _generateMips = value;
                _isCreated = false;
            }
        }

        public bool UseMipMap
        {
            get => _useMipMap;
            set
            {
                _useMipMap = value;
                _isCreated = false;
            }
        }

        private bool _isCreated;
        private bool _disposedValue; // 重複する呼び出しを検出するには
        private int _width;
        private int _height;
        private int _depth;
        private Format _format = Format.B8G8R8A8_UNorm;
        private bool _generateMips = true;
        private bool _useMipMap = true;
        private DepthStencilView _depthBuffer;
        private RenderTargetView _colorBuffer;

        public bool Create()
        {
            if (_isCreated)
            {
                return false;
            }

            Release();
            var renderDesc = new Texture2DDescription
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format,
                Width = Width,
                Height = Height,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.None,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };
            // TODO: 非同期の場合のロック処理
            Device device = ECSystem.Device;
            ColorTexture = new Texture2D(device, renderDesc);
            ColorBuffer = new RenderTargetView(device, ColorTexture);

            var zBufferTextureDescription = new Texture2DDescription
            {
                Format = Depth == 16 ? Format.R16_Float : Depth == 24 ? Format.R24G8_Typeless : Format.D32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = Width,
                Height = Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            var zBufferTexture = new Texture2D(device, zBufferTextureDescription);
            {
                DepthBuffer =
                    new DepthStencilView(device, zBufferTexture, new DepthStencilViewDescription
                    {
                        Format = Depth == 24 ? Format.D24_UNorm_S8_UInt : zBufferTextureDescription.Format,
                        Dimension = DepthStencilViewDimension.Texture2D
                    });
            }
            _isCreated = true;

            Active = this;

            return true;
        }

        public void Release()
        {
            _colorBuffer?.Dispose();
            _colorBuffer = null;

            ColorTexture?.Dispose();
            ColorTexture = null;

            _depthBuffer?.Dispose();
            _depthBuffer = null;

            _isCreated = false;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing) { }
                Release();

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator Texture2D(RenderTexture v)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
