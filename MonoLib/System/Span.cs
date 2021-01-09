using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace System
{
    public struct Span<T> : IEnumerable<T> where T : struct
    {
        private T[] _data;
        private int OffSet;
        private int len;
        public Span(T[] data)
        {
            _data = data;
            OffSet = 0;
            len = data.Length;
        }

        public Span(T[] data, int offSet)
        {
            _data = data;
            OffSet = offSet;
            len = data.Length - offSet;
        }

        public Span(T[] data, int offSet, int length)
        {
            _data = data;
            OffSet = offSet;
            len = length;
        }

        public Span(byte[] data)
        {
            _data = data.Cast<T>().ToArray();
            OffSet = 0;
            len = data.Length;
        }

        public unsafe Span(byte* b, int arrayLen)
        {
            unsafe
            {
                var bytes = new byte[arrayLen];
                unsafe
                {
                    byte* srcPtr = b;
                    fixed (byte* bPtr = bytes)
                    {
                        var j = 0;
                        while (j++ < arrayLen)
                        {
                            *(bPtr + j) = *(srcPtr++);
                        }
                    }
                }
                _data = bytes.Cast<T>().ToArray();
            }
            OffSet = 0;
            len = 0;
        }

        public int Length
        {
            get
            {
                return OffSet - len;
            }
        }

        public Span<T> Slice(int offset)
        {
            return new Span<T>(_data, offset + this.OffSet);
        }

        public Span<T> Slice(int offset, int len)
        {
            return new Span<T>(_data, offset + this.OffSet, len);
        }

        public int IndexOf(T nullTerminator)
        {
            int cur = 0;
            for (var i=0; i < Length; i++)
            {
                var current = this[i];
                if (current.Equals(nullTerminator))
                {
                    return cur;
                }
                cur++;
            }
            return -1;
        }

        public int IndexOf(Span<T> nullTerminator)
        {
            int cur = 0;
            for (var i = 0; i < Length; i++)
            {
                var parts = nullTerminator.ToArray();
                int numFound = 0;
                for (int x = 0; x < parts.Length; x++)
                {
                    var current = this[i + x];
                    if (current.Equals(parts[x]))
                    {
                        numFound++;
                    }
                }
                if (numFound == parts.Length)
                {
                    return cur;
                }
                cur++;
            }
            return -1;
        }

        public T[] ToArray()
        {
            return _data.Skip(OffSet).Take(len).ToArray();
        }

        public T this[int i]
        {
            get
            {
                return _data[i + OffSet];
            }
            set
            {
                _data[i + OffSet] = value;
            }
        }

        public void CopyTo(Span<T> span)
        {
            for (int i = OffSet; i < Length; i++)
            {
                span[i] = this[i];
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            foreach(var item in _data)
            {
                yield return item;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        //public static implicit operator byte[](Span<T> d)
        //{
        //    return d.Cast<byte>().ToArray();
        //}
        //public static explicit operator Span<byte[]>(byte[] b) => new Span<byte[]>(b);
    }

    public static class SpanExtensions
    {
        public static void Read(this Stream stream, Span<byte> span)
        {
            for (var i = 0; i < span.Length; i++)
            {
                var b = (byte)stream.ReadByte();
                span[i] = b;
            }
        }
    }
}
