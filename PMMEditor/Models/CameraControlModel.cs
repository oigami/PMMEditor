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
    public static class MathUtil
    {
        public static Vector3 DegreesToRadians(Vector3 vec)
        {
            return new Vector3(SharpDX.MathUtil.DegreesToRadians(vec.X),
                               SharpDX.MathUtil.DegreesToRadians(vec.Y),
                               SharpDX.MathUtil.DegreesToRadians(vec.Z));
        }
    }

    public class CameraControlModel : BindableDisposableBase
    {
        private Matrix _view;
        private float _distance;
        private Vector3 _rotate;

        private bool _isUpdateRequired = true;

        private bool IsUpdateRequired
        {
            get { return _isUpdateRequired; }
            set
            {
                if (_isUpdateRequired == value)
                {
                    return;
                }

                _isUpdateRequired = value;
                if (value)
                {
                    RaisePropertyChanged(nameof(View));
                }
            }
        }

        private Vector3 _lookAt;

        public float Distance
        {
            get { return _distance; }
            set
            {
                if (SetProperty(ref _distance, value))
                {
                    IsUpdateRequired = true;
                }
            }
        }

        /// <summary>
        /// ラジアン角度
        /// </summary>
        public Vector3 Rotate
        {
            get { return _rotate; }
            set
            {
                if (SetProperty(ref _rotate, value))
                {
                    IsUpdateRequired = true;
                }
            }
        }

        public Vector3 LookAt
        {
            get { return _lookAt; }
            set
            {
                if (SetProperty(ref _lookAt, value))
                {
                    IsUpdateRequired = true;
                }
            }
        }

        public Matrix Perspective { get; set; } = Matrix.PerspectiveFovLH((float) Math.PI / 3, 1.4f, 1f, 10000000f);

        public Matrix View
        {
            get
            {
                if (IsUpdateRequired)
                {
                    _view = CreateView();
                    IsUpdateRequired = false;
                }
                return _view;
            }
        }

        public void AddRotate(Vector3 addRot)
        {
            Rotate += addRot;
        }

        public void Transform(Vector2 addLookAt)
        {
            Matrix w = Matrix.RotationX(-Rotate.X) * Matrix.RotationY(-Rotate.Y);
            Vector4 add = Vector3.Transform(new Vector3(addLookAt.X, addLookAt.Y, 0), w);
            LookAt += new Vector3(add.X, add.Y, add.Z);
        }

        public void SetView(Vector3 lookAt, Vector3 rotate, float distance)
        {
            _lookAt = lookAt;
            _rotate = rotate;
            _distance = distance;
            RaisePropertyChanged(nameof(LookAt));
            RaisePropertyChanged(nameof(Rotate));
            RaisePropertyChanged(nameof(Distance));
            IsUpdateRequired = true;
        }

        public CameraControlModel(Model model)
        {
            Clear();
            model.FrameControlModel.ObserveProperty(_ => _.NowFrame).Subscribe(_ =>
            {
                if (BoneKeyList.Count == 0)
                {
                    return;
                }

                BoneKeyFrame data = BoneKeyList[0].GetInterpolationData(_);
                Distance = -data.Distance;
                Rotate = data.Rotation;
                LookAt = data.Translation;
            }).AddTo(CompositeDisposables);
        }

        public Matrix CreateProjection()
        {
            return Perspective;
        }

        private Matrix CreateView()
        {
            Matrix w = Matrix.Translation(-LookAt);
            w *= Matrix.RotationY(Rotate.Y) * Matrix.RotationX(Rotate.X);

            return w * Matrix.Translation(0, 0, Distance);
        }

        public Matrix CreateViewProj()
        {
            return CreateView() * CreateProjection();
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
                float t = (float) (frame - left.FrameNumber) / (right.FrameNumber - left.FrameNumber);
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
            PmmStruct.CameraFrame[] keyFrame =
                KeyFrameList<BoneKeyFrame, DefaultKeyFrameInterpolationMethod<BoneKeyFrame>>
                    .CreateKeyFrameArray(cameraData);
            BoneKeyList.Add(await Task.Run(() =>
            {
                var list = new KeyFrameList<BoneKeyFrame, BoneInterpolationMethod>("");
                Func<sbyte[], int[]> createArray4 = i => new int[] { i[0], i[1], i[2], i[3] };

                list.CreateKeyFrame(keyFrame, cameraInitFrame, i =>
                {
                    return new BoneKeyFrame
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
                });
                return list;
            }));
        }

        public void Clear()
        {
            BoneKeyList.Clear();
            SetView(new Vector3(0, 10, 0), Vector3.Zero, 45.0f);
        }
    }
}
