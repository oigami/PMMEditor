using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Livet;
using PMMEditor.MMDFileParser;
using PMMEditor.Models;
using Reactive.Bindings.Extensions;
using SharpDX;
using SharpDX.D3DCompiler;
using Direct3D11 = SharpDX.Direct3D11;
using Direct3D = SharpDX.Direct3D;
using Format = SharpDX.DXGI.Format;

namespace PMMEditor.Views.Documents
{
    public interface IInitializable
    {
        void Initialize(Direct3D11.Device device);
    }

    public interface IRenderer : IInitializable
    {
        void Render(Direct3D11.DeviceContext context);
    }

    public class MmdModelRendererSource : IDisposable
    {
        private Direct3D11.Device _device;

        public Direct3D11.Device Device
        {
            get { return _device; }
            set
            {
                if (Equals(_device, value))
                {
                    return;
                }
                _device = value;
                D3DObjectCompositeDisposable.Dispose();
                D3DObjectCompositeDisposable = new LivetCompositeDisposable();
                CreateData();
            }
        }

        private LivetCompositeDisposable D3DObjectCompositeDisposable = new LivetCompositeDisposable();
        private MmdModelModel _model;

        private Direct3D11.Buffer _verteBuffer;

        public Direct3D11.VertexBufferBinding _vertexBufferBinding { get; private set; }

        public Direct3D11.Buffer _indexBuffer { get; private set; }

        public List<Material> Materials { get; private set; }

        public class Material
        {
            public int IndexStart { get; set; }

            public int IndexNum { get; set; }

            public Direct3D11.Buffer PixelConstantBuffer0 { get; set; }
        }

        private struct Vertex
        {
            public Vector4 Position { get; set; }
        }

        public MmdModelRendererSource(MmdModelModel model)
        {
            _model = model;
        }

        private void CreateData()
        {
            if (_device == null)
            {
                return;
            }
            var data = Pmd.ReadFile("C:/tool/MikuMikuDance_v926x64/UserFile/Model/初音ミク.pmd");

            Materials = new List<Material>(data.Materials.Count);
            int preIndex = 0;
            foreach (var material in data.Materials)
            {
                var diffuse = new Color4(new Color3(material.Diffuse.R, material.Diffuse.G, material.Diffuse.B),
                                         material.DiffuseAlpha);
                Materials.Add(new Material
                {
                    IndexNum = (int) material.FaceVertexCount,
                    IndexStart = preIndex,
                    PixelConstantBuffer0 =
                        Direct3D11.Buffer.Create(_device, Direct3D11.BindFlags.ConstantBuffer, ref diffuse, 0,
                                                 Direct3D11.ResourceUsage.Immutable).AddTo(D3DObjectCompositeDisposable)
                });
                preIndex += (int) material.FaceVertexCount;
            }

            // 頂点データ生成
            var typeSize = Utilities.SizeOf<Vertex>();
            _verteBuffer = Direct3D11.Buffer.Create(
                _device,
                data.Vertices.Select(_ => new Vertex
                {
                    Position = new Vector4(_.Position.X, _.Position.Y, _.Position.Z, 1.0f)
                }).ToArray(),
                new Direct3D11.BufferDescription
                {
                    SizeInBytes = typeSize * data.Vertices.Count,
                    Usage = Direct3D11.ResourceUsage.Immutable,
                    BindFlags = Direct3D11.BindFlags.VertexBuffer,
                    StructureByteStride = typeSize
                }).AddTo(D3DObjectCompositeDisposable);
            _vertexBufferBinding = new Direct3D11.VertexBufferBinding(_verteBuffer, typeSize, 0);

            // 頂点インデックス生成
            var indexNum = data.VertexIndex.Count;
            _indexBuffer = Direct3D11.Buffer.Create(_device, data.VertexIndex.ToArray(),
                                                    new Direct3D11.BufferDescription
                                                    {
                                                        SizeInBytes = Utilities.SizeOf<ushort>() * indexNum,
                                                        Usage = Direct3D11.ResourceUsage.Immutable,
                                                        BindFlags = Direct3D11.BindFlags.IndexBuffer,
                                                        StructureByteStride = Utilities.SizeOf<ushort>()
                                                    }).AddTo(D3DObjectCompositeDisposable);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    D3DObjectCompositeDisposable.Dispose();
                    D3DObjectCompositeDisposable = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion


    }

    public class MmdModelRenderer : ViewModel, IRenderer
    {
        public MmdModelRendererSource Model
        {
            get { return _model; }
            set
            {
                if (_model == value)
                {
                    return;
                }
                _model = value;
                if (_model != null)
                {
                    _model.Device = _device;
                }
            }
        }

        private Direct3D11.Device _device;

        private Direct3D11.InputLayout _inputLayout;
        private Direct3D11.VertexShader _vertexShader;
        private Direct3D11.PixelShader _pixelShader;
        private Direct3D11.Buffer _viewProjConstantBuffer;

        private MmdModelRendererSource _model;

        // TODO: test実装
        public MmdModelRenderer() : this(new MmdModelRendererSource(null)) {}

        public MmdModelRenderer(MmdModelRendererSource model)
        {
            Model = model;
        }

        public MmdModelRenderer(MmdModelModel model) : this(new MmdModelRendererSource(model)) {}

        public void Initialize(Direct3D11.Device device)
        {
            _device = device;
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
            _inputLayout = new Direct3D11.InputLayout(_device, inputSignature, new[]
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

            var m = Matrix.LookAtLH(new Vector3(0, 0, -50), new Vector3(0, 0, 0), Vector3.Up);
            m *= Matrix.PerspectiveFovLH((float) Math.PI / 3, 1.4f, 0.1f, 10000000f);
            m.Transpose();
            _viewProjConstantBuffer =
                Direct3D11.Buffer.Create(_device, Direct3D11.BindFlags.ConstantBuffer, ref m, 0,
                                         Direct3D11.ResourceUsage.Immutable).AddTo(CompositeDisposable);

            if (Model != null)
            {
                Model.Device = _device;
            }
        }

        public void Render(Direct3D11.DeviceContext target)
        {
            target.InputAssembler.InputLayout = _inputLayout;

            target.VertexShader.Set(_vertexShader);
            target.VertexShader.SetConstantBuffer(0, _viewProjConstantBuffer);
            target.InputAssembler.SetVertexBuffers(0, Model._vertexBufferBinding);
            target.InputAssembler.SetIndexBuffer(Model._indexBuffer, Format.R16_UInt, 0);

            target.PixelShader.Set(_pixelShader);

            target.InputAssembler.PrimitiveTopology = Direct3D.PrimitiveTopology.TriangleList;
            foreach (var material in Model.Materials)
            {
                target.PixelShader.SetConstantBuffer(0, material.PixelConstantBuffer0);
                target.DrawIndexed(material.IndexNum, material.IndexStart, 0);
            }
        }
    }
}
