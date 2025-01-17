//------------------------------------------------------------------------------
//  此代码版权（除特别声明或在RRQMCore.XREF命名空间的代码）归作者本人若汝棋茗所有
//  源代码使用协议遵循本仓库的开源协议及附加协议，若本仓库没有设置，则按MIT开源协议授权
//  CSDN博客：https://blog.csdn.net/qq_40374647
//  哔哩哔哩视频：https://space.bilibili.com/94253567
//  Gitee源代码仓库：https://gitee.com/RRQM_Home
//  Github源代码仓库：https://github.com/RRQM
//  API首页：https://www.yuque.com/eo2w71/rrqm
//  交流QQ群：234762506
//  感谢您的下载和使用
//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
using RRQMCore.Dependency;
using RRQMCore.Log;

namespace RRQMSocket
{
    /// <summary>
    /// 通讯基类
    /// </summary>
    public abstract class BaseSocket : RRQMDependencyObject, ISocket
    {
        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILog logger;

        private int bufferLength;

        /// <summary>
        /// 数据交互缓存池限制，min=1024 byte
        /// </summary>
        public int BufferLength
        {
            get => this.bufferLength;
            set
            {
                if (value < 1024)
                {
                    value = 1024 * 10;
                }
                this.bufferLength = value;
            }
        }

        /// <summary>
        /// 日志记录器
        /// </summary>
        public ILog Logger
        {
            get => this.logger;
            set => this.logger = value;
        }
    }
}