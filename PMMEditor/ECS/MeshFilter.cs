using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PMMEditor.Models.MMDModel;
using SharpDX;
using SharpDX.DXGI;
using Direct3D11 = SharpDX.Direct3D11;

namespace PMMEditor.ECS
{
    public struct IndexBufferBinding
    {
        public IndexBufferBinding(Direct3D11.Buffer buffer, Format format, int offset)
        {
            Buffer = buffer;
            Format = format;
            Offset = offset;
        }

        public Direct3D11.Buffer Buffer { get; set; }

        public Format Format { get; set; }

        public int Offset { get; set; }
    }

    public struct BoneWeight
    {
        public BoneWeight(int index0, float weight0) : this(index0, weight0, 0, 0) { }

        public BoneWeight(
            int index0, float weight0,
            int index1, float weight1) : this(index0, weight0, index1, weight1, 0, 0, 0, 0) { }

        public BoneWeight(
            int index0, float weight0,
            int index1, float weight1,
            int index2, float weight2,
            int index3, float weight3)
        {
            BoneIndex0 = index0;
            BoneIndex1 = index1;
            BoneIndex2 = index2;
            BoneIndex3 = index3;
            Weight0 = weight0;
            Weight1 = weight1;
            Weight2 = weight2;
            Weight3 = weight3;
        }

        public int BoneIndex0 { get; set; }

        public int BoneIndex1 { get; set; }

        public int BoneIndex2 { get; set; }

        public int BoneIndex3 { get; set; }

        public float Weight0 { get; set; }

        public float Weight1 { get; set; }

        public float Weight2 { get; set; }

        public float Weight3 { get; set; }
    }

    public class Mesh : ECObject
    {
        private bool _requireUploadData;

        private class SubMesh
        {
            public int[] Triangles { get; set; }
        }

        private readonly List<SubMesh> _subMeshes = new List<SubMesh>(1);

        public Mesh()
        {
            _subMeshes.Add(new SubMesh());
        }

        private Vector3[] _vertices;
        private Vector2[] _uv;
        private Vector3[] _normals;
        private BoneWeight[] _boneWeights;

        private readonly GData _gData = new GData();

        public Vector3[] Vertices
        {
            get => _vertices.ToArray();
            set
            {
                _vertices = value;
                _requireUploadData = true;
            }
        }

        public Vector2[] UV
        {
            get { return _uv.ToArray(); }
            set
            {
                _uv = value;
                _requireUploadData = true;
            }
        }

        public Vector3[] Normals
        {
            get { return _normals.ToArray(); }
            set
            {
                _normals = value;
                _requireUploadData = true;
            }
        }

        public BoneWeight[] BoneWeights
        {
            get { return _boneWeights.ToArray(); }
            set
            {
                _requireUploadData = true;
                _boneWeights = value;
            }
        }

        /// <summary>
        /// サブメッシュの数を指定します。マテリアルと同じ数である必要があります。
        /// </summary>
        public int SubMeshCount
        {
            get => _subMeshes.Count;
            set
            {
                int nowCount = SubMeshCount;
                if (nowCount < value)
                {
                    _subMeshes.AddRange(Enumerable.Range(0, value - nowCount).Select(_ => new SubMesh()));
                }
                else if (nowCount != value)
                {
                    _subMeshes.RemoveRange(value, nowCount - value);
                }
            }
        }

        public void SetTriangles(int[] triangles, int submesh)
        {
            if (triangles.Length % 3 != 0)
            {
                throw new ArgumentException("The size of the triangle array must always be a multiple of 3");
            }

            if (_subMeshes.Count <= submesh)
            {
                throw new ArgumentOutOfRangeException($"Parameter name: {nameof(submesh)}. see SubMeshCount.");
            }

            _subMeshes[submesh].Triangles = triangles;
            _requireUploadData = true;
        }

        private struct Vertex
        {
            public Vector4 Position { get; set; }

            public Int4 Idx { get; set; }

            public Vector4 Weight { get; set; }

            public Vector2 UV { get; set; }
        }

        /// <summary>
        /// メッシュデータをグラフィックPAIにアップロードします
        /// </summary>
        /// <param name = "markNoLogerReadable"> True に設定するとメッシュデータのシステムメモリのコピーが解放されます。 </param>
        public void UploadMeshData(bool markNoLogerReadable)
        {
            if (!_requireUploadData)
            {
                if (markNoLogerReadable)
                {
                    ClearData();
                }
                return;
            }


            // 頂点バッファ生成

            Int4 GetIndex(BoneWeight b)
            {
                return new Int4(b.BoneIndex0, b.BoneIndex1, b.BoneIndex2, b.BoneIndex3);
            }

            Vector4 GetWeight(BoneWeight b)
            {
                return new Vector4(b.Weight0, b.Weight1, b.Weight2, b.Weight3);
            }

            int typeSize = Utilities.SizeOf<Vertex>();
            var emptyBoneWeight = new BoneWeight();
            var emptyUV = new Vector2();
            _gData.Vertices = new Direct3D11.VertexBufferBinding(Direct3D11.Buffer.Create(
                ECSystem.Device,
                _vertices.Select((_, i) => new Vertex
                {
                    Position = new Vector4(_, 1.0f),
                    Weight = GetWeight(_boneWeights?[i] ?? emptyBoneWeight),
                    Idx = GetIndex(_boneWeights?[i] ?? emptyBoneWeight),
                    UV = _uv?[i] ?? emptyUV
                }).ToArray(),
                new Direct3D11.BufferDescription
                {
                    SizeInBytes = typeSize * _vertices.Length,
                    Usage = Direct3D11.ResourceUsage.Immutable,
                    BindFlags = Direct3D11.BindFlags.VertexBuffer,
                    StructureByteStride = typeSize
                }), typeSize, 0);

            // 頂点インデックス生成
            int indexSize = GetIndexElementSize();

            var bufferDescription = new Direct3D11.BufferDescription
            {
                Usage = Direct3D11.ResourceUsage.Immutable,
                BindFlags = Direct3D11.BindFlags.IndexBuffer,
                StructureByteStride = indexSize
            };

            Direct3D11.Buffer indexBuffer;
            Format indexFormat;
            if (indexSize == sizeof(int))
            {
                indexFormat = Format.R32_UInt;
                int[] index = CreateTrianglesInt();
                bufferDescription.SizeInBytes = indexSize * index.Length;
                indexBuffer = Direct3D11.Buffer.Create(ECSystem.Device, index, bufferDescription);
            }
            else if (indexSize == sizeof(ushort))
            {
                indexFormat = Format.R16_UInt;
                ushort[] index = CreateTrianglesUShort();
                bufferDescription.SizeInBytes = indexSize * index.Length;
                indexBuffer = Direct3D11.Buffer.Create(ECSystem.Device, index, bufferDescription);
            }
            else
            {
                indexFormat = Format.R8_UInt;
                byte[] index = CreateTrianglesByte();
                bufferDescription.SizeInBytes = indexSize * index.Length;
                indexBuffer = Direct3D11.Buffer.Create(ECSystem.Device, index, bufferDescription);
            }
            _gData.Indices = new IndexBufferBinding(indexBuffer, indexFormat, 0);

            _gData.IndexRanges = new IndexRange[_subMeshes.Count];
            int cntTriangles = 0;
            foreach (var (item, i) in _subMeshes.Indexed())
            {
                _gData.IndexRanges[i] = new IndexRange(cntTriangles, item.Triangles.Length);
                cntTriangles += item.Triangles.Length;
            }

            _requireUploadData = false;

            if (markNoLogerReadable)
            {
                ClearData();
            }
        }

        private int GetIndexElementSize()
        {
            int maxIndex = _subMeshes.SelectMany(x => x.Triangles).Max();

            int indexSize = sizeof(ushort);
            if (ushort.MaxValue < maxIndex)
            {
                indexSize = sizeof(int);
            }
            else if (maxIndex <= byte.MaxValue)
            {
                indexSize = sizeof(byte);
            }
            return indexSize;
        }

        private int[] CreateTrianglesInt()
        {
            return _subMeshes.SelectMany(x => x.Triangles).Select(x => x).ToArray();
        }

        private ushort[] CreateTrianglesUShort()
        {
            return _subMeshes.SelectMany(x => x.Triangles).Select(x => (ushort) x).ToArray();
        }

        private byte[] CreateTrianglesByte()
        {
            return _subMeshes.SelectMany(x => x.Triangles).Select(x => (byte) x).ToArray();
        }

        private void ClearData() { }

        /* 内部データ */

        internal void SetBuffer(Direct3D11.DeviceContext context)
        {
            GData data = InternalData;
            context.InputAssembler.SetVertexBuffers(0, data.Vertices);
            context.InputAssembler.SetIndexBuffer(data.Indices.Buffer, data.Indices.Format, 0);
        }

        internal struct IndexRange
        {
            public IndexRange(int indexStart, int indexCount)
            {
                Start = indexStart;
                Count = indexCount;
            }

            public int Start { get; }

            public int Count { get; }
        }

        internal class GData
        {
            private Direct3D11.VertexBufferBinding _vertices;
            private IndexBufferBinding _indices;

            internal IndexRange[] IndexRanges { get; set; }

            internal Direct3D11.VertexBufferBinding Vertices
            {
                get { return _vertices; }
                set
                {
                    _vertices.Buffer?.Dispose();
                    _vertices = value;
                }
            }

            internal IndexBufferBinding Indices
            {
                get { return _indices; }
                set
                {
                    _indices.Buffer?.Dispose();
                    _indices = value;
                }
            }
        }

        internal GData InternalData
        {
            get
            {
                UploadMeshData(false);
                return _gData;
            }
        }

        protected override void OnDestroyInternal()
        {
            _gData.Vertices.Buffer?.Dispose();
            _gData.Vertices = new Direct3D11.VertexBufferBinding();

            _gData.Indices.Buffer?.Dispose();
            _gData.Indices = new IndexBufferBinding();
        }
    }

    public class MeshFilter : Component
    {
        private Mesh _mesh;

        public Mesh Mesh
        {
            get { return _mesh; }
            set
            {
                _mesh = value;
                foreach (var renderer in GameObject.GetComponents<Renderer>())
                {
                    renderer.Mesh = _mesh;
                }
            }
        }

        protected override void OnDestroy()
        {
            OnDestroyInternal();
            Destroy(Mesh);
        }
    }
}
