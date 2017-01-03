using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PMMEditor.Models
{
    public struct PmmStuct
    {
        public string FormatId;
        public int ViewWidth;
        public int ViewHeight;
        public int FrameWidth;
        public float EditViewAngle;
        // public byte[ /*7*/] Unknown10 { get; }

        #region ModelInfo

        public byte ModelCount;

        public struct ModelData
        {
            public byte Number;
            public string Name;
            public string NameEn;
            public string Path;

            // public byte Unknown11 { get; }
            public int BoneCount;

            public string[ /*BoneCount*/] BoneName;
            public int MorphCount;
            public string[ /*MorphCount*/] MorphName;
            public int IkCount;
            public int[ /*IkCount*/] IkIndex;
            public int OpCount;
            public int[ /*OpCount*/] OpIndex;
            public byte DrawOrder;
            public bool EditIsDisplay;
            public int EditSelectedBone;
            public int[ /*4*/] SkinPanel;
            public byte FrameCount;
            public bool[ /*FrameCount*/] IsFrameOpen;
            public int VScroll;
            public int LastFrame;

            #region BoneInfo

            public struct BoneInitFrame
            {
                public int DataIndex; // 初期フレームのときは-1

                public int FrameNumber;

                public int PreIndex;

                public int NextIndex;

                public byte[ /* 4 */] InterpolationX;

                public byte[ /* 4 */] InterpolationY;

                public byte[ /* 4 */] InterpolationZ;

                public byte[ /* 4 */] InterpolationRotation;

                public float[ /* 3 */] Translation;

                public float[ /* 4 */] Quaternion;

                public bool IsSelected;

                public bool IsPhysicsDisabled;
            }

            public BoneInitFrame[ /*BoneCount*/] BoneInitFrames;

            public int BoneKeyFrameCount;

            public BoneInitFrame[ /* BoneKeyFrameCount */] BoneKeyFrames;

            #endregion BoneInfo

            #region MorphInfo

            public struct MorphFrame
            {
                public int DataIndex; // 初期フレームのときは-1
                public int FrameNumber;
                public int PreIndex;
                public int NextIndex;
                public float Value;
                public bool IsSelected;
            }

            public MorphFrame[ /* MorphCount */] MorphInitFrames;

            public int MorphKeyFrameCount;

            public MorphFrame[ /* MorphCount */] MorphKeyFrames;

            #endregion MorphInfo

            #region その他構成情報

            public struct OpFrame
            {
                public int DataIndex;
                public int FrameNumber;
                public int PreIndex;
                public int NextIndex;
                public bool IsDisplay;
                public bool[ /* IkCount */] IsIkEnabled;
                public KeyValuePair<int, int>[ /* OpCount */] OpData;
                public bool IsSelected;
            }

            public OpFrame OpInitFrame;

            public int OpKeyFrameCount;

            public OpFrame[ /* OpKeyFrameCount */] OpKeyFrames;

            #endregion その他構成情報

            #region CurrentInfo

            public struct BoneCurrentData
            {
                public float[] Translation;
                public float[] Quaternion;
                public bool IsEditUnCommited;
                public bool IsPhysicsDisabled;
                public bool IsRowSelected;
            }

            public BoneCurrentData[ /* BoneCount */] BoneCurrentDatas;

            public float[ /* MorphCount */] MorphCurrentDatas;

            public bool[ /* IkCount */] IsCurrentIkEnabledDatas;

            public struct OpCurrentData
            {
                public int KeyFrameBegin;
                public int KeyFrameEnd;
                public int ModelIndex;
                public int ParentBoneIndex;
            }

            public OpCurrentData[ /* OpCount */] OpCurrentDatas;

            #endregion CurrentInfo

            public bool IsAddBlend; // 加算合成

            public float EdgeWidth;

            public bool IsSelfShadowEnabled;

            public byte CalcOrder;
        }

        public ModelData[ /*ModelCount*/] ModelDatas;

        #endregion ModelInfo

        #region CameraInfo

        public struct CameraFrame
        {
            public int DataIndex;
            public int FrameNumber;
            public int PreIndex;
            public int NextIndex;
            public float Distance;
            public float[] EyePosition;
            public float[] Rotation;
            public int LookingModelIndex; // 非選択時-1
            public int LookingBoneIndex; // 非選択時-1
            public byte[] InterpolationX;
            public byte[] InterpolationY;
            public byte[] InterpolationZ;
            public byte[] InterpolationRotation;
            public byte[] InterpolationDistance;
            public byte[] InterpolationAngleView;
            public bool IsParse;
            public int AngleView;
            public bool IsSelected;
        }


        public CameraFrame CameraInitFrame;

        public int CameraKeyFrameCount;

        public CameraFrame[] CameraKeyFrames;

        public struct CCameraCurrentData
        {
            public float[] EyePosition;
            public float[] TargetPosition;
            public float[] Rotation;
            public bool IsOrthro;
        }

        public CCameraCurrentData CameraCurrentData;

        #endregion CameraInfo

        #region LightingInfo

        public struct LightFrame
        {
            public int DataIndex;
            public int FrameNumber;
            public int PreIndex;
            public int NextIndex;
            public float R, G, B;
            public float X, Y, Z;
            public bool IsSelected;
        }

        public LightFrame LightInitFrame;

        public int LightKeyFrameCount;

        public LightFrame[] LightKeyFrames;

        public struct CLightCurrentData
        {
            public float R, G, B;
            public float X, Y, Z;
            public bool IsSelected;
        }

        public CLightCurrentData LightCurrentData;

        #endregion LightingInfo

        #region AccessoryInfo

        public byte SelectAccessoryIndex;
        public int AccessoryVScroll;
        public byte AccessoryCount;

        public string[] AccessoryName;

        public struct AccessoryData
        {
            public byte Index;
            public string Name;
            public string Path;
            public byte DrawOrder;

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
            Read(out data.ViewWidth);
            Read(out data.ViewHeight);
            Read(out data.FrameWidth);
            Read(out data.EditViewAngle);
            ReadByte(8);

            Read(out data.ModelCount);
            ReadArray(out data.ModelDatas, data.ModelCount, Read);

            ReadInit(out data.CameraInitFrame);
            data.CameraKeyFrameCount = ReadVArray(out data.CameraKeyFrames, ReadNormal);
            Read(out data.CameraCurrentData);

            ReadInit(out data.LightInitFrame);
            data.LightKeyFrameCount = ReadVArray(out data.LightKeyFrames, ReadNormal);

            // TODO

            return data;
        }

        private int ReadArray<T>(out T[] t, int size, ReadVArrayDelegate<T> func)
        {
            t = new T[size];
            for (var i = 0; i < size; i++)
            {
                func(out t[i]);
            }
            return size;
        }

        private int ReadVArray<T>(out T[] t, ReadVArrayDelegate<T> func)
        {
            int size;
            Read(out size);
            return ReadArray(out t, size, func);
        }

        private void Read(out PmmStuct.ModelData o)
        {
            o = new PmmStuct.ModelData();
            Read(out o.Number);
            ReadVString(out o.Name);
            ReadVString(out o.NameEn);
            o.Path = ReadFixedString(256);
            ReadByte(1);
            o.BoneCount = ReadVArray(out o.BoneName, ReadVString);
            o.MorphCount = ReadVArray(out o.MorphName, ReadVString);
            o.IkCount = ReadVArray(out o.IkIndex, Read);
            o.OpCount = ReadVArray(out o.OpIndex, Read);

            Read(out o.DrawOrder);
            Read(out o.EditIsDisplay);
            Read(out o.EditSelectedBone);
            ReadArray(out o.SkinPanel, 4, Read);
            Read(out o.FrameCount);
            ReadArray(out o.IsFrameOpen, o.FrameCount, Read);
            Read(out o.VScroll);
            Read(out o.LastFrame);

            ReadArray(out o.BoneInitFrames, o.BoneCount, ReadInit);
            o.BoneKeyFrameCount = ReadVArray(out o.BoneKeyFrames, ReadNormal);

            ReadArray(out o.MorphInitFrames, o.MorphCount, ReadInit);
            o.MorphKeyFrameCount = ReadVArray(out o.MorphKeyFrames, ReadNormal);

            Read(out o.OpInitFrame, o.IkCount, o.OpCount, true);

            Read(out o.OpKeyFrameCount);
            o.OpKeyFrames = new PmmStuct.ModelData.OpFrame[o.OpKeyFrameCount];
            foreach (var i in Enumerable.Range(0, o.OpKeyFrameCount))
            {
                Read(out o.OpKeyFrames[i], o.IkCount, o.OpCount, false);
            }

            ReadArray(out o.BoneCurrentDatas, o.BoneCount, Read);
            ReadArray(out o.MorphCurrentDatas, o.MorphCount, Read);
            ReadArray(out o.IsCurrentIkEnabledDatas, o.IkCount, Read);
            ReadArray(out o.OpCurrentDatas, o.OpCount, Read);
            Read(out o.IsAddBlend);
            Read(out o.EdgeWidth);
            Read(out o.IsSelfShadowEnabled);
            Read(out o.CalcOrder);
        }

        private delegate void ReadVArrayDelegate<T>(out T t);

        #region CurrentTypeRead

        private void Read(out PmmStuct.CCameraCurrentData o)
        {
            o = new PmmStuct.CCameraCurrentData();
            ReadArray(out o.EyePosition, 3, Read);
            ReadArray(out o.TargetPosition, 3, Read);
            ReadArray(out o.Rotation, 3, Read);
            Read(out o.IsOrthro);
        }

        private void Read(out PmmStuct.ModelData.BoneCurrentData o)
        {
            o = new PmmStuct.ModelData.BoneCurrentData();
            ReadArray(out o.Translation, 3, Read);
            ReadArray(out o.Quaternion, 4, Read);
            Read(out o.IsEditUnCommited);
            Read(out o.IsPhysicsDisabled);
            Read(out o.IsRowSelected);
        }

        private void Read(out PmmStuct.ModelData.OpCurrentData o)
        {
            o = new PmmStuct.ModelData.OpCurrentData();
            Read(out o.KeyFrameBegin);
            Read(out o.KeyFrameEnd);
            Read(out o.ModelIndex);
            Read(out o.ParentBoneIndex);
        }

        #endregion CurrentTypeRead

        #region LightingInfo

        private void Read(out PmmStuct.LightFrame o, bool isInit)
        {
            o = new PmmStuct.LightFrame();
            if (isInit)
            {
                o.DataIndex = -1;
            }
            else
            {
                Read(out o.DataIndex);
            }
            Read(out o.FrameNumber);
            Read(out o.PreIndex);
            Read(out o.NextIndex);
            Read(out o.R);
            Read(out o.G);
            Read(out o.B);
            Read(out o.X);
            Read(out o.Y);
            Read(out o.Z);
            Read(out o.IsSelected);
        }

        private void ReadInit(out PmmStuct.LightFrame o)
        {
            Read(out o, true);
        }

        private void ReadNormal(out PmmStuct.LightFrame o)
        {
            Read(out o, false);
        }

        #endregion LightingInfo

        #region CameraTypeRead

        private void Read(out PmmStuct.CameraFrame o, bool isInit)
        {
            o = new PmmStuct.CameraFrame();
            if (isInit)
            {
                o.DataIndex = -1;
            }
            else
            {
                Read(out o.DataIndex);
            }
            Read(out o.FrameNumber);
            Read(out o.PreIndex);
            Read(out o.NextIndex);
            Read(out o.Distance);
            ReadArray(out o.EyePosition, 3, Read);
            ReadArray(out o.Rotation, 3, Read);
            Read(out o.LookingModelIndex);
            Read(out o.LookingBoneIndex);
            ReadArray(out o.InterpolationX, 4, Read);
            ReadArray(out o.InterpolationY, 4, Read);
            ReadArray(out o.InterpolationZ, 4, Read);
            ReadArray(out o.InterpolationRotation, 4, Read);
            ReadArray(out o.InterpolationDistance, 4, Read);
            ReadArray(out o.InterpolationAngleView, 4, Read);
            Read(out o.IsParse);
            Read(out o.AngleView);
            Read(out o.IsSelected);
        }

        private void ReadInit(out PmmStuct.CameraFrame o)
        {
            Read(out o, true);
        }

        private void ReadNormal(out PmmStuct.CameraFrame o)
        {
            Read(out o, false);
        }

        #endregion CameraTypeRead

        #region OpTypeRead

        private void Read(out PmmStuct.ModelData.OpFrame o, int ikCount, int opCount, bool isInit)
        {
            o = new PmmStuct.ModelData.OpFrame();
            if (isInit)
            {
                o.DataIndex = -1;
            }
            else
            {
                Read(out o.DataIndex);
            }
            Read(out o.FrameNumber);
            Read(out o.PreIndex);
            Read(out o.NextIndex);
            Read(out o.IsDisplay);
            ReadArray(out o.IsIkEnabled, ikCount, Read);
            ReadArray(out o.OpData, opCount, Read);
            Read(out o.IsSelected);
        }

        private void Read(out KeyValuePair<int, int> o)
        {
            int a, b;
            Read(out a);
            Read(out b);
            o = new KeyValuePair<int, int>(a, b);
        }

        #endregion OpTypeRead

        #region MorphTypeRead

        private void Read(out PmmStuct.ModelData.MorphFrame o, bool isInit)
        {
            o = new PmmStuct.ModelData.MorphFrame();
            if (isInit)
            {
                o.DataIndex = -1;
            }
            else
            {
                Read(out o.DataIndex);
            }
            Read(out o.FrameNumber);
            Read(out o.PreIndex);
            Read(out o.NextIndex);
            Read(out o.Value);
            Read(out o.IsSelected);
        }

        private void ReadInit(out PmmStuct.ModelData.MorphFrame o)
        {
            Read(out o, true);
        }

        private void ReadNormal(out PmmStuct.ModelData.MorphFrame o)
        {
            Read(out o, false);
        }

        #endregion MorphTypeRead

        #region BoneTypeRead

        private void Read(out PmmStuct.ModelData.BoneInitFrame o, bool isInitFrame)
        {
            o = new PmmStuct.ModelData.BoneInitFrame();
            if (isInitFrame)
            {
                o.DataIndex = -1;
            }
            else
            {
                Read(out o.DataIndex);
            }
            Read(out o.FrameNumber);
            Read(out o.PreIndex);
            Read(out o.NextIndex);
            ReadArray(out o.InterpolationX, 4, Read);
            ReadArray(out o.InterpolationY, 4, Read);
            ReadArray(out o.InterpolationZ, 4, Read);
            ReadArray(out o.InterpolationRotation, 4, Read);
            ReadArray(out o.Translation, 3, Read);
            ReadArray(out o.Quaternion, 4, Read);
            Read(out o.IsSelected);
            Read(out o.IsPhysicsDisabled);
        }

        private void ReadInit(out PmmStuct.ModelData.BoneInitFrame o)
        {
            Read(out o, true);
        }

        private void ReadNormal(out PmmStuct.ModelData.BoneInitFrame o)
        {
            Read(out o, false);
        }

        #endregion BoneTypeRead

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

        private int ReadInt()
        {
            return BitConverter.ToInt32(ReadByte(4), 0);
        }

        private void Read(out int o)
        {
            o = ReadInt();
        }

        private float ReadFloat()
        {
            return BitConverter.ToSingle(ReadByte(4), 0);
        }

        private void Read(out float o)
        {
            o = ReadFloat();
        }

        private void Read(out byte o)
        {
            o = ReadByte(1)[0];
        }

        private void Read(out bool o)
        {
            byte tmp;
            Read(out tmp);
            o = tmp != 0;
        }

        #endregion PrimitiveTypeRead

        #region StringTypeRead

        private void ReadVString(out string o)
        {
            byte size;
            Read(out size);
            o = ReadFixedString(size);
        }

        private string ReadFixedString(int count)
        {
            return Encoding.GetEncoding("shift_jis").GetString(ReadByte(count), 0, count);
        }

        #endregion StringTypeRead
    }
}
