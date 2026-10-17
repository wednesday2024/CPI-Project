#if UNITY_WEBGL
namespace Sfs2X.Entities.Variables
{
	public interface RoomVariable : Variable
	{
		bool IsPrivate { get; set; }

		bool IsPersistent { get; set; }
	}
}
#else
namespace Sfs2X.Entities.Variables
{
    public interface RoomVariable : Variable
    {
        bool IsPrivate { get; set; }

        bool IsPersistent { get; set; }
    }
}
#endif