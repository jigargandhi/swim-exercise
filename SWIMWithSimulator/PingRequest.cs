using System;
using System.Collections.Generic;
using System.Text;

namespace SWIMWithSimulator
{
    class PingRequest
    {
        public int requestedOn;
        public List<Address> sourceAddresses;
        public PingRequest()
        {
            sourceAddresses = new List<Address>();
        }
    }
}
