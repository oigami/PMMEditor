using System;
using System.Collections.Generic;
using System.Linq;
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

    public class Mesh
    {
        private bool _requireUploadData;

        private class SubMesh
        {
            public int[] Triangles { get; set; }
        }

        private List<SubMesh> _subMeshes { get; } = new List<SubMesh>();

        private Vector3[] _vertices;
        private Vector2[] _uv;
        private Vector3[] _normals;

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
            get { return _uv; }
            set
            {
                _uv = value;
                _requireUploadData = true;
            }
        }

        public Vector3[] Normals
        {
            get { return _normals; }
            set
            {
                _normals = value;
                _requireUploadData = true;
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

        /// <summary>
        /// メッシュデータをグラフィックPAIにアップロードします
        /// </summary>
        /// <param name="markNoLogerReadable">True に設定するとメッシュデータのシステムメモリのコピーが解放されます。</param>
        public void UploadMeshData(bool markNoLogerReadable)
        {

        }

        /* 内部データ */

        internal Direct3D11.VertexBufferBinding GVertices { get; set; }

        internal IndexBufferBinding GIndices { get; set; }
    }

    public class MeshFilter : Component
    {
        public Mesh Mesh { get; set; }
    }
}
