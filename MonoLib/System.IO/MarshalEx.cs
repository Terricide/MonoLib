// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Purpose: Unsafe code that uses pointers should use 
** SafePointer to fix subtle lifetime problems with the 
** underlying resource.
**
===========================================================*/

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Permissions;

namespace SharedMemory
{
#if !NET40Plus

    /// <summary>
    /// <para>Very limited .NET 3.5 implementation of a managed wrapper around memory-mapped files to reflect the .NET 4 API.</para>
    /// <para>Only those methods and features necessary for the SharedMemory library have been implemented.</para>
    /// </summary>
#if NETFULL
    [PermissionSet(SecurityAction.LinkDemand)]
#endif
    class MarshalEx
    {
        // Type must be a value type with no object reference fields.  We only
        // assert this, due to the lack of a suitable generic constraint.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ResourceExposure(ResourceScope.None)]
        //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern uint SizeOfType(Type type);

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

        [ResourceExposure(ResourceScope.None)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int SizeOfHelper(Type t, bool throwIfNotMarshalable);

        // Type must be a value type with no object reference fields.  We only
        // assert this, due to the lack of a suitable generic constraint.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ResourceExposure(ResourceScope.None)]
        //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern uint AlignedSizeOfType(Type type);
    }
#endif
}