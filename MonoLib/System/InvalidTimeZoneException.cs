//------------------------------------------------------------------------------
// <copyright file="InvalidTimeZoneException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace System
{
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;

    [Serializable]
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public class InvalidTimeZoneException : Exception
    {
        public InvalidTimeZoneException(String message)
            : base(message) { }

        public InvalidTimeZoneException(String message, Exception innerException)
            : base(message, innerException) { }

        protected InvalidTimeZoneException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        public InvalidTimeZoneException() { }
    }
}
