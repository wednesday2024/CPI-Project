#if UNITY_WEBGL
#else
namespace Sfs2X.WebSocketSharp
{
	public enum CompressionMethod : byte
	{
		None = 0,
		Deflate = 1
	}
}
#endif