using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.MVVM;

namespace PMMEditor.ECS
{
    public class Entity
    {
        private readonly List<Component> _components = new List<Component>();

        internal Entity(ECSystem system)
        {

        }

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T
            {
                GameObject = this
            };
            _components.Add(component);
            return component;
        }

        public void RemoveComponents<T>() where T : Component
        {
            foreach (var disposable in _components.Where(x => x is T))
            {
                disposable.Dispose();
            }

            _components.RemoveAll(x => x is T);
        }

        public void RemoveComponent(Component component)
        {
            if (_components.Remove(component))
            {
                component.Dispose();
            }
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
