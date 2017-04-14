using System;
using SharpDX.Direct3D9;

namespace PMMEditor.SharpDxControl
{
    internal class D3D9Instance
    {
        public readonly Direct3DEx D3DContext;
        public readonly DeviceEx D3DDevice;

        public static D3D9Instance Instance { get; } = new D3D9Instance();

        public void CheckDeviceLost()
        {
            DeviceState d = D3DDevice.CheckDeviceState((IntPtr) null);
            if (d != DeviceState.Ok)
            {
                var presentParams = new PresentParameters
                {
                    Windowed = true,
                    SwapEffect = SwapEffect.Discard,
                    DeviceWindowHandle = NativeMethods.GetDesktopWindow(),
                    PresentationInterval = PresentInterval.Immediate
                };
                D3DDevice.ResetEx(ref presentParams);
            }
        }

        private D3D9Instance()
        {
            var presentParams = new PresentParameters
            {
                Windowed = true,
                SwapEffect = SwapEffect.Discard,
                DeviceWindowHandle = NativeMethods.GetDesktopWindow(),
                PresentationInterval = PresentInterval.Immediate
            };
            const CreateFlags createFlags =
                CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve;

            D3DContext = new Direct3DEx();
            D3DDevice = new DeviceEx(D3DContext, 0, DeviceType.Hardware, IntPtr.Zero, createFlags, presentParams);
        }
    }
}
