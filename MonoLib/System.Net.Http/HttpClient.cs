using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class HttpClient
    {
        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage());
        }

        //
        // Summary:
        //     Send a GET request to the specified Uri with a cancellation token as an asynchronous
        //     operation.
        //
        // Parameters:
        //   requestUri:
        //     The Uri the request is sent to.
        //
        //   cancellationToken:
        //     A cancellation token that can be used by other objects or threads to receive
        //     notice of cancellation.
        //
        // Returns:
        //     The task object representing the asynchronous operation.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The requestUri was null.
        //
        //   T:System.Net.Http.HttpRequestException:
        //     The request failed due to an underlying issue such as network connectivity, DNS
        //     failure, server certificate validation or timeout.
        public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
        {
            return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage());
        }
    }
}
