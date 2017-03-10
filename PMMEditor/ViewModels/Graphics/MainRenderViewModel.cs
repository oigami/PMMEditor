using System;
using System.Reactive.Linq;
using System.Windows.Media;
using PMMEditor.Models;
using PMMEditor.Models.Graphics;
using PMMEditor.ViewModels.Documents;
using Reactive.Bindings;
using Reactive;
using Reactive.Bindings.Extensions;
using SharpDX.Direct3D11;

namespace PMMEditor.ViewModels.Graphics
{
    public class MainRenderViewModel : DocumentViewModelBase
    {
        private readonly GraphicsModel _model;

        public MainRenderViewModel(Model model)
        {
            _model = model.GraphicsModel;
            Device = _model.Device;
            NowFrame = model.ToReactivePropertyAsSynchronized(_ => _.NowFrame);
            Items = _model.MmdModelSource.ToReadOnlyReactiveCollection(_ => (IRenderer) new MmdModelRenderer(model, _))
                          .AddTo(CompositeDisposable);
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            NowFrame.Value++;
        }

        public ReactiveProperty<int> NowFrame { get; }

        public void Initialize() {}

        public static string GetTitle() => "Main Camera";
        public static string GetContentId() => typeof(MainRenderViewModel).FullName + GetTitle();

        public override string Title { get; } = GetTitle();

        public override string ContentId { get; } = GetContentId();

        public ReadOnlyReactiveCollection<IRenderer> Items { get; set; }

        public Device Device { get; private set; }
    }
}
