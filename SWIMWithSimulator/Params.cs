using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SWIMWithSimulator
{
    class Params
    {
        public int MAX_NNB;                // max number of neighbors
        public int SINGLE_FAILURE;         // single/multi failure
        public double MSG_DROP_PROB;       // message drop probability
        public double STEP_RATE;           // dictates the rate of insertion
        public int EN_GPSZ;                // actual number of peers
        public int MAX_MSG_SIZE;
        public int DROP_MSG;
        public bool dropmsg;
        public int globaltime;
        public int allNodesJoined;
        public short PORTNUM;

        public Params(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);

            foreach (var line in lines)
            {
                var keyValue = line.Split(":");
                switch (keyValue[0].Trim())
                {
                    case "MAX_NNB":
                        MAX_NNB = int.Parse(keyValue[1]);
                        break;
                    case "SINGLE_FAILURE":
                        SINGLE_FAILURE = int.Parse(keyValue[1]);
                        break;
                    case "DROP_MSG":
                        DROP_MSG = int.Parse(keyValue[1]);
                        break;
                    case "MSG_DROP_PROB":
                        MSG_DROP_PROB = double.Parse(keyValue[1]);
                        break;
                }
            }


            EN_GPSZ = MAX_NNB;
            STEP_RATE = .25;
            MAX_MSG_SIZE = 4000;
            globaltime = 0;
            dropmsg = false;
            allNodesJoined = 0;
            for (int i = 0; i < EN_GPSZ; i++)
            {
                allNodesJoined += i;
            }
        }

        public int getCurrentTime() => globaltime;
    }
}
