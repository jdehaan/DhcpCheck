using System;
using System.IO;
using System.Net;
using System.Text;

namespace DhcpCheck
{
    public class DhcpPacket
    {
        private static readonly Random XidRandom;
        private readonly byte[] _data;
        private readonly int _length;

        static DhcpPacket()
        {
            XidRandom = new Random();
        }

        public DhcpPacket(byte[] data, int length)
        {
            _data = data;
            _length = length;
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public int Length
        {
            get { return _length; }
        }

        public static DhcpPacket GenerateDhcpDiscoverPacket(Parameters parameters)
        {
            var packet = new DhcpPacket(new byte[244], 244);

            // Generate a Transaction ID for this request
            var xid = new byte[4];
            xid[0] = 0xE7;
            xid[1] = 0x03;
            xid[2] = 0x00;
            xid[3] = 0x00;
            //XidRandom.NextBytes(xid);

            byte[] macAddress = parameters.MacAddress;

            // Actual DHCPDISCOVER packet
            var data = packet._data;

            Array.Copy(xid, 0, data, 4, 4);
            Array.Copy(macAddress, 0, data, 28, 6);

            // Set the OP Code to BOOTREQUEST
            data[0] = 1;
            // Set the Hardware Address Type to Ethernet
            data[1] = 1;
            // Set the Hardware Address Length (number of bytes)
            data[2] = 6;
            // Set the Broadcast Flag
            data[10] = 128;
            // Set the Magic Cookie values
            data[236] = 99; // D
            data[237] = 130; // H
            data[238] = 83; // C
            data[239] = 99; // P
            // Set the DHCPDiscover Message Type Option
            data[240] = 53;
            data[241] = 1;
            data[242] = 1;

            // End Option
            data[243] = 255;
            return packet;
        }

        public void ReadPacket(Parameters parameters, EndPoint remoteEndPoint)
        {
            //Field      DHCPOFFER            
            //-----      ---------            
            //'op'       BOOTREPLY            
            //'htype'    (From "Assigned Numbers" RFC)
            //'hlen'     (Hardware address length in octets)
            //'hops'     0                    
            //'xid'      'xid' from client DHCPDISCOVER message              
            //'secs'     0
            //'ciaddr'   0
            //                                
            //'yiaddr'   IP address offered to client            
            //'siaddr'   IP address of next bootstrap server     
            //'flags'    'flags' from client DHCPDISCOVER message
            //'giaddr'   'giaddr' from client DHCPDISCOVER message              
            //'chaddr'   'chaddr' from client DHCPDISCOVER message              
            //'sname'    Server host name or options
            //'file'     Client boot file name or options
            //'options'  options

            var binaryReader = new BinaryReader(new MemoryStream(_data));
            string serverIp = "?";
            string leaseTime = "?";
            string renewalTime = "?";
            string rebindingTime = "?";
            string subnetMask = "?";
            string routers = "?";
            string dnsServers = "?";
            string domainName = "?";
            byte otype = 0;

            // skip to xid
            binaryReader.ReadBytes(4);

            // xid chosen by client
            uint xid = binaryReader.ReadUInt32();

            // skip to ciaddr
            binaryReader.ReadBytes(4);

            string ciaddr = ReadIpv4(binaryReader);
            string yiaddr = ReadIpv4(binaryReader);

            // skip 220 bytes to options
            binaryReader.ReadBytes(220);

            while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
            {
                byte optionCode = binaryReader.ReadByte();

                if (optionCode != 0 && optionCode != 255)
                {
                    byte optionLength = binaryReader.ReadByte();

                    switch (optionCode)
                    {
                            // Subnet Mask
                        case 1:
                            subnetMask = ReadIpv4(binaryReader);
                            break;

                        case 3:
                            routers = ReadIpv4List(optionLength, binaryReader);
                            break;

                        case 6:
                            dnsServers = ReadIpv4List(optionLength, binaryReader);
                            break;

                        case 15:
                            domainName = Encoding.ASCII.GetString(binaryReader.ReadBytes(optionLength), 0,
                                                                  optionLength - 1);
                            break;

                            // lease time
                        case 51:
                            leaseTime = ReadTimeInterval(binaryReader);
                            break;

                            // DHCP message type
                        case 53:
                            otype = binaryReader.ReadByte();
                            //1     DHCPDISCOVER
                            //2     DHCPOFFER
                            //3     DHCPREQUEST
                            //4     DHCPDECLINE
                            //5     DHCPACK
                            //6     DHCPNAK
                            //7     DHCPRELEASE
                            break;

                            // DHCP server identifier
                        case 54:
                            serverIp = ReadIpv4(binaryReader);
                            break;

                        case 58:
                            renewalTime = ReadTimeInterval(binaryReader);
                            break;

                        case 59:
                            rebindingTime = ReadTimeInterval(binaryReader);
                            break;

                        case 24: // MTU aging, ignore
                        case 46: // Netbios stuff ignore
                        case 81: // Client FQDN feature ignored
                        case 254: // extensions, ignore
                            binaryReader.ReadBytes(optionLength);
                            break;

                        default:
                            {
                                Console.WriteLine("Option {0} not supported yet (length={1})", optionCode, optionLength);
                                binaryReader.ReadBytes(optionLength);
                                break;
                            }
                    }
                }
            }

            string loggingText = null;
            switch (otype)
            {
                case 1:
                    loggingText = string.Format("{0:yyyyMMdd HHmmss}\tDSCVR\t{1:X8}\t{2}\r\n",
                                                DateTime.Now, xid, ciaddr);
                    break;
                case 2:
                    loggingText =
                        string.Format(
                            "{0:yyyyMMdd HHmmss}\tOFFER\t{11:X8}\t{1}({2})\t{3}\t{4}\t{5}\t{6}\t{7}\tDN={8}\tDNS={9}\tGW={10}\r\n",
                            DateTime.Now, serverIp, remoteEndPoint, yiaddr,
                            leaseTime, renewalTime, rebindingTime,
                            subnetMask,
                            domainName, dnsServers, routers,
                            xid);
                    break;
                case 5:
                    loggingText = string.Format("{0:yyyyMMdd HHmmss}\tDHCPACK\t{1:X8}\t{2}\t{3}\r\n",
                                                DateTime.Now, xid, ciaddr, yiaddr);
                    break;
                default:
                    Console.WriteLine("No handled: DHCP message type {0}", otype);
                    break;
            }

            if (loggingText != null)
            {
                File.AppendAllText(parameters.Logfile, loggingText);
                Console.Write(loggingText);
            }
        }

        private static string ReadIpv4List(byte optionLength, BinaryReader binaryReader)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < optionLength/4; i++)
            {
                sb.Append(ReadIpv4(binaryReader));
                sb.Append(",");
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        private static string ReadIpv4(BinaryReader binaryReader)
        {
            byte[] ipv4 = binaryReader.ReadBytes(4);
            return String.Format("{0}.{1}.{2}.{3}", ipv4[0], ipv4[1], ipv4[2], ipv4[3]);
        }

        private static string ReadTimeInterval(BinaryReader binaryReader)
        {
            return Convert.ToString(
                (binaryReader.ReadByte()*Math.Pow(256, 3)) +
                (binaryReader.ReadByte()*Math.Pow(256, 2)) +
                (binaryReader.ReadByte()*256) +
                binaryReader.ReadByte());
        }
    }
}