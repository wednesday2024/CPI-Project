#if UNITY_WEBGL
namespace Sfs2X.Core.Sockets
{
	public delegate void ConnectionDelegate();
}
#else
namespace Sfs2X.Core.Sockets
{
    public delegate void ConnectionDelegate();
}
#endif