using System;
using System.Collections.ObjectModel;
using System.Windows.Markup;
using PMMEditor.ViewModels.Graphics;
using Reactive.Bindings.Extensions;

namespace PMMEditor.Views.Documents
{
    public abstract class RendererPanel : SharpDxControl.SharpDxControl
    {
        public class ItemCollection : ObservableCollection<IRenderer> {}

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
            foreach (var child in Children)
            {
                child.Render(Device.ImmediateContext);
            }
        }
    }
}
