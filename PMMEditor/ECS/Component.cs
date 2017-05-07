using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.MVVM;

namespace PMMEditor.ECS
{
    public class Component : BindableDisposableBase
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
}
