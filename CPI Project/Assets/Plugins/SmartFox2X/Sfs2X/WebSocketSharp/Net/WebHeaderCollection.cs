using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace Sfs2X.WebSocketSharp.Net
{
	[Serializable]
	[ComVisible(true)]
	public class WebHeaderCollection : NameValueCollection, ISerializable
	{
		private static readonly Dictionary<string, HttpHeaderInfo> _headers;

		private bool _internallyUsed;

		private HttpHeaderType _state;

		internal HttpHeaderType State => _state;

		public override string[] AllKeys => base.AllKeys;

		public override int Count => base.Count;

		public string this[HttpRequestHeader header]
		{
			get
			{
				string key = header.ToString();
				string headerName = getHeaderName(key);
				return Get(headerName);
			}
			set
			{
				Add(header, value);
			}
		}

		public string this[HttpResponseHeader header]
		{
			get
			{
				string key = header.ToString();
				string headerName = getHeaderName(key);
				return Get(headerName);
			}
			set
			{
				Add(header, value);
			}
		}

		public override KeysCollection Keys => base.Keys;

		static WebHeaderCollection()
		{
			_headers = new Dictionary<string, HttpHeaderInfo>(StringComparer.InvariantCultureIgnoreCase)
			{
				{
					"Accept",
					new HttpHeaderInfo("Accept", HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				},
				{
					"AcceptCharset",
					new HttpHeaderInfo("Accept-Charset", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"AcceptEncoding",
					new HttpHeaderInfo("Accept-Encoding", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"AcceptLanguage",
					new HttpHeaderInfo("Accept-Language", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"AcceptRanges",
					new HttpHeaderInfo("Accept-Ranges", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Age",
					new HttpHeaderInfo("Age", HttpHeaderType.Response)
				},
				{
					"Allow",
					new HttpHeaderInfo("Allow", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Authorization",
					new HttpHeaderInfo("Authorization", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"CacheControl",
					new HttpHeaderInfo("Cache-Control", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Connection",
					new HttpHeaderInfo("Connection", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				},
				{
					"ContentEncoding",
					new HttpHeaderInfo("Content-Encoding", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"ContentLanguage",
					new HttpHeaderInfo("Content-Language", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"ContentLength",
					new HttpHeaderInfo("Content-Length", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
				},
				{
					"ContentLocation",
					new HttpHeaderInfo("Content-Location", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"ContentMd5",
					new HttpHeaderInfo("Content-MD5", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"ContentRange",
					new HttpHeaderInfo("Content-Range", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"ContentType",
					new HttpHeaderInfo("Content-Type", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
				},
				{
					"Cookie",
					new HttpHeaderInfo("Cookie", HttpHeaderType.Request)
				},
				{
					"Cookie2",
					new HttpHeaderInfo("Cookie2", HttpHeaderType.Request)
				},
				{
					"Date",
					new HttpHeaderInfo("Date", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
				},
				{
					"Expect",
					new HttpHeaderInfo("Expect", HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				},
				{
					"Expires",
					new HttpHeaderInfo("Expires", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"ETag",
					new HttpHeaderInfo("ETag", HttpHeaderType.Response)
				},
				{
					"From",
					new HttpHeaderInfo("From", HttpHeaderType.Request)
				},
				{
					"Host",
					new HttpHeaderInfo("Host", HttpHeaderType.Request | HttpHeaderType.Restricted)
				},
				{
					"IfMatch",
					new HttpHeaderInfo("If-Match", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"IfModifiedSince",
					new HttpHeaderInfo("If-Modified-Since", HttpHeaderType.Request | HttpHeaderType.Restricted)
				},
				{
					"IfNoneMatch",
					new HttpHeaderInfo("If-None-Match", HttpHeaderType.Request | HttpHeaderType.MultiValue)
				},
				{
					"IfRange",
					new HttpHeaderInfo("If-Range", HttpHeaderType.Request)
				},
				{
					"IfUnmodifiedSince",
					new HttpHeaderInfo("If-Unmodified-Since", HttpHeaderType.Request)
				},
				{
					"KeepAlive",
					new HttpHeaderInfo("Keep-Alive", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"LastModified",
					new HttpHeaderInfo("Last-Modified", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"Location",
					new HttpHeaderInfo("Location", HttpHeaderType.Response)
				},
				{
					"MaxForwards",
					new HttpHeaderInfo("Max-Forwards", HttpHeaderType.Request)
				},
				{
					"Pragma",
					new HttpHeaderInfo("Pragma", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"ProxyAuthenticate",
					new HttpHeaderInfo("Proxy-Authenticate", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"ProxyAuthorization",
					new HttpHeaderInfo("Proxy-Authorization", HttpHeaderType.Request)
				},
				{
					"ProxyConnection",
					new HttpHeaderInfo("Proxy-Connection", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
				},
				{
					"Public",
					new HttpHeaderInfo("Public", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Range",
					new HttpHeaderInfo("Range", HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				},
				{
					"Referer",
					new HttpHeaderInfo("Referer", HttpHeaderType.Request | HttpHeaderType.Restricted)
				},
				{
					"RetryAfter",
					new HttpHeaderInfo("Retry-After", HttpHeaderType.Response)
				},
				{
					"SecWebSocketAccept",
					new HttpHeaderInfo("Sec-WebSocket-Accept", HttpHeaderType.Response | HttpHeaderType.Restricted)
				},
				{
					"SecWebSocketExtensions",
					new HttpHeaderInfo("Sec-WebSocket-Extensions", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValueInRequest)
				},
				{
					"SecWebSocketKey",
					new HttpHeaderInfo("Sec-WebSocket-Key", HttpHeaderType.Request | HttpHeaderType.Restricted)
				},
				{
					"SecWebSocketProtocol",
					new HttpHeaderInfo("Sec-WebSocket-Protocol", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValueInRequest)
				},
				{
					"SecWebSocketVersion",
					new HttpHeaderInfo("Sec-WebSocket-Version", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValueInResponse)
				},
				{
					"Server",
					new HttpHeaderInfo("Server", HttpHeaderType.Response)
				},
				{
					"SetCookie",
					new HttpHeaderInfo("Set-Cookie", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"SetCookie2",
					new HttpHeaderInfo("Set-Cookie2", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Te",
					new HttpHeaderInfo("TE", HttpHeaderType.Request)
				},
				{
					"Trailer",
					new HttpHeaderInfo("Trailer", HttpHeaderType.Request | HttpHeaderType.Response)
				},
				{
					"TransferEncoding",
					new HttpHeaderInfo("Transfer-Encoding", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				},
				{
					"Translate",
					new HttpHeaderInfo("Translate", HttpHeaderType.Request)
				},
				{
					"Upgrade",
					new HttpHeaderInfo("Upgrade", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"UserAgent",
					new HttpHeaderInfo("User-Agent", HttpHeaderType.Request | HttpHeaderType.Restricted)
				},
				{
					"Vary",
					new HttpHeaderInfo("Vary", HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Via",
					new HttpHeaderInfo("Via", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"Warning",
					new HttpHeaderInfo("Warning", HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
				},
				{
					"WwwAuthenticate",
					new HttpHeaderInfo("WWW-Authenticate", HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
				}
			};
		}

		internal WebHeaderCollection(HttpHeaderType state, bool internallyUsed)
		{
			_state = state;
			_internallyUsed = internallyUsed;
		}

		protected WebHeaderCollection(SerializationInfo serializationInfo, StreamingContext streamingContext)
		{
			if (serializationInfo == null)
			{
				throw new ArgumentNullException("serializationInfo");
			}
			try
			{
				_internallyUsed = serializationInfo.GetBoolean("InternallyUsed");
				_state = (HttpHeaderType)serializationInfo.GetInt32("State");
				int @int = serializationInfo.GetInt32("Count");
				for (int i = 0; i < @int; i++)
				{
					base.Add(serializationInfo.GetString(i.ToString()), serializationInfo.GetString((@int + i).ToString()));
				}
			}
			catch (SerializationException ex)
			{
				throw new ArgumentException(ex.Message, "serializationInfo", ex);
			}
		}

		public WebHeaderCollection()
		{
		}

		private void add(string name, string value, HttpHeaderType headerType)
		{
			base.Add(name, value);
			if (_state == HttpHeaderType.Unspecified && headerType != HttpHeaderType.Unspecified)
			{
				_state = headerType;
			}
		}

		private void checkAllowed(HttpHeaderType headerType)
		{
			if (_state == HttpHeaderType.Unspecified || headerType == HttpHeaderType.Unspecified || headerType == _state)
			{
				return;
			}
			string message = "This instance does not allow the header.";
			throw new InvalidOperationException(message);
		}

		private static string checkName(string name, string paramName)
		{
			if (name == null)
			{
				string message = "The name is null.";
				throw new ArgumentNullException(paramName, message);
			}
			if (name.Length == 0)
			{
				string message2 = "The name is an empty string.";
				throw new ArgumentException(message2, paramName);
			}
			name = name.Trim();
			if (name.Length == 0)
			{
				string message3 = "The name is a string of spaces.";
				throw new ArgumentException(message3, paramName);
			}
			if (!name.IsToken())
			{
				string message4 = "The name contains an invalid character.";
				throw new ArgumentException(message4, paramName);
			}
			return name;
		}

		private void checkRestricted(string name, HttpHeaderType headerType)
		{
			if (!_internallyUsed)
			{
				bool response = headerType == HttpHeaderType.Response;
				if (isRestricted(name, response))
				{
					string message = "The header is a restricted header.";
					throw new ArgumentException(message);
				}
			}
		}

		private static string checkValue(string value, string paramName)
		{
			if (value == null)
			{
				return string.Empty;
			}
			value = value.Trim();
			int length = value.Length;
			if (length == 0)
			{
				return value;
			}
			if (length > 65535)
			{
				string message = "The length of the value is greater than 65,535 characters.";
				throw new ArgumentOutOfRangeException(paramName, message);
			}
			if (!value.IsText())
			{
				string message2 = "The value contains an invalid character.";
				throw new ArgumentException(message2, paramName);
			}
			return value;
		}

		private static HttpHeaderInfo getHeaderInfo(string name)
		{
			StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase;
			foreach (HttpHeaderInfo value in _headers.Values)
			{
				if (value.HeaderName.Equals(name, comparisonType))
				{
					return value;
				}
			}
			return null;
		}

		private static string getHeaderName(string key)
		{
			HttpHeaderInfo value;
			return _headers.TryGetValue(key, out value) ? value.HeaderName : null;
		}

		private static HttpHeaderType getHeaderType(string name)
		{
			HttpHeaderInfo headerInfo = getHeaderInfo(name);
			if (headerInfo == null)
			{
				return HttpHeaderType.Unspecified;
			}
			if (headerInfo.IsRequest)
			{
				return (!headerInfo.IsResponse) ? HttpHeaderType.Request : HttpHeaderType.Unspecified;
			}
			return headerInfo.IsResponse ? HttpHeaderType.Response : HttpHeaderType.Unspecified;
		}

		private static bool isMultiValue(string name, bool response)
		{
			return getHeaderInfo(name)?.IsMultiValue(response) ?? false;
		}

		private static bool isRestricted(string name, bool response)
		{
			return getHeaderInfo(name)?.IsRestricted(response) ?? false;
		}

		private void set(string name, string value, HttpHeaderType headerType)
		{
			base.Set(name, value);
			if (_state == HttpHeaderType.Unspecified && headerType != HttpHeaderType.Unspecified)
			{
				_state = headerType;
			}
		}

		internal void InternalRemove(string name)
		{
			base.Remove(name);
		}

		internal void InternalSet(string header, bool response)
		{
			int num = header.IndexOf(':');
			if (num == -1)
			{
				string message = "It does not contain a colon character.";
				throw new ArgumentException(message, "header");
			}
			string name = header.Substring(0, num);
			string value = ((num < header.Length - 1) ? header.Substring(num + 1) : string.Empty);
			name = checkName(name, "header");
			value = checkValue(value, "header");
			if (isMultiValue(name, response))
			{
				base.Add(name, value);
			}
			else
			{
				base.Set(name, value);
			}
		}

		internal void InternalSet(string name, string value, bool response)
		{
			value = checkValue(value, "value");
			if (isMultiValue(name, response))
			{
				base.Add(name, value);
			}
			else
			{
				base.Set(name, value);
			}
		}

		internal string ToStringMultiValue(bool response)
		{
			int count = Count;
			if (count == 0)
			{
				return "\r\n";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < count; i++)
			{
				string key = GetKey(i);
				if (isMultiValue(key, response))
				{
					string[] values = GetValues(i);
					foreach (string arg in values)
					{
						stringBuilder.AppendFormat("{0}: {1}\r\n", key, arg);
					}
				}
				else
				{
					stringBuilder.AppendFormat("{0}: {1}\r\n", key, Get(i));
				}
			}
			stringBuilder.Append("\r\n");
			return stringBuilder.ToString();
		}

		protected void AddWithoutValidate(string headerName, string headerValue)
		{
			headerName = checkName(headerName, "headerName");
			headerValue = checkValue(headerValue, "headerValue");
			HttpHeaderType headerType = getHeaderType(headerName);
			checkAllowed(headerType);
			add(headerName, headerValue, headerType);
		}

		public void Add(string header)
		{
			if (header == null)
			{
				throw new ArgumentNullException("header");
			}
			int length = header.Length;
			if (length == 0)
			{
				string message = "An empty string.";
				throw new ArgumentException(message, "header");
			}
			int num = header.IndexOf(':');
			if (num == -1)
			{
				string message2 = "It does not contain a colon character.";
				throw new ArgumentException(message2, "header");
			}
			string name = header.Substring(0, num);
			string value = ((num < length - 1) ? header.Substring(num + 1) : string.Empty);
			name = checkName(name, "header");
			value = checkValue(value, "header");
			HttpHeaderType headerType = getHeaderType(name);
			checkRestricted(name, headerType);
			checkAllowed(headerType);
			add(name, value, headerType);
		}

		public void Add(HttpRequestHeader header, string value)
		{
			value = checkValue(value, "value");
			string key = header.ToString();
			string headerName = getHeaderName(key);
			checkRestricted(headerName, HttpHeaderType.Request);
			checkAllowed(HttpHeaderType.Request);
			add(headerName, value, HttpHeaderType.Request);
		}

		public void Add(HttpResponseHeader header, string value)
		{
			value = checkValue(value, "value");
			string key = header.ToString();
			string headerName = getHeaderName(key);
			checkRestricted(headerName, HttpHeaderType.Response);
			checkAllowed(HttpHeaderType.Response);
			add(headerName, value, HttpHeaderType.Response);
		}

		public override void Add(string name, string value)
		{
			name = checkName(name, "name");
			value = checkValue(value, "value");
			HttpHeaderType headerType = getHeaderType(name);
			checkRestricted(name, headerType);
			checkAllowed(headerType);
			add(name, value, headerType);
		}

		public override void Clear()
		{
			base.Clear();
			_state = HttpHeaderType.Unspecified;
		}

		public override string Get(int index)
		{
			return base.Get(index);
		}

		public override string Get(string name)
		{
			return base.Get(name);
		}

		public override IEnumerator GetEnumerator()
		{
			return base.GetEnumerator();
		}

		public override string GetKey(int index)
		{
			return base.GetKey(index);
		}

		public override string[] GetValues(int index)
		{
			string[] values = base.GetValues(index);
			return (values != null && values.Length != 0) ? values : null;
		}

		public override string[] GetValues(string name)
		{
			string[] values = base.GetValues(name);
			return (values != null && values.Length != 0) ? values : null;
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
		{
			if (serializationInfo == null)
			{
				throw new ArgumentNullException("serializationInfo");
			}
			serializationInfo.AddValue("InternallyUsed", _internallyUsed);
			serializationInfo.AddValue("State", (int)_state);
			int count = Count;
			serializationInfo.AddValue("Count", count);
			for (int i = 0; i < count; i++)
			{
				serializationInfo.AddValue(i.ToString(), GetKey(i));
				serializationInfo.AddValue((count + i).ToString(), Get(i));
			}
		}

		public static bool IsRestricted(string headerName)
		{
			return IsRestricted(headerName, response: false);
		}

		public static bool IsRestricted(string headerName, bool response)
		{
			headerName = checkName(headerName, "headerName");
			return isRestricted(headerName, response);
		}

		public override void OnDeserialization(object sender)
		{
		}

		public void Remove(HttpRequestHeader header)
		{
			string key = header.ToString();
			string headerName = getHeaderName(key);
			checkRestricted(headerName, HttpHeaderType.Request);
			checkAllowed(HttpHeaderType.Request);
			base.Remove(headerName);
		}

		public void Remove(HttpResponseHeader header)
		{
			string key = header.ToString();
			string headerName = getHeaderName(key);
			checkRestricted(headerName, HttpHeaderType.Response);
			checkAllowed(HttpHeaderType.Response);
			base.Remove(headerName);
		}

		public override void Remove(string name)
		{
			name = checkName(name, "name");
			HttpHeaderType headerType = getHeaderType(name);
			checkRestricted(name, headerType);
			checkAllowed(headerType);
			base.Remove(name);
		}

		public void Set(HttpRequestHeader header, string value)
		{
			value = checkValue(value, "value");
			string key = header.ToString();
			string headerName = getHeaderName(key);
			checkRestricted(headerName, HttpHeaderType.Request);
			checkAllowed(HttpHeaderType.Request);
			set(headerName, value, HttpHeaderType.Request);
		}

		public void Set(HttpResponseHeader header, string value)
		{
			value = checkValue(value, "value");
			string key = header.ToString();
			string headerName = getHeaderName(key);
			checkRestricted(headerName, HttpHeaderType.Response);
			checkAllowed(HttpHeaderType.Response);
			set(headerName, value, HttpHeaderType.Response);
		}

		public override void Set(string name, string value)
		{
			name = checkName(name, "name");
			value = checkValue(value, "value");
			HttpHeaderType headerType = getHeaderType(name);
			checkRestricted(name, headerType);
			checkAllowed(headerType);
			set(name, value, headerType);
		}

		public byte[] ToByteArray()
		{
			return Encoding.UTF8.GetBytes(ToString());
		}

		public override string ToString()
		{
			int count = Count;
			if (count == 0)
			{
				return "\r\n";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < count; i++)
			{
				stringBuilder.AppendFormat("{0}: {1}\r\n", GetKey(i), Get(i));
			}
			stringBuilder.Append("\r\n");
			return stringBuilder.ToString();
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter, SerializationFormatter = true)]
		void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
		{
			GetObjectData(serializationInfo, streamingContext);
		}
	}
}
