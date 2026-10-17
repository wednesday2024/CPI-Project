#if UNITY_WEBGL
#else
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Sfs2X.WebSocketSharp.Net;
using Sfs2X.WebSocketSharp.Net.WebSockets;

namespace Sfs2X.WebSocketSharp.Server
{
	public class WebSocketServer
	{
		private IPAddress _address;

		private Sfs2X.WebSocketSharp.Net.AuthenticationSchemes _authSchemes;

		private static readonly string _defaultRealm;

		private bool _dnsStyle;

		private string _hostname;

		private TcpListener _listener;

		private Logger _log;

		private int _port;

		private string _realm;

		private string _realmInUse;

		private Thread _receiveThread;

		private bool _reuseAddress;

		private bool _secure;

		private WebSocketServiceManager _services;

		private ServerSslConfiguration _sslConfig;

		private ServerSslConfiguration _sslConfigInUse;

		private volatile ServerState _state;

		private object _sync;

		private Func<IIdentity, Sfs2X.WebSocketSharp.Net.NetworkCredential> _userCredFinder;

		public IPAddress Address => _address;

		public Sfs2X.WebSocketSharp.Net.AuthenticationSchemes AuthenticationSchemes
		{
			get
			{
				return _authSchemes;
			}
			set
			{
				lock (_sync)
				{
					if (canSet())
					{
						_authSchemes = value;
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
				return _realm;
			}
			set
			{
				lock (_sync)
				{
					if (canSet())
					{
						_realm = value;
					}
				}
			}
		}

		public bool ReuseAddress
		{
			get
			{
				return _reuseAddress;
			}
			set
			{
				lock (_sync)
				{
					if (canSet())
					{
						_reuseAddress = value;
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
				return getSslConfiguration();
			}
		}

		public Func<IIdentity, Sfs2X.WebSocketSharp.Net.NetworkCredential> UserCredentialsFinder
		{
			get
			{
				return _userCredFinder;
			}
			set
			{
				lock (_sync)
				{
					if (canSet())
					{
						_userCredFinder = value;
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

		static WebSocketServer()
		{
			_defaultRealm = "SECRET AREA";
		}

		public WebSocketServer()
		{
			IPAddress any = IPAddress.Any;
			init(any.ToString(), any, 80, secure: false);
		}

		public WebSocketServer(int port)
			: this(port, port == 443)
		{
		}

		public WebSocketServer(string url)
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
			string dnsSafeHost = result.DnsSafeHost;
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
			init(dnsSafeHost, iPAddress, result.Port, result.Scheme == "wss");
		}

		public WebSocketServer(int port, bool secure)
		{
			if (!port.IsPortNumber())
			{
				string message = "Less than 1 or greater than 65535.";
				throw new ArgumentOutOfRangeException("port", message);
			}
			IPAddress any = IPAddress.Any;
			init(any.ToString(), any, port, secure);
		}

		public WebSocketServer(IPAddress address, int port)
			: this(address, port, port == 443)
		{
		}

		public WebSocketServer(IPAddress address, int port, bool secure)
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
			init(address.ToString(), address, port, secure);
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
				_listener.Stop();
			}
			catch (Exception ex)
			{
				_log.Fatal(ex.Message);
				_log.Debug(ex.ToString());
			}
			try
			{
				_services.Stop(1006, string.Empty);
			}
			catch (Exception ex2)
			{
				_log.Fatal(ex2.Message);
				_log.Debug(ex2.ToString());
			}
			_state = ServerState.Stop;
		}

		private bool authenticateClient(TcpListenerWebSocketContext context)
		{
			if (_authSchemes == Sfs2X.WebSocketSharp.Net.AuthenticationSchemes.Anonymous)
			{
				return true;
			}
			if (_authSchemes == Sfs2X.WebSocketSharp.Net.AuthenticationSchemes.None)
			{
				return false;
			}
			string chal = new AuthenticationChallenge(_authSchemes, _realmInUse).ToString();
			int retry = -1;
			Func<bool> auth = null;
			auth = () =>
			{
				retry++;
				if (retry > 99)
				{
					return false;
				}
				if (context.SetUser(_authSchemes, _realmInUse, _userCredFinder))
				{
					return true;
				}
				context.SendAuthenticationChallenge(chal);
				return auth();
			};
			return auth();
		}

		private bool canSet()
		{
			return _state == ServerState.Ready || _state == ServerState.Stop;
		}

		private bool checkHostNameForRequest(string name)
		{
			return !_dnsStyle || Uri.CheckHostName(name) != UriHostNameType.Dns || name == _hostname;
		}

		private string getRealm()
		{
			string realm = _realm;
			return (realm != null && realm.Length > 0) ? realm : _defaultRealm;
		}

		private ServerSslConfiguration getSslConfiguration()
		{
			if (_sslConfig == null)
			{
				_sslConfig = new ServerSslConfiguration();
			}
			return _sslConfig;
		}

		private void init(string hostname, IPAddress address, int port, bool secure)
		{
			_hostname = hostname;
			_address = address;
			_port = port;
			_secure = secure;
			_authSchemes = Sfs2X.WebSocketSharp.Net.AuthenticationSchemes.Anonymous;
			_dnsStyle = Uri.CheckHostName(hostname) == UriHostNameType.Dns;
			_listener = new TcpListener(address, port);
			_log = new Logger();
			_services = new WebSocketServiceManager(_log);
			_sync = new object();
		}

		private void processRequest(TcpListenerWebSocketContext context)
		{
			if (!authenticateClient(context))
			{
				context.Close(Sfs2X.WebSocketSharp.Net.HttpStatusCode.Forbidden);
				return;
			}
			Uri requestUri = context.RequestUri;
			if (requestUri == null)
			{
				context.Close(Sfs2X.WebSocketSharp.Net.HttpStatusCode.BadRequest);
				return;
			}
			string dnsSafeHost = requestUri.DnsSafeHost;
			if (!checkHostNameForRequest(dnsSafeHost))
			{
				context.Close(Sfs2X.WebSocketSharp.Net.HttpStatusCode.NotFound);
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
				TcpClient cl = null;
				try
				{
					cl = _listener.AcceptTcpClient();
					ThreadPool.QueueUserWorkItem((object state) =>
					{
						try
						{
							TcpListenerWebSocketContext context = new TcpListenerWebSocketContext(cl, null, _secure, _sslConfigInUse, _log);
							processRequest(context);
						}
						catch (Exception ex4)
						{
							_log.Error(ex4.Message);
							_log.Debug(ex4.ToString());
							cl.Close();
						}
					});
				}
				catch (SocketException ex)
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
					if (cl != null)
					{
						cl.Close();
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
				if (_state == ServerState.Start || _state == ServerState.ShuttingDown)
				{
					return;
				}
				if (_secure)
				{
					ServerSslConfiguration sslConfiguration = getSslConfiguration();
					ServerSslConfiguration serverSslConfiguration = new ServerSslConfiguration(sslConfiguration);
					if (serverSslConfiguration.ServerCertificate == null)
					{
						string message = "There is no server certificate for secure connection.";
						throw new InvalidOperationException(message);
					}
					_sslConfigInUse = serverSslConfiguration;
				}
				_realmInUse = getRealm();
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

		private void startReceiving()
		{
			if (_reuseAddress)
			{
				_listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
			}
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
				int millisecondsTimeout = 5000;
				stopReceiving(millisecondsTimeout);
			}
			catch (Exception ex)
			{
				_log.Fatal(ex.Message);
				_log.Debug(ex.ToString());
			}
			try
			{
				_services.Stop(code, reason);
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
			if (!uriString.TryCreateWebSocketUri(out result, out message))
			{
				return false;
			}
			if (result.PathAndQuery != "/")
			{
				result = null;
				message = "It includes either or both path and query components.";
				return false;
			}
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
#endif