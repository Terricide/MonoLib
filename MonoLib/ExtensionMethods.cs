using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace System
{
    public static class TypeExtensionMonoClass
    {
        private static readonly Type s_DecimalConstantAttributeType = typeof(DecimalConstantAttribute);
        private static readonly Type s_CustomConstantAttributeType = typeof(CustomConstantAttribute);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="del"></param>
        /// <returns></returns>
        public static MethodInfo GetMethodInfo(this Delegate del)
        {
            if (del == null) throw new ArgumentNullException("del");

            return del.Method;
        }

        /// <summary>
        /// A FX 3.5 way to mimic the FX4 "HasFlag" method.
        /// </summary>
        /// <param name="variable">The tested enum.</param>
        /// <param name="value">The value to test.</param>
        /// <returns>True if the flag is set. Otherwise false.</returns>
        public static bool HasFlag(this Enum variable, Enum value)
        {
            // check if from the same type.
            if (variable.GetType() != value.GetType())
            {
                throw new ArgumentException("The checked flag is not from the same type as the checked variable.");
            }

            ulong num = Convert.ToUInt64(value);
            ulong num2 = Convert.ToUInt64(variable);

            return (num2 & num) == num;
        }


        public static bool IsDefined(this MemberInfo element, Type attributeType)
        {
            return Attribute.IsDefined(element, attributeType);
        }

        //public static TypeInfo GetTypeInfo(this object obj)
        //{
        //    return obj?.GetType().GetTypeInfo();
        //}

        public static bool TryParse<TEnum>(string str, out TEnum val) where TEnum : struct
        {
            try
            {
                val = (TEnum)Enum.Parse(typeof(TEnum), str, true);
                return true;
            }
            catch
            {
                val = default(TEnum);
                return false;
            }
        }

        public static bool IsConstructedGenericType(this Type type)
        {
            var info = type.GetTypeInfo();
            return info.IsGenericType && !info.IsGenericTypeDefinition;
        }

        public static Type AsType(this TypeInfo t)
        {
            return t.GetType();
        }

        internal static CustomAttributeTypedArgument Filter(IList<CustomAttributeData> attrs, Type caType, int parameter)
        {
            for (int i = 0; i < attrs.Count; i++)
            {
                if (attrs[i].Constructor.DeclaringType == caType)
                {
                    return attrs[i].ConstructorArguments[parameter];
                }
            }

            return default(CustomAttributeTypedArgument);
        }
        
        public static MethodInfo GetRuntimeMethod(this Type type, string name, Type[] parameters)
        {
            CheckAndThrow(type);
            return type.GetMethod(name, parameters);
        }

        private static void CheckAndThrow(Type t)
        {
            if (t == null) throw new ArgumentNullException("type");
            //if (!(t is RuntimeType)) throw new ArgumentException("Argument_MustBeRuntimeType"));
        }

        static internal bool ImplementInterface(this Type thisType, Type ifaceType)
        {
            Type t = thisType;
            while (t != null)
            {
                Type[] interfaces = t.GetInterfaces();
                if (interfaces != null)
                {
                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        // Interfaces don't derive from other interfaces, they implement them.
                        // So instead of IsSubclassOf, we should use ImplementInterface instead.
                        if (interfaces[i] == ifaceType ||
                            (interfaces[i] != null && interfaces[i].ImplementInterface(ifaceType)))
                            return true;
                    }
                }

                t = t.BaseType;
            }

            return false;
        }


        public static TypeInfo GetTypeInfo(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (type is IReflectableType reflectableType)
                return reflectableType.GetTypeInfo();

            return new TypeDelegator(type);
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrEmpty(str?.Trim());
        }

        public static T[] ToArray<T>(this ArraySegment<T> t)
        {
            return t.Array;
        }
    }

    public class EnumEx
    {
        public static bool TryParse<T>(string str, out T val) where T : struct
        {
            var names = Enum.GetNames(typeof(T));
            foreach (var name in names)
            {
                if (string.Compare(name, str, true) == 0)
                {
                    val = (T)Enum.Parse(typeof(T), name);
                    return true;
                }
            }
            val = default(T);
            return false;
        }
    }
}

namespace System.IO
{
    public static class ExtendClass
    {
        public static Task<int> ReadAsync(this Stream s, byte[] buffer, int offset, int count)
        {
            var t = Task.Factory.FromAsync<int>(
                (callback, state) => s.BeginRead(buffer, offset, count, callback, state),
                (ar) => { return s.EndRead(ar); },
                null);
            return t;
        }

        public static Task<int> ReadAsync(this TextReader s, char[] buffer, int offset, int count)
        {
            var res = s.Read(buffer, offset, count);
            return Task.FromResult(res);
        }

        public static Task WriteAsync(this TextWriter s, byte[] buffer)
        {
            s.Write(buffer);
            return Task.FromResult(0);
        }

        public static Task WriteAsync(this TextWriter s, string buffer)
        {
            s.Write(buffer);
            return Task.FromResult(0);
        }

        public static Task WriteAsync(this Stream s, byte[] buffer, int offset, int count)
        {
            var t = Task.Factory.FromAsync(
                (callback, state) => s.BeginWrite(buffer, offset, count, callback, state),
                (ar) => s.EndWrite(ar),
                null);
            return t;
        }

        public static Task WriteAsync(this Stream s, byte[] buffer, int offset, int count, CancellationToken token)
        {
            var t = Task.Factory.FromAsync(
                (callback, state) => s.BeginWrite(buffer, offset, count, callback, state),
                (ar) => s.EndWrite(ar),
                null);
            return t;
        }

        public static Task<int> ReadAsync(this Stream s, byte[] buffer, int offset, int count, CancellationToken token)
        {
            var t = Task.Factory.FromAsync<int>(
                (callback, state) => s.BeginRead(buffer, offset, count, callback, state),
                (ar) => s.EndRead(ar),
                null);
            return t;
        }

        public static Task FlushAsync(this Stream s, CancellationToken token)
        {
            s.Flush();
            return Task.CompletedTask;
        }

        public static void CopyTo(this Stream source, Stream dest)
        {
            byte[] buffer = new byte[4096];
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
                dest.Write(buffer, 0, count);
        }

        public static void CopyTo(this byte[] source, Stream dest)
        {
            byte[] buffer = new byte[4096];
            int count;
            BinaryReader br = new BinaryReader(new MemoryStream(source));
            while ((count = br.Read(buffer, 0, buffer.Length)) != 0)
                dest.Write(buffer, 0, count);
        }

        public static async Task<bool> CopyToAsync(this Stream source, Stream destination, int bufferLength = 4096)
        {
            var tcs = new TaskCompletionSource<bool>();
            var buff = new byte[bufferLength];

            AsyncCallback callback = null;
            callback = ar => {
                try
                {
                    var nread = source.EndRead(ar);
                    if (nread <= 0)
                    {
                        tcs.TrySetResult(true);
                        return;
                    }

                    destination.Write(buff, 0, nread);
                    source.BeginRead(buff, 0, bufferLength, callback, null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };

            try
            {
                source.BeginRead(buff, 0, bufferLength, callback, null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return await tcs.Task;
        }
    }
}

namespace System.Net
{
    public static class ExtendClass
    {
        public static Task<WebResponse> GetResponseAsync(this WebRequest r)
        {
            var t = Task<WebResponse>.Factory.FromAsync(
                (callback, state) => r.BeginGetResponse(callback, state),
                (ar) => r.EndGetResponse(ar),
                null);
            return t;
        }

        #region HttpWebRequest.AddRange(long)
        static MethodInfo httpWebRequestAddRangeHelper = typeof(WebHeaderCollection).GetMethod
                                                ("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
        /// <summary>
        /// Adds a byte range header to a request for a specific range from the beginning or end of the requested data.
        /// </summary>
        /// <param name="request">The <see cref="System.Web.HttpWebRequest"/> to add the range specifier to.</param>
        /// <param name="start">The starting or ending point of the range.</param>
        public static void AddRange(this HttpWebRequest request, string rangeSpecifier, long start) { request.AddRange(start, -1L); }

        /// <summary>Adds a byte range header to the request for a specified range.</summary>
        /// <param name="request">The <see cref="System.Web.HttpWebRequest"/> to add the range specifier to.</param>
        /// <param name="start">The position at which to start sending data.</param>
        /// <param name="end">The position at which to stop sending data.</param>
        public static void AddRange(this HttpWebRequest request, long start, long end)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (start < 0) throw new ArgumentOutOfRangeException("start", "Starting byte cannot be less than 0.");
            if (end < start) end = -1;

            string key = "Range";
            string val = string.Format("bytes={0}-{1}", start, end == -1 ? "" : end.ToString());

            httpWebRequestAddRangeHelper.Invoke(request.Headers, new object[] { key, val });
        }

        /// <summary>Adds a byte range header to the request for a specified range.</summary>
        /// <param name="request">The <see cref="System.Web.HttpWebRequest"/> to add the range specifier to.</param>
        /// <param name="start">The position at which to start sending data.</param>
        /// <param name="end">The position at which to stop sending data.</param>
        public static void AddRange(this HttpWebRequest request, string rangeSpecifier, long start, long end)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (start < 0) throw new ArgumentOutOfRangeException("start", "Starting byte cannot be less than 0.");
            if (end < start) end = -1;

            string key = "Range";
            string val = string.Format("bytes={0}-{1}", start, end == -1 ? "" : end.ToString());

            httpWebRequestAddRangeHelper.Invoke(request.Headers, new object[] { key, val });
        }

        public static Task<int> SendAsync(this UdpClient s, byte[] buffer, int bytes, IPEndPoint endPoint)
        {
            //var tcs = new TaskCompletionSource<int>();
            //s.BeginSend(buffer, bytes, endPoint, (ar) =>
            //{
            //    try
            //    {
            //        var res = s.EndSend(ar);
            //        tcs.TrySetResult(res);
            //    }
            //    catch (Exception ex)
            //    {
            //        tcs.TrySetException(ex);
            //    }
            //}, null);

            //return tcs.Task;

            var t = Task.Factory.FromAsync(
                (callback, state) => s.BeginSend(buffer, bytes, endPoint, callback, state),
                (ar) => s.EndSend(ar),
                null);
            return t;
        }

        internal const int IPv6AddressBytes = 16;
        const int NumberOfLabels = IPv6AddressBytes / 2;

        // Takes the last 4 bytes of an IPv6 address and converts it to an IPv4 address.
        // This does not restrict to address with the ::FFFF: prefix because other types of 
        // addresses display the tail segments as IPv4 like Terado.
        public static IPAddress MapToIPv4(this IPAddress addr)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                return addr;
            }

            ushort[] m_Numbers = IPAddressNumbers(addr);

            // Cast the ushort values to a uint and mask with unsigned literal before bit shifting.
            // Otherwise, we can end up getting a negative value for any IPv4 address that ends with
            // a byte higher than 127 due to sign extension of the most significant 1 bit.
            long address = ((((uint)m_Numbers[6] & 0x0000FF00u) >> 8) | (((uint)m_Numbers[6] & 0x000000FFu) << 8)) |
                    (((((uint)m_Numbers[7] & 0x0000FF00u) >> 8) | (((uint)m_Numbers[7] & 0x000000FFu) << 8)) << 16);

            return new IPAddress(address);
        }

        public static ushort[] IPAddressNumbers(this IPAddress addr)
        {
            var addressBytes = addr.GetAddressBytes();

            ushort[] m_Numbers = new ushort[NumberOfLabels];

            for (int i = 0; i < NumberOfLabels; i++)
            {
                m_Numbers[i] = (ushort)(addressBytes[i * 2] * 256 + addressBytes[i * 2 + 1]);
            }
            return m_Numbers;
        }
        #endregion
    }
}

namespace System.Diagnostics
{
    public static class DianosticsExtensions
    {
        public static void Restart(this Stopwatch sw)
        {
            sw.Stop();
            sw.Reset();
            sw.Start();
        }
    }
}

namespace System.Net.Sockets
{
    public static class SocketsExtensionMethods
    {
        public static void Dispose(this Socket socket)
        {
            var type = socket.GetType();
            var methods = type.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | Reflection.BindingFlags.Public).Where(n => n.Name == "Dispose");
            var method = methods.Where(n => n.GetParameters().Count() == 0).FirstOrDefault();
            if (method != null)
            {
                method.Invoke(socket, null);
            }
        }

        public static void Dispose(this UdpClient socket)
        {
            var type = socket.GetType();
            var methods = type.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | Reflection.BindingFlags.Public).Where(n => n.Name == "Dispose");
            var method = methods.Where(n => n.GetParameters().Count() == 0).FirstOrDefault();
            if (method != null)
            {
                method.Invoke(socket, null);
            }
        }

        //
        // Summary:
        //     Returns a UDP datagram asynchronously that was sent by a remote host.
        //
        // Returns:
        //     Returns System.Threading.Tasks.Task`1.The task object representing the asynchronous
        //     operation.
        //
        // Exceptions:
        //   T:System.ObjectDisposedException:
        //     The underlying System.Net.Sockets.Socket has been closed.
        //
        //   T:System.Net.Sockets.SocketException:
        //     An error occurred when accessing the socket. See the Remarks section for more
        //     information.
        public static Task<UdpReceiveResult> ReceiveAsync(this UdpClient s)
        {
            var t = Task<UdpReceiveResult>.Factory.FromAsync(
                (callback, state) => s.BeginReceive(callback, state),
                (ar) =>
                {
                    IPEndPoint remoteEp = null;
                    var result = s.EndReceive(ar, ref remoteEp);
                    return new UdpReceiveResult(result, remoteEp);
                },
                null);
            return t;
        }

        public static Task ConnectAsync(this TcpClient s, IPAddress address, int port)
        {
            var t = Task.Factory.FromAsync(
                (callback, state) => s.BeginConnect(address, port, callback, state),
                (ar) => s.EndConnect(ar),
                null);
            return t;
        }

        public static Task<UdpReceiveResult> ReceiveAsync(this UdpClient udpClient, CancellationToken cancellationToken)
        {
            // Start the original operation
            Task<UdpReceiveResult> receiveTask = udpClient.ReceiveAsync();

            // Add support for cancellation
            return receiveTask.WithCancellation(cancellationToken);
        }


        public static void Dispose(this TcpClient socket)
        {
            var type = socket.GetType();
            var methods = type.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | Reflection.BindingFlags.Public).Where(n => n.Name == "Dispose");
            var method = methods.Where(n => n.GetParameters().Count() == 0).FirstOrDefault();
            if (method != null)
            {
                method.Invoke(socket, null);
            }
        }

        public static Task<Socket> AcceptSocketAsync(this TcpListener s)
        {
            var t = Task<Socket>.Factory.FromAsync(
                (callback, state) => s.BeginAcceptSocket(callback, state),
                (ar) => s.EndAcceptSocket(ar),
                null);
            return t;
        }

        public static Task AuthenticateAsServerAsync(this SslStream s, X509Certificate2 cert)
        {
            var t = Task.Factory.FromAsync(
                (callback, state) => s.BeginAuthenticateAsServer(cert, callback, state),
                (ar) => s.EndAuthenticateAsServer(ar),
                null);
            return t;
        }

        public static Task AuthenticateAsClientAsync(this SslStream s, string certName)
        {
            var t = Task.Factory.FromAsync(
                (callback, state) => s.BeginAuthenticateAsClient(certName, callback, state),
                (ar) => s.EndAuthenticateAsClient(ar),
                null);
            return t;
        }
    }
}

namespace System.Text
{
    public static class TextExtensionMethods
    {
        public static void Clear(this StringBuilder sb)
        {
            sb.Length = 0;
            sb.Capacity = 0;
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

namespace System.Threading
{
    public static class ThreadingExtensionMethods
    {
        public static void Dispose(this EventWaitHandle evt)
        {
            if (evt == null)
                throw new NullReferenceException();
            evt.Close();
            //var type = evt.GetType();
            //var method = type.GetMethod("Dispose", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            //if (method != null)
            //{
            //    method.Invoke(evt, null);
            //}
        }
    }
}

namespace System.Threading.Tasks
{
    public static class TaskExtensionMethods
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            // Create a self-cancelling TaskCompletionSource 
            var tcs = new TaskCompletionSourceWithCancellation<T>(cancellationToken);

            // Wait for completion or cancellation
            Task<T> completedTask = await Task.WhenAny(task, tcs.Task);
            return await completedTask;
        }
    }

    public class TaskCompletionSourceWithCancellation<TResult> : TaskCompletionSource<TResult>
    {
        public TaskCompletionSourceWithCancellation(CancellationToken cancellationToken)
        {
            CancellationTokenRegistration registration =
                cancellationToken.Register(() => TrySetResult(default(TResult)));

            // Remove the registration after the task completes
            Task.ContinueWith(_ => registration.Dispose());
        }
    }
}
