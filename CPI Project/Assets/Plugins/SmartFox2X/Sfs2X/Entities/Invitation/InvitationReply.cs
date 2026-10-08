#if UNITY_WEBGL
namespace Sfs2X.Entities.Invitation
{
	public enum InvitationReply
	{
		ACCEPT = 0,
		REFUSE = 1,
		EXPIRED = 255
	}
}
#else
namespace Sfs2X.Entities.Invitation
{
    public enum InvitationReply
    {
        ACCEPT = 0,
        REFUSE = 1,
        EXPIRED = 255
    }
}
#endif