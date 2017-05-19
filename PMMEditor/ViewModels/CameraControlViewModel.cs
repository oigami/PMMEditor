using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Livet.Commands;
using PMMEditor.Models;
using PMMEditor.Models.MMDModel;
using PMMEditor.MVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

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
            string[] s = str.Split(',');
            if (s.Length != 3)
            {
                throw new ArgumentException(nameof(value));
            }

            Vector3 res = Vector3.Zero;
            res.X = float.Parse(s[0].Trim());
            res.Y = float.Parse(s[1].Trim());
            res.Z = float.Parse(s[2].Trim());

            return res;
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

        private void SetLookAt(Vector3 value)
        {
        }

        public float LookAtX
        {
            get { return Model.LookAt.X; }
            set
            {
                Vector3 tmp = Model.LookAt;
                tmp.X = value;
                Model.LookAt = tmp;
            }
        }

        public float LookAtY
        {
            get { return Model.LookAt.Y; }
            set
            {
                Vector3 tmp = Model.LookAt;
                tmp.Y = value;
                Model.LookAt = tmp;
            }
        }

        public float LookAtZ
        {
            get { return Model.LookAt.Z; }
            set
            {
                Vector3 tmp = Model.LookAt;
                tmp.Z = value;
                Model.LookAt = tmp;
            }
        }

        public float RotateX
        {
            get { return Model.Rotate.X; }
            set
            {
                Vector3 tmp = Model.Rotate;
                tmp.X = value;
                Model.Rotate = tmp;
            }
        }

        public float RotateY
        {
            get { return Model.Rotate.Y; }
            set
            {
                Vector3 tmp = Model.Rotate;
                tmp.Y = value;
                Model.Rotate = tmp;
            }
        }

        public float RotateZ
        {
            get { return Model.Rotate.Z; }
            set
            {
                Vector3 tmp = Model.Rotate;
                tmp.Z = value;
                Model.Rotate = tmp;
            }
        }

        #region ResetFrontCommand

        private ListenerCommand<CameraData> _resetCameraCommand;

        public ListenerCommand<CameraData> ResetCameraCommand
            => _resetCameraCommand ?? (_resetCameraCommand = new ListenerCommand<CameraData>(
                _ => Model.SetView(_.LookAt, MathUtil.DegreeToRadian(_.Rotate), _.Distance)));

        #endregion
    }
}
