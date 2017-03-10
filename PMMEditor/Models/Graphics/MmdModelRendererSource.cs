using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using PMMEditor.MMDFileParser;
using PMMEditor.Models.MMDModel;
using PMMEditor.MVVM;
using Reactive.Bindings.Extensions;
using SharpDX;
using Direct3D11 = SharpDX.Direct3D11;

namespace PMMEditor.Models.Graphics
{
    public sealed class MmdModelRendererSource : BindableBase, IDisposable
    {
        public MmdModelRendererSource(MmdModelModel model, Direct3D11.Device device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            _model = model;
            _device = device;

            BoneCount = _model.BoneKeyList.Count;
            Task.Run(() =>
            {
                CreateData();
                IsInitialized = true;
            });
        }

        public MmdModelBoneCalculator BoneCalculator => _model.BoneCalculator;

        public int BoneCount { get; private set; }

        private CompositeDisposable D3DObjectCompositeDisposable = new CompositeDisposable();
        private bool disposedValue;
        private readonly Direct3D11.Device _device;

        #region IsInitialized変更通知プロパティ

        private bool _isInitialized;

        public bool IsInitialized
        {
            get { return _isInitialized; }
            private set { SetProperty(ref _isInitialized, value); }
        }

        #endregion

        private readonly MmdModelModel _model;

        #region VertexBufferBinding変更通知プロパティ

        private Direct3D11.Buffer _verteBuffer;
        private Direct3D11.VertexBufferBinding _vertexBufferBinding;

        public Direct3D11.VertexBufferBinding VertexBufferBinding
        {
            get { return _vertexBufferBinding; }
            private set { SetProperty(ref _vertexBufferBinding, value); }
        }

        #endregion

        #region IndexBuffer変更通知プロパティ

        private Direct3D11.Buffer _indexBuffer;

        public Direct3D11.Buffer IndexBuffer
        {
            get { return _indexBuffer; }
            private set { SetProperty(ref _indexBuffer, value); }
        }

        #endregion

        #region Materials変更通知プロパティ

        private List<Material> _materials;

        public List<Material> Materials
        {
            get { return _materials; }
            private set { SetProperty(ref _materials, value); }
        }

        #endregion

        public class Material
        {
            public int IndexStart { get; set; }

            public int IndexNum { get; set; }

            public Direct3D11.Buffer PixelConstantBuffer0 { get; set; }
        }

        private struct Vertex
        {
            public Vector4 Position { get; set; }

            public Int4 Idx { get; set; }

            public float Weight { get; set; }
        }


        private void OnUnload()
        {
            D3DObjectCompositeDisposable.Dispose();
            D3DObjectCompositeDisposable = new CompositeDisposable();
            IsInitialized = false;
            VertexBufferBinding = new Direct3D11.VertexBufferBinding();
            IndexBuffer = null;
            Materials = null;
        }

        private void OnLoad()
        {
            IsInitialized = true;
        }

        private void CreateData()
        {
            OnUnload();

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
                        Weight = _.BoneWeight / 100.0f,
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
                VertexBufferBinding = new Direct3D11.VertexBufferBinding(_verteBuffer, typeSize, 0);

                // 頂点インデックス生成
                var indexNum = data.VertexIndex.Count;
                IndexBuffer = Direct3D11.Buffer.Create(
                    _device, data.VertexIndex.ToArray(),
                    new Direct3D11.BufferDescription
                    {
                        SizeInBytes = Utilities.SizeOf<ushort>() * indexNum,
                        Usage = Direct3D11.ResourceUsage.Immutable,
                        BindFlags = Direct3D11.BindFlags.IndexBuffer,
                        StructureByteStride = Utilities.SizeOf<ushort>()
                    }).AddTo(D3DObjectCompositeDisposable);
            }

            OnLoad();
        }

        #region IDisposable Support

        private void Dispose(bool disposing)
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
}
