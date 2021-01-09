using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.InteropServices
{
    public static class RuntimeInformation
    {
        public static Architecture ProcessArchitecture => Architecture.X86;

        public static Architecture OSArchitecture => Architecture.X86;

        public static string OSDescription => null;

        public static string FrameworkDescription => null;

        public static bool IsOSPlatform(OSPlatform osPlatform)
        {
            return false;
        }
    }
}
