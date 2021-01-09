//------------------------------------------------------------------------------
// <copyright file="TimeZoneNotFoundException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace System
{
    using System.Runtime.Serialization;

    [Serializable]
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public class TimeZoneNotFoundException : Exception
    {
        public TimeZoneNotFoundException(String message)
            : base(message) { }

        public TimeZoneNotFoundException(String message, Exception innerException)
            : base(message, innerException) { }

        protected TimeZoneNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        public TimeZoneNotFoundException() { }
    }
}
