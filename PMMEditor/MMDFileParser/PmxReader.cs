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
    internal class PmxReader : MmdFileReaderBase
    {
        public PmxReader(byte[] binaryData) : base(new MemoryStream(binaryData)) { }

        public static bool MagicNumberEqual(byte[] binaryData)
        {
            return Encoding.ASCII.GetString(binaryData, 0, 4) == "PMX ";
        }

        public PmxStruct Read()
        {
            var o = new PmxStruct
            {
                Magic = ReadFixedString(4, Encoding.ASCII)
            };
            if (o.Magic != "PMX ")
            {
                throw new ArgumentException("Magic id is not \"PMX \"");
            }

            o.Version = ReadFloat();
            if (Math.Abs(o.Version - 2.0f) > 1e-5)
            {
                throw new NotSupportedException("Pmx not supprted version:" + o.Version);
            }

            byte optionByte = ReadByte();
            if (optionByte != 8)
            {
                throw new InvalidOperationException(nameof(optionByte));
            }

            {
                byte encodingKind = ReadByte();
                switch (encodingKind)
                {
                    case 0:
                        o.EncodingOption = PmxStruct.EncodingKind.UTF16;
                        DefaultEncoding = Encoding.Unicode;
                        break;
                    case 1:
                        o.EncodingOption = PmxStruct.EncodingKind.UTF8;
                        DefaultEncoding = Encoding.UTF8;
                        break;
                    default:
                        throw new InvalidOperationException(nameof(encodingKind));
                }
            }

            o.AddUvSizeOption = ReadByte();
            if (4 < o.AddUvSizeOption)
            {
                throw new InvalidOperationException(nameof(o.AddUvSizeOption));
            }

            void CheckObjSize(int size, string errorMessage)
            {
                // 取り合える値は1,2,4のいずれか
                if (size < 0 || 4 < size || size == 3)
                {
                    throw new InvalidOperationException(errorMessage);
                }
            }

            int ReadOption(string errorMessage)
            {
                int size = ReadByte();
                CheckObjSize(size, errorMessage);
                return size;
            }

            o.VertexSizeOption = ReadOption(nameof(o.VertexSizeOption));
            o.TextureIndexSizeOption = ReadOption(nameof(o.TextureIndexSizeOption));
            o.MaterialIndexSizeOption = ReadOption(nameof(o.MaterialIndexSizeOption));
            o.BoneIndexSizeOption = ReadOption(nameof(o.BoneIndexSizeOption));
            o.MorphIndexSizeOption = ReadOption(nameof(o.MorphIndexSizeOption));
            o.RigidBodyIndexSizeOption = ReadOption(nameof(o.RigidBodyIndexSizeOption));

            o.Name = ReadVIntString();
            o.NameEnglish = ReadVIntString();
            o.Comment = ReadVIntString();
            o.CommentEnglish = ReadVIntString();

            o.Vertices = ReadVIntList(() => ReadVertex(o));
            o.indices = ReadVIntList(() => ReadSizeOption(o.VertexSizeOption));
            o.TexturePath = ReadVIntList(ReadVIntString);
            o.Materials = ReadVIntList(() => ReadMaterial(o));
            o.Bones = ReadVIntList(() => ReadBone(o));
            o.Morphs = ReadVIntList(() => ReadMorph(o));
            o.Disps = ReadVIntList(() => ReadDisp(o));
            o.RigidBodies = ReadVIntList(() => ReadRigidBody(o));
            o.Joints = ReadVIntList(() =>
            {
                var joint = new PmxStruct.Joint
                {
                    Name = ReadVIntString(),
                    NameEnglish = ReadVIntString()
                };
                switch (ReadByte())
                {
                    case 0:
                        joint.Kind = PmxStruct.Joint.JointKind.Spring6Dof;
                        break;
                    default:
                        throw new InvalidOperationException("Joint.Kind");
                }

                int? CheckInvalidData(int data, int InvalidValue)
                {
                    return data == InvalidValue ? null : (int?) data;
                }

                joint.RigidBodyIndexA = CheckInvalidData(ReadSizeOption(o.RigidBodyIndexSizeOption), -1);
                joint.RigidBodyIndexB = CheckInvalidData(ReadSizeOption(o.RigidBodyIndexSizeOption), -1);

                joint.Position = ReadVector3();
                joint.Rotation = ReadVector3();

                joint.ConstrainLowerPosition = ReadVector3();
                joint.ConstrainUpperPosition = ReadVector3();
                joint.ConstrainLowerRotation = ReadVector3();
                joint.ConstrainUpperRotation = ReadVector3();

                joint.SpringPosition = ReadVector3();
                joint.SpringRotation = ReadVector3();
                return joint;
            });

            return o;
        }

        private ColorA ReadColorA()
        {
            return new ColorA
            {
                R = ReadFloat(),
                G = ReadFloat(),
                B = ReadFloat(),
                A = ReadFloat()
            };
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

        private int ReadSizeOption(int size)
        {
            switch (size)
            {
                case 1:
                    return ReadByte();
                case 2:
                    return ReadInt16();
                case 3:
                    return ReadInt();
                default:
                    throw new InvalidOperationException();
            }
        }

        private PmxStruct.Vertex ReadVertex(PmxStruct o)
        {
            var vertex = new PmxStruct.Vertex
            {
                Position = ReadVector3(),
                Normal = ReadVector3(),
                UV = ReadVector2(),
                ExtensionUV = new Vector4[o.AddUvSizeOption]
            };
            for (int i = 0; i < o.AddUvSizeOption; i++)
            {
                vertex.ExtensionUV[i] = ReadVector4();
            }

            switch (ReadByte())
            {
                case 0:
                    vertex.WeightKind = PmxStruct.BoneWeightKind.BDEF1;
                    vertex.BdefN = new List<PmxStruct.Bdef>
                    {
                        new PmxStruct.Bdef
                        {
                            BoneIndex = ReadSizeOption(o.BoneIndexSizeOption),
                            Weight = 1.0f
                        }
                    };
                    break;
                case 1:
                    vertex.WeightKind = PmxStruct.BoneWeightKind.BDEF2;
                    vertex.BdefN = new List<PmxStruct.Bdef>();
                    for (int i = 0; i < 2; i++)
                    {
                        vertex.BdefN.Add(new PmxStruct.Bdef
                        {
                            BoneIndex = ReadSizeOption(o.BoneIndexSizeOption)
                        });
                    }

                    vertex.BdefN[0].Weight = ReadFloat();
                    vertex.BdefN[1].Weight = 1.0f - vertex.BdefN[0].Weight;
                    break;
                case 2:
                    vertex.WeightKind = PmxStruct.BoneWeightKind.BDEF4;
                    vertex.BdefN = new List<PmxStruct.Bdef>();
                    for (int i = 0; i < 4; i++)
                    {
                        vertex.BdefN.Add(new PmxStruct.Bdef
                        {
                            BoneIndex = ReadSizeOption(o.BoneIndexSizeOption)
                        });
                    }
                    foreach (var bdef in vertex.BdefN)
                    {
                        bdef.Weight = ReadFloat();
                    }

                    break;
                case 3:
                    vertex.WeightKind = PmxStruct.BoneWeightKind.SDEF;
                    vertex.Sdef = new PmxStruct.Sdef
                    {
                        BoneIndex = new[]
                        {
                            ReadSizeOption(o.BoneIndexSizeOption), ReadSizeOption(o.BoneIndexSizeOption)
                        },
                        Weight = ReadFloat(),
                        C = ReadVector3(),
                        R0 = ReadVector3(),
                        R1 = ReadVector3()
                    };
                    break;
                default:
                    throw new InvalidOperationException("BoneWeightKind");
            }

            vertex.EdgeMagnification = ReadFloat();
            return vertex;
        }

        private PmxStruct.Material ReadMaterial(PmxStruct o)
        {
            var material = new PmxStruct.Material
            {
                Name = ReadVIntString(),
                NameEnglish = ReadVIntString(),
                Diffuse = ReadColorA(),
                Specular = ReadColor(),
                Specularity = ReadFloat(),
                Ambient = ReadColor(),
                RenderFlags = (PmxStruct.Material.RenderFlag) ReadByte(),
                EdgeColor = ReadColorA(),
                EdgeSize = ReadFloat(),
                TextureIndex = ReadSizeOption(o.TextureIndexSizeOption),
                SphereTextureIndex = ReadSizeOption(o.TextureIndexSizeOption)
            };
            byte sphereMode = ReadByte();
            if (4 <= sphereMode)
            {
                throw new InvalidOperationException(nameof(material.SphereMode));
            }

            material.SphereMode = (PmxStruct.Material.SphereModes) sphereMode;

            switch (ReadByte())
            {
                case 0:
                    material.IsCommonToon = false;
                    material.ToonTextureIndex = ReadByte();
                    break;
                case 1:
                    material.IsCommonToon = true;
                    material.ToonTextureIndex = ReadByte();
                    if (10 <= material.ToonTextureIndex)
                    {
                        throw new InvalidOperationException(nameof(material.ToonTextureIndex));
                    }

                    break;
                default:
                    throw new InvalidOperationException(nameof(material.IsCommonToon));
            }

            material.Memo = ReadVIntString();
            material.FaceVertexCount = ReadInt();
            if (material.FaceVertexCount % 3 != 0)
            {
                throw new InvalidOperationException(nameof(material.FaceVertexCount));
            }

            return material;
        }

        private PmxStruct.Bone ReadBone(PmxStruct o)
        {
            var bone = new PmxStruct.Bone
            {
                Name = ReadVIntString(),
                NameEnglish = ReadVIntString(),
                Position = ReadVector3(),
                ParentBoneIndex = ReadSizeOption(o.BoneIndexSizeOption),
                TransformLevel = ReadInt(),
                Flags = (PmxStruct.Bone.Flag) ReadInt16()
            };
            if ((bone.Flags & PmxStruct.Bone.Flag.Connection) != 0)
            {
                bone.ConnectionBoneIndex = ReadSizeOption(o.BoneIndexSizeOption);
            }
            else
            {
                bone.PositionOffset = ReadVector3();
            }

            if ((bone.Flags & PmxStruct.Bone.Flag.AddRotation) != 0 || (bone.Flags & PmxStruct.Bone.Flag.AddMove) != 0)
            {
                bone.AddParentBoneIndex = ReadSizeOption(o.BoneIndexSizeOption);
                bone.AddRate = ReadFloat();
            }

            if ((bone.Flags & PmxStruct.Bone.Flag.FixedAxis) != 0)
            {
                bone.FixedAxisVector = ReadVector3();
            }

            if ((bone.Flags & PmxStruct.Bone.Flag.LocalAxis) != 0)
            {
                bone.LocalAxisXVector = ReadVector3();
                bone.LocalAxisZVector = ReadVector3();
            }

            if ((bone.Flags & PmxStruct.Bone.Flag.ExternalParentTransform) != 0)
            {
                bone.ExternalParentTransformKey = ReadInt();
            }

            if ((bone.Flags & PmxStruct.Bone.Flag.IkFlag) != 0)
            {
                bone.IK = new PmxStruct.Bone.IKData
                {
                    TargetBoneIndex = ReadSizeOption(o.BoneIndexSizeOption),
                    Iterations = ReadInt(),
                    LimitAngle = ReadFloat()
                };
                int ikLinkNum = ReadInt();
                for (int i = 0; i < ikLinkNum; i++)
                {
                    var link = new PmxStruct.Bone.IKLink
                    {
                        BoneIndex = ReadSizeOption(o.BoneIndexSizeOption)
                    };
                    switch (ReadByte())
                    {
                        case 0:
                            link.LowerLimit = new Vector3((float) -Math.PI, (float) -Math.PI, (float) -Math.PI);
                            link.UpperLimit = new Vector3((float) Math.PI, (float) Math.PI, (float) Math.PI);
                            break;
                        case 1:
                            link.LowerLimit = ReadVector3();
                            link.UpperLimit = ReadVector3();
                            break;
                        default:
                            throw new InvalidOperationException("IsIKLimitAngle");
                    }

                    bone.IK.IKLinks.Add(link);
                }
            }

            return bone;
        }

        private PmxStruct.Morph ReadMorph(PmxStruct o)
        {
            var morph = new PmxStruct.Morph
            {
                Name = ReadVIntString(),
                NameEnglish = ReadVIntString()
            };

            byte morphPanel = ReadByte();
            if (5 <= morphPanel)
            {
                throw new InvalidOperationException(nameof(morphPanel));
            }

            morph.Panel = (PmxStruct.Morph.MorphPanel) morphPanel;

            byte morphKind = ReadByte();
            if (9 <= morphKind)
            {
                throw new InvalidOperationException(nameof(morphKind));
            }

            morph.Kind = (PmxStruct.Morph.MorphKind) morphKind;

            morph.Offsets = ReadVIntList(() =>
            {
                PmxStruct.Morph.IMorphOffset offset;
                switch (morph.Kind)
                {
                    case PmxStruct.Morph.MorphKind.Group:
                        offset = new PmxStruct.Morph.GroupMorph
                        {
                            MorphIndex = ReadSizeOption(o.MorphIndexSizeOption),
                            MorphRate = ReadFloat()
                        };
                        break;
                    case PmxStruct.Morph.MorphKind.Vertex:
                        offset = new PmxStruct.Morph.VertexMorph
                        {
                            VertexIndex = ReadSizeOption(o.VertexSizeOption),
                            PositionOffset = ReadVector3()
                        };
                        break;
                    case PmxStruct.Morph.MorphKind.Bone:
                        offset = new PmxStruct.Morph.BoneMorph
                        {
                            BoneIndex = ReadSizeOption(o.BoneIndexSizeOption),
                            MoveValue = ReadVector3(),
                            RotateValue = new Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat())
                        };
                        break;
                    case PmxStruct.Morph.MorphKind.UV:
                    case PmxStruct.Morph.MorphKind.ExUV1:
                    case PmxStruct.Morph.MorphKind.ExUV2:
                    case PmxStruct.Morph.MorphKind.ExUV3:
                    case PmxStruct.Morph.MorphKind.ExUV4:
                        offset = new PmxStruct.Morph.UVMorph
                        {
                            VertexIndex = ReadSizeOption(o.VertexSizeOption),
                            UVOffset = ReadVector4()
                        };
                        break;
                    case PmxStruct.Morph.MorphKind.Material:
                        var materialMorph = new PmxStruct.Morph.MaterialMorph
                        {
                            MaterialIndex = ReadSizeOption(o.MaterialIndexSizeOption)
                        };
                        switch (ReadByte())
                        {
                            case 0:
                                materialMorph.Kind = PmxStruct.Morph.MaterialMorph.CalcKind.Mul;
                                break;
                            case 1:
                                materialMorph.Kind = PmxStruct.Morph.MaterialMorph.CalcKind.Add;
                                break;
                            default:
                                throw new InvalidOperationException("MaterialMorph.Kind");
                        }

                        materialMorph.Diffuse = ReadColorA();
                        materialMorph.Specular = ReadColor();
                        materialMorph.Specularity = ReadFloat();
                        materialMorph.Ambient = ReadColor();
                        materialMorph.EdgeColor = ReadColorA();
                        materialMorph.EdgeSize = ReadFloat();
                        materialMorph.TextureCoefficient = ReadColorA();
                        materialMorph.SphereTextureCoefficient = ReadColorA();
                        materialMorph.ToonTextureCoefficient = ReadColorA();
                        offset = materialMorph;
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                return offset;
            });
            return morph;
        }

        PmxStruct.Disp ReadDisp(PmxStruct o)
        {
            var disp = new PmxStruct.Disp
            {
                Name = ReadVIntString(),
                NameEnglish = ReadVIntString()
            };
            switch (ReadByte())
            {
                case 0:
                    disp.IsExtension = false;
                    break;
                case 1:
                    disp.IsExtension = true;
                    break;
                default:
                    throw new InvalidOperationException("Disp.Flag");
            }

            int size = ReadInt();
            disp.DataList = new List<PmxStruct.Disp.Data>(size);
            for (int i = 0; i < size; i++)
            {
                var data = new PmxStruct.Disp.Data();
                switch (ReadByte())
                {
                    case 0:
                        data.Kind = PmxStruct.Disp.Data.IndexKind.Bone;
                        data.Index = ReadSizeOption(o.BoneIndexSizeOption);
                        break;
                    case 1:
                        data.Kind = PmxStruct.Disp.Data.IndexKind.Morph;
                        data.Index = ReadSizeOption(o.MorphIndexSizeOption);
                        break;
                    default:
                        throw new InvalidOperationException("Disp.Data.Kind");
                }
            }

            return disp;
        }

        PmxStruct.RigidBody ReadRigidBody(PmxStruct o)
        {
            int? CheckInvalidData(int data, int invalidValue)
            {
                return data == invalidValue ? null : (int?) data;
            }

            var rigidBody = new PmxStruct.RigidBody
            {
                Name = ReadVIntString(),
                NameEnglish = ReadVIntString(),
                RelativeBoneIndex = CheckInvalidData(ReadSizeOption(o.BoneIndexSizeOption), -1),
                Group = ReadByte(),
                IgnoreCollisionGroup = ReadUInt16()
            };
            switch (ReadByte())
            {
                case 0:
                    rigidBody.Kind = PmxStruct.RigidBody.ShapeKind.Sphere;
                    break;
                case 1:
                    rigidBody.Kind = PmxStruct.RigidBody.ShapeKind.Box;
                    break;
                case 2:
                    rigidBody.Kind = PmxStruct.RigidBody.ShapeKind.Capsule;
                    break;

                default:
                    throw new InvalidOperationException("RigidBody.ShapeKind");
            }

            rigidBody.Size = ReadVector3();
            rigidBody.Position = ReadVector3();
            rigidBody.Rotation = ReadVector3();
            rigidBody.Mass = ReadFloat();
            rigidBody.PositionDim = ReadFloat();
            rigidBody.RotationDim = ReadFloat();
            rigidBody.Recoil = ReadFloat();
            rigidBody.Friction = ReadFloat();
            switch (ReadByte())
            {
                case 0:
                    rigidBody.OperationKind = PmxStruct.RigidBody.OperationKinds.Static;
                    break;
                case 1:
                    rigidBody.OperationKind = PmxStruct.RigidBody.OperationKinds.Dynamic;
                    break;
                case 2:
                    rigidBody.OperationKind = PmxStruct.RigidBody.OperationKinds.DynamicAndPositionAdjust;
                    break;
                default:
                    throw new InvalidOperationException("RigidBody.OperationKind");
            }

            return rigidBody;
        }
    }
}
