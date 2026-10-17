#if UNITY_WEBGL
namespace Sfs2X.Entities.Variables
{
	public class ReservedRoomVariables
	{
		public static readonly string RV_GAME_STARTED = "$GS";
	}
}
#else
namespace Sfs2X.Entities.Variables
{
    public class ReservedRoomVariables
    {
        public static readonly string RV_GAME_STARTED = "$GS";
    }
}
#endif