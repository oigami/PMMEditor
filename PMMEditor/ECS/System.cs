using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using PMMEditor.Models.Thread;
using PMMEditor.SharpDxControl;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace PMMEditor.ECS
{
    public class ECSystem : IDisposable
    {
        internal static Device Device { get; set; }

        private readonly List<Entity> _entity = new List<Entity>();

        private readonly ThreadQueue _renderQueue = new ThreadQueue();
        private readonly ThreadQueue _mainQueue = new ThreadQueue();

        private readonly List<RendererArgs> _rendererComponents = new List<RendererArgs>();

        private readonly List<Component> _allComponents = new List<Component>();
        private readonly List<Component> _newComponents = new List<Component>();
        private readonly List<Component> _deleteComponents = new List<Component>();

        public RenderTextureQueue RenderTextureQueue { get; } = new RenderTextureQueue();

        public ECSystem()
        {
            if (Device == null)
            {
                throw new ArgumentNullException(nameof(Device));
            }

            _mainQueue.PushQueue(() => Update());
            _renderQueue.PushQueue(() => Render());
        }

        public Entity CreateEntity()
        {
            var entity = new Entity(this);
            _entity.Add(entity);
            return entity;
        }

        internal void DestroyEntity(Entity entity)
        {
            _entity.Remove(entity);
        }

        internal void AddComponent(Component component)
        {
            lock (_newComponents)
            {
                _newComponents.Add(component);
            }
        }

        internal void RemoveComponent(Component component)
        {
            lock (_deleteComponents)
            {
                _deleteComponents.Add(component);
            }

        }

        public void Update()
        {
            lock (_deleteComponents)
            {
                lock (_rendererComponents)
                {
                    foreach (var component in _deleteComponents)
                    {
                        _allComponents.Remove(component);
                        if (component is Renderer x)
                        {
                            _rendererComponents.RemoveAll(y => y.Renderer == x);
                        }
                    }
                }

                _deleteComponents.Clear();
            }

            lock (_newComponents)
            {
                if (_newComponents.Count != 0)
                {
                    lock (_rendererComponents)
                    {
                        foreach (var component in _newComponents)
                        {
                            _allComponents.Add(component);
                            component.Start();
                            if (component is Renderer x)
                            {
                                _rendererComponents.Add(new RendererArgs(x, Device));
                            }
                        }

                        foreach (var component in _rendererComponents)
                        {
                            foreach (var o in component.UpdatedDataQueue)
                            {
                                if (o != null)
                                {
                                    component.Renderer.EnqueueRenderData(o);
                                }
                            }

                            component.RenderData = null;
                            component.UpdatedDataQueue.Clear();
                        }
                    }
                }

                _newComponents.Clear();
            }

            lock (_rendererComponents)
            {
                if (_rendererComponents.Count != 0 && _rendererComponents[0].UpdatedDataQueue.Count >= 2)
                {
                    return;
                }

                Parallel.ForEach(_allComponents, x => x.UpdateTask());

                foreach (var component in _allComponents)
                {
                    component.Update();
                }

                foreach (var component in _rendererComponents)
                {
                    if (component.RenderData != null)
                    {
                        component.Renderer.EnqueueRenderData(component.RenderData);
                    }
                    component.UpdatedDataQueue.Enqueue(component.Renderer.DequeueRenderData());
                }
            }
        }

        public void Render()
        {

            Device device = Device;

            lock (_rendererComponents)
            {
                if (_rendererComponents.Count != 0 && _rendererComponents[0].UpdatedDataQueue.Count != 0)
                {
                    RenderTexture tex = RenderTextureQueue.Dequeue();
                    if (tex == null)
                    {
                        return;
                    }

                    foreach (var data in _rendererComponents)
                    {
                        data.RenderData = data.UpdatedDataQueue.Dequeue();
                        data.Context.Rasterizer.SetViewport(0, 0, tex.Width, tex.Height);
                        data.Context.OutputMerger.SetRenderTargets(tex.DepthBuffer, tex.ColorBuffer);
                        data.Renderer.Render(data);
                        data.CommandList = data.Context.FinishCommandList(false);
                    }

                    DeviceContext context = device.ImmediateContext;
                    lock (context)
                    {
                        context.ClearDepthStencilView(tex.DepthBuffer, DepthStencilClearFlags.Depth, 1.0f, 0);
                        context.ClearRenderTargetView(tex.ColorBuffer, new RawColor4(0, 0, 0, 0));
                        foreach (var command in _rendererComponents)
                        {
                            context.ExecuteCommandList(command.CommandList, false);
                            command.CommandList.Dispose();
                        }
                    }

                    RenderTextureQueue.Enqueue(tex);
                }
            }

        }


        public class RendererArgs
        {
            public RendererArgs(Renderer renderer, Device device)
            {
                Renderer = renderer;
                Context = new DeviceContext(device);
                RenderData = null;
            }

            public Renderer Renderer { get; }

            public DeviceContext Context { get; }

            public Queue<object> UpdatedDataQueue { get; } = new Queue<object>();

            public object RenderData { get; set; }

            public CommandList CommandList { get; set; }
        }

        #region IDisposable Support
        private bool _disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _mainQueue.Dispose();
                    _renderQueue.Dispose();
                }

                _disposedValue = true;
            }
        }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
