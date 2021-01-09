using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using System.Threading;

namespace SharedMemory.MemoryMappedFiles
{
    public static class DecimalEx
    {
        internal static void GetBytes(Decimal d, byte[] buffer)
        {
            Contract.Requires((buffer != null && buffer.Length >= 16), "[GetBytes]buffer != null && buffer.Length >= 16");
            var intArr = decimal.GetBits(d);
            for(int i=0; i < intArr.Length; i++)
            {
                buffer[i] = (byte)intArr[i];
            }
            //buffer[0] = (byte)d.lo;
            //buffer[1] = (byte)(d.lo >> 8);
            //buffer[2] = (byte)(d.lo >> 16);
            //buffer[3] = (byte)(d.lo >> 24);

            //buffer[4] = (byte)d.mid;
            //buffer[5] = (byte)(d.mid >> 8);
            //buffer[6] = (byte)(d.mid >> 16);
            //buffer[7] = (byte)(d.mid >> 24);

            //buffer[8] = (byte)d.hi;
            //buffer[9] = (byte)(d.hi >> 8);
            //buffer[10] = (byte)(d.hi >> 16);
            //buffer[11] = (byte)(d.hi >> 24);

            //buffer[12] = (byte)d.flags;
            //buffer[13] = (byte)(d.flags >> 8);
            //buffer[14] = (byte)(d.flags >> 16);
            //buffer[15] = (byte)(d.flags >> 24);
        }
    }
}
