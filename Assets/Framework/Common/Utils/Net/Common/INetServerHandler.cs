using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Net.Common
{
    public interface INetServerHandler
    {
        void Close();
        void HandleAcceptConnected(Socket client, byte[] kiv);
    }
}
