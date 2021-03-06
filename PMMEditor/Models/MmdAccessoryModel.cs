﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using PMMEditor.MMDFileParser;
using PMMEditor.MVVM;

namespace PMMEditor.Models
{
    using MmdAccessoryKeyFrameList = KeyFrameList<MmdAccessoryModel.BoneKeyFrame, DefaultKeyFrameInterpolationMethod<MmdAccessoryModel.BoneKeyFrame>>;
    public class MmdAccessoryModel : BindableBase
    {
        public class BoneKeyFrame : KeyFrameBase
        {
            public Point3D Translation { get; set; }

            public float Scale { get; set; }

            public int ParentModelIndex { get; set; }

            public int ParentBoneIndex { get; set; }

            public int Transparency { get; set; }

            public bool IsShadowEnabled { get; set; }

            public bool IsVisible { get; set; }
        }

        #region BonekeyListプロパティ


        public ObservableCollection<MmdAccessoryKeyFrameList> BoneKeyList { get; } =
            new ObservableCollection<MmdAccessoryKeyFrameList>();

        #endregion

        #region Name変更通知プロパティ

        public string Name { get; private set; }

        public string NameEnglish { get; private set; }

        #endregion

        public async Task Set(PmmStruct.AccessoryData accessoryData)
        {
            Name = accessoryData.Name;
            NameEnglish = Name;
            BoneKeyList.Clear();
            PmmStruct.AccessoryData.KeyFrame[] keyFrame = MmdAccessoryKeyFrameList.CreateKeyFrameArray(accessoryData.KeyFrames);
            BoneKeyList.Add(await Task.Run(() =>
            {
                var list = new MmdAccessoryKeyFrameList(Name);

                list.CreateKeyFrame(keyFrame, accessoryData.InitFrame, i =>
                {
                    return new BoneKeyFrame
                    {
                        IsSelected = i.IsSelected,
                        IsShadowEnabled = i.IsShadowEnabled,
                        IsVisible = i.IsVisible,
                        ParentBoneIndex = i.ParentBoneIndex,
                        ParentModelIndex = i.ParentModelIndex,
                        Scale = i.Scale,
                        Translation = new Point3D(i.Translation[0], i.Translation[1], i.Translation[2]),
                        Transparency = i.Transparency
                    };
                });
                return list;
            }));
        }
    }
}
