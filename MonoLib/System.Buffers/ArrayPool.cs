// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace System.Buffers
{
    public abstract class ArrayPool<T>
    {
        private readonly ConcurrentDictionary<int, ConcurrentBag<T[]>> _pool;

        public ArrayPool()
        {
            _pool = new ConcurrentDictionary<int, ConcurrentBag<T[]>>();
        }

        protected ArrayPool(int maxArrayLength, int maxArraysPerBucket)
        {
            if (maxArrayLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxArrayLength));
            }
            if (maxArraysPerBucket <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxArraysPerBucket));
            }
            _pool = new ConcurrentDictionary<int, ConcurrentBag<T[]>>(maxArrayLength, maxArraysPerBucket);
            for (var i = 1; i <= maxArraysPerBucket; i++)
            {
                _pool.TryAdd(i, new ConcurrentBag<T[]>());
            }
        }

        /// <summary>
        /// Creates a new <see cref="ArrayPool{T}"/> instance using custom configuration options.
        /// </summary>
        /// <param name="maxArrayLength">The maximum length of array instances that may be stored in the pool.</param>
        /// <param name="maxArraysPerBucket">
        /// The maximum number of array instances that may be stored in each bucket in the pool.  The pool
        /// groups arrays of similar lengths into buckets for faster access.
        /// </param>
        /// <returns>A new <see cref="ArrayPool{T}"/> instance with the specified configuration options.</returns>
        /// <remarks>
        /// The created pool will group arrays into buckets, with no more than <paramref name="maxArraysPerBucket"/>
        /// in each bucket and with those arrays not exceeding <paramref name="maxArrayLength"/> in length.
        /// </remarks>
        public static ArrayPool<T> Create(int maxArrayLength, int maxArraysPerBucket) =>
            new ConfigurableArrayPool<T>(maxArrayLength, maxArraysPerBucket);

        public T[] Rent(int capacity)
        {
            if (capacity < 1)
            {
                return null;
            }
            if (_pool.ContainsKey(capacity))
            {
                var subpool = _pool[capacity];
                T[] result;
                if (subpool != null) return subpool.TryTake(out result) ? result : new T[capacity];
                subpool = new ConcurrentBag<T[]>();
                _pool.TryAdd(capacity, subpool);
                _pool[capacity] = subpool;
                return subpool.TryTake(out result) ? result : new T[capacity];
            }
            _pool[capacity] = new ConcurrentBag<T[]>();
            return new T[capacity];
        }

        public void Return(T[] array)
        {
            if (array == null || array.Length < 1)
            {
                return;
            }
            var len = array.Length;
            Array.Clear(array, 0, len);
            var subpool = _pool[len] ?? new ConcurrentBag<T[]>();
            subpool.Add(array);
        }

    }
}
