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
    public class PmxStruct
    {
        #region ヘッダ

        [StringLength(4, MinimumLength = 4)]
        public string Magic { get; set; } = "Pmx ";

        public float Version { get; set; } = 2.0f;

        #region Option

        public enum EncodingKind
        {
            UTF16,
            UTF8
        }

        public EncodingKind EncodingOption { get; set; } = EncodingKind.UTF8;

        public int AddUvSizeOption { get; set; } = 0;

        public int VertexSizeOption { get; set; } = 4;

        public int TextureIndexSizeOption { get; set; } = 4;

        public int MaterialIndexSizeOption { get; set; } = 4;

        public int BoneIndexSizeOption { get; set; } = 4;

        public int MorphIndexSizeOption { get; set; } = 4;

        public int RigidBodyIndexSizeOption { get; set; } = 4;

        #endregion

        public string Name { get; set; }

        public string EnglishName { get; set; }

        public string Comment { get; set; }

        public string CommentEnglish { get; set; }

        #endregion

        #region 頂点

        public enum BoneWeightKind
        {
            BDEF1,
            BDEF2,
            BDEF4,
            SDEF
        }

        public class Bdef
        {
            public int BoneIndex { get; set; }

            public float Weight { get; set; }
        }

        public class Sdef
        {
            public int[] BoneIndex { get; set; }

            public float Weight { get; set; }

            public Vector3 C { get; set; }

            public Vector3 R0 { get; set; }

            public Vector3 R1 { get; set; }
        }

        public class Vertex
        {
            public Vector3 Position { get; set; }

            public Vector3 Normal { get; set; }

            public Vector2 UV { get; set; }

            public Vector4[] ExtensionUV { get; set; }

            public BoneWeightKind WeightKind { get; set; }

            public List<Bdef> BdefN { get; set; }

            public Sdef Sdef { get; set; }

            public float EdgeMagnification { get; set; }
        }

        public List<Vertex> Vertices { get; set; }

        #endregion

        public List<int> Indices { get; set; }

        public List<string> TexturePath { get; set; }


        public class Material
        {
            public string Name { get; set; }

            public string EnglishName { get; set; }

            public ColorA Diffuse { get; set; }

            public Color Specular { get; set; }

            public float Specularity { get; set; }

            public Color Ambient { get; set; }

            [Flags]
            public enum RenderFlag
            {
                /// <summary>
                /// 両面描画
                /// </summary>
                Reversible = 1 << 0,

                /// <summary>
                /// 地面影
                /// </summary>
                CastShadow = 1 << 1,

                /// <summary>
                /// セルフシャドウマップへの描画
                /// </summary>
                CastSelfShadow = 1 << 2,

                /// <summary>
                /// セルフシャドウの描画
                /// </summary>
                ReceiveSelfShadow = 1 << 3,

                /// <summary>
                /// エッジ描画
                /// </summary>
                Edge = 1 << 4
            }

            public RenderFlag RenderFlags { get; set; }

            public ColorA EdgeColor { get; set; }

            public float EdgeSize { get; set; }

            public int? TextureIndex { get; set; }

            public int SphereTextureIndex { get; set; }

            public enum SphereModes
            {
                Disabled,
                Mul,
                Add,
                SubTexture
            }

            public SphereModes SphereMode { get; set; } = SphereModes.Disabled;

            public bool IsCommonToon { get; set; }

            public int ToonTextureIndex { get; set; }

            public string Memo { get; set; }

            public uint FaceVertexCount { get; set; }
        }

        public List<Material> Materials { get; set; }

        public class Bone
        {
            public string Name { get; set; }

            public string EnglishName { get; set; }

            public Vector3 Position { get; set; }

            public int? ParentBoneIndex { get; set; }

            public int TransformLevel { get; set; }

            [Flags]
            public enum Flag
            {
                /// <summary>
                /// 接続先(PMD子ボーン指定)表示方法(ON:ボーンで指定 OFF:座標オフセットで指定)
                /// </summary>
                Connection = 1 << 0,

                /// <summary>
                /// 回転可能
                /// </summary>
                Rotatable = 1 << 1,

                /// <summary>
                /// 移動可能
                /// </summary>
                Movable = 1 << 2,

                /// <summary>
                /// 表示
                /// </summary>
                DisplayFlag = 1 << 3,

                /// <summary>
                /// 操作可
                /// </summary>
                CanOperate = 1 << 4,

                /// <summary>
                /// IK
                /// </summary>
                IkFlag = 1 << 5,

                /// <summary>
                /// ローカル付与 | 付与対象(ON:親のローカル変形量 OFF:ユーザー変形値／IKリンク／多重付与)
                /// </summary>
                AddLocal = 1 << 7,

                /// <summary>
                /// 回転付与
                /// </summary>
                AddRotation = 1 << 8,

                /// <summary>
                /// 移動付与
                /// </summary>
                AddMove = 1 << 9,

                /// <summary>
                /// 軸固定
                /// </summary>
                FixedAxis = 1 << 10,

                /// <summary>
                /// ローカル軸
                /// </summary>
                LocalAxis = 1 << 11,

                /// <summary>
                /// 物理後変形
                /// </summary>
                PhysicsTransform = 1 << 12,

                /// <summary>
                /// 外部親変形
                /// </summary>
                ExternalParentTransform = 1 << 13
            }

            public Flag Flags { get; set; }

            /// <summary>
            /// 接続先:OFF の場合
            /// </summary>
            public Vector3 PositionOffset { get; set; }

            /// <summary>
            /// 接続先:ON の場合
            /// </summary>
            public int ConnectionBoneIndex { get; set; }

            #region 回転付与:ON または 移動付与:OFF の場合

            public int AddParentBoneIndex { get; set; }

            public float AddRate { get; set; }

            #endregion

            /// <summary>
            /// 軸固定:ON の場合
            /// </summary>
            public Vector3 FixedAxisVector { get; set; }

            #region ローカル軸:ON の場合

            public Vector3 LocalAxisXVector { get; set; }

            public Vector3 LocalAxisZVector { get; set; }

            #endregion

            /// <summary>
            /// 外部親変形:ON の場合
            /// </summary>
            public int ExternalParentTransformKey { get; set; }

            #region IK:ON の場合

            public class IKLink
            {
                public int BoneIndex { get; set; }

                /// <summary>
                /// ラジアン角
                /// </summary>
                public Vector3 LowerLimit { get; set; }

                /// <summary>
                /// ラジアン角
                /// </summary>
                public Vector3 UpperLimit { get; set; }
            }

            public class IKData
            {
                public int TargetBoneIndex { get; set; }

                public int Iterations { get; set; }

                /// <summary>
                /// ラジアン角
                /// </summary>
                public float LimitAngle { get; set; }

                public List<IKLink> IKLinks { get; set; } = new List<IKLink>();
            }

            public IKData IK { get; set; }

            #endregion
        }

        public List<Bone> Bones { get; set; }

        public class Morph
        {
            public string Name { get; set; }

            public string NameEnglish { get; set; }

            public enum MorphPanel
            {
                Base,
                Eyebrow,
                Eye,
                Lip,
                Others
            }

            public MorphPanel Panel { get; set; }

            public enum MorphKind
            {
                Group,
                Vertex,
                Bone,
                UV,
                ExUV1,
                ExUV2,
                ExUV3,
                ExUV4,
                Material
            }

            public MorphKind Kind { get; set; }

            public interface IMorphOffset { }

            public class VertexMorph : IMorphOffset
            {
                public uint VertexIndex { get; set; }

                public Vector3 PositionOffset { get; set; }
            }

            public class UVMorph : IMorphOffset
            {
                public int VertexIndex { get; set; }

                public Vector4 UVOffset { get; set; }
            }

            public class BoneMorph : IMorphOffset
            {
                public int BoneIndex { get; set; }

                public Vector3 MoveValue { get; set; }

                public Quaternion RotateValue { get; set; }
            }

            public class MaterialMorph : IMorphOffset
            {
                public int MaterialIndex { get; set; }

                public enum CalcKind
                {
                    Mul,
                    Add
                }

                public CalcKind Kind { get; set; }

                public ColorA Diffuse { get; set; }

                public Color Specular { get; set; }

                public float Specularity { get; set; }

                public Color Ambient { get; set; }

                public ColorA EdgeColor { get; set; }

                public float EdgeSize { get; set; }

                public ColorA TextureCoefficient { get; set; }

                public ColorA SphereTextureCoefficient { get; set; }

                public ColorA ToonTextureCoefficient { get; set; }
            }

            public class GroupMorph : IMorphOffset
            {
                public int MorphIndex { get; set; }

                public float MorphRate { get; set; }
            }

            public List<IMorphOffset> Offsets { get; set; }
        }

        public List<Morph> Morphs { get; set; }

        public class Disp
        {
            public string Name { get; set; }

            public string NameEnglish { get; set; }

            public bool IsExtension { get; set; }

            public class Data
            {
                public enum IndexKind
                {
                    Bone,
                    Morph
                }

                public IndexKind Kind { get; set; }

                public int Index { get; set; }
            }

            public List<Data> DataList { get; set; }
        }

        public List<Disp> Disps { get; set; }

        public class RigidBody
        {
            public string Name { get; set; }

            public string NameEnglish { get; set; }

            public int? RelativeBoneIndex { get; set; }

            public byte Group { get; set; }

            public ushort IgnoreCollisionGroup { get; set; }

            public enum ShapeKind
            {
                Sphere,
                Box,
                Capsule
            }

            public ShapeKind Kind { get; set; }

            public Vector3 Size { get; set; }

            public Vector3 Position { get; set; }

            /// <summary>
            /// ラジアン角
            /// </summary>
            public Vector3 Rotation { get; set; }

            public float Mass { get; set; }

            /// <summary>
            /// 移動減衰
            /// </summary>
            public float PositionDim { get; set; }

            /// <summary>
            /// 回転減衰
            /// </summary>
            public float RotationDim { get; set; }

            public float Recoil { get; set; }

            public float Friction { get; set; }

            public enum OperationKinds
            {
                /// <summary>
                /// ボーン追従
                /// </summary>
                Static,

                /// <summary>
                /// 物理演算
                /// </summary>
                Dynamic,

                /// <summary>
                /// 物理演算（ボーン位置合わせ）
                /// </summary>
                DynamicAndPositionAdjust
            }

            public OperationKinds OperationKind { get; set; }
        }

        public List<RigidBody> RigidBodies { get; set; }


        public class Joint
        {
            public string Name { get; set; }

            public string NameEnglish { get; set; }

            public enum JointKind
            {
                Spring6Dof
            }

            public JointKind Kind { get; set; }

            public int? RigidBodyIndexA { get; set; }

            public int? RigidBodyIndexB { get; set; }

            public Vector3 Position { get; set; }

            public Vector3 Rotation { get; set; }

            public Vector3 ConstrainLowerPosition { get; set; }

            public Vector3 ConstrainUpperPosition { get; set; }

            /// <summary>
            /// ラジアン角
            /// </summary>
            public Vector3 ConstrainLowerRotation { get; set; }

            /// <summary>
            /// ラジアン角
            /// </summary>
            public Vector3 ConstrainUpperRotation { get; set; }

            public Vector3 SpringPosition { get; set; }

            public Vector3 SpringRotation { get; set; }
        }

        public List<Joint> Joints { get; set; }
    }
}
