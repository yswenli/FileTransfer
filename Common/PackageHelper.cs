using FileTransfer.Model;
using System;
using System.Collections.Generic;

namespace FileTransfer.Common
{
    public class PackageHelper
    {
        public const int P_LEN = 8;

        public const int P_Type = 1;

        public const int P_Head = 9;

        private List<byte> _buffer = new List<byte>();

        private object _locker = new object();

        public void Add(byte[] data, Action<DateTime> OnHeart, Action<SocketMessage> OnUnPackage, Action<byte[]> OnFile)
        {
            lock (_locker)
            {
                _buffer.AddRange(data);

                var buffer = _buffer.ToArray();

                if (buffer.Length >= P_Head)
                {
                    var bodyLen = GetLength(buffer);

                    var type = GetType(buffer);

                    if (bodyLen == 0) //空包认为是心跳包
                    {
                        var sm = new SocketMessage() { BodyLength = bodyLen, Type = (byte)type };
                        _buffer.Clear();
                        OnHeart?.Invoke(DateTimeHelper.Now);
                    }
                    else if (buffer.Length >= P_Head + bodyLen)
                    {
                        if (type == SocketMessageType.BigData)
                        {
                            var content = GetContent(buffer, P_Head, (int)bodyLen);
                            _buffer.RemoveRange(0, (int)(P_Head + bodyLen));
                            bodyLen = 0;
                            OnFile?.Invoke(content);
                        }
                        else
                        {
                            var sm = new SocketMessage() { BodyLength = bodyLen, Type = (byte)type, Content = GetContent(buffer, P_Head, (int)bodyLen) };
                            _buffer.RemoveRange(0, (int)(P_Head + bodyLen));
                            bodyLen = 0;
                            OnUnPackage?.Invoke(sm);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
        }

        public void GetReply(byte[] data, Action<bool> Commplete)
        {
            lock (_locker)
            {
                _buffer.AddRange(data);

                var buffer = _buffer.ToArray();

                if (buffer.Length >= P_Head)
                {
                    var bodyLen = GetLength(buffer);

                    var type = GetType(buffer);

                    if (bodyLen == 0) //空包认为是回复包
                    {
                        var sm = new SocketMessage() { BodyLength = bodyLen, Type = (byte)type };
                        _buffer.Clear();

                        var result = false;
                        if (sm.Type == (byte)SocketMessageType.Allow)
                        {
                            result = true;
                        }
                        Commplete?.Invoke(result);

                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
        }

        public static long GetLength(byte[] data)
        {
            return ByteHelper.ConvertToLong(data);
        }

        public static SocketMessageType GetType(byte[] data)
        {
            return (SocketMessageType)data[P_LEN];
        }

        public static byte[] GetContent(byte[] data, int offset, int count)
        {
            var buffer = new byte[count];
            Buffer.BlockCopy(data, offset, buffer, 0, count);
            return buffer;
        }

    }
}
