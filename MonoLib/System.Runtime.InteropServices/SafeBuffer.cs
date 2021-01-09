namespace System.Runtime.InteropServices
{
    using System;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using Microsoft.Win32.SafeHandles;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using SharedMemory;

    [System.Security.SecurityCritical]
    public abstract unsafe class SafeBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Steal UIntPtr.MaxValue as our uninitialized value.
        private static readonly UIntPtr Uninitialized = (UIntPtr.Size == 4) ?
            ((UIntPtr)UInt32.MaxValue) : ((UIntPtr)UInt64.MaxValue);

        private UIntPtr _numBytes;

        protected SafeBuffer(bool ownsHandle) : base(ownsHandle)
        {
            _numBytes = Uninitialized;
        }

        /// <summary>
        /// Specifies the size of the region of memory, in bytes.  Must be 
        /// called before using the SafeBuffer.
        /// </summary>
        /// <param name="numBytes">Number of valid bytes in memory.</param>
        [CLSCompliant(false)]
        public void Initialize(ulong numBytes)
        {
            if (numBytes < 0)
                throw new ArgumentOutOfRangeException("numBytes", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (IntPtr.Size == 4 && numBytes > UInt32.MaxValue)
                throw new ArgumentOutOfRangeException("numBytes", EnvironmentEx.GetResourceString("ArgumentOutOfRange_AddressSpace"));
            Contract.EndContractBlock();

            if (numBytes >= (ulong)Uninitialized)
                throw new ArgumentOutOfRangeException("numBytes", EnvironmentEx.GetResourceString("ArgumentOutOfRange_UIntPtrMax-1"));

            _numBytes = (UIntPtr)numBytes;
        }

        /// <summary>
        /// Specifies the the size of the region in memory, as the number of 
        /// elements in an array.  Must be called before using the SafeBuffer.
        /// </summary>
        [CLSCompliant(false)]
        public void Initialize(uint numElements, uint sizeOfEachElement)
        {
            if (numElements < 0)
                throw new ArgumentOutOfRangeException("numElements", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (sizeOfEachElement < 0)
                throw new ArgumentOutOfRangeException("sizeOfEachElement", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

            if (IntPtr.Size == 4 && numElements * sizeOfEachElement > UInt32.MaxValue)
                throw new ArgumentOutOfRangeException("numBytes", EnvironmentEx.GetResourceString("ArgumentOutOfRange_AddressSpace"));
            Contract.EndContractBlock();

            if (numElements * sizeOfEachElement >= (ulong)Uninitialized)
                throw new ArgumentOutOfRangeException("numElements", EnvironmentEx.GetResourceString("ArgumentOutOfRange_UIntPtrMax-1"));

            _numBytes = checked((UIntPtr)(numElements * sizeOfEachElement));
        }

        /// <summary>
        /// Specifies the the size of the region in memory, as the number of 
        /// elements in an array.  Must be called before using the SafeBuffer.
        /// </summary>
        [CLSCompliant(false)]
        public void Initialize<T>(uint numElements) where T : struct
        {
            Initialize(numElements, AlignedSizeOf<T>());
        }

        internal static uint AlignedSizeOf<T>() where T : struct
        {
            uint size = SizeOfType(typeof(T));
            if (size == 1 || size == 2)
            {
                return size;
            }
            if (IntPtr.Size == 8 && size == 4)
            {
                return size;
            }
            return AlignedSizeOfType(typeof(T));
        }

        // Type must be a value type with no object reference fields.  We only
        // assert this, due to the lack of a suitable generic constraint.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern uint AlignedSizeOfType(Type type);

        // Type must be a value type with no object reference fields.  We only
        // assert this, due to the lack of a suitable generic constraint.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern uint SizeOfType(Type type);

        // Callers should ensure that they check whether the pointer ref param
        // is null when AcquirePointer returns.  If it is not null, they must 
        // call ReleasePointer in a CER.  This method calls DangerousAddRef 
        // & exposes the pointer. Unlike Read, it does not alter the "current 
        // position" of the pointer.  Here's how to use it:
        //
        // byte* pointer = null;
        // RuntimeHelpers.PrepareConstrainedRegions();
        // try {
        //     safeBuffer.AcquirePointer(ref pointer);
        //     // Use pointer here, with your own bounds checking
        // }
        // finally {
        //     if (pointer != null)
        //         safeBuffer.ReleasePointer();
        // }
        //
        // Note: If you cast this byte* to a T*, you have to worry about 
        // whether your pointer is aligned.  Additionally, you must take
        // responsibility for all bounds checking with this pointer.
        /// <summary>
        /// Obtain the pointer from a SafeBuffer for a block of code,
        /// with the express responsibility for bounds checking and calling 
        /// ReleasePointer later within a CER to ensure the pointer can be 
        /// freed later.  This method either completes successfully or 
        /// throws an exception and returns with pointer set to null.
        /// </summary>
        /// <param name="pointer">A byte*, passed by reference, to receive
        /// the pointer from within the SafeBuffer.  You must set
        /// pointer to null before calling this method.</param>
        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void AcquirePointer(ref byte* pointer)
        {
            if (_numBytes == Uninitialized)
                throw NotInitialized();

            pointer = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                bool junk = false;
                DangerousAddRef(ref junk);
                pointer = (byte*)handle;
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public void ReleasePointer()
        {
            if (_numBytes == Uninitialized)
                throw NotInitialized();

            DangerousRelease();
        }
#if !FEATURE_CORECLR || FEATURE_CORESYSTEM
        /// <summary>
        /// Read a value type from memory at the given offset.  This is
        /// equivalent to:  return *(T*)(bytePtr + byteOffset);
        /// </summary>
        /// <typeparam name="T">The value type to read</typeparam>
        /// <param name="byteOffset">Where to start reading from memory.  You 
        /// may have to consider alignment.</param>
        /// <returns>An instance of T read from memory.</returns>
        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public T Read<T>(ulong byteOffset) where T : struct {
            if (_numBytes == Uninitialized)
                throw NotInitialized();
 
            uint sizeofT = Marshal.SizeOfType(typeof(T));
            byte* ptr = (byte*)handle + byteOffset;
            SpaceCheck(ptr, sizeofT);
 
            // return *(T*) (_ptr + byteOffset);
            T value;
            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);
 
                GenericPtrToStructure<T>(ptr, out value, sizeofT);
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }
            return value;
        }
 
        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void ReadArray<T>(ulong byteOffset, T[] array, int index, int count)
            where T : struct
        {
            if (array == null)
                throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
 
            if (_numBytes == Uninitialized)
                throw NotInitialized();
 
            uint sizeofT = Marshal.SizeOfType(typeof(T));
            uint alignedSizeofT = Marshal.AlignedSizeOf<T>();
            byte* ptr = (byte*)handle + byteOffset;
            SpaceCheck(ptr, checked((ulong)(alignedSizeofT * count)));
 
            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);
 
                for (int i = 0; i < count; i++)
                    unsafe { GenericPtrToStructure<T>(ptr + alignedSizeofT * i, out array[i + index], sizeofT); }
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }
        }
 
        /// <summary>
        /// Write a value type to memory at the given offset.  This is
        /// equivalent to:  *(T*)(bytePtr + byteOffset) = value;
        /// </summary>
        /// <typeparam name="T">The type of the value type to write to memory.</typeparam>
        /// <param name="byteOffset">The location in memory to write to.  You 
        /// may have to consider alignment.</param>
        /// <param name="value">The value type to write to memory.</param>
        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void Write<T>(ulong byteOffset, T value) where T : struct {
            if (_numBytes == Uninitialized)
                throw NotInitialized();
 
            uint sizeofT = Marshal.SizeOfType(typeof(T));
            byte* ptr = (byte*)handle + byteOffset;
            SpaceCheck(ptr, sizeofT);
 
            // *((T*) (_ptr + byteOffset)) = value;
            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);
                GenericStructureToPtr(ref value, ptr, sizeofT);
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }
        }
 
        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void WriteArray<T>(ulong byteOffset, T[] array, int index, int count)
            where T : struct
        {
            if (array == null)
                throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
 
            if (_numBytes == Uninitialized)
                throw NotInitialized();
 
            uint sizeofT = Marshal.SizeOfType(typeof(T));
            uint alignedSizeofT = Marshal.AlignedSizeOf<T>();
            byte* ptr = (byte*)handle + byteOffset;
            SpaceCheck(ptr, checked((ulong)(alignedSizeofT * count)));
            
            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);
                for (int i = 0; i < count; i++)
                    unsafe { GenericStructureToPtr(ref array[i + index], ptr + alignedSizeofT * i, sizeofT); }
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }
        }
#endif // !FEATURE_CORECLR || FEATURE_CORESYSTEM


        /// <summary>
        /// Returns the number of bytes in the memory region.
        /// </summary>
        [CLSCompliant(false)]
        public ulong ByteLength
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                if (_numBytes == Uninitialized)
                    throw NotInitialized();

                return (ulong)_numBytes;
            }
        }

        /* No indexer.  The perf would be misleadingly bad.  People should use 
         * AcquirePointer and ReleasePointer instead.  */

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private void SpaceCheck(byte* ptr, ulong sizeInBytes)
        {
            if ((ulong)_numBytes < sizeInBytes)
                NotEnoughRoom();
            if ((ulong)(ptr - (byte*)handle) > ((ulong)_numBytes) - sizeInBytes)
                NotEnoughRoom();
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static void NotEnoughRoom()
        {
            throw new ArgumentException(EnvironmentEx.GetResourceString("Arg_BufferTooSmall"));
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static InvalidOperationException NotInitialized()
        {
            Contract.Assert(false, "Uninitialized SafeBuffer!  Someone needs to call Initialize before using this instance!");
            return new InvalidOperationException(EnvironmentEx.GetResourceString("InvalidOperation_MustCallInitialize"));
        }

        // FCALL limitations mean we can't have generic FCALL methods.  However, we can pass 
        // TypedReferences to FCALL methods.
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static void GenericPtrToStructure<T>(byte* ptr, out T structure, uint sizeofT) where T : struct
        {
            structure = default(T);  // Dummy assignment to silence the compiler
            PtrToStructureNative(ptr, __makeref(structure), sizeofT);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern void PtrToStructureNative(byte* ptr, /*out T*/ TypedReference structure, uint sizeofT);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static void GenericStructureToPtr<T>(ref T structure, byte* ptr, uint sizeofT) where T : struct
        {
            StructureToPtrNative(__makeref(structure), ptr, sizeofT);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern void StructureToPtrNative(/*ref T*/ TypedReference structure, byte* ptr, uint sizeofT);

        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void WriteArray<T>(ulong byteOffset, T[] array, int index, int count)
                    where T : struct
        {
            if (array == null)
                throw new ArgumentNullException("array", EnvironmentEx.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - index < count)
                throw new ArgumentException(EnvironmentEx.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            if (_numBytes == Uninitialized)
                throw NotInitialized();

            uint sizeofT = MarshalEx.SizeOfType(typeof(T));
            uint alignedSizeofT = MarshalEx.AlignedSizeOf<T>();
            byte* ptr = (byte*)handle + byteOffset;
            SpaceCheck(ptr, checked((ulong)(alignedSizeofT * count)));

            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);
                for (int i = 0; i < count; i++)
                    unsafe { GenericStructureToPtr(ref array[i + index], ptr + alignedSizeofT * i, sizeofT); }
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }
        }

        /// <summary>
        /// Write a value type to memory at the given offset.  This is
        /// equivalent to:  *(T*)(bytePtr + byteOffset) = value;
        /// </summary>
        /// <typeparam name="T">The type of the value type to write to memory.</typeparam>
        /// <param name="byteOffset">The location in memory to write to.  You 
        /// may have to consider alignment.</param>
        /// <param name="value">The value type to write to memory.</param>
        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void Write<T>(ulong byteOffset, T value) where T : struct
        {
            if (_numBytes == Uninitialized)
                throw NotInitialized();

            uint sizeofT = MarshalEx.SizeOfType(typeof(T));
            byte* ptr = (byte*)handle + byteOffset;
            SpaceCheck(ptr, sizeofT);

            // *((T*) (_ptr + byteOffset)) = value;
            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);
                GenericStructureToPtr(ref value, ptr, sizeofT);
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }
        }

        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void ReadArray<T>(ulong byteOffset, T[] array, int index, int count)
                    where T : struct
        {
            if (array == null)
                throw new ArgumentNullException("array", EnvironmentEx.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - index < count)
                throw new ArgumentException(EnvironmentEx.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            if (_numBytes == Uninitialized)
                throw NotInitialized();

            uint sizeofT = MarshalEx.SizeOfType(typeof(T));
            uint alignedSizeofT = MarshalEx.AlignedSizeOf<T>();
            byte* ptr = (byte*)handle + byteOffset;
            SpaceCheck(ptr, checked((ulong)(alignedSizeofT * count)));

            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);

                for (int i = 0; i < count; i++)
                    unsafe { GenericPtrToStructure<T>(ptr + alignedSizeofT * i, out array[i + index], sizeofT); }
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }
        }

        /// <summary>
        /// Read a value type from memory at the given offset.  This is
        /// equivalent to:  return *(T*)(bytePtr + byteOffset);
        /// </summary>
        /// <typeparam name="T">The value type to read</typeparam>
        /// <param name="byteOffset">Where to start reading from memory.  You 
        /// may have to consider alignment.</param>
        /// <returns>An instance of T read from memory.</returns>
        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public T Read<T>(ulong byteOffset) where T : struct
        {
            if (_numBytes == Uninitialized)
                throw NotInitialized();

            uint sizeofT = MarshalEx.SizeOfType(typeof(T));
            byte* ptr = (byte*)handle + byteOffset;
            SpaceCheck(ptr, sizeofT);

            // return *(T*) (_ptr + byteOffset);
            T value;
            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);

                GenericPtrToStructure<T>(ptr, out value, sizeofT);
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }
            return value;
        }
    }
}
