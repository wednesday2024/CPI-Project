#if UNITY_WEBGL
namespace Sfs2X.Core.Sockets
{
	public delegate void OnStringDataDelegate(string msg);
}
#else
namespace Sfs2X.Core.Sockets
{
    public delegate void OnStringDataDelegate(string msg);
}
#endif