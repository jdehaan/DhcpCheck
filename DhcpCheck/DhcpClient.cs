using System;
using System.Net;
using System.Net.Sockets;

namespace DhcpCheck
{

    internal class DhcpClient : IDisposable
    {
        private Socket _localSocket;
        private readonly Parameters _parameters;

        public DhcpClient(Parameters parameters)
        {
            _parameters = parameters;
            CreateSocket();
        }

        private void CreateSocket()
        {
            _localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                {
                    EnableBroadcast = true,
                    ExclusiveAddressUse = false,
                    SendTimeout = _parameters.SendTimeout,
                    ReceiveTimeout = _parameters.ReceiveTimeout
                };
            _localSocket.Bind(new IPEndPoint(IPAddress.Any, 68));
        }

        public void Dispose()
        {
            _localSocket.Shutdown(SocketShutdown.Both);
            _localSocket.Close();
        }

        public void SendDiscover()
        {
            try
            {
                var dhcpcs = new IPEndPoint(IPAddress.Broadcast, 67);
                DhcpPacket dhcpDiscoverPacket = DhcpPacket.GenerateDhcpDiscoverPacket(_parameters);
                _localSocket.SendTo(dhcpDiscoverPacket.Data, dhcpcs);
            }
            catch
            {
                Rebind();
                throw;
            }
        }

        private void Rebind()
        {
            // recreate socket
            Dispose();
            CreateSocket();
        }
    }
}