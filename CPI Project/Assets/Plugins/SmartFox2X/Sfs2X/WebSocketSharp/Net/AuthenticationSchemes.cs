#if UNITY_WEBGL
#else
namespace Sfs2X.WebSocketSharp.Net
{
	public enum AuthenticationSchemes
	{
		None = 0,
		Digest = 1,
		Basic = 8,
		Anonymous = 0x8000
	}
}
#endif