using System;
using System.Collections.Generic;
using System.Text;

namespace FileTransfer.Common
{
    public static class LengthHelper
    {
        static int k = 1024;

        static int m = k * k;

        static long g = m * k;

        static long t = g * k;

        public static string Convert(long len)
        {
            string result = string.Empty;

            if (len < k)
            {
                result = string.Format("{0:F} B", len);
            }
            else if (len < m)
            {
                result = string.Format("{0} KB", ((len / 1.00 / k)).ToString("f2"));
            }
            else if (len < g)
            {
                result = string.Format("{0} MB", ((len / 1.00 / m)).ToString("f2"));
            }
            else
            {
                result = string.Format("{0} GB", ((len / 1.00 / g)).ToString("f2"));
            }
            return result;
        }

        public static string ToFString(this long l)
        {
            return Convert(l);
        }


    }
}
namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : Attribute { }
}


