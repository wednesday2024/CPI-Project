#if UNITY_WEBGL
#else
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sfs2X.WebSocketSharp
{
	internal class PayloadData : IEnumerable<byte>, IEnumerable
	{
		private byte[] _data;

		private long _extDataLength;

		private long _length;

		public static readonly PayloadData Empty;

		public static readonly ulong MaxLength;

		internal ushort Code => (ushort)((_length >= 2) ? _data.SubArray(0, 2).ToUInt16(ByteOrder.Big) : 1005);

		internal long ExtensionDataLength
		{
			get
			{
				return _extDataLength;
			}
			set
			{
				_extDataLength = value;
			}
		}

		internal bool HasReservedCode => _length >= 2 && Code.IsReservedStatusCode();

		internal string Reason
		{
			get
			{
				if (_length <= 2)
				{
					return string.Empty;
				}
				byte[] bytes = _data.SubArray(2L, _length - 2);
				string s;
				return bytes.TryGetUTF8DecodedString(out s) ? s : string.Empty;
			}
		}

		public byte[] ApplicationData => (_extDataLength > 0) ? _data.SubArray(_extDataLength, _length - _extDataLength) : _data;

		public byte[] ExtensionData => (_extDataLength > 0) ? _data.SubArray(0L, _extDataLength) : WebSocket.EmptyBytes;

		public ulong Length => (ulong)_length;

		static PayloadData()
		{
			Empty = new PayloadData(WebSocket.EmptyBytes, 0L);
			MaxLength = 9223372036854775807uL;
		}

		internal PayloadData(byte[] data)
			: this(data, data.LongLength)
		{
		}

		internal PayloadData(byte[] data, long length)
		{
			_data = data;
			_length = length;
		}

		internal PayloadData(ushort code, string reason)
		{
			_data = code.Append(reason);
			_length = _data.LongLength;
		}

		internal void Mask(byte[] key)
		{
			for (long num = 0L; num < _length; num++)
			{
				_data[num] ^= key[num % 4];
			}
		}

		public IEnumerator<byte> GetEnumerator()
		{
			byte[] data = _data;
			for (int i = 0; i < data.Length; i++)
			{
				yield return data[i];
			}
		}

		public byte[] ToArray()
		{
			return _data;
		}

		public override string ToString()
		{
			return BitConverter.ToString(_data);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
#endif