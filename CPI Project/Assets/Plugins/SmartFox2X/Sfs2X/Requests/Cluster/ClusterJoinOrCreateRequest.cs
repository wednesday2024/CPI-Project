#if UNITY_WEBGL
using System.Collections.Generic;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Match;
using Sfs2X.Exceptions;
using Sfs2X.Requests.Game;
using Sfs2X.Requests.MMO;

namespace Sfs2X.Requests.Cluster
{
	public class ClusterJoinOrCreateRequest : BaseRequest
	{
		private static readonly string KEY_GROUP_LIST = "gl";

		public static readonly string KEY_ROOM_SETTINGS = "rs";

		public static readonly string KEY_MATCH_EXPRESSION = "me";

		public static readonly string KEY_IS_PUBLIC = "gip";

		public static readonly string KEY_MIN_PLAYERS = "gmp";

		public static readonly string KEY_INVITED_PLAYERS = "ginp";

		public static readonly string KEY_INVITATION_EXPIRY = "gie";

		public static readonly string KEY_NOTIFY_GAME_STARTED = "gns";

		public static readonly string KEY_INVITATION_PARAMS = "ip";

		private MatchExpression matchExpression;

		private List<string> groupNames;

		private RoomSettings settings;

		private CreateRoomRequest createRoomRequest;

		public ClusterJoinOrCreateRequest(MatchExpression matchExpression, List<string> groupNames, RoomSettings settings)
			: base(RequestType.ClusterJoinOrCreateRequest)
		{
			Init(matchExpression, groupNames, settings);
		}

		public ClusterJoinOrCreateRequest(MatchExpression matchExpression, List<string> groupNames)
			: base(RequestType.ClusterJoinOrCreateRequest)
		{
			Init(matchExpression, groupNames, null);
		}

		public ClusterJoinOrCreateRequest(MatchExpression matchExpression)
			: base(RequestType.ClusterJoinOrCreateRequest)
		{
			Init(matchExpression, null, null);
		}

		public ClusterJoinOrCreateRequest(RoomSettings settings)
			: base(RequestType.ClusterJoinOrCreateRequest)
		{
			Init(null, null, settings);
		}

		private void Init(MatchExpression matchExpression, List<string> groupNames, RoomSettings settings)
		{
			this.matchExpression = matchExpression;
			this.groupNames = groupNames;
			this.settings = settings;
			if (settings != null)
			{
				createRoomRequest = new CreateRoomRequest(settings, autoJoin: false, null);
			}
		}

		public override void Validate(SmartFox sfs)
		{
			List<string> list = new List<string>();
			if (settings != null)
			{
				bool flag = settings is ClusterRoomSettings;
				bool flag2 = settings is MMORoomSettings;
				if (!flag && !flag2)
				{
					list.Add("Unsupported RoomSetting type: " + settings.GetType().Name + ". Accepted types are: ClusterRoomSettings and MMORoomSettings");
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
					if (flag)
					{
						ValidateClusterRoom((ClusterRoomSettings)settings, list);
					}
				}
			}
			if (list.Count > 0)
			{
				throw new SFSValidationError("ClusterJoinOrCreateRequest request error", list);
			}
		}

		public override void Execute(SmartFox sfs)
		{
			if (matchExpression != null)
			{
				sfso.PutSFSArray(KEY_MATCH_EXPRESSION, matchExpression.ToSFSArray());
			}
			if (groupNames != null && groupNames.Count > 0)
			{
				sfso.PutUtfStringArray(KEY_GROUP_LIST, groupNames.ToArray());
			}
			if (settings == null)
			{
				return;
			}
			createRoomRequest.Execute(sfs);
			ISFSObject content = createRoomRequest.Message.Content;
			if (settings is ClusterRoomSettings)
			{
				ClusterRoomSettings clusterRoomSettings = (ClusterRoomSettings)settings;
				content.PutBool(KEY_IS_PUBLIC, clusterRoomSettings.IsPublic);
				content.PutShort(KEY_MIN_PLAYERS, (short)clusterRoomSettings.MinPlayersToStartGame);
				content.PutShort(KEY_INVITATION_EXPIRY, (short)clusterRoomSettings.InvitationExpiryTime);
				content.PutBool(KEY_NOTIFY_GAME_STARTED, clusterRoomSettings.NotifyGameStarted);
				List<object> invitedPlayers = clusterRoomSettings.InvitedPlayers;
				if (invitedPlayers != null)
				{
					List<int> list = new List<int>(invitedPlayers.Count);
					foreach (object item in invitedPlayers)
					{
						if (item is User)
						{
							list.Add(((User)item).Id);
						}
						else if (item is Buddy)
						{
							list.Add(((Buddy)item).Id);
						}
					}
					content.PutIntArray(KEY_INVITED_PLAYERS, list.ToArray());
				}
				if (clusterRoomSettings.InvitationParams != null)
				{
					content.PutSFSObject(KEY_INVITATION_PARAMS, clusterRoomSettings.InvitationParams);
				}
			}
			sfso.PutSFSObject(KEY_ROOM_SETTINGS, content);
		}

		private void ValidateClusterRoom(ClusterRoomSettings settings, List<string> errors)
		{
			if (settings.MinPlayersToStartGame > settings.MaxUsers)
			{
				errors.Add("MinPlayersToStartGame cannot be greater than MaxUsers");
			}
			if (settings.InvitationExpiryTime < InviteUsersRequest.MIN_EXPIRY_TIME || settings.InvitationExpiryTime > InviteUsersRequest.MAX_EXPIRY_TIME)
			{
				string[] obj = new string[5] { "Expiry time value is out of range (", null, null, null, null };
				int mIN_EXPIRY_TIME = InviteUsersRequest.MIN_EXPIRY_TIME;
				obj[1] = mIN_EXPIRY_TIME.ToString();
				obj[2] = "-";
				mIN_EXPIRY_TIME = InviteUsersRequest.MAX_EXPIRY_TIME;
				obj[3] = mIN_EXPIRY_TIME.ToString();
				obj[4] = ")";
				errors.Add(string.Concat(obj));
			}
			if (settings.InvitedPlayers != null && settings.InvitedPlayers.Count > InviteUsersRequest.MAX_INVITATIONS_FROM_CLIENT_SIDE)
			{
				int mIN_EXPIRY_TIME = InviteUsersRequest.MAX_INVITATIONS_FROM_CLIENT_SIDE;
				errors.Add("Cannot invite more than " + mIN_EXPIRY_TIME + " players from client side");
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
using Sfs2X.Requests.Game;
using Sfs2X.Requests.MMO;

namespace Sfs2X.Requests.Cluster
{
    public class ClusterJoinOrCreateRequest : BaseRequest
    {
        private static readonly string KEY_GROUP_LIST = "gl";

        public static readonly string KEY_ROOM_SETTINGS = "rs";

        public static readonly string KEY_MATCH_EXPRESSION = "me";

        public static readonly string KEY_IS_PUBLIC = "gip";

        public static readonly string KEY_MIN_PLAYERS = "gmp";

        public static readonly string KEY_INVITED_PLAYERS = "ginp";

        public static readonly string KEY_INVITATION_EXPIRY = "gie";

        public static readonly string KEY_NOTIFY_GAME_STARTED = "gns";

        public static readonly string KEY_INVITATION_PARAMS = "ip";

        private MatchExpression matchExpression;

        private List<string> groupNames;

        private RoomSettings settings;

        private CreateRoomRequest createRoomRequest;

        public ClusterJoinOrCreateRequest(MatchExpression matchExpression, List<string> groupNames, RoomSettings settings)
            : base(RequestType.ClusterJoinOrCreateRequest)
        {
            Init(matchExpression, groupNames, settings);
        }

        public ClusterJoinOrCreateRequest(MatchExpression matchExpression, List<string> groupNames)
            : base(RequestType.ClusterJoinOrCreateRequest)
        {
            Init(matchExpression, groupNames, null);
        }

        public ClusterJoinOrCreateRequest(MatchExpression matchExpression)
            : base(RequestType.ClusterJoinOrCreateRequest)
        {
            Init(matchExpression, null, null);
        }

        public ClusterJoinOrCreateRequest(RoomSettings settings)
            : base(RequestType.ClusterJoinOrCreateRequest)
        {
            Init(null, null, settings);
        }

        private void Init(MatchExpression matchExpression, List<string> groupNames, RoomSettings settings)
        {
            this.matchExpression = matchExpression;
            this.groupNames = groupNames;
            this.settings = settings;
            if (settings != null)
            {
                createRoomRequest = new CreateRoomRequest(settings, autoJoin: false, null);
            }
        }

        public override void Validate(SmartFox sfs)
        {
            List<string> list = new List<string>();
            if (settings != null)
            {
                bool flag = settings is ClusterRoomSettings;
                bool flag2 = settings is MMORoomSettings;
                if (!flag && !flag2)
                {
                    list.Add("Unsupported RoomSetting type: " + settings.GetType().Name + ". Accepted types are: ClusterRoomSettings and MMORoomSettings");
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
                    if (flag)
                    {
                        ValidateClusterRoom((ClusterRoomSettings)settings, list);
                    }
                }
            }
            if (list.Count > 0)
            {
                throw new SFSValidationError("ClusterJoinOrCreateRequest request error", list);
            }
        }

        public override void Execute(SmartFox sfs)
        {
            if (matchExpression != null)
            {
                sfso.PutSFSArray(KEY_MATCH_EXPRESSION, matchExpression.ToSFSArray());
            }
            if (groupNames != null && groupNames.Count > 0)
            {
                sfso.PutUtfStringArray(KEY_GROUP_LIST, groupNames.ToArray());
            }
            if (settings == null)
            {
                return;
            }
            createRoomRequest.Execute(sfs);
            ISFSObject content = createRoomRequest.Message.Content;
            if (settings is ClusterRoomSettings)
            {
                ClusterRoomSettings clusterRoomSettings = (ClusterRoomSettings)settings;
                content.PutBool(KEY_IS_PUBLIC, clusterRoomSettings.IsPublic);
                content.PutShort(KEY_MIN_PLAYERS, (short)clusterRoomSettings.MinPlayersToStartGame);
                content.PutShort(KEY_INVITATION_EXPIRY, (short)clusterRoomSettings.InvitationExpiryTime);
                content.PutBool(KEY_NOTIFY_GAME_STARTED, clusterRoomSettings.NotifyGameStarted);
                List<object> invitedPlayers = clusterRoomSettings.InvitedPlayers;
                if (invitedPlayers != null)
                {
                    List<int> list = new List<int>(invitedPlayers.Count);
                    foreach (object item in invitedPlayers)
                    {
                        if (item is User)
                        {
                            list.Add(((User)item).Id);
                        }
                        else if (item is Buddy)
                        {
                            list.Add(((Buddy)item).Id);
                        }
                    }
                    content.PutIntArray(KEY_INVITED_PLAYERS, list.ToArray());
                }
                if (clusterRoomSettings.InvitationParams != null)
                {
                    content.PutSFSObject(KEY_INVITATION_PARAMS, clusterRoomSettings.InvitationParams);
                }
            }
            sfso.PutSFSObject(KEY_ROOM_SETTINGS, content);
        }

        private void ValidateClusterRoom(ClusterRoomSettings settings, List<string> errors)
        {
            if (settings.MinPlayersToStartGame > settings.MaxUsers)
            {
                errors.Add("MinPlayersToStartGame cannot be greater than MaxUsers");
            }
            if (settings.InvitationExpiryTime < InviteUsersRequest.MIN_EXPIRY_TIME || settings.InvitationExpiryTime > InviteUsersRequest.MAX_EXPIRY_TIME)
            {
                string[] obj = new string[5] { "Expiry time value is out of range (", null, null, null, null };
                int mIN_EXPIRY_TIME = InviteUsersRequest.MIN_EXPIRY_TIME;
                obj[1] = mIN_EXPIRY_TIME.ToString();
                obj[2] = "-";
                mIN_EXPIRY_TIME = InviteUsersRequest.MAX_EXPIRY_TIME;
                obj[3] = mIN_EXPIRY_TIME.ToString();
                obj[4] = ")";
                errors.Add(string.Concat(obj));
            }
            if (settings.InvitedPlayers != null && settings.InvitedPlayers.Count > InviteUsersRequest.MAX_INVITATIONS_FROM_CLIENT_SIDE)
            {
                int mIN_EXPIRY_TIME = InviteUsersRequest.MAX_INVITATIONS_FROM_CLIENT_SIDE;
                errors.Add("Cannot invite more than " + mIN_EXPIRY_TIME + " players from client side");
            }
        }
    }
}
#endif