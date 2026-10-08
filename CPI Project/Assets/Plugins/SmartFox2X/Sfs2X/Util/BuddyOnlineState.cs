#if UNITY_WEBGL
namespace Sfs2X.Util
{
	public enum BuddyOnlineState
	{
		ONLINE = 0,
		OFFLINE = 1,
		LEFT_THE_SERVER = 2
	}
}
#else
namespace Sfs2X.Util
{
    public enum BuddyOnlineState
    {
        ONLINE = 0,
        OFFLINE = 1,
        LEFT_THE_SERVER = 2
    }
}
#endif