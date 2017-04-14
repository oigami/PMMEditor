using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using PMMEditor.ViewModels.Graphics;
using PMMEditor.Views.Timeline;
using Reactive.Bindings.Extensions;
using SharpDX;
using SharpDX.Direct3D11;

namespace PMMEditor.Views.Graphics
{
    public abstract class RendererPanel : SharpDxControl.SharpDxControl
    {
        public class ItemCollection : ObservableCollection<IRenderer> { }

        public ItemCollection Children { get; } = new ItemCollection();

        public static readonly DependencyProperty ViewProperty =
            DependencyProperty.Register(nameof(View), typeof(Matrix), typeof(MainRenderer),
                                        new FrameworkPropertyMetadata(default(Matrix)));

        public Matrix View
        {
            get { return (Matrix) GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        public static readonly DependencyProperty ProjectionProperty =
            DependencyProperty.Register(nameof(Projection), typeof(Matrix), typeof(MainRenderer),
                                        new FrameworkPropertyMetadata(default(Matrix)));

        public Matrix Projection
        {
            get { return (Matrix) GetValue(ProjectionProperty); }
            set { SetValue(ProjectionProperty, value); }
        }
    }

    [ContentProperty("Children")]
    public class MainRenderer : RendererPanel
    {
        private readonly List<DeviceContext> _deviceContexts = new List<DeviceContext>();
        private readonly List<CommandList> _commandLists = new List<CommandList>();

        public MainRenderer()
        {
            if (IsInDesignMode)
            {
                return;
            }

            Children.ObserveAddChanged().Subscribe(_ =>
            {
                _.Initialize(Device);
                if (_deviceContexts.Count < Children.Count)
                {
                    _deviceContexts.Add(new DeviceContext(Device));
                    _commandLists.Add(null);
                }
            }).AddTo(AllCompositeDisposable);
        }

        protected override void Render()
        {

            Task.WhenAll(Children.Select(x => Task.Run(() => x.UpdateTask()))).Wait();
            foreach (var child in Children)
            {
                child.Update();
            }

            Matrix viewProj = View * Projection;

            Task.WhenAll(Children.Indexed().Select(x => Task.Run(() =>
            {
                var (child, i) = x;
                SetRenderTarget(_deviceContexts[i]);
                child.Render(new RenderArgs(_deviceContexts[i], viewProj));
                _commandLists[i] = _deviceContexts[i].FinishCommandList(false);
            }))).Wait();
            foreach (var command in _commandLists)
            {
                Device.ImmediateContext.ExecuteCommandList(command, false);
                command.Dispose();
            }
        }

        protected override void Render2D()
        {
            foreach (var child in Children)
            {
                child.Render(new Render2DArgs(D2DRenderTarget));
            }
        }
    }
}
