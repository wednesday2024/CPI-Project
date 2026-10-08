#if UNITY_WEBGL
namespace Sfs2X.Entities.Variables
{
	public class ReservedBuddyVariables
	{
		public static readonly string BV_ONLINE = "$__BV_ONLINE__";

		public static readonly string BV_STATE = "$__BV_STATE__";

		public static readonly string BV_NICKNAME = "$__BV_NICKNAME__";
	}
}
#else
namespace Sfs2X.Entities.Variables
{
    public class ReservedBuddyVariables
    {
        public static readonly string BV_ONLINE = "$__BV_ONLINE__";

        public static readonly string BV_STATE = "$__BV_STATE__";

        public static readonly string BV_NICKNAME = "$__BV_NICKNAME__";
    }
}
#endif