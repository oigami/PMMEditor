using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.MVVM;

namespace PMMEditor.ECS
{
    public class Entity : ECObject
    {
        private readonly List<Component> _components = new List<Component>();

        private bool _isExsits = true;

        /// <summary>
        /// 生存時に自身のオブジェクトを返します。すでに破壊されているときはnullを返します。
        /// </summary>
        /// <returns></returns>
        public Entity As() => _isExsits ? this : null;

        private readonly ECSystem _system;
        internal Entity(ECSystem system)
        {
            _system = system;
        }

        protected override void OnDestroyInternal()
        {
            _isExsits = false;
            _components.ForEach(Destroy);
            _system.DestroyEntity(this);
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T
            {
                GameObject = this
            };
            _components.Add(component);
            _system.AddComponent(component);
            return component;
        }

        internal void RemoveComponents<T>() where T : Component
        {
            _components.RemoveAll(x => x is T);
        }

        internal void RemoveComponent(Component component)
        {
            _components.Remove(component);
        }

        public Component GetComponent(Type type)
        {
            return _components.Find(type.IsInstanceOfType);
        }


        public T GetComponent<T>() where T : Component
        {
            return (T) _components.Find(x => x is T);
        }

        public IEnumerable<T> GetComponents<T>()
        {
            return _components.OfType<T>();
        }
    }
}
