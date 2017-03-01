using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Livet;
using PMMEditor.MMDFileParser;

namespace PMMEditor.Models
{
    public class MmdLightModel : NotificationObject
    {
        public class BoneKeyFrame : KeyFrameBase
        {
            public Point3D Translation { get; set; }

            public Color Color { get; set; }
        }

        #region BoneKeyList変更通知プロパティ

        private ObservableCollection<KeyFrameList<BoneKeyFrame>> _BoneKeyList =
            new ObservableCollection<KeyFrameList<BoneKeyFrame>>();

        public ObservableCollection<KeyFrameList<BoneKeyFrame>> BoneKeyList
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

        public async Task Set(List<PmmStruct.LightFrame> cameraData, PmmStruct.LightFrame lightInitFrame)
        {
            BoneKeyList.Clear();
            var keyFrame = await KeyFrameList<BoneKeyFrame>.CreateKeyFrameArray(cameraData);
            BoneKeyList.Add(await Task.Run(async () =>
            {
                var list = new KeyFrameList<BoneKeyFrame>("");

                await list.CreateKeyFrame(keyFrame, lightInitFrame, i =>
                {
                    var res = new BoneKeyFrame
                    {
                        IsSelected = i.IsSelected,
                        Translation = new Point3D(i.X, i.Y, i.Z),
                        Color = new Color
                        {
                            ScR = i.R,
                            ScG = i.G,
                            ScB = i.B,
                            ScA = 1.0f
                        }
                    };
                    return res;
                });
                return list;
            }));
        }
    }
}
