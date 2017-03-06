using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Livet;

namespace PMMEditor.MVVM
{
    public class BindableBase : NotificationObject
    {
        protected bool SetProperty<T>(ref T t, T val, [CallerMemberName] string propertyName = "")
        {
            if (Equals(t, val))
            {
                return false;
            }
            t = val;
            RaisePropertyChanged(propertyName);
            return true;
        }
    }
}
