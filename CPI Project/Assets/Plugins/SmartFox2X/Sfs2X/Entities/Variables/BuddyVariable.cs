#if UNITY_WEBGL
namespace Sfs2X.Entities.Variables
{
	public interface BuddyVariable : Variable
	{
		bool IsOffline { get; }
	}
}
#else
namespace Sfs2X.Entities.Variables
{
    public interface BuddyVariable : Variable
    {
        bool IsOffline { get; }
    }
}
#endif