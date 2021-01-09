// SharedMemory (File: SharedMemory\UnsafeNativeMethods.cs)
// Copyright (c) 2014 Justin Stenning
// http://spazzarama.com
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// The SharedMemory library is inspired by the following Code Project article:
//   "Fast IPC Communication Using Shared Memory and InterlockedCompareExchange"
//   http://www.codeproject.com/Articles/14740/Fast-IPC-Communication-Using-Shared-Memory-and-Int
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace SharedMemory
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal class UnsafeNativeMethods
    {
        private UnsafeNativeMethods() { }

#if !NETCORE
        /// <summary>
        /// Allow copying memory from one IntPtr to another. Required as the <see cref="System.Runtime.InteropServices.Marshal.Copy(System.IntPtr, System.IntPtr[], int, int)"/> implementation does not provide an appropriate override.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="src"></param>
        /// <param name="count"></param>
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        [SecurityCritical]
        internal static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        [SecurityCritical]
        internal static extern unsafe void CopyMemoryPtr(void* dest, void* src, uint count);
#endif

#if !NET40Plus

        [DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Auto, ExactSpelling = false)]
        [SecurityCritical]
        internal static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr va_list_arguments);

        [SecurityCritical]
        internal static string GetMessage(int errorCode)
        {
            StringBuilder stringBuilder = new StringBuilder(512);
            if (UnsafeNativeMethods.FormatMessage(12800, IntPtr.Zero, errorCode, 0, stringBuilder, stringBuilder.Capacity, IntPtr.Zero) != 0)
            {
                return stringBuilder.ToString();
            }
            return string.Concat("UnknownError_Num ", errorCode);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_INFO
        {
            internal int dwOemId;    // This is a union of a DWORD and a struct containing 2 WORDs.
            internal int dwPageSize;
            internal IntPtr lpMinimumApplicationAddress;
            internal IntPtr lpMaximumApplicationAddress;
            internal IntPtr dwActiveProcessorMask;
            internal int dwNumberOfProcessors;
            internal int dwProcessorType;
            internal int dwAllocationGranularity;
            internal short wProcessorLevel;
            internal short wProcessorRevision;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct _PROCESSOR_INFO_UNION
        {
            [FieldOffset(0)]
            internal uint dwOemId;
            [FieldOffset(0)]
            internal ushort wProcessorArchitecture;
            [FieldOffset(2)]
            internal ushort wReserved;
        }

        [Flags]
        public enum FileMapAccess : uint
        {
            FileMapCopy = 0x0001,
            FileMapWrite = 0x0002,
            FileMapRead = 0x0004,
            FileMapAllAccess = 0x001f,
            FileMapExecute = 0x0020,
        }

        [Flags]
        internal enum FileMapProtection : uint
        {
            PageReadonly = 0x02,
            PageReadWrite = 0x04,
            PageWriteCopy = 0x08,
            PageExecuteRead = 0x20,
            PageExecuteReadWrite = 0x40,
            SectionCommit = 0x8000000,
            SectionImage = 0x1000000,
            SectionNoCache = 0x10000000,
            SectionReserve = 0x4000000,
        }
        internal const String KERNEL32 = "kernel32.dll";
        internal const String ADVAPI32 = "advapi32.dll";
        internal const String WEVTAPI = "wevtapi.dll";
        internal static readonly IntPtr NULL = IntPtr.Zero;

        internal class SECURITY_ATTRIBUTES
        {
            internal int nLength;
            [SecurityCritical]
            internal unsafe byte* pSecurityDescriptor;
            internal int bInheritHandle;
        }

        //
        // Win32 IO
        //
        internal const int CREDUI_MAX_USERNAME_LENGTH = 513;


        // WinError.h codes:

        internal const int ERROR_SUCCESS = 0x0;
        internal const int ERROR_FILE_NOT_FOUND = 0x2;
        internal const int ERROR_PATH_NOT_FOUND = 0x3;
        internal const int ERROR_ACCESS_DENIED = 0x5;
        internal const int ERROR_INVALID_HANDLE = 0x6;

        // Can occurs when filled buffers are trying to flush to disk, but disk IOs are not fast enough. 
        // This happens when the disk is slow and event traffic is heavy. 
        // Eventually, there are no more free (empty) buffers and the event is dropped.
        internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;

        internal const int ERROR_INVALID_DRIVE = 0xF;
        internal const int ERROR_NO_MORE_FILES = 0x12;
        internal const int ERROR_NOT_READY = 0x15;
        internal const int ERROR_BAD_LENGTH = 0x18;
        internal const int ERROR_SHARING_VIOLATION = 0x20;
        internal const int ERROR_LOCK_VIOLATION = 0x21;  // 33
        internal const int ERROR_HANDLE_EOF = 0x26;  // 38
        internal const int ERROR_FILE_EXISTS = 0x50;
        internal const int ERROR_INVALID_PARAMETER = 0x57;  // 87
        internal const int ERROR_BROKEN_PIPE = 0x6D;  // 109
        internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;  // 122
        internal const int ERROR_INVALID_NAME = 0x7B;
        internal const int ERROR_BAD_PATHNAME = 0xA1;
        internal const int ERROR_ALREADY_EXISTS = 0xB7;
        internal const int ERROR_ENVVAR_NOT_FOUND = 0xCB;
        internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long
        internal const int ERROR_PIPE_BUSY = 0xE7;  // 231
        internal const int ERROR_NO_DATA = 0xE8;  // 232
        internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;  // 233
        internal const int ERROR_MORE_DATA = 0xEA;
        internal const int ERROR_NO_MORE_ITEMS = 0x103;  // 259
        internal const int ERROR_PIPE_CONNECTED = 0x217;  // 535
        internal const int ERROR_PIPE_LISTENING = 0x218;  // 536
        internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
        internal const int ERROR_IO_PENDING = 0x3E5;  // 997
        internal const int ERROR_NOT_FOUND = 0x490;  // 1168      

        //
        // ErrorCode & format 
        //

        // Use this to translate error codes like the above into HRESULTs like

        // 0x80070006 for ERROR_INVALID_HANDLE
        internal static int MakeHRFromErrorCode(int errorCode)
        {
            return unchecked(((int)0x80070000) | errorCode);
        }

        // The event size is larger than the allowed maximum (64k - header).
        internal const int ERROR_ARITHMETIC_OVERFLOW = 0x216;  // 534

        internal const int ERROR_RESOURCE_LANG_NOT_FOUND = 0x717;  // 1815


        // Event log specific codes:

        internal const int ERROR_EVT_MESSAGE_NOT_FOUND = 15027;
        internal const int ERROR_EVT_MESSAGE_ID_NOT_FOUND = 15028;
        internal const int ERROR_EVT_UNRESOLVED_VALUE_INSERT = 15029;
        internal const int ERROR_EVT_UNRESOLVED_PARAMETER_INSERT = 15030;
        internal const int ERROR_EVT_MAX_INSERTS_REACHED = 15031;
        internal const int ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND = 15033;
        internal const int ERROR_MUI_FILE_NOT_FOUND = 15100;


        internal const int SECURITY_SQOS_PRESENT = 0x00100000;
        internal const int SECURITY_ANONYMOUS = 0 << 16;
        internal const int SECURITY_IDENTIFICATION = 1 << 16;
        internal const int SECURITY_IMPERSONATION = 2 << 16;
        internal const int SECURITY_DELEGATION = 3 << 16;

        internal const int GENERIC_READ = unchecked((int)0x80000000);
        internal const int GENERIC_WRITE = 0x40000000;

        internal const int STD_INPUT_HANDLE = -10;
        internal const int STD_OUTPUT_HANDLE = -11;
        internal const int STD_ERROR_HANDLE = -12;

        internal const int DUPLICATE_SAME_ACCESS = 0x00000002;

        internal const int PIPE_ACCESS_INBOUND = 1;
        internal const int PIPE_ACCESS_OUTBOUND = 2;
        internal const int PIPE_ACCESS_DUPLEX = 3;
        internal const int PIPE_TYPE_BYTE = 0;
        internal const int PIPE_TYPE_MESSAGE = 4;
        internal const int PIPE_READMODE_BYTE = 0;
        internal const int PIPE_READMODE_MESSAGE = 2;
        internal const int PIPE_UNLIMITED_INSTANCES = 255;

        internal const int FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;
        internal const int FILE_SHARE_READ = 0x00000001;
        internal const int FILE_SHARE_WRITE = 0x00000002;
        internal const int FILE_ATTRIBUTE_NORMAL = 0x00000080;

        internal const int FILE_FLAG_OVERLAPPED = 0x40000000;

        internal const int OPEN_EXISTING = 3;

        // From WinBase.h
        internal const int FILE_TYPE_DISK = 0x0001;
        internal const int FILE_TYPE_CHAR = 0x0002;
        internal const int FILE_TYPE_PIPE = 0x0003;

        // Memory mapped file constants
        internal const int MEM_COMMIT = 0x1000;
        internal const int MEM_RESERVE = 0x2000;
        internal const int INVALID_FILE_SIZE = -1;
        internal const int PAGE_READWRITE = 0x04;
        internal const int PAGE_READONLY = 0x02;
        internal const int PAGE_WRITECOPY = 0x08;
        internal const int PAGE_EXECUTE_READ = 0x20;
        internal const int PAGE_EXECUTE_READWRITE = 0x40;

        internal const int FILE_MAP_COPY = 0x0001;
        internal const int FILE_MAP_WRITE = 0x0002;
        internal const int FILE_MAP_READ = 0x0004;
        internal const int FILE_MAP_EXECUTE = 0x0020;

        [DllImport("kernel32.dll", CharSet = CharSet.None, SetLastError = true)]
        [SecurityCritical]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        [SecurityCritical]
        internal static extern SafeMemoryMappedFileHandle CreateFileMapping(
                                SafeFileHandle hFile,
                                SECURITY_ATTRIBUTES lpAttributes,
                                int fProtect,
                                int dwMaximumSizeHigh,
                                int dwMaximumSizeLow,
                                String lpName
                                );
        //internal static SafeMemoryMappedFileHandle CreateFileMapping(SafeFileHandle hFile, FileMapProtection flProtect, Int64 ddMaxSize, string lpName)
        //{
        //    int hi = (Int32)(ddMaxSize / Int32.MaxValue);
        //    int lo = (Int32)(ddMaxSize % Int32.MaxValue);
        //    return CreateFileMapping(hFile, IntPtr.Zero, flProtect, hi, lo, lpName);
        //}

        [DllImport("kernel32.dll")]
        internal static extern void GetSystemInfo([MarshalAs(UnmanagedType.Struct)] ref SYSTEM_INFO lpSystemInfo);

        [DllImport(KERNEL32, SetLastError = true, ExactSpelling = true)]
        [SecurityCritical]
        internal static extern SafeMemoryMappedViewHandle MapViewOfFile(
                                SafeMemoryMappedFileHandle handle,
                                int dwDesiredAccess,
                                uint dwFileOffsetHigh,
                                uint dwFileOffsetLow,
                                UIntPtr dwNumberOfBytesToMap
                                );

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        unsafe internal static extern bool FlushViewOfFile(
                                byte* lpBaseAddress,
                                IntPtr dwNumberOfBytesToFlush
                                );


        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        [SecurityCritical]
        internal static extern SafeMemoryMappedFileHandle OpenFileMapping(
                                 int dwDesiredAccess,
                                 [MarshalAs(UnmanagedType.Bool)]
                                bool bInheritHandle,
                                 string lpName
                                 );

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct MEMORYSTATUSEX
        {
            internal uint dwLength;
            internal uint dwMemoryLoad;
            internal ulong ullTotalPhys;
            internal ulong ullAvailPhys;
            internal ulong ullTotalPageFile;
            internal ulong ullAvailPageFile;
            internal ulong ullTotalVirtual;
            internal ulong ullAvailVirtual;
            internal ulong ullAvailExtendedVirtual;
        }

        [SecurityCritical]
        internal static bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer)
        {
            lpBuffer.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            return GlobalMemoryStatusExNative(ref lpBuffer);
        }

        [DllImport(KERNEL32, CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "GlobalMemoryStatusEx")]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusExNative([In, Out] ref MEMORYSTATUSEX lpBuffer);

        //
        // EventLog
        // 

        //
        // Memory Mapped File
        //
        [StructLayout(LayoutKind.Sequential)]
#pragma warning disable 618 // Ssytem.Core still uses SecurityRuleSet.Level1
        [SecurityCritical(SecurityCriticalScope.Everything)]
#pragma warning restore 618
        internal unsafe struct MEMORY_BASIC_INFORMATION
        {
            internal void* BaseAddress;
            internal void* AllocationBase;
            internal uint AllocationProtect;
            internal UIntPtr RegionSize;
            internal uint State;
            internal uint Protect;
            internal uint Type;
        }

        [DllImport(KERNEL32, SetLastError = true)]
        [SecurityCritical]
        unsafe internal static extern IntPtr VirtualQuery(
                                SafeMemoryMappedViewHandle address,
                                ref MEMORY_BASIC_INFORMATION buffer,
                                IntPtr sizeOfBuffer
                                );

        [DllImport(KERNEL32, SetLastError = true)]
        [SecurityCritical]
        unsafe internal static extern IntPtr VirtualAlloc(
                                SafeMemoryMappedViewHandle address,
                                UIntPtr numBytes,
                                int commitOrReserve,
                                int pageProtectionMode
                                );

#endif
    }
}
