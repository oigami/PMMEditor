using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace PMMEditor.Models
{
    public class CameraControlModel
    {
        public float Distance { get; set; } = 45;

        public Vector2 Rotate { get; set; } = new Vector2(0, 0);

        public Vector3 LookAt { get; set; } = new Vector3(0, 10, 0);

        public Matrix Perspective { get; set; } = Matrix.PerspectiveFovLH((float) Math.PI / 3, 1.4f, 1f, 10000000f);

        public Matrix CreateProjection()
        {
            return Perspective;
        }

        public Matrix CreateView()
        {
            return Matrix.Translation(0, 0, Distance);
        }

        public Matrix CreateWorld()
        {
            var w = Matrix.Translation(LookAt);
            w *= Matrix.RotationY(Rotate.Y) * Matrix.RotationX(Rotate.X);

            return Matrix.Invert(w);

        }

        public Matrix CreateWorldViewProj()
        {
            return CreateWorld() * CreateView() * CreateProjection();
        }
    }
}
