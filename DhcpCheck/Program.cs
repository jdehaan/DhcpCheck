using System;
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
            _captureDriver = new DhcpCapture(_parameters);
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
            _captureDriver.StartCapturing();

            while (!_exit)
            {
                using (var dhcpClient = new DhcpClient(_parameters))
                {
                    try
                    {
                        dhcpClient.SendDiscover();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception during SendDiscover: {0}", ex.Message);
                        Console.WriteLine("Will retry later...");
                    }
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