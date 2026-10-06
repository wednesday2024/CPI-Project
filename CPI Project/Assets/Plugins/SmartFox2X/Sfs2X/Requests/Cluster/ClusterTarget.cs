namespace Sfs2X.Requests.Cluster
{
	public class ClusterTarget
	{
		private string serverId;

		private int roomId;

		public string ServerId => serverId;

		public int RoomId => roomId;

		public ClusterTarget(string serverId, int roomId)
		{
			this.serverId = serverId;
			this.roomId = roomId;
		}
	}
}
