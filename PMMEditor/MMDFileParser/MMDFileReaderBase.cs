using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace PMMEditor.MMDFileParser
{
    internal abstract class MMDFileReaderBase
    {
        protected byte[] _buffer;
        protected Stream _stream;

        #region PrimitiveTypeRead

        protected byte[] ReadByte(int size)
        {
            var len = _stream.Read(_buffer, 0, size);
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
            for (var i = 0; i < size; i++)
            {
                t[i] = func();
            }
            return t;
        }

        protected T[] ReadVArray<T>(Func<T> func)
        {
            var size = ReadInt();
            return ReadArray(size, func);
        }

        protected List<T> ReadList<T>(int size, Func<T> func)
        {
            var t = new List<T>(size);
            for (var i = 0; i < size; i++)
            {
                t.Add(func());
            }
            return t;
        }

        protected List<T> ReadVList<T>(Func<T> func)
        {
            var size = ReadInt();
            return ReadList(size, func);
        }

        #endregion ArrayTypeRead

        #region StringTypeRead

        protected string ReadVString()
        {
            return ReadFixedString(ReadByte());
        }

        protected string ReadFixedString(int count)
        {
            return Encoding.GetEncoding("shift_jis").GetString(ReadByte(count), 0, count);
        }

        protected string ReadFixedStringTerminationChar(int count)
        {
            return string.Concat(ReadFixedString(count).TakeWhile(s => s != '\0'));
        }

        #endregion StringTypeRead

        protected Vector2 ReadVector2()
        {
            return new Vector2(ReadFloat(), ReadFloat());
        }

        protected Vector3 ReadVector3()
        {
            return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }
    }
}
