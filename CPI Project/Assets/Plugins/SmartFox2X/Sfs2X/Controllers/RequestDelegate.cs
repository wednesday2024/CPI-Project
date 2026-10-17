#if UNITY_WEBGL
using Sfs2X.Bitswarm;

namespace Sfs2X.Controllers
{
	public delegate void RequestDelegate(IMessage msg);
}
#else
using Sfs2X.Bitswarm;

namespace Sfs2X.Controllers
{
    public delegate void RequestDelegate(IMessage msg);
}
#endif