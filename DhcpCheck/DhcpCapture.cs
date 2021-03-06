﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace DhcpCheck
{
    public class DhcpCapture
    {
        private readonly Queue<Packet> _packets;
        private readonly Parameters _parameters;
        private readonly CaptureFileWriterDevice _pcapFileWriter;
        private Thread _writer;

        public DhcpCapture(Parameters parameters)
        {
            _parameters = parameters;
            _packets = new Queue<Packet>();
            _pcapFileWriter = new CaptureFileWriterDevice(
                LinkLayers.Ethernet, null, _parameters.PcapFilename, FileMode.OpenOrCreate);
        }

        public string Version
        {
            get { return String.Format("Using SharpPcap v{0}, {1}", SharpPcap.Version.VersionString, Pcap.Version); }
        }

        private void WriterThread()
        {
            try
            {
                while (true)
                {
                    if (_packets.Count > 0)
                    {
                        try
                        {
                            Write(_packets.Dequeue());
                        }
                        catch (ThreadInterruptedException)
                        {
                            throw; // rethrow this one
                        }
                        catch (Exception)
                        {
                            // but swallow this one by dumping an error to the console
                            Console.WriteLine("ERROR: Invalid BOOTP packet received (ignored)!");
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (ThreadInterruptedException ex)
            {
                // return
            }
        }

        private void Write(Packet packet)
        {
            var ethPacket = packet as EthernetPacket;
            if (ethPacket == null)
                return;
            var ipPacket = ethPacket.Extract(typeof (IPv4Packet)) as IPv4Packet;
            if (ipPacket == null)
                return;
            var udpPacket = ipPacket.Extract(typeof (UdpPacket)) as UdpPacket;
            if (udpPacket == null)
                return;

            var sb = new StringBuilder();
            sb.Append(String.Format("{0:yyyyMMdd HHmmss:fff}", DateTime.Now));
            sb.Append("\t");
            sb.Append(ethPacket.SourceHwAddress);
            sb.Append("\t");
            sb.Append(ethPacket.DestinationHwAddress);
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
            File.AppendAllText(_parameters.LogFilename, loggingText);
            Console.Write(loggingText);
        }

        public void StartCapturing()
        {
            _writer = new Thread(WriterThread);
            _writer.Start();
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
            _writer.Interrupt();
            _writer.Join();
            _writer = null;
            _packets.Clear();
        }

        private void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            var packet = Packet.ParsePacket(LinkLayers.Ethernet, e.Packet.Data) as EthernetPacket;
            if (packet == null)
                return;
            _pcapFileWriter.Write(e.Packet);
            _packets.Enqueue(packet);
        }
    }
}