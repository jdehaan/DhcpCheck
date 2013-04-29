using System;

namespace DhcpCheck
{
    public class DhcpPacket
    {
        private static readonly Random XidRandom;

        static DhcpPacket()
        {
            XidRandom = new Random();
        }

        public static byte[] GenerateDhcpDiscoverPacket(Parameters parameters)
        {
            // Generate a Transaction ID for this request
            var xid = new byte[4];
            XidRandom.NextBytes(xid);

            byte[] macAddress = parameters.MacAddress;

            // Actual DHCPDISCOVER packet
            var dhcpDiscoverPacket = new byte[244];

            Array.Copy(xid, 0, dhcpDiscoverPacket, 4, 4);
            Array.Copy(macAddress, 0, dhcpDiscoverPacket, 28, 6);

            // Set the OP Code to BOOTREQUEST
            dhcpDiscoverPacket[0] = 1;
            // Set the Hardware Address Type to Ethernet
            dhcpDiscoverPacket[1] = 1;
            // Set the Hardware Address Length (number of bytes)
            dhcpDiscoverPacket[2] = 6;
            // Set the Broadcast Flag
            dhcpDiscoverPacket[10] = 128;
            // Set the Magic Cookie values
            dhcpDiscoverPacket[236] = 99;   // D
            dhcpDiscoverPacket[237] = 130;  // H
            dhcpDiscoverPacket[238] = 83;   // C
            dhcpDiscoverPacket[239] = 99;   // P
            // Set the DHCPDiscover Message Type Option
            dhcpDiscoverPacket[240] = 53;
            dhcpDiscoverPacket[241] = 1;
            dhcpDiscoverPacket[242] = 1;

            // End Option
            dhcpDiscoverPacket[243] = 255;
            return dhcpDiscoverPacket;
        }
    }
}