using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Logging.Abstractions
{
    public class NullLoggerFactory : ILoggerFactory, IDisposable
    {
        //
        // Summary:
        //     Returns the shared instance of Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.
        public static readonly NullLoggerFactory Instance = new NullLoggerFactory();

        //
        // Summary:
        //     Creates a new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory instance.
        public NullLoggerFactory()
        {
        }

        //
        // Remarks:
        //     This returns a Microsoft.Extensions.Logging.Abstractions.NullLogger instance
        //     which logs nothing.
        public ILogger CreateLogger(string name)
        {
            return NullLogger.Instance;
        }

        //
        // Remarks:
        //     This method ignores the parameter and does nothing.
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }
}
