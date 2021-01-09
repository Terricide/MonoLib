using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace System.Security.Cryptography
{
    public static class CryptographyExtensions
    {
        public static void TryComputeHash(this HashAlgorithm ha, Span<byte> span, Span<byte> hash, out int val)
        {
            val = 0;

            var dataToCompute = span.ToArray();
            var data = ha.ComputeHash(dataToCompute);
            for(var i=0; i < hash.Length; i++)
            {
                hash[i] = data[i];
            }
        }
    }
}
