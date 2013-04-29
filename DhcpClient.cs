using System;
using System.Net;
using System.Net.Sockets;

namespace DhcpCheck
{
    internal class DhcpClient : IDisposable
    {
        public const int PacketSize = 1024;
        private readonly Socket _localSocket;
        private readonly IDhcpPacketReader _packetReader;
        private readonly Parameters _parameters;
        private EndPoint _dhcpsc;
        private int _receiveBytes = 0;
        private readonly byte[] _receiveBuffer = new byte[PacketSize];

        public DhcpClient(Parameters parameters, IDhcpPacketReader packetReader)
        {
            _parameters = parameters;
            _packetReader = packetReader;

            _localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                {
                    EnableBroadcast = true,
                    ExclusiveAddressUse = false,
                    SendTimeout = parameters.SendTimeout,
                    ReceiveTimeout = parameters.ReceiveTimeout
                };

            _dhcpsc = new IPEndPoint(IPAddress.Any, 68);
            _localSocket.Bind(_dhcpsc);
        }

        public void Dispose()
        {
            _localSocket.Shutdown(SocketShutdown.Both);
            _localSocket.Close();
        }

        public void Discover()
        {
            var dhcpcs = new IPEndPoint(IPAddress.Broadcast, 67);

            byte[] dhcpDiscoverPacket = DhcpPacket.GenerateDhcpDiscoverPacket(_parameters);

            _localSocket.SendTo(dhcpDiscoverPacket, dhcpcs);
        }

        internal void BeginReceiveFrom()
        {
            _localSocket.BeginReceiveFrom(_receiveBuffer, 0, PacketSize, 0, ref _dhcpsc, CheckPacket, _localSocket);
        }

        private void CheckPacket(IAsyncResult ar)
        {
            var remoteSocket = (Socket) ar.AsyncState;

            try
            {
                _receiveBytes = remoteSocket.EndReceiveFrom(ar, ref _dhcpsc);
                _packetReader.ReadPacket(_receiveBuffer, _receiveBytes);
                BeginReceiveFrom();
            }
            catch (Exception ex)
            {
            }
        }
    }
}