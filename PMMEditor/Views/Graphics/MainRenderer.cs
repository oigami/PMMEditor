using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;
using PMMEditor.ViewModels.Graphics;
using Reactive.Bindings.Extensions;

namespace PMMEditor.Views.Graphics
{
    public abstract class RendererPanel : SharpDxControl.SharpDxControl
    {
        public class ItemCollection : ObservableCollection<IRenderer> { }

        public ItemCollection Children { get; } = new ItemCollection();
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
            foreach (var child in Children)
            {
                child.Render(Device.ImmediateContext);
            }
        }
    }
}
