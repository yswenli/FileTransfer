using FileTransfer.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FileTransfer
{
    public partial class MainForm : Form
    {
        bool _connected = false;

        bool _runing = false;

        string _localFile = string.Empty;

        FileManager _fileManager;

        delegate void InvokeHandler();

        public MainForm()
        {
            InitializeComponent();
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            openFileDialog1.Title = "请选择文件";
            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.Multiselect = false;

            saveFileDialog1.Title = "保存文件";
            saveFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            saveFileDialog1.RestoreDirectory = true;

            _fileManager = new FileManager();
            _fileManager.OnReceiveBegin += _fileManager_OnReceiveBegin;
            _fileManager.OnReceiveEnd += _fileManager_OnReceiveEnd;
            _fileManager.OnReceiverDisconnected += _fileManager_OnReceiverDisconnected;

            _fileManager.OnSending += _fileManager_OnSending;
            _fileManager.OnSended += _fileManager_OnSended;
            _fileManager.OnSenderDisconnected += _fileManager_OnSenderDisconnected;

        }

        #region fileManager events

        #region fileManager接收
        private string _fileManager_OnReceiveBegin(string ID, string fileName, long length)
        {
            if (MessageBox.Show("收到IP " + ID + " 的文件传输 " + fileName + ",确定要接收吗？", "FileTransfer", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                string saveFile = string.Empty;

                this.Invoke(new InvokeHandler(() =>
                {
                    saveFileDialog1.FileName = fileName;

                    var result = saveFileDialog1.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        saveFile = saveFileDialog1.FileName;

                        _runing = true;
                    }
                }));

                return saveFile;
            }
            return string.Empty;
        }

        private void _fileManager_OnReceiveEnd()
        {
            MessageBox.Show("文件接收完成！", "FileTransfer");

            _runing = false;

            this.Invoke(new InvokeHandler(() =>
            {
                lblDisplay.Text = "准备就绪";
            }));
        }

        private void _fileManager_OnReceiverDisconnected(string ID, Exception ex)
        {
            _connected = false;
            _runing = false;

            this.Invoke(new InvokeHandler(() =>
            {
                lblDisplay.Text = "连接已断开，ID:" + ID + " error:" + ex.Message;
                textBox1.Enabled = button1.Enabled = true;
                button2.Enabled = progressBar1.Visible = false;
            }));
        }

        #endregion

        #region fileManager 发送
        private void _fileManager_OnSending(string info)
        {
            _runing = true;

            lblDisplay.Invoke(new InvokeHandler(() =>
            {
                lblDisplay.Text = info;
            }));
        }
        private void _fileManager_OnSended()
        {
            MessageBox.Show("文件发送完成！", "FileTransfer");

            _runing = false;

            this.Invoke(new InvokeHandler(() =>
            {
                lblDisplay.Text = "准备就绪";
            }));
        }
        private void _fileManager_OnSenderDisconnected(Exception ex)
        {
            _connected = false;
            _runing = false;

            this.Invoke(new InvokeHandler(() =>
            {
                lblDisplay.Text = "连接已断开，error:" + ex.Message;
                textBox1.Enabled = button1.Enabled = true;
                button2.Enabled = progressBar1.Visible = false;
            }));
        }
        #endregion



        #endregion


        /// <summary>
        /// 连接到远程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            var ip = textBox1.Text;

            textBox1.Enabled = button1.Enabled = false;

            new Thread(new ThreadStart(() =>
            {
                try
                {
                    _fileManager.Connect(ip);
                    _connected = true;
                    this.Invoke(new InvokeHandler(() =>
                    {
                        button2.Enabled = true;
                        lblDisplay.Text = "准备就绪";
                    }));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "error");
                    this.Invoke(new InvokeHandler(() =>
                    {
                        textBox1.Enabled = button1.Enabled = true;
                    }));
                }
            }))
            { IsBackground = true }.Start();
        }

        private void textBox3_DoubleClick(object sender, EventArgs e)
        {
            if (_connected && !_runing)
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    textBox3.Text = _localFile = openFileDialog1.FileName;
                }
            }

        }

        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_localFile) && File.Exists(_localFile))
            {
                button2.Enabled = false;

                progressBar1.Visible = true;

                new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        _fileManager.SendFile(_localFile, (d) =>
                        {
                            this.Invoke(new InvokeHandler(() =>
                            {
                                button2.Enabled = true;
                                progressBar1.Visible = false;
                            }));
                            if (d)
                            {

                                MessageBox.Show("传输成功！");
                            }
                            else
                            {
                                MessageBox.Show("传输失败,对方已拒绝接收！");
                            }

                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "error");
                        this.Invoke(new InvokeHandler(() =>
                        {
                            button2.Enabled = true;
                            progressBar1.Visible = false;
                        }));
                    }
                }))
                { IsBackground = true }.Start();
            }

        }

        #region 拖拽
        private void textBox3_DragEnter(object sender, DragEventArgs e)
        {
            if (_connected && !_runing)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Link;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
        }
        private void textBox3_DragDrop(object sender, DragEventArgs e)
        {
            if (_connected && !_runing)
            {
                textBox3.Text = _localFile = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            }
            else
            {
                MessageBox.Show("当前操作有误！");
            }
        }



        #endregion


    }
}
