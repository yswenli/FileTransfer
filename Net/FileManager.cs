using FileTransfer.Common;
using FileTransfer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FileTransfer.Net
{
    public class FileManager
    {
        FServer _receiver;

        FClient _sender;

        string _saveFile = string.Empty;

        long _current = 0;

        long _length = 0;

        FileStream _fileStream;

        bool _beginSend = false;

        bool _beginReceive = false;

        public FileManager()
        {
            _receiver = new FServer();
            _receiver.OnRequested += _receiver_OnBegin;
            _receiver.OnFile += _receiver_OnFile;
            _receiver.OnError += _receiver_OnError;
            _receiver.OnDisconnected += _receiver_OnDisconnected;
            _receiver.Start();
            ShowMonitorInfo();
        }



        #region 接收

        public delegate string OnReceiveBeginHandler(string ID, string fileName, long length);

        public event OnReceiveBeginHandler OnReceiveBegin;

        public delegate void OnReceiveEndHandler();

        public event OnReceiveEndHandler OnReceiveEnd;

        public delegate void OnReceiverDisconnectedHandler(string ID, Exception ex);

        public event OnReceiverDisconnectedHandler OnReceiverDisconnected;

        private void _receiver_OnBegin(string ID, string fileName, long length)
        {
            var saveFile = OnReceiveBegin.Invoke(ID, fileName, length);
            if (!string.IsNullOrEmpty(saveFile))
            {
                _length = length;
                if (File.Exists(saveFile))
                {
                    File.Delete(saveFile);
                }
                _fileStream = File.Create(saveFile);
                _receiver.Allow(ID);
            }
            else
                _receiver.Refuse(ID);
        }

        private void _receiver_OnFile(byte[] content)
        {
            _beginReceive = true;

            _fileStream.Write(content, 0, content.Length);
            _current += content.Length;
            if (_current == _length)
            {
                _fileStream.Flush();
                _fileStream.Close();
                _current = 0;
                _length = 0;
                _beginReceive = false;
                OnReceiveEnd?.Invoke();
            }
        }

        private void _receiver_OnError(string ID, Exception ex)
        {

        }

        private void _receiver_OnDisconnected(string ID, Exception ex)
        {
            OnReceiverDisconnected?.Invoke(ID, ex);
        }

        #endregion


        #region 发送

        public delegate void OnSendingHandler(string info);

        public event OnSendingHandler OnSending;


        public delegate void OnSendedHandler();

        public event OnSendedHandler OnSended;

        public delegate void OnSenderDisconnectedHandler(Exception ex);

        public event OnSenderDisconnectedHandler OnSenderDisconnected;


        public void Connect(string ip)
        {
            _sender = new FClient(ip);
            _sender.OnDisconnected += _sender_OnDisconnected;
            _sender.OnError += _sender_OnError;
            _sender.Connect();
        }


        public void SendFile(string fileName, Action<bool> complete)
        {
            if (File.Exists(fileName))
            {
                var fName = fileName.Substring(fileName.LastIndexOf("\\") + 1);

                var data = SerializeHelper.ByteSerialize(new FileMessage() { FileName = fName, Length = new FileInfo(fileName).Length });

                _sender.SendInfo(data, (d) =>
                {
                    if (d)
                    {
                        _beginSend = true;
                        _sender.SendFile(fileName);
                        _beginSend = false;
                        OnSended?.Invoke();
                    }
                    complete?.Invoke(d);
                });
            }
        }

        private void _sender_OnError(Exception ex)
        {

        }
        private void _sender_OnDisconnected(Exception ex)
        {
            OnSenderDisconnected?.Invoke(ex);
        }

        #endregion

        /// <summary>
        /// 监控文件管理逻辑信息
        /// </summary>
        private void ShowMonitorInfo()
        {
            new Thread(new ThreadStart(() =>
            {
                string result;

                long oldSended = 0;
                long oldRecevied = 0;

                long s_speed = 0;
                long r_speed = 0;

                while (true)
                {
                    result = string.Empty;

                    while (true)
                    {
                        if (_beginSend && _beginReceive)
                        {
                            s_speed = _sender.Out - oldSended;
                            oldSended = _sender.Out;

                            r_speed = _receiver.In - oldRecevied;
                            oldRecevied = _receiver.In;

                            result = string.Format("总数：{0} 已发送：{1} 发送速度：{2}/s 接收：{3} 接收速度：{4}/s", _receiver.Total.ToFString(), _sender.Out.ToFString(), s_speed.ToFString(), _receiver.In.ToFString(), r_speed.ToFString());
                        }
                        else if (_beginSend)
                        {
                            s_speed = _sender.Out - oldSended;
                            oldSended = _sender.Out;

                            result = string.Format("总数：{0} 发送：{1} 发送速度：{2}/s", _sender.Total.ToFString(), _sender.Out.ToFString(), s_speed.ToFString());
                        }
                        else if (_beginReceive)
                        {
                            r_speed = _receiver.In - oldRecevied;
                            oldRecevied = _receiver.In;

                            result = string.Format("总数：{0} 接收：{1} 接收速度：{2}/s", _receiver.Total.ToFString(), _receiver.In.ToFString(), r_speed.ToFString());
                        }
                        else
                        {
                            break;
                        }

                        if (!string.IsNullOrEmpty(result))
                        {
                            OnSending?.Invoke(result);
                        }
                        Thread.Sleep(1000);
                    }
                    Thread.Sleep(1000);
                }
            }))
            { IsBackground = true }.Start();
        }

    }
}
