using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DataMemberAttribute : Attribute
    {
        string name;
        bool isNameSetExplicitly;
        int order = -1;
        bool isRequired;
        bool emitDefaultValue = false;

        public DataMemberAttribute()
        {
        }

        public string Name
        {
            get { return name; }
            set { name = value; isNameSetExplicitly = true; }
        }

        public bool IsNameSetExplicitly
        {
            get { return isNameSetExplicitly; }
        }

        public int Order
        {
            get { return order; }
            set
            {
                if (value < 0)
                    throw new InvalidDataContractException("OrderCannotBeNegative");
                order = value;
            }
        }

        public bool IsRequired
        {
            get { return isRequired; }
            set { isRequired = value; }
        }

        public bool EmitDefaultValue
        {
            get { return emitDefaultValue; }
            set { emitDefaultValue = value; }
        }
    }
}
