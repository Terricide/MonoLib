using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Logging.Abstractions
{
    internal class NullScope : IDisposable
    {
        public static NullScope Instance
        {
            get;
        } = new NullScope();


        private NullScope()
        {
        }

        public void Dispose()
        {
        }
    }
}
