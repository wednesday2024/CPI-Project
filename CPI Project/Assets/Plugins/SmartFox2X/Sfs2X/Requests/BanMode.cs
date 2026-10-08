#if UNITY_WEBGL
namespace Sfs2X.Requests
{
	public enum BanMode
	{
		BY_ADDRESS = 0,
		BY_NAME = 1
	}
}
#else
namespace Sfs2X.Requests
{
    public enum BanMode
    {
        BY_ADDRESS = 0,
        BY_NAME = 1
    }
}
#endif