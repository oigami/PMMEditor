using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PMMEditor.MMDFileParser
{
    public class PmdStruct
    {
        #region ヘッダ

        [StringLength(3, MinimumLength = 3)]
        public string Magic { get; set; }

        public float Version { get; set; }

        [StringLength(20)]
        public string ModelName { get; set; }

        [StringLength(256)]
        public string Comment { get; set; }

        #endregion

        #region 頂点

        class Vertex
        {
            public Vector3 Position { get; set; }

            public Vector3 NormalVector { get; set; }

            public Vector2 UV { get; set; }

            public ushort BoneNum1 { get; set; }

            public ushort BoneNum2 { get; set; }

            [Range(0, 100)]
            public byte BoneWeight { get; set; }

            public bool IsEdgeEnabled { get; set; }
        }

        #endregion

        #region 頂点インデックス

        public List<ushort> VertexIndex { get; set; }

        #endregion

        #region 材質

        class Color
        {
            public float R { get; set; }

            public float G { get; set; }

            public float B { get; set; }
        }

        class Material
        {
            public Color Diffuse { get; set; }

            public float DiffuseAlpha { get; set; }

            public float Specularity { get; set; }

            public Color Specular { get; set; }

            public Color Ambient { get; set; }

            public byte ToonIndex { get; set; }

            public byte EdgeFlag { get; set; }

            public uint FaceVertexCount { get; set; }

            [StringLength(20)]
            public string TextureFilename { get; set; }
        }

        #endregion

        #region ボーン

        enum BoneKind
        {
            Rotate,
            RotateAndMove,
            IK,
            Unknown,
            IKAffected,
            RotateAffected,
            IKTarget,
            Invisible,
            Twist,
            RotationAssociated
        }

        class Bone
        {
            [StringLength(20)]
            public string Name { get; set; }

            public ushort? ParentBoneIndex { get; set; }

            public ushort? TailBoneIndex { get; set; }

            public BoneKind Kind { get; set; }

            public ushort? IKParentBoneIndex { get; set; }

            public Vector3 Position { get; set; }
        }

        #endregion

        #region IK

        class IK
        {
            public ushort BoneIndex { get; set; }

            public ushort TargetBoneIndex { get; set; }

            public ushort Iterations { get; set; }

            public float LimitAngle { get; set; }

            public List<ushort> IKChildBoneIndex { get; set; }
        }

        #endregion

        #region 表情

        enum SkinKind
        {
            Base,
            Eyebrow,
            Eye,
            Lip,
            Others
        }

        class SkinVertex
        {
            public uint VertexIndex { get; set; }

            public Vector3 VertexPosition { get; set; }
        }

        class Skin
        {
            [StringLength(20)]
            public string Name { get; set; }

            public uint SkinVertexCount { get; set; }

            public SkinKind Kind { get; set; }

            public List<SkinVertex> SkinVertices { get; set; }
        }

        #endregion

        #region 表示枠

        class BoneDisp
        {
            public ushort BoneIndex { get; set; }

            public byte BoneDispFrameIndex { get; set; }
        }

        #endregion

        #region 剛体

        enum RigidBodyShapeKind
        {
            Sphere,
            Box,
            Capsule
        }

        enum RigidBodyKind
        {
            BoneTracking,
            Physics,
            PhysicsAndBonePositionTracking
        }

        class RigidBody
        {
            [StringLength(20)]
            public string Name { get; set; }

            public ushort? BoneIndex { get; set; }

            public byte GroupIndex { get; set; }

            public ushort GroupTarget { get; set; }


            public RigidBodyShapeKind ShapeKind { get; set; }

            public float HalfWidth { get; set; }

            public float HalfHeight { get; set; }

            public float HalfDepth { get; set; }

            public Vector3 RelativePosition { get; set; }

            public Vector3 Rotation { get; set; }

            public float Weight { get; set; }

            public float MoveDamping { get; set; }

            public float RotateDamping { get; set; }

            public float Recoil { get; set; }

            public float Friction { get; set; }

            public RigidBodyKind Kind { get; set; }
        }

        #endregion

        #region ジョイント

        class Joint
        {
            [StringLength(20)]
            public string Name { get; set; }

            public uint RigidBodyA { get; set; }

            public uint RigidBodyB { get; set; }

            public Vector3 Position { get; set; }

            public Vector3 Rotation { get; set; }

            public Vector3 ConstrainLowerPosition { get; set; }

            public Vector3 ConstrainUpperPosition { get; set; }

            public Vector3 ConstrainLowerRotation { get; set; }

            public Vector3 ConstrainUpperRotation { get; set; }

            public Vector3 SpringPosition { get; set; }

            public Vector3 SpringRotation { get; set; }
        }

        #endregion
    }

    public class PmdReader
    {
        private readonly byte[] _binaryData;
        private Stream _stream;

        public PmdReader(byte[] binaryData)
        {
            _binaryData = binaryData;
        }

        public PmdStruct Read()
        {
            _stream = new MemoryStream(_binaryData);
            var o = new PmdStruct();


            return o;
        }
    }
}
