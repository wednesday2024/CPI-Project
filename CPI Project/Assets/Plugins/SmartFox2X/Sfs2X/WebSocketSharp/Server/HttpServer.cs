using System;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Sfs2X.WebSocketSharp.Net;
using Sfs2X.WebSocketSharp.Net.WebSockets;

namespace Sfs2X.WebSocketSharp.Server
{
	public class HttpServer
	{
		private IPAddress _address;

		private string _docRootPath;

		private string _hostname;

		private Sfs2X.WebSocketSharp.Net.HttpListener _listener;

		private Logger _log;

		private int _port;

		private Thread _receiveThread;

		private bool _secure;

		private WebSocketServiceManager _services;

		private volatile ServerState _state;

		private object _sync;

		public IPAddress Address => _address;

		public Sfs2X.WebSocketSharp.Net.AuthenticationSchemes AuthenticationSchemes
		{
			get
			{
				return _listener.AuthenticationSchemes;
			}
			set
			{
				lock (_sync)
				{
					if (canSet())
					{
						_listener.AuthenticationSchemes = value;
					}
				}
			}
		}

		public string DocumentRootPath
		{
			get
			{
				return _docRootPath;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (value.Length == 0)
				{
					throw new ArgumentException("An empty string.", "value");
				}
				value = value.TrimSlashOrBackslashFromEnd();
				if (value == "/")
				{
					throw new ArgumentException("An absolute root.", "value");
				}
				if (value == "\\")
				{
					throw new ArgumentException("An absolute root.", "value");
				}
				if (value.Length == 2 && value[1] == ':')
				{
					throw new ArgumentException("An absolute root.", "value");
				}
				string text = null;
				try
				{
					text = Path.GetFullPath(value);
				}
				catch (Exception innerException)
				{
					throw new ArgumentException("An invalid path string.", "value", innerException);
				}
				if (text == "/")
				{
					throw new ArgumentException("An absolute root.", "value");
				}
				text = text.TrimSlashOrBackslashFromEnd();
				if (text.Length == 2 && text[1] == ':')
				{
					throw new ArgumentException("An absolute root.", "value");
				}
				lock (_sync)
				{
					if (canSet())
					{
						_docRootPath = value;
					}
				}
			}
		}

		public bool IsListening => _state == ServerState.Start;

		public bool IsSecure => _secure;

		public bool KeepClean
		{
			get
			{
				return _services.KeepClean;
			}
			set
			{
				_services.KeepClean = value;
			}
		}

		public Logger Log => _log;

		public int Port => _port;

		public string Realm
		{
			get
			{
				return _listener.Realm;
			}
			set
			{
				lock (_sync)
				{
					if (canSet())
					{
						_listener.Realm = value;
					}
				}
			}
		}

		public bool ReuseAddress
		{
			get
			{
				return _listener.ReuseAddress;
			}
			set
			{
				lock (_sync)
				{
					if (canSet())
					{
						_listener.ReuseAddress = value;
					}
				}
			}
		}

		public ServerSslConfiguration SslConfiguration
		{
			get
			{
				if (!_secure)
				{
					string message = "The server does not provide secure connections.";
					throw new InvalidOperationException(message);
				}
				return _listener.SslConfiguration;
			}
		}

		public Func<IIdentity, Sfs2X.WebSocketSharp.Net.NetworkCredential> UserCredentialsFinder
		{
			get
			{
				return _listener.UserCredentialsFinder;
			}
			set
			{
				lock (_sync)
				{
					if (canSet())
					{
						_listener.UserCredentialsFinder = value;
					}
				}
			}
		}

		public TimeSpan WaitTime
		{
			get
			{
				return _services.WaitTime;
			}
			set
			{
				_services.WaitTime = value;
			}
		}

		public WebSocketServiceManager WebSocketServices => _services;

		public event EventHandler<HttpRequestEventArgs> OnConnect;

		public event EventHandler<HttpRequestEventArgs> OnDelete;

		public event EventHandler<HttpRequestEventArgs> OnGet;

		public event EventHandler<HttpRequestEventArgs> OnHead;

		public event EventHandler<HttpRequestEventArgs> OnOptions;

		public event EventHandler<HttpRequestEventArgs> OnPost;

		public event EventHandler<HttpRequestEventArgs> OnPut;

		public event EventHandler<HttpRequestEventArgs> OnTrace;

		public HttpServer()
		{
			init("*", IPAddress.Any, 80, secure: false);
		}

		public HttpServer(int port)
			: this(port, port == 443)
		{
		}

		public HttpServer(string url)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}
			if (url.Length == 0)
			{
				throw new ArgumentException("An empty string.", "url");
			}
			if (!tryCreateUri(url, out var result, out var message))
			{
				throw new ArgumentException(message, "url");
			}
			string dnsSafeHost = result.GetDnsSafeHost(bracketIPv6: true);
			IPAddress iPAddress = dnsSafeHost.ToIPAddress();
			if (iPAddress == null)
			{
				message = "The host part could not be converted to an IP address.";
				throw new ArgumentException(message, "url");
			}
			if (!iPAddress.IsLocal())
			{
				message = "The IP address of the host is not a local IP address.";
				throw new ArgumentException(message, "url");
			}
			init(dnsSafeHost, iPAddress, result.Port, result.Scheme == "https");
		}

		public HttpServer(int port, bool secure)
		{
			if (!port.IsPortNumber())
			{
				string message = "Less than 1 or greater than 65535.";
				throw new ArgumentOutOfRangeException("port", message);
			}
			init("*", IPAddress.Any, port, secure);
		}

		public HttpServer(IPAddress address, int port)
			: this(address, port, port == 443)
		{
		}

		public HttpServer(IPAddress address, int port, bool secure)
		{
			if (address == null)
			{
				throw new ArgumentNullException("address");
			}
			if (!address.IsLocal())
			{
				string message = "Not a local IP address.";
				throw new ArgumentException(message, "address");
			}
			if (!port.IsPortNumber())
			{
				string message2 = "Less than 1 or greater than 65535.";
				throw new ArgumentOutOfRangeException("port", message2);
			}
			init(address.ToString(bracketIPv6: true), address, port, secure);
		}

		private void abort()
		{
			lock (_sync)
			{
				if (_state != ServerState.Start)
				{
					return;
				}
				_state = ServerState.ShuttingDown;
			}
			try
			{
				_services.Stop(1006, string.Empty);
			}
			catch (Exception ex)
			{
				_log.Fatal(ex.Message);
				_log.Debug(ex.ToString());
			}
			try
			{
				_listener.Abort();
			}
			catch (Exception ex2)
			{
				_log.Fatal(ex2.Message);
				_log.Debug(ex2.ToString());
			}
			_state = ServerState.Stop;
		}

		private bool canSet()
		{
			return _state == ServerState.Ready || _state == ServerState.Stop;
		}

		private bool checkCertificate(out string message)
		{
			message = null;
			bool flag = _listener.SslConfiguration.ServerCertificate != null;
			string certificateFolderPath = _listener.CertificateFolderPath;
			bool flag2 = EndPointListener.CertificateExists(_port, certificateFolderPath);
			if (!(flag | flag2))
			{
				message = "There is no server certificate for secure connection.";
				return false;
			}
			if (flag & flag2)
			{
				string message2 = "The server certificate associated with the port is used.";
				_log.Warn(message2);
			}
			return true;
		}

		private static Sfs2X.WebSocketSharp.Net.HttpListener createListener(string hostname, int port, bool secure)
		{
			Sfs2X.WebSocketSharp.Net.HttpListener httpListener = new Sfs2X.WebSocketSharp.Net.HttpListener();
			string arg = (secure ? "https" : "http");
			string uriPrefix = $"{arg}://{hostname}:{port}/";
			httpListener.Prefixes.Add(uriPrefix);
			return httpListener;
		}

		private void init(string hostname, IPAddress address, int port, bool secure)
		{
			_hostname = hostname;
			_address = address;
			_port = port;
			_secure = secure;
			_docRootPath = "./Public";
			_listener = createListener(_hostname, _port, _secure);
			_log = _listener.Log;
			_services = new WebSocketServiceManager(_log);
			_sync = new object();
		}

		private void processRequest(Sfs2X.WebSocketSharp.Net.HttpListenerContext context)
		{
			EventHandler<HttpRequestEventArgs> eventHandler = context.Request.HttpMethod switch
			{
				"TRACE" => OnTrace, 
				"OPTIONS" => OnOptions, 
				"CONNECT" => OnConnect, 
				"DELETE" => OnDelete, 
				"PUT" => OnPut, 
				"POST" => OnPost, 
				"HEAD" => OnHead, 
				"GET" => OnGet, 
				_ => null, 
			};
			if (eventHandler == null)
			{
				context.ErrorStatusCode = 501;
				context.SendError();
			}
			else
			{
				HttpRequestEventArgs e = new HttpRequestEventArgs(context, _docRootPath);
				eventHandler(this, e);
				context.Response.Close();
			}
		}

		private void processRequest(HttpListenerWebSocketContext context)
		{
			Uri requestUri = context.RequestUri;
			if (requestUri == null)
			{
				context.Close(Sfs2X.WebSocketSharp.Net.HttpStatusCode.BadRequest);
				return;
			}
			string text = requestUri.AbsolutePath;
			if (text.IndexOfAny(new char[2] { '%', '+' }) > -1)
			{
				text = HttpUtility.UrlDecode(text, Encoding.UTF8);
			}
			if (!_services.InternalTryGetServiceHost(text, out var host))
			{
				context.Close(Sfs2X.WebSocketSharp.Net.HttpStatusCode.NotImplemented);
			}
			else
			{
				host.StartSession(context);
			}
		}

		private void receiveRequest()
		{
			while (true)
			{
				Sfs2X.WebSocketSharp.Net.HttpListenerContext ctx = null;
				try
				{
					ctx = _listener.GetContext();
					ThreadPool.QueueUserWorkItem((object state) =>
					{
						try
						{
							if (ctx.Request.IsUpgradeRequest("websocket"))
							{
								processRequest(ctx.GetWebSocketContext(null));
							}
							else
							{
								processRequest(ctx);
							}
						}
						catch (Exception ex4)
						{
							_log.Error(ex4.Message);
							_log.Debug(ex4.ToString());
							ctx.Connection.Close(force: true);
						}
					});
				}
				catch (Sfs2X.WebSocketSharp.Net.HttpListenerException ex)
				{
					if (_state == ServerState.ShuttingDown)
					{
						return;
					}
					_log.Fatal(ex.Message);
					_log.Debug(ex.ToString());
					break;
				}
				catch (InvalidOperationException ex2)
				{
					if (_state == ServerState.ShuttingDown)
					{
						return;
					}
					_log.Fatal(ex2.Message);
					_log.Debug(ex2.ToString());
					break;
				}
				catch (Exception ex3)
				{
					_log.Fatal(ex3.Message);
					_log.Debug(ex3.ToString());
					if (ctx != null)
					{
						ctx.Connection.Close(force: true);
					}
					if (_state == ServerState.ShuttingDown)
					{
						return;
					}
					break;
				}
			}
			abort();
		}

		private void start()
		{
			lock (_sync)
			{
				if (_state != ServerState.Start && _state != ServerState.ShuttingDown)
				{
					if (_secure && !checkCertificate(out var message))
					{
						throw new InvalidOperationException(message);
					}
					_services.Start();
					try
					{
						startReceiving();
					}
					catch
					{
						_services.Stop(1011, string.Empty);
						throw;
					}
					_state = ServerState.Start;
				}
			}
		}

		private void startReceiving()
		{
			try
			{
				_listener.Start();
			}
			catch (Exception innerException)
			{
				string message = "The underlying listener has failed to start.";
				throw new InvalidOperationException(message, innerException);
			}
			ThreadStart threadStart = receiveRequest;
			_receiveThread = new Thread(threadStart);
			_receiveThread.IsBackground = true;
			_receiveThread.Start();
		}

		private void stop(ushort code, string reason)
		{
			lock (_sync)
			{
				if (_state != ServerState.Start)
				{
					return;
				}
				_state = ServerState.ShuttingDown;
			}
			try
			{
				_services.Stop(code, reason);
			}
			catch (Exception ex)
			{
				_log.Fatal(ex.Message);
				_log.Debug(ex.ToString());
			}
			try
			{
				int millisecondsTimeout = 5000;
				stopReceiving(millisecondsTimeout);
			}
			catch (Exception ex2)
			{
				_log.Fatal(ex2.Message);
				_log.Debug(ex2.ToString());
			}
			_state = ServerState.Stop;
		}

		private void stopReceiving(int millisecondsTimeout)
		{
			_listener.Stop();
			_receiveThread.Join(millisecondsTimeout);
		}

		private static bool tryCreateUri(string uriString, out Uri result, out string message)
		{
			result = null;
			message = null;
			Uri uri = uriString.ToUri();
			if (uri == null)
			{
				message = "An invalid URI string.";
				return false;
			}
			if (!uri.IsAbsoluteUri)
			{
				message = "A relative URI.";
				return false;
			}
			string scheme = uri.Scheme;
			if (!(scheme == "http") && !(scheme == "https"))
			{
				message = "The scheme part is not 'http' or 'https'.";
				return false;
			}
			if (uri.PathAndQuery != "/")
			{
				message = "It includes either or both path and query components.";
				return false;
			}
			if (uri.Fragment.Length > 0)
			{
				message = "It includes the fragment component.";
				return false;
			}
			if (uri.Port == 0)
			{
				message = "The port part is zero.";
				return false;
			}
			result = uri;
			return true;
		}

		public void AddWebSocketService<TBehavior>(string path) where TBehavior : WebSocketBehavior, new()
		{
			_services.AddService<TBehavior>(path, null);
		}

		public void AddWebSocketService<TBehavior>(string path, Action<TBehavior> initializer) where TBehavior : WebSocketBehavior, new()
		{
			_services.AddService(path, initializer);
		}

		public bool RemoveWebSocketService(string path)
		{
			return _services.RemoveService(path);
		}

		public void Start()
		{
			if (_state != ServerState.Start && _state != ServerState.ShuttingDown)
			{
				start();
			}
		}

		public void Stop()
		{
			if (_state == ServerState.Start)
			{
				stop(1001, string.Empty);
			}
		}
	}
}
