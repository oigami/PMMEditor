using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Livet;
using PMMEditor.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using Direct3D11 = SharpDX.Direct3D11;
using Direct3D = SharpDX.Direct3D;
using PMMEditor.Models.Graphics;
using PMMEditor.MVVM;

namespace PMMEditor.ViewModels.Graphics
{
    public interface IInitializable
    {
        void Initialize(Direct3D11.Device device);
    }

    public interface IRenderer : IInitializable
    {
        void Render(Direct3D11.DeviceContext context);
    }


    public class MmdModelBoneCalculatorSRV : BindableDisposableBase
    {
        private readonly MmdModelRendererSource _model;
        private readonly Direct3D11.Device _device;
        private Direct3D11.Texture2D _boneTexture2D;
        private Matrix[] _outputArr;

        public Direct3D11.ShaderResourceView BoneSrv { get; private set; }

        public MmdModelBoneCalculatorSRV(MmdModelRendererSource model, Direct3D11.Device device)
        {
            _device = device;
            _model = model;
            CreateData(model);
        }

        private void CreateData(MmdModelRendererSource model)
        {
            var boneNum = model.BoneCount * 2;
            _outputArr = new Matrix[boneNum];
            _boneTexture2D = new Direct3D11.Texture2D(
                _device,
                new Direct3D11.Texture2DDescription
                {
                    CpuAccessFlags = Direct3D11.CpuAccessFlags.None,
                    OptionFlags = Direct3D11.ResourceOptionFlags.None,
                    BindFlags = Direct3D11.BindFlags.ShaderResource,
                    ArraySize = 1,
                    MipLevels = 1,
                    Width = Math.Min(boneNum * 4, 1024),
                    Height = (boneNum * 4 + 1024 - 1) / 1024,
                    Usage = Direct3D11.ResourceUsage.Default,
                    SampleDescription = new SampleDescription(1, 0),
                    Format = Format.R32G32B32A32_Float
                }).AddTo(CompositeDisposable);
            BoneSrv = new Direct3D11.ShaderResourceView(_device, _boneTexture2D)
                .AddTo(CompositeDisposable);
        }

        public void Update(Direct3D11.DeviceContext context, int nowFrame)
        {
            var calclator = _model.BoneCalculator;
            calclator.Update(nowFrame);
            calclator.WorldBones.CopyTo(_outputArr, 0);
            calclator.ModelLocalBones.CopyTo(_outputArr, calclator.WorldBones.Length);
            context.UpdateSubresource(_outputArr, _boneTexture2D, 0, 16 * 1024, 16 * 1024, new Direct3D11.ResourceRegion
            {
                Left = 0,
                Top = 0,
                Right = _boneTexture2D.Description.Width,
                Bottom = _boneTexture2D.Description.Height,
                Front = 0,
                Back = 1
            });
        }
    }

    public class MmdModelRenderer : BindableDisposableBase, IRenderer
    {
        public MmdModelRendererSource ModelSource { get; }

        private readonly Model _model;
        private MmdModelBoneCalculatorSRV boneCalculator;
        private Direct3D11.Device _device;

        private Direct3D11.InputLayout _inputLayout;
        private Direct3D11.VertexShader _vertexShader;
        private Direct3D11.PixelShader _pixelShader;
        private Direct3D11.Buffer _viewProjConstantBuffer;

        private readonly ReadOnlyReactiveProperty<int> _nowFrame;
        private readonly ReactiveProperty<bool> _isInternalInitialized = new ReactiveProperty<bool>(false);

        public MmdModelRenderer(Model model, MmdModelRendererSource sourceModel)
        {
            _model = model;
            ModelSource = sourceModel;
            _nowFrame = model.FrameControlModel.ObserveProperty(_ => _.NowFrame).ToReadOnlyReactiveProperty()
                             .AddTo(CompositeDisposable);
            IsInitialized =
                ModelSource.ObserveProperty(_ => _.IsInitialized)
                           .CombineLatest(_isInternalInitialized.AsObservable(), (a, b) => a && b)
                           .ToReadOnlyReactiveProperty(false).AddTo(CompositeDisposable);
        }

        public ReadOnlyReactiveProperty<bool> IsInitialized { get; }

        private void InitializeInternal()
        {
            boneCalculator = new MmdModelBoneCalculatorSRV(ModelSource, _device)
                .AddTo(CompositeDisposable);
            BoneRenderer = new BoneRenderer(ModelSource, _device).AddTo(CompositeDisposable);

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
                new Direct3D11.InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new Direct3D11.InputElement("BONE_INDEX", 0, Format.R32G32B32A32_SInt, 16, 0),
                new Direct3D11.InputElement("BONE_WEIGHT", 0, Format.R32_Float, 32, 0)
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
                Direct3D11.Buffer.Create(_device, Direct3D11.BindFlags.ConstantBuffer, ref m).AddTo(CompositeDisposable);

            _isInternalInitialized.Value = true;
        }

        public BoneRenderer BoneRenderer { get; set; }

        public void Initialize(Direct3D11.Device device)
        {
            _device = device;
            Task.Run(() => InitializeInternal());
        }

        public void Render(Direct3D11.DeviceContext target)
        {
            if (IsInitialized.Value == false)
            {
                return;
            }

            // view,proj行列の設定
            var m = _model.Camera.CreateWorldViewProj();
            m.Transpose();
            target.UpdateSubresource(ref m, _viewProjConstantBuffer);

            // シェーダの設定
            target.InputAssembler.InputLayout = _inputLayout;

            target.VertexShader.Set(_vertexShader);
            target.VertexShader.SetConstantBuffer(0, _viewProjConstantBuffer);
            target.InputAssembler.SetVertexBuffers(0, ModelSource.VertexBufferBinding);
            target.InputAssembler.SetIndexBuffer(ModelSource.IndexBuffer, Format.R16_UInt, 0);

            target.PixelShader.Set(_pixelShader);

            target.InputAssembler.PrimitiveTopology = Direct3D.PrimitiveTopology.TriangleList;
            boneCalculator.Update(target, _nowFrame.Value);
            target.VertexShader.SetShaderResource(0, boneCalculator.BoneSrv);
            foreach (var material in ModelSource.Materials)
            {
                target.PixelShader.SetConstantBuffer(0, material.PixelConstantBuffer0);
                target.DrawIndexed(material.IndexNum, material.IndexStart, 0);
            }

            BoneRenderer.Render(target);
        }
    }
}
