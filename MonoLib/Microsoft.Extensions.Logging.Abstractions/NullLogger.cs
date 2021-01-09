using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Logging.Abstractions
{
    public class NullLogger : ILogger
    {
        //
        // Summary:
        //     Returns the shared instance of Microsoft.Extensions.Logging.Abstractions.NullLogger.
        public static NullLogger Instance
        {
            get;
        } = new NullLogger();


        private NullLogger()
        {
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }
    }
}
