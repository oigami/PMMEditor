using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.MVVM;

namespace PMMEditor.ECS
{
    public class Component : BindableBase
    {
        private Entity _gameObject;

        protected Component() { }

        public Entity GameObject
        {
            get => _gameObject;
            internal set
            {
                if (_gameObject != null)
                {
                    throw new InvalidOperationException("代入は禁止です");
                }

                _gameObject = value;
            }
        }

        public virtual void Start() { }

        public virtual void Update() { }
    }

    public class ComponentDisposable : BindableBase, IDisposable
    {
        private Entity _gameObject;

        protected ComponentDisposable() { }

        public Entity GameObject
        {
            get => _gameObject;
            internal set
            {
                if (_gameObject != null)
                {
                    throw new InvalidOperationException("代入は禁止です");
                }

                _gameObject = value;
            }
        }

        public virtual void Start() { }

        public virtual void Update() { }

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
