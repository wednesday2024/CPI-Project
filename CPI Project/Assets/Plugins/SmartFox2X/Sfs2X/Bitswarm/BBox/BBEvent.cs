using System.Collections.Generic;
using Sfs2X.Core;

namespace Sfs2X.Bitswarm.BBox
{
	public class BBEvent : BaseEvent
	{
		public static readonly string CONNECT = "bb-connect";

		public static readonly string DISCONNECT = "bb-disconnect";

		public static readonly string DATA = "bb-data";

		public static readonly string IO_ERROR = "bb-ioError";

		public static readonly string SECURITY_ERROR = "bb-securityError";

		public BBEvent(string type)
			: base(type, null)
		{
		}

		public BBEvent(string type, Dictionary<string, object> arguments)
			: base(type, arguments)
		{
		}
	}
}
