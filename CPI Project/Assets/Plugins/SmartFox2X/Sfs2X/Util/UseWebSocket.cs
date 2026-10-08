#if UNITY_WEBGL
namespace Sfs2X.Util
{
	public enum UseWebSocket
	{
		WS = 0,
		WSS = 1,
		WS_BIN = 2,
		WSS_BIN = 3
	}
}
#else
namespace Sfs2X.Util
{
    public enum UseWebSocket
    {
        WS = 0,
        WSS = 1,
        WS_BIN = 2,
        WSS_BIN = 3
    }
}
#endif