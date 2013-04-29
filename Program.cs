using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace DhcpCheck
{
    internal class Program : IDhcpPacketReader
    {
        private readonly Parameters _parameters;

        private Program()
        {
            _parameters = new Parameters();
        }

        public void ReadPacket(EndPoint remoteEndPoint, byte[] data, int length)
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

            var binaryReader = new BinaryReader(new MemoryStream(data));
            string serverIp = "?";
            string leaseTime = "?";
            string renewalTime = "?";
            string rebindingTime = "?";
            string subnetMask = "?";
            string routers = "?";
            string dnsServers = "?";
            string domainName = "?";
            byte otype = 0;

            // skip to yiaddr
            binaryReader.ReadBytes(16);

            // yiaddr offered to client
            byte[] yiaddr = binaryReader.ReadBytes(4);
            string offeredIp = String.Format("{0}.{1}.{2}.{3}", yiaddr[0], yiaddr[1], yiaddr[2], yiaddr[3]);

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
                            domainName = Encoding.ASCII.GetString(binaryReader.ReadBytes(optionLength), 0, optionLength-1);
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
                    loggingText = string.Format("{0:yyyyMMdd HHmmss}\tDISCOVER\r\n",
                                                DateTime.Now);
                    break;
                case 2:
                    loggingText = string.Format("{0:yyyyMMdd HHmmss}\tOFFER\t{1}({2})\t{3}\t{4}\t{5}\t{6}\t{7}\tDN={8}\tDNS={9}\tGW={10}\r\n",
                                                DateTime.Now, serverIp, remoteEndPoint, offeredIp,
                                                leaseTime, renewalTime, rebindingTime,
                                                subnetMask,
                                                domainName, dnsServers, routers);
                    break;
            }

            if (loggingText != null)
            {
                File.AppendAllText(_parameters.Logfile, loggingText);
                Console.Write(loggingText);
            }
        }

        private static string ReadIpv4List(byte optionLength, BinaryReader binaryReader)
        {
            string dnsServers;
            var sb = new StringBuilder();
            for (int i = 0; i < optionLength/4; i++)
            {
                sb.Append(ReadIpv4(binaryReader));
                sb.Append(",");
            }
            sb.Remove(sb.Length - 1, 1);
            dnsServers = sb.ToString();
            return dnsServers;
        }

        private static string ReadIpv4(BinaryReader binaryReader)
        {
            string srv;
            byte[] serverid = binaryReader.ReadBytes(4);
            srv = String.Format("{0}.{1}.{2}.{3}", serverid[0], serverid[1], serverid[2], serverid[3]);
            return srv;
        }

        private static string ReadTimeInterval(BinaryReader binaryReader)
        {
            return Convert.ToString(
                (binaryReader.ReadByte()*Math.Pow(256, 3)) +
                (binaryReader.ReadByte()*Math.Pow(256, 2)) +
                (binaryReader.ReadByte()*256) +
                binaryReader.ReadByte());
        }

        private static void Main(string[] args)
        {
            var p = new Program();
            p.Run();
        }

        private void Run()
        {
            while (true)
            {
                using (var dhcpClient = new DhcpClient(_parameters, this))
                {
                    dhcpClient.Discover();
                    dhcpClient.BeginReceiveFrom();
                    Thread.Sleep(9*1000);
                }
                Thread.Sleep(1000);
            }
        }
    }
}