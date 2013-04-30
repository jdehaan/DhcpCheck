using System;
using System.Net;
using System.Net.Sockets;

namespace DhcpCheck
{
    public class DhcpClientData
    {
        public const int PacketSize = 1024;
        private readonly byte[] _buffer;

        public EndPoint RemoteEndPoint;

        public DhcpClientData()
        {
            RemoteEndPoint = new IPEndPoint(IPAddress.Any, 68);
            _buffer = new byte[PacketSize];
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }
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

        public void SendDiscover()
        {
            var dhcpcs = new IPEndPoint(IPAddress.Broadcast, 67);
            DhcpPacket dhcpDiscoverPacket = DhcpPacket.GenerateDhcpDiscoverPacket(_parameters);
            _localSocket.SendTo(dhcpDiscoverPacket.Data, dhcpcs);
            _packetReader.ReadPacket(dhcpcs, dhcpDiscoverPacket.Data, dhcpDiscoverPacket.Length);
        }

        /// <summary>
        ///     Synchronous call
        /// </summary>
        public void ReceiveFrom()
        {
            var dhcpClientData = new DhcpClientData();
            int receiveBytes = _localSocket.ReceiveFrom(dhcpClientData.Buffer, 0, DhcpClientData.PacketSize,
                                                        SocketFlags.None, ref dhcpClientData.RemoteEndPoint);
            _packetReader.ReadPacket(dhcpClientData.RemoteEndPoint, dhcpClientData.Buffer, receiveBytes);
        }

        /// <summary>
        ///     Asynchronous call
        /// </summary>
        public void BeginReceiveFrom()
        {
            var dhcpClientData = new DhcpClientData();
            _localSocket.BeginReceiveFrom(dhcpClientData.Buffer, 0, DhcpClientData.PacketSize,
                                          SocketFlags.None, ref dhcpClientData.RemoteEndPoint, CheckPacket,
                                          dhcpClientData);
        }

        private void CheckPacket(IAsyncResult ar)
        {
            var dhcpClientData = (DhcpClientData) ar.AsyncState;

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