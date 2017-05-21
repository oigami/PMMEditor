using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PMMEditor.ECS
{
    public class Camera
    {
        private Matrix4x4 _projection = Matrix4x4.CreatePerspectiveFieldOfView((float) Math.PI / 3, 1.4f, 1f, 100000f);

        public static Camera Main { get; set; } = new Camera();

        public Matrix4x4 Projection
        {
            get => _projection;
            set => _projection = value;
        }

        // TODO: あとでデータを分ける
        public Matrix4x4 View { get; set; }

        static Camera()
        {
            Main._projection.M33 *= -1;
            Main._projection.M34 *= -1;

            Matrix4x4 w = Matrix4x4.CreateTranslation(-new Vector3(0, 10, 0));
            Main.View = w * Matrix4x4.CreateTranslation(0, 0, 45);
        }


    }
}
