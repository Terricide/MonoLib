
namespace System
{
    /// <summary>
    /// EnvironmentEx
    /// </summary>
    public static class EnvironmentEx
    {
        /// <summary>
        /// 
        /// </summary>
        public static bool Is64BitProcess
        {
            get { return IntPtr.Size == 8; }
        }

        public static string GetResourceString(string str)
        {
            return str;
        }

        public static string GetResourceString(string str, int errorCode)
        {
            return $"{str} {errorCode}";
        }

        public static string GetResourceString(string str1, string str2)
        {
            return str1 + " " + str2;
        }
    }
}
