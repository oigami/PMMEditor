using PMMEditor.Log;
using PMMEditor.MVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using SharpDX.Direct3D;
using Direct3D11 = SharpDX.Direct3D11;

namespace PMMEditor.Models.Graphics
{
    public class GraphicsModel : BindableDisposableBase
    {
        private readonly ILogger _logger;

        public GraphicsModel(ILogger logger, MmdModelList mmdModelList)
        {
            _logger = logger;
            MmdModelSource = mmdModelList.List.ToReadOnlyReactiveCollection(_ => new MmdModelRendererSource(_logger, _, Device))
                                         .AddTo(CompositeDisposables);
        }

        public Direct3D11.Device Device { get; } = new Direct3D11.Device(DriverType.Hardware,
                                                                         Direct3D11.DeviceCreationFlags.BgraSupport);

        public ReadOnlyReactiveCollection<MmdModelRendererSource> MmdModelSource { get; }
    }
}
