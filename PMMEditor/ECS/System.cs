using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace PMMEditor.ECS
{
    public class ECSystem
    {
        internal static Device Device { get; set; }

        private List<Entity> _entity = new List<Entity>();
        private List<Entity> _newEntitiy = new List<Entity>();

        public Entity CreateEntity()
        {
            var entity = new Entity(this);
            _newEntitiy.Add(entity);
            return entity;
        }

        internal void DestroyEntity(Entity entity)
        {
            _entity.Remove(entity);
            _newEntitiy.Remove(entity);
        }

        internal void AddComponent(Component component)
        {
            _allComponents.Add(component);
            if (component is Renderer x)
            {
                _rendererComponents.Add(x);
            }
        }

        internal void RemoveComponent(Component component)
        {
            _allComponents.Remove(component);
            if (component is Renderer x)
            {
                _rendererComponents.Remove(x);
            }
        }

        public void Update()
        {
            foreach (var component in _allComponents)
            {
                component.Update();
            }
        }

        public void Render()
        {
            foreach (var renderer in _rendererComponents)
            {
                renderer.Render();
            }
        }

        private readonly List<Component> _allComponents = new List<Component>();
        private readonly List<Renderer> _rendererComponents = new List<Renderer>();
    }
}
