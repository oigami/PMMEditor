using PMMEditor.Models;
using PMMEditor.Views.Documents;
using Reactive.Bindings;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace PMMEditor.ViewModels.Documents
{
    public class MainRenderViewModel : DocumentViewModelBase
    {
        private Model _model;

        public MainRenderViewModel(Model model)
        {
            Device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            _model = model;
            Items = _model.MmdModelList.List.ToReadOnlyReactiveCollection(_ => (IRenderer) new MmdModelRenderer(_));
        }
            
        public void Initialize()
        {
        }

        public static string GetTitle() => "Main Camera";
        public static string GetContentId() => typeof(MainRenderViewModel).FullName + GetTitle();

        public override string Title { get; } = GetTitle();

        public override string ContentId { get; } = GetContentId();

        public ReadOnlyReactiveCollection<IRenderer> Items { get; set; }

        public Device Device { get; private set; }
    }
}
