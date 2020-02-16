using Crypto;
using Helper;
using Net.Client;
using Net.Server;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Net.Common
{
    internal class AesKeyIVAes
    {
        public AesKeyIVAes(NetClient.SocketArgs args, Action<NetClient.SocketArgs,byte[]> conn, Action<NetClient.SocketArgs> close, byte[] kiv)
        {
            m_KeyIV = kiv;
            m_Buffer = new byte[4];
            m_BufferReceivedSize = 0;
            m_HandleConnected = conn;
            m_HandleClose = close;
            args.m_Socket.BeginReceive(m_Buffer, m_BufferReceivedSize, m_Buffer.Length - m_BufferReceivedSize, SocketFlags.None, new AsyncCallback(HandleHeadReceived), args);
        }
        Action<NetClient.SocketArgs,byte[]> m_HandleConnected;
        Action<NetClient.SocketArgs> m_HandleClose;
        byte[] m_Buffer;
        int m_BufferReceivedSize;
        byte[] m_KeyIV;
        void HandleHeadReceived(IAsyncResult ar)
        {
            var args = ar.AsyncState as NetClient.SocketArgs;
            try
            {
                int bytesRead = args.m_Socket.EndReceive(ar);
                if (bytesRead > 0)
                {
                    m_BufferReceivedSize += bytesRead;
                    if (m_BufferReceivedSize == m_Buffer.Length)
                    {
                        NetHelper.CheckReverse(m_Buffer);
                        //解包大小
                        var len = BitConverter.ToInt32(m_Buffer, 0);
                        m_Buffer = new byte[len];
                        m_BufferReceivedSize = 0;
                        args.m_Socket.BeginReceive(m_Buffer, m_BufferReceivedSize, m_Buffer.Length - m_BufferReceivedSize, SocketFlags.None, new AsyncCallback(HandleDataReceived), args);
                    }
                    else
                    {
                        args.m_Socket.BeginReceive(m_Buffer, m_BufferReceivedSize, m_Buffer.Length - m_BufferReceivedSize, SocketFlags.None, new AsyncCallback(HandleHeadReceived), args);
                    }
                }
                else
                {
                    LogHelper.Error(SocketError.NoData.ToString());
                    m_HandleClose?.Invoke(args);
                }
            }
            catch (Exception e)
            {
                LogHelper.Exception(e);
                m_HandleClose?.Invoke(args);
            }
        }
        void HandleDataReceived(IAsyncResult ar)
        {
            var args = ar.AsyncState as NetClient.SocketArgs;
            try
            {
                int bytesRead = args.m_Socket.EndReceive(ar);
                if (bytesRead > 0)
                {
                    m_BufferReceivedSize += bytesRead;
                    if (m_BufferReceivedSize == m_Buffer.Length)
                    {
                        //解Aes Key IV
                        var decrypt = new AesDecryptor(m_KeyIV, m_KeyIV);
                        var bytes = decrypt.Decrypt(m_Buffer);
                        if (!AesKeyIV.Check(bytes))
                        {
                            LogHelper.Error("Aes Key IV len error {0}", bytes.Length);
                            m_HandleClose?.Invoke(args);
                        }
                        else
                        {
                            m_HandleConnected?.Invoke(args, bytes);
                        }
                    }
                    else
                    {
                        args.m_Socket.BeginReceive(m_Buffer, m_BufferReceivedSize, m_Buffer.Length - m_BufferReceivedSize, SocketFlags.None, new AsyncCallback(HandleHeadReceived), args);
                    }
                }
                else
                {
                    LogHelper.Error(SocketError.NoData.ToString());
                    m_HandleClose?.Invoke(args);
                }
            }
            catch (Exception e)
            {
                LogHelper.Exception(e);
                m_HandleClose?.Invoke(args);
            }
        }
    }
}
