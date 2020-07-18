using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace SWIMWithSimulator
{
    class Application
    {
        const int TOTAL_RUNNING_TIME = 1000;
        int nodeCount = 0;
        Params param;
        EmulNet en;
        MP1[] node;
        Address joinAddress;
        Random random;
        const int APPLICATION_SEED = 3333;

        public Application(string paramFileName)
        {
            param = new Params(paramFileName);
            en = new EmulNet(param);
            node = new MP1[param.EN_GPSZ];
            random = new Random(APPLICATION_SEED);
            for (int i = 0; i < param.EN_GPSZ; i++)
            {
                Member member = new Member();
                Address addr = new Address();
                en.ENinit(addr);
                node[i] = new MP1(member, param, en, addr, i == 3 );

            }
        }

        public void Run()
        {
            for (param.globaltime = 0; param.globaltime < TOTAL_RUNNING_TIME; param.globaltime++)
            {
                mp1Run();

                fail();
            }

            printFinal();
            en.GetStatistics();
        }

        private void printFinal()
        {
            foreach (var n in node)
            {
                Console.WriteLine(n);
            }
        }

        private void fail()
        {
            if (param.DROP_MSG == 1 && param.getCurrentTime() == 50)
            {
                param.dropmsg = true;
            }

            if (param.SINGLE_FAILURE == 1 && param.getCurrentTime() == 100)
            {
                var removed = random.Next(param.EN_GPSZ);
                Console.WriteLine($"{node[removed].GetMember().addr} failed at {param.getCurrentTime()}");
                node[removed].GetMember().bFailed = true;
            }
            else if (param.getCurrentTime() == 100)
            {
                var removed = new Random().Next(param.EN_GPSZ / 2);
                for (int i = removed; i < removed + param.EN_GPSZ / 2; i++)
                {
                    Console.WriteLine($"{node[i].GetMember().addr} failed at {param.getCurrentTime()}");
                    node[i].GetMember().bFailed = true;
                }
            }

            if (param.DROP_MSG == 1 && param.getCurrentTime() == 300)
            {
                param.dropmsg = false;
            }
        }

        private void mp1Run()
        {
            for (int i = 0; i < param.EN_GPSZ; i++)
            {
                if (param.getCurrentTime() > (int)param.STEP_RATE * i && !node[i].GetMember().bFailed)
                {
                    node[i].recvLoop();
                }
            }

            for (int i = param.EN_GPSZ - 1; i >= 0; i--)
            {
                if (param.getCurrentTime() == (int)(param.STEP_RATE * i))
                {
                    // introduce the ith node into the system at time STEPRATE*i
                    node[i].nodeStart(joinAddress);
                    Console.WriteLine($"{i}-th introduced node is assigned with the address: {node[i].GetMember().addr}");
                    nodeCount += i;
                }
                else if (param.getCurrentTime() > (int)(param.STEP_RATE * i) && !node[i].GetMember().bFailed)
                {
                    node[i].nodeLoop();
                }
            }
        }
    }
}

