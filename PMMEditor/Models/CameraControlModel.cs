using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using PMMEditor.MMDFileParser;
using PMMEditor.MVVM;
using Reactive.Bindings.Extensions;
using SharpDX;

namespace PMMEditor.Models
{
    public class CameraControlModel : BindableDisposableBase
    {
        public float Distance { get; set; } = 45;

        public Vector3 Rotate { get; set; } = new Vector3(0, 0, 0);

        public Vector3 LookAt { get; set; } = new Vector3(0, 10, 0);

        public Matrix Perspective { get; set; } = Matrix.PerspectiveFovLH((float) Math.PI / 3, 1.4f, 1f, 10000000f);


        public CameraControlModel(Model model)
        {
            model.FrameControlModel.ObserveProperty(_ => _.NowFrame).Subscribe(_ =>
            {
                if (BoneKeyList.Count == 0)
                {
                    return;
                }
                var data = BoneKeyList[0].GetInterpolationData(_);
                Distance = -data.Distance;
                Rotate = data.Rotation;
                LookAt = data.Translation;
            }).AddTo(CompositeDisposable);
        }

        public Matrix CreateProjection()
        {
            return Perspective;
        }

        public Matrix CreateView()
        {
            return Matrix.Translation(0, 0, Distance);
        }

        public Matrix CreateWorld()
        {
            var w = Matrix.Translation(LookAt);
            w *= Matrix.RotationY(-Rotate.Y) * Matrix.RotationX(-Rotate.X);

            return Matrix.Invert(w);
        }

        public Matrix CreateWorldViewProj()
        {
            return CreateWorld() * CreateView() * CreateProjection();
        }

        public class BoneKeyFrame : KeyFrameBase
        {
            public Vector3 Translation { get; set; }

            public float Distance { get; set; }

            public int LookingBoneIndex { get; set; }

            public int LookingModelIndex { get; set; }

            public bool IsParse { get; set; }

            public int AngleView { get; set; }

            public Vector3 Rotation { get; set; }

            public int[] InterpolationX { get; set; }

            public int[] InterpolationY { get; set; }

            public int[] InterpolationZ { get; set; }

            public int[] InterpolationRotaiton { get; set; }

            public int[] InterpolationDistance { get; set; }

            public int[] InterpolationAngleView { get; set; }
        }

        public class BoneInterpolationMethod : IKeyFrameInterpolationMethod<BoneKeyFrame>
        {
            private readonly BoneKeyFrame _res;

            public BoneInterpolationMethod()
            {
                _res = new BoneKeyFrame();
            }

            public BoneKeyFrame Interpolation(BoneKeyFrame left, BoneKeyFrame right, int frame)
            {
                var t = (float) (frame - left.FrameNumber) / (right.FrameNumber - left.FrameNumber);
                _res.Translation = left.Translation + (right.Translation - left.Translation) * t;
                _res.Rotation = left.Rotation + (right.Rotation - left.Rotation) * t;
                _res.Distance = left.Distance + (right.Distance - left.Distance) * t;
                return _res;
            }
        }

        #region BoneKeyList変更通知プロパティ

        public ObservableCollection<KeyFrameList<BoneKeyFrame, BoneInterpolationMethod>> BoneKeyList { get; } =
            new ObservableCollection<KeyFrameList<BoneKeyFrame, BoneInterpolationMethod>>();

        #endregion

        public async Task Set(List<PmmStruct.CameraFrame> cameraData, PmmStruct.CameraFrame cameraInitFrame)
        {
            BoneKeyList.Clear();
            var keyFrame =
                await KeyFrameList<BoneKeyFrame, DefaultKeyFrameInterpolationMethod<BoneKeyFrame>>
                    .CreateKeyFrameArray(cameraData);
            BoneKeyList.Add(await Task.Run(async () =>
            {
                var list = new KeyFrameList<BoneKeyFrame, BoneInterpolationMethod>("");
                Func<sbyte[], int[]> createArray4 = i => new int[] { i[0], i[1], i[2], i[3] };

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
                        Translation = new Vector3(i.EyePosition[0], i.EyePosition[1], i.EyePosition[2]),
                        Rotation = new Vector3(i.Rotation[0], i.Rotation[1], i.Rotation[2]),
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
        }
    }
}
