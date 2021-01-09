// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  UnmanagedMemoryStream
**
** <OWNER>Microsoft</OWNER>
**
** Purpose: Create a stream over unmanaged memory, mostly
**          useful for memory-mapped files.
**
** Date:  October 20, 2000 (made public August 4, 2003)
**
===========================================================*/
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Diagnostics.Contracts;
using Microsoft.Win32.SafeHandles;
using System.Runtime.Versioning;
#if !FEATURE_PAL && FEATURE_ASYNC_IO
using System.Threading.Tasks; 
#endif  // !FEATURE_PAL && FEATURE_ASYNC_IO 


namespace System.IO
{

    /*
     * This class is used to access a contiguous block of memory, likely outside 
     * the GC heap (or pinned in place in the GC heap, but a MemoryStream may 
     * make more sense in those cases).  It's great if you have a pointer and
     * a length for a section of memory mapped in by someone else and you don't
     * want to copy this into the GC heap.  UnmanagedMemoryStream assumes these 
     * two things:
     *
     * 1) All the memory in the specified block is readable or writable,
     *    depending on the values you pass to the constructor.
     * 2) The lifetime of the block of memory is at least as long as the lifetime
     *    of the UnmanagedMemoryStream.
     * 3) You clean up the memory when appropriate.  The UnmanagedMemoryStream 
     *    currently will do NOTHING to free this memory.
     * 4) All calls to Write and WriteByte may not be threadsafe currently.
     *
     * It may become necessary to add in some sort of 
     * DeallocationMode enum, specifying whether we unmap a section of memory, 
     * call free, run a user-provided delegate to free the memory, etc etc.  
     * We'll suggest user write a subclass of UnmanagedMemoryStream that uses
     * a SafeHandle subclass to hold onto the memory.
     * Check for problems when using this in the negative parts of a 
     * process's address space.  We may need to use unsigned longs internally
     * and change the overflow detection logic.
     * 
     * -----SECURITY MODEL AND SILVERLIGHT-----
     * A few key notes about exposing UMS in silverlight:
     * 1. No ctors are exposed to transparent code. This version of UMS only
     * supports byte* (not SafeBuffer). Therefore, framework code can create
     * a UMS and hand it to transparent code. Transparent code can use most
     * operations on a UMS, but not operations that directly expose a 
     * pointer.
     * 
     * 2. Scope of "unsafe" and non-CLS compliant operations reduced: The
     * Whidbey version of this class has CLSCompliant(false) at the class 
     * level and unsafe modifiers at the method level. These were reduced to 
     * only where the unsafe operation is performed -- i.e. immediately 
     * around the pointer manipulation. Note that this brings UMS in line 
     * with recent changes in pu/clr to support SafeBuffer.
     * 
     * 3. Currently, the only caller that creates a UMS is ResourceManager, 
     * which creates read-only UMSs, and therefore operations that can 
     * change the length will throw because write isn't supported. A 
     * conservative option would be to formalize the concept that _only_
     * read-only UMSs can be creates, and enforce this by making WriteX and
     * SetLength SecurityCritical. However, this is a violation of 
     * security inheritance rules, so we must keep these safe. The 
     * following notes make this acceptable for future use.
     *    a. a race condition in WriteX that could have allowed a thread to 
     *    read from unzeroed memory was fixed
     *    b. memory region cannot be expanded beyond _capacity; in other 
     *    words, a UMS creator is saying a writeable UMS is safe to 
     *    write to anywhere in the memory range up to _capacity, specified
     *    in the ctor. Even if the caller doesn't specify a capacity, then
     *    length is used as the capacity.
     */
#if !NET40Plus

    /// <summary>
    /// <para>Very limited .NET 3.5 implementation of a managed wrapper around memory-mapped files to reflect the .NET 4 API.</para>
    /// <para>Only those methods and features necessary for the SharedMemory library have been implemented.</para>
    /// </summary>
#if NETFULL
    [PermissionSet(SecurityAction.LinkDemand)]
#endif
    public class UnmanagedMemoryStreamEx : Stream
    {
        // 

        private const long UnmanagedMemStreamMaxLength = Int64.MaxValue;

        [System.Security.SecurityCritical] // auto-generated
        private SafeBuffer _buffer;
        [SecurityCritical]
        private unsafe byte* _mem;
        private long _length;
        private long _capacity;
        private long _position;
        private long _offset;
        private FileAccess _access;
        internal bool _isOpen;
#if !FEATURE_PAL && FEATURE_ASYNC_IO
        [NonSerialized] 
        private Task<Int32> _lastReadTask; // The last successful task returned from ReadAsync 
#endif  // FEATURE_PAL && FEATURE_ASYNC_IO 


        // Needed for subclasses that need to map a file, etc.
        [System.Security.SecuritySafeCritical]  // auto-generated
        protected UnmanagedMemoryStreamEx()
        {
            unsafe
            {
                _mem = null;
            }
            _isOpen = false;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public UnmanagedMemoryStreamEx(SafeBuffer buffer, long offset, long length)
        {
            Initialize(buffer, offset, length, FileAccess.Read, false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public UnmanagedMemoryStreamEx(SafeBuffer buffer, long offset, long length, FileAccess access)
        {
            Initialize(buffer, offset, length, access, false);
        }

        // We must create one of these without doing a security check.  This
        // class is created while security is trying to start up.  Plus, doing
        // a Demand from Assembly.GetManifestResourceStream isn't useful.
        [System.Security.SecurityCritical]  // auto-generated
        internal UnmanagedMemoryStreamEx(SafeBuffer buffer, long offset, long length, FileAccess access, bool skipSecurityCheck)
        {
            Initialize(buffer, offset, length, access, skipSecurityCheck);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected void Initialize(SafeBuffer buffer, long offset, long length, FileAccess access)
        {
            Initialize(buffer, offset, length, access, false);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void Initialize(SafeBuffer buffer, long offset, long length, FileAccess access, bool skipSecurityCheck)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (buffer.ByteLength < (ulong)(offset + length))
            {
                throw new ArgumentException(EnvironmentEx.GetResourceString("Argument_InvalidSafeBufferOffLen"));
            }
            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
            {
                throw new ArgumentOutOfRangeException("access");
            }
            Contract.EndContractBlock();

            if (_isOpen)
            {
                throw new InvalidOperationException(EnvironmentEx.GetResourceString("InvalidOperation_CalledTwice"));
            }
            if (!skipSecurityCheck)
            {
#pragma warning disable 618
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
#pragma warning restore 618
            }

            // check for wraparound
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    buffer.AcquirePointer(ref pointer);
                    if ((pointer + offset + length) < pointer)
                    {
                        throw new ArgumentException(EnvironmentEx.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamWrapAround"));
                    }
                }
                finally
                {
                    if (pointer != null)
                    {
                        buffer.ReleasePointer();
                    }
                }
            }

            _offset = offset;
            _buffer = buffer;
            _length = length;
            _capacity = length;
            _access = access;
            _isOpen = true;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public unsafe UnmanagedMemoryStreamEx(byte* pointer, long length)
        {
            Initialize(pointer, length, length, FileAccess.Read, false);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        public unsafe UnmanagedMemoryStreamEx(byte* pointer, long length, long capacity, FileAccess access)
        {
            Initialize(pointer, length, capacity, access, false);
        }

        // We must create one of these without doing a security check.  This
        // class is created while security is trying to start up.  Plus, doing
        // a Demand from Assembly.GetManifestResourceStream isn't useful.
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe UnmanagedMemoryStreamEx(byte* pointer, long length, long capacity, FileAccess access, bool skipSecurityCheck)
        {
            Initialize(pointer, length, capacity, access, skipSecurityCheck);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [CLSCompliant(false)]
        protected unsafe void Initialize(byte* pointer, long length, long capacity, FileAccess access)
        {
            Initialize(pointer, length, capacity, access, false);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe void Initialize(byte* pointer, long length, long capacity, FileAccess access, bool skipSecurityCheck)
        {
            if (pointer == null)
                throw new ArgumentNullException("pointer");
            if (length < 0 || capacity < 0)
                throw new ArgumentOutOfRangeException((length < 0) ? "length" : "capacity", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (length > capacity)
                throw new ArgumentOutOfRangeException("length", EnvironmentEx.GetResourceString("ArgumentOutOfRange_LengthGreaterThanCapacity"));
            Contract.EndContractBlock();
            // Check for wraparound.
            if (((byte*)((long)pointer + capacity)) < pointer)
                throw new ArgumentOutOfRangeException("capacity", EnvironmentEx.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamWrapAround"));
            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
                throw new ArgumentOutOfRangeException("access", EnvironmentEx.GetResourceString("ArgumentOutOfRange_Enum"));
            if (_isOpen)
                throw new InvalidOperationException(EnvironmentEx.GetResourceString("InvalidOperation_CalledTwice"));

            if (!skipSecurityCheck)
#pragma warning disable 618
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
#pragma warning restore 618

            _mem = pointer;
            _offset = 0;
            _length = length;
            _capacity = capacity;
            _access = access;
            _isOpen = true;
        }

        public override bool CanRead
        {
            [Pure]
            get { return _isOpen && (_access & FileAccess.Read) != 0; }
        }

        public override bool CanSeek
        {
            [Pure]
            get { return _isOpen; }
        }

        public override bool CanWrite
        {
            [Pure]
            get { return _isOpen && (_access & FileAccess.Write) != 0; }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override void Dispose(bool disposing)
        {
            _isOpen = false;
            unsafe { _mem = null; }

            // Stream allocates WaitHandles for async calls. So for correctness 
            // call base.Dispose(disposing) for better perf, avoiding waiting
            // for the finalizers to run on those types.
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            if (!_isOpen) __Error.StreamIsClosed();
        }

#if !FEATURE_PAL && FEATURE_ASYNC_IO
        [HostProtection(ExternalThreading=true)] 
        [ComVisible(false)] 
        public override Task FlushAsync(CancellationToken cancellationToken) { 
        
            if (cancellationToken.IsCancellationRequested) 
                return Task.FromCancellation(cancellationToken); 
 
            try { 
            
                Flush(); 
                return Task.CompletedTask; 
                
            } catch(Exception ex) { 
            
                return Task.FromException(ex); 
            } 
      } 
#endif  // !FEATURE_PAL && FEATURE_ASYNC_IO 


        public override long Length
        {
            get
            {
                if (!_isOpen) __Error.StreamIsClosed();
                return Interlocked.Read(ref _length);
            }
        }

        public long Capacity
        {
            get
            {
                if (!_isOpen) __Error.StreamIsClosed();
                return _capacity;
            }
        }

        public override long Position
        {
            get
            {
                if (!CanSeek) __Error.StreamIsClosed();
                Contract.EndContractBlock();
                return Interlocked.Read(ref _position);
            }
            [System.Security.SecuritySafeCritical]  // auto-generated
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                Contract.EndContractBlock();
                if (!CanSeek) __Error.StreamIsClosed();

#if WIN32
                unsafe {
                    // On 32 bit machines, ensure we don't wrap around.
                    if (value > (long) Int32.MaxValue || _mem + value < _mem)
                        throw new ArgumentOutOfRangeException("value", EnvironmentEx.GetResourceString("ArgumentOutOfRange_StreamLength"));
                }
#endif
                Interlocked.Exchange(ref _position, value);
            }
        }

        [CLSCompliant(false)]
        public unsafe byte* PositionPointer
        {
            [System.Security.SecurityCritical]  // auto-generated_required
            get
            {
                if (_buffer != null)
                {
                    throw new NotSupportedException(EnvironmentEx.GetResourceString("NotSupported_UmsSafeBuffer"));
                }

                // Use a temp to avoid a race
                long pos = Interlocked.Read(ref _position);
                if (pos > _capacity)
                    throw new IndexOutOfRangeException(EnvironmentEx.GetResourceString("IndexOutOfRange_UMSPosition"));
                byte* ptr = _mem + pos;
                if (!_isOpen) __Error.StreamIsClosed();
                return ptr;
            }
            [System.Security.SecurityCritical]  // auto-generated_required
            set
            {
                if (_buffer != null)
                    throw new NotSupportedException(EnvironmentEx.GetResourceString("NotSupported_UmsSafeBuffer"));
                if (!_isOpen) __Error.StreamIsClosed();

                // Note: subtracting pointers returns an Int64.  Working around
                // to avoid hitting compiler warning CS0652 on this line. 
                if (new IntPtr(value - _mem).ToInt64() > UnmanagedMemStreamMaxLength)
                    throw new ArgumentOutOfRangeException("offset", EnvironmentEx.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamLength"));
                if (value < _mem)
                    throw new IOException(EnvironmentEx.GetResourceString("IO.IO_SeekBeforeBegin"));

                Interlocked.Exchange(ref _position, value - _mem);
            }
        }

        internal unsafe byte* Pointer
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (_buffer != null)
                    throw new NotSupportedException(EnvironmentEx.GetResourceString("NotSupported_UmsSafeBuffer"));

                return _mem;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", EnvironmentEx.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(EnvironmentEx.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();  // Keep this in sync with contract validation in ReadAsync

            if (!_isOpen) __Error.StreamIsClosed();
            if (!CanRead) __Error.ReadNotSupported();

            // Use a local variable to avoid a race where another thread 
            // changes our position after we decide we can read some bytes.
            long pos = Interlocked.Read(ref _position);
            long len = Interlocked.Read(ref _length);
            long n = len - pos;
            if (n > count)
                n = count;
            if (n <= 0)
                return 0;

            int nInt = (int)n; // Safe because n <= count, which is an Int32
            if (nInt < 0)
                nInt = 0;  // _position could be beyond EOF
            Contract.Assert(pos + nInt >= 0, "_position + n >= 0");  // len is less than 2^63 -1.

            if (_buffer != null)
            {
                unsafe
                {
                    byte* pointer = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        _buffer.AcquirePointer(ref pointer);
                        BufferEx.Memcpy(buffer, offset, pointer + pos + _offset, 0, nInt);
                    }
                    finally
                    {
                        if (pointer != null)
                        {
                            _buffer.ReleasePointer();
                        }
                    }
                }
            }
            else
            {
                unsafe
                {
                    BufferEx.Memcpy(buffer, offset, _mem + pos, 0, nInt);
                }
            }
            Interlocked.Exchange(ref _position, pos + n);
            return nInt;
        }

#if !FEATURE_PAL && FEATURE_ASYNC_IO
        [HostProtection(ExternalThreading = true)]
        [ComVisible(false)]
        public override Task<Int32> ReadAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken) {        
            if (buffer==null)
                throw new ArgumentNullException("buffer", EnvironmentEx.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(EnvironmentEx.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();  // contract validation copied from Read(...) 
      
            if (cancellationToken.IsCancellationRequested)  
                return Task.FromCancellation<Int32>(cancellationToken); 
        
            try { 
            
                Int32 n = Read(buffer, offset, count); 
                Task<Int32> t = _lastReadTask;
                return (t != null && t.Result == n) ? t : (_lastReadTask = Task.FromResult<Int32>(n)); 
                
            } catch (Exception ex) { 
            
                Contract.Assert(! (ex is OperationCanceledException));
                return Task.FromException<Int32>(ex); 
            } 
        } 
#endif  // !FEATURE_PAL && FEATURE_ASYNC_IO 

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override int ReadByte()
        {
            if (!_isOpen) __Error.StreamIsClosed();
            if (!CanRead) __Error.ReadNotSupported();

            long pos = Interlocked.Read(ref _position);  // Use a local to avoid a race condition
            long len = Interlocked.Read(ref _length);
            if (pos >= len)
                return -1;
            Interlocked.Exchange(ref _position, pos + 1);
            int result;
            if (_buffer != null)
            {
                unsafe
                {
                    byte* pointer = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        _buffer.AcquirePointer(ref pointer);
                        result = *(pointer + pos + _offset);
                    }
                    finally
                    {
                        if (pointer != null)
                        {
                            _buffer.ReleasePointer();
                        }
                    }
                }
            }
            else
            {
                unsafe
                {
                    result = _mem[pos];
                }
            }
            return result;
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            if (!_isOpen) __Error.StreamIsClosed();
            if (offset > UnmanagedMemStreamMaxLength)
                throw new ArgumentOutOfRangeException("offset", EnvironmentEx.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamLength"));
            switch (loc)
            {
                case SeekOrigin.Begin:
                    if (offset < 0)
                        throw new IOException(EnvironmentEx.GetResourceString("IO.IO_SeekBeforeBegin"));
                    Interlocked.Exchange(ref _position, offset);
                    break;

                case SeekOrigin.Current:
                    long pos = Interlocked.Read(ref _position);
                    if (offset + pos < 0)
                        throw new IOException(EnvironmentEx.GetResourceString("IO.IO_SeekBeforeBegin"));
                    Interlocked.Exchange(ref _position, offset + pos);
                    break;

                case SeekOrigin.End:
                    long len = Interlocked.Read(ref _length);
                    if (len + offset < 0)
                        throw new IOException(EnvironmentEx.GetResourceString("IO.IO_SeekBeforeBegin"));
                    Interlocked.Exchange(ref _position, len + offset);
                    break;

                default:
                    throw new ArgumentException(EnvironmentEx.GetResourceString("Argument_InvalidSeekOrigin"));
            }

            long finalPos = Interlocked.Read(ref _position);
            Contract.Assert(finalPos >= 0, "_position >= 0");
            return finalPos;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("length", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            if (_buffer != null)
                throw new NotSupportedException(EnvironmentEx.GetResourceString("NotSupported_UmsSafeBuffer"));
            if (!_isOpen) __Error.StreamIsClosed();
            if (!CanWrite) __Error.WriteNotSupported();

            if (value > _capacity)
                throw new IOException(EnvironmentEx.GetResourceString("IO.IO_FixedCapacity"));

            long pos = Interlocked.Read(ref _position);
            long len = Interlocked.Read(ref _length);
            if (value > len)
            {
                unsafe
                {
                    BufferEx.ZeroMemory(_mem + len, value - len);
                }
            }
            Interlocked.Exchange(ref _length, value);
            if (pos > value)
            {
                Interlocked.Exchange(ref _position, value);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", EnvironmentEx.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(EnvironmentEx.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();  // Keep contract validation in sync with WriteAsync(..)

            if (!_isOpen) __Error.StreamIsClosed();
            if (!CanWrite) __Error.WriteNotSupported();

            long pos = Interlocked.Read(ref _position);  // Use a local to avoid a race condition
            long len = Interlocked.Read(ref _length);
            long n = pos + count;
            // Check for overflow
            if (n < 0)
                throw new IOException(EnvironmentEx.GetResourceString("IO.IO_StreamTooLong"));

            if (n > _capacity)
            {
                throw new NotSupportedException(EnvironmentEx.GetResourceString("IO.IO_FixedCapacity"));
            }

            if (_buffer == null)
            {
                // Check to see whether we are now expanding the stream and must 
                // zero any memory in the middle.
                if (pos > len)
                {
                    unsafe
                    {
                        BufferEx.ZeroMemory(_mem + len, pos - len);
                    }
                }

                // set length after zeroing memory to avoid race condition of accessing unzeroed memory
                if (n > len)
                {
                    Interlocked.Exchange(ref _length, n);
                }
            }

            if (_buffer != null)
            {

                long bytesLeft = _capacity - pos;
                if (bytesLeft < count)
                {
                    throw new ArgumentException(EnvironmentEx.GetResourceString("Arg_BufferTooSmall"));
                }

                unsafe
                {
                    byte* pointer = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        _buffer.AcquirePointer(ref pointer);
                        BufferEx.Memcpy(pointer + pos + _offset, 0, buffer, offset, count);
                    }
                    finally
                    {
                        if (pointer != null)
                        {
                            _buffer.ReleasePointer();
                        }
                    }
                }
            }
            else
            {
                unsafe
                {
                    BufferEx.Memcpy(_mem + pos, 0, buffer, offset, count);
                }
            }
            Interlocked.Exchange(ref _position, n);
            return;
        }

#if !FEATURE_PAL && FEATURE_ASYNC_IO
        [HostProtection(ExternalThreading = true)] 
        [ComVisible(false)] 
        public override Task WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken) { 
        
            if (buffer==null)
                throw new ArgumentNullException("buffer", EnvironmentEx.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", EnvironmentEx.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(EnvironmentEx.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();  // contract validation copied from Write(..) 
                            
            if (cancellationToken.IsCancellationRequested)  
                return Task.FromCancellation(cancellationToken); 
         
            try { 
                       
                Write(buffer, offset, count); 
                return Task.CompletedTask; 
                
            } catch (Exception ex) { 
            
                Contract.Assert(! (ex is OperationCanceledException));
                return Task.FromException<Int32>(ex); 
            } 
        } 
#endif  // !FEATURE_PAL && FEATURE_ASYNC_IO 


        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void WriteByte(byte value)
        {
            if (!_isOpen) __Error.StreamIsClosed();
            if (!CanWrite) __Error.WriteNotSupported();

            long pos = Interlocked.Read(ref _position);  // Use a local to avoid a race condition
            long len = Interlocked.Read(ref _length);
            long n = pos + 1;
            if (pos >= len)
            {
                // Check for overflow
                if (n < 0)
                    throw new IOException(EnvironmentEx.GetResourceString("IO.IO_StreamTooLong"));

                if (n > _capacity)
                    throw new NotSupportedException(EnvironmentEx.GetResourceString("IO.IO_FixedCapacity"));

                // Check to see whether we are now expanding the stream and must 
                // zero any memory in the middle.
                // don't do if created from SafeBuffer
                if (_buffer == null)
                {
                    if (pos > len)
                    {
                        unsafe
                        {
                            BufferEx.ZeroMemory(_mem + len, pos - len);
                        }
                    }

                    // set length after zeroing memory to avoid race condition of accessing unzeroed memory
                    Interlocked.Exchange(ref _length, n);
                }
            }

            if (_buffer != null)
            {
                unsafe
                {
                    byte* pointer = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        _buffer.AcquirePointer(ref pointer);
                        *(pointer + pos + _offset) = value;
                    }
                    finally
                    {
                        if (pointer != null)
                        {
                            _buffer.ReleasePointer();
                        }
                    }
                }
            }
            else
            {
                unsafe
                {
                    _mem[pos] = value;
                }
            }
            Interlocked.Exchange(ref _position, n);
        }
    }

    public static class BufferEx
    {
        [SecurityCritical]
        internal unsafe static void Memcpy(byte[] dest, int destIndex, byte* src, int srcIndex, int len)
        {
            Contract.Assert((srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");
            Contract.Assert(dest.Length - destIndex >= len, "not enough bytes in dest");
            // If dest has 0 elements, the fixed statement will throw an 
            // IndexOutOfRangeException.  Special-case 0-byte copies.
            if (len == 0)
                return;
            fixed (byte* pDest = dest)
            {
                Memcpy(pDest + destIndex, src + srcIndex, len);
            }
        }

        [SecurityCritical]
        //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal unsafe static void Memcpy(byte* pDest, int destIndex, byte[] src, int srcIndex, int len)
        {
            Contract.Assert((srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");
            Contract.Assert(src.Length - srcIndex >= len, "not enough bytes in src");
            // If dest has 0 elements, the fixed statement will throw an 
            // IndexOutOfRangeException.  Special-case 0-byte copies.
            if (len == 0)
                return;
            fixed (byte* pSrc = src)
            {
                Memcpy(pDest + destIndex, pSrc + srcIndex, len);
            }
        }

        internal unsafe static void Memcpy(byte* dest, byte* src, int len)
        {
            Contract.Assert(len >= 0, "Negative length in memcopy!");
            IntPtr pDst = new IntPtr(dest);
            IntPtr pSrc = new IntPtr(src);
            UIntPtr pLen = new UIntPtr((uint)len);
            memmove(pDst, pSrc, pLen);
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr memmove(IntPtr dest, IntPtr src, UIntPtr count);

        // This method has different signature for x64 and other platforms and is done for performance reasons.
        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.None)]
        //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal unsafe static void Memmove(byte* dest, byte* src, uint len)
        {
#if AMD64 || (BIT32 && !ARM)
            const nuint CopyThreshold = 2048;
#elif ARM64
#if PLATFORM_WINDOWS
            // 
 
            const nuint CopyThreshold = 2048;
#else // PLATFORM_WINDOWS
            // Managed code is currently faster than glibc unoptimized memmove
            // 
 
            const nuint CopyThreshold = UInt64.MaxValue;
#endif // PLATFORM_WINDOWS
#else
            const uint CopyThreshold = 512;
#endif // AMD64 || (BIT32 && !ARM)

            // P/Invoke into the native version when the buffers are overlapping.

            if (((uint)dest - (uint)src < len) || ((uint)src - (uint)dest < len)) goto PInvoke;

            byte* srcEnd = src + len;
            byte* destEnd = dest + len;

            if (len <= 16) goto MCPY02;
            if (len > 64) goto MCPY05;

            MCPY00:
            // Copy bytes which are multiples of 16 and leave the remainder for MCPY01 to handle.
            Contract.Assert(len > 16 && len <= 64);
#if HAS_CUSTOM_BLOCKS
            *(Block16*)dest = *(Block16*)src;                   // [0,16]
#elif BIT64
            *(long*)dest = *(long*)src;
            *(long*)(dest + 8) = *(long*)(src + 8);             // [0,16]
#else
            *(int*)dest = *(int*)src;
            *(int*)(dest + 4) = *(int*)(src + 4);
            *(int*)(dest + 8) = *(int*)(src + 8);
            *(int*)(dest + 12) = *(int*)(src + 12);             // [0,16]
#endif
            if (len <= 32) goto MCPY01;
#if HAS_CUSTOM_BLOCKS
            *(Block16*)(dest + 16) = *(Block16*)(src + 16);     // [0,32]
#elif BIT64
            *(long*)(dest + 16) = *(long*)(src + 16);
            *(long*)(dest + 24) = *(long*)(src + 24);           // [0,32]
#else
            *(int*)(dest + 16) = *(int*)(src + 16);
            *(int*)(dest + 20) = *(int*)(src + 20);
            *(int*)(dest + 24) = *(int*)(src + 24);
            *(int*)(dest + 28) = *(int*)(src + 28);             // [0,32]
#endif
            if (len <= 48) goto MCPY01;
#if HAS_CUSTOM_BLOCKS
            *(Block16*)(dest + 32) = *(Block16*)(src + 32);     // [0,48]
#elif BIT64
            *(long*)(dest + 32) = *(long*)(src + 32);
            *(long*)(dest + 40) = *(long*)(src + 40);           // [0,48]
#else
            *(int*)(dest + 32) = *(int*)(src + 32);
            *(int*)(dest + 36) = *(int*)(src + 36);
            *(int*)(dest + 40) = *(int*)(src + 40);
            *(int*)(dest + 44) = *(int*)(src + 44);             // [0,48]
#endif

        MCPY01:
            // Unconditionally copy the last 16 bytes using destEnd and srcEnd and return.
            Contract.Assert(len > 16 && len <= 64);
#if HAS_CUSTOM_BLOCKS
            *(Block16*)(destEnd - 16) = *(Block16*)(srcEnd - 16);
#elif BIT64
            *(long*)(destEnd - 16) = *(long*)(srcEnd - 16);
            *(long*)(destEnd - 8) = *(long*)(srcEnd - 8);
#else
            *(int*)(destEnd - 16) = *(int*)(srcEnd - 16);
            *(int*)(destEnd - 12) = *(int*)(srcEnd - 12);
            *(int*)(destEnd - 8) = *(int*)(srcEnd - 8);
            *(int*)(destEnd - 4) = *(int*)(srcEnd - 4);
#endif
            return;

        MCPY02:
            // Copy the first 8 bytes and then unconditionally copy the last 8 bytes and return.
            if ((len & 24) == 0) goto MCPY03;
            Contract.Assert(len >= 8 && len <= 16);
#if BIT64
            *(long*)dest = *(long*)src;
            *(long*)(destEnd - 8) = *(long*)(srcEnd - 8);
#else
            *(int*)dest = *(int*)src;
            *(int*)(dest + 4) = *(int*)(src + 4);
            *(int*)(destEnd - 8) = *(int*)(srcEnd - 8);
            *(int*)(destEnd - 4) = *(int*)(srcEnd - 4);
#endif
            return;

        MCPY03:
            // Copy the first 4 bytes and then unconditionally copy the last 4 bytes and return.
            if ((len & 4) == 0) goto MCPY04;
            Contract.Assert(len >= 4 && len < 8);
            *(int*)dest = *(int*)src;
            *(int*)(destEnd - 4) = *(int*)(srcEnd - 4);
            return;

        MCPY04:
            // Copy the first byte. For pending bytes, do an unconditionally copy of the last 2 bytes and return.
            Contract.Assert(len < 4);
            if (len == 0) return;
            *dest = *src;
            if ((len & 2) == 0) return;
            *(short*)(destEnd - 2) = *(short*)(srcEnd - 2);
            return;

        MCPY05:
            // PInvoke to the native version when the copy length exceeds the threshold.
            if (len > CopyThreshold)
            {
                goto PInvoke;
            }
            // Copy 64-bytes at a time until the remainder is less than 64.
            // If remainder is greater than 16 bytes, then jump to MCPY00. Otherwise, unconditionally copy the last 16 bytes and return.
            Contract.Assert(len > 64 && len <= CopyThreshold);
            uint n = len >> 6;

        MCPY06:
#if HAS_CUSTOM_BLOCKS
            *(Block64*)dest = *(Block64*)src;
#elif BIT64
            *(long*)dest = *(long*)src;
            *(long*)(dest + 8) = *(long*)(src + 8);
            *(long*)(dest + 16) = *(long*)(src + 16);
            *(long*)(dest + 24) = *(long*)(src + 24);
            *(long*)(dest + 32) = *(long*)(src + 32);
            *(long*)(dest + 40) = *(long*)(src + 40);
            *(long*)(dest + 48) = *(long*)(src + 48);
            *(long*)(dest + 56) = *(long*)(src + 56);
#else
            *(int*)dest = *(int*)src;
            *(int*)(dest + 4) = *(int*)(src + 4);
            *(int*)(dest + 8) = *(int*)(src + 8);
            *(int*)(dest + 12) = *(int*)(src + 12);
            *(int*)(dest + 16) = *(int*)(src + 16);
            *(int*)(dest + 20) = *(int*)(src + 20);
            *(int*)(dest + 24) = *(int*)(src + 24);
            *(int*)(dest + 28) = *(int*)(src + 28);
            *(int*)(dest + 32) = *(int*)(src + 32);
            *(int*)(dest + 36) = *(int*)(src + 36);
            *(int*)(dest + 40) = *(int*)(src + 40);
            *(int*)(dest + 44) = *(int*)(src + 44);
            *(int*)(dest + 48) = *(int*)(src + 48);
            *(int*)(dest + 52) = *(int*)(src + 52);
            *(int*)(dest + 56) = *(int*)(src + 56);
            *(int*)(dest + 60) = *(int*)(src + 60);
#endif
            dest += 64;
            src += 64;
            n--;
            if (n != 0) goto MCPY06;

            len %= 64;
            if (len > 16) goto MCPY00;
#if HAS_CUSTOM_BLOCKS
            *(Block16*)(destEnd - 16) = *(Block16*)(srcEnd - 16);
#elif BIT64
            *(long*)(destEnd - 16) = *(long*)(srcEnd - 16);
            *(long*)(destEnd - 8) = *(long*)(srcEnd - 8);
#else
            *(int*)(destEnd - 16) = *(int*)(srcEnd - 16);
            *(int*)(destEnd - 12) = *(int*)(srcEnd - 12);
            *(int*)(destEnd - 8) = *(int*)(srcEnd - 8);
            *(int*)(destEnd - 4) = *(int*)(srcEnd - 4);
#endif
            return;

        PInvoke:
            _Memmove(dest, src, len);
        }

        private unsafe static void _Memmove(byte* dest, byte* src, uint len)
        {
            __Memmove(dest, src, len);
        }

        internal const string QCall = "QCall";
        [DllImport(QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        [ResourceExposure(ResourceScope.None)]
        //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        extern private unsafe static void __Memmove(byte* dest, byte* src, uint len);

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe static void ZeroMemory(byte* src, long len)
        {
            while (len-- > 0)
                *(src + len) = 0;
        }
    }
#endif
}