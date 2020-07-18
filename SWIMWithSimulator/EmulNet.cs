using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace SWIMWithSimulator
{
    class EmulNet
    {
        const int MAX_NODES = 1000;
        const int MAX_TIME = 10000;
        private readonly Params param;
        private readonly EM emulnet;
        private int enInited;
        private int[,] sent_msgs = new int[MAX_NODES + 1, MAX_TIME];
        private int[,] recv_msgs = new int[MAX_NODES + 1, MAX_TIME];
        private Random random;
        const int SEED = 11111;
        private int droppedMessages;

        public EmulNet(Params param)
        {
            this.param = param;
            this.emulnet = new EM();
            this.emulnet.nextid = 1;
            this.emulnet.currbuffsize = 0;
            this.enInited = 0;
            this.random = new Random(SEED);
            this.droppedMessages = 0;
            for (int i = 0; i < MAX_NODES; i++)
            {
                for (int j = 0; j < MAX_TIME; j++)
                {
                    sent_msgs[i, j] = 0;
                    recv_msgs[i, j] = 0;
                }
            }
        }

        public void ENinit(Address address)
        {
            address.addr = BitConverter.GetBytes(emulnet.nextid++);
        }

        public void ENSend(Address fromAddress, Address toAdress, Message message)
        {
            int sendmsg = random.Next() % 100;
            if(emulnet.currbuffsize >= EM.ENBUFFSIZE || 
                (param.dropmsg && (sendmsg < param.MSG_DROP_PROB * 100 )))
            {
                droppedMessages++;
                //Console.WriteLine($"Dropping message {fromAddress} to {toAdress} of type {message.messageType} at {param.getCurrentTime()}");
                return;
            }
            emulnet.buffer[emulnet.currbuffsize++] = new NetworkMessage()
            {
                from = fromAddress,
                to = toAdress,
                message = message
            };
            var time = param.getCurrentTime();
            sent_msgs[fromAddress.addr[0], time]++;
        }

        public void ENReceive(Address address, Action<Message> action)
        {
            for (int i = emulnet.currbuffsize - 1; i >= 0; i--)
            {
                var msg = emulnet.buffer[i];
                if (msg.to == address)
                {
                    emulnet.buffer[i] = emulnet.buffer[emulnet.currbuffsize - 1];
                    emulnet.currbuffsize--;
                    action(msg.message);
                    var time = param.getCurrentTime();
                    recv_msgs[address.addr[0], time]++;
                }
            }
        }

        public void GetStatistics()
        {
            int sent = 0;
            int received = 0;
            for (int i = 0; i < MAX_NODES; i++)
            {
                for (int j = 0; j < MAX_TIME; j++)
                {
                    sent += sent_msgs[i, j];
                    received+= recv_msgs[i, j];
                }
            }

            Console.WriteLine($"Sent: {sent}\tReceived: {received}\tDropped:{droppedMessages}");
        }
    }
}
