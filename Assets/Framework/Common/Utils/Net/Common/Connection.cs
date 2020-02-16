using Crypto;
using Helper;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Net.Common
{
    public sealed class Connection
    {
        public void Close(string message)
        {
            if (IsRunning)
            {
                IsRunning = false;
                try
                {
                    m_Socket?.Shutdown(SocketShutdown.Both);
                    m_Socket?.Close();
                }
                catch (Exception e)
                {
                    LogHelper.Exception(e);
                }
                m_Close?.Invoke(this, message);
                m_Handler?.HandleDisconnected();
                m_Handler?.HandleClose(message);
                m_Socket = null;
                m_Close = null;
                m_Handler = null;
                m_Buffer = null;
            }
        }

        public Connection(Socket sock, IMessageHandler handler, FunctionClose close,byte[] kiv)
        {
            IsRunning = false;
            m_Initialized = false;
            m_Socket = sock;
            m_Handler = handler;
            m_Close = close;
            m_SendIdx = 0;
            m_RecvIdx = 0;
            m_AesDecryptor = new AesDecryptor(kiv, kiv);
            m_AesEncryptor = new AesEncryptor(kiv, kiv);
            var ep = (IPEndPoint)(sock.RemoteEndPoint);
            IP = ep.Address.ToString();
            Port = ep.Port;
        }

        public void Initialize()
        {
            if (!m_Initialized)
            {
                m_Initialized = true;
                IsRunning = true;
                m_Handler.HandleInitialize(this);
                m_Handler.HandleConnected(true, string.Empty);
                m_Socket.BeginReceive(m_Buffer, 0, m_Buffer.Length, SocketFlags.None, new AsyncCallback(HandleDataReceived), m_Socket);
            }
        }

        #region Fields
        public delegate void FunctionClose(Connection conn, string message);
        FunctionClose m_Close;
        IMessageHandler m_Handler;
        Socket m_Socket;
        bool m_Initialized;
        AesDecryptor m_AesDecryptor;
        AesEncryptor m_AesEncryptor;
        #endregion

        #region Properties
        public bool IsRunning
        {
            get;
            private set;
        }
        #endregion

        #region Method

        public void Send(byte[] buffer)
        {
            BeginSend(buffer);
        }
        object m_SendLock = new object();
        void BeginSend(byte[] data)
        {
            try
            {
                //这个锁是用来保证CurSendIdx顺序的，不加锁多线程情况下会出现CurSendIdx 乱序到达目的地
                lock (m_SendLock)
                {
                    data = NetHelper.Encode(data, m_AesEncryptor, CurSendIdx);
                    m_Socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), m_Socket);
                }
            }
            catch (Exception e)
            {
                LogHelper.Exception(e);
                Close(e.Message);
            }
        }
        void SendCallback(IAsyncResult ar)
        {
            try
            {
                var s = (Socket)ar.AsyncState;
                s.EndSend(ar);
            }
            catch (Exception e)
            {
                LogHelper.Exception(e);
                Close(e.Message);
            }
        }

        void HandleDataReceived(IAsyncResult ar)
        {
            if (IsRunning)
            {
                var client = ar.AsyncState as Socket;
                try
                {
                    int bytesRead = client.EndReceive(ar);
                    if (bytesRead > 0)
                    {
                        m_BufferReceivedSize += bytesRead;
                        NetHelper.SplitPack(ref m_Buffer, ref m_BufferReceivedSize, ref m_BufferSize, PushPack);
                        client.BeginReceive(m_Buffer, m_BufferReceivedSize, m_BufferSize - m_BufferReceivedSize, SocketFlags.None, new AsyncCallback(HandleDataReceived), client);
                    }
                    else
                    {
                        Close(SocketError.NoData.ToString());
                    }
                }
                catch (Exception e)
                {
                    LogHelper.Exception(e);
                    Close(e.Message);
                }
            }
        }

        void PushPack(byte[] pack)
        {
            if (!IsRunning)
            {
                return;
            }
            pack = NetHelper.Decode(pack, m_AesDecryptor, ref m_RecvIdx);//解压缩，解加密
            m_Handler?.Handle(pack);
        }
        #endregion

        #region Encode&Decode
        byte m_SendIdx;
        int m_RecvIdx;
        byte CurSendIdx
        {
            get
            {
                byte idx = m_SendIdx;
                m_SendIdx++;
                m_SendIdx = (byte)((m_SendIdx > 0x1F) ? 0 : m_SendIdx);
                return idx;
            }
        }

        public string IP
        {
            get;
            private set;
        }

        public int Port
        {
            get;
            private set;
        }

        #endregion

        #region Buffer
        const int BufferSize = 1024;
        byte[] m_Buffer = new byte[BufferSize];
        int m_BufferReceivedSize = 0;
        int m_BufferSize = BufferSize;
        #endregion
    }
}
