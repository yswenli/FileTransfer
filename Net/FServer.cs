using FileTransfer.Common;
using FileTransfer.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FileTransfer.Net
{
    public class FServer
    {
        Socket _listener;

        const int BUFFER_SIZE = 102400;

        private long _total;

        private long _in;

        public long Total { get => _total; set => _total = value; }
        public long In { get => _in; set => _in = value; }


        #region events

        public delegate void OnRequestHandler(string ID, string fileName, long length);

        public event OnRequestHandler OnRequested;

        public delegate void OnFileHandler(byte[] content);

        public event OnFileHandler OnFile;

        public delegate void OnErrorHandler(string ID, Exception ex);

        public event OnErrorHandler OnError;

        public delegate void OnDisconnectedHandler(string ID, Exception ex);

        public event OnDisconnectedHandler OnDisconnected;

        #endregion

        public void Start()
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(new IPEndPoint(IPAddress.Any, 39654));
            _listener.Listen(100);
            ProcessAccept(null);
        }

        private void ProcessAccept(SocketAsyncEventArgs args)
        {
            if (args == null)
            {
                args = new SocketAsyncEventArgs();
                args.Completed += ProcessAccepted;
            }
            else
            {
                args.AcceptSocket = null;
            }
            if (!_listener.AcceptAsync(args))
            {
                ProcessAccepted(_listener, args);
            }
        }

        private void ProcessAccepted(object sender, SocketAsyncEventArgs e)
        {
            var readArgs = new SocketAsyncEventArgs();
            readArgs.AcceptSocket = e.AcceptSocket;

            var buffer = new byte[BUFFER_SIZE];
            readArgs.SetBuffer(buffer, 0, BUFFER_SIZE);
            readArgs.Completed += IO_Completed;

            var userToken = new UserToken()
            {
                ID = readArgs.AcceptSocket.RemoteEndPoint.ToString(),
                Linked = DateTimeHelper.Now,
                Actived = DateTimeHelper.Now,
                Package = new PackageHelper(),
                Socket = readArgs.AcceptSocket
            };

            SessionManagercs.Add(userToken);

            readArgs.UserToken = userToken;

            if (!userToken.Socket.ReceiveAsync(readArgs))
            {
                ProcessReceived(readArgs);
            }

            //接入新的请求
            ProcessAccept(e);
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceived(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSended(e);
                    break;
                default:
                    try
                    {
                        OnError?.Invoke(e.AcceptSocket.RemoteEndPoint.ToString(), new Exception("当前操作异常，SocketAsyncOperation：" + e.LastOperation));
                    }
                    catch { }
                    break;
            }
        }

        private void ProcessReceived(SocketAsyncEventArgs e)
        {
            var userToken = (UserToken)e.UserToken;
            try
            {
                if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
                {
                    SessionManagercs.Active(userToken.ID);

                    userToken.Socket = e.AcceptSocket;

                    var data = new byte[e.BytesTransferred];

                    Buffer.BlockCopy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);

                    userToken.Package.Add(data, null, (s) =>
                    {
                        string fileName = string.Empty;

                        long length = 0;

                        if (s.Content != null)
                        {
                            var fi = SerializeHelper.ByteDeserialize<FileMessage>(s.Content);
                            fileName = fi.FileName;
                            length = fi.Length;
                        }
                        OnRequested?.Invoke(userToken.ID, fileName, length);

                        _total = length;

                    }, (b) =>
                    {
                        Interlocked.Add(ref _in, b.Length);
                        OnFile?.Invoke(b);
                    });

                    if (!userToken.Socket.ReceiveAsync(e))
                    {
                        ProcessReceived(e);
                    }
                }
                else
                {
                    Disconnected(userToken.ID, new Exception("远程主机已断开连接！"));
                }
            }
            catch (SocketException sex)
            {
                OnDisconnected?.Invoke(userToken.ID, sex);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(userToken.ID, ex);
                Disconnected(userToken.ID, ex);
            }
        }

        private void ProcessSended(SocketAsyncEventArgs e)
        {
            using (e)
            {
                e.AcceptSocket = null;
            }
        }

        private void Send(string ID, byte[] data)
        {
            var userToken = SessionManagercs.Get(ID);

            if (userToken == null || userToken.Socket == null || !userToken.Socket.Connected)
            {
                OnError?.Invoke(ID, new Exception("当前连接已被移除"));

                return;
            }

            var writeArgs = new SocketAsyncEventArgs();
            writeArgs.Completed += IO_Completed;
            writeArgs.SetBuffer(data, 0, data.Length);
            writeArgs.UserToken = userToken;

            if (!userToken.Socket.SendAsync(writeArgs))
            {
                ProcessSended(writeArgs);
            }
        }

        public void Allow(string ID)
        {
            var sm = new SocketMessage()
            {
                BodyLength = 0,
                Type = (byte)SocketMessageType.Allow
            };

            var data = sm.ToBytes();

            Send(ID, data);
        }

        public void Refuse(string ID)
        {
            var sm = new SocketMessage()
            {
                BodyLength = 0,
                Type = (byte)SocketMessageType.Refuse
            };

            var data = sm.ToBytes();

            Send(ID, data);
        }

        public void Disconnected(string ID, Exception ex = null)
        {
            try
            {
                var userToken = SessionManagercs.Get(ID);
                userToken.Socket.Close();
                userToken.Socket = null;
                SessionManagercs.Remove(ID);
                OnDisconnected?.Invoke(userToken.ID, ex ?? new Exception("服务器主动断开连接！"));
            }
            catch
            {
            }
        }
    }
}
