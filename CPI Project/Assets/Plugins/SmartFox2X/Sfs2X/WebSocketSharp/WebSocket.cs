#if UNITY_WEBGL
#else
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Sfs2X.WebSocketSharp.Net;
using Sfs2X.WebSocketSharp.Net.WebSockets;

namespace Sfs2X.WebSocketSharp
{
	public class WebSocket : IDisposable
	{
		private AuthenticationChallenge _authChallenge;

		private string _base64Key;

		private bool _client;

		private Action _closeContext;

		private CompressionMethod _compression;

		private WebSocketContext _context;

		private CookieCollection _cookies;

		private NetworkCredential _credentials;

		private bool _emitOnPing;

		private bool _enableRedirection;

		private string _extensions;

		private bool _extensionsRequested;

		private object _forMessageEventQueue;

		private object _forPing;

		private object _forSend;

		private object _forState;

		private MemoryStream _fragmentsBuffer;

		private bool _fragmentsCompressed;

		private Opcode _fragmentsOpcode;

		private const string _guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

		private Func<WebSocketContext, string> _handshakeRequestChecker;

		private bool _ignoreExtensions;

		private bool _inContinuation;

		private volatile bool _inMessage;

		private volatile Logger _log;

		private static readonly int _maxRetryCountForConnect;

		private Action<MessageEventArgs> _message;

		private Queue<MessageEventArgs> _messageEventQueue;

		private uint _nonceCount;

		private string _origin;

		private ManualResetEvent _pongReceived;

		private bool _preAuth;

		private string _protocol;

		private string[] _protocols;

		private bool _protocolsRequested;

		private NetworkCredential _proxyCredentials;

		private Uri _proxyUri;

		private volatile WebSocketState _readyState;

		private ManualResetEvent _receivingExited;

		private int _retryCountForConnect;

		private bool _secure;

		private ClientSslConfiguration _sslConfig;

		private Stream _stream;

		private TcpClient _tcpClient;

		private Uri _uri;

		private const string _version = "13";

		private TimeSpan _waitTime;

		internal static readonly byte[] EmptyBytes;

		internal static readonly int FragmentLength;

		internal static readonly RandomNumberGenerator RandomNumber;

		internal CookieCollection CookieCollection => _cookies;

		internal Func<WebSocketContext, string> CustomHandshakeRequestChecker
		{
			get
			{
				return _handshakeRequestChecker;
			}
			set
			{
				_handshakeRequestChecker = value;
			}
		}

		internal bool IgnoreExtensions
		{
			get
			{
				return _ignoreExtensions;
			}
			set
			{
				_ignoreExtensions = value;
			}
		}

		public CompressionMethod Compression
		{
			get
			{
				return _compression;
			}
			set
			{
				if (!_client)
				{
					string text = "The interface is not for the client.";
					throw new InvalidOperationException(text);
				}
				lock (_forState)
				{
					if (canSet())
					{
						_compression = value;
					}
				}
			}
		}

		public IEnumerable<Cookie> Cookies
		{
			get
			{
				lock (_cookies.SyncRoot)
				{
					foreach (Cookie cookie in _cookies)
					{
						yield return cookie;
					}
				}
			}
		}

		public NetworkCredential Credentials => _credentials;

		public bool EmitOnPing
		{
			get
			{
				return _emitOnPing;
			}
			set
			{
				_emitOnPing = value;
			}
		}

		public bool EnableRedirection
		{
			get
			{
				return _enableRedirection;
			}
			set
			{
				if (!_client)
				{
					string text = "The interface is not for the client.";
					throw new InvalidOperationException(text);
				}
				lock (_forState)
				{
					if (canSet())
					{
						_enableRedirection = value;
					}
				}
			}
		}

		public string Extensions => _extensions ?? string.Empty;

		public bool IsAlive => ping(EmptyBytes);

		public bool IsSecure => _secure;

		public Logger Log
		{
			get
			{
				return _log;
			}
			internal set
			{
				_log = value;
			}
		}

		public string Origin
		{
			get
			{
				return _origin;
			}
			set
			{
				if (!_client)
				{
					string text = "The interface is not for the client.";
					throw new InvalidOperationException(text);
				}
				if (!value.IsNullOrEmpty())
				{
					if (!Uri.TryCreate(value, UriKind.Absolute, out var result))
					{
						string text2 = "Not an absolute URI string.";
						throw new ArgumentException(text2, "value");
					}
					if (result.Segments.Length > 1)
					{
						string text3 = "It includes the path segments.";
						throw new ArgumentException(text3, "value");
					}
				}
				lock (_forState)
				{
					if (canSet())
					{
						_origin = ((!value.IsNullOrEmpty()) ? value.TrimEnd(new char[1] { '/' }) : value);
					}
				}
			}
		}

		public string Protocol
		{
			get
			{
				return _protocol ?? string.Empty;
			}
			internal set
			{
				_protocol = value;
			}
		}

		public WebSocketState ReadyState => _readyState;

		public ClientSslConfiguration SslConfiguration
		{
			get
			{
				if (!_client)
				{
					string text = "The interface is not for the client.";
					throw new InvalidOperationException(text);
				}
				if (!_secure)
				{
					string text2 = "The interface does not use a secure connection.";
					throw new InvalidOperationException(text2);
				}
				return getSslConfiguration();
			}
		}

		public Uri Url => _client ? _uri : _context.RequestUri;

		public TimeSpan WaitTime
		{
			get
			{
				return _waitTime;
			}
			set
			{
				if (value <= TimeSpan.Zero)
				{
					string text = "Zero or less.";
					throw new ArgumentOutOfRangeException("value", text);
				}
				lock (_forState)
				{
					if (canSet())
					{
						_waitTime = value;
					}
				}
			}
		}

		public event EventHandler<CloseEventArgs> OnClose;

		public event EventHandler<ErrorEventArgs> OnError;

		public event EventHandler<MessageEventArgs> OnMessage;

		public event EventHandler OnOpen;

		static WebSocket()
		{
			_maxRetryCountForConnect = 10;
			EmptyBytes = new byte[0];
			FragmentLength = 1016;
			RandomNumber = new RNGCryptoServiceProvider();
		}

		internal WebSocket(HttpListenerWebSocketContext context, string protocol)
		{
			_context = context;
			_protocol = protocol;
			_closeContext = context.Close;
			_log = context.Log;
			_message = messages;
			_secure = context.IsSecureConnection;
			_stream = context.Stream;
			_waitTime = TimeSpan.FromSeconds(1.0);
			init();
		}

		internal WebSocket(TcpListenerWebSocketContext context, string protocol)
		{
			_context = context;
			_protocol = protocol;
			_closeContext = context.Close;
			_log = context.Log;
			_message = messages;
			_secure = context.IsSecureConnection;
			_stream = context.Stream;
			_waitTime = TimeSpan.FromSeconds(1.0);
			init();
		}

		public WebSocket(string url, params string[] protocols)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}
			if (url.Length == 0)
			{
				throw new ArgumentException("An empty string.", "url");
			}
			if (!url.TryCreateWebSocketUri(out _uri, out var text))
			{
				throw new ArgumentException(text, "url");
			}
			if (protocols != null && protocols.Length != 0)
			{
				if (!checkProtocols(protocols, out text))
				{
					throw new ArgumentException(text, "protocols");
				}
				_protocols = protocols;
			}
			_base64Key = CreateBase64Key();
			_client = true;
			_log = new Logger();
			_message = messagec;
			_retryCountForConnect = -1;
			_secure = _uri.Scheme == "wss";
			_waitTime = TimeSpan.FromSeconds(5.0);
			init();
		}

		private void abort(string reason, Exception exception)
		{
			ushort code = (ushort)((exception is WebSocketException) ? ((WebSocketException)exception).Code : 1006);
			abort(code, reason);
		}

		private void abort(ushort code, string reason)
		{
			PayloadData payloadData = new PayloadData(code, reason);
			close(payloadData, send: false, received: false);
		}

		private bool accept()
		{
			lock (_forState)
			{
				if (_readyState == WebSocketState.Open)
				{
					_log.Trace("The connection has already been established.");
					return false;
				}
				if (_readyState == WebSocketState.Closing)
				{
					_log.Error("The close process is in progress.");
					error("An error has occurred before accepting.", null);
					return false;
				}
				if (_readyState == WebSocketState.Closed)
				{
					_log.Error("The connection has been closed.");
					error("An error has occurred before accepting.", null);
					return false;
				}
				_readyState = WebSocketState.Connecting;
				bool flag = false;
				try
				{
					flag = acceptHandshake();
				}
				catch (Exception ex)
				{
					_log.Fatal(ex.Message);
					_log.Debug(ex.ToString());
					abort(1011, "An exception has occurred while accepting.");
				}
				if (!flag)
				{
					return false;
				}
				_readyState = WebSocketState.Open;
				return true;
			}
		}

		private bool acceptHandshake()
		{
			if (!checkHandshakeRequest(_context, out var text))
			{
				_log.Error(text);
				_log.Debug(_context.ToString());
				refuseHandshake(1002, "A handshake error has occurred.");
				return false;
			}
			if (!customCheckHandshakeRequest(_context, out text))
			{
				_log.Error(text);
				_log.Debug(_context.ToString());
				refuseHandshake(1002, "A handshake error has occurred.");
				return false;
			}
			_base64Key = _context.Headers["Sec-WebSocket-Key"];
			if (_protocol != null && !_context.SecWebSocketProtocols.Contains((string p) => p == _protocol))
			{
				_protocol = null;
			}
			if (!_ignoreExtensions)
			{
				string value = _context.Headers["Sec-WebSocket-Extensions"];
				processSecWebSocketExtensionsClientHeader(value);
			}
			createHandshakeResponse().WriteTo(_stream);
			return true;
		}

		private bool canSet()
		{
			return _readyState == WebSocketState.New || _readyState == WebSocketState.Closed;
		}

		private bool checkHandshakeRequest(WebSocketContext context, out string message)
		{
			message = null;
			if (!context.IsWebSocketRequest)
			{
				message = "Not a WebSocket handshake request.";
				return false;
			}
			NameValueCollection headers = context.Headers;
			string text = headers["Sec-WebSocket-Key"];
			if (text == null)
			{
				message = "The Sec-WebSocket-Key header is non-existent.";
				return false;
			}
			if (text.Length == 0)
			{
				message = "The Sec-WebSocket-Key header is invalid.";
				return false;
			}
			string text2 = headers["Sec-WebSocket-Version"];
			if (text2 == null)
			{
				message = "The Sec-WebSocket-Version header is non-existent.";
				return false;
			}
			if (text2 != "13")
			{
				message = "The Sec-WebSocket-Version header is invalid.";
				return false;
			}
			string text3 = headers["Sec-WebSocket-Protocol"];
			if (text3 != null && text3.Length == 0)
			{
				message = "The Sec-WebSocket-Protocol header is invalid.";
				return false;
			}
			if (!_ignoreExtensions)
			{
				string text4 = headers["Sec-WebSocket-Extensions"];
				if (text4 != null && text4.Length == 0)
				{
					message = "The Sec-WebSocket-Extensions header is invalid.";
					return false;
				}
			}
			return true;
		}

		private bool checkHandshakeResponse(HttpResponse response, out string message)
		{
			message = null;
			if (response.IsRedirect)
			{
				message = "The redirection is indicated.";
				return false;
			}
			if (response.IsUnauthorized)
			{
				message = "The authentication is required.";
				return false;
			}
			if (!response.IsWebSocketResponse)
			{
				message = "Not a WebSocket handshake response.";
				return false;
			}
			NameValueCollection headers = response.Headers;
			string text = headers["Sec-WebSocket-Accept"];
			if (text == null)
			{
				message = "The Sec-WebSocket-Accept header is non-existent.";
				return false;
			}
			if (text != CreateResponseKey(_base64Key))
			{
				message = "The Sec-WebSocket-Accept header is invalid.";
				return false;
			}
			string text2 = headers["Sec-WebSocket-Version"];
			if (text2 != null && text2 != "13")
			{
				message = "The Sec-WebSocket-Version header is invalid.";
				return false;
			}
			string subp = headers["Sec-WebSocket-Protocol"];
			if (subp == null)
			{
				if (_protocolsRequested)
				{
					message = "The Sec-WebSocket-Protocol header is non-existent.";
					return false;
				}
			}
			else if (subp.Length <= 0 || !_protocolsRequested || !_protocols.Contains((string p) => p == subp))
			{
				message = "The Sec-WebSocket-Protocol header is invalid.";
				return false;
			}
			string value = headers["Sec-WebSocket-Extensions"];
			if (!validateSecWebSocketExtensionsServerHeader(value))
			{
				message = "The Sec-WebSocket-Extensions header is invalid.";
				return false;
			}
			return true;
		}

		private static bool checkProtocols(string[] protocols, out string message)
		{
			message = null;
			Func<string, bool> condition = (string protocol) => protocol.IsNullOrEmpty() || !protocol.IsToken();
			if (protocols.Contains(condition))
			{
				message = "It contains a value that is not a token.";
				return false;
			}
			if (protocols.ContainsTwice())
			{
				message = "It contains a value twice.";
				return false;
			}
			return true;
		}

		private bool checkReceivedFrame(WebSocketFrame frame, out string message)
		{
			message = null;
			if (frame.IsMasked)
			{
				if (_client)
				{
					message = "A frame from the server is masked.";
					return false;
				}
			}
			else if (!_client)
			{
				message = "A frame from a client is not masked.";
				return false;
			}
			if (frame.IsCompressed)
			{
				if (_compression == CompressionMethod.None)
				{
					message = "A frame is compressed without any agreement for it.";
					return false;
				}
				if (!frame.IsData)
				{
					message = "A non data frame is compressed.";
					return false;
				}
			}
			if (frame.IsData && _inContinuation)
			{
				message = "A data frame was received while receiving continuation frames.";
				return false;
			}
			if (frame.IsControl)
			{
				if (frame.Fin == Fin.More)
				{
					message = "A control frame is fragmented.";
					return false;
				}
				if (frame.PayloadLength > 125)
				{
					message = "The payload length of a control frame is greater than 125.";
					return false;
				}
			}
			if (frame.Rsv2 == Rsv.On)
			{
				message = "The RSV2 of a frame is non-zero without any negotiation for it.";
				return false;
			}
			if (frame.Rsv3 == Rsv.On)
			{
				message = "The RSV3 of a frame is non-zero without any negotiation for it.";
				return false;
			}
			return true;
		}

		private void close(ushort code, string reason)
		{
			if (_readyState == WebSocketState.Closing)
			{
				_log.Trace("The close process is already in progress.");
				return;
			}
			if (_readyState == WebSocketState.Closed)
			{
				_log.Trace("The connection has already been closed.");
				return;
			}
			if (code == 1005)
			{
				close(PayloadData.Empty, send: true, received: false);
				return;
			}
			PayloadData payloadData = new PayloadData(code, reason);
			bool flag = !code.IsReservedStatusCode();
			close(payloadData, flag, received: false);
		}

		private void close(PayloadData payloadData, bool send, bool received)
		{
			lock (_forState)
			{
				if (_readyState == WebSocketState.Closing)
				{
					_log.Trace("The close process is already in progress.");
					return;
				}
				if (_readyState == WebSocketState.Closed)
				{
					_log.Trace("The connection has already been closed.");
					return;
				}
				send = send && _readyState == WebSocketState.Open;
				_readyState = WebSocketState.Closing;
			}
			_log.Trace("Begin closing the connection.");
			bool clean = closeHandshake(payloadData, send, received);
			releaseResources();
			_log.Trace("End closing the connection.");
			_readyState = WebSocketState.Closed;
			CloseEventArgs e = new CloseEventArgs(payloadData, clean);
			try
			{
				OnClose.Emit(this, e);
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
				_log.Debug(ex.ToString());
			}
		}

		private void closeAsync(ushort code, string reason)
		{
			if (_readyState == WebSocketState.Closing)
			{
				_log.Trace("The close process is already in progress.");
				return;
			}
			if (_readyState == WebSocketState.Closed)
			{
				_log.Trace("The connection has already been closed.");
				return;
			}
			if (code == 1005)
			{
				closeAsync(PayloadData.Empty, send: true, received: false);
				return;
			}
			PayloadData payloadData = new PayloadData(code, reason);
			bool flag = !code.IsReservedStatusCode();
			closeAsync(payloadData, flag, received: false);
		}

		private void closeAsync(PayloadData payloadData, bool send, bool received)
		{
			Action<PayloadData, bool, bool> closer = close;
			closer.BeginInvoke(payloadData, send, received, (IAsyncResult ar) =>
			{
				closer.EndInvoke(ar);
			}, null);
		}

		private bool closeHandshake(PayloadData payloadData, bool send, bool received)
		{
			bool flag = false;
			if (send)
			{
				WebSocketFrame webSocketFrame = WebSocketFrame.CreateCloseFrame(payloadData, _client);
				byte[] bytes = webSocketFrame.ToArray();
				flag = sendBytes(bytes);
				if (_client)
				{
					webSocketFrame.Unmask();
				}
			}
			if ((!received & flag) && _receivingExited != null)
			{
				received = _receivingExited.WaitOne(_waitTime);
			}
			bool flag2 = flag & received;
			string text = $"The closing was clean? {flag2} (sent: {flag} received: {received})";
			_log.Debug(text);
			return flag2;
		}

		private bool connect()
		{
			if (_readyState == WebSocketState.Connecting)
			{
				_log.Trace("The connect process is in progress.");
				return false;
			}
			lock (_forState)
			{
				if (_readyState == WebSocketState.Open)
				{
					_log.Trace("The connection has already been established.");
					return false;
				}
				if (_readyState == WebSocketState.Closing)
				{
					_log.Error("The close process is in progress.");
					error("An error has occurred before connecting.", null);
					return false;
				}
				if (_retryCountForConnect >= _maxRetryCountForConnect)
				{
					_log.Error("An opportunity for reconnecting has been lost.");
					error("An error has occurred before connecting.", null);
					return false;
				}
				_retryCountForConnect++;
				_readyState = WebSocketState.Connecting;
				bool flag = false;
				try
				{
					flag = doHandshake();
				}
				catch (Exception ex)
				{
					_log.Fatal(ex.Message);
					_log.Debug(ex.ToString());
					abort("An exception has occurred while connecting.", ex);
				}
				if (!flag)
				{
					return false;
				}
				_retryCountForConnect = -1;
				_readyState = WebSocketState.Open;
				return true;
			}
		}

		private AuthenticationResponse createAuthenticationResponse()
		{
			if (_credentials == null)
			{
				return null;
			}
			if (_authChallenge != null)
			{
				AuthenticationResponse authenticationResponse = new AuthenticationResponse(_authChallenge, _credentials, _nonceCount);
				_nonceCount = authenticationResponse.NonceCount;
				return authenticationResponse;
			}
			return _preAuth ? new AuthenticationResponse(_credentials) : null;
		}

		private string createExtensions()
		{
			StringBuilder stringBuilder = new StringBuilder(80);
			if (_compression != CompressionMethod.None)
			{
				string arg = _compression.ToExtensionString("server_no_context_takeover", "client_no_context_takeover");
				stringBuilder.AppendFormat("{0}, ", arg);
			}
			int length = stringBuilder.Length;
			if (length <= 2)
			{
				return null;
			}
			stringBuilder.Length = length - 2;
			return stringBuilder.ToString();
		}

		private HttpResponse createHandshakeFailureResponse()
		{
			HttpResponse httpResponse = HttpResponse.CreateCloseResponse(HttpStatusCode.BadRequest);
			httpResponse.Headers["Sec-WebSocket-Version"] = "13";
			return httpResponse;
		}

		private HttpRequest createHandshakeRequest()
		{
			HttpRequest httpRequest = HttpRequest.CreateWebSocketHandshakeRequest(_uri);
			NameValueCollection headers = httpRequest.Headers;
			headers["Sec-WebSocket-Key"] = _base64Key;
			headers["Sec-WebSocket-Version"] = "13";
			if (!_origin.IsNullOrEmpty())
			{
				headers["Origin"] = _origin;
			}
			if (_protocols != null)
			{
				headers["Sec-WebSocket-Protocol"] = _protocols.ToString(", ");
				_protocolsRequested = true;
			}
			string text = createExtensions();
			if (text != null)
			{
				headers["Sec-WebSocket-Extensions"] = text;
				_extensionsRequested = true;
			}
			AuthenticationResponse authenticationResponse = createAuthenticationResponse();
			if (authenticationResponse != null)
			{
				headers["Authorization"] = authenticationResponse.ToString();
			}
			if (_cookies.Count > 0)
			{
				httpRequest.SetCookies(_cookies);
			}
			return httpRequest;
		}

		private HttpResponse createHandshakeResponse()
		{
			HttpResponse httpResponse = HttpResponse.CreateWebSocketHandshakeResponse();
			NameValueCollection headers = httpResponse.Headers;
			headers["Sec-WebSocket-Accept"] = CreateResponseKey(_base64Key);
			if (_protocol != null)
			{
				headers["Sec-WebSocket-Protocol"] = _protocol;
			}
			if (_extensions != null)
			{
				headers["Sec-WebSocket-Extensions"] = _extensions;
			}
			if (_cookies.Count > 0)
			{
				httpResponse.SetCookies(_cookies);
			}
			return httpResponse;
		}

		private bool customCheckHandshakeRequest(WebSocketContext context, out string message)
		{
			message = null;
			if (_handshakeRequestChecker == null)
			{
				return true;
			}
			message = _handshakeRequestChecker(context);
			return message == null;
		}

		private MessageEventArgs dequeueFromMessageEventQueue()
		{
			lock (_forMessageEventQueue)
			{
				return (_messageEventQueue.Count > 0) ? _messageEventQueue.Dequeue() : null;
			}
		}

		private bool doHandshake()
		{
			setClientStream();
			HttpResponse httpResponse = sendHandshakeRequest();
			if (!checkHandshakeResponse(httpResponse, out var text))
			{
				_log.Error(text);
				_log.Debug(httpResponse.ToString());
				abort(1002, "A handshake error has occurred.");
				return false;
			}
			if (_protocolsRequested)
			{
				_protocol = httpResponse.Headers["Sec-WebSocket-Protocol"];
			}
			if (_extensionsRequested)
			{
				string value = httpResponse.Headers["Sec-WebSocket-Extensions"];
				processSecWebSocketExtensionsServerHeader(value);
			}
			processCookies(httpResponse.Cookies);
			return true;
		}

		private void enqueueToMessageEventQueue(MessageEventArgs e)
		{
			lock (_forMessageEventQueue)
			{
				_messageEventQueue.Enqueue(e);
			}
		}

		private void error(string message, Exception exception)
		{
			ErrorEventArgs e = new ErrorEventArgs(message, exception);
			try
			{
				OnError.Emit(this, e);
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
				_log.Debug(ex.ToString());
			}
		}

		private ClientSslConfiguration getSslConfiguration()
		{
			if (_sslConfig == null)
			{
				_sslConfig = new ClientSslConfiguration(_uri.DnsSafeHost);
			}
			return _sslConfig;
		}

		private void init()
		{
			_compression = CompressionMethod.None;
			_cookies = new CookieCollection();
			_forPing = new object();
			_forSend = new object();
			_forState = new object();
			_messageEventQueue = new Queue<MessageEventArgs>();
			_forMessageEventQueue = ((ICollection)_messageEventQueue).SyncRoot;
			_readyState = WebSocketState.New;
		}

		private void message()
		{
			MessageEventArgs obj = null;
			lock (_forMessageEventQueue)
			{
				if (_inMessage || _messageEventQueue.Count == 0 || _readyState != WebSocketState.Open)
				{
					return;
				}
				obj = _messageEventQueue.Dequeue();
				_inMessage = true;
			}
			_message(obj);
		}

		private void messagec(MessageEventArgs e)
		{
			while (true)
			{
				try
				{
					OnMessage.Emit(this, e);
				}
				catch (Exception ex)
				{
					_log.Error(ex.Message);
					_log.Debug(ex.ToString());
					error("An exception has occurred during an OnMessage event.", ex);
				}
				lock (_forMessageEventQueue)
				{
					if (_messageEventQueue.Count == 0)
					{
						_inMessage = false;
						break;
					}
					if (_readyState != WebSocketState.Open)
					{
						_inMessage = false;
						break;
					}
					e = _messageEventQueue.Dequeue();
				}
				bool flag = true;
			}
		}

		private void messages(MessageEventArgs e)
		{
			try
			{
				OnMessage.Emit(this, e);
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
				_log.Debug(ex.ToString());
				error("An exception has occurred during an OnMessage event.", ex);
			}
			lock (_forMessageEventQueue)
			{
				if (_messageEventQueue.Count == 0)
				{
					_inMessage = false;
					return;
				}
				if (_readyState != WebSocketState.Open)
				{
					_inMessage = false;
					return;
				}
				e = _messageEventQueue.Dequeue();
			}
			ThreadPool.QueueUserWorkItem((object state) =>
			{
				messages(e);
			});
		}

		private void open()
		{
			_inMessage = true;
			startReceiving();
			try
			{
				OnOpen.Emit(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
				_log.Debug(ex.ToString());
				error("An exception has occurred during the OnOpen event.", ex);
			}
			MessageEventArgs obj = null;
			lock (_forMessageEventQueue)
			{
				if (_messageEventQueue.Count == 0)
				{
					_inMessage = false;
					return;
				}
				if (_readyState != WebSocketState.Open)
				{
					_inMessage = false;
					return;
				}
				obj = _messageEventQueue.Dequeue();
			}
			_message.BeginInvoke(obj, (IAsyncResult ar) =>
			{
				_message.EndInvoke(ar);
			}, null);
		}

		private bool ping(byte[] data)
		{
			if (_readyState != WebSocketState.Open)
			{
				return false;
			}
			ManualResetEvent pongReceived = _pongReceived;
			if (pongReceived == null)
			{
				return false;
			}
			lock (_forPing)
			{
				try
				{
					pongReceived.Reset();
					if (!send(Fin.Final, Opcode.Ping, data, compressed: false))
					{
						return false;
					}
					return pongReceived.WaitOne(_waitTime);
				}
				catch (ObjectDisposedException)
				{
					return false;
				}
			}
		}

		private bool processCloseFrame(WebSocketFrame frame)
		{
			PayloadData payloadData = frame.PayloadData;
			bool flag = !payloadData.HasReservedCode;
			close(payloadData, flag, received: true);
			return false;
		}

		private void processCookies(CookieCollection cookies)
		{
			if (cookies.Count != 0)
			{
				_cookies.SetOrRemove(cookies);
			}
		}

		private bool processDataFrame(WebSocketFrame frame)
		{
			MessageEventArgs e = (frame.IsCompressed ? new MessageEventArgs(frame.Opcode, frame.PayloadData.ApplicationData.Decompress(_compression)) : new MessageEventArgs(frame));
			enqueueToMessageEventQueue(e);
			return true;
		}

		private bool processFragmentFrame(WebSocketFrame frame)
		{
			if (!_inContinuation)
			{
				if (frame.IsContinuation)
				{
					return true;
				}
				_fragmentsOpcode = frame.Opcode;
				_fragmentsCompressed = frame.IsCompressed;
				_fragmentsBuffer = new MemoryStream();
				_inContinuation = true;
			}
			_fragmentsBuffer.WriteBytes(frame.PayloadData.ApplicationData, 1024);
			if (frame.IsFinal)
			{
				using (_fragmentsBuffer)
				{
					byte[] rawData = (_fragmentsCompressed ? _fragmentsBuffer.DecompressToArray(_compression) : _fragmentsBuffer.ToArray());
					MessageEventArgs e = new MessageEventArgs(_fragmentsOpcode, rawData);
					enqueueToMessageEventQueue(e);
				}
				_fragmentsBuffer = null;
				_inContinuation = false;
			}
			return true;
		}

		private bool processPingFrame(WebSocketFrame frame)
		{
			_log.Trace("A ping was received.");
			WebSocketFrame webSocketFrame = WebSocketFrame.CreatePongFrame(frame.PayloadData, _client);
			lock (_forState)
			{
				if (_readyState != WebSocketState.Open)
				{
					_log.Trace("A pong to this ping cannot be sent.");
					return true;
				}
				byte[] bytes = webSocketFrame.ToArray();
				if (!sendBytes(bytes))
				{
					return false;
				}
			}
			_log.Trace("A pong to this ping has been sent.");
			if (_emitOnPing)
			{
				if (_client)
				{
					webSocketFrame.Unmask();
				}
				MessageEventArgs e = new MessageEventArgs(frame);
				enqueueToMessageEventQueue(e);
			}
			return true;
		}

		private bool processPongFrame(WebSocketFrame frame)
		{
			_log.Trace("A pong was received.");
			try
			{
				_pongReceived.Set();
			}
			catch (NullReferenceException)
			{
				return false;
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
			_log.Trace("It has been signaled.");
			return true;
		}

		private bool processReceivedFrame(WebSocketFrame frame)
		{
			if (!checkReceivedFrame(frame, out var text))
			{
				_log.Error(text);
				_log.Debug(frame.ToString(dump: false));
				abort(1002, "An error has occurred while receiving.");
				return false;
			}
			frame.Unmask();
			return frame.IsFragment ? processFragmentFrame(frame) : (frame.IsData ? processDataFrame(frame) : (frame.IsPing ? processPingFrame(frame) : (frame.IsPong ? processPongFrame(frame) : (frame.IsClose ? processCloseFrame(frame) : processUnsupportedFrame(frame)))));
		}

		private void processSecWebSocketExtensionsClientHeader(string value)
		{
			if (value == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(80);
			bool flag = false;
			foreach (string item in value.SplitHeaderValue(','))
			{
				string text = item.Trim();
				if (text.Length != 0 && !flag && text.IsCompressionExtension(CompressionMethod.Deflate))
				{
					_compression = CompressionMethod.Deflate;
					string arg = _compression.ToExtensionString("client_no_context_takeover", "server_no_context_takeover");
					stringBuilder.AppendFormat("{0}, ", arg);
					flag = true;
				}
			}
			int length = stringBuilder.Length;
			if (length > 2)
			{
				stringBuilder.Length = length - 2;
				_extensions = stringBuilder.ToString();
			}
		}

		private void processSecWebSocketExtensionsServerHeader(string value)
		{
			if (value == null)
			{
				_compression = CompressionMethod.None;
			}
			else
			{
				_extensions = value;
			}
		}

		private bool processUnsupportedFrame(WebSocketFrame frame)
		{
			_log.Fatal("An unsupported frame was received.");
			_log.Debug(frame.ToString(dump: false));
			abort(1003, "There is no way to handle it.");
			return false;
		}

		private void refuseHandshake(ushort code, string reason)
		{
			createHandshakeFailureResponse().WriteTo(_stream);
			abort(code, reason);
		}

		private void releaseClientResources()
		{
			if (_stream != null)
			{
				_stream.Dispose();
				_stream = null;
			}
			if (_tcpClient != null)
			{
				_tcpClient.Close();
				_tcpClient = null;
			}
		}

		private void releaseCommonResources()
		{
			if (_fragmentsBuffer != null)
			{
				_fragmentsBuffer.Dispose();
				_fragmentsBuffer = null;
				_inContinuation = false;
			}
			if (_pongReceived != null)
			{
				_pongReceived.Close();
				_pongReceived = null;
			}
			if (_receivingExited != null)
			{
				_receivingExited.Close();
				_receivingExited = null;
			}
		}

		private void releaseResources()
		{
			if (_client)
			{
				releaseClientResources();
			}
			else
			{
				releaseServerResources();
			}
			releaseCommonResources();
		}

		private void releaseServerResources()
		{
			if (_closeContext != null)
			{
				_closeContext();
				_closeContext = null;
			}
			_stream = null;
			_context = null;
		}

		private bool send(byte[] rawFrame)
		{
			lock (_forState)
			{
				if (_readyState != WebSocketState.Open)
				{
					_log.Error("The current state of the interface is not Open.");
					return false;
				}
				return sendBytes(rawFrame);
			}
		}

		private bool send(Opcode opcode, Stream sourceStream)
		{
			lock (_forSend)
			{
				Stream stream = sourceStream;
				bool flag = false;
				bool flag2 = false;
				try
				{
					if (_compression != CompressionMethod.None)
					{
						stream = sourceStream.Compress(_compression);
						flag = true;
					}
					flag2 = send(opcode, stream, flag);
					if (!flag2)
					{
						error("A send has failed.", null);
					}
				}
				catch (Exception ex)
				{
					_log.Error(ex.Message);
					_log.Debug(ex.ToString());
					error("An exception has occurred during a send.", ex);
				}
				finally
				{
					if (flag)
					{
						stream.Dispose();
					}
					sourceStream.Dispose();
				}
				return flag2;
			}
		}

		private bool send(Opcode opcode, Stream dataStream, bool compressed)
		{
			long length = dataStream.Length;
			if (length == 0)
			{
				return send(Fin.Final, opcode, EmptyBytes, compressed: false);
			}
			long num = length / FragmentLength;
			int num2 = (int)(length % FragmentLength);
			byte[] array = null;
			switch (num)
			{
			case 0L:
				array = new byte[num2];
				return dataStream.Read(array, 0, num2) == num2 && send(Fin.Final, opcode, array, compressed);
			case 1L:
				if (num2 == 0)
				{
					array = new byte[FragmentLength];
					return dataStream.Read(array, 0, FragmentLength) == FragmentLength && send(Fin.Final, opcode, array, compressed);
				}
				break;
			}
			array = new byte[FragmentLength];
			if (dataStream.Read(array, 0, FragmentLength) != FragmentLength || !send(Fin.More, opcode, array, compressed))
			{
				return false;
			}
			long num3 = ((num2 == 0) ? (num - 2) : (num - 1));
			for (long num4 = 0L; num4 < num3; num4++)
			{
				if (dataStream.Read(array, 0, FragmentLength) != FragmentLength || !send(Fin.More, Opcode.Cont, array, compressed: false))
				{
					return false;
				}
			}
			if (num2 == 0)
			{
				num2 = FragmentLength;
			}
			else
			{
				array = new byte[num2];
			}
			return dataStream.Read(array, 0, num2) == num2 && send(Fin.Final, Opcode.Cont, array, compressed: false);
		}

		private bool send(Fin fin, Opcode opcode, byte[] data, bool compressed)
		{
			WebSocketFrame webSocketFrame = new WebSocketFrame(fin, opcode, data, compressed, _client);
			byte[] rawFrame = webSocketFrame.ToArray();
			return send(rawFrame);
		}

		private void sendAsync(Opcode opcode, Stream sourceStream, Action<bool> completed)
		{
			Func<Opcode, Stream, bool> sender = send;
			sender.BeginInvoke(opcode, sourceStream, (IAsyncResult ar) =>
			{
				try
				{
					bool obj = sender.EndInvoke(ar);
					if (completed != null)
					{
						completed(obj);
					}
				}
				catch (Exception ex)
				{
					_log.Error(ex.Message);
					_log.Debug(ex.ToString());
					error("An exception has occurred during the callback for an async send.", ex);
				}
			}, null);
		}

		private bool sendBytes(byte[] bytes)
		{
			try
			{
				_stream.Write(bytes, 0, bytes.Length);
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
				_log.Debug(ex.ToString());
				return false;
			}
			return true;
		}

		private HttpResponse sendHandshakeRequest()
		{
			HttpRequest httpRequest = createHandshakeRequest();
			HttpResponse httpResponse = sendHttpRequest(httpRequest, 90000);
			if (httpResponse.IsUnauthorized)
			{
				if (_credentials == null)
				{
					_log.Error("No credential is specified.");
					return httpResponse;
				}
				string value = httpResponse.Headers["WWW-Authenticate"];
				if (value.IsNullOrEmpty())
				{
					_log.Error("No authentication challenge is specified.");
					return httpResponse;
				}
				AuthenticationChallenge authenticationChallenge = AuthenticationChallenge.Parse(value);
				if (authenticationChallenge == null)
				{
					_log.Error("An invalid authentication challenge is specified.");
					return httpResponse;
				}
				_authChallenge = authenticationChallenge;
				if (_preAuth && _authChallenge.Scheme == AuthenticationSchemes.Basic)
				{
					_log.Error("The authentication has failed.");
					return httpResponse;
				}
				AuthenticationResponse authenticationResponse = new AuthenticationResponse(_authChallenge, _credentials, _nonceCount);
				_nonceCount = authenticationResponse.NonceCount;
				httpRequest.Headers["Authorization"] = authenticationResponse.ToString();
				if (httpResponse.CloseConnection)
				{
					releaseClientResources();
					setClientStream();
				}
				httpResponse = sendHttpRequest(httpRequest, 15000);
			}
			if (httpResponse.IsRedirect)
			{
				if (!_enableRedirection)
				{
					return httpResponse;
				}
				string text = httpResponse.Headers["Location"];
				if (text.IsNullOrEmpty())
				{
					_log.Error("No url to redirect is located.");
					return httpResponse;
				}
				if (!text.TryCreateWebSocketUri(out var result, out var _))
				{
					_log.Error("An invalid url to redirect is located.");
					return httpResponse;
				}
				releaseClientResources();
				_uri = result;
				_secure = result.Scheme == "wss";
				setClientStream();
				return sendHandshakeRequest();
			}
			return httpResponse;
		}

		private HttpResponse sendHttpRequest(HttpRequest request, int millisecondsTimeout)
		{
			return request.GetResponse(_stream, millisecondsTimeout);
		}

		private void sendProxyConnectRequest()
		{
			HttpRequest httpRequest = HttpRequest.CreateConnectRequest(_uri);
			HttpResponse httpResponse = sendHttpRequest(httpRequest, 90000);
			if (httpResponse.IsProxyAuthenticationRequired)
			{
				if (_proxyCredentials == null)
				{
					string text = "No credential for the proxy is specified.";
					throw new WebSocketException(text);
				}
				string value = httpResponse.Headers["Proxy-Authenticate"];
				if (value.IsNullOrEmpty())
				{
					string text2 = "No proxy authentication challenge is specified.";
					throw new WebSocketException(text2);
				}
				AuthenticationChallenge authenticationChallenge = AuthenticationChallenge.Parse(value);
				if (authenticationChallenge == null)
				{
					string text3 = "An invalid proxy authentication challenge is specified.";
					throw new WebSocketException(text3);
				}
				AuthenticationResponse authenticationResponse = new AuthenticationResponse(authenticationChallenge, _proxyCredentials, 0u);
				httpRequest.Headers["Proxy-Authorization"] = authenticationResponse.ToString();
				if (httpResponse.CloseConnection)
				{
					releaseClientResources();
					_tcpClient = new TcpClient(_proxyUri.DnsSafeHost, _proxyUri.Port);
					_stream = _tcpClient.GetStream();
				}
				httpResponse = sendHttpRequest(httpRequest, 15000);
				if (httpResponse.IsProxyAuthenticationRequired)
				{
					string text4 = "The proxy authentication has failed.";
					throw new WebSocketException(text4);
				}
			}
			if (!httpResponse.IsSuccess)
			{
				string text5 = "The proxy has failed a connection to the requested URL.";
				throw new WebSocketException(text5);
			}
		}

		private void setClientStream()
		{
			if (_proxyUri != null)
			{
				_tcpClient = new TcpClient(_proxyUri.DnsSafeHost, _proxyUri.Port);
				_stream = _tcpClient.GetStream();
				sendProxyConnectRequest();
			}
			else
			{
				_tcpClient = new TcpClient(_uri.DnsSafeHost, _uri.Port);
				_stream = _tcpClient.GetStream();
			}
			if (_secure)
			{
				ClientSslConfiguration sslConfiguration = getSslConfiguration();
				string targetHost = sslConfiguration.TargetHost;
				if (targetHost != _uri.DnsSafeHost)
				{
					string text = "An invalid host name is specified.";
					throw new WebSocketException(CloseStatusCode.TlsHandshakeFailure, text);
				}
				try
				{
					SslStream sslStream = new SslStream(_stream, leaveInnerStreamOpen: false, sslConfiguration.ServerCertificateValidationCallback, sslConfiguration.ClientCertificateSelectionCallback);
					sslStream.AuthenticateAsClient(targetHost, sslConfiguration.ClientCertificates, sslConfiguration.EnabledSslProtocols, sslConfiguration.CheckCertificateRevocation);
					_stream = sslStream;
				}
				catch (Exception innerException)
				{
					throw new WebSocketException(CloseStatusCode.TlsHandshakeFailure, innerException);
				}
			}
		}

		private void startReceiving()
		{
			if (_messageEventQueue.Count > 0)
			{
				_messageEventQueue.Clear();
			}
			_pongReceived = new ManualResetEvent(initialState: false);
			_receivingExited = new ManualResetEvent(initialState: false);
			Action receive = null;
			receive = () =>
			{
				WebSocketFrame.ReadFrameAsync(_stream, unmask: false, (WebSocketFrame frame) =>
				{
					if (!processReceivedFrame(frame) || _readyState == WebSocketState.Closed)
					{
						_receivingExited?.Set();
					}
					else
					{
						receive();
						if (!_inMessage)
						{
							message();
						}
					}
				}, (Exception ex) =>
				{
					_log.Fatal(ex.Message);
					_log.Debug(ex.ToString());
					abort("An exception has occurred while receiving.", ex);
				});
			};
			receive();
		}

		private bool validateSecWebSocketExtensionsServerHeader(string value)
		{
			if (value == null)
			{
				return true;
			}
			if (value.Length == 0)
			{
				return false;
			}
			if (!_extensionsRequested)
			{
				return false;
			}
			bool flag = _compression != CompressionMethod.None;
			foreach (string item in value.SplitHeaderValue(','))
			{
				string text = item.Trim();
				if (flag && text.IsCompressionExtension(_compression))
				{
					string param1 = "server_no_context_takeover";
					string param2 = "client_no_context_takeover";
					if (!text.Contains(param1))
					{
						string format = "The server did not send back '{0}'.";
						string text2 = string.Format(format, param1);
						_log.Error(text2);
						return false;
					}
					string name = _compression.ToExtensionString();
					if (text.SplitHeaderValue(';').Contains((string t) =>
					{
						t = t.Trim();
						bool flag2 = t == name || t == param1 || t == param2;
						return !flag2;
					}))
					{
						return false;
					}
					flag = false;
					continue;
				}
				return false;
			}
			return true;
		}

		internal void Accept()
		{
			if (accept())
			{
				open();
			}
		}

		internal void AcceptAsync()
		{
			Func<bool> acceptor = accept;
			acceptor.BeginInvoke((IAsyncResult ar) =>
			{
				if (acceptor.EndInvoke(ar))
				{
					open();
				}
			}, null);
		}

		internal void Close(PayloadData payloadData, byte[] rawFrame)
		{
			lock (_forState)
			{
				if (_readyState == WebSocketState.Closing)
				{
					_log.Trace("The close process is already in progress.");
					return;
				}
				if (_readyState == WebSocketState.Closed)
				{
					_log.Trace("The connection has already been closed.");
					return;
				}
				_readyState = WebSocketState.Closing;
			}
			_log.Trace("Begin closing the connection.");
			bool flag = rawFrame != null && sendBytes(rawFrame);
			bool flag2 = flag && _receivingExited != null && _receivingExited.WaitOne(_waitTime);
			bool flag3 = flag & flag2;
			string text = $"The closing was clean? {flag3} (sent: {flag} received: {flag2})";
			_log.Debug(text);
			releaseServerResources();
			releaseCommonResources();
			_log.Trace("End closing the connection.");
			_readyState = WebSocketState.Closed;
			CloseEventArgs e = new CloseEventArgs(payloadData, flag3);
			try
			{
				OnClose.Emit(this, e);
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
				_log.Debug(ex.ToString());
			}
		}

		internal static string CreateBase64Key()
		{
			byte[] array = new byte[16];
			RandomNumber.GetBytes(array);
			return Convert.ToBase64String(array);
		}

		internal static string CreateResponseKey(string base64Key)
		{
			SHA1 sHA = new SHA1CryptoServiceProvider();
			string s = base64Key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
			byte[] uTF8EncodedBytes = s.GetUTF8EncodedBytes();
			byte[] inArray = sHA.ComputeHash(uTF8EncodedBytes);
			return Convert.ToBase64String(inArray);
		}

		internal bool Ping(byte[] rawFrame)
		{
			if (_readyState != WebSocketState.Open)
			{
				return false;
			}
			ManualResetEvent pongReceived = _pongReceived;
			if (pongReceived == null)
			{
				return false;
			}
			lock (_forPing)
			{
				try
				{
					pongReceived.Reset();
					if (!send(rawFrame))
					{
						return false;
					}
					return pongReceived.WaitOne(_waitTime);
				}
				catch (ObjectDisposedException)
				{
					return false;
				}
			}
		}

		internal void Send(Opcode opcode, byte[] data, Dictionary<CompressionMethod, byte[]> cache)
		{
			lock (_forSend)
			{
				if (!cache.TryGetValue(_compression, out var value))
				{
					value = new WebSocketFrame(Fin.Final, opcode, data.Compress(_compression), _compression != CompressionMethod.None, mask: false).ToArray();
					cache.Add(_compression, value);
				}
				send(value);
			}
		}

		internal void Send(Opcode opcode, Stream sourceStream, Dictionary<CompressionMethod, Stream> cache)
		{
			lock (_forSend)
			{
				if (!cache.TryGetValue(_compression, out var value))
				{
					value = sourceStream.Compress(_compression);
					cache.Add(_compression, value);
				}
				else
				{
					value.Position = 0L;
				}
				send(opcode, value, _compression != CompressionMethod.None);
			}
		}

		public void Close()
		{
			close(1005, string.Empty);
		}

		public void Close(ushort code)
		{
			Close(code, string.Empty);
		}

		public void Close(CloseStatusCode code)
		{
			Close(code, string.Empty);
		}

		public void Close(ushort code, string reason)
		{
			if (!code.IsCloseStatusCode())
			{
				string text = "Less than 1000 or greater than 4999.";
				throw new ArgumentOutOfRangeException("code", text);
			}
			if (_client && code == 1011)
			{
				string text2 = "1011 cannot be used.";
				throw new ArgumentException(text2, "code");
			}
			if (!_client && code == 1010)
			{
				string text3 = "1010 cannot be used.";
				throw new ArgumentException(text3, "code");
			}
			if (reason.IsNullOrEmpty())
			{
				close(code, string.Empty);
				return;
			}
			if (code == 1005)
			{
				string text4 = "1005 cannot be used.";
				throw new ArgumentException(text4, "code");
			}
			if (!reason.TryGetUTF8EncodedBytes(out var bytes))
			{
				string text5 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(text5, "reason");
			}
			if (bytes.Length > 123)
			{
				string text6 = "Its size is greater than 123 bytes.";
				throw new ArgumentOutOfRangeException("reason", text6);
			}
			close(code, reason);
		}

		public void Close(CloseStatusCode code, string reason)
		{
			if (_client && code == CloseStatusCode.ServerError)
			{
				string text = "ServerError cannot be used.";
				throw new ArgumentException(text, "code");
			}
			if (!_client && code == CloseStatusCode.MandatoryExtension)
			{
				string text2 = "MandatoryExtension cannot be used.";
				throw new ArgumentException(text2, "code");
			}
			if (reason.IsNullOrEmpty())
			{
				close((ushort)code, string.Empty);
				return;
			}
			if (code == CloseStatusCode.NoStatus)
			{
				string text3 = "NoStatus cannot be used.";
				throw new ArgumentException(text3, "code");
			}
			if (!reason.TryGetUTF8EncodedBytes(out var bytes))
			{
				string text4 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(text4, "reason");
			}
			if (bytes.Length > 123)
			{
				string text5 = "Its size is greater than 123 bytes.";
				throw new ArgumentOutOfRangeException("reason", text5);
			}
			close((ushort)code, reason);
		}

		public void CloseAsync()
		{
			closeAsync(1005, string.Empty);
		}

		public void CloseAsync(ushort code)
		{
			CloseAsync(code, string.Empty);
		}

		public void CloseAsync(CloseStatusCode code)
		{
			CloseAsync(code, string.Empty);
		}

		public void CloseAsync(ushort code, string reason)
		{
			if (!code.IsCloseStatusCode())
			{
				string text = "Less than 1000 or greater than 4999.";
				throw new ArgumentOutOfRangeException("code", text);
			}
			if (_client && code == 1011)
			{
				string text2 = "1011 cannot be used.";
				throw new ArgumentException(text2, "code");
			}
			if (!_client && code == 1010)
			{
				string text3 = "1010 cannot be used.";
				throw new ArgumentException(text3, "code");
			}
			if (reason.IsNullOrEmpty())
			{
				closeAsync(code, string.Empty);
				return;
			}
			if (code == 1005)
			{
				string text4 = "1005 cannot be used.";
				throw new ArgumentException(text4, "code");
			}
			if (!reason.TryGetUTF8EncodedBytes(out var bytes))
			{
				string text5 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(text5, "reason");
			}
			if (bytes.Length > 123)
			{
				string text6 = "Its size is greater than 123 bytes.";
				throw new ArgumentOutOfRangeException("reason", text6);
			}
			closeAsync(code, reason);
		}

		public void CloseAsync(CloseStatusCode code, string reason)
		{
			if (_client && code == CloseStatusCode.ServerError)
			{
				string text = "ServerError cannot be used.";
				throw new ArgumentException(text, "code");
			}
			if (!_client && code == CloseStatusCode.MandatoryExtension)
			{
				string text2 = "MandatoryExtension cannot be used.";
				throw new ArgumentException(text2, "code");
			}
			if (reason.IsNullOrEmpty())
			{
				closeAsync((ushort)code, string.Empty);
				return;
			}
			if (code == CloseStatusCode.NoStatus)
			{
				string text3 = "NoStatus cannot be used.";
				throw new ArgumentException(text3, "code");
			}
			if (!reason.TryGetUTF8EncodedBytes(out var bytes))
			{
				string text4 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(text4, "reason");
			}
			if (bytes.Length > 123)
			{
				string text5 = "Its size is greater than 123 bytes.";
				throw new ArgumentOutOfRangeException("reason", text5);
			}
			closeAsync((ushort)code, reason);
		}

		public void Connect()
		{
			if (!_client)
			{
				string text = "The interface is not for the client.";
				throw new InvalidOperationException(text);
			}
			if (_retryCountForConnect >= _maxRetryCountForConnect)
			{
				string text2 = "A series of reconnecting has failed.";
				throw new InvalidOperationException(text2);
			}
			if (connect())
			{
				open();
			}
		}

		public void ConnectAsync()
		{
			if (!_client)
			{
				string text = "The interface is not for the client.";
				throw new InvalidOperationException(text);
			}
			if (_retryCountForConnect >= _maxRetryCountForConnect)
			{
				string text2 = "A series of reconnecting has failed.";
				throw new InvalidOperationException(text2);
			}
			Func<bool> connector = connect;
			connector.BeginInvoke((IAsyncResult ar) =>
			{
				if (connector.EndInvoke(ar))
				{
					open();
				}
			}, null);
		}

		public bool Ping()
		{
			return ping(EmptyBytes);
		}

		public bool Ping(string message)
		{
			if (message.IsNullOrEmpty())
			{
				return ping(EmptyBytes);
			}
			if (!message.TryGetUTF8EncodedBytes(out var bytes))
			{
				string text = "It could not be UTF-8-encoded.";
				throw new ArgumentException(text, "message");
			}
			if (bytes.Length > 125)
			{
				string text2 = "Its size is greater than 125 bytes.";
				throw new ArgumentOutOfRangeException("message", text2);
			}
			return ping(bytes);
		}

		public void Send(byte[] data)
		{
			if (_readyState != WebSocketState.Open)
			{
				string text = "The current state of the interface is not Open.";
				throw new InvalidOperationException(text);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			send(Opcode.Binary, new MemoryStream(data));
		}

		public void Send(FileInfo fileInfo)
		{
			if (_readyState != WebSocketState.Open)
			{
				string text = "The current state of the interface is not Open.";
				throw new InvalidOperationException(text);
			}
			if (fileInfo == null)
			{
				throw new ArgumentNullException("fileInfo");
			}
			if (!fileInfo.Exists)
			{
				string text2 = "The file does not exist.";
				throw new ArgumentException(text2, "fileInfo");
			}
			if (!fileInfo.TryOpenRead(out var fileStream))
			{
				string text3 = "The file could not be opened.";
				throw new ArgumentException(text3, "fileInfo");
			}
			send(Opcode.Binary, fileStream);
		}

		public void Send(string data)
		{
			if (_readyState != WebSocketState.Open)
			{
				string text = "The current state of the interface is not Open.";
				throw new InvalidOperationException(text);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (!data.TryGetUTF8EncodedBytes(out var bytes))
			{
				string text2 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(text2, "data");
			}
			send(Opcode.Text, new MemoryStream(bytes));
		}

		public void Send(Stream stream, int length)
		{
			if (_readyState != WebSocketState.Open)
			{
				string text = "The current state of the interface is not Open.";
				throw new InvalidOperationException(text);
			}
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (!stream.CanRead)
			{
				string text2 = "It cannot be read.";
				throw new ArgumentException(text2, "stream");
			}
			if (length < 1)
			{
				string text3 = "Less than 1.";
				throw new ArgumentException(text3, "length");
			}
			byte[] array = stream.ReadBytes(length);
			int num = array.Length;
			if (num == 0)
			{
				string text4 = "No data could be read from it.";
				throw new ArgumentException(text4, "stream");
			}
			if (num < length)
			{
				string format = "Only {0} byte(s) of data could be read from the stream.";
				string text5 = string.Format(format, num);
				_log.Warn(text5);
			}
			send(Opcode.Binary, new MemoryStream(array));
		}

		public void SendAsync(byte[] data, Action<bool> completed)
		{
			if (_readyState != WebSocketState.Open)
			{
				string text = "The current state of the interface is not Open.";
				throw new InvalidOperationException(text);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			sendAsync(Opcode.Binary, new MemoryStream(data), completed);
		}

		public void SendAsync(FileInfo fileInfo, Action<bool> completed)
		{
			if (_readyState != WebSocketState.Open)
			{
				string text = "The current state of the interface is not Open.";
				throw new InvalidOperationException(text);
			}
			if (fileInfo == null)
			{
				throw new ArgumentNullException("fileInfo");
			}
			if (!fileInfo.Exists)
			{
				string text2 = "The file does not exist.";
				throw new ArgumentException(text2, "fileInfo");
			}
			if (!fileInfo.TryOpenRead(out var fileStream))
			{
				string text3 = "The file could not be opened.";
				throw new ArgumentException(text3, "fileInfo");
			}
			sendAsync(Opcode.Binary, fileStream, completed);
		}

		public void SendAsync(string data, Action<bool> completed)
		{
			if (_readyState != WebSocketState.Open)
			{
				string text = "The current state of the interface is not Open.";
				throw new InvalidOperationException(text);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (!data.TryGetUTF8EncodedBytes(out var bytes))
			{
				string text2 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(text2, "data");
			}
			sendAsync(Opcode.Text, new MemoryStream(bytes), completed);
		}

		public void SendAsync(Stream stream, int length, Action<bool> completed)
		{
			if (_readyState != WebSocketState.Open)
			{
				string text = "The current state of the interface is not Open.";
				throw new InvalidOperationException(text);
			}
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (!stream.CanRead)
			{
				string text2 = "It cannot be read.";
				throw new ArgumentException(text2, "stream");
			}
			if (length < 1)
			{
				string text3 = "Less than 1.";
				throw new ArgumentException(text3, "length");
			}
			byte[] array = stream.ReadBytes(length);
			int num = array.Length;
			if (num == 0)
			{
				string text4 = "No data could be read from it.";
				throw new ArgumentException(text4, "stream");
			}
			if (num < length)
			{
				string format = "Only {0} byte(s) of data could be read from the stream.";
				string text5 = string.Format(format, num);
				_log.Warn(text5);
			}
			sendAsync(Opcode.Binary, new MemoryStream(array), completed);
		}

		public void SetCookie(Cookie cookie)
		{
			if (!_client)
			{
				string text = "The interface is not for the client.";
				throw new InvalidOperationException(text);
			}
			if (cookie == null)
			{
				throw new ArgumentNullException("cookie");
			}
			lock (_forState)
			{
				if (!canSet())
				{
					return;
				}
				lock (_cookies.SyncRoot)
				{
					_cookies.SetOrRemove(cookie);
				}
			}
		}

		public void SetCredentials(string username, string password, bool preAuth)
		{
			if (!_client)
			{
				string text = "The interface is not for the client.";
				throw new InvalidOperationException(text);
			}
			if (!username.IsNullOrEmpty() && (Ext.Contains(username, ':') || !username.IsText()))
			{
				string text2 = "It contains an invalid character.";
				throw new ArgumentException(text2, "username");
			}
			if (!password.IsNullOrEmpty() && !password.IsText())
			{
				string text3 = "It contains an invalid character.";
				throw new ArgumentException(text3, "password");
			}
			lock (_forState)
			{
				if (canSet())
				{
					if (username.IsNullOrEmpty())
					{
						_credentials = null;
						_preAuth = false;
					}
					else
					{
						_credentials = new NetworkCredential(username, password, _uri.PathAndQuery);
						_preAuth = preAuth;
					}
				}
			}
		}

		public void SetProxy(string url, string username, string password)
		{
			if (!_client)
			{
				string text = "The interface is not for the client.";
				throw new InvalidOperationException(text);
			}
			Uri result = null;
			if (!url.IsNullOrEmpty())
			{
				if (!Uri.TryCreate(url, UriKind.Absolute, out result))
				{
					string text2 = "Not an absolute URI string.";
					throw new ArgumentException(text2, "url");
				}
				if (result.Scheme != "http")
				{
					string text3 = "The scheme part is not http.";
					throw new ArgumentException(text3, "url");
				}
				if (result.Segments.Length > 1)
				{
					string text4 = "It includes the path segments.";
					throw new ArgumentException(text4, "url");
				}
			}
			if (!username.IsNullOrEmpty() && (Ext.Contains(username, ':') || !username.IsText()))
			{
				string text5 = "It contains an invalid character.";
				throw new ArgumentException(text5, "username");
			}
			if (!password.IsNullOrEmpty() && !password.IsText())
			{
				string text6 = "It contains an invalid character.";
				throw new ArgumentException(text6, "password");
			}
			lock (_forState)
			{
				if (canSet())
				{
					if (url.IsNullOrEmpty())
					{
						_proxyUri = null;
						_proxyCredentials = null;
					}
					else
					{
						_proxyUri = result;
						_proxyCredentials = ((!username.IsNullOrEmpty()) ? new NetworkCredential(username, password, $"{_uri.DnsSafeHost}:{_uri.Port}") : null);
					}
				}
			}
		}

		void IDisposable.Dispose()
		{
			close(1001, string.Empty);
		}
	}
}
#endif