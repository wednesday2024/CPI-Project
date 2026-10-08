#if UNITY_WEBGL
#else
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;

namespace Sfs2X.WebSocketSharp.Net.WebSockets
{
	internal class TcpListenerWebSocketContext : WebSocketContext
	{
		private Logger _log;

		private NameValueCollection _queryString;

		private HttpRequest _request;

		private Uri _requestUri;

		private bool _secure;

		private EndPoint _serverEndPoint;

		private Stream _stream;

		private TcpClient _tcpClient;

		private IPrincipal _user;

		private EndPoint _userEndPoint;

		private WebSocket _websocket;

		internal Logger Log => _log;

		internal Stream Stream => _stream;

		public override CookieCollection CookieCollection => _request.Cookies;

		public override NameValueCollection Headers => _request.Headers;

		public override string Host => _request.Headers["Host"];

		public override bool IsAuthenticated => _user != null;

		public override bool IsLocal => UserEndPoint.Address.IsLocal();

		public override bool IsSecureConnection => _secure;

		public override bool IsWebSocketRequest => _request.IsWebSocketRequest;

		public override string Origin => _request.Headers["Origin"];

		public override NameValueCollection QueryString
		{
			get
			{
				if (_queryString == null)
				{
					Uri requestUri = RequestUri;
					string query = ((requestUri != null) ? requestUri.Query : null);
					_queryString = QueryStringCollection.Parse(query, Encoding.UTF8);
				}
				return _queryString;
			}
		}

		public override Uri RequestUri
		{
			get
			{
				if (_requestUri == null)
				{
					_requestUri = HttpUtility.CreateRequestUrl(_request.RequestTarget, _request.Headers["Host"], _request.IsWebSocketRequest, _secure);
				}
				return _requestUri;
			}
		}

		public override string SecWebSocketKey => _request.Headers["Sec-WebSocket-Key"];

		public override IEnumerable<string> SecWebSocketProtocols
		{
			get
			{
				string val = _request.Headers["Sec-WebSocket-Protocol"];
				if (val == null || val.Length == 0)
				{
					yield break;
				}
				string[] array = val.Split(new char[1] { ',' });
				foreach (string elm in array)
				{
					string protocol = elm.Trim();
					if (protocol.Length != 0)
					{
						yield return protocol;
					}
				}
			}
		}

		public override string SecWebSocketVersion => _request.Headers["Sec-WebSocket-Version"];

		public override IPEndPoint ServerEndPoint => (IPEndPoint)_serverEndPoint;

		public override IPrincipal User => _user;

		public override IPEndPoint UserEndPoint => (IPEndPoint)_userEndPoint;

		public override WebSocket WebSocket => _websocket;

		internal TcpListenerWebSocketContext(TcpClient tcpClient, string protocol, bool secure, ServerSslConfiguration sslConfig, Logger log)
		{
			_tcpClient = tcpClient;
			_secure = secure;
			_log = log;
			NetworkStream stream = tcpClient.GetStream();
			if (secure)
			{
				SslStream sslStream = new SslStream(stream, leaveInnerStreamOpen: false, sslConfig.ClientCertificateValidationCallback);
				sslStream.AuthenticateAsServer(sslConfig.ServerCertificate, sslConfig.ClientCertificateRequired, sslConfig.EnabledSslProtocols, sslConfig.CheckCertificateRevocation);
				_stream = sslStream;
			}
			else
			{
				_stream = stream;
			}
			Socket client = tcpClient.Client;
			_serverEndPoint = client.LocalEndPoint;
			_userEndPoint = client.RemoteEndPoint;
			_request = HttpRequest.ReadRequest(_stream, 90000);
			_websocket = new WebSocket(this, protocol);
		}

		internal void Close()
		{
			_stream.Close();
			_tcpClient.Close();
		}

		internal void Close(HttpStatusCode code)
		{
			HttpResponse.CreateCloseResponse(code).WriteTo(_stream);
			_stream.Close();
			_tcpClient.Close();
		}

		internal void SendAuthenticationChallenge(string challenge)
		{
			HttpResponse.CreateUnauthorizedResponse(challenge).WriteTo(_stream);
			_request = HttpRequest.ReadRequest(_stream, 15000);
		}

		internal bool SetUser(AuthenticationSchemes scheme, string realm, Func<IIdentity, NetworkCredential> credentialsFinder)
		{
			IPrincipal principal = HttpUtility.CreateUser(_request.Headers["Authorization"], scheme, realm, _request.HttpMethod, credentialsFinder);
			if (principal == null)
			{
				return false;
			}
			if (!principal.Identity.IsAuthenticated)
			{
				return false;
			}
			_user = principal;
			return true;
		}

		public override string ToString()
		{
			return _request.ToString();
		}
	}
}
#endif