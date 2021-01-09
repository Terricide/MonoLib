using System;
using System.Collections.Generic;
using System.Text;

namespace System.Reflection
{
    public interface IReflectableType
    {
        TypeInfo GetTypeInfo();
    }
}
