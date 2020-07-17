using System;
using System.Collections.Generic;
using System.Text;

namespace SWIMWithSimulator
{
    class EM
    {
        public const int ENBUFFSIZE = 30000;
        public int nextid;
        public int currbuffsize;
        public int firsteltindex;
        public NetworkMessage[] buffer = new NetworkMessage[ENBUFFSIZE];
    }
}
