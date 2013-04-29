using System.Net;

namespace DhcpCheck
{
    public interface IDhcpPacketReader
    {
        void ReadPacket(EndPoint remoteEndPoint, byte[] data, int length);
    }
}