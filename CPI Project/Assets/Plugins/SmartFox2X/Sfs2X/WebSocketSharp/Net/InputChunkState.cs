namespace Sfs2X.WebSocketSharp.Net
{
	internal enum InputChunkState
	{
		None = 0,
		Data = 1,
		DataEnded = 2,
		Trailer = 3,
		End = 4
	}
}
