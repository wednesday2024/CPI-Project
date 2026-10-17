#if UNITY_WEBGL
#else
using System;

namespace Sfs2X.WebSocketSharp.Net
{
	internal class Chunk
	{
		private byte[] _data;

		private int _offset;

		public int ReadLeft => _data.Length - _offset;

		public Chunk(byte[] data)
		{
			_data = data;
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			int num = _data.Length - _offset;
			if (num == 0)
			{
				return 0;
			}
			if (count > num)
			{
				count = num;
			}
			Buffer.BlockCopy(_data, _offset, buffer, offset, count);
			_offset += count;
			return count;
		}
	}
}
#endif