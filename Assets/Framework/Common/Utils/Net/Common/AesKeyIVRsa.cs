using Crypto;
using Helper;
using Net.Server;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Net.Common
{
    internal class AesKeyIVRsa
    {
        public AesKeyIVRsa(Socket sock, INetServerHandler handler, string rsa)
        {
            m_Handler = handler;
            m_RsaKey = rsa;
            m_Buffer = new byte[4];
            m_BufferReceivedSize = 0;
            sock.BeginReceive(m_Buffer, m_BufferReceivedSize, m_Buffer.Length - m_BufferReceivedSize, SocketFlags.None, new AsyncCallback(HandleHeadReceived), sock);
        }
        byte[] m_Buffer;
        int m_BufferReceivedSize;
        INetServerHandler m_Handler;
        string m_RsaKey;
        void HandleHeadReceived(IAsyncResult ar)
        {
            var sock = ar.AsyncState as Socket;
            try
            {
                int bytesRead = sock.EndReceive(ar);
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
                        sock.BeginReceive(m_Buffer, m_BufferReceivedSize, m_Buffer.Length - m_BufferReceivedSize, SocketFlags.None, new AsyncCallback(HandleDataReceived), sock);
                    }
                    else
                    {
                        sock.BeginReceive(m_Buffer, m_BufferReceivedSize, m_Buffer.Length - m_BufferReceivedSize, SocketFlags.None, new AsyncCallback(HandleHeadReceived), sock);
                    }
                }
                else
                {
                    LogHelper.Error(SocketError.NoData.ToString());
                    sock.Close();
                }
            }
            catch (Exception e)
            {
                LogHelper.Exception(e);
                sock.Close();
            }
        }
        void HandleDataReceived(IAsyncResult ar)
        {
            var sock = ar.AsyncState as Socket;
            try
            {
                int bytesRead = sock.EndReceive(ar);
                if (bytesRead > 0)
                {
                    m_BufferReceivedSize += bytesRead;
                    if (m_BufferReceivedSize == m_Buffer.Length)
                    {
                        //解Aes Key IV
                        byte[] bytes;
                        if (!Rsa.Decrypt(m_RsaKey, m_Buffer, out bytes))
                        {
                            LogHelper.Error("Rsa Decrypt Error {0}", m_RsaKey);
                            sock.Close();
                            return;
                        }
                        if (!AesKeyIV.Check(bytes))
                        {
                            LogHelper.Error("Aes Key IV len error {0}", bytes.Length);
                            sock.Close();
                        }
                        else
                        {
                            var kiv = AesKeyIV.GenKeyIV();
                            AesKeyIV.SendAesKeyIVAes(sock, bytes, kiv);
                            m_Handler.HandleAcceptConnected(sock, kiv);
                        }
                    }
                    else
                    {
                        sock.BeginReceive(m_Buffer, m_BufferReceivedSize, m_Buffer.Length - m_BufferReceivedSize, SocketFlags.None, new AsyncCallback(HandleHeadReceived), sock);
                    }
                }
                else
                {
                    LogHelper.Error(SocketError.NoData.ToString());
                    sock.Close();
                }
            }
            catch (Exception e)
            {
                LogHelper.Exception(e);
                sock.Close();
            }
        }
    }
}
