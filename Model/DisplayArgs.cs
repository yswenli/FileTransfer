using System;
using System.Collections.Generic;
using System.Text;

namespace FileTransfer.Model
{
    public class DisplayArgs
    {
        public string Message
        {
            get;set;
        }

        public DisplayArgs(string msg)
        {
            this.Message = msg;
        }
    }
}
