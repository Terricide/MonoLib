using System.Linq;
using System.IO;
using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace System
{
    public struct Memory<T> where T : struct
    {
        // The highest order bit of _index is used to discern whether _object is a pre-pinned array.
        // (_index < 0) => _object is a pre-pinned array, so Pin() will not allocate a new GCHandle
        //       (else) => Pin() needs to allocate a new GCHandle to pin the object.
        private readonly T[] _object;
        private readonly int _index;
        private readonly int _length;
        private int OffSet;
        private int len;

        public Memory(T[] array)
        {
            if (array == null)
            {
                this = default;
                return; // returns default
            }
            //if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
            //    ThrowHelper.ThrowArrayTypeMismatchException();

            _object = array;
            _index = 0;
            _length = array.Length;
            OffSet = 0;
            len = 0;
        }
        public Memory(T[] array, int offSet)
        {
            if (array == null)
            {
                this = default;
                return; // returns default
            }
            //if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
            //    ThrowHelper.ThrowArrayTypeMismatchException();

            _object = array;
            _index = 0;
            _length = array.Length;
            OffSet = offSet;
            len = 0;
        }
        public Memory(T[] array, int offSet, int length)
        {
            if (array == null)
            {
                this = default;
                return; // returns default
            }
            //if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
            //    ThrowHelper.ThrowArrayTypeMismatchException();

            _object = array;
            _index = 0;
            _length = array.Length;
            OffSet = offSet;
            len = length;
        }
        public Span<T> Span
        {
            get
            {
                var span = new Span<T>(_object);
                return span;
            }
        }

        public T this[int index]
        {
            get
            {
                return _object[index + OffSet];
            }
            set
            {
                _object[index + OffSet] = value;
            }
        }

        public int Length
        {
            get
            {
                return _length - OffSet - len;
            }
        }

        /// <summary>
        /// Copies the contents from the memory into a new array.  This heap
        /// allocates, so should generally be avoided, however it is sometimes
        /// necessary to bridge the gap with APIs written in terms of arrays.
        /// </summary>
        public T[] ToArray() => Span.ToArray();

        /// <summary>
        /// Copies the contents of the memory into the destination. If the source
        /// and destination overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.
        ///
        /// <param name="destination">The Memory to copy items into.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when the destination is shorter than the source.
        /// </exception>
        /// </summary>
        public void CopyTo(Memory<T> destination) => Span.CopyTo(destination.Span);

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// Returns true if the object is Memory or ReadOnlyMemory and if both objects point to the same array and have the same length.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            if (obj is Memory<T> memory)
            {
                return Equals(memory);
            }
            else
            {
                return false;
            }
        }

        public Memory<T> Slice(int offset, int length)
        {
            var mem = new Memory<T>(_object, offset, length);
            return mem;
        }

        public Memory<T> Slice(int offset)
        {
            var mem = new Memory<T>(_object, offset);
            return mem;
        }
    }
}
