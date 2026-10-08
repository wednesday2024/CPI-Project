#if UNITY_WEBGL
#else
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Sfs2X.WebSocketSharp.Net;

namespace Sfs2X.WebSocketSharp
{
	internal abstract class HttpBase
	{
		private NameValueCollection _headers;

		private static readonly int _maxMessageHeaderLength;

		private string _messageBody;

		private byte[] _messageBodyData;

		private Version _version;

		protected static readonly string CrLf;

		protected static readonly string CrLfHt;

		protected static readonly string CrLfSp;

		internal byte[] MessageBodyData => _messageBodyData;

		protected string HeaderSection
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder(64);
				string[] allKeys = _headers.AllKeys;
				foreach (string text in allKeys)
				{
					stringBuilder.AppendFormat("{0}: {1}{2}", text, _headers[text], CrLf);
				}
				stringBuilder.Append(CrLf);
				return stringBuilder.ToString();
			}
		}

		public bool HasMessageBody => _messageBodyData != null;

		public NameValueCollection Headers => _headers;

		public string MessageBody
		{
			get
			{
				if (_messageBody == null)
				{
					_messageBody = getMessageBody();
				}
				return _messageBody;
			}
		}

		public abstract string MessageHeader { get; }

		public Version ProtocolVersion => _version;

		static HttpBase()
		{
			_maxMessageHeaderLength = 8192;
			CrLf = "\r\n";
			CrLfHt = "\r\n\t";
			CrLfSp = "\r\n ";
		}

		protected HttpBase(Version version, NameValueCollection headers)
		{
			_version = version;
			_headers = headers;
		}

		private string getMessageBody()
		{
			if (_messageBodyData == null || _messageBodyData.LongLength == 0)
			{
				return string.Empty;
			}
			string text = _headers["Content-Type"];
			Encoding encoding = ((text != null && text.Length > 0) ? HttpUtility.GetEncoding(text) : Encoding.UTF8);
			return encoding.GetString(_messageBodyData);
		}

		private static byte[] readMessageBodyFrom(Stream stream, string length)
		{
			if (!long.TryParse(length, out var result))
			{
				string message = "It cannot be parsed.";
				throw new ArgumentException(message, "length");
			}
			if (result < 0)
			{
				string message2 = "It is less than zero.";
				throw new ArgumentOutOfRangeException("length", message2);
			}
			return (result > 1024) ? stream.ReadBytes(result, 1024) : ((result > 0) ? stream.ReadBytes((int)result) : null);
		}

		private static string[] readMessageHeaderFrom(Stream stream)
		{
			List<byte> buff = new List<byte>();
			int cnt = 0;
			Action<int> beforeComparing = (int i) =>
			{
				if (i == -1)
				{
					string message2 = "The header could not be read from the data stream.";
					throw new EndOfStreamException(message2);
				}
				buff.Add((byte)i);
				cnt++;
			};
			bool flag = false;
			do
			{
				flag = stream.ReadByte().IsEqualTo('\r', beforeComparing) && stream.ReadByte().IsEqualTo('\n', beforeComparing) && stream.ReadByte().IsEqualTo('\r', beforeComparing) && stream.ReadByte().IsEqualTo('\n', beforeComparing);
				if (cnt > _maxMessageHeaderLength)
				{
					string message = "The length of the header is greater than the max length.";
					throw new InvalidOperationException(message);
				}
			}
			while (!flag);
			byte[] bytes = buff.ToArray();
			return Encoding.UTF8.GetString(bytes).Replace(CrLfSp, " ").Replace(CrLfHt, " ")
				.Split(new string[1] { CrLf }, StringSplitOptions.RemoveEmptyEntries);
		}

		internal void WriteTo(Stream stream)
		{
			byte[] array = ToByteArray();
			stream.Write(array, 0, array.Length);
		}

		protected static T Read<T>(Stream stream, Func<string[], T> parser, int millisecondsTimeout) where T : HttpBase
		{
			T val = null;
			bool timeout = false;
			Timer timer = new Timer((object state) =>
			{
				timeout = true;
				stream.Close();
			}, null, millisecondsTimeout, -1);
			Exception ex = null;
			try
			{
				string[] arg = readMessageHeaderFrom(stream);
				val = parser(arg);
				string text = val.Headers["Content-Length"];
				if (text != null && text.Length > 0)
				{
					val._messageBodyData = readMessageBodyFrom(stream, text);
				}
			}
			catch (Exception ex2)
			{
				ex = ex2;
			}
			finally
			{
				timer.Change(-1, -1);
				timer.Dispose();
			}
			if (timeout)
			{
				string message = "A timeout has occurred.";
				throw new WebSocketException(message);
			}
			if (ex != null)
			{
				string message2 = "An exception has occurred.";
				throw new WebSocketException(message2, ex);
			}
			return val;
		}

		public byte[] ToByteArray()
		{
			byte[] bytes = Encoding.UTF8.GetBytes(MessageHeader);
			return (_messageBodyData != null) ? bytes.Concat(_messageBodyData).ToArray() : bytes;
		}

		public override string ToString()
		{
			return (_messageBodyData != null) ? (MessageHeader + MessageBody) : MessageHeader;
		}
	}
}
#endif