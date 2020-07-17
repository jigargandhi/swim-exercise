using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SWIMWithSimulator
{
    class Address
    {
        public byte[] addr = new byte[4];
        public short port;

        public int Id => addr[0];


        public override string ToString()
        {
            return $"{addr[3]}.{addr[2]}.{addr[1]}.{addr[0]}:{port}";
        }

        public static bool operator ==(Address address1, Address address2)
        {
            for(int i = 0; i < 4; i++)
            {
                if (address1.addr[i] != address2.addr[i]) return false;
            }

            return true;
        }

        public static bool operator !=(Address address1, Address address2)
        {
            for (int i = 0; i < 4; i++)
            {
                if (address1.addr[i] != address2.addr[i]) return true;
            }

            return false;
        }

        public Address Clone()
        {
            return new Address()
            {
                addr = addr.Select(d => d).ToArray(),
                port = this.port
            };
        }
    }
}
