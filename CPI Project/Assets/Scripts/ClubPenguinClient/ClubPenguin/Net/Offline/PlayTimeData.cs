namespace ClubPenguin.Net.Offline
{
	public struct PlayTimeData : IOfflineData
	{
		public long TotalSeconds;

		public void Init()
		{
			TotalSeconds = 0L;
		}
	}
}
