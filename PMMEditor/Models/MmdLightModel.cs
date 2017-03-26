using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Livet;
using PMMEditor.MMDFileParser;
using PMMEditor.MVVM;

namespace PMMEditor.Models
{
    using MmdLightKeyFrameList = KeyFrameList<MmdLightModel.BoneKeyFrame, DefaultKeyFrameInterpolationMethod<MmdLightModel.BoneKeyFrame>>;
    public class MmdLightModel : BindableBase
    {
        public class BoneKeyFrame : KeyFrameBase
        {
            public Point3D Translation { get; set; }

            public Color Color { get; set; }
        }

        #region BoneKeyList変更通知プロパティ

        public ObservableCollection<MmdLightKeyFrameList> BoneKeyList { get; }
            = new ObservableCollection<MmdLightKeyFrameList>();

        #endregion

        public async Task Set(List<PmmStruct.LightFrame> cameraData, PmmStruct.LightFrame lightInitFrame)
        {
            BoneKeyList.Clear();
            var keyFrame =
                MmdLightKeyFrameList.CreateKeyFrameArray(
                    cameraData);
            BoneKeyList.Add(await Task.Run(() =>
            {
                var list = new MmdLightKeyFrameList("");

                list.CreateKeyFrame(keyFrame, lightInitFrame, i =>
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
