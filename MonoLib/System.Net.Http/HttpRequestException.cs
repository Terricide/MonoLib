using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.Http
{
    public class HttpRequestException : Exception
    {
        public HttpRequestException(string message) : base(message)
        {
        }

        public HttpRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
