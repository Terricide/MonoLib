using System;
using System.Collections.Generic;
using System.Text;

namespace System.Threading
{
    public static class InterlockedEx
    {
        public static T CompareExchangeEnum<T>(ref T location, T value, T comparand)
        {
            return CompareExchangeEnumImpl<T>.Impl(ref location, value, comparand);
        }
    }
}
