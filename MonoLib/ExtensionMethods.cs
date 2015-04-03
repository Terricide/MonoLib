using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public delegate void EventHandler<T>(object sender, T args);
}

namespace System.IO
{
    public static class ExtendClass
    {
        public async static Task<int> ReadAsync(this Stream s, byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            return await new System.Threading.Tasks.TaskFactory().StartNew(() =>
            {
                bytesRead = s.Read(buffer, offset, count);
            }).ContinueWith(x => bytesRead);
        }
    }
}

namespace System.Net
{
    public static class ExtendClass
    {
        public async static Task<WebResponse> GetResponseAsync(this WebRequest r)
        {
            WebResponse response = null;
            return await new System.Threading.Tasks.TaskFactory().StartNew(() =>
            {
                response = r.GetResponse();
            }).ContinueWith(x => response);
        }
    }
}

namespace System.Net.Sockets
{
    public static class ExtendClass
    {
        public static void Dispose(this Socket p)
        {

        }
    }
}

namespace System.Security.Cryptography
{
    public static class ExtendClass
    {
        public static void Dispose(this SHA1 p)
        {

        }
    }
}
