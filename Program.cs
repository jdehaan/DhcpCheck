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
        private volatile bool _exit;

        private Program()
        {
            _parameters = new Parameters();
        }

        private static void Main(string[] args)
        {
            var p = new Program();
            p.Run();
        }

        private void Run()
        {
            Console.WriteLine("Press Ctrl+C to exit");
            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            while (!_exit)
            {
                using (var dhcpClient = new DhcpClient(_parameters, this))
                {
                    dhcpClient.SendDiscover();
                    dhcpClient.BeginReceiveFrom();
                    Thread.Sleep(9900);
                }
                Thread.Sleep(100);
            }
            Console.WriteLine("Ctrl+C pressed, exitting");
        }

        private void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs consoleCancelEventArgs)
        {
            _exit = true;
            consoleCancelEventArgs.Cancel = true;
        }

        public void ReadPacket(EndPoint remoteEndPoint, byte[] data, int length)
        {
            var dhcpPacket = new DhcpPacket(data, length);
            dhcpPacket.ReadPacket(_parameters, remoteEndPoint);
        }
    }
}