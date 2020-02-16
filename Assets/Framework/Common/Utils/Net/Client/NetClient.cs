using Crypto;
using Helper;
using Net.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Net.Client
{
    public sealed class NetClient
    {
        public class SocketArgs
        {
            public Socket m_Socket;
            public Connection m_Connection;
            public bool IsConnected;
            public string m_RsaPub;
            public void Close(string message)
            {
                m_Connection?.Close(message);
                m_Connection = null;
                IsConnected = false;
                if (m_Socket != null && m_Socket.Connected) { m_Socket.Close(); }
                m_Socket = null;
            }
        }

        SocketArgs m_SocketArgs;
        IMessageHandler m_Handler;

        public bool IsRunning
        {
            get {
                return m_SocketArgs != null;
            }
        }
        
        public bool IsConnected
        {
            get
            {
                if(m_SocketArgs == null)
                {
                    return false;
                }
                return m_SocketArgs.IsConnected;
            }
        }

        private NetClient(IMessageHandler handler)
        {
            m_Handler = handler;
        }

        public static NetClient Create<T>() where T : class, IMessageHandler, new()
        {
            var handler = Activator.CreateInstance<T>();
            return new NetClient(handler);
        }

        public static NetClient Create(IMessageHandler handler)
        {
            return new NetClient(handler);
        }
        
        public void Connect(string addr, int port,string pub, bool async = true)
        {
            var ip = NetHelper.ParseIpAddressV6(addr);
            if (ip == null)
            {
                throw new Exception("Unknown addr = " + addr);
            }
            Close(string.Empty);
            
            m_SocketArgs = new SocketArgs();
            m_SocketArgs.m_RsaPub = pub;
            if (!Rsa.CheckIsPub(pub))
            {
                throw new Exception(string.Format("RsaPub error {0}", pub));
            }
            m_SocketArgs.m_Socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            m_SocketArgs.m_Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            m_SocketArgs.m_Socket.NoDelay = true;
            LogHelper.Debug("NetClient Connect {0}:{1}", ip, port);
            if (async)
            {
                m_SocketArgs.m_Socket.BeginConnect(new IPEndPoint(ip, port), new AsyncCallback(ConnectCallback), m_SocketArgs);
            }
            else
            {
                m_SocketArgs.m_Socket.Connect(new IPEndPoint(ip, port));
                LogHelper.Debug("NetClient Connect {0}", "OK");
                OnConnected(m_SocketArgs);
            }
        }

        void OnConnected(SocketArgs args)
        {
            if (m_SocketArgs != args) return;
            var kiv = AesKeyIV.GenKeyIV();
            AesKeyIV.SendAesKeyIVRsa(args.m_Socket, args.m_RsaPub, kiv);
            new AesKeyIVAes(args, KIVHandleConnected, KIVHandleClose, kiv);
        }
        
        void KIVHandleClose(SocketArgs args)
        {
            args.Close(string.Empty);
            if (args != m_SocketArgs) return;
            Close(string.Empty);
        }
        void KIVHandleConnected(SocketArgs args,byte[] kiv)
        {
            if (args != m_SocketArgs) return;
            args.IsConnected = true;
            args.m_Connection = new Connection(args.m_Socket, m_Handler, ConnectionClose, kiv);
            args.m_Connection.Initialize();
        }

        void ConnectionClose(Connection conn, string message)
        {
            if (conn == m_SocketArgs?.m_Connection)
            {
                Close(string.Empty);
            }
        }
        void ConnectCallback(IAsyncResult ar)
        {
            LogHelper.Debug("NetClient ConnectCallback");
            var args = ar.AsyncState as SocketArgs;
            if(args != m_SocketArgs)
            {
                try
                {
                    args.m_Socket?.EndConnect(ar);
                    args.m_Socket?.Close();
                }catch(Exception e)
                {
                    LogHelper.Exception(e);
                }
                return;
            }
            try
            {
                var s = args.m_Socket;
                s.EndConnect(ar);
                OnConnected(args);
            }
            catch (Exception e)
            {
                LogHelper.Exception(e);
                Close(e.Message);
                m_Handler.HandleConnected(false, e.Message);
                m_Handler.HandleClose(e.Message);
            }
        }
        public void Close(string message)
        {
            if (m_SocketArgs != null)
            {
                m_SocketArgs.Close(message);
                m_SocketArgs = null;
            }
        }
    }
}
