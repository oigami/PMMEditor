using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using PMMEditor.ECS;
using PMMEditor.Models.MMDModel;
using PMMEditor.MVVM;
using PMMEditor.ViewModels.Graphics;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using Direct3D11 = SharpDX.Direct3D11;
using Direct3D = SharpDX.Direct3D;
using Direct2D1 = SharpDX.Direct2D1;

namespace PMMEditor.Models.Graphics
{
    public interface IInitializable
    {
        void Initialize(Direct3D11.Device device);
    }

    public struct RenderArgs
    {
        public RenderArgs(Direct3D11.DeviceContext context, Matrix viewProj)
        {
            Context = context;
            ViewProj = viewProj;
        }

        public Direct3D11.DeviceContext Context { get; }

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


    public class MmdModelBoneCalculatorSRV : Component
    {
        private readonly CompositeDisposable _compositeDisposables = new CompositeDisposable();
        private Direct3D11.Texture2D _boneTexture2D;
        private Matrix[] _outputArr;
        private BoneFrameControlModel _controller;

        public Direct3D11.ShaderResourceView BoneSrv { get; private set; }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _compositeDisposables.Dispose();
        }

        public MmdModelBoneCalculatorSRV Initialize()
        {
            _controller = GameObject.GetComponent<BoneFrameControlModel>();
            CreateData(GameObject.GetComponent(typeof(IMmdModelRendererSource)) as IMmdModelRendererSource);
            return this;
        }

        private void CreateData(IMmdModelRendererSource model)
        {
            Direct3D11.Device device = GraphicsModel.Device;
            int boneNum = model.Model.BoneKeyList.Count * 2;
            _outputArr = new Matrix[boneNum];
            _boneTexture2D = new Direct3D11.Texture2D(
                device,
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
                }).AddTo(_compositeDisposables);
            BoneSrv = new Direct3D11.ShaderResourceView(device, _boneTexture2D)
                .AddTo(_compositeDisposables);
        }

        public void UpdateBone()
        {
            _controller.Update();
        }

        public void Update(Direct3D11.DeviceContext context, int nowFrame)
        {
            MmdModelBoneCalculator calclator = _controller.BoneCalculator;
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

    public class MmdModelRenderer : Renderer, IRenderer
    {
        private IMmdModelRendererSource ModelSource { get; set; }

        private Model _model;
        private MmdModelBoneCalculatorSRV _boneCalculator;
        private Direct3D11.Device _device;

        private Direct3D11.InputLayout _inputLayout;

        private ReadOnlyReactiveProperty<int> _nowFrame;
        private readonly ReactiveProperty<bool> _isInternalInitialized = new ReactiveProperty<bool>(false);
        private Direct3D11.Effect _effect;

        public class Technique
        {
            public Direct3D11.EffectTechnique EffectTechnique { get; set; }

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
            public Direct3D11.EffectMatrixVariable ViewProj { get; set; }

            public Direct3D11.EffectShaderResourceVariable MaterialTexture { get; set; }

            public Direct3D11.EffectVectorVariable Diffuse { get; set; }
        }

        private MyEffect _myEffect;
        private readonly List<Technique> _techniques = new List<Technique>();
        private readonly CompositeDisposable _compositeDisposables = new CompositeDisposable();

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _compositeDisposables.Dispose();
        }

        public void Initialize(Model model)
        {
            _model = model;
            ModelSource = GameObject.GetComponent(typeof(IMmdModelRendererSource)) as IMmdModelRendererSource;
            Mesh = GameObject.GetComponent<MeshFilter>()?.Mesh ?? Mesh;
            _nowFrame = model.FrameControlModel.ObserveProperty(_ => _.NowFrame).ToReadOnlyReactiveProperty()
                             .AddTo(_compositeDisposables);
            IsInitialized =
                new[] { _isInternalInitialized }
                    .CombineLatestValuesAreAllTrue()
                    .ToReadOnlyReactiveProperty()
                    .AddTo(_compositeDisposables);
        }

        public ReadOnlyReactiveProperty<bool> IsInitialized { get; private set; }

        private void InitializeInternal()
        {
            GameObject.AddComponent<FrameControlFilter>().ControlModel = _model.FrameControlModel;
            GameObject.AddComponent<BoneFrameControlModel>().Initialize();
            _boneCalculator = GameObject.AddComponent<MmdModelBoneCalculatorSRV>().Initialize();
            BoneRenderer = new BoneRenderer(ModelSource, _device).AddTo(_compositeDisposables);

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
            _effect = new Direct3D11.Effect(_device, shaderBytes);

            U Cast<T, U>(T variable, Func<T, U> func)
                where T : Direct3D11.EffectVariable
                where U : Direct3D11.EffectVariable
            {
                if (variable?.IsValid != true)
                {
                    return null;
                }

                return func(variable) ?? throw new ArgumentException();
            }

            for (int j = 0; j < _effect.Description.TechniqueCount; j++)
            {
                Direct3D11.EffectTechnique technique = _effect.GetTechniqueByIndex(j);
                _techniques.Add(new Technique
                {
                    EffectTechnique = technique
                });

                Direct3D11.EffectScalarVariable variable = Cast(technique.GetAnnotationByName("UseTexture"), x => x.AsScalar());
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
            Direct3D11.EffectPass pass = _effect.GetTechniqueByIndex(0).GetPassByIndex(0);
            ShaderBytecode inputSignature = pass.Description.Signature;
            _inputLayout = new Direct3D11.InputLayout(_device, inputSignature, new[]
            {
                new Direct3D11.InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new Direct3D11.InputElement("BONE_INDEX", 0, Format.R32G32B32A32_SInt, 16, 0),
                new Direct3D11.InputElement("BONE_WEIGHT", 0, Format.R32G32B32A32_Float, 32, 0),
                new Direct3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 48, 0)
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
            if (!IsInitialized.Value || Mesh == null)
            {
                return;
            }

            Direct3D11.DeviceContext target = args.Context;
            Matrix m = args.ViewProj;

            // シェーダの設定
            target.InputAssembler.InputLayout = _inputLayout;

            Mesh.SetBuffer(target);

            target.InputAssembler.PrimitiveTopology = Direct3D.PrimitiveTopology.TriangleList;
            _boneCalculator.Update(target, _nowFrame.Value);
            Direct3D11.EffectShaderResourceVariable boneTex = _effect.GetVariableBySemantic("BONE_TEXTURE").AsShaderResource();
            if (boneTex?.IsValid == true)
            {
                boneTex.SetResource(_boneCalculator.BoneSrv);
            }
            _myEffect.ViewProj?.SetMatrix(m);
            Mesh.IndexRange[] indexRanges = Mesh.InternalData.IndexRanges;
            if (indexRanges.Length != SharedMaterials.Length)
            {
                throw new InvalidOperationException();
            }

            foreach (var (material, j) in SharedMaterials.Indexed())
            {
                Mesh.IndexRange indexRange = indexRanges[j];
                bool useTexture = material.MainTexture != null;
                if (useTexture)
                {
                    _myEffect.MaterialTexture?.SetResource(material.MainTexture);
                }

                _myEffect.Diffuse?.Set(material.Diffuse);

                foreach (var item in _techniques)
                {
                    Direct3D11.EffectTechnique technique = item.EffectTechnique;
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
                        Direct3D11.EffectPass techPass = technique.GetPassByIndex(i);
                        techPass.Apply(target);
                        target.DrawIndexed(indexRange.Count, indexRange.Start, 0);
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

        public override void Update() { }

        internal override void Render()
        {
            throw new NotImplementedException();
        }
    }
}
