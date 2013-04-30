using System;
using System.Net;
using System.Threading;

namespace DhcpCheck
{
    internal class Program
    {
        private readonly DhcpCapture _captureDriver;
        private readonly Parameters _parameters;
        private volatile bool _exit;

        private Program()
        {
            _parameters = new Parameters();
            _captureDriver = new DhcpCapture();
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
            _captureDriver.StartCapturing(_parameters);

            while (!_exit)
            {
                using (var dhcpClient = new DhcpClient(_parameters))
                {
                    dhcpClient.SendDiscover();
                }
                Thread.Sleep(_parameters.WaitingTime);
            }

            Console.WriteLine("Ctrl+C pressed, exiting...");
            _captureDriver.StopCapturing();
        }

        private void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs consoleCancelEventArgs)
        {
            _exit = true;
            consoleCancelEventArgs.Cancel = true;
        }
    }
}