#if UNITY_WEBGL
using System.Collections.Generic;
using Sfs2X.Entities.Data;

namespace Sfs2X.Requests.Cluster
{
	public class ClusterRoomSettings : RoomSettings
	{
		private bool isPublic;

		private int minPlayersToStartGame;

		private List<object> invitedPlayers;

		private int invitationExpiryTime;

		private bool notifyGameStarted;

		private ISFSObject invitationParams;

		public bool IsPublic
		{
			get
			{
				return isPublic;
			}
			set
			{
				isPublic = value;
			}
		}

		public int MinPlayersToStartGame
		{
			get
			{
				return minPlayersToStartGame;
			}
			set
			{
				minPlayersToStartGame = value;
			}
		}

		public List<object> InvitedPlayers
		{
			get
			{
				return invitedPlayers;
			}
			set
			{
				invitedPlayers = value;
			}
		}

		public int InvitationExpiryTime
		{
			get
			{
				return invitationExpiryTime;
			}
			set
			{
				invitationExpiryTime = value;
			}
		}

		public bool NotifyGameStarted
		{
			get
			{
				return notifyGameStarted;
			}
			set
			{
				notifyGameStarted = value;
			}
		}

		public ISFSObject InvitationParams
		{
			get
			{
				return invitationParams;
			}
			set
			{
				invitationParams = value;
			}
		}

		public ClusterRoomSettings(string name)
			: base(name)
		{
			base.IsGame = true;
			isPublic = true;
			minPlayersToStartGame = 2;
			invitationExpiryTime = 30;
			invitedPlayers = new List<object>();
		}
	}
}
#else
using System.Collections.Generic;
using Sfs2X.Entities.Data;

namespace Sfs2X.Requests.Cluster
{
    public class ClusterRoomSettings : RoomSettings
    {
        private bool isPublic;

        private int minPlayersToStartGame;

        private List<object> invitedPlayers;

        private int invitationExpiryTime;

        private bool notifyGameStarted;

        private ISFSObject invitationParams;

        public bool IsPublic
        {
            get
            {
                return isPublic;
            }
            set
            {
                isPublic = value;
            }
        }

        public int MinPlayersToStartGame
        {
            get
            {
                return minPlayersToStartGame;
            }
            set
            {
                minPlayersToStartGame = value;
            }
        }

        public List<object> InvitedPlayers
        {
            get
            {
                return invitedPlayers;
            }
            set
            {
                invitedPlayers = value;
            }
        }

        public int InvitationExpiryTime
        {
            get
            {
                return invitationExpiryTime;
            }
            set
            {
                invitationExpiryTime = value;
            }
        }

        public bool NotifyGameStarted
        {
            get
            {
                return notifyGameStarted;
            }
            set
            {
                notifyGameStarted = value;
            }
        }

        public ISFSObject InvitationParams
        {
            get
            {
                return invitationParams;
            }
            set
            {
                invitationParams = value;
            }
        }

        public ClusterRoomSettings(string name)
            : base(name)
        {
            base.IsGame = true;
            isPublic = true;
            minPlayersToStartGame = 2;
            invitationExpiryTime = 30;
            invitedPlayers = new List<object>();
        }
    }
}
#endif