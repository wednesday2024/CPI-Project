#if UNITY_WEBGL
#else
namespace Sfs2X.WebSocketSharp.Net
{
	internal enum InputState
	{
		RequestLine = 0,
		Headers = 1
	}
}
#endif