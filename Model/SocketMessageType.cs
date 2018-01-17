using System;
using System.Collections.Generic;
using System.Text;

namespace FileTransfer.Model
{
    public enum SocketMessageType
    {
        Heart = 1,
        Request = 2,
        BigData = 3,

        Allow = 5,
        Refuse = 6,
    }
}
