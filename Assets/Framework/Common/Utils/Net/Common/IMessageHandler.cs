using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Common
{
    public interface IMessageHandler
    {
        void HandleInitialize(Connection conn);
        void HandleConnected(bool v,string err);
        void Handle(byte[] message);
        void HandleDisconnected();
        void HandleClose(string message);
    }
}
