#if UNITY_WEBGL
namespace Sfs2X.Core
{
	public delegate void EventListenerDelegate(BaseEvent evt);
}
#else
namespace Sfs2X.Core
{
    public delegate void EventListenerDelegate(BaseEvent evt);
}
#endif