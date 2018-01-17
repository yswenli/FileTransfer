using FileTransfer.Common;
using FileTransfer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FileTransfer.Net
{
    public class FClient
    {
        Socket _socket;

        string _ip = string.Empty;

        const int BUFFER_SIZE = 102400;

        private PackageHelper Packager;

        private Action<bool> OnComlete;

        private long _total;

        private long _out;

        public DateTime Actived
        {
            get; set;
        }

        //心跳间隔
        int HeartSpan = 1;

        public bool IsConnected
        {
            get; set;
        } = false;
        public long Total { get => _total; set => _total = value; }
        public long Out { get => _out; set => _out = value; }

        public delegate void OnDisconnectedHandler(Exception ex);

        public event OnDisconnectedHandler OnDisconnected;

        public delegate void OnErrorHandler(Exception ex);

        public event OnErrorHandler OnError;

        public FClient(string ip)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _ip = ip;
            Packager = new PackageHelper();
        }

        public void Connect()
        {
            _socket.Connect(new IPEndPoint(IPAddress.Parse(_ip), 39654));
            IsConnected = true;
            Actived = DateTimeHelper.Now;
            HeartAsync();
            ProcessReceived();
        }

        private void ProcessReceived()
        {
            try
            {
                var buffer = new byte[BUFFER_SIZE];

                new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        while (true)
                        {
                            if (_socket.Available > 0 && IsConnected)
                            {
                                var readNum = _socket.Receive(buffer);

                                var data = new byte[readNum];

                                Buffer.BlockCopy(buffer, 0, data, 0, readNum);

                                Packager.GetReply(buffer, (d) =>
                                {
                                    OnComlete?.Invoke(d);
                                });
                            }
                            else
                            {
                                Thread.Sleep(1);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        IsConnected = false;
                        OnDisconnected?.Invoke(ex);
                    }
                }))
                { IsBackground = true }.Start();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
        }

        private void SendAsync(SocketMessage sm)
        {
            var data = sm.ToBytes();

            var sendNum = 0;

            int offset = 0;

            try
            {

                while (IsConnected)
                {
                    sendNum += _socket.Send(data, offset, data.Length - offset, SocketFlags.None);

                    offset += sendNum;

                    if (sendNum == data.Length)
                    {
                        break;
                    }
                }
                Actived = DateTimeHelper.Now;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                OnDisconnected?.Invoke(ex);
            }

        }

        private void SendFileAsync(byte[] content)
        {
            var sm = SocketMessage.ParseStream(content);
            SendAsync(sm);
        }

        public void SendInfo(byte[] content, Action<bool> onComlete)
        {
            var sm = SocketMessage.Parse(content);
            SendAsync(sm);
            OnComlete = onComlete;
        }

        private void HeartAsync()
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    while (true)
                    {
                        if (Actived.AddSeconds(HeartSpan) <= DateTimeHelper.Now)
                        {
                            var sm = new SocketMessage()
                            {
                                BodyLength = 0,
                                Type = (byte)SocketMessageType.Heart
                            };
                            SendAsync(sm);
                        }
                        Thread.Sleep(HeartSpan * 1000);
                    }
                }
                catch { }
            }))
            { IsBackground = true }.Start();
        }

        public void SendFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                var buffer = new byte[BUFFER_SIZE];

                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    _total = fs.Length;

                    int readNum = 0;

                    long offset = 0;

                    do
                    {
                        fs.Position = offset;

                        readNum = fs.Read(buffer, 0, BUFFER_SIZE);

                        offset += readNum;

                        if (readNum > 0)
                        {
                            var content = new byte[readNum];

                            Buffer.BlockCopy(buffer, 0, content, 0, readNum);

                            SendFileAsync(content);

                            Interlocked.Add(ref _out, readNum);
                        }
                        else
                            break;
                    }
                    while (true);
                }
            }
        }


        public void Disconnect()
        {
            try
            {
                IsConnected = false;
                _socket.Close();
                _socket = null;
            }
            catch { }
        }


    }
}
