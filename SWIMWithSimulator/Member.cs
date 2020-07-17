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
		// Membership table
		public List<MemberListEntry> memberList;
		// My position in the membership table
		List<MemberListEntry>.Enumerator myPos;
        // Queue for failure detection messages
        //queue<q_elt> mp1q;

        public Member()
        {
			addr = new Address();
        }

		public void PushFirstToBack()
        {
			if (memberList.Count < 1) return;
			var firstMember = memberList[0];
			for(int i = 1; i < memberList.Count; i++)
            {
				memberList[i - 1] = memberList[i];
            }
			memberList[memberList.Count - 1] = firstMember;
        }
    }
}
