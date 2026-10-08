#if UNITY_WEBGL
namespace Sfs2X.Core.Sockets
{
	public delegate void DisconnectionDelegate(string reason = null);
}
#else
namespace Sfs2X.Core.Sockets
{
    public delegate void DisconnectionDelegate(string reason = null);
}
#endif