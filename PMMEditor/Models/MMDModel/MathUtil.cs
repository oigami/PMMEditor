using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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

        bool CreateXYZ(Matrix4x4 rot)
        {
            float tmp = rot.M13;
            if (IsGimballock(tmp))
            {
                return false;
            }

            Y = (float) -Math.Asin(tmp);
            X = (float) Math.Atan2(rot.M23, (rot.M33));
            Z = (float) Math.Atan2(rot.M12, (rot.M11));
            _type = Type.XYZ;
            return true;
        }

        bool CreateYZX(Matrix4x4 rot)
        {
            float tmp = rot.M21;
            if (IsGimballock(tmp))
            {
                return false;
            }

            Z = (float) -Math.Asin(tmp);
            X = (float) Math.Atan2(rot.M23, rot.M22);
            Y = (float) Math.Atan2(rot.M31, rot.M11);
            _type = Type.YZX;
            return true;
        }

        bool CreateZXY(Matrix4x4 rot)
        {
            float tmp = rot.M32;
            if (IsGimballock(tmp))
            {
                return false;
            }

            X = (float) -Math.Asin(tmp);
            Y = (float) Math.Atan2(rot.M31, rot.M33);
            Z = (float) Math.Atan2(rot.M12, rot.M22);
            _type = Type.ZXY;
            return true;
        }

        Matrix4x4 CreateX()
        {
            return Matrix4x4.CreateRotationX(X);
        }

        Matrix4x4 CreateY()
        {
            return Matrix4x4.CreateRotationY(Y);
        }

        Matrix4x4 CreateZ()
        {
            return Matrix4x4.CreateRotationZ(Z);
        }

        private Type _type;

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }


        public EulerAngles(Matrix4x4 rot) : this()
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

        public Matrix4x4 CreateMatrix()
        {
            switch (_type)
            {
                case Type.XYZ:
                    return CreateX() * CreateY() * CreateZ();
                case Type.ZXY:
                    return CreateZ() * CreateX() * CreateY();
                case Type.YZX:
                    return CreateY() * CreateZ() * CreateX();
            }

            Debug.Assert(false);
            return Matrix4x4.Identity;
        }
    }

    public struct Int3
    {
        public int X, Y, Z;

        public Int3(int nowX = 0, int nowY = 0, int nowZ = 0)
        {
            X = nowX;
            Y = nowY;
            Z = nowZ;
        }
    }

    public struct Int4
    {
        public int X, Y, Z, W;

        public Int4(int nowX = 0, int nowY = 0, int nowZ = 0, int nowW = 0)
        {
            X = nowX;
            Y = nowY;
            Z = nowZ;
            W = nowW;
        }
    }

    public static class MathUtil
    {
        public static float DegreeToRadian(float angle)
        {
            return (float) Math.PI * angle / 180.0f;
        }

        public static Vector3 DegreeToRadian(Vector3 vec)
        {
            return new Vector3(DegreeToRadian(vec.X),
                               DegreeToRadian(vec.Y),
                               DegreeToRadian(vec.Z));
        }

        /// <summary>
        /// translationを行列に変換し、matとの積行列を返します。
        /// <para>
        /// returns: Matrix4x4.CreateTranslation(translation) * mat
        /// </para>
        /// </summary>
        /// <param name="translation"></param>
        /// <param name="mat"></param>
        /// <returns> Matrix4x4.CreateTranslation(translation) * mat </returns>
        public static Matrix4x4 Mul(this Vector3 translation, Matrix4x4 mat)
        {
            var tmp = new Vector3
            {
                X = Vector3.Dot(translation, new Vector3(mat.M11, mat.M21, mat.M31)),
                Y = Vector3.Dot(translation, new Vector3(mat.M12, mat.M22, mat.M32)),
                Z = Vector3.Dot(translation, new Vector3(mat.M13, mat.M23, mat.M33))
            };
            mat.Translation += tmp;
            return mat;
        }
    }

}
