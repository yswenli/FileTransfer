using System;
using System.Collections.Generic;
using System.Text;

namespace FileTransfer.Common
{
    public static class ByteHelper
    {
        public static byte[] ConvertToBytes(long num)
        {
            return BitConverter.GetBytes(num);
        }

        public static long ConvertToLong(byte[] data, int offset = 0)
        {
            return BitConverter.ToInt64(data, offset);
        }

    }
}
