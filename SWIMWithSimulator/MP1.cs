using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace SWIMWithSimulator
{
    class MP1
    {
        const int TFAIL = 5;
        const int TREMOVE = 20;
        private readonly Member member;
        private readonly Params param;
        private readonly EmulNet network;
        private readonly Address address;
        private readonly Queue<Message> queue;
        private readonly Dictionary<Address, PingRequest> requestCache;
        private int timeLeft;
        const int TProtocolPeriod = 10;
        public MP1(Member member, Params param, EmulNet network, Address address)
        {
            this.member = member;
            this.param = param;
            this.network = network;
            this.address = address;
            this.member.addr = address;
            this.queue = new Queue<Message>();
            this.requestCache = new Dictionary<Address, PingRequest>(new AddressComparer());
        }

        public Member GetMember() => member;

        public void recvLoop()
        {
            if (member.bFailed) return;

            network.ENReceive(member.addr, (Message msg) =>
            {
                queue.Enqueue(msg);
            });
        }

        public void nodeStart(Address address)
        {
            Address joinAddress = new Address();
            joinAddress.addr[0] = 1;

            initThisNode(joinAddress);

            introduceSelfToGroup(joinAddress);
        }

        public void nodeLoop()
        {
            if (member.bFailed) return;

            checkMessages();

            if (!member.inGroup) return;

            nodeLoopOps();

        }

        private void nodeLoopOps()
        {
            if (timeLeft == 0)
            {
                // New protocol period
                // choose a random member and send ping to it 
                timeLeft = TProtocolPeriod;
                if (member.memberList.Count == 0) return;
                DetectFailureRemove();
                StartNewSWIMProtocol();
            }
            else
            {
                timeLeft--;
            }

        }

        private void StartNewSWIMProtocol()
        {
            if (member.memberList.Count == 0) return; // Do not have any members yet
            member.PushFirstToBack();

            var randomMember = member.memberList[0];
            if (randomMember.MemberStatus >= MemberStatus.Queried)
            {
                return;
            }
            PingMessage message = new PingMessage()
            {
                memberAddress = member.addr
            };
            network.ENSend(member.addr, randomMember.address, new Message()
            {
                messageType = MessageType.PING,
                Data = JsonConvert.SerializeObject(message),
            });
            //Console.WriteLine($"Pinging member {randomMember.address} from {member.addr} at {param.getCurrentTime()}");
            randomMember.MemberStatus = MemberStatus.Queried;
            randomMember.timestamp = param.getCurrentTime();
        }

        private void DetectFailureRemove()
        {
            int failedIndex = -1;
            for (int i = 0; i < member.memberList.Count; i++)

            {
                var memberEntry = member.memberList[i];

                if (memberEntry.MemberStatus == MemberStatus.Suspect && (param.getCurrentTime() - memberEntry.timestamp + 1 > TREMOVE))
                {
                    failedIndex = i;
                    //Console.WriteLine($"Member {memberEntry.address} detected as and removed at {member.addr} ");
                    // Ideally node should be removed;
                    memberEntry.MemberStatus = MemberStatus.Fail;
                }
                else if (memberEntry.MemberStatus == MemberStatus.Queried && param.getCurrentTime() - memberEntry.timestamp + 1 > TProtocolPeriod)
                {
                    //Console.WriteLine($"Member {memberEntry.address} detected as suspect at {member.addr} ");

                    memberEntry.MemberStatus = MemberStatus.Suspect;
                }

            }

            if (failedIndex > -1)
            {
                var memberToRemove = member.memberList[failedIndex];
                Console.WriteLine($"Removing member {memberToRemove.address} on {member.addr} at {param.getCurrentTime()}");
                member.memberList.RemoveAt(failedIndex);
            }
        }

        private void checkMessages()
        {
            while (queue.Count > 0)
            {
                var message = queue.Dequeue();
                receiveCallBack(message);
            }
        }

        public bool initThisNode(Address joinAddress)
        {
            int id = BitConverter.ToInt32(member.addr.addr);
            int port = member.addr.port;
            member.bFailed = false;
            member.inited = true;
            member.inGroup = false;
            member.nnb = 0;
            member.heartbeat = 0;
            member.pingCounter = TFAIL;
            member.timeOutCounter = -1;
            member.memberList = new List<MemberListEntry>();
            return true;
        }


        private bool introduceSelfToGroup(Address joinAddress)
        {

            if (member.addr == joinAddress)
            {
                member.inGroup = true;
            }
            else
            {
                Message message = new Message();
                JoinReqMessage joinReqMessage = new JoinReqMessage()
                {
                    Member = member,
                    heartbeat = member.heartbeat,
                    memberAddress = member.addr
                };

                network.ENSend(member.addr, joinAddress, joinReqMessage.GetMessage());
            }

            return true;
        }

        private void receiveCallBack(Message message)
        {
            switch (message.messageType)
            {
                case MessageType.JOINREQ:
                    HandleJoinReq(message);
                    break;
                case MessageType.JOINREP:
                    HandleJoinResponse(message);
                    break;
                case MessageType.PING:
                    HandlePing(message);
                    break;
                case MessageType.ACK:
                    HandleAck(message);
                    break;
                case MessageType.PINGREQ:
                    HandlePingReq(message);
                    break;
            }
        }

        private void HandleAck(Message message)
        {

            var ackMessage = JsonConvert.DeserializeObject<AckMessage>(message.Data);
            // forwarding of messages
            if (requestCache.ContainsKey(ackMessage.memberAddress))
            {
                // send ack responses to all the ones who request ack from this member;
                foreach (var waitingMember in requestCache[ackMessage.memberAddress].sourceAddresses)
                {
                    network.ENSend(member.addr, waitingMember, message);
                }
                requestCache.Remove(ackMessage.memberAddress);

            }
            // member is alive
            var memberEntry = member.memberList.Where(mem => mem.address == ackMessage.memberAddress).First();
            memberEntry.heartbeat = ackMessage.heartbeat;
            memberEntry.timestamp = param.getCurrentTime();
            memberEntry.MemberStatus = MemberStatus.Alive;
        }

        private void HandlePing(Message message)
        {
            PingMessage pingMessage = JsonConvert.DeserializeObject<PingMessage>(message.Data);
            AckMessage ackMessage = new AckMessage()
            {
                heartbeat = member.heartbeat,
                memberAddress = member.addr,
            };
            network.ENSend(member.addr, pingMessage.memberAddress, new Message()
            {
                messageType = MessageType.ACK,
                Data = JsonConvert.SerializeObject(ackMessage),
            }); ;
        }

        private void HandleJoinResponse(Message message)
        {
            var joinResponse = JsonConvert.DeserializeObject<JoinRespMessage>(message.Data);
            member.inGroup = true;
            MemberListEntry existingMember;
            foreach (var memberEntry in joinResponse.memberList)
            {
                if ((existingMember = member.memberList.FirstOrDefault(peer => peer.address == member.addr)) != null)
                {
                    // merge with existing member


                }
                else
                {
                    if (memberEntry.MemberStatus == MemberStatus.Queried)
                    {
                        memberEntry.MemberStatus = MemberStatus.Alive;
                    }
                    //add
                    member.memberList.Add(memberEntry);
                }
            }

            member.memberList.Add(new MemberListEntry()
            {
                timestamp = param.getCurrentTime(),
                heartbeat = joinResponse.heartbeat,
                id = joinResponse.memberAddress.addr[0],
                address = joinResponse.memberAddress,
                MemberStatus = MemberStatus.Alive,
            });
            int index = member.memberList.FindIndex(m => m.address == member.addr);

            if (index >= 0)
            {
                member.memberList.RemoveAt(index);
            }
            ////Console.WriteLine($"Received JOIN Response from {message.memberAddress} to {member.addr} with memberlist {string.Join(",", message.memberList.Select(d => d.address.ToString()).ToArray())}");
        }

        private void HandleJoinReq(Message message)
        {
            var joinReqMessage = JsonConvert.DeserializeObject<JoinReqMessage>(message.Data);

            member.memberList.Add(new MemberListEntry()
            {
                timestamp = param.getCurrentTime(),
                heartbeat = joinReqMessage.heartbeat,
                id = joinReqMessage.memberAddress.addr[0],
                address = joinReqMessage.memberAddress,
                MemberStatus = MemberStatus.Alive,
            });
            var joinResponse = new JoinRespMessage()
            {
                memberAddress = member.addr,
                memberList = member.memberList.ToArray(),
                heartbeat = member.heartbeat
            };

            network.ENSend(member.addr, joinReqMessage.memberAddress, joinResponse.GetMessage());
        }

        private void HandlePingReq(Message message)
        {
            PingReqMessage pingReqMessage = JsonConvert.DeserializeObject<PingReqMessage>(message.Data);
            if (requestCache.ContainsKey(pingReqMessage.targetAddress))
            {
                // target address already ping will not send duplicate ping;
                // todo what is the appropriate time to re ping
            }
            else
            {
                PingRequest request = new PingRequest();
                request.requestedOn = param.getCurrentTime();
                request.sourceAddresses.Add(pingReqMessage.sourceAddress);

                requestCache.Add(pingReqMessage.targetAddress, request);

                PingMessage pingMessage = new PingMessage()
                {
                    memberAddress = member.addr,
                };
                Message toSend = new Message()
                {
                    messageType = MessageType.PING,
                    Data = JsonConvert.SerializeObject(pingMessage)
                };
                network.ENSend(member.addr, pingReqMessage.targetAddress, toSend);
            }
        }

        public override string ToString()
        {

            return $"Addr: {member.addr}\nQueue Size: {queue.Count}\nIsKilled: {member.bFailed}\nMember List:{string.Join<Address>(',', member.memberList.Select(m => m.address).ToArray())}\n------------------------------";
        }
    }
}
