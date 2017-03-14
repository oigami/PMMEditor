using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.MMDFileParser;
using PMMEditor.Models.Graphics;
using PMMEditor.MVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using Direct3D11 = SharpDX.Direct3D11;

namespace PMMEditor.ViewModels.Graphics
{
    public class BoneRenderer : BindableDisposableBase, IRenderer
    {
        private Direct3D11.Buffer _vertexBuffer;
        private Direct3D11.VertexBufferBinding _vertexBufferBinding;

        private struct Vertex
        {
            public Int3 Id;
            public int IsBegin; // 0 or 1
        }

        public readonly ReadOnlyReactiveProperty<bool> IsInitialized;
        private readonly MmdModelRendererSource _model;
        private readonly Direct3D11.Device _device;
        private int _numVertex;
        private Direct3D11.DepthStencilState _boneRenderDepthState;
        private Direct3D11.VertexShader _vertexShader;
        private Direct3D11.InputLayout _inputLayout;
        private Direct3D11.PixelShader _pixelShader;
        private bool _isInitialized;

        private bool IsInternalInitialized
        {
            get { return _isInitialized; }
            set { SetProperty(ref _isInitialized, value); }
        }

        private void InitializeInternal()
        {
            var boneNum = _model.Model.BoneKeyList.Count;
            // 頂点バッファ生成
            var vertexArr = _model.Model.BoneKeyList.SelectMany(_ =>
            {
                if (_.Type == PmdStruct.BoneKind.Invisible
                    || _.TailChildIndex == -1)
                {
                    return Enumerable.Empty<Vertex>();
                }
                var now = (_.Index + boneNum) * 4;
                var nowX = now % 1024;
                var nowY = now / 1024;

                var next = (_.TailChildIndex + boneNum) * 4;
                var nextX = next % 1024;
                var nextY = next / 1024;

                return new[]
                {
                    new Vertex {Id = new Int3(nowX, nowY, 0), IsBegin = 1},
                    new Vertex {Id = new Int3(nextX, nextY, 0), IsBegin = 0}
                };
            }).ToArray();
            _numVertex = vertexArr.Length;
            _vertexBuffer = Direct3D11.Buffer.Create(_device, vertexArr, new Direct3D11.BufferDescription
            {
                BindFlags = Direct3D11.BindFlags.VertexBuffer,
                CpuAccessFlags = Direct3D11.CpuAccessFlags.None,
                OptionFlags = Direct3D11.ResourceOptionFlags.None,
                SizeInBytes = Utilities.SizeOf<Vertex>() * _numVertex,
                StructureByteStride = Utilities.SizeOf<Vertex>(),
                Usage = Direct3D11.ResourceUsage.Immutable
            }).AddTo(CompositeDisposable);
            _vertexBufferBinding = new Direct3D11.VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vertex>(),
                                                                      0);
            _boneRenderDepthState = new Direct3D11.DepthStencilState(
                _device,
                new Direct3D11.DepthStencilStateDescription
                {
                    IsDepthEnabled = false,
                    IsStencilEnabled = false
                });

            // 頂点シェーダ生成
            var shaderSource = Resource1.BoneRenderingShader;
            // UTF-8 BOMチェック
            if (3 <= shaderSource.Length
                && shaderSource[0] == 0xEF && shaderSource[1] == 0xBB && shaderSource[2] == 0xBF)
            {
                for (int i = 0; i < 3; i++)
                {
                    shaderSource[i] = (byte) ' ';
                }
            }
            var vertexShaderByteCode = ShaderBytecode.Compile(shaderSource, "VS", "vs_4_0", ShaderFlags.Debug);
            if (vertexShaderByteCode.HasErrors)
            {
                Console.WriteLine(vertexShaderByteCode.Message);
                return;
            }
            _vertexShader = new Direct3D11.VertexShader(_device, vertexShaderByteCode);

            // インプットレイアウト生成
            var inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            _inputLayout = new Direct3D11.InputLayout(_device, inputSignature, new[]
            {
                new Direct3D11.InputElement("BONE_INDEX", 0, Format.R32G32B32_SInt, 0, 0),
                new Direct3D11.InputElement("IS_BEGIN", 0, Format.R32_SInt, 12, 0)
            });

            // ピクセルシェーダ生成
            var pixelShaderByteCode = ShaderBytecode.Compile(shaderSource, "PS", "ps_4_0", ShaderFlags.Debug);
            if (pixelShaderByteCode.HasErrors)
            {
                Console.WriteLine(pixelShaderByteCode.Message);
                return;
            }
            _pixelShader = new Direct3D11.PixelShader(_device, pixelShaderByteCode);

            IsInternalInitialized = true;
        }

        public BoneRenderer(MmdModelRendererSource model, Direct3D11.Device device)
        {
            IsInitialized = this.ObserveProperty(_ => _.IsInternalInitialized).ToReadOnlyReactiveProperty();
            _model = model;
            _device = device;
            Task.Run(() => InitializeInternal());
        }

        public void Initialize(Direct3D11.Device device) {}

        public void Render(Direct3D11.DeviceContext context)
        {
            if (IsInitialized.Value == false)
            {
                return;
            }
            context.InputAssembler.InputLayout = _inputLayout;

            var depthState = context.OutputMerger.DepthStencilState;
            context.OutputMerger.DepthStencilState = _boneRenderDepthState;

            context.VertexShader.Set(_vertexShader);
            context.InputAssembler.SetVertexBuffers(0, _vertexBufferBinding);

            context.PixelShader.Set(_pixelShader);

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            context.Draw(_numVertex, 0);
            context.OutputMerger.DepthStencilState = depthState;
        }
    }
}
