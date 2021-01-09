using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.Serialization
{
    public class InvalidDataContractException : Exception
    {
        public InvalidDataContractException(string msg) : base(msg)
        {

        }
    }
}
