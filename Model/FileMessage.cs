using System;
using System.Collections.Generic;
using System.Text;

namespace FileTransfer.Model
{
    [Serializable]
    public class FileMessage
    {
        public string FileName
        {
            get; set;
        }

        public long Length
        {
            get; set;
        }

        public long Offset
        {
            get; set;
        }
    }
}
