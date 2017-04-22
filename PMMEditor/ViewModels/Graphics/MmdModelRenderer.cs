using System;
using System.Collections.Generic;
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
using PMMEditor.Models.MMDModel;
using PMMEditor.MVVM;
using Direct2D1 = SharpDX.Direct2D1;
using SharpDX.Direct3D11;

namespace PMMEditor.ViewModels.Graphics
{
    public interface IInitializable
    {
        void Initialize(Direct3D11.Device device);
    }

    public struct RenderArgs
    {
        public RenderArgs(DeviceContext context, Matrix viewProj)
        {
            Context = context;
            ViewProj = viewProj;
        }

        public DeviceContext Context { get; }

        public Matrix ViewProj { get; }
    }

    public struct Render2DArgs
    {
        public Direct2D1.RenderTarget D2DRenderTarget { get; }

        public Render2DArgs(Direct2D1.RenderTarget d2DRenderTarget)
        {
            D2DRenderTarget = d2DRenderTarget;
        }
    }

    public interface IRenderer : IInitializable
    {
        void UpdateTask();

        void Update();

        void Render(RenderArgs args);

        void Render(Render2DArgs args);
    }


    public class MmdModelBoneCalculatorSRV : BindableDisposableBase
    {
        private readonly Direct3D11.Device _device;
        private Texture2D _boneTexture2D;
        private Matrix[] _outputArr;
        private readonly BoneFrameControlModel _controller;

        public ShaderResourceView BoneSrv { get; private set; }

        public MmdModelBoneCalculatorSRV(
            IMmdModelRendererSource model, BoneFrameControlModel controller, Direct3D11.Device device)
        {
            _device = device;
            _controller = controller;
            CreateData(model);
        }

        private void CreateData(IMmdModelRendererSource model)
        {
            int boneNum = model.Model.BoneKeyList.Count * 2;
            _outputArr = new Matrix[boneNum];
            _boneTexture2D = new Texture2D(
                _device,
                new Texture2DDescription
                {
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    BindFlags = BindFlags.ShaderResource,
                    ArraySize = 1,
                    MipLevels = 1,
                    Width = Math.Min(boneNum * 4, 1024),
                    Height = (boneNum * 4 + 1024 - 1) / 1024,
                    Usage = ResourceUsage.Default,
                    SampleDescription = new SampleDescription(1, 0),
                    Format = Format.R32G32B32A32_Float
                }).AddTo(CompositeDisposables);
            BoneSrv = new ShaderResourceView(_device, _boneTexture2D)
                .AddTo(CompositeDisposables);
        }

        public void UpdateBone()
        {
            _controller.Update();
        }

        public void Update(DeviceContext context, int nowFrame)
        {
            MmdModelBoneCalculator calclator = _controller.BoneCalculator;
            calclator.WorldBones.CopyTo(_outputArr, 0);
            calclator.ModelLocalBones.CopyTo(_outputArr, calclator.WorldBones.Length);
            context.UpdateSubresource(_outputArr, _boneTexture2D, 0, 16 * 1024, 16 * 1024, new ResourceRegion
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
        public IMmdModelRendererSource ModelSource { get; }

        private readonly Model _model;
        private MmdModelBoneCalculatorSRV _boneCalculator;
        private Direct3D11.Device _device;

        private InputLayout _inputLayout;

        private readonly ReadOnlyReactiveProperty<int> _nowFrame;
        private readonly ReactiveProperty<bool> _isInternalInitialized = new ReactiveProperty<bool>(false);
        private BoneFrameControlModel _boneFrameController;
        private Effect _effect;

        public class Technique
        {
            public EffectTechnique EffectTechnique { get; set; }

            public enum UseTextureFlag
            {
                Both,
                True,
                False
            }

            public UseTextureFlag UseTexture { get; set; }
        }

        private struct MyEffect
        {
            public EffectMatrixVariable ViewProj { get; set; }

            public EffectShaderResourceVariable MaterialTexture { get; set; }

            public EffectVectorVariable Diffuse { get; set; }
        }

        private MyEffect _myEffect;
        private readonly List<Technique> _techniques = new List<Technique>();

        public MmdModelRenderer(Model model, IMmdModelRendererSource sourceModel)
        {
            _model = model;
            ModelSource = sourceModel;
            _nowFrame = model.FrameControlModel.ObserveProperty(_ => _.NowFrame).ToReadOnlyReactiveProperty()
                             .AddTo(CompositeDisposables);
            IsInitialized =
                new[] { _isInternalInitialized }
                    .CombineLatestValuesAreAllTrue()
                    .ToReadOnlyReactiveProperty()
                    .AddTo(CompositeDisposables);
        }

        public ReadOnlyReactiveProperty<bool> IsInitialized { get; }

        private void InitializeInternal()
        {
            _boneFrameController = new BoneFrameControlModel(_model.FrameControlModel, ModelSource.Model);
            _boneCalculator = new MmdModelBoneCalculatorSRV(ModelSource, _boneFrameController, _device)
                .AddTo(CompositeDisposables);
            BoneRenderer = new BoneRenderer(ModelSource, _device).AddTo(CompositeDisposables);

            byte[] shaderSource = Resource1.TestShader;
            // UTF-8 BOMチェック
            if (3 <= shaderSource.Length
                && shaderSource[0] == 0xEF && shaderSource[1] == 0xBB && shaderSource[2] == 0xBF)
            {
                for (int i = 0; i < 3; i++)
                {
                    shaderSource[i] = (byte) ' ';
                }
            }

            ShaderBytecode shaderBytes = ShaderBytecode.Compile(shaderSource, "fx_5_0", ShaderFlags.Debug);
            _effect = new Effect(_device, shaderBytes);

            U Cast<T, U>(T variable, Func<T, U> func)
                where T : EffectVariable
                where U : EffectVariable
            {
                if (variable?.IsValid != true)
                {
                    return null;
                }

                return func(variable) ?? throw new ArgumentException();
            }

            for (int j = 0; j < _effect.Description.TechniqueCount; j++)
            {
                EffectTechnique technique = _effect.GetTechniqueByIndex(j);
                _techniques.Add(new Technique
                {
                    EffectTechnique = technique
                });

                EffectScalarVariable variable = Cast(technique.GetAnnotationByName("UseTexture"), x => x.AsScalar());
                if (variable?.IsValid == true)
                {
                    _techniques[j].UseTexture = variable.GetBool()
                        ? Technique.UseTextureFlag.True
                        : Technique.UseTextureFlag.False;
                }
            }

            _myEffect.MaterialTexture = Cast(_effect.GetVariableBySemantic("MATERIALTEXTURE"), x => x.AsShaderResource());
            _myEffect.ViewProj = Cast(_effect.GetVariableBySemantic("VIEWPROJECTION"), x => x.AsMatrix());
            _myEffect.Diffuse = Cast(_effect.GetVariableBySemantic("DIFFUSE"), x => x.AsVector());

            // インプットレイアウト生成
            EffectPass pass = _effect.GetTechniqueByIndex(0).GetPassByIndex(0);
            ShaderBytecode inputSignature = pass.Description.Signature;
            _inputLayout = new InputLayout(_device, inputSignature, new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("BONE_INDEX", 0, Format.R32G32B32A32_SInt, 16, 0),
                new InputElement("BONE_WEIGHT", 0, Format.R32G32B32A32_Float, 32, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 48, 0)
            });

            _isInternalInitialized.Value = true;
        }

        public BoneRenderer BoneRenderer { get; set; }

        public void Initialize(Direct3D11.Device device)
        {
            _device = device;
            // TODO:エラー処理
            Task.Run(() => InitializeInternal());
        }

        public void Render(RenderArgs args)
        {
            if (!IsInitialized.Value)
            {
                return;
            }

            DeviceContext target = args.Context;
            Matrix m = args.ViewProj;

            // シェーダの設定
            target.InputAssembler.InputLayout = _inputLayout;

            ModelSource.SetVertexBuffer(target);
            ModelSource.SetIndexBuffer(target);

            target.InputAssembler.PrimitiveTopology = Direct3D.PrimitiveTopology.TriangleList;
            _boneCalculator.Update(target, _nowFrame.Value);
            EffectShaderResourceVariable boneTex = _effect.GetVariableBySemantic("BONE_TEXTURE").AsShaderResource();
            if (boneTex?.IsValid == true)
            {
                boneTex.SetResource(_boneCalculator.BoneSrv);
            }
            _myEffect.ViewProj?.SetMatrix(m);
            foreach (var material in ModelSource.Materials)
            {
                bool useTexture = material.TexuteIndex != null;
                if (useTexture)
                {
                    _myEffect.MaterialTexture?.SetResource(ModelSource.Textures[(int) material.TexuteIndex]);
                }

                _myEffect.Diffuse?.Set(material.Diffuse);

                foreach (var item in _techniques)
                {
                    EffectTechnique technique = item.EffectTechnique;
                    if (item.UseTexture == Technique.UseTextureFlag.False && useTexture)
                    {
                        continue;
                    }
                    if (item.UseTexture == Technique.UseTextureFlag.True && !useTexture)
                    {
                        continue;
                    }

                    for (int i = 0; i < technique.Description.PassCount; ++i)
                    {
                        EffectPass techPass = technique.GetPassByIndex(i);
                        techPass.Apply(target);
                        target.DrawIndexed(material.IndexNum, material.IndexStart, 0);
                    }
                }
            }

            target.VertexShader.SetShaderResource(0, _boneCalculator.BoneSrv);
            BoneRenderer.Render(args);
        }

        public void Render(Render2DArgs args) { }

        public void UpdateTask()
        {
            if (!IsInitialized.Value)
            {
                return;
            }

            _boneCalculator.UpdateBone();
        }

        public void Update() { }
    }
}
