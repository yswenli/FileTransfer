using FileTransfer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTransfer.Common
{
    public delegate void DisplayHandler(DisplayArgs args);

    public class Displayer
    {
        public event DisplayHandler Display;

        public void SetMessage(string msg)
        {
            Display?.BeginInvoke(new DisplayArgs(msg), null, null);
        }
    }
}
