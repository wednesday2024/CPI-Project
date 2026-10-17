#if UNITY_WEBGL
#else
namespace Sfs2X.WebSocketSharp
{
	public enum LogLevel
	{
		Trace = 0,
		Debug = 1,
		Info = 2,
		Warn = 3,
		Error = 4,
		Fatal = 5,
		None = 6
	}
}
#endif