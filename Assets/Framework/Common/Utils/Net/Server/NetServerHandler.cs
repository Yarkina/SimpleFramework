using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Net.Common;

namespace Net.Server
{
    public sealed class NetServerHandler<T> : INetServerHandler where T : class, IMessageHandler, new()
    {
        bool m_Closed = false;
        ConcurrentDictionary<Connection, T> m_Connections = new ConcurrentDictionary<Connection, T>();
        void INetServerHandler.Close()
        {
            m_Closed = true;
            var tmp = m_Connections.ToArray();
            m_Connections.Clear();
            foreach (var kv in tmp)
            {
                kv.Key.Close(string.Empty);
            }
        }

        void ConnectionClose(Connection conn, string message)
        {
            T tmp;
            m_Connections.TryRemove(conn, out tmp);
        }

        void ConnectionAdd(Connection conn,T handler)
        {
            if (m_Closed)
            {
                conn.Close(string.Empty);
                return;
            }
            m_Connections.TryAdd(conn, handler);
        }

        void INetServerHandler.HandleAcceptConnected(Socket client, byte[] kiv)
        {
            if (m_Closed)
            {
                client.Close();
                return;
            }
            var handler = Activator.CreateInstance<T>();
            var conn = new Connection(client, handler, ConnectionClose, kiv);
            ConnectionAdd(conn, handler);
            conn.Initialize();
        }
    }
}
