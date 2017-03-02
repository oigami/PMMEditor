using System;
using System.Collections.ObjectModel;
using System.Windows.Markup;
using Reactive.Bindings.Extensions;

namespace PMMEditor.Views.Documents
{
    [ContentProperty("Children")]
    public class MainRenderer : SharpDxControl.SharpDxControl
    {
        public class ItemCollection : ObservableCollection<IRender> {}

        public ItemCollection Children { get; } = new ItemCollection();

        public MainRenderer()
        {
            if (IsInDesignMode)
            {
                return;
            }

            Children.ObserveAddChanged().Subscribe(_ => _.Initialize(Device)).AddTo(CompositeDisposable);
        }

        protected override void Render()
        {
            foreach (var child in Children)
            {
                child.Render(Device.ImmediateContext);
            }
        }

        /// <summary> 子オブジェクトを追加します。 </summary>
        /// <param name = "value"> 追加する子オブジェクト。 </param>
        public void AddChild(object value)
        {
            throw new NotImplementedException();
        }

        /// <summary> オブジェクトにノードのテキスト コンテンツを追加します。 </summary>
        /// <param name = "text"> オブジェクトに追加するテキスト。 </param>
        public void AddText(string text)
        {
            throw new NotImplementedException();
        }
    }
}
