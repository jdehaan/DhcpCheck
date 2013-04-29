using System;

namespace DhcpCheck
{
    public class Parameters
    {
        private readonly byte[] _macAddress;

        public Parameters()
        {
            // Fake mac address
            // AA-BB-CC-DD-EE-FF
            _macAddress = new byte[6];
            _macAddress[0] = 0xAA;
            _macAddress[1] = 0xBB;
            _macAddress[2] = 0xCC;
            _macAddress[3] = 0xDD;
            _macAddress[4] = 0xEE;
            _macAddress[5] = 0xFF;
        }

        public string Logfile
        {
            get { return string.Format("dhcp-{0:yyyyMMdd}.log", DateTime.Now);  }
        }

        public byte[] MacAddress
        {
            get {  return _macAddress; }
        }

        public int SendTimeout
        {
            get { return 5*1000; }
        }

        public int ReceiveTimeout
        {
            get { return 5*1000; }
        }
    }
}