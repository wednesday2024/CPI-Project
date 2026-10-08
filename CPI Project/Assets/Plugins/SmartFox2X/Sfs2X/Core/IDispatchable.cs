#if UNITY_WEBGL
namespace Sfs2X.Core
{
	public interface IDispatchable
	{
		EventDispatcher Dispatcher { get; }

		void AddEventListener(string eventType, EventListenerDelegate listener);
	}
}
#else
namespace Sfs2X.Core
{
    public interface IDispatchable
    {
        EventDispatcher Dispatcher { get; }

        void AddEventListener(string eventType, EventListenerDelegate listener);
    }
}
#endif