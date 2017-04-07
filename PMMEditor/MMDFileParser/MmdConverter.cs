using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D9;

namespace PMMEditor.MMDFileParser
{
    public static class MmdConverter
    {
        public static PmxStruct ToPmx(this PmdStruct pmd)
        {
            var o = new PmxStruct
            {
                EnglishName = pmd.EnglishData?.ModelName ?? "",
                Name = pmd.Name,
                Comment = pmd.Comment,
                CommentEnglish = pmd.EnglishData?.Comment ?? "",
                Vertices = new List<PmxStruct.Vertex>(pmd.Vertices.Count)
            };
            foreach (var vertex in pmd.Vertices)
            {
                o.Vertices.Add(new PmxStruct.Vertex
                {
                    Position = vertex.Position,
                    Normal = vertex.NormalVector,
                    EdgeMagnification = vertex.IsEdgeEnabled ? 1 : 0,
                    UV = vertex.UV,
                    WeightKind = PmxStruct.BoneWeightKind.BDEF2,
                    BdefN = new List<PmxStruct.Bdef>
                    {
                        new PmxStruct.Bdef
                        {
                            BoneIndex = vertex.BoneNum1,
                            Weight = vertex.BoneWeight / 100.0f
                        },
                        new PmxStruct.Bdef
                        {
                            BoneIndex = vertex.BoneNum2,
                            Weight = 1.0f - (vertex.BoneWeight / 100.0f)
                        }
                    },
                    ExtensionUV = new Vector4[0]
                });
            }

            o.Indices = pmd.Indices.Select(_ => (int) _).ToList();

            var texturePathIndex = new Dictionary<string, int>();
            int nowIndexCount = 0;
            o.Materials = new List<PmxStruct.Material>(pmd.Materials.Count);
            foreach (var (material, i) in pmd.Materials.Select((_, i) => (_, i)))
            {
                var mat = new PmxStruct.Material
                {
                    Name = "材質" + (i + 1),
                    EnglishName = "",
                    Diffuse = material.Diffuse,
                    Specular = material.Specular,
                    Specularity = material.Specularity,
                    Ambient = material.Ambient,
                    EdgeColor = new ColorA(0, 0, 0, 1),
                    EdgeSize = 1.0f
                };
                if (!string.IsNullOrEmpty(material.TextureFilename))
                {
                    if (!texturePathIndex.TryGetValue(material.TextureFilename, out int textureIndex))
                    {
                        textureIndex = nowIndexCount;
                        texturePathIndex.Add(material.TextureFilename, nowIndexCount++);
                    }
                    mat.TextureIndex = textureIndex;
                }

                if (!string.IsNullOrEmpty(material.SphereTextureName))
                {
                    if (!texturePathIndex.TryGetValue(material.SphereTextureName, out int textureIndex))
                    {
                        textureIndex = nowIndexCount;
                        texturePathIndex.Add(material.SphereTextureName, nowIndexCount++);
                    }
                    mat.SphereTextureIndex = textureIndex;
                    string ext = Path.GetExtension(material.SphereTextureName);
                    mat.SphereMode = ext == ".spa"
                        ? PmxStruct.Material.SphereModes.Add
                        : PmxStruct.Material.SphereModes.Mul;
                }

                mat.IsCommonToon = true;
                mat.ToonTextureIndex = material.ToonIndex;

                if (material.Diffuse.A <= 1.0f)
                {
                    mat.RenderFlags |= PmxStruct.Material.RenderFlag.Reversible;
                }
                if (material.IsEdge)
                {
                    mat.RenderFlags |= PmxStruct.Material.RenderFlag.Edge
                                       | PmxStruct.Material.RenderFlag.CastSelfShadow
                                       | PmxStruct.Material.RenderFlag.CastShadow;
                }
                if (Math.Abs(0.98f - material.Diffuse.A) > 1e-5)
                {
                    mat.RenderFlags |= PmxStruct.Material.RenderFlag.ReceiveSelfShadow;
                }

                mat.Memo = "";
                mat.FaceVertexCount = material.FaceVertexCount;

                o.Materials.Add(mat);
            }

            o.TexturePath = texturePathIndex.Keys.ToList();

            o.Bones = new List<PmxStruct.Bone>(pmd.Bones.Count);
            foreach (var (bone, i) in pmd.Bones.Indexed())
            {
                var res = new PmxStruct.Bone
                {
                    Name = bone.Name,
                    EnglishName = pmd.EnglishData?.BoneName?[i] ?? "",
                    Position = bone.Position,
                    ParentBoneIndex = bone.ParentBoneIndex,
                    TransformLevel = 0,
                    PositionOffset = Vector3.Zero,
                    LocalAxisXVector = Vector3.Zero,
                    LocalAxisZVector = Vector3.Zero,
                    ConnectionBoneIndex = 0,
                    ExternalParentTransformKey = 0,
                    AddRate = 0,
                    FixedAxisVector = Vector3.Zero,
                    AddParentBoneIndex = 0
                };

                switch (bone.Kind)
                {
                    case PmdStruct.BoneKind.Rotate:
                        res.Flags |= PmxStruct.Bone.Flag.Rotatable
                                     | PmxStruct.Bone.Flag.DisplayFlag
                                     | PmxStruct.Bone.Flag.CanOperate;
                        break;
                    case PmdStruct.BoneKind.RotateAndMove:
                        res.Flags |= PmxStruct.Bone.Flag.Movable;
                        goto case PmdStruct.BoneKind.Rotate;
                    case PmdStruct.BoneKind.IK:
                        res.Flags |= PmxStruct.Bone.Flag.IkFlag
                                     | PmxStruct.Bone.Flag.DisplayFlag
                                     | PmxStruct.Bone.Flag.CanOperate;
                        break;
                    case PmdStruct.BoneKind.Unknown:
                    case PmdStruct.BoneKind.IKAffected:
                    case PmdStruct.BoneKind.RotateAffected:
                    case PmdStruct.BoneKind.IKTarget:
                    case PmdStruct.BoneKind.Invisible:
                    case PmdStruct.BoneKind.Twist:
                    case PmdStruct.BoneKind.RotationAssociated:
                    default:
                        break;
                }

                if (bone.TailBoneIndex != null)
                {
                    res.ConnectionBoneIndex = (int) bone.TailBoneIndex;
                    res.Flags |= PmxStruct.Bone.Flag.Connection;
                }

                PmdStruct.IK ik = pmd.IKs.Find(_ => _.BoneIndex == i);
                if (ik != null)
                {
                    res.IK = new PmxStruct.Bone.IKData
                    {
                        TargetBoneIndex = ik.BoneIndex,
                        Iterations = ik.Iterations,
                        LimitAngle = ik.LimitAngle * 4.0f,
                        IKLinks = new List<PmxStruct.Bone.IKLink>(ik.IKChildBoneIndex.Count)
                    };
                    foreach (var child in ik.IKChildBoneIndex)
                    {
                        res.IK.IKLinks.Add(new PmxStruct.Bone.IKLink
                        {
                            BoneIndex = child,
                            UpperLimit = Vector3.One * (float) Math.PI,
                            LowerLimit = Vector3.One * (float) -Math.PI
                        });
                    }
                }
                else
                {
                    res.IK = new PmxStruct.Bone.IKData();
                }

                o.Bones.Add(res);
            }

            o.Morphs = ConvertMorph(pmd);
            // TODO: Disps, RigidBody, Joint
            return o;
        }

        private static List<PmxStruct.Morph> ConvertMorph(PmdStruct pmd)
        {
            var res = new List<PmxStruct.Morph>(pmd.Morphs.Count);
            if (pmd.Morphs.Count == 0)
            {
                return res;
            }

            var baseSkin = new Dictionary<uint, uint>();
            PmdStruct.Skin baseMorph = pmd.Morphs.First(_ => _.SkinPanel == PmdStruct.SkinKind.Base);
            foreach (var (skin, i) in baseMorph.SkinVertices.Indexed())
            {
                baseSkin.Add((uint) i, skin.VertexIndex);
            }

            foreach (var (morph, i) in pmd.Morphs.Where(_ => _.SkinPanel != PmdStruct.SkinKind.Base).Indexed())
            {
                var morphRes = new PmxStruct.Morph
                {
                    Name = morph.Name,
                    NameEnglish = pmd.EnglishData?.SkinName[i],
                    Kind = PmxStruct.Morph.MorphKind.Vertex
                };

                switch (morph.SkinPanel)
                {
                    case PmdStruct.SkinKind.Base:
                        morphRes.Panel = PmxStruct.Morph.MorphPanel.Base;
                        break;
                    case PmdStruct.SkinKind.Eyebrow:
                        morphRes.Panel = PmxStruct.Morph.MorphPanel.Eyebrow;
                        break;
                    case PmdStruct.SkinKind.Eye:
                        morphRes.Panel = PmxStruct.Morph.MorphPanel.Eye;
                        break;
                    case PmdStruct.SkinKind.Lip:
                        morphRes.Panel = PmxStruct.Morph.MorphPanel.Lip;
                        break;
                    case PmdStruct.SkinKind.Others:
                        morphRes.Panel = PmxStruct.Morph.MorphPanel.Others;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                morphRes.Offsets = new List<PmxStruct.Morph.IMorphOffset>();
                foreach (var vertex in morph.SkinVertices)
                {
                    morphRes.Offsets.Add(new PmxStruct.Morph.VertexMorph
                    {
                        VertexIndex = baseSkin[vertex.VertexIndex]
                    });
                }

                res.Add(morphRes);
            }

            return res;
        }
    }
}
