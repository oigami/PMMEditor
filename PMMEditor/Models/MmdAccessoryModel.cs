using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents.DocumentStructures;
using System.Windows.Media.Media3D;
using Livet;
using PMMEditor.MMDFileParser;

namespace PMMEditor.Models
{
    public class MmdAccessoryModel : NotificationObject
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

        private readonly List<KeyFrameList<BoneKeyFrame>> _boneKeyList = new List<KeyFrameList<BoneKeyFrame>>();

        #region BonekeyListプロパティ

        private ReadOnlyCollection<KeyFrameList<BoneKeyFrame>> _BoneKeyList;

        public ReadOnlyCollection<KeyFrameList<BoneKeyFrame>> BoneKeyList
        {
            get { return _BoneKeyList; }
            set
            {
                if (_BoneKeyList == value)
                {
                    return;
                }
                _BoneKeyList = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Name変更通知プロパティ

        public string Name { get; private set; }

        public string NameEnglish { get; private set; }

        #endregion

        public async Task Set(PmmStruct.AccessoryData accessoryData)
        {
            Name = accessoryData.Name;
            NameEnglish = Name;
            _boneKeyList.Clear();
            var keyFrame = await KeyFrameList<BoneKeyFrame>.CreateKeyFrameArray(accessoryData.KeyFrames);
            _boneKeyList.Add(await Task.Run(async () =>
            {
                var list = new KeyFrameList<BoneKeyFrame>(Name);

                await list.CreateKeyFrame(keyFrame, accessoryData.InitFrame, i =>
                {
                    var res = new BoneKeyFrame
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
                    return res;
                });
                return list;
            }));
            BoneKeyList = new ReadOnlyCollection<KeyFrameList<BoneKeyFrame>>(_boneKeyList);
        }
    }
}
