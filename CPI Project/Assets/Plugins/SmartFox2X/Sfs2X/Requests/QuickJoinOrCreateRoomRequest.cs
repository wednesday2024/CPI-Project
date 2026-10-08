#if UNITY_WEBGL
using System.Collections.Generic;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Match;
using Sfs2X.Exceptions;

namespace Sfs2X.Requests
{
	public class QuickJoinOrCreateRoomRequest : BaseRequest
	{
		public static readonly string KEY_MATCH_EXPRESSION = "me";

		public static readonly string KEY_GROUP_LIST = "gl";

		public static readonly string KEY_ROOM_SETTINGS = "rs";

		public static readonly string KEY_ROOM_TO_LEAVE = "tl";

		private MatchExpression matchExpression;

		private List<string> groupList;

		private RoomSettings settings;

		private Room roomToLeave;

		private CreateRoomRequest createRoomRequest;

		public QuickJoinOrCreateRoomRequest(MatchExpression matchExpression, List<string> groupList, RoomSettings settings, Room roomToLeave)
			: base(RequestType.QuickJoinOrCreateRoom)
		{
			Init(matchExpression, groupList, settings, roomToLeave);
		}

		public QuickJoinOrCreateRoomRequest(MatchExpression matchExpression, List<string> groupList, RoomSettings settings)
			: base(RequestType.QuickJoinOrCreateRoom)
		{
			Init(matchExpression, groupList, settings, null);
		}

		private void Init(MatchExpression matchExpression, List<string> groupList, RoomSettings settings, Room roomToLeave)
		{
			this.matchExpression = matchExpression;
			this.groupList = groupList;
			this.settings = settings;
			this.roomToLeave = roomToLeave;
			createRoomRequest = new CreateRoomRequest(settings, autoJoin: false, null);
		}

		public override void Validate(SmartFox sfs)
		{
			List<string> list = new List<string>();
			if (matchExpression == null)
			{
				list.Add("Missing match expression");
			}
			if (groupList == null || groupList.Count == 0)
			{
				list.Add("List of groups to search is null or empty");
			}
			if (settings == null)
			{
				list.Add("No Room settings provided");
			}
			else
			{
				try
				{
					createRoomRequest.Validate(sfs);
				}
				catch (SFSValidationError sFSValidationError)
				{
					list.AddRange(sFSValidationError.Errors);
				}
			}
			if (list.Count > 0)
			{
				throw new SFSValidationError("QuickJoinOrCreateRoom request error", list);
			}
		}

		public override void Execute(SmartFox sfs)
		{
			createRoomRequest.Execute(sfs);
			ISFSObject content = createRoomRequest.Message.Content;
			sfso.PutSFSArray(KEY_MATCH_EXPRESSION, matchExpression.ToSFSArray());
			sfso.PutUtfStringArray(KEY_GROUP_LIST, groupList.ToArray());
			sfso.PutSFSObject(KEY_ROOM_SETTINGS, content);
			if (roomToLeave != null)
			{
				sfso.PutInt(KEY_ROOM_TO_LEAVE, roomToLeave.Id);
			}
		}
	}
}
#else
using System.Collections.Generic;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Match;
using Sfs2X.Exceptions;

namespace Sfs2X.Requests
{
    public class QuickJoinOrCreateRoomRequest : BaseRequest
    {
        public static readonly string KEY_MATCH_EXPRESSION = "me";

        public static readonly string KEY_GROUP_LIST = "gl";

        public static readonly string KEY_ROOM_SETTINGS = "rs";

        public static readonly string KEY_ROOM_TO_LEAVE = "tl";

        private MatchExpression matchExpression;

        private List<string> groupList;

        private RoomSettings settings;

        private Room roomToLeave;

        private CreateRoomRequest createRoomRequest;

        public QuickJoinOrCreateRoomRequest(MatchExpression matchExpression, List<string> groupList, RoomSettings settings, Room roomToLeave)
            : base(RequestType.QuickJoinOrCreateRoom)
        {
            Init(matchExpression, groupList, settings, roomToLeave);
        }

        public QuickJoinOrCreateRoomRequest(MatchExpression matchExpression, List<string> groupList, RoomSettings settings)
            : base(RequestType.QuickJoinOrCreateRoom)
        {
            Init(matchExpression, groupList, settings, null);
        }

        private void Init(MatchExpression matchExpression, List<string> groupList, RoomSettings settings, Room roomToLeave)
        {
            this.matchExpression = matchExpression;
            this.groupList = groupList;
            this.settings = settings;
            this.roomToLeave = roomToLeave;
            createRoomRequest = new CreateRoomRequest(settings, autoJoin: false, null);
        }

        public override void Validate(SmartFox sfs)
        {
            List<string> list = new List<string>();
            if (matchExpression == null)
            {
                list.Add("Missing match expression");
            }
            if (groupList == null || groupList.Count == 0)
            {
                list.Add("List of groups to search is null or empty");
            }
            if (settings == null)
            {
                list.Add("No Room settings provided");
            }
            else
            {
                try
                {
                    createRoomRequest.Validate(sfs);
                }
                catch (SFSValidationError sFSValidationError)
                {
                    list.AddRange(sFSValidationError.Errors);
                }
            }
            if (list.Count > 0)
            {
                throw new SFSValidationError("QuickJoinOrCreateRoom request error", list);
            }
        }

        public override void Execute(SmartFox sfs)
        {
            createRoomRequest.Execute(sfs);
            ISFSObject content = createRoomRequest.Message.Content;
            sfso.PutSFSArray(KEY_MATCH_EXPRESSION, matchExpression.ToSFSArray());
            sfso.PutUtfStringArray(KEY_GROUP_LIST, groupList.ToArray());
            sfso.PutSFSObject(KEY_ROOM_SETTINGS, content);
            if (roomToLeave != null)
            {
                sfso.PutInt(KEY_ROOM_TO_LEAVE, roomToLeave.Id);
            }
        }
    }
}
#endif