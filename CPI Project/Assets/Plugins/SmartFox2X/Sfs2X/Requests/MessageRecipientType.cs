#if UNITY_WEBGL
namespace Sfs2X.Requests
{
	public enum MessageRecipientType
	{
		TO_USER = 0,
		TO_ROOM = 1,
		TO_GROUP = 2,
		TO_ZONE = 3
	}
}
#else
namespace Sfs2X.Requests
{
    public enum MessageRecipientType
    {
        TO_USER = 0,
        TO_ROOM = 1,
        TO_GROUP = 2,
        TO_ZONE = 3
    }
}
#endif