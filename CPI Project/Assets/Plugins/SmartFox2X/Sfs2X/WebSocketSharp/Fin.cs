#if UNITY_WEBGL
#else
namespace Sfs2X.WebSocketSharp
{
	internal enum Fin : byte
	{
		More = 0,
		Final = 1
	}
}
#endif