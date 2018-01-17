using FileTransfer.Common;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace FileTransfer.Model
{
    public class UserToken
    {
        public string ID
        {
            get; set;
        }
        public Socket Socket
        {
            get; set;
        }

        public DateTime Linked
        {
            get;set;
        }

        public DateTime Actived
        {
            get;set;
        }

        public PackageHelper Package
        {
            get;set;
        }  
        
        public UserToken()
        {
            this.Linked = DateTimeHelper.Now;
        }
    }
}
