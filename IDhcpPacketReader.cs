namespace DhcpCheck
{
    public interface IDhcpPacketReader
    {
        void ReadPacket(byte[] data, int length);
    }
}