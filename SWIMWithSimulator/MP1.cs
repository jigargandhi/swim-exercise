using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWIMWithSimulator
{
    class MP1
    {
        const int TFAIL = 6;
        const int TREMOVE = 40;
        private readonly Member member;
        private readonly Params param;
        private readonly EmulNet network;
        private readonly Address address;
        private readonly bool log;
        private readonly Queue<Message> queue;
        private readonly Dictionary<Address, PingRequest> requestCache;
        private int timeLeft;
        const int TProtocolPeriod = 20;
        const int TRoundTripTime = 5;
        const int KRandomMessage = 4;

        public MP1(Member member, Params param, EmulNet network, Address address, bool log = false)
        {
            this.member = member;
            this.param = param;
            this.network = network;
            this.address = address;
            this.log = log;
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
                if (member.activeMembers.Count == 0) return;
                DeclareUnreceivedMemberAsSuspected();
                StartNewSWIMProtocol();
            }
            else if (TProtocolPeriod - timeLeft == TRoundTripTime)
            {
                CheckIfTargetReceived();
            }

            timeLeft--;

            DeclareTimedOutMembersAsFailed();
            ClearRequestCache();
        }

        private void CheckIfTargetReceived()
        {

            var peerQueries = member.activeMembers.FirstOrDefault(m => m.queried);
            //Console.WriteLine("Pinging random K members on {}");
            if (peerQueries != null)
            {
                if (peerQueries.timestamp > peerQueries.queriedOn)
                {
                    peerQueries.queried = false;
                    peerQueries.MemberStatus = MemberStatus.Alive;

                }
                else
                {
                    PingRandomK();
                }
            }

        }

        private void DeclareTimedOutMembersAsFailed()
        {
            for (int i = 0; i < member.activeMembers.Count; i++)
            {
                var peer = member.activeMembers[i];
                var currentTimestamp = param.getCurrentTime();

                if (peer.MemberStatus == MemberStatus.Suspect && currentTimestamp - peer.timestamp > TREMOVE)
                {
                    
                    peer.MemberStatus = MemberStatus.Confirm;
                    // bye bye 
                    member.activeMembers.RemoveAt(i);
                    member.inactiveMembers.Add(peer);
                    Log($"Considering member {peer.address} as failed on {param.getCurrentTime()} at {member.addr.Id}");
                }
            }
        }

        private List<MemberListEntry> GetPayload(int max = 5)
        {
            var list = member.activeMembers.Concat(member.inactiveMembers).ToList();
            list.Add(new MemberListEntry()
            {
                address = member.addr,
                incarnation = member.incarnation,
                MemberStatus = MemberStatus.Alive
            });
            return list;
        }

        private void Merge(List<MemberListEntry> fromPiggyBack)
        {
            foreach (var source in fromPiggyBack)
            {
                if (source.address == member.addr) continue;
                Predicate<MemberListEntry> predicate = m => m.address == source.address;
                var target = member.activeMembers.Find(predicate);
                bool memberActive = true;
                if (target == null)
                {
                    target = member.inactiveMembers.Find(predicate);
                    memberActive = false;
                }
                if (target == null)
                {
                    if (source.MemberStatus == MemberStatus.Alive || source.MemberStatus == MemberStatus.Suspect)
                    {
                        source.queried = false;
                        source.queriedOn = 0;
                        member.activeMembers.Add(source);
                    }
                    else
                    {
                        member.inactiveMembers.Add(source);

                    }
                }
                else
                {
                    switch (source.MemberStatus)
                    {
                        case MemberStatus.Unkown:
                            break;
                        case MemberStatus.Alive:
                            if ((target.MemberStatus == MemberStatus.Alive || target.MemberStatus == MemberStatus.Suspect) && source.incarnation > target.incarnation)
                            {
                                target.MemberStatus = MemberStatus.Alive;
                                target.incarnation = source.incarnation;
                            }
                            break;
                        case MemberStatus.Suspect:
                            if (target.MemberStatus == MemberStatus.Alive && source.incarnation > target.incarnation)
                            {
                                target.MemberStatus = MemberStatus.Suspect;
                                target.incarnation = source.incarnation;
                            }
                            if (target.MemberStatus == MemberStatus.Suspect && source.incarnation >= target.incarnation)
                            {
                                target.MemberStatus = MemberStatus.Suspect;
                                target.incarnation = source.incarnation;
                            }
                            break;
                        case MemberStatus.Confirm:
                            Log($"{source.address} received as failed on {member.addr} at {param.getCurrentTime()}");
                            target.MemberStatus = MemberStatus.Confirm;
                            target.incarnation = source.incarnation;
                            if (memberActive)
                            {
                                member.activeMembers.Remove(target);
                                member.inactiveMembers.Add(target);
                            }
                            break;
                    }
                }
            }
        }

        private void DeclareUnreceivedMemberAsSuspected()
        {
            foreach (var peer in member.activeMembers)
            {
                if (peer.queried)
                {
                    Log($"Suspecting member {peer.address} on {member.addr.Id} at {param.getCurrentTime()}");
                    peer.MemberStatus = MemberStatus.Suspect;
                    peer.queried = false;
                    peer.timestamp = param.getCurrentTime();
                }
            }
        }

        private void PingRandomK()
        {
            int suspectIndex = -1;
            for (int i = 0; i < member.activeMembers.Count; i++)

            {
                var memberEntry = member.activeMembers[i];
                if (memberEntry.queried && param.getCurrentTime() - memberEntry.timestamp >= TRoundTripTime)
                {
                    suspectIndex = i;
                    break;
                }
            }

            // there is only one member ignore which is suspected cannot proceed further
            // lets wait for some other members to discover us.
            if (suspectIndex == -1 || member.activeMembers.Count == 1) return;

            var random = new Random(10);
            for (int i = 0; i < Math.Min(member.activeMembers.Count, KRandomMessage); i++)
            {
                int j = -1;
                do
                {
                    j = random.Next(member.activeMembers.Count);
                } while (j == suspectIndex);
                Log($"PINGREQ from {member.addr} to {member.activeMembers[j].address} for {member.activeMembers[suspectIndex].address} on {param.getCurrentTime()}");
                network.ENSend(member.addr, member.activeMembers[j].address, new PingReqMessage()
                {
                    sourceAddress = member.addr,
                    targetAddress = member.activeMembers[suspectIndex].address,
                    piggyBackMemberList = member.activeMembers,
                }.GetMessage());


            }
        }

        private void ClearRequestCache()
        {
            List<Address> toBeKilled = new List<Address>();
            foreach (var request in requestCache)
            {
                if (param.getCurrentTime() - request.Value.requestedOn > TFAIL)
                {
                    // safe enough to delete these request 
                    toBeKilled.Add(request.Key);
                }
            }

            foreach (var addr in toBeKilled)
            {
                requestCache.Remove(addr);
            }
        }

        private void StartNewSWIMProtocol()
        {
            if (member.activeMembers.Count == 0) return; // Do not have any members yet
            member.PushFirstToBack();


            var randomMember = member.activeMembers[0];
            if (randomMember.queried)
            {
                return;
            }
            PingMessage message = new PingMessage()
            {
                memberAddress = member.addr,
                piggyBackMemberList = member.activeMembers
            };

            network.ENSend(member.addr, randomMember.address, message.GetMessage());
            //Console.WriteLine($"Pinging member {randomMember.address} from {member.addr} at {param.getCurrentTime()}");
            randomMember.queried = true;
            randomMember.queriedOn = param.getCurrentTime();
            randomMember.timestamp = param.getCurrentTime();
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
            member.incarnation = 0;
            member.pingCounter = TFAIL;
            member.timeOutCounter = -1;
            member.activeMembers = new List<MemberListEntry>();
            member.inactiveMembers = new List<MemberListEntry>();
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
            Log($"ACK from {ackMessage.memberAddress} to {member.addr} on {param.getCurrentTime()}");
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
            var memberEntry = member.activeMembers.Where(mem => mem.address == ackMessage.memberAddress).FirstOrDefault();
            if (memberEntry == null)
            {
                memberEntry = member.inactiveMembers.Where(mem => mem.address == ackMessage.memberAddress).FirstOrDefault();
            }
            if (memberEntry == null) return;
            memberEntry.heartbeat = ackMessage.heartbeat;
            memberEntry.timestamp = param.getCurrentTime();
            memberEntry.MemberStatus = MemberStatus.Alive;
            memberEntry.queried = false;
            if (member.inactiveMembers.Contains(memberEntry))
            {
                member.inactiveMembers.Remove(memberEntry);
            }
            if (!member.activeMembers.Contains(memberEntry))
            {
                member.activeMembers.Add(memberEntry);
            }
        }

        private void HandlePing(Message message)
        {
            PingMessage pingMessage = JsonConvert.DeserializeObject<PingMessage>(message.Data);
            Merge(pingMessage.piggyBackMemberList);
            BeAlive(pingMessage.piggyBackMemberList);
            AckMessage ackMessage = new AckMessage()
            {
                heartbeat = member.heartbeat,
                memberAddress = member.addr,
                piggyBackMemberList = GetPayload(),
            };
            network.ENSend(member.addr, pingMessage.memberAddress, ackMessage.GetMessage());
        }

        private void BeAlive(List<MemberListEntry> piggyBackMemberList)
        {
            var mySelfInPing = piggyBackMemberList.Find(m => m.address == member.addr);
            if (mySelfInPing != null && mySelfInPing.MemberStatus == MemberStatus.Suspect && member.incarnation == mySelfInPing.incarnation)
            {
                Log($"Incrementing incarnation number for {member.addr}");
                member.incarnation++;
            }
        }

        private void HandleJoinResponse(Message message)
        {
            var joinResponse = JsonConvert.DeserializeObject<JoinRespMessage>(message.Data);
            member.inGroup = true;
            Merge(joinResponse.piggyBackMemberList);
            BeAlive(joinResponse.piggyBackMemberList);
        }

        private void HandleJoinReq(Message message)
        {
            var joinReqMessage = JsonConvert.DeserializeObject<JoinReqMessage>(message.Data);

            member.activeMembers.Add(new MemberListEntry()
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
                piggyBackMemberList = GetPayload(),
                heartbeat = member.heartbeat
            };

            network.ENSend(member.addr, joinReqMessage.memberAddress, joinResponse.GetMessage());
        }

        private void HandlePingReq(Message message)
        {
            PingReqMessage pingReqMessage = JsonConvert.DeserializeObject<PingReqMessage>(message.Data);
            Merge(pingReqMessage.piggyBackMemberList);
            BeAlive(pingReqMessage.piggyBackMemberList);
            if (requestCache.ContainsKey(pingReqMessage.targetAddress))
            {
                // target address already ping will not send duplicate ping;
                // todo what is the appropriate time to re ping
                var request = requestCache[pingReqMessage.targetAddress];
                if (param.getCurrentTime() - request.requestedOn > TFAIL)
                {
                    request.requestedOn = param.getCurrentTime();

                    PingMessage pingMessage = new PingMessage()
                    {
                        memberAddress = member.addr,
                        piggyBackMemberList = GetPayload(),
                    };
                    network.ENSend(member.addr, pingReqMessage.targetAddress, pingMessage.GetMessage());
                }
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
                    piggyBackMemberList = GetPayload(),
                };
                network.ENSend(member.addr, pingReqMessage.targetAddress, pingMessage.GetMessage());
            }
        }

        public override string ToString()
        {
            return $"Addr: {member.addr}\nIncarnation: {member.incarnation}\nIsKilled: {member.bFailed}\nMember List:{string.Join(',', member.activeMembers.Select(m => m.address.Id + "-" + m.MemberStatus.ToString()).ToArray())}\nInActivMember:{string.Join(',', member.inactiveMembers.Select(m => m.address.Id + "-" + m.MemberStatus.ToString()).ToArray())}\n------------------------------";
        }

        private void Log(string message)
        {
            if (log)
            {
                Console.WriteLine(message);
            }
        }
    }
}
