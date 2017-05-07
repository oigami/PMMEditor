using SharpDX.DXGI;
using Direct3D11 = SharpDX.Direct3D11;

namespace PMMEditor.ECS
{
    public struct IndexBufferBinding
    {
        public IndexBufferBinding(Direct3D11.Buffer buffer, Format format, int offset)
        {
            Buffer = buffer;
            Format = format;
            Offset = offset;
        }

        public Direct3D11.Buffer Buffer { get; set; }
        public SharpDX.DXGI.Format Format { get; set; }
        public int Offset { get; set; }
    }

    public class MeshFilter : Component
    {
        public Direct3D11.VertexBufferBinding Vertices { get; set; }

        public IndexBufferBinding Indices { get; set; }
    }
}
