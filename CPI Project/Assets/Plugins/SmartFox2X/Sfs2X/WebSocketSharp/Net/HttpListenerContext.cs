using System;
using System.Security.Principal;
using System.Text;
using Sfs2X.WebSocketSharp.Net.WebSockets;

namespace Sfs2X.WebSocketSharp.Net
{
	public sealed class HttpListenerContext
	{
		private HttpConnection _connection;

		private string _errorMessage;

		private int _errorStatusCode;

		private HttpListener _listener;

		private HttpListenerRequest _request;

		private HttpListenerResponse _response;

		private IPrincipal _user;

		private HttpListenerWebSocketContext _websocketContext;

		internal HttpConnection Connection => _connection;

		internal string ErrorMessage
		{
			get
			{
				return _errorMessage;
			}
			set
			{
				_errorMessage = value;
			}
		}

		internal int ErrorStatusCode
		{
			get
			{
				return _errorStatusCode;
			}
			set
			{
				_errorStatusCode = value;
			}
		}

		internal bool HasErrorMessage => _errorMessage != null;

		internal HttpListener Listener
		{
			get
			{
				return _listener;
			}
			set
			{
				_listener = value;
			}
		}

		public HttpListenerRequest Request => _request;

		public HttpListenerResponse Response => _response;

		public IPrincipal User => _user;

		internal HttpListenerContext(HttpConnection connection)
		{
			_connection = connection;
			_errorStatusCode = 400;
			_request = new HttpListenerRequest(this);
			_response = new HttpListenerResponse(this);
		}

		private static string createErrorContent(int statusCode, string statusDescription, string message)
		{
			return (message != null && message.Length > 0) ? $"<html><body><h1>{statusCode} {statusDescription} ({message})</h1></body></html>" : $"<html><body><h1>{statusCode} {statusDescription}</h1></body></html>";
		}

		internal HttpListenerWebSocketContext GetWebSocketContext(string protocol)
		{
			_websocketContext = new HttpListenerWebSocketContext(this, protocol);
			return _websocketContext;
		}

		internal void SendAuthenticationChallenge(AuthenticationSchemes scheme, string realm)
		{
			_response.StatusCode = 401;
			string value = new AuthenticationChallenge(scheme, realm).ToString();
			_response.Headers.InternalSet("WWW-Authenticate", value, response: true);
			_response.Close();
		}

		internal void SendError()
		{
			try
			{
				_response.StatusCode = _errorStatusCode;
				_response.ContentType = "text/html";
				string s = createErrorContent(_errorStatusCode, _response.StatusDescription, _errorMessage);
				Encoding uTF = Encoding.UTF8;
				byte[] bytes = uTF.GetBytes(s);
				_response.ContentEncoding = uTF;
				_response.ContentLength64 = bytes.LongLength;
				_response.Close(bytes, willBlock: true);
			}
			catch
			{
				_connection.Close(force: true);
			}
		}

		internal void SendError(int statusCode)
		{
			_errorStatusCode = statusCode;
			SendError();
		}

		internal void SendError(int statusCode, string message)
		{
			_errorStatusCode = statusCode;
			_errorMessage = message;
			SendError();
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

		internal void Unregister()
		{
			if (_listener != null)
			{
				_listener.UnregisterContext(this);
			}
		}

		public HttpListenerWebSocketContext AcceptWebSocket(string protocol)
		{
			return AcceptWebSocket(protocol, null);
		}

		public HttpListenerWebSocketContext AcceptWebSocket(string protocol, Action<WebSocket> initializer)
		{
			if (_websocketContext != null)
			{
				string message = "The method has already been done.";
				throw new InvalidOperationException(message);
			}
			if (!_request.IsWebSocketRequest)
			{
				string message2 = "The request is not a WebSocket handshake request.";
				throw new InvalidOperationException(message2);
			}
			if (protocol != null)
			{
				if (protocol.Length == 0)
				{
					string message3 = "An empty string.";
					throw new ArgumentException(message3, "protocol");
				}
				if (!protocol.IsToken())
				{
					string message4 = "It contains an invalid character.";
					throw new ArgumentException(message4, "protocol");
				}
			}
			HttpListenerWebSocketContext webSocketContext = GetWebSocketContext(protocol);
			WebSocket webSocket = webSocketContext.WebSocket;
			if (initializer != null)
			{
				try
				{
					initializer(webSocket);
				}
				catch (Exception innerException)
				{
					if (webSocket.ReadyState == WebSocketState.New)
					{
						_websocketContext = null;
					}
					string message5 = "It caused an exception.";
					throw new ArgumentException(message5, "initializer", innerException);
				}
			}
			webSocket.Accept();
			return webSocketContext;
		}
	}
}
