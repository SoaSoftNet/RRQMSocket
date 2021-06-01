//------------------------------------------------------------------------------
//  此代码版权归作者本人若汝棋茗所有
//  源代码使用协议遵循本仓库的开源协议及附加协议，若本仓库没有设置，则按MIT开源协议授权
//  CSDN博客：https://blog.csdn.net/qq_40374647
//  哔哩哔哩视频：https://space.bilibili.com/94253567
//  Gitee源代码仓库：https://gitee.com/RRQM_Home
//  Github源代码仓库：https://github.com/RRQM
//  交流QQ群：234762506
//  感谢您的下载和使用
//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
using RRQMCore.ByteManager;
using RRQMCore.Exceptions;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RRQMSocket
{
    /// <summary>
    /// 需要验证的TCP客户端
    /// </summary>
    public class TokenClient : TcpClient
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public TokenClient() : this(new BytePool(1024 * 1024 * 1000, 1024 * 1024 * 20))
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="bytePool">设置内存池实例</param>
        public TokenClient(BytePool bytePool) : base(bytePool)
        {
        }

        /// <summary>
        /// 判断是否已连接
        /// </summary>
        public override bool Online { get { return online; } }

        private bool online;

        private string verifyToken = "rrqm";

        /// <summary>
        /// 验证令箭,当为null或空时，重置为默认值“rrqm”
        /// </summary>
        public string VerifyToken
        {
            get { return verifyToken; }
            set
            {
                if (value == null || value == string.Empty)
                {
                    value = "rrqm";
                }
                verifyToken = value;
            }
        }

        /// <summary>
        /// 获取服务器分配的ID
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// 获取或设置验证超时时间,默认为3秒；
        /// </summary>
        public int VerifyTimeout { get; set; } = 3;


        /// <summary>
        /// 连接到服务器
        /// </summary>
        /// <param name="iPHost"></param>
        /// <exception cref="RRQMException"></exception>
        /// <exception cref="RRQMTimeoutException"></exception>
        public override void Connect(IPHost iPHost)
        {
            if (this.disposable)
            {
                throw new RRQMException("无法重新利用已释放对象");
            }


            try
            {
                Socket socket = new Socket(iPHost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                PreviewConnect(socket);
                socket.Connect(iPHost.EndPoint);
                this.MainSocket = socket;
                this.MainSocket.Send(Encoding.UTF8.GetBytes(this.VerifyToken == null ? string.Empty : this.VerifyToken));
            }
            catch (Exception e)
            {
                throw new RRQMException(e.Message);
            }

            int waitCount = 0;
            while (waitCount < VerifyTimeout * 1000 / 10)
            {
                if (this.MainSocket.Available > 0)
                {
                    ByteBlock byteBlock = this.BytePool.GetByteBlock(this.BufferLength);
                    try
                    {
                        int r = this.MainSocket.Receive(byteBlock.Buffer);
                        if (r > 0)
                        {
                            if (byteBlock.Buffer[0] == 1)
                            {
                                this.ID = Encoding.UTF8.GetString(byteBlock.Buffer, 1, r - 1);
                                Start();
                                this.online = true;
                                return;
                            }
                            else if (byteBlock.Buffer[0] == 2)
                            {
                                throw new RRQMException(Encoding.UTF8.GetString(byteBlock.Buffer, 1, r - 1));
                            }
                            else if (byteBlock.Buffer[0] == 3)
                            {
                                throw new RRQMException("连接数量已达到服务器设定最大值");
                            }
                        }
                    }
                    finally
                    {
                        byteBlock.Dispose();
                    }
                }
                waitCount++;
                Thread.Sleep(10);
            }

            throw new RRQMTimeoutException("验证Token超时");
        }
    }
}