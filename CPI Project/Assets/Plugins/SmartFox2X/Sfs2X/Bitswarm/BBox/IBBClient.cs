using Sfs2X.Core;
using Sfs2X.Util;

namespace Sfs2X.Bitswarm.BBox
{
	public interface IBBClient : IDispatchable
	{
		bool IsConnected { get; }

		string SessionId { get; }

		bool IsDebug { get; set; }

		void Connect(ConfigData cfg);

		void Send(ByteArray binData);

		void Close(string reason);
	}
}
