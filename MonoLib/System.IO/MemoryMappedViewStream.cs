using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace System.IO.MemoryMappedFiles
{
#if !NET40Plus
    /// <summary>
    /// <para>Very limited .NET 3.5 implementation of a managed wrapper around memory-mapped files to reflect the .NET 4 API.</para>
    /// <para>Only those methods and features necessary for the SharedMemory library have been implemented.</para>
    /// </summary>
#if NETFULL
    [PermissionSet(SecurityAction.LinkDemand)]
#endif
    public sealed class MemoryMappedViewStream : UnmanagedMemoryStreamEx
    {

        private MemoryMappedView m_view;

        [System.Security.SecurityCritical]
        internal unsafe MemoryMappedViewStream(MemoryMappedView view)
        {
            Debug.Assert(view != null, "view is null");

            m_view = view;
            Initialize(m_view.ViewHandle, m_view.PointerOffset, m_view.Size, MemoryMappedFile.GetFileAccess(m_view.Access));
        }

        public SafeMemoryMappedViewHandle SafeMemoryMappedViewHandle
        {

            [System.Security.SecurityCritical]
            [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                return m_view != null ? m_view.ViewHandle : null;
            }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public long PointerOffset
        {
            get
            {
                if (m_view == null)
                {
                    throw new InvalidOperationException();
                }

                return m_view.PointerOffset;
            }
        }

        [SecuritySafeCritical]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && m_view != null && !m_view.IsClosed)
                {
                    Flush();
                }
            }
            finally
            {
                try
                {
                    if (m_view != null)
                    {
                        m_view.Dispose();
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        // Flushes the changes such that they are in sync with the FileStream bits (ones obtained
        // with the win32 ReadFile and WriteFile functions).  Need to call FileStream's Flush to 
        // flush to the disk.
        // NOTE: This will flush all bytes before and after the view up until an offset that is a 
        // multiple of SystemPageSize.
        [System.Security.SecurityCritical]
        public override void Flush()
        {
            if (!CanSeek)
            {
                __Error.StreamIsClosed();
            }

            unsafe
            {
                if (m_view != null)
                {
                    m_view.Flush((IntPtr)Capacity);
                }
            }
        }
    }
#endif
}
