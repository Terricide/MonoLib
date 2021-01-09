namespace System.Runtime.InteropServices
{
    public struct OSPlatform : IEquatable<OSPlatform>
    {
        public static OSPlatform Linux => default(OSPlatform);

        public static OSPlatform OSX => default(OSPlatform);

        public static OSPlatform Windows => default(OSPlatform);

        public static OSPlatform Create(string osPlatform)
        {
            return default(OSPlatform);
        }

        public override bool Equals(object obj)
        {
            return false;
        }

        public bool Equals(OSPlatform other)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(OSPlatform left, OSPlatform right)
        {
            return false;
        }

        public static bool operator !=(OSPlatform left, OSPlatform right)
        {
            return false;
        }

        public override string ToString()
        {
            return null;
        }
    }
}
