using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.MVVM;

namespace PMMEditor.ECS
{
    public class Component : ECObject
    {
        private Entity _gameObject;

        protected Component() { }

        protected virtual void OnDestroy() { }

        protected override void OnDestroyInternal()
        {
            OnDestroy();
            GameObject.As()?.RemoveComponent(this);
        }

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

        public virtual void UpdateTask() { }
    }
}
