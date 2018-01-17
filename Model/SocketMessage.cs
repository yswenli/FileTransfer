using FileTransfer.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTransfer.Model
{
    public class SocketMessage
    {
        public long BodyLength
        {
            get; set;
        }
        public byte Type
        {
            get; set;
        }
        public Byte[] Content
        {
            get; set;
        }

        public byte[] ToBytes()
        {
            var len = ByteHelper.ConvertToBytes(BodyLength);

            var data = new List<byte>();

            data.AddRange(len);

            data.Add(Type);

            if (Content != null)
            {
                data.AddRange(Content);
            }

            return data.ToArray();
        }

        public static SocketMessage Parse(byte[] data)
        {
            var msg = new SocketMessage();

            msg.BodyLength = data.Length;

            msg.Type = (byte)SocketMessageType.Request;

            if (msg.BodyLength > 0)
            {
                msg.Content = data;
            }

            return msg;
        }
        public static SocketMessage ParseStream(byte[] data)
        {
            var msg = new SocketMessage();

            msg.BodyLength = data.Length;

            msg.Type = (byte)SocketMessageType.BigData;

            if (msg.BodyLength > 0)
            {
                msg.Content = data;
            }

            return msg;
        }
    }
}
