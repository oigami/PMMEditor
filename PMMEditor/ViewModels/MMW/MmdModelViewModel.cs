using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using PMMEditor.Models;
using Direct3D11= SharpDX.Direct3D11;

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

        public void Initialize()
        {
        }
    }
}
