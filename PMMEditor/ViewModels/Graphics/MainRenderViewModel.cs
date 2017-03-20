using System;
using PMMEditor.Models;
using PMMEditor.Models.Graphics;
using PMMEditor.ViewModels.Documents;
using Reactive.Bindings;
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
            NowFrame = model.FrameControlModel.ObserveProperty(_ => _.NowFrame).ToReadOnlyReactiveProperty()
                            .AddTo(CompositeDisposable);
            Items = _model.MmdModelSource.ToReadOnlyReactiveCollection(_ => (IRenderer) new MmdModelRenderer(model, _), UIDispatcherScheduler.Default)
                          .AddTo(CompositeDisposable);
        }

        public ReadOnlyReactiveProperty<int> NowFrame { get; }

        public void Initialize() {}

        public static string GetTitle() => "Main Camera";
        public static string GetContentId() => typeof(MainRenderViewModel).FullName + GetTitle();

        public override string Title { get; } = GetTitle();

        public override string ContentId { get; } = GetContentId();

        public ReadOnlyReactiveCollection<IRenderer> Items { get; set; }

        public Device Device { get; private set; }
    }
}
