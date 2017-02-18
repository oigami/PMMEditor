using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Livet;

namespace PMMEditor.Models
{
    public class MmdCameraModel : NotificationObject
    {
        public class BoneKeyFrame : KeyFrameBase
        {
            public Point3D Translation { get; set; }

            public float Distance { get; set; }

            public int LookingBoneIndex { get; set; }

            public int LookingModelIndex { get; set; }

            public bool IsParse { get; set; }

            public int AngleView { get; set; }

            public float[] Rotation { get; set; }

            public int[] InterpolationX { get; set; }

            public int[] InterpolationY { get; set; }

            public int[] InterpolationZ { get; set; }

            public int[] InterpolationRotaiton { get; set; }

            public int[] InterpolationDistance { get; set; }

            public int[] InterpolationAngleView { get; set; }
        }

        private readonly List<KeyFrameList<BoneKeyFrame>> _boneKeyList = new List<KeyFrameList<BoneKeyFrame>>();

        #region BoneKeyList変更通知プロパティ

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

        public async Task Set(List<PmmStruct.CameraFrame> cameraData, PmmStruct.CameraFrame cameraInitFrame)
        {
            _boneKeyList.Clear();
            var keyFrame = await KeyFrameList<BoneKeyFrame>.CreateKeyFrameArray(cameraData);
            _boneKeyList.Add(await Task.Run(async () =>
            {
                var list = new KeyFrameList<BoneKeyFrame>("");
                Func<sbyte[], int[]> createArray4 = i => new int[] {i[0], i[1], i[2], i[3]};

                await list.CreateKeyFrame(keyFrame, cameraInitFrame, i =>
                {
                    var res = new BoneKeyFrame
                    {
                        IsSelected = i.IsSelected,
                        IsParse = i.IsParse,
                        AngleView = i.AngleView,
                        LookingModelIndex = i.LookingModelIndex,
                        LookingBoneIndex = i.LookingBoneIndex,
                        Distance = i.Distance,
                        Translation = new Point3D(i.EyePosition[0], i.EyePosition[1], i.EyePosition[2]),
                        Rotation = new[] {i.Rotation[0], i.Rotation[1], i.Rotation[2]},
                        InterpolationX = createArray4(i.InterpolationX),
                        InterpolationY = createArray4(i.InterpolationY),
                        InterpolationZ = createArray4(i.InterpolationZ),
                        InterpolationAngleView = createArray4(i.InterpolationAngleView),
                        InterpolationDistance = createArray4(i.InterpolationDistance),
                        InterpolationRotaiton = createArray4(i.InterpolationRotation)
                    };
                    return res;
                });
                return list;
            }));
            BoneKeyList = new ReadOnlyCollection<KeyFrameList<BoneKeyFrame>>(_boneKeyList);
        }
    }
}
