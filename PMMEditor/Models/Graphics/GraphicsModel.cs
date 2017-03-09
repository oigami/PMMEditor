using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.Views.Documents;
using Reactive.Bindings;
using SharpDX.Direct3D;
using Direct3D11 = SharpDX.Direct3D11;
namespace PMMEditor.Models.Graphics
{
    public class GraphicsModel
    {
        public GraphicsModel(MmdModelList mmdModelList)
        {
            MmdModelSource = mmdModelList.List.ToReadOnlyReactiveCollection(_ => new MmdModelRendererSource(_, Device));
        }

        public Direct3D11.Device Device { get; } = new Direct3D11.Device(DriverType.Hardware,
                                                                     Direct3D11.DeviceCreationFlags.BgraSupport);

        public ReadOnlyReactiveCollection<MmdModelRendererSource> MmdModelSource { get; set; }

    }
}
