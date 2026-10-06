using System;

namespace Sfs2X.WebSocketSharp
{
	public class MessageEventArgs : EventArgs
	{
		private string _data;

		private bool _dataSet;

		private Opcode _opcode;

		private byte[] _rawData;

		internal Opcode Opcode => _opcode;

		public string Data
		{
			get
			{
				setData();
				return _data;
			}
		}

		public bool IsBinary => _opcode == Opcode.Binary;

		public bool IsPing => _opcode == Opcode.Ping;

		public bool IsText => _opcode == Opcode.Text;

		public byte[] RawData
		{
			get
			{
				setData();
				return _rawData;
			}
		}

		internal MessageEventArgs(WebSocketFrame frame)
		{
			_opcode = frame.Opcode;
			_rawData = frame.PayloadData.ApplicationData;
		}

		internal MessageEventArgs(Opcode opcode, byte[] rawData)
		{
			if ((ulong)rawData.LongLength > PayloadData.MaxLength)
			{
				throw new WebSocketException(CloseStatusCode.TooBig);
			}
			_opcode = opcode;
			_rawData = rawData;
		}

		private void setData()
		{
			if (_dataSet)
			{
				return;
			}
			if (_opcode == Opcode.Binary)
			{
				_dataSet = true;
				return;
			}
			if (_rawData.TryGetUTF8DecodedString(out var s))
			{
				_data = s;
			}
			_dataSet = true;
		}
	}
}
