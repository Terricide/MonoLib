using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace MonoLib.System
{
    public static class ConsoleExtensions
    {
        private static volatile object s_InternalSyncObject;
        private static volatile bool _stdOutRedirectQueried = false;
        private static volatile bool _stdErrRedirectQueried = false;
        private static bool _isStdOutRedirected;
        private static bool _isStdErrRedirected;
        private static volatile IntPtr _consoleOutputHandle;

        private static IntPtr ConsoleOutputHandle
        {
            [SecurityCritical]
            get
            {
                if (_consoleOutputHandle == IntPtr.Zero)
                {
                    _consoleOutputHandle = Win32Native.GetStdHandle(-11);
                }

                return _consoleOutputHandle;
            }
        }


        private static object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    object value = new object();
                    Interlocked.CompareExchange<object>(ref s_InternalSyncObject, value, (object)null);
                }

                return s_InternalSyncObject;
            }
        }

        public static bool IsOutputRedirected
        {
            [SecuritySafeCritical]
            get
            {
                if (_stdOutRedirectQueried)
                {
                    return _isStdOutRedirected;
                }

                lock (InternalSyncObject)
                {
                    if (_stdOutRedirectQueried)
                    {
                        return _isStdOutRedirected;
                    }

                    _isStdOutRedirected = IsHandleRedirected(ConsoleOutputHandle);
                    _stdOutRedirectQueried = true;
                    return _isStdOutRedirected;
                }
            }
        }


        [SecuritySafeCritical]
        private static bool IsHandleRedirected(IntPtr ioHandle)
        {
            SafeFileHandle handle = new SafeFileHandle(ioHandle, ownsHandle: false);
            int fileType = Win32Native.GetFileType(handle);
            if ((fileType & 2) != 2)
            {
                return true;
            }

            int mode;
            bool consoleMode = Win32Native.GetConsoleMode(ioHandle, out mode);
            return !consoleMode;
        }

        public static bool IsErrorRedirected
        {
            [SecuritySafeCritical]
            get
            {
                if (_stdErrRedirectQueried)
                {
                    return _isStdErrRedirected;
                }

                lock (InternalSyncObject)
                {
                    if (_stdErrRedirectQueried)
                    {
                        return _isStdErrRedirected;
                    }

                    IntPtr stdHandle = Win32Native.GetStdHandle(-12);
                    _isStdErrRedirected = IsHandleRedirected(stdHandle);
                    _stdErrRedirectQueried = true;
                    return _isStdErrRedirected;
                }
            }
        }
    }
}
