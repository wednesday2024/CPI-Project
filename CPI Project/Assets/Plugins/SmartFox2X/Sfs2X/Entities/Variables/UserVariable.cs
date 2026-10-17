#if UNITY_WEBGL
namespace Sfs2X.Entities.Variables
{
	public interface UserVariable : Variable
	{
		bool IsPrivate { get; set; }
	}
}
#else
namespace Sfs2X.Entities.Variables
{
    public interface UserVariable : Variable
    {
        bool IsPrivate { get; set; }
    }
}
#endif