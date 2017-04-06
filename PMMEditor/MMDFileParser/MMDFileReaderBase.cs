using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace PMMEditor.MMDFileParser
{
    public struct Color
    {
        public float R { get; set; }

        public float G { get; set; }

        public float B { get; set; }
    }

    public struct ColorA
    {
        public float R { get; set; }

        public float G { get; set; }

        public float B { get; set; }

        public float A { get; set; }
    }

    internal abstract class MMDFileReaderBase
    {
        protected byte[] _buffer;
        protected Stream _stream;

        public Encoding Encoding { get; set; }

        public MMDFileReaderBase(Stream stream, byte[] tmpBuffer = null, Encoding encoding = null)
        {
            _stream = stream;
            _buffer = tmpBuffer ?? new byte[1024];
            Encoding = encoding ?? Encoding.GetEncoding("Shift_jis");
        }

        #region PrimitiveTypeRead

        protected byte[] ReadByte(int size)
        {
            if (_buffer.Length < size)
            {
                _buffer = new byte[Math.Max(size, _buffer.Length * 2)];
            }
            int len = _stream.Read(_buffer, 0, size);
            if (len != size)
            {
                throw new ArgumentException("");
            }

            return _buffer;
        }

        protected byte ReadByte()
        {
            return ReadByte(1)[0];
        }

        protected sbyte ReadSByte()
        {
            return (sbyte) ReadByte(1)[0];
        }

        protected short ReadInt16()
        {
            return BitConverter.ToInt16(ReadByte(2), 0);
        }

        protected ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(ReadByte(2), 0);
        }

        protected int ReadInt()
        {
            return BitConverter.ToInt32(ReadByte(4), 0);
        }

        protected uint ReadUInt()
        {
            return BitConverter.ToUInt32(ReadByte(4), 0);
        }

        protected float ReadFloat()
        {
            return BitConverter.ToSingle(ReadByte(4), 0);
        }

        protected bool ReadBool()
        {
            return ReadByte() != 0;
        }

        #endregion PrimitiveTypeRead

        #region ArrayTypeRead

        protected T[] ReadArray<T>(int size, Func<T> func)
        {
            var t = new T[size];
            for (int i = 0; i < size; i++)
            {
                t[i] = func();
            }

            return t;
        }

        protected T[] ReadVIntArray<T>(Func<T> func)
        {
            int size = ReadInt();
            return ReadArray(size, func);
        }

        protected List<T> ReadList<T>(int size, Func<T> func)
        {
            var t = new List<T>(size);
            for (int i = 0; i < size; i++)
            {
                t.Add(func());
            }

            return t;
        }

        protected List<T> ReadVIntList<T>(Func<T> func)
        {
            int size = ReadInt();
            return ReadList(size, func);
        }

        #endregion ArrayTypeRead

        #region StringTypeRead

        protected string ReadVByteString()
        {
            return ReadFixedString(ReadByte());
        }

        protected string ReadVIntString()
        {
            return ReadFixedString(ReadInt());
        }


        protected string ReadFixedString(int count)
        {
            return ReadFixedString(count, Encoding);
        }

        protected string ReadFixedString(int count, Encoding encoding)
        {
            return encoding.GetString(ReadByte(count), 0, count);
        }

        protected string ReadFixedStringTerminationChar(int count)
        {
            return string.Concat(ReadFixedString(count).TakeWhile(s => s != '\0'));
        }

        #endregion StringTypeRead

        #region VectorTypeRead

        protected Vector2 ReadVector2()
        {
            return new Vector2(ReadFloat(), ReadFloat());
        }

        protected Vector3 ReadVector3()
        {
            return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }

        protected Vector4 ReadVector4()
        {
            return new Vector4(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
        }

        #endregion

        protected long RemainingLength()
        {
            return _stream.Length - _stream.Position;
        }

        protected bool IsRemaining()
        {
            return RemainingLength() > 0;
        }
    }
}
