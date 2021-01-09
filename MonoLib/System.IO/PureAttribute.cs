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
using System.Security.Permissions;

namespace System.IO
{
#if !NET40Plus

    /// <summary>
    /// <para>Very limited .NET 3.5 implementation of a managed wrapper around memory-mapped files to reflect the .NET 4 API.</para>
    /// <para>Only those methods and features necessary for the SharedMemory library have been implemented.</para>
    /// </summary>
#if NETFULL
    [PermissionSet(SecurityAction.LinkDemand)]
#endif
    internal class PureAttribute : Attribute
    {
    }
#endif
}