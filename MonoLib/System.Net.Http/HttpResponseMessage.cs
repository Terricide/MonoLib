using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.Http
{
    public class HttpResponseMessage
    {
        //
        // Summary:
        //     Gets or sets the status code of the HTTP response.
        //
        // Returns:
        //     The status code of the HTTP response.
        public HttpStatusCode StatusCode { get; set; }

        public HttpContent Content { get; set; }
    }
}
