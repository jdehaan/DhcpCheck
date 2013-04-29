using System;
using System.Net;
using System.Net.Sockets;

namespace DhcpCheck
{
    public class DhcpClientData
    {
        public const int PacketSize = 1024;

        public DhcpClientData()
        {
            RemoteEndPoint = new IPEndPoint(IPAddress.Any, 68);
            _buffer = new byte[PacketSize];
        }

        public EndPoint RemoteEndPoint;
        public byte[] Buffer { get { return _buffer; } }

        private readonly byte[] _buffer;
    }

    internal class DhcpClient : IDisposable
    {
        private readonly Socket _localSocket;
        private readonly IDhcpPacketReader _packetReader;
        private readonly Parameters _parameters;

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
            _localSocket.Bind(new IPEndPoint(IPAddress.Any, 68));
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
            var data = new DhcpClientData();
            _localSocket.BeginReceiveFrom(data.Buffer, 0, DhcpClientData.PacketSize,
                0, ref data.RemoteEndPoint, CheckPacket, data);
        }

        private void CheckPacket(IAsyncResult ar)
        {
            var dhcpClientData = (DhcpClientData)ar.AsyncState;

            try
            {
                BeginReceiveFrom();
                int receiveBytes = _localSocket.EndReceiveFrom(ar, ref dhcpClientData.RemoteEndPoint);
                _packetReader.ReadPacket(dhcpClientData.RemoteEndPoint, dhcpClientData.Buffer, receiveBytes);
            }
            catch (Exception ex)
            {
            }
        }
    }
}