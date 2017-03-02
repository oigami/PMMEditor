using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Resources;
using Livet;
using PMMEditor.MMDFileParser;
using PMMEditor.Models;
using PMMEditor.ViewModels.Documents;
using Reactive.Bindings.Extensions;
using SharpDX;
using Direct3D11 = SharpDX.Direct3D11;
using Direct3D = SharpDX.Direct3D;
using Format = SharpDX.DXGI.Format;
using SharpDX.D3DCompiler;
using SharpDX.Mathematics.Interop;

namespace PMMEditor.ViewModels.MMW
{
    public interface IInitializable
    {
        void Initialize(Direct3D11.Device device);
    }

    public interface IRender : IInitializable
    {
        void Render();
    }

    public class MmdModelViewModel : DocumentViewModelBase, IRender
    {
        private readonly MmdModelModel _model;
        private Direct3D11.Device _device;

        private Direct3D11.Buffer _verteBuffer;
        private Direct3D11.VertexBufferBinding _vertexBufferBinding;

        private Direct3D11.Buffer _indexBuffer;
        private int _indexNum;
        private Direct3D11.InputLayout _inputLayout;
        private Direct3D11.VertexShader _vertexShader;
        private Direct3D11.PixelShader _pixelShader;
        private Direct3D11.Buffer _viewProjConstantBuffer;

        public MmdModelViewModel(MmdModelModel model)
        {
            _model = model;
        }

        public void Initialize(Direct3D11.Device device)
        {
            _device = device;

            var data = Pmd.ReadFile("C:/tool/MikuMikuDance_v926x64/UserFile/Model/初音ミク.pmd");

            // 頂点データ生成
            var typeSize = Utilities.SizeOf<Vector4>();
            _verteBuffer = Direct3D11.Buffer.Create(
                device,
                data.Vertices.Select(_ => new Vector4(_.Position.X, _.Position.Y, _.Position.Z, 1.0f)).ToArray(),
                new Direct3D11.BufferDescription
                {
                    SizeInBytes = typeSize * data.Vertices.Count,
                    Usage = Direct3D11.ResourceUsage.Immutable,
                    BindFlags = Direct3D11.BindFlags.VertexBuffer,
                    StructureByteStride = typeSize
                }).AddTo(CompositeDisposable);
            _vertexBufferBinding = new Direct3D11.VertexBufferBinding(_verteBuffer, typeSize, 0);

            // 頂点インデックス生成
            _indexNum = data.VertexIndex.Count;
            _indexBuffer = Direct3D11.Buffer.Create(device, data.VertexIndex.ToArray(), new Direct3D11.BufferDescription
            {
                SizeInBytes = Utilities.SizeOf<ushort>() * _indexNum,
                Usage = Direct3D11.ResourceUsage.Immutable,
                BindFlags = Direct3D11.BindFlags.IndexBuffer,
                StructureByteStride = Utilities.SizeOf<ushort>()
            }).AddTo(CompositeDisposable);

            // 頂点シェーダ生成
            var shaderSource = Resource1.TestShader;
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
            _inputLayout = new Direct3D11.InputLayout(device, inputSignature, new[]
            {
                new Direct3D11.InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0)
            });

            // ピクセルシェーダ生成
            var pixelShaderByteCode = ShaderBytecode.Compile(shaderSource, "PS", "ps_4_0", ShaderFlags.Debug);
            if (pixelShaderByteCode.HasErrors)
            {
                Console.WriteLine(pixelShaderByteCode.Message);
                return;
            }
            _pixelShader = new Direct3D11.PixelShader(_device, pixelShaderByteCode);

            var m = Matrix.LookAtLH(new Vector3(0, 0, 100), new Vector3(0, 0, 0), Vector3.Up);
            m *= Matrix.PerspectiveFovLH((float) Math.PI / 4f, 1.4f, 0.0001f, 1000f);
            m.Transpose();
            _viewProjConstantBuffer =
                Direct3D11.Buffer.Create(device, Direct3D11.BindFlags.ConstantBuffer, ref m, 0,
                                         Direct3D11.ResourceUsage.Immutable).AddTo(CompositeDisposable);
        }


        public void Render()
        {
            if (_device == null)
            {
                return;
            }
            var target = _device.ImmediateContext;
            target.InputAssembler.InputLayout = _inputLayout;

            target.VertexShader.Set(_vertexShader);
            target.VertexShader.SetConstantBuffer(0, _viewProjConstantBuffer);
            target.InputAssembler.SetVertexBuffers(0, _vertexBufferBinding);
            target.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);

            target.PixelShader.Set(_pixelShader);

            target.InputAssembler.PrimitiveTopology = Direct3D.PrimitiveTopology.TriangleStrip;

            _device.ImmediateContext.DrawIndexed(_indexNum, 0, 0);
        }

        public override string Title { get; } = "test";

        public override string ContentId { get; } = "test";
    }
}
