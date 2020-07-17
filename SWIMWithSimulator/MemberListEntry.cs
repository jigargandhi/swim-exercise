namespace SWIMWithSimulator
{
    enum MemberStatus
    {
        Unkown = 0,
        Alive = 1,
        Queried = 2,
        Suspect = 3,
        Fail = 4
    }
    class MemberListEntry
    {
        public int id;
        public short port;
        public long heartbeat;
        public long timestamp;
        public Address address;
        public MemberStatus MemberStatus;

        public MemberListEntry Clone()
        {
            return new MemberListEntry()
            {
                id = this.id,
                port = this.port,
                timestamp = this.timestamp,
                address = this.address?.Clone(),
                MemberStatus = MemberStatus
            };
        }
    }
}