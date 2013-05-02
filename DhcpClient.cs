using System;
using System.Net;
using System.Net.Sockets;

namespace DhcpCheck
{

    internal class DhcpClient : IDisposable
    {
        private readonly Socket _localSocket;
        private readonly Parameters _parameters;

        public DhcpClient(Parameters parameters)
        {
            _parameters = parameters;
            _localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                {
                    EnableBroadcast = true,
                    ExclusiveAddressUse = false,
                    SendTimeout = parameters.SendTimeout,
                    ReceiveTimeout = parameters.ReceiveTimeout
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
            var dhcpcs = new IPEndPoint(IPAddress.Broadcast, 67);
            DhcpPacket dhcpDiscoverPacket = DhcpPacket.GenerateDhcpDiscoverPacket(_parameters);
            _localSocket.SendTo(dhcpDiscoverPacket.Data, dhcpcs);
        }
    }
}