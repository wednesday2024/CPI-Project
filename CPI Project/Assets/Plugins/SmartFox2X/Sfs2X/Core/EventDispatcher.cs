using System;
using System.Collections;

namespace Sfs2X.Core
{
	public class EventDispatcher
	{
		private object target;

		private Hashtable listeners = new Hashtable();

		public EventDispatcher(object target)
		{
			this.target = target;
		}

		public void AddEventListener(string eventType, EventListenerDelegate listener)
		{
			EventListenerDelegate a = listeners[eventType] as EventListenerDelegate;
			a = (EventListenerDelegate)Delegate.Combine(a, listener);
			listeners[eventType] = a;
		}

		public void RemoveEventListener(string eventType, EventListenerDelegate listener)
		{
			EventListenerDelegate eventListenerDelegate = listeners[eventType] as EventListenerDelegate;
			if (eventListenerDelegate != null)
			{
				eventListenerDelegate = (EventListenerDelegate)Delegate.Remove(eventListenerDelegate, listener);
			}
			listeners[eventType] = eventListenerDelegate;
		}

		public void DispatchEvent(BaseEvent evt)
		{
			if (listeners[evt.Type] is EventListenerDelegate eventListenerDelegate)
			{
				evt.Target = target;
				try
				{
					eventListenerDelegate(evt);
				}
				catch (Exception ex)
				{
					throw new Exception("Error dispatching event " + evt.Type + ": " + ex.Message + " " + ex.StackTrace, ex);
				}
			}
		}

		public void RemoveAll()
		{
			listeners.Clear();
		}
	}
}
