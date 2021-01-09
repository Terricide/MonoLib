using System;
namespace System.Buffers
{

    internal sealed partial class ConfigurableArrayPool<T> : ArrayPool<T>
    {
        /// <summary>The default maximum length of each array in the pool (2^20).</summary>
        private const int DefaultMaxArrayLength = 1024 * 1024;
        /// <summary>The default maximum number of arrays per bucket that are available for rent.</summary>
        private const int DefaultMaxNumberOfArraysPerBucket = 50;

        internal ConfigurableArrayPool() : this(DefaultMaxArrayLength, DefaultMaxNumberOfArraysPerBucket)
        {
        }

        internal ConfigurableArrayPool(int maxArrayLength, int maxArraysPerBucket) : base(maxArrayLength, maxArraysPerBucket)
        {


            // Our bucketing algorithm has a min length of 2^4 and a max length of 2^30.
            // Constrain the actual max used to those values.
            const int MinimumArrayLength = 0x10, MaximumArrayLength = 0x40000000;
            if (maxArrayLength > MaximumArrayLength)
            {
                maxArrayLength = MaximumArrayLength;
            }
            else if (maxArrayLength < MinimumArrayLength)
            {
                maxArrayLength = MinimumArrayLength;
            }
        }

        /// <summary>Gets an ID for the pool to use with events.</summary>
        private int Id => GetHashCode();
    }
}
