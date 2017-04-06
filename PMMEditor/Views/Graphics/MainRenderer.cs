using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using PMMEditor.ViewModels.Graphics;
using PMMEditor.Views.Timeline;
using Reactive.Bindings.Extensions;
using SharpDX;

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
        public MainRenderer()
        {
            if (IsInDesignMode)
            {
                return;
            }

            Children.ObserveAddChanged().Subscribe(_ => _.Initialize(Device)).AddTo(AllCompositeDisposable);
        }

        protected override void Render()
        {
            Parallel.ForEach(Children, _ => _.UpdateTask());
            foreach (var child in Children)
            {
                child.Update();
            }

            Matrix viewProj = View * Projection;
            foreach (var child in Children)
            {
                child.Render(new RenderArgs(Device.ImmediateContext, viewProj));
            }
        }
    }
}
