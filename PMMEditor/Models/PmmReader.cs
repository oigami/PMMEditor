using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PMMEditor.Models
{
    public class PmmStuct
    {
        public string FormatId { get; set; }

        public int ViewWidth { get; set; }

        public int ViewHeight { get; set; }

        public int FrameWidth { get; set; }

        public float EditViewAngle { get; set; }

        // public byte[ /*7*/] Unknown10 { get{get;set;} }

        #region ModelInfo

        public byte ModelCount { get; set; }

        public class ModelData
        {
            public byte Number { get; set; }

            public string Name { get; set; }

            public string NameEn { get; set; }

            public string Path { get; set; }

            // public byte Unknown11 { get{get;set;} }
            public int BoneCount { get; set; }

            public string[ /*BoneCount*/] BoneName { get; set; }

            public int MorphCount { get; set; }

            public string[ /*MorphCount*/] MorphName { get; set; }

            public int IkCount { get; set; }

            public int[ /*IkCount*/] IkIndex { get; set; }

            public int OpCount { get; set; }

            public int[ /*OpCount*/] OpIndex { get; set; }

            public byte DrawOrder { get; set; }

            public bool EditIsDisplay { get; set; }

            public int EditSelectedBone { get; set; }

            public int[ /*4*/] SkinPanel { get; set; }

            public byte FrameCount { get; set; }

            public bool[ /*FrameCount*/] IsFrameOpen { get; set; }

            public int VScroll { get; set; }

            public int LastFrame { get; set; }

            #region BoneInfo

            public class BoneInitFrame
            {
                public int DataIndex { get; set; } // 初期フレームのときは-1

                public int FrameNumber { get; set; }

                public int PreIndex { get; set; }

                public int NextIndex { get; set; }

                public byte[ /* 4 */] InterpolationX { get; set; }

                public byte[ /* 4 */] InterpolationY { get; set; }

                public byte[ /* 4 */] InterpolationZ { get; set; }

                public byte[ /* 4 */] InterpolationRotation { get; set; }

                public float[ /* 3 */] Translation { get; set; }

                public float[ /* 4 */] Quaternion { get; set; }

                public bool IsSelected { get; set; }

                public bool IsPhysicsDisabled { get; set; }
            }

            public BoneInitFrame[ /*BoneCount*/] BoneInitFrames { get; set; }

            public int BoneKeyFrameCount { get; set; }

            public BoneInitFrame[ /* BoneKeyFrameCount */] BoneKeyFrames { get; set; }

            #endregion BoneInfo

            #region MorphInfo

            public class MorphFrame
            {
                public int DataIndex { get; set; } // 初期フレームのときは-1

                public int FrameNumber { get; set; }

                public int PreIndex { get; set; }

                public int NextIndex { get; set; }

                public float Value { get; set; }

                public bool IsSelected { get; set; }
            }

            public MorphFrame[ /* MorphCount */] MorphInitFrames { get; set; }

            public int MorphKeyFrameCount { get; set; }

            public MorphFrame[ /* MorphCount */] MorphKeyFrames { get; set; }

            #endregion MorphInfo

            #region その他構成情報

            public class OpFrame
            {
                public int DataIndex { get; set; }

                public int FrameNumber { get; set; }

                public int PreIndex { get; set; }

                public int NextIndex { get; set; }

                public bool IsDisplay { get; set; }

                public bool[ /* IkCount */] IsIkEnabled { get; set; }

                public KeyValuePair<int, int>[ /* OpCount */] OpData { get; set; }

                public bool IsSelected { get; set; }
            }

            public OpFrame OpInitFrame { get; set; }

            public int OpKeyFrameCount { get; set; }

            public OpFrame[ /* OpKeyFrameCount */] OpKeyFrames { get; set; }

            #endregion その他構成情報

            #region CurrentInfo

            public class BoneCurrentData
            {
                public float[] Translation { get; set; }

                public float[] Quaternion { get; set; }

                public bool IsEditUnCommited { get; set; }

                public bool IsPhysicsDisabled { get; set; }

                public bool IsRowSelected { get; set; }
            }

            public BoneCurrentData[ /* BoneCount */] BoneCurrentDatas { get; set; }

            public float[ /* MorphCount */] MorphCurrentDatas { get; set; }

            public bool[ /* IkCount */] IsCurrentIkEnabledDatas { get; set; }

            public class OpCurrentData
            {
                public int KeyFrameBegin { get; set; }

                public int KeyFrameEnd { get; set; }

                public int ModelIndex { get; set; }

                public int ParentBoneIndex { get; set; }
            }

            public OpCurrentData[ /* OpCount */] OpCurrentDatas { get; set; }

            #endregion CurrentInfo

            #region PostInfo

            public bool IsAddBlend { get; set; } // 加算合成

            public float EdgeWidth { get; set; }

            public bool IsSelfShadowEnabled { get; set; }

            public byte CalcOrder { get; set; }

            #endregion PostInfo
        }

        public ModelData[ /*ModelCount*/] ModelDatas { get; set; }

        #endregion ModelInfo

        #region CameraInfo

        public class CameraFrame
        {
            public int DataIndex { get; set; }

            public int FrameNumber { get; set; }

            public int PreIndex { get; set; }

            public int NextIndex { get; set; }

            public float Distance { get; set; }

            public float[] EyePosition { get; set; }

            public float[] Rotation { get; set; }

            public int LookingModelIndex { get; set; } // 非選択時-1

            public int LookingBoneIndex { get; set; } // 非選択時-1

            public byte[] InterpolationX { get; set; }

            public byte[] InterpolationY { get; set; }

            public byte[] InterpolationZ { get; set; }

            public byte[] InterpolationRotation { get; set; }

            public byte[] InterpolationDistance { get; set; }

            public byte[] InterpolationAngleView { get; set; }

            public bool IsParse { get; set; }

            public int AngleView { get; set; }

            public bool IsSelected { get; set; }
        }


        public CameraFrame CameraInitFrame { get; set; }

        public int CameraKeyFrameCount { get; set; }

        public CameraFrame[] CameraKeyFrames { get; set; }

        public class CCameraCurrentData
        {
            public float[] EyePosition { get; set; }

            public float[] TargetPosition { get; set; }

            public float[] Rotation { get; set; }

            public bool IsOrthro { get; set; }
        }

        public CCameraCurrentData CameraCurrentData { get; set; }

        #endregion CameraInfo

        #region LightingInfo

        public class LightFrame
        {
            public int DataIndex { get; set; }

            public int FrameNumber { get; set; }

            public int PreIndex { get; set; }

            public int NextIndex { get; set; }

            public float R { get; set; }

            public float G { get; set; }

            public float B { get; set; }

            public float X { get; set; }

            public float Y { get; set; }

            public float Z { get; set; }

            public bool IsSelected { get; set; }
        }

        public LightFrame LightInitFrame { get; set; }

        public int LightKeyFrameCount { get; set; }

        public LightFrame[] LightKeyFrames { get; set; }

        public class CLightCurrentData
        {
            public float R { get; set; }

            public float G { get; set; }

            public float B { get; set; }

            public float X { get; set; }

            public float Y { get; set; }

            public float Z { get; set; }

            public bool IsSelected { get; set; }
        }

        public CLightCurrentData LightCurrentData { get; set; }

        #endregion LightingInfo

        #region AccessoryInfo

        public byte SelectAccessoryIndex { get; set; }

        public int AccessoryVScroll { get; set; }

        public byte AccessoryCount { get; set; }

        public string[] AccessoryName { get; set; }

        public class AccessoryData
        {
            public byte Index { get; set; }

            public string Name { get; set; }

            public string Path { get; set; }

            public byte DrawOrder { get; set; }

            // TODO
        }

        #endregion AccessoryInfo
    }

    public class PmmReader
    {
        private readonly byte[] _binaryData;

        private readonly byte[] _buffer = new byte[256];
        private Stream _stream;

        public PmmReader(byte[] binaryData)
        {
            _binaryData = binaryData;
        }

        public PmmStuct Read()
        {
            var data = new PmmStuct();
            _stream = new MemoryStream(_binaryData);
            data.FormatId = string.Concat(ReadFixedString(30).TakeWhile(s => s != '\0'));
            if (data.FormatId != "Polygon Movie maker 0002")
            {
                throw new ArgumentException("Format id is not \"Polygon Movie maker 0002\"");
            }
            data.ViewWidth = ReadInt();
            data.ViewHeight = ReadInt();
            data.FrameWidth = ReadInt();
            data.EditViewAngle = ReadFloat();
            ReadByte(8);

            data.ModelCount = ReadByte();
            data.ModelDatas = ReadArray(data.ModelCount, ReadModelData);

            data.CameraInitFrame = ReadCameraFrame(true);
            data.CameraKeyFrames = ReadVArray(() => ReadCameraFrame(false));
            data.CameraKeyFrameCount = data.CameraKeyFrames.Length;
            data.CameraCurrentData = ReadCameraCurrentData();

            data.LightInitFrame = ReadLightFrame(true);
            data.LightKeyFrames = ReadVArray(() => ReadLightFrame(false));
            data.LightKeyFrameCount = data.LightKeyFrames.Length;

            // TODO

            return data;
        }

        private T[] ReadArray<T>(int size, Func<T> func)
        {
            var t = new T[size];
            for (var i = 0; i < size; i++)
            {
                t[i] = func();
            }
            return t;
        }

        private T[] ReadVArray<T>(Func<T> func)
        {
            var size = ReadInt();
            return ReadArray(size, func);
        }

        private PmmStuct.ModelData ReadModelData()
        {
            var o = new PmmStuct.ModelData();
            o.Number = ReadByte();
            o.Name = ReadVString();
            o.NameEn = ReadVString();
            o.Path = string.Concat(ReadFixedString(256).TakeWhile(s => s != '\0'));
            ReadByte(1);
            o.BoneName = ReadVArray(ReadVString);
            o.BoneCount = o.BoneName.Length;
            o.MorphName = ReadVArray(ReadVString);
            o.MorphCount = o.MorphName.Length;
            o.IkIndex = ReadVArray(ReadInt);
            o.IkCount = o.IkIndex.Length;
            o.OpIndex = ReadVArray(ReadInt);
            o.OpCount = o.OpIndex.Length;

            o.DrawOrder = ReadByte();
            o.EditIsDisplay = ReadBool();
            o.EditSelectedBone = ReadInt();
            o.SkinPanel = ReadArray(4, ReadInt);
            o.FrameCount = ReadByte();
            o.IsFrameOpen = ReadArray(o.FrameCount, ReadBool);
            o.VScroll = ReadInt();
            o.LastFrame = ReadInt();

            o.BoneInitFrames = ReadArray(o.BoneCount, () => ReadBoneFrame(true));
            o.BoneKeyFrames = ReadVArray(() => ReadBoneFrame(false));
            o.BoneKeyFrameCount = o.BoneKeyFrames.Length;

            o.MorphInitFrames = ReadArray(o.MorphCount, () => ReadMorphFrame(true));
            o.MorphKeyFrames = ReadVArray(() => ReadMorphFrame(false));
            o.MorphKeyFrameCount = o.MorphKeyFrames.Length;

            o.OpInitFrame = ReadOpFrame(o.IkCount, o.OpCount, true);

            o.OpKeyFrames = ReadVArray(() => ReadOpFrame(o.IkCount, o.OpCount, false));
            o.OpKeyFrameCount = o.OpKeyFrames.Length;

            o.BoneCurrentDatas = ReadArray(o.BoneCount, ReadBoneCurrentData);
            o.MorphCurrentDatas = ReadArray(o.MorphCount, ReadFloat);
            o.IsCurrentIkEnabledDatas = ReadArray(o.IkCount, ReadBool);
            o.OpCurrentDatas = ReadArray(o.OpCount, ReadOpCurrentData);
            o.IsAddBlend = ReadBool();
            o.EdgeWidth = ReadFloat();
            o.IsSelfShadowEnabled = ReadBool();
            o.CalcOrder = ReadByte();
            return o;
        }

        #region LightingInfo

        private PmmStuct.LightFrame ReadLightFrame(bool isInit)
        {
            var o = new PmmStuct.LightFrame
            {
                DataIndex = isInit ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt(),
                R = ReadFloat(),
                G = ReadFloat(),
                B = ReadFloat(),
                X = ReadFloat(),
                Y = ReadFloat(),
                Z = ReadFloat(),
                IsSelected = ReadBool()
            };
            return o;
        }

        #endregion LightingInfo

        #region MorphTypeRead

        private PmmStuct.ModelData.MorphFrame ReadMorphFrame(bool isInit)
        {
            var o = new PmmStuct.ModelData.MorphFrame
            {
                DataIndex = isInit ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt(),
                Value = ReadFloat(),
                IsSelected = ReadBool()
            };
            return o;
        }

        #endregion MorphTypeRead

        #region BoneTypeRead

        private PmmStuct.ModelData.BoneInitFrame ReadBoneFrame(bool isInitFrame)
        {
            var o = new PmmStuct.ModelData.BoneInitFrame
            {
                DataIndex = isInitFrame ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt(),
                InterpolationX = ReadArray(4, ReadByte),
                InterpolationY = ReadArray(4, ReadByte),
                InterpolationZ = ReadArray(4, ReadByte),
                InterpolationRotation = ReadArray(4, ReadByte),
                Translation = ReadArray(3, ReadFloat),
                Quaternion = ReadArray(4, ReadFloat),
                IsSelected = ReadBool(),
                IsPhysicsDisabled = ReadBool()
            };
            return o;
        }

        private PmmStuct.ModelData.BoneCurrentData ReadBoneCurrentData()
        {
            return new PmmStuct.ModelData.BoneCurrentData
            {
                Translation = ReadArray(3, ReadFloat),
                Quaternion = ReadArray(4, ReadFloat),
                IsEditUnCommited = ReadBool(),
                IsPhysicsDisabled = ReadBool(),
                IsRowSelected = ReadBool()
            };
        }

        #endregion BoneTypeRead

        #region CameraTypeRead

        private PmmStuct.CameraFrame ReadCameraFrame(bool isInit)
        {
            return new PmmStuct.CameraFrame
            {
                DataIndex = isInit ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt(),
                Distance = ReadInt(),
                EyePosition = ReadArray(3, ReadFloat),
                Rotation = ReadArray(3, ReadFloat),
                LookingModelIndex = ReadInt(),
                LookingBoneIndex = ReadInt(),
                InterpolationX = ReadArray(4, ReadByte),
                InterpolationY = ReadArray(4, ReadByte),
                InterpolationZ = ReadArray(4, ReadByte),
                InterpolationRotation = ReadArray(4, ReadByte),
                InterpolationDistance = ReadArray(4, ReadByte),
                InterpolationAngleView = ReadArray(4, ReadByte),
                IsParse = ReadBool(),
                AngleView = ReadInt(),
                IsSelected = ReadBool()
            };
        }

        private PmmStuct.CCameraCurrentData ReadCameraCurrentData()
        {
            var o = new PmmStuct.CCameraCurrentData
            {
                EyePosition = ReadArray(3, ReadFloat),
                TargetPosition = ReadArray(3, ReadFloat),
                Rotation = ReadArray(3, ReadFloat),
                IsOrthro = ReadBool()
            };
            return o;
        }

        #endregion CameraTypeRead

        #region OpTypeRead

        private PmmStuct.ModelData.OpFrame ReadOpFrame(int ikCount, int opCount, bool isInit)
        {
            return new PmmStuct.ModelData.OpFrame
            {
                DataIndex = isInit ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt(),
                IsDisplay = ReadBool(),
                IsIkEnabled = ReadArray(ikCount, ReadBool),
                OpData = ReadArray(opCount, ReadPair),
                IsSelected = ReadBool()
            };
        }

        private PmmStuct.ModelData.OpCurrentData ReadOpCurrentData()
        {
            return new PmmStuct.ModelData.OpCurrentData
            {
                KeyFrameBegin = ReadInt(),
                KeyFrameEnd = ReadInt(),
                ModelIndex = ReadInt(),
                ParentBoneIndex = ReadInt()
            };
        }

        #endregion OpTypeRead

        #region PrimitiveTypeRead

        private byte[] ReadByte(int size)
        {
            var len = _stream.Read(_buffer, 0, size);
            if (len != size)
            {
                throw new ArgumentException("");
            }
            return _buffer;
        }

        private byte ReadByte()
        {
            return ReadByte(1)[0];
        }

        private int ReadInt()
        {
            return BitConverter.ToInt32(ReadByte(4), 0);
        }

        private float ReadFloat()
        {
            return BitConverter.ToSingle(ReadByte(4), 0);
        }

        private bool ReadBool()
        {
            return ReadByte() != 0;
        }

        #endregion PrimitiveTypeRead

        #region StringTypeRead

        private string ReadVString()
        {
            return ReadFixedString(ReadByte());
        }

        private string ReadFixedString(int count)
        {
            return Encoding.GetEncoding("shift_jis").GetString(ReadByte(count), 0, count);
        }

        private KeyValuePair<int, int> ReadPair()
        {
            var a = ReadInt();
            var b = ReadInt();
            return new KeyValuePair<int, int>(a, b);
        }

        #endregion StringTypeRead
    }
}
