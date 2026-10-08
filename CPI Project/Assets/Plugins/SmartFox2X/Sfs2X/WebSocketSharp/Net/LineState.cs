#if UNITY_WEBGL
#else
namespace Sfs2X.WebSocketSharp.Net
{
	internal enum LineState
	{
		None = 0,
		Cr = 1,
		Lf = 2
	}
}
#endif