using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Numerics;

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

        public class Vertex
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

        public List<Vertex> Vertices { get; set; }

        #endregion

        #region 頂点インデックス

        public List<ushort> VertexIndex { get; set; }

        #endregion

        #region 材質

        public class Material
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

        public List<Material> Materials { get; set; }

        #endregion

        #region ボーン

        public enum BoneKind
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

        public class Bone
        {
            [StringLength(20)]
            public string Name { get; set; }

            public ushort? ParentBoneIndex { get; set; }

            public ushort? TailBoneIndex { get; set; }

            public BoneKind Kind { get; set; }

            public ushort? IKParentBoneIndex { get; set; }

            public Vector3 Position { get; set; }
        }

        public List<Bone> Bones { get; set; }

        #endregion

        #region IK

        public class IK
        {
            public ushort BoneIndex { get; set; }

            public ushort TargetBoneIndex { get; set; }

            public ushort Iterations { get; set; }

            public float LimitAngle { get; set; }

            public List<ushort> IKChildBoneIndex { get; set; }
        }

        public List<IK> IKs { get; set; }

        #endregion

        #region 表情

        public enum SkinKind
        {
            Base,
            Eyebrow,
            Eye,
            Lip,
            Others
        }

        public class SkinVertex
        {
            public uint VertexIndex { get; set; }

            public Vector3 VertexPosition { get; set; }
        }

        public class Skin
        {
            [StringLength(20)]
            public string Name { get; set; }


            public SkinKind Kind { get; set; }

            public List<SkinVertex> SkinVertices { get; set; }
        }

        public List<Skin> Skins { get; set; }

        #endregion

        #region 表示枠

        [StringLength(50)]
        public List<string> BoneDispNames { get; set; }

        public List<ushort> SkinIndices { get; set; }

        public class BoneDisp
        {
            public ushort BoneIndex { get; set; }

            public byte BoneDispFrameIndex { get; set; }
        }

        public List<BoneDisp> BoneDisps { get; set; }

        #endregion

        #region 英語

        public class EnglishNames
        {
            [StringLength(20)]
            public string ModelName { get; set; }

            [StringLength(256)]
            public string Comment { get; set; }

            [StringLength(20)]
            public List<string> BoneName { get; set; }

            [StringLength(50)]
            public List<string> SkinName { get; set; }

            [StringLength(50)]
            public List<string> BoneDipsName { get; set; }
        }

        public EnglishNames EnglishName { get; set; }

        #endregion

        [StringLength(100)]
        public List<string> ToonFilenames { get; set; }

        #region 剛体

        public enum RigidBodyShapeKind
        {
            Sphere,
            Box,
            Capsule
        }

        public enum RigidBodyKind
        {
            BoneTracking,
            Physics,
            PhysicsAndBonePositionTracking
        }

        public class RigidBody
        {
            [StringLength(20)]
            public string Name { get; set; }

            public ushort? RelationBoneIndex { get; set; }

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

        public List<RigidBody> RigidBodies { get; set; }

        #endregion

        #region ジョイント

        public class Joint
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

        public List<Joint> Joints { get; set; }

        #endregion
    }

    internal class PmdReader : MmdFileReaderBase
    {
        private readonly byte[] _binaryData;

        public PmdReader(byte[] binaryData) : base(new MemoryStream(binaryData))
        {
            _binaryData = binaryData;
        }

        public PmdStruct Read()
        {
            _stream.Seek(0, SeekOrigin.Begin);
            var o = new PmdStruct
            {
                #region ヘッダ

                Magic = ReadFixedString(3),
                Version = ReadFloat(),
                ModelName = ReadFixedStringTerminationChar(20),
                Comment = ReadFixedStringTerminationChar(256),

                #endregion

                #region 頂点

                Vertices = ReadVIntList(ReadVertex),
                VertexIndex = ReadVIntList(ReadUInt16),

                #endregion

                Materials = ReadVIntList(ReadMaterial),
                Bones = ReadList(ReadUInt16(), ReadBone),
                IKs = ReadList(ReadUInt16(), ReadIK),
                Skins = ReadList(ReadUInt16(), ReadSkin),
                SkinIndices = ReadList(ReadByte(), ReadUInt16),
                BoneDispNames = ReadList(ReadByte(), () => ReadFixedStringTerminationChar(50)),
                BoneDisps = ReadVIntList(() => new PmdStruct.BoneDisp
                {
                    BoneIndex = ReadUInt16(),
                    BoneDispFrameIndex = ReadByte()
                })
            };
            bool hasEnglishName = ReadByte() == 1;
            if (hasEnglishName)
            {
                o.EnglishName = new PmdStruct.EnglishNames
                {
                    ModelName = ReadFixedStringTerminationChar(20),
                    Comment = ReadFixedStringTerminationChar(256),
                    BoneName = ReadList(o.Bones.Count, () => ReadFixedStringTerminationChar(20)),
                    SkinName = ReadList(Math.Max(0, o.Skins.Count - 1),
                                        () => ReadFixedStringTerminationChar(20)),
                    BoneDipsName = ReadList(o.BoneDispNames.Count,
                                            () => ReadFixedStringTerminationChar(50))
                };
            }
            if (IsRemaining())
            {
                o.ToonFilenames = ReadList(10, () => ReadFixedStringTerminationChar(100));
            }
            if (IsRemaining())
            {
                o.RigidBodies = ReadVIntList(ReadRigidBody);
            }
            if (IsRemaining())

            {
                o.Joints = ReadVIntList(ReadJoint);
            }

            return o;
        }


        private PmdStruct.Vertex ReadVertex()
        {
            return new PmdStruct.Vertex
            {
                Position = ReadVector3(),
                NormalVector = ReadVector3(),
                UV = ReadVector2(),
                BoneNum1 = ReadUInt16(),
                BoneNum2 = ReadUInt16(),
                BoneWeight = ReadByte(),
                IsEdgeEnabled = ReadByte() == 0
            };
        }

        private PmdStruct.Material ReadMaterial()
        {
            return new PmdStruct.Material
            {
                Diffuse = ReadColor(),
                DiffuseAlpha = ReadFloat(),
                Specularity = ReadFloat(),
                Specular = ReadColor(),
                Ambient = ReadColor(),
                ToonIndex = ReadByte(),
                EdgeFlag = ReadByte(),
                FaceVertexCount = ReadUInt(),
                TextureFilename = ReadFixedStringTerminationChar(20)
            };
        }

        private PmdStruct.Bone ReadBone()
        {
            return new PmdStruct.Bone
            {
                Name = ReadFixedStringTerminationChar(20),
                ParentBoneIndex = ParameterCheck<ushort>(ReadUInt16(), 0xffff),
                TailBoneIndex = ParameterCheck<ushort>(ReadUInt16(), 0),
                Kind = (PmdStruct.BoneKind) ReadByte(),
                IKParentBoneIndex = ParameterCheck<ushort>(ReadUInt16(), 0),
                Position = ReadVector3()
            };
        }

        private PmdStruct.IK ReadIK()
        {
            var o = new PmdStruct.IK
            {
                BoneIndex = ReadUInt16(),
                TargetBoneIndex = ReadUInt16()
            };
            byte childSize = ReadByte();
            o.Iterations = ReadUInt16();
            o.LimitAngle = ReadFloat();
            o.IKChildBoneIndex = ReadList(childSize, ReadUInt16);
            return o;
        }

        private PmdStruct.Skin ReadSkin()
        {
            var o = new PmdStruct.Skin
            {
                Name = ReadFixedStringTerminationChar(20)
            };

            int skinVertexCount = ReadInt();
            o.Kind = (PmdStruct.SkinKind) ReadByte();

            o.SkinVertices = ReadList(skinVertexCount, () => new PmdStruct.SkinVertex
            {
                VertexIndex = ReadUInt(),
                VertexPosition = ReadVector3()
            });
            return o;
        }


        private PmdStruct.RigidBody ReadRigidBody()
        {
            return new PmdStruct.RigidBody
            {
                Name = ReadFixedStringTerminationChar(20),
                RelationBoneIndex = ParameterCheck<ushort>(ReadUInt16(), 0xffff),
                GroupIndex = ReadByte(),
                GroupTarget = ReadUInt16(),
                ShapeKind = (PmdStruct.RigidBodyShapeKind) ReadByte(),
                HalfWidth = ReadFloat(),
                HalfHeight = ReadFloat(),
                HalfDepth = ReadFloat(),
                RelativePosition = ReadVector3(),
                Rotation = ReadVector3(),
                Weight = ReadFloat(),
                MoveDamping = ReadFloat(),
                RotateDamping = ReadFloat(),
                Recoil = ReadFloat(),
                Friction = ReadFloat(),
                Kind = (PmdStruct.RigidBodyKind) ReadByte()
            };
        }

        private PmdStruct.Joint ReadJoint()
        {
            return new PmdStruct.Joint
            {
                Name = ReadFixedStringTerminationChar(20),
                RigidBodyA = ReadUInt(),
                RigidBodyB = ReadUInt(),
                Position = ReadVector3(),
                Rotation = ReadVector3(),
                ConstrainLowerPosition = ReadVector3(),
                ConstrainUpperPosition = ReadVector3(),
                ConstrainLowerRotation = ReadVector3(),
                ConstrainUpperRotation = ReadVector3(),
                SpringPosition = ReadVector3(),
                SpringRotation = ReadVector3()
            };
        }

        private T? ParameterCheck<T>(T val, T invalidVal) where T : struct, IComparable<T>
        {
            return val.CompareTo(invalidVal) != 0 ? val : (T?) null;
        }

        private Color ReadColor()
        {
            return new Color
            {
                R = ReadFloat(),
                G = ReadFloat(),
                B = ReadFloat()
            };
        }
    }
}
