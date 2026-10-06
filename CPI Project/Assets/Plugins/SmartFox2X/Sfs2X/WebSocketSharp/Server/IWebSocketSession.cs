using System;

namespace Sfs2X.WebSocketSharp.Server
{
	public interface IWebSocketSession
	{
		string ID { get; }

		DateTime StartTime { get; }

		WebSocket WebSocket { get; }
	}
}
