using System;
using System.Collections.Generic;
using System.IO;

namespace PMMEditor.MMDFileParser
{
    public class PmmStruct
    {
        public interface IKeyFrame
        {
            int DataIndex { get; set; }

            int FrameNumber { get; set; }

            int PreIndex { get; set; }

            int NextIndex { get; set; }
        }

        public string FormatId { get; set; }

        public int ViewWidth { get; set; }

        public int ViewHeight { get; set; }

        public int FrameWidth { get; set; }

        public float EditViewAngle { get; set; }

        public bool IsEditCameraLightAccessory { get; set; }

        public bool IsOpenCameraPanel { get; set; }

        public bool IsOpenLightPanel { get; set; }

        public bool IsOpenAccessoryPanel { get; set; }

        public bool IsOpenBonePanel { get; set; }

        public bool IsOpenMorphPanel { get; set; }

        public bool IsOpenSelfShadowPanel { get; set; }

        public byte SelectedModelIndex { get; set; }

        #region ModelInfo

        public class ModelData
        {
            public byte Number { get; set; }

            public string Name { get; set; }

            public string NameEn { get; set; }

            public string Path { get; set; }

            public byte KeyFrameEditorTopLevelRows { get; set; }

            public List<string> BoneName { get; set; }

            public List<string> MorphName { get; set; }

            public List<int> IkIndex { get; set; }

            public List<int> OpIndex { get; set; }

            public byte DrawOrder { get; set; }

            public bool EditIsDisplay { get; set; }

            public int EditSelectedBone { get; set; }

            public int[ /*4*/] SkinPanel { get; set; }

            public List<bool> IsFrameOpen { get; set; }

            public int VScroll { get; set; }

            public int LastFrame { get; set; }

            #region BoneInfo

            public class BoneInitFrame : IKeyFrame
            {
                public int DataIndex { get; set; } // 初期フレームのときは-1

                public int FrameNumber { get; set; }

                public int PreIndex { get; set; }

                public int NextIndex { get; set; }

                public sbyte[ /* 4 */] InterpolationX { get; set; }

                public sbyte[ /* 4 */] InterpolationY { get; set; }

                public sbyte[ /* 4 */] InterpolationZ { get; set; }

                public sbyte[ /* 4 */] InterpolationRotation { get; set; }

                public float[ /* 3 */] Translation { get; set; }

                public float[ /* 4 */] Quaternion { get; set; }

                public bool IsSelected { get; set; }

                public bool IsPhysicsDisabled { get; set; }
            }

            public List<BoneInitFrame> BoneInitFrames { get; set; }

            public List<BoneInitFrame> BoneKeyFrames { get; set; }

            #endregion BoneInfo

            #region MorphInfo

            public class MorphFrame : IKeyFrame
            {
                public int DataIndex { get; set; } // 初期フレームのときは-1

                public int FrameNumber { get; set; }

                public int PreIndex { get; set; }

                public int NextIndex { get; set; }

                public float Value { get; set; }

                public bool IsSelected { get; set; }
            }

            public List<MorphFrame> MorphInitFrames { get; set; }

            public List<MorphFrame> MorphKeyFrames { get; set; }

            #endregion MorphInfo

            #region その他構成情報

            public class OpFrame
            {
                public int DataIndex { get; set; }

                public int FrameNumber { get; set; }

                public int PreIndex { get; set; }

                public int NextIndex { get; set; }

                public bool IsDisplay { get; set; }

                public List<bool> IsIkEnabled { get; set; }

                public List<KeyValuePair<int, int>> OpData { get; set; }

                public bool IsSelected { get; set; }
            }

            public OpFrame OpInitFrame { get; set; }

            public List<OpFrame> OpKeyFrames { get; set; }

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

            public List<BoneCurrentData> BoneCurrentDatas { get; set; }

            public List<float> MorphCurrentDatas { get; set; }

            public List<bool> IsCurrentIkEnabledDatas { get; set; }

            public class OpCurrentData
            {
                public int KeyFrameBegin { get; set; }

                public int KeyFrameEnd { get; set; }

                public int ModelIndex { get; set; }

                public int ParentBoneIndex { get; set; }
            }

            public List<OpCurrentData> OpCurrentDatas { get; set; }

            #endregion CurrentInfo

            #region PostInfo

            public bool IsAddBlend { get; set; } // 加算合成

            public float EdgeWidth { get; set; }

            public bool IsSelfShadowEnabled { get; set; }

            public byte CalcOrder { get; set; }

            #endregion PostInfo
        }

        public List<ModelData> ModelDatas { get; set; }

        #endregion ModelInfo

        #region CameraInfo

        public class CameraFrame : IKeyFrame
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

            public sbyte[] InterpolationX { get; set; }

            public sbyte[] InterpolationY { get; set; }

            public sbyte[] InterpolationZ { get; set; }

            public sbyte[] InterpolationRotation { get; set; }

            public sbyte[] InterpolationDistance { get; set; }

            public sbyte[] InterpolationAngleView { get; set; }

            public bool IsParse { get; set; }

            public int AngleView { get; set; }

            public bool IsSelected { get; set; }
        }


        public CameraFrame CameraInitFrame { get; set; }

        public List<CameraFrame> CameraKeyFrames { get; set; }

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

        public class LightFrame : IKeyFrame
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

        public List<LightFrame> LightKeyFrames { get; set; }

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

        public byte SelectedAccessoryIndex { get; set; }

        public int AccessoryVScroll { get; set; }

        public List<string> AccessoryName { get; set; }

        public class AccessoryData
        {
            public byte Index { get; set; }

            public string Name { get; set; }

            public string Path { get; set; }

            public byte DrawOrder { get; set; }

            public class DataBody
            {
                public byte Transparency { get; set; }

                public bool IsVisible { get; set; }

                public int ParentModelIndex { get; set; }

                public int ParentBoneIndex { get; set; }

                public float[] Translation { get; set; }

                public float[] Rotation { get; set; }

                public float Scale { get; set; }

                public bool IsShadowEnabled { get; set; }
            }

            public class KeyFrame : DataBody, IKeyFrame
            {
                public int DataIndex { get; set; }

                public int FrameNumber { get; set; }

                public int PreIndex { get; set; }

                public int NextIndex { get; set; }

                public bool IsSelected { get; set; }
            }

            public KeyFrame InitFrame { get; set; }

            public List<KeyFrame> KeyFrames { get; set; }

            public DataBody CurrentData { get; set; }

            public bool IsAddBlend { get; set; }
        }

        public List<AccessoryData> AccessoryDatas { get; set; }

        #endregion AccessoryInfo

        #region AnotherInfo

        public int CurrentFramePosition { get; set; }

        public int HScrollPosition { get; set; }

        public int HScrollScale { get; set; }

        public int BoneOperationKind { get; set; } // 0:選択 1:box選択 2:何も選択していない 3:回転 4:移動

        public byte LookingAt { get; set; } // 0:なし 1:モデル 2:ボーン

        public bool IsRepeat { get; set; }

        public bool IsPlayFromFrame { get; set; }

        public bool IsPlayToFrame { get; set; }

        public int PlayStartFrame { get; set; }

        public int PlayEndFrame { get; set; }

        public bool IsWaveEnabled { get; set; }

        public string WavePath { get; set; }

        public int AviOffsetX { get; set; }

        public int AviOffsetY { get; set; }

        public float AviScale { get; set; }

        public string AviPath { get; set; }

        public bool IsShowAvi { get; set; } // バイナリ上では4バイト(01 00 00 00)で表示 それ以外は非表示

        public int BackgroundImageOffsetX { get; set; }

        public int BackgroundImageOffsetY { get; set; }

        public float BackgroundImageScale { get; set; }

        public string BackgroundImagePath { get; set; }

        public bool IsShowBackgroundImage { get; set; }

        public bool IsShowInfomation { get; set; }

        public bool IsShowAxis { get; set; }

        public bool IsShowGroundShadow { get; set; }

        public float FpsLimit { get; set; }

        public int ScreenCaptureMode { get; set; }

        public int AccessoryNumberRenderAfterModel { get; set; }

        public float GroundShadowBrightness { get; set; }

        public bool IsTransparentGroundShadow { get; set; }

        public byte PhysicsMode { get; set; } // 1:常に 2:オンオフモード 3:トレース 0:演算しない

        #endregion AnotherInfo

        #region GravityInfo

        public class CGravityCurrentData
        {
            public float Acceleration { get; set; }

            public int NoizeAmount { get; set; }

            public float DirectionX { get; set; }

            public float DirectionY { get; set; }

            public float DirectionZ { get; set; }

            public bool IsAddNoize { get; set; }
        }

        public CGravityCurrentData GravityCurrentData { get; set; }

        public class GravityKeyFrame : IKeyFrame
        {
            public int DataIndex { get; set; }

            public int FrameNumber { get; set; }

            public int PreIndex { get; set; }

            public int NextIndex { get; set; }

            public bool IsAddNoize { get; set; }

            public int NoizeAmount { get; set; }

            public float Acceleration { get; set; }

            public float DirectionX { get; set; }

            public float DirectionY { get; set; }

            public float DirectionZ { get; set; }

            public bool IsSelected { get; set; }
        }

        public GravityKeyFrame GravityInitFrame { get; set; }

        public List<GravityKeyFrame> GravityKeyFrames { get; set; }

        #endregion GravityInfo

        #region SelfShadowInfo

        public bool IsShowSelfShadow { get; set; }

        public float SelfShadowCurrentData { get; set; }

        public class SelfShadowFrame : IKeyFrame
        {
            public int DataIndex { get; set; }

            public int FrameNumber { get; set; }

            public int PreIndex { get; set; }

            public int NextIndex { get; set; }

            public byte Mode { get; set; }

            public float Distance { get; set; }

            public bool IsSelected { get; set; }
        }

        public SelfShadowFrame SelfShadowInitFrame { get; set; }

        public List<SelfShadowFrame> SelfShadowKeyFrames { get; set; }

        #endregion SelfShadowInfo

        #region AnotherInfo

        public float EdgeColorR { get; set; }

        public float EdgeColorG { get; set; }

        public float EdgeColorB { get; set; }


        public bool IsBlackBackground { get; set; }

        public int CameraCurrentLookingAtModel { get; set; }

        public int CameraCurrentLookingAtBone { get; set; }

        #endregion AnotherInfo

        #region AnotherInfo

        public float[] Unknown1_12 { get; set; }

        public bool IsViewLookAtEnabled { get; set; }

        public byte Unknown2 { get; set; }

        public bool IsPhysicsGroundEnabled { get; set; }

        public int FrameTextbox { get; set; }

        #endregion AnotherInfo

        #region SelectorChoiceInfo

        public byte SelectorChoiceSelectionFollowing { get; set; }

        public class CSelectorChoiceData
        {
            public byte ModelIndex { get; set; }

            public int SelectorChoice { get; set; }
        }

        public List<CSelectorChoiceData> SelectorChoiceDatas { get; set; }

        #endregion SelectorChoiceInfo
    }

    internal class PmmReader : MMDFileReaderBase
    {
        private readonly byte[] _binaryData;

        public PmmReader(byte[] binaryData)
        {
            _binaryData = binaryData;
            _buffer = new byte[256];
        }

        public PmmStruct Read()
        {
            var data = new PmmStruct();
            _stream = new MemoryStream(_binaryData);
            data.FormatId = ReadFixedStringTerminationChar(30);
            if (data.FormatId != "Polygon Movie maker 0002")
            {
                throw new ArgumentException("Format id is not \"Polygon Movie maker 0002\"");
            }
            data.ViewWidth = ReadInt();
            data.ViewHeight = ReadInt();
            data.FrameWidth = ReadInt();
            data.EditViewAngle = ReadFloat();
            data.IsEditCameraLightAccessory = ReadBool();
            data.IsOpenCameraPanel = ReadBool();
            data.IsOpenLightPanel = ReadBool();
            data.IsOpenAccessoryPanel = ReadBool();
            data.IsOpenBonePanel = ReadBool();
            data.IsOpenMorphPanel = ReadBool();
            data.IsOpenSelfShadowPanel = ReadBool();
            data.SelectedModelIndex = ReadByte();

            var modelCount = ReadByte();
            data.ModelDatas = ReadList(modelCount, ReadModelData);

            data.CameraInitFrame = ReadCameraFrame(true);
            data.CameraKeyFrames = ReadVList(() => ReadCameraFrame(false));
            data.CameraCurrentData = ReadCameraCurrentData();

            data.LightInitFrame = ReadLightFrame(true);
            data.LightKeyFrames = ReadVList(() => ReadLightFrame(false));
            data.LightCurrentData = ReadLightCurrentData();

            data.SelectedAccessoryIndex = ReadByte();
            data.AccessoryVScroll = ReadInt();
            var accessoryCount = ReadByte();
            data.AccessoryName = ReadList(accessoryCount, () => ReadFixedStringTerminationChar(100));
            data.AccessoryDatas = ReadList(accessoryCount, ReadAccessory);

            data.CurrentFramePosition = ReadInt();
            data.HScrollPosition = ReadInt();
            data.HScrollScale = ReadInt();
            data.BoneOperationKind = ReadInt();
            data.LookingAt = ReadByte();
            data.IsRepeat = ReadBool();
            data.IsPlayFromFrame = ReadBool();
            data.IsPlayToFrame = ReadBool();
            data.PlayStartFrame = ReadInt();
            data.PlayEndFrame = ReadInt();

            data.IsWaveEnabled = ReadBool();
            data.WavePath = ReadFixedStringTerminationChar(256);

            data.AviOffsetX = ReadInt();
            data.AviOffsetY = ReadInt();
            data.AviScale = ReadFloat();
            data.AviPath = ReadFixedStringTerminationChar(256);
            data.IsShowAvi = ReadInt() == 1;

            data.BackgroundImageOffsetX = ReadInt();
            data.BackgroundImageOffsetY = ReadInt();
            data.BackgroundImageScale = ReadInt();
            data.BackgroundImagePath = ReadFixedStringTerminationChar(256);
            data.IsShowBackgroundImage = ReadBool();

            data.IsShowInfomation = ReadBool();
            data.IsShowAxis = ReadBool();
            data.IsShowGroundShadow = ReadBool();
            data.FpsLimit = ReadFloat();
            data.ScreenCaptureMode = ReadInt();
            data.AccessoryNumberRenderAfterModel = ReadInt();
            data.GroundShadowBrightness = ReadFloat();
            data.IsTransparentGroundShadow = ReadBool();
            data.PhysicsMode = ReadByte();

            data.GravityCurrentData = ReadGravityCurrentData();
            data.GravityInitFrame = ReadGravityKeyFrame(true);
            data.GravityKeyFrames = ReadVList(() => ReadGravityKeyFrame(false));

            data.IsShowSelfShadow = ReadBool();
            data.SelfShadowCurrentData = ReadFloat();
            data.SelfShadowInitFrame = ReadSelfShadowKeyFrame(true);
            data.SelfShadowKeyFrames = ReadVList(() => ReadSelfShadowKeyFrame(false));

            data.EdgeColorR = ReadInt();
            data.EdgeColorG = ReadInt();
            data.EdgeColorB = ReadInt();

            data.IsBlackBackground = ReadBool();

            data.CameraCurrentLookingAtModel = ReadInt();
            data.CameraCurrentLookingAtBone = ReadInt();

            ReadList(4 * 4, ReadFloat);

            data.IsViewLookAtEnabled = ReadBool();

            ReadByte();

            data.IsPhysicsGroundEnabled = ReadBool();
            data.FrameTextbox = ReadInt();

            data.SelectorChoiceSelectionFollowing = ReadByte();
            data.SelectorChoiceDatas = ReadList(data.ModelDatas.Count, ReadSelectorChoice);

            if (IsRemaining())
            {
                throw new Exception("Tail Error.");
            }

            return data;
        }

        #region SelectorChoiceTypeRead

        private PmmStruct.CSelectorChoiceData ReadSelectorChoice()
        {
            return new PmmStruct.CSelectorChoiceData
            {
                ModelIndex = ReadByte(),
                SelectorChoice = ReadInt()
            };
        }

        #endregion SelectorChoiceTypeRead

        #region SelfShadowTypeRead

        private PmmStruct.SelfShadowFrame ReadSelfShadowKeyFrame(bool isInit)
        {
            return new PmmStruct.SelfShadowFrame
            {
                DataIndex = isInit ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt(),
                Mode = ReadByte(),
                Distance = ReadFloat(),
                IsSelected = ReadBool()
            };
        }

        #endregion SelfShadowTypeRead

        #region GravityTypeRead

        private PmmStruct.GravityKeyFrame ReadGravityKeyFrame(bool isInit)
        {
            return new PmmStruct.GravityKeyFrame
            {
                DataIndex = isInit ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt(),
                IsAddNoize = ReadBool(),
                NoizeAmount = ReadInt(),
                Acceleration = ReadFloat(),
                DirectionX = ReadFloat(),
                DirectionY = ReadFloat(),
                DirectionZ = ReadFloat(),
                IsSelected = ReadBool()
            };
        }

        private PmmStruct.CGravityCurrentData ReadGravityCurrentData()
        {
            return new PmmStruct.CGravityCurrentData
            {
                Acceleration = ReadFloat(),
                NoizeAmount = ReadInt(),
                DirectionX = ReadFloat(),
                DirectionY = ReadFloat(),
                DirectionZ = ReadFloat(),
                IsAddNoize = ReadBool()
            };
        }

        #endregion GravityTypeRead

        #region ModelTypeRead

        private PmmStruct.ModelData ReadModelData()
        {
            var o = new PmmStruct.ModelData
            {
                Number = ReadByte(),
                Name = ReadVString(),
                NameEn = ReadVString(),
                Path = ReadFixedStringTerminationChar(256),
                KeyFrameEditorTopLevelRows = ReadByte(),
                BoneName = ReadVList(ReadVString),
                MorphName = ReadVList(ReadVString),
                IkIndex = ReadVList(ReadInt),
                OpIndex = ReadVList(ReadInt),

                DrawOrder = ReadByte(),
                EditIsDisplay = ReadBool(),
                EditSelectedBone = ReadInt(),
                SkinPanel = ReadArray(4, ReadInt),
                IsFrameOpen = ReadList(ReadByte(), ReadBool),
                VScroll = ReadInt(),
                LastFrame = ReadInt()
            };
            o.BoneInitFrames = ReadList(o.BoneName.Count, () => ReadBoneFrame(true));
            o.BoneKeyFrames = ReadVList(() => ReadBoneFrame(false));

            o.MorphInitFrames = ReadList(o.MorphName.Count, () => ReadMorphFrame(true));
            o.MorphKeyFrames = ReadVList(() => ReadMorphFrame(false));

            o.OpInitFrame = ReadOpFrame(o.IkIndex.Count, o.OpIndex.Count, true);

            o.OpKeyFrames = ReadVList(() => ReadOpFrame(o.IkIndex.Count, o.OpIndex.Count, false));

            o.BoneCurrentDatas = ReadList(o.BoneName.Count, ReadBoneCurrentData);
            o.MorphCurrentDatas = ReadList(o.MorphName.Count, ReadFloat);
            o.IsCurrentIkEnabledDatas = ReadList(o.IkIndex.Count, ReadBool);
            o.OpCurrentDatas = ReadList(o.OpIndex.Count, ReadOpCurrentData);
            o.IsAddBlend = ReadBool();
            o.EdgeWidth = ReadFloat();
            o.IsSelfShadowEnabled = ReadBool();
            o.CalcOrder = ReadByte();
            return o;
        }

        #endregion ModelTypeRead

        #region AccessoryTypeRead

        private void ReadAccessoryDataBody<T>(ref T o) where T : PmmStruct.AccessoryData.DataBody
        {
            var tmp = ReadByte();
            o.Transparency = (byte) ((tmp & 0xfe) >> 1);
            o.IsVisible = (tmp & 0x01) != 0;
            o.ParentModelIndex = ReadInt();
            o.ParentBoneIndex = ReadInt();
            o.Translation = ReadArray(3, ReadFloat);
            o.Rotation = ReadArray(3, ReadFloat);
            o.Scale = ReadFloat();
            o.IsShadowEnabled = ReadBool();
        }

        private PmmStruct.AccessoryData.KeyFrame ReadAccessoryKeyFrame(bool isInit)
        {
            var o = new PmmStruct.AccessoryData.KeyFrame
            {
                DataIndex = isInit ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt()
            };
            ReadAccessoryDataBody(ref o);
            o.IsSelected = ReadBool();
            return o;
        }

        private PmmStruct.AccessoryData ReadAccessory()
        {
            var o = new PmmStruct.AccessoryData
            {
                Index = ReadByte(),
                Name = ReadFixedStringTerminationChar(100),
                Path = ReadFixedStringTerminationChar(256),
                DrawOrder = ReadByte(),
                InitFrame = ReadAccessoryKeyFrame(true),
                KeyFrames = ReadVList(() => ReadAccessoryKeyFrame(false))
            };
            var tmp = new PmmStruct.AccessoryData.DataBody();
            ReadAccessoryDataBody(ref tmp);
            o.CurrentData = tmp;
            o.IsAddBlend = ReadBool();
            return o;
        }

        #endregion AccessoryTypeRead

        #region LightingTypeRead

        private PmmStruct.CLightCurrentData ReadLightCurrentData()
        {
            return new PmmStruct.CLightCurrentData
            {
                R = ReadFloat(),
                G = ReadFloat(),
                B = ReadFloat(),
                X = ReadFloat(),
                Y = ReadFloat(),
                Z = ReadFloat()
            };
        }

        private PmmStruct.LightFrame ReadLightFrame(bool isInit)
        {
            return new PmmStruct.LightFrame
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
        }

        #endregion LightingTypeRead

        #region MorphTypeRead

        private PmmStruct.ModelData.MorphFrame ReadMorphFrame(bool isInit)
        {
            return new PmmStruct.ModelData.MorphFrame
            {
                DataIndex = isInit ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt(),
                Value = ReadFloat(),
                IsSelected = ReadBool()
            };
        }

        #endregion MorphTypeRead

        #region BoneTypeRead

        private PmmStruct.ModelData.BoneInitFrame ReadBoneFrame(bool isInitFrame)
        {
            return new PmmStruct.ModelData.BoneInitFrame
            {
                DataIndex = isInitFrame ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt(),
                InterpolationX = ReadArray(4, ReadSByte),
                InterpolationY = ReadArray(4, ReadSByte),
                InterpolationZ = ReadArray(4, ReadSByte),
                InterpolationRotation = ReadArray(4, ReadSByte),
                Translation = ReadArray(3, ReadFloat),
                Quaternion = ReadArray(4, ReadFloat),
                IsSelected = ReadBool(),
                IsPhysicsDisabled = ReadBool()
            };
        }

        private PmmStruct.ModelData.BoneCurrentData ReadBoneCurrentData()
        {
            return new PmmStruct.ModelData.BoneCurrentData
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

        private PmmStruct.CameraFrame ReadCameraFrame(bool isInit)
        {
            return new PmmStruct.CameraFrame
            {
                DataIndex = isInit ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt(),
                Distance = ReadFloat(),
                EyePosition = ReadArray(3, ReadFloat),
                Rotation = ReadArray(3, ReadFloat),
                LookingModelIndex = ReadInt(),
                LookingBoneIndex = ReadInt(),
                InterpolationX = ReadArray(4, ReadSByte),
                InterpolationY = ReadArray(4, ReadSByte),
                InterpolationZ = ReadArray(4, ReadSByte),
                InterpolationRotation = ReadArray(4, ReadSByte),
                InterpolationDistance = ReadArray(4, ReadSByte),
                InterpolationAngleView = ReadArray(4, ReadSByte),
                IsParse = ReadBool(),
                AngleView = ReadInt(),
                IsSelected = ReadBool()
            };
        }

        private PmmStruct.CCameraCurrentData ReadCameraCurrentData()
        {
            return new PmmStruct.CCameraCurrentData
            {
                EyePosition = ReadArray(3, ReadFloat),
                TargetPosition = ReadArray(3, ReadFloat),
                Rotation = ReadArray(3, ReadFloat),
                IsOrthro = ReadBool()
            };
        }

        #endregion CameraTypeRead

        #region OpTypeRead

        private PmmStruct.ModelData.OpFrame ReadOpFrame(int ikCount, int opCount, bool isInit)
        {
            return new PmmStruct.ModelData.OpFrame
            {
                DataIndex = isInit ? -1 : ReadInt(),
                FrameNumber = ReadInt(),
                PreIndex = ReadInt(),
                NextIndex = ReadInt(),
                IsDisplay = ReadBool(),
                IsIkEnabled = ReadList(ikCount, ReadBool),
                OpData = ReadList(opCount, ReadPair),
                IsSelected = ReadBool()
            };
        }

        private PmmStruct.ModelData.OpCurrentData ReadOpCurrentData()
        {
            return new PmmStruct.ModelData.OpCurrentData
            {
                KeyFrameBegin = ReadInt(),
                KeyFrameEnd = ReadInt(),
                ModelIndex = ReadInt(),
                ParentBoneIndex = ReadInt()
            };
        }

        #endregion OpTypeRead

        #region TheOtherTypeRead

        private KeyValuePair<int, int> ReadPair()
        {
            var a = ReadInt();
            var b = ReadInt();
            return new KeyValuePair<int, int>(a, b);
        }

        #endregion TheOtherTypeRead
    }
}
