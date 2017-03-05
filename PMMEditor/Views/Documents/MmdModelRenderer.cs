using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using PMMEditor.MMDFileParser;
using PMMEditor.Models;
using Reactive.Bindings.Extensions;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using Direct3D11 = SharpDX.Direct3D11;
using Direct3D = SharpDX.Direct3D;

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

    public class MmdModelBoneCalculator
    {
        private MmdModelModel _model;
        private readonly Direct3D11.Device _device;

        public Direct3D11.ShaderResourceView BoneSrv { get; private set; }

        public MmdModelBoneCalculator(Direct3D11.Device device)
        {
            _device = device;
        }

        private void CalcBoneWorld(
            MmdModelRendererSource.Bone me, List<MmdModelRendererSource.Bone> bones, Matrix parent,
            ref Matrix[] resultWorlds)
        {
            var m = me.boneMat * parent;
            resultWorlds[me.id] = me.offsetMat * m;
            if (me.firstChild != -1)
            {
                CalcBoneWorld(bones[me.firstChild], bones, m, ref resultWorlds);
            }
            if (me.sibling != -1)
            {
                CalcBoneWorld(bones[me.sibling], bones, parent, ref resultWorlds);
            }
        }

        public void CreateData(MmdModelModel model, List<MmdModelRendererSource.Bone> bones)
        {
            _model = model;
            int boneNum = bones.Count;

            Direct3D11.Texture2D boneTexture2D = new Direct3D11.Texture2D(
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
                });
            BoneSrv = new Direct3D11.ShaderResourceView(_device, boneTexture2D);

            foreach (var i in Enumerable.Range(0, bones.Count))
            {
                var boneKeyFrames = _model.BoneKeyList.First(_ => _.Name == bones[i].name);

                var pos = boneKeyFrames[0].Position;
                var q = boneKeyFrames[0].Quaternion;
                bones[i].boneMat = Matrix.RotationQuaternion(new Quaternion(q.X, q.Y, q.Z, q.W))
                                   * Matrix.Translation(pos.X, pos.Y, pos.Z) * bones[i].initMat;
            }
            Matrix[] worldBones = Enumerable.Range(0, bones.Count).Select(_ => Matrix.Identity).ToArray();
            CalcBoneWorld(bones[0], bones, Matrix.Identity, ref worldBones);
            _device.ImmediateContext.UpdateSubresource(worldBones, boneTexture2D);
        }
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
        private readonly MmdModelModel _model;
        private List<Bone> _bones;

        private Direct3D11.Buffer _verteBuffer;

        public MmdModelBoneCalculator BoneCalculator { get; private set; }

        public Direct3D11.VertexBufferBinding _vertexBufferBinding { get; private set; }

        public Direct3D11.Buffer _indexBuffer { get; private set; }

        public List<Material> Materials { get; private set; }

        public class Material
        {
            public int IndexStart { get; set; }

            public int IndexNum { get; set; }

            public Direct3D11.Buffer PixelConstantBuffer0 { get; set; }
        }

        private struct Int2
        {
            public int x, y;
        }

        private struct Vertex
        {
            public Vector4 Position { get; set; }

            public Int4 Idx { get; set; }

            public float Weight { get; set; }
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
            var data = Pmd.ReadFile(_model.FilePath);

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
            if (data.Vertices.Count > 0)
            {
                var typeSize = Utilities.SizeOf<Vertex>();
                _verteBuffer = Direct3D11.Buffer.Create(
                    _device,
                    data.Vertices.Select(_ => new Vertex
                    {
                        Position = new Vector4(_.Position.X, _.Position.Y, _.Position.Z, 1.0f),
                        Weight = 1,
                        Idx = new Int4
                        {
                            X = _.BoneNum1,
                            Y = _.BoneNum2
                        }
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

            CreateBone(data);
            BoneCalculator = new MmdModelBoneCalculator(_device);
            BoneCalculator.CreateData(_model, _bones);
        }

        public class Bone
        {
            public int sibling = -1;
            public int firstChild = -1;
            public int id;
            public int parent = -1;
            public PmdStruct.BoneKind type;
            public Matrix initMat;
            public Matrix boneMatML;
            public Matrix initMatML;
            public Matrix offsetMat;
            public Matrix boneMat;
            public string name;
        }


        void InitBoneCalc(Bone me, Matrix parentoffsetMat)
        {
            if (me.firstChild != -1)
            {
                InitBoneCalc(_bones[me.firstChild], me.offsetMat);
            }
            if (me.sibling != -1)
            {
                InitBoneCalc(_bones[me.sibling], parentoffsetMat);
            }
            me.initMat = me.initMatML * parentoffsetMat;
        }

        private void CreateBone(PmdStruct data)
        {
            var size = data.Bones.Count;
            _bones = new List<Bone>(size);

            foreach (int i in Enumerable.Range(0, size))
            {
                var item = data.Bones[i];
                var inputBone = data.Bones;
                var outputBone = new Bone();
                if (item.ParentBoneIndex != null)
                {
                    ushort parentBoneIndex = (ushort) item.ParentBoneIndex;


                    //自分と同じ親で自分よりあとのボーンが兄弟になる
                    for (int j = i + 1; j < size; ++j)
                    {
                        if (parentBoneIndex == inputBone[j].ParentBoneIndex)
                        {
                            outputBone.sibling = j;
                            break;
                        }
                    }

                    outputBone.parent = parentBoneIndex;
                }

                //自分が親になっていて一番早く現れるボーンが子になる
                foreach (int j in Enumerable.Range(0, size))
                {
                    if (i == inputBone[j].ParentBoneIndex)
                    {
                        outputBone.firstChild = j;
                        break;
                    }
                }


                outputBone.name = item.Name;
                outputBone.id = i;
                outputBone.type = item.Kind;
                Matrix modelLocalInitMat = Matrix.Translation(item.Position.X, item.Position.Y, item.Position.Z);
                outputBone.initMatML = outputBone.boneMatML = outputBone.initMat = modelLocalInitMat; // モデルローカル座標系
                outputBone.offsetMat = Matrix.Invert(modelLocalInitMat);

                _bones.Add(outputBone);
            }


            InitBoneCalc(_bones[0], Matrix.Identity);
            foreach (var i in _bones)
            {
                i.boneMat = i.initMat;
            }
        }

        #region IDisposable Support

        private bool disposedValue; // 重複する呼び出しを検出するには

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

            target.VertexShader.SetShaderResource(0, _model.BoneCalculator.BoneSrv);
            foreach (var material in Model.Materials)
            {
                target.PixelShader.SetConstantBuffer(0, material.PixelConstantBuffer0);
                target.DrawIndexed(material.IndexNum, material.IndexStart, 0);
            }
        }
    }
}
