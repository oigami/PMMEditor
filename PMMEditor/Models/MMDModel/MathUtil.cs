using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace PMMEditor.Models.MMDModel
{
    public struct EulerAngles
    {
        private enum Type
        {
            XYZ,
            YZX,
            ZXY
        }

        static bool IsGimballock(float val)
        {
            float eps = 1.0e-4f;
            if (val < -1 + eps || 1 - eps < val)
            {
                return true;
            }
            return false;
        }

        bool CreateXYZ(Matrix rot)
        {
            float tmp = rot.M13;
            if (IsGimballock(tmp))
            {
                return false;
            }

            Y = (float) -Math.Asin(tmp);
            X = (float) Math.Atan2(rot.M23, (rot.M33));
            Z = (float) Math.Atan2(rot.M12, (rot.M11));
            type = Type.XYZ;
            return true;
        }

        bool CreateYZX(Matrix rot)
        {
            float tmp = rot.M21;
            if (IsGimballock(tmp))
            {
                return false;
            }

            Z = (float) -Math.Asin(tmp);
            X = (float) Math.Atan2(rot.M23, rot.M22);
            Y = (float) Math.Atan2(rot.M31, rot.M11);
            type = Type.YZX;
            return true;
        }

        bool CreateZXY(Matrix rot)
        {
            float tmp = rot.M32;
            if (IsGimballock(tmp))
            {
                return false;
            }

            X = (float) -Math.Asin(tmp);
            Y = (float) Math.Atan2(rot.M31, rot.M33);
            Z = (float) Math.Atan2(rot.M12, rot.M22);
            type = Type.ZXY;
            return true;
        }

        Matrix CreateX()
        {
            return Matrix.RotationX(X);
        }

        Matrix CreateY()
        {
            return Matrix.RotationY(Y);
        }

        Matrix CreateZ()
        {
            return Matrix.RotationZ(Z);
        }

        Type type;

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }


        public EulerAngles(Matrix rot) : this()
        {
            if (!CreateXYZ(rot))
            {
                if (!CreateYZX(rot))
                {
                    //if ( !CreateZXY(rot) ) // x が90度以内に制限されると膝が上がらなくなってしまう
                    //{
                        throw new ArgumentException(nameof(rot));
                    //}
                }
            }
        }

        public Matrix CreateMatrix()
        {
            switch (type)
            {
                case Type.XYZ:
                    return CreateX() * CreateY() * CreateZ();
                case Type.ZXY:
                    return CreateZ() * CreateX() * CreateY();
                case Type.YZX:
                    return CreateY() * CreateZ() * CreateX();
            }
            Debug.Assert(false);
            return Matrix.Identity;
        }
    }

    public static class MathUtil
    {
        public static float Radians(float degree)
        {
            return degree / 180.0f * (float) Math.PI;
        }
    }
}
