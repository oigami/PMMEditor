using Livet;
using PMMEditor.Models;
using Direct3D11 = SharpDX.Direct3D11;

namespace PMMEditor.ViewModels.MMW
{
    public class MmdModelViewModel : ViewModel
    {
        private MmdModelModel _model;
        private Direct3D11.Device _device;

        private Direct3D11.Buffer _verteBuffer;

        public MmdModelViewModel(Direct3D11.Device device, MmdModelModel model)
        {
            _model = model;
            _device = device;
        }

        public void Initialize() {}
    }
}
