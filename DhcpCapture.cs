using System;
using System.IO;
using System.Text;
using PacketDotNet;
using SharpPcap;

namespace DhcpCheck
{
    public class DhcpCapture
    {
        private Parameters _parameters;

        public string Version
        {
            get { return String.Format("Using SharpPcap v{0}, {1}", SharpPcap.Version.VersionString, Pcap.Version); }
        }

        public void StartCapturing(Parameters parameters)
        {
            _parameters = parameters;
            foreach (ICaptureDevice device in CaptureDeviceList.Instance)
            {
                device.OnPacketArrival += device_OnPacketArrival;
                const int readTimeoutMilliseconds = 1000;
                device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);

                try
                {
                    device.Filter = "udp port 67 or port 68";
                }
                catch
                {
                    device.Close();
                    device.OnPacketArrival -= device_OnPacketArrival;
                    throw;
                }
                device.StartCapture();
            }
        }

        public void StopCapturing()
        {
            foreach (ICaptureDevice device in CaptureDeviceList.Instance)
            {
                try
                {
                    device.StopCaptureTimeout = TimeSpan.FromMilliseconds(2000);
                    device.StopCapture();
                }
                catch
                {
                }
                device.Close();
                device.OnPacketArrival -= device_OnPacketArrival;
            }
        }

        private void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            var packet = Packet.ParsePacket(LinkLayers.Ethernet, e.Packet.Data) as EthernetPacket;
            if (packet == null)
                return;
            var ipPacket = packet.Extract(typeof (IPv4Packet)) as IPv4Packet;
            if (ipPacket == null)
                return;
            var udpPacket = ipPacket.Extract(typeof (UdpPacket)) as UdpPacket;
            if (udpPacket == null)
                return;

            var sb = new StringBuilder();
            sb.Append(String.Format("{0:yyyyMMdd HHmmss:fff}", DateTime.Now));
            sb.Append("\t");
            sb.Append(packet.SourceHwAddress);
            sb.Append("\t");
            sb.Append(packet.DestinationHwAddress);
            sb.Append("\t");
            sb.Append(ipPacket.SourceAddress);
            sb.Append("\t");
            sb.Append(ipPacket.DestinationAddress);
            sb.Append("\t");
            sb.Append(udpPacket.SourcePort);
            sb.Append("\t");
            sb.Append(udpPacket.DestinationPort);
            sb.Append("\t");

            var dhcpPacket = new DhcpPacket(udpPacket.PayloadData);
            dhcpPacket.ReadPacket(_parameters, sb);
            sb.Append("\r\n");

            String loggingText = sb.ToString();
            File.AppendAllText(_parameters.Logfile, loggingText);
            Console.Write(loggingText);
        }
    }
}