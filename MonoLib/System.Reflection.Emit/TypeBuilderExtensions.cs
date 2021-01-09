using System;
using System.Collections.Generic;
using System.Text;

namespace System.Reflection.Emit
{
    public static class TypeBuilderExtensions
    {
        public static TypeInfo CreateTypeInfo(this TypeBuilder tb)
        {
            return tb.CreateType().GetTypeInfo();
        }
    }
}
