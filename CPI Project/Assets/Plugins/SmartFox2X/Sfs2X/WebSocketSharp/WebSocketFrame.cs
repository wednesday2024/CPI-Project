using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sfs2X.WebSocketSharp
{
	internal class WebSocketFrame : IEnumerable<byte>, IEnumerable
	{
		private static readonly int _defaultHeaderLength;

		private static readonly int _defaultMaskingKeyLength;

		private byte[] _extPayloadLength;

		private Fin _fin;

		private Mask _mask;

		private byte[] _maskingKey;

		private Opcode _opcode;

		private PayloadData _payloadData;

		private byte _payloadLength;

		private Rsv _rsv1;

		private Rsv _rsv2;

		private Rsv _rsv3;

		internal ulong ExactPayloadLength => (_payloadLength < 126) ? _payloadLength : ((_payloadLength == 126) ? _extPayloadLength.ToUInt16(ByteOrder.Big) : _extPayloadLength.ToUInt64(ByteOrder.Big));

		internal int ExtendedPayloadLengthWidth => (_payloadLength >= 126) ? ((_payloadLength == 126) ? 2 : 8) : 0;

		public byte[] ExtendedPayloadLength => _extPayloadLength;

		public Fin Fin => _fin;

		public bool IsBinary => _opcode == Opcode.Binary;

		public bool IsClose => _opcode == Opcode.Close;

		public bool IsCompressed => _rsv1 == Rsv.On;

		public bool IsContinuation => _opcode == Opcode.Cont;

		public bool IsControl => (int)_opcode >= 8;

		public bool IsData => _opcode == Opcode.Text || _opcode == Opcode.Binary;

		public bool IsFinal => _fin == Fin.Final;

		public bool IsFragment => _fin == Fin.More || _opcode == Opcode.Cont;

		public bool IsMasked => _mask == Mask.On;

		public bool IsPing => _opcode == Opcode.Ping;

		public bool IsPong => _opcode == Opcode.Pong;

		public bool IsText => _opcode == Opcode.Text;

		public ulong Length => (ulong)(_defaultHeaderLength + _extPayloadLength.Length + _maskingKey.Length) + _payloadData.Length;

		public Mask Mask => _mask;

		public byte[] MaskingKey => _maskingKey;

		public Opcode Opcode => _opcode;

		public PayloadData PayloadData => _payloadData;

		public byte PayloadLength => _payloadLength;

		public Rsv Rsv1 => _rsv1;

		public Rsv Rsv2 => _rsv2;

		public Rsv Rsv3 => _rsv3;

		static WebSocketFrame()
		{
			_defaultHeaderLength = 2;
			_defaultMaskingKeyLength = 4;
		}

		private WebSocketFrame()
		{
		}

		internal WebSocketFrame(Fin fin, Opcode opcode, byte[] data, bool compressed, bool mask)
			: this(fin, opcode, new PayloadData(data), compressed, mask)
		{
		}

		internal WebSocketFrame(Fin fin, Opcode opcode, PayloadData payloadData, bool compressed, bool mask)
		{
			_fin = fin;
			_opcode = opcode;
			_rsv1 = (compressed ? Rsv.On : Rsv.Off);
			_rsv2 = Rsv.Off;
			_rsv3 = Rsv.Off;
			ulong length = payloadData.Length;
			if (length < 126)
			{
				_payloadLength = (byte)length;
				_extPayloadLength = WebSocket.EmptyBytes;
			}
			else if (length < 65536)
			{
				_payloadLength = 126;
				_extPayloadLength = ((ushort)length).ToByteArray(ByteOrder.Big);
			}
			else
			{
				_payloadLength = 127;
				_extPayloadLength = length.ToByteArray(ByteOrder.Big);
			}
			if (mask)
			{
				_mask = Mask.On;
				_maskingKey = createMaskingKey();
				payloadData.Mask(_maskingKey);
			}
			else
			{
				_mask = Mask.Off;
				_maskingKey = WebSocket.EmptyBytes;
			}
			_payloadData = payloadData;
		}

		private static byte[] createMaskingKey()
		{
			byte[] array = new byte[_defaultMaskingKeyLength];
			WebSocket.RandomNumber.GetBytes(array);
			return array;
		}

		private static WebSocketFrame processHeader(byte[] header)
		{
			if (header.Length != _defaultHeaderLength)
			{
				string message = "The header part of a frame could not be read.";
				throw new WebSocketException(message);
			}
			Fin fin = (((header[0] & 0x80) == 128) ? Fin.Final : Fin.More);
			Rsv rsv = (((header[0] & 0x40) == 64) ? Rsv.On : Rsv.Off);
			Rsv rsv2 = (((header[0] & 0x20) == 32) ? Rsv.On : Rsv.Off);
			Rsv rsv3 = (((header[0] & 0x10) == 16) ? Rsv.On : Rsv.Off);
			byte opcode = (byte)(header[0] & 0xF);
			Mask mask = (((header[1] & 0x80) == 128) ? Mask.On : Mask.Off);
			byte payloadLength = (byte)(header[1] & 0x7F);
			if (!opcode.IsSupportedOpcode())
			{
				string message2 = "The opcode of a frame is not supported.";
				throw new WebSocketException(CloseStatusCode.UnsupportedData, message2);
			}
			WebSocketFrame webSocketFrame = new WebSocketFrame();
			webSocketFrame._fin = fin;
			webSocketFrame._rsv1 = rsv;
			webSocketFrame._rsv2 = rsv2;
			webSocketFrame._rsv3 = rsv3;
			webSocketFrame._opcode = (Opcode)opcode;
			webSocketFrame._mask = mask;
			webSocketFrame._payloadLength = payloadLength;
			return webSocketFrame;
		}

		private static WebSocketFrame readExtendedPayloadLength(Stream stream, WebSocketFrame frame)
		{
			int extendedPayloadLengthWidth = frame.ExtendedPayloadLengthWidth;
			if (extendedPayloadLengthWidth == 0)
			{
				frame._extPayloadLength = WebSocket.EmptyBytes;
				return frame;
			}
			byte[] array = stream.ReadBytes(extendedPayloadLengthWidth);
			if (array.Length != extendedPayloadLengthWidth)
			{
				string message = "The extended payload length of a frame could not be read.";
				throw new WebSocketException(message);
			}
			frame._extPayloadLength = array;
			return frame;
		}

		private static void readExtendedPayloadLengthAsync(Stream stream, WebSocketFrame frame, Action<WebSocketFrame> completed, Action<Exception> error)
		{
			int len = frame.ExtendedPayloadLengthWidth;
			if (len == 0)
			{
				frame._extPayloadLength = WebSocket.EmptyBytes;
				completed(frame);
				return;
			}
			stream.ReadBytesAsync(len, (byte[] bytes) =>
			{
				if (bytes.Length != len)
				{
					string message = "The extended payload length of a frame could not be read.";
					throw new WebSocketException(message);
				}
				frame._extPayloadLength = bytes;
				completed(frame);
			}, error);
		}

		private static WebSocketFrame readHeader(Stream stream)
		{
			byte[] header = stream.ReadBytes(_defaultHeaderLength);
			return processHeader(header);
		}

		private static void readHeaderAsync(Stream stream, Action<WebSocketFrame> completed, Action<Exception> error)
		{
			stream.ReadBytesAsync(_defaultHeaderLength, (byte[] bytes) =>
			{
				WebSocketFrame obj = processHeader(bytes);
				completed(obj);
			}, error);
		}

		private static WebSocketFrame readMaskingKey(Stream stream, WebSocketFrame frame)
		{
			if (!frame.IsMasked)
			{
				frame._maskingKey = WebSocket.EmptyBytes;
				return frame;
			}
			byte[] array = stream.ReadBytes(_defaultMaskingKeyLength);
			if (array.Length != _defaultMaskingKeyLength)
			{
				string message = "The masking key of a frame could not be read.";
				throw new WebSocketException(message);
			}
			frame._maskingKey = array;
			return frame;
		}

		private static void readMaskingKeyAsync(Stream stream, WebSocketFrame frame, Action<WebSocketFrame> completed, Action<Exception> error)
		{
			if (!frame.IsMasked)
			{
				frame._maskingKey = WebSocket.EmptyBytes;
				completed(frame);
				return;
			}
			stream.ReadBytesAsync(_defaultMaskingKeyLength, (byte[] bytes) =>
			{
				if (bytes.Length != _defaultMaskingKeyLength)
				{
					string message = "The masking key of a frame could not be read.";
					throw new WebSocketException(message);
				}
				frame._maskingKey = bytes;
				completed(frame);
			}, error);
		}

		private static WebSocketFrame readPayloadData(Stream stream, WebSocketFrame frame)
		{
			ulong exactPayloadLength = frame.ExactPayloadLength;
			if (exactPayloadLength > PayloadData.MaxLength)
			{
				string message = "The payload data of a frame is too big.";
				throw new WebSocketException(CloseStatusCode.TooBig, message);
			}
			if (exactPayloadLength == 0)
			{
				frame._payloadData = PayloadData.Empty;
				return frame;
			}
			long num = (long)exactPayloadLength;
			byte[] array = ((frame._payloadLength > 126) ? stream.ReadBytes(num, 1024) : stream.ReadBytes((int)num));
			if (array.LongLength != num)
			{
				string message2 = "The payload data of a frame could not be read.";
				throw new WebSocketException(message2);
			}
			frame._payloadData = new PayloadData(array, num);
			return frame;
		}

		private static void readPayloadDataAsync(Stream stream, WebSocketFrame frame, Action<WebSocketFrame> completed, Action<Exception> error)
		{
			ulong exactPayloadLength = frame.ExactPayloadLength;
			if (exactPayloadLength > PayloadData.MaxLength)
			{
				string message = "The payload data of a frame is too big.";
				throw new WebSocketException(CloseStatusCode.TooBig, message);
			}
			if (exactPayloadLength == 0)
			{
				frame._payloadData = PayloadData.Empty;
				completed(frame);
				return;
			}
			long len = (long)exactPayloadLength;
			Action<byte[]> completed2 = (byte[] bytes) =>
			{
				if (bytes.LongLength != len)
				{
					string message2 = "The payload data of a frame could not be read.";
					throw new WebSocketException(message2);
				}
				frame._payloadData = new PayloadData(bytes, len);
				completed(frame);
			};
			if (frame._payloadLength > 126)
			{
				stream.ReadBytesAsync(len, 1024, completed2, error);
			}
			else
			{
				stream.ReadBytesAsync((int)len, completed2, error);
			}
		}

		private string toDumpString()
		{
			ulong length = Length;
			long num = (long)(length / 4);
			int num2 = (int)(length % 4);
			string arg;
			string arg2;
			if (num < 10000)
			{
				arg = "{0,4}";
				arg2 = "{0,4}";
			}
			else if (num < 65536)
			{
				arg = "{0,4}";
				arg2 = "{0,4:X}";
			}
			else if (num < 4294967296L)
			{
				arg = "{0,8}";
				arg2 = "{0,8:X}";
			}
			else
			{
				arg = "{0,16}";
				arg2 = "{0,16:X}";
			}
			string format = "{0} 01234567 89ABCDEF 01234567 89ABCDEF\n{0}+--------+--------+--------+--------+\n";
			string format2 = string.Format(format, arg);
			format = "{0}|{{1,8}} {{2,8}} {{3,8}} {{4,8}}|\n";
			string lineFmt = string.Format(format, arg2);
			format = "{0}+--------+--------+--------+--------+";
			string format3 = string.Format(format, arg);
			StringBuilder buff = new StringBuilder(64);
			Func<Action<string, string, string, string>> func = () =>
			{
				long lineCnt = 0L;
				return (string text, string text2, string text3, string text4) =>
				{
					buff.AppendFormat(lineFmt, ++lineCnt, text, text2, text3, text4);
				};
			};
			Action<string, string, string, string> action = func();
			byte[] array = ToArray();
			buff.AppendFormat(format2, string.Empty);
			for (long num3 = 0L; num3 <= num; num3++)
			{
				long num4 = num3 * 4;
				if (num3 < num)
				{
					string arg3 = Convert.ToString(array[num4], 2).PadLeft(8, '0');
					string arg4 = Convert.ToString(array[num4 + 1], 2).PadLeft(8, '0');
					string arg5 = Convert.ToString(array[num4 + 2], 2).PadLeft(8, '0');
					string arg6 = Convert.ToString(array[num4 + 3], 2).PadLeft(8, '0');
					action(arg3, arg4, arg5, arg6);
				}
				else if (num2 > 0)
				{
					string arg7 = Convert.ToString(array[num4], 2).PadLeft(8, '0');
					string arg8 = ((num2 >= 2) ? Convert.ToString(array[num4 + 1], 2).PadLeft(8, '0') : string.Empty);
					string arg9 = ((num2 == 3) ? Convert.ToString(array[num4 + 2], 2).PadLeft(8, '0') : string.Empty);
					action(arg7, arg8, arg9, string.Empty);
				}
			}
			buff.AppendFormat(format3, string.Empty);
			return buff.ToString();
		}

		private string toString()
		{
			string text = ((_payloadLength >= 126) ? ExactPayloadLength.ToString() : string.Empty);
			string text2 = ((_mask == Mask.On) ? BitConverter.ToString(_maskingKey) : string.Empty);
			string text3 = ((_payloadLength >= 126) ? "***" : ((_payloadLength > 0) ? _payloadData.ToString() : string.Empty));
			string format = "                    FIN: {0}\n                   RSV1: {1}\n                   RSV2: {2}\n                   RSV3: {3}\n                 Opcode: {4}\n                   MASK: {5}\n         Payload Length: {6}\nExtended Payload Length: {7}\n            Masking Key: {8}\n           Payload Data: {9}";
			return string.Format(format, _fin, _rsv1, _rsv2, _rsv3, _opcode, _mask, _payloadLength, text, text2, text3);
		}

		internal static WebSocketFrame CreateCloseFrame(PayloadData payloadData, bool mask)
		{
			return new WebSocketFrame(Fin.Final, Opcode.Close, payloadData, compressed: false, mask);
		}

		internal static WebSocketFrame CreatePingFrame(bool mask)
		{
			return new WebSocketFrame(Fin.Final, Opcode.Ping, PayloadData.Empty, compressed: false, mask);
		}

		internal static WebSocketFrame CreatePingFrame(byte[] data, bool mask)
		{
			return new WebSocketFrame(Fin.Final, Opcode.Ping, new PayloadData(data), compressed: false, mask);
		}

		internal static WebSocketFrame CreatePongFrame(PayloadData payloadData, bool mask)
		{
			return new WebSocketFrame(Fin.Final, Opcode.Pong, payloadData, compressed: false, mask);
		}

		internal static WebSocketFrame ReadFrame(Stream stream, bool unmask)
		{
			WebSocketFrame webSocketFrame = readHeader(stream);
			readExtendedPayloadLength(stream, webSocketFrame);
			readMaskingKey(stream, webSocketFrame);
			readPayloadData(stream, webSocketFrame);
			if (unmask)
			{
				webSocketFrame.Unmask();
			}
			return webSocketFrame;
		}

		internal static void ReadFrameAsync(Stream stream, bool unmask, Action<WebSocketFrame> completed, Action<Exception> error)
		{
			readHeaderAsync(stream, (WebSocketFrame frame) =>
			{
				readExtendedPayloadLengthAsync(stream, frame, (WebSocketFrame frame2) =>
				{
					readMaskingKeyAsync(stream, frame2, (WebSocketFrame frame3) =>
					{
						readPayloadDataAsync(stream, frame3, (WebSocketFrame webSocketFrame) =>
						{
							if (unmask)
							{
								webSocketFrame.Unmask();
							}
							completed(webSocketFrame);
						}, error);
					}, error);
				}, error);
			}, error);
		}

		internal string ToString(bool dump)
		{
			return dump ? toDumpString() : toString();
		}

		internal void Unmask()
		{
			if (_mask != Mask.Off)
			{
				_payloadData.Mask(_maskingKey);
				_maskingKey = WebSocket.EmptyBytes;
				_mask = Mask.Off;
			}
		}

		public IEnumerator<byte> GetEnumerator()
		{
			byte[] array = ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				yield return array[i];
			}
		}

		public byte[] ToArray()
		{
			using MemoryStream memoryStream = new MemoryStream();
			int fin = (int)_fin;
			fin = (fin << 1) + (int)_rsv1;
			fin = (fin << 1) + (int)_rsv2;
			fin = (fin << 1) + (int)_rsv3;
			fin = (fin << 4) + (int)_opcode;
			fin = (fin << 1) + (int)_mask;
			fin = (fin << 7) + _payloadLength;
			ushort value = (ushort)fin;
			byte[] buffer = value.ToByteArray(ByteOrder.Big);
			memoryStream.Write(buffer, 0, _defaultHeaderLength);
			if (_payloadLength >= 126)
			{
				memoryStream.Write(_extPayloadLength, 0, _extPayloadLength.Length);
			}
			if (_mask == Mask.On)
			{
				memoryStream.Write(_maskingKey, 0, _defaultMaskingKeyLength);
			}
			if (_payloadLength > 0)
			{
				byte[] array = _payloadData.ToArray();
				if (_payloadLength > 126)
				{
					memoryStream.WriteBytes(array, 1024);
				}
				else
				{
					memoryStream.Write(array, 0, array.Length);
				}
			}
			memoryStream.Close();
			return memoryStream.ToArray();
		}

		public override string ToString()
		{
			byte[] array = ToArray();
			return BitConverter.ToString(array);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
