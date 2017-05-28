using System;
using System.Collections.Generic;
using System.Numerics;
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
    public struct RenderArgs
    {
        public RenderArgs(Direct3D11.DeviceContext context, Matrix4x4 viewProj)
        {
            Context = context;
            ViewProj = viewProj;
        }

        public Direct3D11.DeviceContext Context { get; }

        public Matrix4x4 ViewProj { get; }
    }

    public struct Render2DArgs
    {
        public Direct2D1.RenderTarget D2DRenderTarget { get; }

        public Render2DArgs(Direct2D1.RenderTarget d2DRenderTarget)
        {
            D2DRenderTarget = d2DRenderTarget;
        }
    }

    public interface IRenderer
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
        private BoneFrameControlModel _controller;

        public Direct3D11.ShaderResourceView BoneSrv { get; private set; }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _compositeDisposables.Dispose();
        }

        public override void Start()
        {
            _controller = GameObject.GetComponent<BoneFrameControlModel>();
            CreateData(GameObject.GetComponent(typeof(IMmdModelRendererSource)) as IMmdModelRendererSource);
        }

        private void CreateData(IMmdModelRendererSource model)
        {
            Direct3D11.Device device = GraphicsModel.Device;
            int boneNum = model.Model.BoneKeyList.Count * 2;
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

        public void UpdateBone(Matrix4x4[] OutputArr)
        {
            _controller.Update();
            MmdModelBoneCalculator calclator = _controller.BoneCalculator;
            calclator.WorldBones.CopyTo(OutputArr, 0);
            calclator.ModelLocalBones.CopyTo(OutputArr, calclator.WorldBones.Length);
        }

        public void Update(Direct3D11.DeviceContext context, Matrix4x4[] copyBoneData)
        {
            context.UpdateSubresource(copyBoneData, _boneTexture2D, 0, 16 * 1024, 16 * 1024,
                                      new Direct3D11.ResourceRegion
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
            _device = GraphicsModel.Device;
            InitializeInternal();
        }

        public ReadOnlyReactiveProperty<bool> IsInitialized { get; private set; }

        private void InitializeInternal()
        {
            GameObject.AddComponent<FrameControlFilter>().ControlModel = _model.FrameControlModel;
            GameObject.AddComponent<BoneFrameControlModel>();
            _boneCalculator = GameObject.AddComponent<MmdModelBoneCalculatorSRV>();
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

                Direct3D11.EffectScalarVariable variable = Cast(technique.GetAnnotationByName("UseTexture"),
                                                                x => x.AsScalar());
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

        public void Render(RenderArgs args) { }

        public void Render(Render2DArgs args) { }

        private RenderData _nowUpdateData;

        public override void UpdateTask()
        {
            if (IsInitialized?.Value != true)
            {
                return;
            }

            lock (_freeRenderDatas)
            {
                if (_freeRenderDatas.Count == 0)
                {
                    _nowUpdateData = new RenderData
                    {
                        BoneData = new Matrix4x4[ModelSource.Model.BoneKeyList.Count * 2]
                    };
                }
                else
                {
                    _nowUpdateData = _freeRenderDatas.Dequeue();
                }
            }
            _boneCalculator.UpdateBone(_nowUpdateData.BoneData);
        }

        public override void Update()
        {
            if (_nowUpdateData == null)
            {
                return;
            }

            _nowUpdateData.ViewProj = Camera.Main.View * Camera.Main.Projection;
        }

        private class RenderData
        {
            public Matrix4x4 ViewProj { get; set; }

            public Matrix4x4[] BoneData { get; set; }
        }

        private readonly Queue<RenderData> _freeRenderDatas = new Queue<RenderData>();

        internal override object DequeueRenderData()
        {
            return _nowUpdateData;
        }

        internal override void EnqueueRenderData(object obj)
        {
            _freeRenderDatas.Enqueue((RenderData) obj);
        }


        internal override void Render(ECSystem.RendererArgs args)
        {
            if (IsInitialized?.Value != true || Mesh == null)
            {
                return;
            }

            var data = (RenderData) args.RenderData;
            if (data == null)
            {
                return;
            }

            Direct3D11.DeviceContext target = args.Context;
            Matrix4x4 m = data.ViewProj;

            // シェーダの設定
            target.InputAssembler.InputLayout = _inputLayout;

            Mesh.SetBuffer(target);

            target.InputAssembler.PrimitiveTopology = Direct3D.PrimitiveTopology.TriangleList;
            _boneCalculator.Update(target, data.BoneData);
            Direct3D11.EffectShaderResourceVariable boneTex =
                _effect.GetVariableBySemantic("BONE_TEXTURE").AsShaderResource();
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
            //BoneRenderer.Render(args);
        }
    }
}
