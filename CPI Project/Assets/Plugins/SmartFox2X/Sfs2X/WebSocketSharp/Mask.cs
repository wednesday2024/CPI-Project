#if UNITY_WEBGL
#else
namespace Sfs2X.WebSocketSharp
{
	internal enum Mask : byte
	{
		Off = 0,
		On = 1
	}
}
#endif