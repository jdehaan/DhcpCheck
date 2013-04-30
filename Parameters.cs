using System;

namespace DhcpCheck
{
    public class Parameters
    {
        private readonly byte[] _macAddress;

        public Parameters()
        {
            // Fake mac address
            _macAddress = new byte[6];

            // use similar fake as DHCP find
            _macAddress[0] = 0x08;
            _macAddress[1] = 0x00;
            _macAddress[2] = 0x27;
            _macAddress[3] = 0x00;
            _macAddress[4] = 0xE8;
            _macAddress[5] = 0x45;

            // use a completely fake MAC AA-BB-CC-DD-EE-FF
            //_macAddress[0] = 0xAA;
            //_macAddress[1] = 0xBB;
            //_macAddress[2] = 0xCC;
            //_macAddress[3] = 0xDD;
            //_macAddress[4] = 0xEE;
            //_macAddress[5] = 0xFF;
        }

        public string Logfile
        {
            get { return string.Format("dhcp-{0:yyyyMMdd}.log", DateTime.Now); }
        }

        public byte[] MacAddress
        {
            get { return _macAddress; }
        }

        public int SendTimeout
        {
            get { return 5*1000; }
        }

        public int ReceiveTimeout
        {
            get { return 5*1000; }
        }

        public int WaitingTime
        {
            get { return 10*1000; }
        }
    }
}