#if UNITY_WEBGL
#else
namespace Sfs2X.WebSocketSharp.Server
{
	internal enum ServerState
	{
		Ready = 0,
		Start = 1,
		ShuttingDown = 2,
		Stop = 3
	}
}
#endif