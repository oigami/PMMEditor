using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PMMEditor.MMDFileParser
{
    public class PmmWriter
    {
        private readonly Stream _stream;

        public PmmWriter(Stream stream)
        {
            _stream = stream;
        }

        private void Write(byte[] bytes)
        {
            _stream.Write(bytes, 0, bytes.Length);
        }

        #region WritePrimitive

        private void Write(int value) => Write(BitConverter.GetBytes(value));
        private void Write(sbyte value) => Write((byte) value);
        private void Write(float value) => Write(BitConverter.GetBytes(value));
        private void Write(byte value) => _stream.WriteByte(value);
        private void Write(bool value) => _stream.WriteByte((byte) (value ? 1 : 0));

        private void Write(KeyValuePair<int, int> value)
        {
            Write(value.Key);
            Write(value.Value);
        }

        #endregion

        #region WriteString

        private void WriteVString(string num)
        {
            var bytes = Encoding.GetEncoding("shift_jis").GetBytes(num);
            Write((byte) bytes.Length);
            Write(bytes);
        }

        private void WriteFixedString(string str, int num)
        {
            _stream.Write(StringToByteArray(str, num), 0, num);
        }

        private static byte[] StringToByteArray(string str, int byteNum)
        {
            if (byteNum - str.Length > 0)
            {
                str += new string('\0', byteNum - str.Length);
            }

            return Encoding.GetEncoding("shift_jis").GetBytes(str);
        }

        #endregion

        #region WriteList

        private void WriteList<T>(List<T> list, Action<T> func)
        {
            for (int i = 0, len = list.Count; i < len; i++)
            {
                func(list[i]);
            }
        }

        private void WriteVList<T>(List<T> list, Action<T> func)
        {
            var len = list.Count;
            Write(len);
            WriteList(list, func);
        }

        private void WriteArray<T>(T[] list, Action<T> func)
        {
            for (int i = 0, len = list.Length; i < len; i++)
            {
                func(list[i]);
            }
        }

        #endregion

        #region WriteModel

        private void WriteBoneFrame(PmmStruct.ModelData.BoneInitFrame frame, bool isInit)
        {
            if (isInit == false)
            {
                Write(frame.DataIndex);
            }
            Write(frame.FrameNumber);
            Write(frame.PreIndex);
            Write(frame.NextIndex);
            WriteArray(frame.InterpolationX, Write);
            WriteArray(frame.InterpolationY, Write);
            WriteArray(frame.InterpolationZ, Write);
            WriteArray(frame.InterpolationRotation, Write);
            WriteArray(frame.Translation, Write);
            WriteArray(frame.Quaternion, Write);
            Write(frame.IsSelected);
            Write(frame.IsPhysicsDisabled);
        }

        private void WriteMorphFrame(PmmStruct.ModelData.MorphFrame frame, bool isInit)
        {
            if (isInit == false)
            {
                Write(frame.DataIndex);
            }
            Write(frame.FrameNumber);
            Write(frame.PreIndex);
            Write(frame.NextIndex);
            Write(frame.Value);
            Write(frame.IsSelected);
        }

        private void WriteOpFrame(PmmStruct.ModelData.OpFrame frame, bool isInit)
        {
            if (isInit == false)
            {
                Write(frame.DataIndex);
            }
            Write(frame.FrameNumber);
            Write(frame.PreIndex);
            Write(frame.NextIndex);
            Write(frame.IsDisplay);
            WriteList(frame.IsIkEnabled, Write);
            WriteList(frame.OpData, Write);
            Write(frame.IsSelected);
        }

        #endregion

        #region WriteCamera

        private void WriteCameraFrame(PmmStruct.CameraFrame frame, bool isInit)
        {
            if (isInit == false)
            {
                Write(frame.DataIndex);
            }
            Write(frame.FrameNumber);
            Write(frame.PreIndex);
            Write(frame.NextIndex);
            Write(frame.Distance);
            WriteArray(frame.EyePosition, Write);
            WriteArray(frame.Rotation, Write);
            Write(frame.LookingModelIndex);
            Write(frame.LookingBoneIndex);
            WriteArray(frame.InterpolationX, Write);
            WriteArray(frame.InterpolationY, Write);
            WriteArray(frame.InterpolationZ, Write);
            WriteArray(frame.InterpolationRotation, Write);
            WriteArray(frame.InterpolationDistance, Write);
            WriteArray(frame.InterpolationAngleView, Write);
            Write(frame.IsParse);
            Write(frame.AngleView);
            Write(frame.IsSelected);
        }

        private void WriteCameraCurrentData(PmmStruct.CCameraCurrentData data)
        {
            WriteArray(data.EyePosition, Write);
            WriteArray(data.TargetPosition, Write);
            WriteArray(data.Rotation, Write);
            Write(data.IsOrthro);
        }

        #endregion

        #region WriteLight

        private void WriteLightFrame(PmmStruct.LightFrame frame, bool isInit)
        {
            if (isInit == false)
            {
                Write(frame.DataIndex);
            }
            Write(frame.FrameNumber);
            Write(frame.PreIndex);
            Write(frame.NextIndex);
            Write(frame.R);
            Write(frame.G);
            Write(frame.B);
            Write(frame.X);
            Write(frame.Y);
            Write(frame.Z);
            Write(frame.IsSelected);
        }

        private void WriteLightCurrentData(PmmStruct.CLightCurrentData data)
        {
            Write(data.R);
            Write(data.G);
            Write(data.B);
            Write(data.X);
            Write(data.Y);
            Write(data.Z);
        }

        #endregion

        #region WriteAccessory

        private void WriteAccessoryDataBody(PmmStruct.AccessoryData.DataBody body)
        {
            Write((byte) ((body.Transparency << 1) | (byte) (body.IsVisible ? 1 : 0)));
            Write(body.ParentModelIndex);
            Write(body.ParentBoneIndex);
            WriteArray(body.Translation, Write);
            WriteArray(body.Rotation, Write);
            Write(body.Scale);
            Write(body.IsShadowEnabled);
        }

        private void WriteAccessoryKeyFrame(PmmStruct.AccessoryData.KeyFrame frame, bool isInit)
        {
            if (isInit == false)
            {
                Write(frame.DataIndex);
            }
            Write(frame.FrameNumber);
            Write(frame.PreIndex);
            Write(frame.NextIndex);
            WriteAccessoryDataBody(frame);
            Write(frame.IsSelected);
        }

        #endregion

        #region WriteGravity

        private void WriteGravityCurrentData(PmmStruct.CGravityCurrentData data)
        {
            Write(data.Acceleration);
            Write(data.NoizeAmount);
            Write(data.DirectionX);
            Write(data.DirectionY);
            Write(data.DirectionZ);
            Write(data.IsAddNoize);
        }


        private void WriteGravityKeyFrame(PmmStruct.GravityKeyFrame frame, bool isInit)
        {
            if (isInit == false)
            {
                Write(frame.DataIndex);
            }
            Write(frame.FrameNumber);
            Write(frame.PreIndex);
            Write(frame.NextIndex);
            Write(frame.IsAddNoize);
            Write(frame.NoizeAmount);
            Write(frame.Acceleration);
            Write(frame.DirectionX);
            Write(frame.DirectionY);
            Write(frame.DirectionZ);
            Write(frame.IsSelected);
        }

        #endregion

        #region WriteSelfShadow

        private void WriteSelfShadowKeyFrame(PmmStruct.SelfShadowFrame frame, bool isInit)
        {
            if (isInit == false)
            {
                Write(frame.DataIndex);
            }
            Write(frame.FrameNumber);
            Write(frame.PreIndex);
            Write(frame.NextIndex);
            Write(frame.Mode);
            Write(frame.Distance);
            Write(frame.IsSelected);
        }

        #endregion

        public void Write(PmmStruct pmm)
        {
            WriteFixedString(pmm.FormatId, 30);
            Write(pmm.ViewWidth);
            Write(pmm.ViewHeight);
            Write(pmm.FrameWidth);
            Write(pmm.EditViewAngle);
            Write(pmm.IsEditCameraLightAccessory);
            Write(pmm.IsOpenCameraPanel);
            Write(pmm.IsOpenLightPanel);
            Write(pmm.IsOpenAccessoryPanel);
            Write(pmm.IsOpenBonePanel);
            Write(pmm.IsOpenMorphPanel);
            Write(pmm.IsOpenSelfShadowPanel);
            Write(pmm.SelectedModelIndex);

            Write((byte) pmm.ModelDatas.Count);
            WriteList(pmm.ModelDatas, d =>
            {
                Write(d.Number);
                WriteVString(d.Name);
                WriteVString(d.NameEn);
                WriteFixedString(d.Path, 256);
                Write(d.KeyFrameEditorTopLevelRows);
                WriteVList(d.BoneName, WriteVString);
                WriteVList(d.MorphName, WriteVString);
                WriteVList(d.IkIndex, Write);
                WriteVList(d.OpIndex, Write);

                Write(d.DrawOrder);
                Write(d.EditIsDisplay);
                Write(d.EditSelectedBone);
                WriteArray(d.SkinPanel, Write);
                Write((byte) d.IsFrameOpen.Count);
                WriteList(d.IsFrameOpen, Write);
                Write(d.VScroll);
                Write(d.LastFrame);

                WriteList(d.BoneInitFrames, boneFrame => WriteBoneFrame(boneFrame, true));
                WriteVList(d.BoneKeyFrames, boneFrame => WriteBoneFrame(boneFrame, false));

                WriteList(d.MorphInitFrames, morphFrame => WriteMorphFrame(morphFrame, true));
                WriteVList(d.MorphKeyFrames, morphFrame => WriteMorphFrame(morphFrame, false));

                WriteOpFrame(d.OpInitFrame, true);
                WriteVList(d.OpKeyFrames, opFrame => WriteOpFrame(opFrame, false));

                WriteList(d.BoneCurrentDatas, data =>
                {
                    WriteArray(data.Translation, Write);
                    WriteArray(data.Quaternion, Write);
                    Write(data.IsEditUnCommited);
                    Write(data.IsPhysicsDisabled);
                    Write(data.IsRowSelected);
                });

                WriteList(d.MorphCurrentDatas, Write);
                WriteList(d.IsCurrentIkEnabledDatas, Write);
                WriteList(d.OpCurrentDatas, data =>
                {
                    Write(data.KeyFrameBegin);
                    Write(data.KeyFrameEnd);
                    Write(data.ModelIndex);
                    Write(data.ParentBoneIndex);
                });
                Write(d.IsAddBlend);
                Write(d.EdgeWidth);
                Write(d.IsSelfShadowEnabled);
                Write(d.CalcOrder);
            });

            WriteCameraFrame(pmm.CameraInitFrame, true);
            WriteVList(pmm.CameraKeyFrames, d => WriteCameraFrame(d, false));
            WriteCameraCurrentData(pmm.CameraCurrentData);

            WriteLightFrame(pmm.LightInitFrame, true);
            WriteVList(pmm.LightKeyFrames, d => WriteLightFrame(d, false));
            WriteLightCurrentData(pmm.LightCurrentData);

            Write(pmm.SelectedAccessoryIndex);
            Write(pmm.AccessoryVScroll);
            Write((byte) pmm.AccessoryName.Count);
            WriteList(pmm.AccessoryName, str => WriteFixedString(str, 100));
            WriteList(pmm.AccessoryDatas, d =>
            {
                Write(d.Index);
                WriteFixedString(d.Name, 100);
                WriteFixedString(d.Path, 256);
                Write(d.DrawOrder);
                WriteAccessoryKeyFrame(d.InitFrame, true);
                WriteVList(d.KeyFrames, key => WriteAccessoryKeyFrame(key, false));
                WriteAccessoryDataBody(d.CurrentData);
                Write(d.IsAddBlend);
            });

            Write(pmm.CurrentFramePosition);
            Write(pmm.HScrollPosition);
            Write(pmm.HScrollScale);
            Write(pmm.BoneOperationKind);
            Write(pmm.LookingAt);
            Write(pmm.IsRepeat);
            Write(pmm.IsPlayFromFrame);
            Write(pmm.IsPlayToFrame);
            Write(pmm.PlayStartFrame);
            Write(pmm.PlayEndFrame);

            Write(pmm.IsWaveEnabled);
            WriteFixedString(pmm.WavePath, 256);

            Write(pmm.AviOffsetX);
            Write(pmm.AviOffsetY);
            Write(pmm.AviScale);
            WriteFixedString(pmm.AviPath, 256);
            Write(pmm.IsShowAvi ? 1 : 0);

            Write(pmm.BackgroundImageOffsetX);
            Write(pmm.BackgroundImageOffsetY);
            Write(pmm.BackgroundImageScale);
            WriteFixedString(pmm.BackgroundImagePath, 256);
            Write(pmm.IsShowBackgroundImage);

            Write(pmm.IsShowInfomation);
            Write(pmm.IsShowAxis);
            Write(pmm.IsShowGroundShadow);
            Write(pmm.FpsLimit);
            Write(pmm.ScreenCaptureMode);
            Write(pmm.AccessoryNumberRenderAfterModel);
            Write(pmm.GroundShadowBrightness);
            Write(pmm.IsTransparentGroundShadow);
            Write(pmm.PhysicsMode);

            WriteGravityCurrentData(pmm.GravityCurrentData);
            WriteGravityKeyFrame(pmm.GravityInitFrame, true);
            WriteVList(pmm.GravityKeyFrames, d => WriteGravityKeyFrame(d, false));

            Write(pmm.IsShowSelfShadow);
            Write(pmm.SelfShadowCurrentData);
            WriteSelfShadowKeyFrame(pmm.SelfShadowInitFrame, true);
            WriteVList(pmm.SelfShadowKeyFrames, d => WriteSelfShadowKeyFrame(d, false));

            Write(pmm.EdgeColorR);
            Write(pmm.EdgeColorG);
            Write(pmm.EdgeColorB);

            Write(pmm.IsBlackBackground);

            Write(pmm.CameraCurrentLookingAtModel);
            Write(pmm.CameraCurrentLookingAtBone);

            WriteArray(new[] { 1.0f, 0.0f, 0.0f, 0.0f }, Write);
            WriteArray(new[] { 0.0f, 1.0f, 0.0f, 0.0f }, Write);
            WriteArray(new[] { 0.0f, 0.0f, 1.0f, 0.0f }, Write);
            WriteArray(new[] { 0.0f, 0.0f, 0.0f, 1.0f }, Write);

            Write(pmm.IsViewLookAtEnabled);

            Write((byte) 0);

            Write(pmm.IsPhysicsGroundEnabled);
            Write(pmm.FrameTextbox);

            Write(pmm.SelectorChoiceSelectionFollowing);
            WriteList(pmm.SelectorChoiceDatas, d =>
            {
                Write(d.ModelIndex);
                Write(d.SelectorChoice);
            });
        }
    }
}
