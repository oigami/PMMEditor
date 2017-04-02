using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Livet.Commands;
using PMMEditor.Models;
using PMMEditor.MVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using SharpDX;

namespace PMMEditor.ViewModels
{
    public struct CameraData
    {
        [TypeConverter(typeof(StringToVector3Array))]
        public Vector3 LookAt { get; set; }

        [TypeConverter(typeof(StringToVector3Array))]
        public Vector3 Rotate { get; set; }

        public float Distance { get; set; }
    }

    public class StringToVector3Array : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var str = (string) value;
            var s = str.Split(',');
            var arr = new float[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                float.TryParse(s[i], out arr[i]);
            }
            return new Vector3(arr[0], arr[1], arr[2]);
        }
    }

    public class CameraControlViewModel : BindableDisposableBase
    {
        public CameraControlModel Model { get; }

        public CameraControlViewModel(CameraControlModel model)
        {
            Model = model;
            Distance = Model.ToReactivePropertyAsSynchronized(_ => _.Distance).AddTo(CompositeDisposables);
            Model.ObserveProperty(_ => _.LookAt).Subscribe(_ =>
            {
                RaisePropertyChanged(nameof(LookAtX));
                RaisePropertyChanged(nameof(LookAtY));
                RaisePropertyChanged(nameof(LookAtZ));
            }).AddTo(CompositeDisposables);
            Model.ObserveProperty(_ => _.Rotate).Subscribe(_ =>
            {
                RaisePropertyChanged(nameof(RotateX));
                RaisePropertyChanged(nameof(RotateY));
                RaisePropertyChanged(nameof(RotateZ));
            }).AddTo(CompositeDisposables);
        }

        public ReactiveProperty<float> Distance { get; set; }

        private void SetLookAt(int index, float value)
        {
            if (Math.Abs(Model.LookAt[index] - value) > 1e-5f)
            {
                var tmp = Model.LookAt;
                tmp[index] = value;
                Model.LookAt = tmp;
            }
        }

        public float LookAtX
        {
            get { return Model.LookAt.X; }
            set { SetLookAt(0, value); }
        }

        public float LookAtY
        {
            get { return Model.LookAt.Y; }
            set { SetLookAt(1, value); }
        }

        public float LookAtZ
        {
            get { return Model.LookAt.Z; }
            set { SetLookAt(2, value); }
        }


        private void SetRotate(int index, float value)
        {
            if (Math.Abs(Model.Rotate[index] - value) > 1e-5f)
            {
                var tmp = Model.Rotate;
                tmp[index] = value;
                Model.Rotate = tmp;
            }
        }

        public float RotateX
        {
            get { return Model.Rotate.X; }
            set { SetRotate(0, value); }
        }

        public float RotateY
        {
            get { return Model.Rotate.Y; }
            set { SetRotate(1, value); }
        }

        public float RotateZ
        {
            get { return Model.Rotate.Z; }
            set { SetRotate(2, value); }
        }

        #region ResetFrontCommand

        private ListenerCommand<CameraData> _resetCameraCommand;

        public ListenerCommand<CameraData> ResetCameraCommand
            => _resetCameraCommand ?? (_resetCameraCommand = new ListenerCommand<CameraData>(
                _ => Model.SetView(_.LookAt, Models.MathUtil.DegreesToRadians(_.Rotate), _.Distance)));

        #endregion
    }
}
