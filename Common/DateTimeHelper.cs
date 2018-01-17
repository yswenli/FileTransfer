using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FileTransfer.Common
{
    public static class DateTimeHelper
    {

        static DateTime _current;

        static DateTimeHelper()
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    while (true)
                    {
                        _current = DateTime.Now;
                        Thread.Sleep(1);
                    }
                }
                catch { }

            }))
            { IsBackground = true }.Start();
        }

        public static DateTime Now
        {
            get
            {
                if (_current == null)
                {
                    _current = DateTime.Now;
                }
                return _current;
            }
        }

        public static string ToString(string format = "yyyy-MM-dd HH:mm:ss.fff")
        {
            return Now.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
