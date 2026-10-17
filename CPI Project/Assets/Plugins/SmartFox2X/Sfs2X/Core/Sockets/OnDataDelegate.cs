#if UNITY_WEBGL
namespace Sfs2X.Core.Sockets
{
	public delegate void OnDataDelegate(byte[] msg);
}
#else
namespace Sfs2X.Core.Sockets
{
    public delegate void OnDataDelegate(byte[] msg);
}
#endif