using Crypto;
using Helper;
using Net.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Net.Server
{
    public sealed class NetServer
    {
        Socket m_Socket;
        INetServerHandler m_Handler;

        public bool IsRunning
        {
            get;
            private set;
        }

        public IPAddress Address
        {
            get;
            private set;
        }

        public int Port
        {
            get;
            private set;
        }

        public string RsaKey
        {
            get;
            private set;
        }

        public static NetServer Create<T>(int listenPort,string rsaKey) where T : class, IMessageHandler, new()
        {
            var handler = new NetServerHandler<T>();
            return new NetServer(handler, listenPort, rsaKey);
        }

        public static NetServer Create<T>(IPAddress localIPAddress, int listenPort, string rsaKey) where T : class, IMessageHandler, new()
        {
            var handler = new NetServerHandler<T>();
            return new NetServer(handler, localIPAddress, listenPort, rsaKey);
        }

        public static NetServer Create(INetServerHandler handler, int listenPort, string rsaKey)
        {
            return new NetServer(handler, listenPort, rsaKey);
        }

        public static NetServer Create(INetServerHandler handler, IPAddress localIPAddress, int listenPort, string rsaKey)
        {
            return new NetServer(handler, localIPAddress, listenPort, rsaKey);
        }

        private NetServer(INetServerHandler handler, int listenPort, string rsaKey)
            : this(handler, IPAddress.Any, listenPort, rsaKey)
        {
        }

        private NetServer(INetServerHandler handler, IPEndPoint localEP, string rsaKey)
            : this(handler, localEP.Address, localEP.Port, rsaKey)
        {
        }

        private NetServer(INetServerHandler handler, IPAddress localIPAddress, int listenPort, string rsaKey)
        {
            m_Handler = handler;
            Address = localIPAddress;
            Port = listenPort;
            RsaKey = rsaKey;
            if (!Rsa.CheckIsKey(rsaKey))
            {
                throw new Exception(string.Format("RasKey error {0}", rsaKey));
            }
            m_Socket = new Socket(Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }
        public void Start(int backlog = 1024)
        {
            if (!IsRunning)
            {
                IsRunning = true;
                m_Socket.Bind(new IPEndPoint(Address, Port));
                m_Socket.Listen(backlog);
                m_Socket.BeginAccept(new AsyncCallback(HandleAcceptConnected), m_Socket);
                LogHelper.Debug("NetServer Start {0}:{1},backlog:{2}", Address, Port, backlog);
            }
        }
        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                m_Handler.Close();
                m_Socket.Close();
            }
        }

        void HandleAcceptConnected(IAsyncResult ar)
        {
            if (IsRunning)
            {
                var server = ar.AsyncState as Socket;
                var client = server.EndAccept(ar);
                LogHelper.Debug("NetServer HandleAcceptConnected {0}:{1}", (client.RemoteEndPoint as IPEndPoint).Address, (client.RemoteEndPoint as IPEndPoint).Port);
                new AesKeyIVRsa(client, m_Handler, RsaKey);
                server.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                server.NoDelay = true;
                server.BeginAccept(new AsyncCallback(HandleAcceptConnected), server);
            }
        }
    }
}
