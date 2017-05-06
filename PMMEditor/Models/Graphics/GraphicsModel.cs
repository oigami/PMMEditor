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
        }

        public static Direct3D11.Device Device { get; } = new Direct3D11.Device(DriverType.Hardware,
                                                                                Direct3D11.DeviceCreationFlags
                                                                                          .BgraSupport);

        public static object SyncObject { get; } = new object();

        private static (bool, bool)? _featureThreadingSuppert { get; set; }

        public static (bool supportsConcurrentResources, bool supportsCommandLists) FeatureThreading
        {
            get
            {
                if (_featureThreadingSuppert != null)
                {
                    return ((bool, bool)) _featureThreadingSuppert;
                }

                Device.CheckThreadingSupport(out bool a, out bool b);
                _featureThreadingSuppert = (a, b);
                return ((bool, bool)) _featureThreadingSuppert;
            }
        }

    }
}
