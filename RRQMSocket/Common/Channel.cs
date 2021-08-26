﻿using RRQMCore.ByteManager;
using RRQMCore.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RRQMSocket
{
    /// <summary>
    /// 通道
    /// </summary>
    public class Channel : IDisposable
    {
        internal byte[] current;
        internal int id = 0;
        internal ConcurrentDictionary<int, Channel> parent;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BytePool bytePool;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ConcurrentQueue<ChannelData> dataQueue;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ProcotolHelper procotolHelper;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly AutoResetEvent waitHandle;

        private int bufferLength;
        private ChannelStatus status;
        internal Channel(ProcotolHelper procotolHelper)
        {
            this.status = ChannelStatus.Success;
            this.procotolHelper = procotolHelper;
            this.dataQueue = new ConcurrentQueue<ChannelData>();
            this.waitHandle = new AutoResetEvent(false);
            this.bytePool = procotolHelper.Client.BytePool;
            this.bufferLength = procotolHelper.Client.BufferLength;
        }

        /// <summary>
        /// 获取当前的数据
        /// </summary>
        public byte[] Current
        {
            get { return current; }
        }

        /// <summary>
        /// ID
        /// </summary>
        public int ID
        {
            get { return id; }
        }

        /// <summary>
        /// 状态
        /// </summary>
        public ChannelStatus Status
        {
            get { return status; }
        }

        /// <summary>
        /// 取消
        /// </summary>
        public void Cancel()
        {
            ByteBlock byteBlock = this.bytePool.GetByteBlock(this.bufferLength);
            try
            {
                byteBlock.Write(this.id);
                this.procotolHelper.SocketSend(-5, byteBlock.Buffer, 0, byteBlock.Len, false);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                byteBlock.Dispose();
            }
        }

        /// <summary>
        /// 完成操作
        /// </summary>
        public void Complete()
        {
            this.RequestComplete();
            ByteBlock byteBlock = this.bytePool.GetByteBlock(this.bufferLength);
            try
            {
                byteBlock.Write(this.id);
                this.procotolHelper.SocketSend(-4, byteBlock.Buffer, 0, byteBlock.Len, false);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                byteBlock.Dispose();
            }
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            ByteBlock byteBlock = this.bytePool.GetByteBlock(this.bufferLength);
            try
            {
                this.RequestDispose();
                byteBlock.Write(this.id);
                this.procotolHelper.SocketSend(-6, byteBlock.Buffer, 0, byteBlock.Len, false);
            }
            catch
            {

            }
            finally
            {
                byteBlock.Dispose();
            }
        }

        /// <summary>
        /// 转向下个元素
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool MoveNext(int timeout = 60 * 1000)
        {
            if (this.status != ChannelStatus.Success)
            {
                return false;
            }
            if (this.dataQueue.TryDequeue(out ChannelData channelData))
            {
                switch (channelData.type)
                {
                    case -3:
                        {
                            this.current = channelData.data;
                            return true;
                        }
                    case -4:
                        {
                            this.RequestComplete();
                            break;
                        }
                    case -5:
                        {
                            this.RequestCancel();
                            break;
                        }
                    case -6:
                        {
                            this.RequestDispose();
                            break;
                        }
                    default:
                        break;
                }

                return false;
            }
            else
            {
                this.waitHandle.Reset();
                if (this.waitHandle.WaitOne(timeout))
                {
                   return this.MoveNext();
                }
                else
                {
                    this.status = ChannelStatus.Timeout;
                    return false;
                }
            }
        }

        /// <summary>
        /// 转向下个元素
        /// </summary>
        /// <returns></returns>
        public Task<bool> MoveNextAsync(int timeout = 60 * 1000)
        {
            return Task.Run(() =>
             {
                 return this.MoveNext(timeout);
             });
        }

        /// <summary>
        /// 写入通道
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void Write(byte[] data, int offset, int length)
        {
            if (this.status != ChannelStatus.Success)
            {
                throw new RRQMException("通道已不允许使用");
            }
            ByteBlock byteBlock = this.bytePool.GetByteBlock(length + 4);
            try
            {
                byteBlock.Write(this.id);
                byteBlock.WriteBytesPackage(data, offset, length);
                this.procotolHelper.SocketSend(-3, byteBlock.Buffer, 0, byteBlock.Len, false);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                byteBlock.Dispose();
            }
        }

        /// <summary>
        /// 写入通道
        /// </summary>
        /// <param name="data"></param>
        public void Write(byte[] data)
        {
            this.Write(data, 0, data.Length);
        }

        /// <summary>
        /// 写入通道
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void WriteAsync(byte[] data, int offset, int length)
        {
            if (this.status != ChannelStatus.Success)
            {
                throw new RRQMException("通道已不允许使用");
            }
            ByteBlock byteBlock = this.bytePool.GetByteBlock(length + 4);
            try
            {
                byteBlock.Write(this.id);
                byteBlock.WriteBytesPackage(data, offset, length);
                this.procotolHelper.SocketSendAsync(-3, byteBlock.Buffer, 0, byteBlock.Len);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                byteBlock.Dispose();
            }
        }

        /// <summary>
        /// 写入通道
        /// </summary>
        /// <param name="data"></param>
        public void WriteAsync(byte[] data)
        {
            this.WriteAsync(data, 0, data.Length);
        }

        internal void ReceivedData(ChannelData data)
        {
            this.dataQueue.Enqueue(data);
            this.waitHandle.Set();
        }

        private void RequestCancel()
        {
            this.current = null;
            this.status = ChannelStatus.Cancel;
            this.waitHandle.Set();
        }

        private void RequestComplete()
        {
            this.current = null;
            this.status = ChannelStatus.Completed;
            this.waitHandle.Set();
        }

        private void RequestDispose()
        {
            this.current = null;
            this.waitHandle.Dispose();
            this.status = ChannelStatus.Disposed;
            this.waitHandle.Set();
            this.parent.TryRemove(this.id, out _);
        }
    }
}