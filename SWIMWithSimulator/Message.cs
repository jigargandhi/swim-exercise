using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SWIMWithSimulator
{
    class Message
    {

        public MessageType messageType;
        public string Data;
    }

    interface IGetNetworkMessage
    {
        Message GetMessage();
    }

    class JoinReqMessage : IGetNetworkMessage
    {
        public long heartbeat;
        public Address memberAddress;
        public Member Member;

        public Message GetMessage()
        {
            return new Message()
            {
                messageType = MessageType.JOINREQ,
                Data = JsonConvert.SerializeObject(this)
            };
        }
    }

    class JoinRespMessage : IGetNetworkMessage
    {
        public MemberListEntry[] memberList;
        public Address memberAddress;
        public long heartbeat;

        public Message GetMessage()
        {
            return new Message()
            {
                messageType = MessageType.JOINREP,
                Data = JsonConvert.SerializeObject(this),
            };
        }
    }

    class PingMessage : IGetNetworkMessage
    {
        public long heartbeat;
        public Address memberAddress;

        public Message GetMessage()
        {
            throw new NotImplementedException();
        }
    }

    class AckMessage : IGetNetworkMessage
    {
        public long heartbeat;
        public Address memberAddress;

        public Message GetMessage()
        {
            return new Message()
            {
                messageType = MessageType.ACK,
                Data = JsonConvert.SerializeObject(this),
            };
        }
    }

    class PingReqMessage : IGetNetworkMessage
    {
        public Address sourceAddress;
        public Address targetAddress;

        public Message GetMessage()
        {
            return new Message()
            {
                messageType = MessageType.PINGREQ,
                Data = JsonConvert.SerializeObject(this)
            };
        }
    }
}
