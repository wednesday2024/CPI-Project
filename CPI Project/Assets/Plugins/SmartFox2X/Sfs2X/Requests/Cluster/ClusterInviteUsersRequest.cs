#if UNITY_WEBGL
using System.Collections.Generic;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Exceptions;

namespace Sfs2X.Requests.Cluster
{
	public class ClusterInviteUsersRequest : BaseRequest
	{
		public static readonly string KEY_USER = "u";

		public static readonly string KEY_USER_ID = "ui";

		public static readonly string KEY_INVITATION_ID = "ii";

		public static readonly string KEY_TIME = "t";

		public static readonly string KEY_PARAMS = "p";

		public static readonly string KEY_INVITEE_ID = "ee";

		public static readonly string KEY_INVITED_USERS = "iu";

		public static readonly string KEY_SERVER_ID = "ss";

		public static readonly string KEY_ROOM_ID = "rr";

		public static readonly string KEY_REPLY_ID = "ri";

		public static readonly int MAX_INVITATIONS_FROM_CLIENT_SIDE = 8;

		public static readonly int MIN_EXPIRY_TIME = 5;

		public static readonly int MAX_EXPIRY_TIME = 300;

		private List<object> invitedUsers;

		private int secondsForReply;

		private ISFSObject parameters;

		private ClusterTarget target;

		public ClusterInviteUsersRequest(ClusterTarget target, List<object> invitedUsers, int secondsForReply, ISFSObject parameters)
			: base(RequestType.ClusterInviteUsers)
		{
			this.target = target;
			this.invitedUsers = invitedUsers;
			this.secondsForReply = secondsForReply;
			this.parameters = parameters;
		}

		public override void Validate(SmartFox sfs)
		{
			List<string> list = new List<string>();
			if (invitedUsers == null || invitedUsers.Count < 1)
			{
				list.Add("No invitation(s) to send");
			}
			if (invitedUsers.Count > MAX_INVITATIONS_FROM_CLIENT_SIDE)
			{
				int mAX_INVITATIONS_FROM_CLIENT_SIDE = MAX_INVITATIONS_FROM_CLIENT_SIDE;
				list.Add("Too many invitations. Max allowed from client side is: " + mAX_INVITATIONS_FROM_CLIENT_SIDE);
			}
			if (secondsForReply < MIN_EXPIRY_TIME || secondsForReply > MAX_EXPIRY_TIME)
			{
				string[] obj = new string[5] { "SecondsForReply value is out of range (", null, null, null, null };
				int mAX_INVITATIONS_FROM_CLIENT_SIDE = MIN_EXPIRY_TIME;
				obj[1] = mAX_INVITATIONS_FROM_CLIENT_SIDE.ToString();
				obj[2] = "-";
				mAX_INVITATIONS_FROM_CLIENT_SIDE = MAX_EXPIRY_TIME;
				obj[3] = mAX_INVITATIONS_FROM_CLIENT_SIDE.ToString();
				obj[4] = ")";
				list.Add(string.Concat(obj));
			}
			if (target == null)
			{
				list.Add("Missing cluster target (server id and room id)");
			}
			if (list.Count > 0)
			{
				throw new SFSValidationError("ClusterInviteUsers request error", list);
			}
		}

		public override void Execute(SmartFox sfs)
		{
			List<int> list = new List<int>();
			foreach (object invitedUser in invitedUsers)
			{
				if (invitedUser is User)
				{
					if (invitedUser as User != sfs.MySelf)
					{
						list.Add((invitedUser as User).Id);
					}
				}
				else if (invitedUser is Buddy)
				{
					list.Add((invitedUser as Buddy).Id);
				}
			}
			sfso.PutUtfString(KEY_SERVER_ID, target.ServerId);
			sfso.PutInt(KEY_ROOM_ID, target.RoomId);
			sfso.PutIntArray(KEY_INVITED_USERS, list.ToArray());
			sfso.PutShort(KEY_TIME, (short)secondsForReply);
			if (parameters != null)
			{
				sfso.PutSFSObject(KEY_PARAMS, parameters);
			}
		}
	}
}
#else
using System.Collections.Generic;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Exceptions;

namespace Sfs2X.Requests.Cluster
{
    public class ClusterInviteUsersRequest : BaseRequest
    {
        public static readonly string KEY_USER = "u";

        public static readonly string KEY_USER_ID = "ui";

        public static readonly string KEY_INVITATION_ID = "ii";

        public static readonly string KEY_TIME = "t";

        public static readonly string KEY_PARAMS = "p";

        public static readonly string KEY_INVITEE_ID = "ee";

        public static readonly string KEY_INVITED_USERS = "iu";

        public static readonly string KEY_SERVER_ID = "ss";

        public static readonly string KEY_ROOM_ID = "rr";

        public static readonly string KEY_REPLY_ID = "ri";

        public static readonly int MAX_INVITATIONS_FROM_CLIENT_SIDE = 8;

        public static readonly int MIN_EXPIRY_TIME = 5;

        public static readonly int MAX_EXPIRY_TIME = 300;

        private List<object> invitedUsers;

        private int secondsForReply;

        private ISFSObject parameters;

        private ClusterTarget target;

        public ClusterInviteUsersRequest(ClusterTarget target, List<object> invitedUsers, int secondsForReply, ISFSObject parameters)
            : base(RequestType.ClusterInviteUsers)
        {
            this.target = target;
            this.invitedUsers = invitedUsers;
            this.secondsForReply = secondsForReply;
            this.parameters = parameters;
        }

        public override void Validate(SmartFox sfs)
        {
            List<string> list = new List<string>();
            if (invitedUsers == null || invitedUsers.Count < 1)
            {
                list.Add("No invitation(s) to send");
            }
            if (invitedUsers.Count > MAX_INVITATIONS_FROM_CLIENT_SIDE)
            {
                int mAX_INVITATIONS_FROM_CLIENT_SIDE = MAX_INVITATIONS_FROM_CLIENT_SIDE;
                list.Add("Too many invitations. Max allowed from client side is: " + mAX_INVITATIONS_FROM_CLIENT_SIDE);
            }
            if (secondsForReply < MIN_EXPIRY_TIME || secondsForReply > MAX_EXPIRY_TIME)
            {
                string[] obj = new string[5] { "SecondsForReply value is out of range (", null, null, null, null };
                int mAX_INVITATIONS_FROM_CLIENT_SIDE = MIN_EXPIRY_TIME;
                obj[1] = mAX_INVITATIONS_FROM_CLIENT_SIDE.ToString();
                obj[2] = "-";
                mAX_INVITATIONS_FROM_CLIENT_SIDE = MAX_EXPIRY_TIME;
                obj[3] = mAX_INVITATIONS_FROM_CLIENT_SIDE.ToString();
                obj[4] = ")";
                list.Add(string.Concat(obj));
            }
            if (target == null)
            {
                list.Add("Missing cluster target (server id and room id)");
            }
            if (list.Count > 0)
            {
                throw new SFSValidationError("ClusterInviteUsers request error", list);
            }
        }

        public override void Execute(SmartFox sfs)
        {
            List<int> list = new List<int>();
            foreach (object invitedUser in invitedUsers)
            {
                if (invitedUser is User)
                {
                    if (invitedUser as User != sfs.MySelf)
                    {
                        list.Add((invitedUser as User).Id);
                    }
                }
                else if (invitedUser is Buddy)
                {
                    list.Add((invitedUser as Buddy).Id);
                }
            }
            sfso.PutUtfString(KEY_SERVER_ID, target.ServerId);
            sfso.PutInt(KEY_ROOM_ID, target.RoomId);
            sfso.PutIntArray(KEY_INVITED_USERS, list.ToArray());
            sfso.PutShort(KEY_TIME, (short)secondsForReply);
            if (parameters != null)
            {
                sfso.PutSFSObject(KEY_PARAMS, parameters);
            }
        }
    }
}
#endif