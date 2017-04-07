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
            Model = model;
            _device = device ?? throw new ArgumentNullException(nameof(device));

            BoneCount = Model.BoneKeyList.Count;
            Task.Run(() =>
            {
                CreateData();
                IsInitialized = true;
            });
        }

        public int BoneCount { get; private set; }

        private CompositeDisposable _d3DObjectCompositeDisposable2 = new CompositeDisposable();
        private readonly Direct3D11.Device _device;

        #region IsInitialized変更通知プロパティ

        private bool _isInitialized;

        public bool IsInitialized
        {
            get { return _isInitialized; }
            private set { SetProperty(ref _isInitialized, value); }
        }

        #endregion

        public MmdModelModel Model { get; }

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
            _d3DObjectCompositeDisposable2.Dispose();
            _d3DObjectCompositeDisposable2 = new CompositeDisposable();
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

            Materials = new List<Material>(Model.Materials.Count);
            int preIndex = 0;
            foreach (var material in Model.Materials)
            {
                var diffuse = new Color4(new Color3(material.Diffuse.R, material.Diffuse.G, material.Diffuse.B),
                                         material.Diffuse.A);
                Materials.Add(new Material
                {
                    IndexNum = (int) material.FaceVertexCount,
                    IndexStart = preIndex,
                    PixelConstantBuffer0 =
                        Direct3D11.Buffer.Create(_device, Direct3D11.BindFlags.ConstantBuffer, ref diffuse, 0,
                                                 Direct3D11.ResourceUsage.Immutable).AddTo(_d3DObjectCompositeDisposable2)
                });
                preIndex += (int) material.FaceVertexCount;
            }

            // 頂点データ生成
            if (Model.Vertices.Count > 0)
            {
                int typeSize = Utilities.SizeOf<Vertex>();
                _verteBuffer = Direct3D11.Buffer.Create(
                    _device,
                    Model.Vertices.Select(_ => new Vertex
                    {
                        Position = new Vector4(_.Position.X, _.Position.Y, _.Position.Z, 1.0f),
                        Weight = _.BdefN[0].Weight,
                        Idx = new Int4
                        {
                            X = _.BdefN[0].BoneIndex,
                            Y = _.BdefN[1].BoneIndex
                        }
                    }).ToArray(),
                    new Direct3D11.BufferDescription
                    {
                        SizeInBytes = typeSize * Model.Vertices.Count,
                        Usage = Direct3D11.ResourceUsage.Immutable,
                        BindFlags = Direct3D11.BindFlags.VertexBuffer,
                        StructureByteStride = typeSize
                    }).AddTo(_d3DObjectCompositeDisposable2);
                VertexBufferBinding = new Direct3D11.VertexBufferBinding(_verteBuffer, typeSize, 0);

                // 頂点インデックス生成
                int indexNum = Model.Indices.Count;
                IndexBuffer = Direct3D11.Buffer.Create(
                    _device, Model.Indices.ToArray(),
                    new Direct3D11.BufferDescription
                    {
                        SizeInBytes = Utilities.SizeOf<int>() * indexNum,
                        Usage = Direct3D11.ResourceUsage.Immutable,
                        BindFlags = Direct3D11.BindFlags.IndexBuffer,
                        StructureByteStride = Utilities.SizeOf<int>()
                    }).AddTo(_d3DObjectCompositeDisposable2);
            }

            OnLoad();
        }

        #region IDisposable Support

        private bool _disposedValue;
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _d3DObjectCompositeDisposable2.Dispose();
                    _d3DObjectCompositeDisposable2 = null;
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
