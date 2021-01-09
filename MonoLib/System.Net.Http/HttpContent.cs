using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class HttpContent
    {
        public Task<string> ReadAsStringAsync()
        {
            return Task.FromResult(string.Empty);
        }
    }
}
