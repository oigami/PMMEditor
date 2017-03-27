using System;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
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

    public class BindableViewModel : ViewModel
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

    public class BindableDisposableBase : BindableBase, IDisposable
    {
        protected readonly CompositeDisposable CompositeDisposables = new CompositeDisposable();

        #region IDisposable Support

        private bool _disposedValue; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    CompositeDisposables.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
