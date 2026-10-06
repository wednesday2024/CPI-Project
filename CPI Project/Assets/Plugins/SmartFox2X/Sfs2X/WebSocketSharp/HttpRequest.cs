using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using Sfs2X.WebSocketSharp.Net;

namespace Sfs2X.WebSocketSharp
{
	internal class HttpRequest : HttpBase
	{
		private CookieCollection _cookies;

		private string _method;

		private string _target;

		internal string RequestLine => $"{_method} {_target} HTTP/{base.ProtocolVersion}{HttpBase.CrLf}";

		public AuthenticationResponse AuthenticationResponse
		{
			get
			{
				string text = base.Headers["Authorization"];
				return (text != null && text.Length > 0) ? AuthenticationResponse.Parse(text) : null;
			}
		}

		public CookieCollection Cookies
		{
			get
			{
				if (_cookies == null)
				{
					_cookies = base.Headers.GetCookies(response: false);
				}
				return _cookies;
			}
		}

		public string HttpMethod => _method;

		public bool IsWebSocketRequest => _method == "GET" && base.ProtocolVersion > HttpVersion.Version10 && base.Headers.Upgrades("websocket");

		public override string MessageHeader => RequestLine + base.HeaderSection;

		public string RequestTarget => _target;

		private HttpRequest(string method, string target, Version version, NameValueCollection headers)
			: base(version, headers)
		{
			_method = method;
			_target = target;
		}

		internal HttpRequest(string method, string target)
			: this(method, target, HttpVersion.Version11, new NameValueCollection())
		{
			base.Headers["User-Agent"] = "websocket-sharp/1.0";
		}

		internal static HttpRequest CreateConnectRequest(Uri targetUri)
		{
			string dnsSafeHost = targetUri.DnsSafeHost;
			int port = targetUri.Port;
			string text = $"{dnsSafeHost}:{port}";
			HttpRequest httpRequest = new HttpRequest("CONNECT", text);
			httpRequest.Headers["Host"] = ((port != 80) ? text : dnsSafeHost);
			return httpRequest;
		}

		internal static HttpRequest CreateWebSocketHandshakeRequest(Uri targetUri)
		{
			HttpRequest httpRequest = new HttpRequest("GET", targetUri.PathAndQuery);
			NameValueCollection headers = httpRequest.Headers;
			int port = targetUri.Port;
			string scheme = targetUri.Scheme;
			bool flag = (port == 80 && scheme == "ws") || (port == 443 && scheme == "wss");
			headers["Host"] = ((!flag) ? targetUri.Authority : targetUri.DnsSafeHost);
			headers["Upgrade"] = "websocket";
			headers["Connection"] = "Upgrade";
			return httpRequest;
		}

		internal HttpResponse GetResponse(Stream stream, int millisecondsTimeout)
		{
			WriteTo(stream);
			return HttpResponse.ReadResponse(stream, millisecondsTimeout);
		}

		internal static HttpRequest Parse(string[] messageHeader)
		{
			int num = messageHeader.Length;
			if (num == 0)
			{
				string message = "An empty request header.";
				throw new ArgumentException(message);
			}
			string[] array = messageHeader[0].Split(new char[1] { ' ' }, 3);
			if (array.Length != 3)
			{
				string message2 = "It includes an invalid request line.";
				throw new ArgumentException(message2);
			}
			string method = array[0];
			string target = array[1];
			Version version = array[2].Substring(5).ToVersion();
			WebHeaderCollection webHeaderCollection = new WebHeaderCollection();
			for (int i = 1; i < num; i++)
			{
				webHeaderCollection.InternalSet(messageHeader[i], response: false);
			}
			return new HttpRequest(method, target, version, webHeaderCollection);
		}

		internal static HttpRequest ReadRequest(Stream stream, int millisecondsTimeout)
		{
			return HttpBase.Read(stream, Parse, millisecondsTimeout);
		}

		public void SetCookies(CookieCollection cookies)
		{
			if (cookies == null || cookies.Count == 0)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(64);
			foreach (Cookie item in cookies.Sorted)
			{
				if (!item.Expired)
				{
					stringBuilder.AppendFormat("{0}; ", item);
				}
			}
			int length = stringBuilder.Length;
			if (length > 2)
			{
				stringBuilder.Length = length - 2;
				base.Headers["Cookie"] = stringBuilder.ToString();
			}
		}
	}
}
