#if UNITY_WEBGL
using System.Net.Sockets;

namespace Sfs2X.Core.Sockets
{
	public delegate void OnErrorDelegate(string error, SocketError se);
}
#else
using System.Net.Sockets;

namespace Sfs2X.Core.Sockets
{
    public delegate void OnErrorDelegate(string error, SocketError se);
}
#endif