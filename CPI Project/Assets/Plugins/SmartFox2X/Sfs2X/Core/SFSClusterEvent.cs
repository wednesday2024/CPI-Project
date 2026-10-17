#if UNITY_WEBGL
using System.Collections.Generic;

namespace Sfs2X.Core
{
	public class SFSClusterEvent : BaseEvent
	{
		public static readonly string CONNECTION_REQUIRED = "connectionRequired";

		public static readonly string LOAD_BALANCER_ERROR = "loadBalancerError";

		public SFSClusterEvent(string type, Dictionary<string, object> data)
			: base(type, data)
		{
		}

		public SFSClusterEvent(string type)
			: base(type)
		{
		}
	}
}
#else
using System.Collections.Generic;

namespace Sfs2X.Core
{
    public class SFSClusterEvent : BaseEvent
    {
        public static readonly string CONNECTION_REQUIRED = "connectionRequired";

        public static readonly string LOAD_BALANCER_ERROR = "loadBalancerError";

        public SFSClusterEvent(string type, Dictionary<string, object> data)
            : base(type, data)
        {
        }

        public SFSClusterEvent(string type)
            : base(type)
        {
        }
    }
}
#endif