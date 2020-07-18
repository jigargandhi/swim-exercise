using System;
using System.Collections.Generic;
using System.Text;

namespace SWIMWithSimulator
{
    class Member
    {
		// This member's Address
		public Address addr;
		// boolean indicating if this member is up
		public bool inited;
		// boolean indicating if this member is in the group
		public bool inGroup;
		// boolean indicating if this member has failed
		public bool bFailed;
		// number of my neighbors
		public int nnb;
		// the node's own heartbeat
		public long heartbeat;
		// counter for next ping
		public int pingCounter;
		// counter for ping timeout
		public int timeOutCounter;
		// incarnation number;
		public int incarnation;
		// Membership table
		public List<MemberListEntry> activeMembers;

		//Inactive members
		public List<MemberListEntry> inactiveMembers;

        public Member()
        {
			addr = new Address();
        }

		public void PushFirstToBack()
        {
			if (activeMembers.Count < 1) return;
			var firstMember = activeMembers[0];
			for(int i = 1; i < activeMembers.Count; i++)
            {
				activeMembers[i - 1] = activeMembers[i];
            }
			activeMembers[activeMembers.Count - 1] = firstMember;
        }
    }
}
