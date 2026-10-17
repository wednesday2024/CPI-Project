#if UNITY_WEBGL
#else
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Sfs2X.WebSocketSharp.Net
{
	internal sealed class EndPointListener
	{
		private List<HttpListenerPrefix> _all;

		private Dictionary<HttpConnection, HttpConnection> _connections;

		private object _connectionsSync;

		private static readonly string _defaultCertFolderPath;

		private IPEndPoint _endpoint;

		private List<HttpListenerPrefix> _prefixes;

		private bool _secure;

		private Socket _socket;

		private ServerSslConfiguration _sslConfig;

		private List<HttpListenerPrefix> _unhandled;

		public IPAddress Address => _endpoint.Address;

		public bool IsSecure => _secure;

		public int Port => _endpoint.Port;

		public ServerSslConfiguration SslConfiguration => _sslConfig;

		static EndPointListener()
		{
			_defaultCertFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		}

		internal EndPointListener(IPEndPoint endpoint, bool secure, string certificateFolderPath, ServerSslConfiguration sslConfig, bool reuseAddress)
		{
			_endpoint = endpoint;
			if (secure)
			{
				X509Certificate2 certificate = getCertificate(endpoint.Port, certificateFolderPath, sslConfig.ServerCertificate);
				if (certificate == null)
				{
					string message = "No server certificate could be found.";
					throw new ArgumentException(message);
				}
				_secure = true;
				_sslConfig = new ServerSslConfiguration(sslConfig);
				_sslConfig.ServerCertificate = certificate;
			}
			_prefixes = new List<HttpListenerPrefix>();
			_connections = new Dictionary<HttpConnection, HttpConnection>();
			_connectionsSync = ((ICollection)_connections).SyncRoot;
			_socket = new Socket(endpoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			if (reuseAddress)
			{
				_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
			}
			_socket.Bind(endpoint);
			_socket.Listen(500);
			_socket.BeginAccept(onAccept, this);
		}

		private static void addSpecial(List<HttpListenerPrefix> prefixes, HttpListenerPrefix prefix)
		{
			string path = prefix.Path;
			foreach (HttpListenerPrefix prefix2 in prefixes)
			{
				if (prefix2.Path == path)
				{
					string message = "The prefix is already in use.";
					throw new HttpListenerException(87, message);
				}
			}
			prefixes.Add(prefix);
		}

		private void clearConnections()
		{
			HttpConnection[] array = null;
			lock (_connectionsSync)
			{
				int count = _connections.Count;
				if (count == 0)
				{
					return;
				}
				array = new HttpConnection[count];
				Dictionary<HttpConnection, HttpConnection>.ValueCollection values = _connections.Values;
				values.CopyTo(array, 0);
				_connections.Clear();
			}
			HttpConnection[] array2 = array;
			foreach (HttpConnection httpConnection in array2)
			{
				httpConnection.Close(force: true);
			}
		}

		private static RSACryptoServiceProvider createRSAFromFile(string path)
		{
			RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			byte[] keyBlob = File.ReadAllBytes(path);
			rSACryptoServiceProvider.ImportCspBlob(keyBlob);
			return rSACryptoServiceProvider;
		}

		private static X509Certificate2 getCertificate(int port, string folderPath, X509Certificate2 defaultCertificate)
		{
			if (folderPath == null || folderPath.Length == 0)
			{
				folderPath = _defaultCertFolderPath;
			}
			try
			{
				string text = Path.Combine(folderPath, $"{port}.cer");
				string path = Path.Combine(folderPath, $"{port}.key");
				if (File.Exists(text) && File.Exists(path))
				{
					X509Certificate2 x509Certificate = new X509Certificate2(text);
					x509Certificate.PrivateKey = createRSAFromFile(path);
					return x509Certificate;
				}
			}
			catch
			{
			}
			return defaultCertificate;
		}

		private void leaveIfNoPrefix()
		{
			if (_prefixes.Count > 0)
			{
				return;
			}
			List<HttpListenerPrefix> unhandled = _unhandled;
			if (unhandled == null || unhandled.Count <= 0)
			{
				unhandled = _all;
				if (unhandled == null || unhandled.Count <= 0)
				{
					Close();
				}
			}
		}

		private static void onAccept(IAsyncResult asyncResult)
		{
			EndPointListener endPointListener = (EndPointListener)asyncResult.AsyncState;
			Socket socket = null;
			try
			{
				socket = endPointListener._socket.EndAccept(asyncResult);
			}
			catch (ObjectDisposedException)
			{
				return;
			}
			catch (Exception)
			{
			}
			try
			{
				endPointListener._socket.BeginAccept(onAccept, endPointListener);
			}
			catch (Exception)
			{
				socket?.Close();
				return;
			}
			if (socket != null)
			{
				processAccepted(socket, endPointListener);
			}
		}

		private static void processAccepted(Socket socket, EndPointListener listener)
		{
			HttpConnection httpConnection = null;
			try
			{
				httpConnection = new HttpConnection(socket, listener);
			}
			catch (Exception)
			{
				socket.Close();
				return;
			}
			lock (listener._connectionsSync)
			{
				listener._connections.Add(httpConnection, httpConnection);
			}
			httpConnection.BeginReadRequest();
		}

		private static bool removeSpecial(List<HttpListenerPrefix> prefixes, HttpListenerPrefix prefix)
		{
			string path = prefix.Path;
			int count = prefixes.Count;
			for (int i = 0; i < count; i++)
			{
				if (prefixes[i].Path == path)
				{
					prefixes.RemoveAt(i);
					return true;
				}
			}
			return false;
		}

		private static HttpListener searchHttpListenerFromSpecial(string path, List<HttpListenerPrefix> prefixes)
		{
			if (prefixes == null)
			{
				return null;
			}
			HttpListener result = null;
			int num = -1;
			foreach (HttpListenerPrefix prefix in prefixes)
			{
				string path2 = prefix.Path;
				int length = path2.Length;
				if (length >= num && path.StartsWith(path2, StringComparison.Ordinal))
				{
					num = length;
					result = prefix.Listener;
				}
			}
			return result;
		}

		internal static bool CertificateExists(int port, string folderPath)
		{
			if (folderPath == null || folderPath.Length == 0)
			{
				folderPath = _defaultCertFolderPath;
			}
			string path = Path.Combine(folderPath, $"{port}.cer");
			string path2 = Path.Combine(folderPath, $"{port}.key");
			return File.Exists(path) && File.Exists(path2);
		}

		internal void RemoveConnection(HttpConnection connection)
		{
			lock (_connectionsSync)
			{
				_connections.Remove(connection);
			}
		}

		internal bool TrySearchHttpListener(Uri uri, out HttpListener listener)
		{
			listener = null;
			if (uri == null)
			{
				return false;
			}
			string host = uri.Host;
			bool flag = Uri.CheckHostName(host) == UriHostNameType.Dns;
			string text = uri.Port.ToString();
			string text2 = HttpUtility.UrlDecode(uri.AbsolutePath);
			if (text2[text2.Length - 1] != '/')
			{
				text2 += "/";
			}
			if (host != null && host.Length > 0)
			{
				List<HttpListenerPrefix> prefixes = _prefixes;
				int num = -1;
				foreach (HttpListenerPrefix item in prefixes)
				{
					if (flag)
					{
						string host2 = item.Host;
						if (Uri.CheckHostName(host2) == UriHostNameType.Dns && host2 != host)
						{
							continue;
						}
					}
					if (!(item.Port != text))
					{
						string path = item.Path;
						int length = path.Length;
						if (length >= num && text2.StartsWith(path, StringComparison.Ordinal))
						{
							num = length;
							listener = item.Listener;
						}
					}
				}
				if (num != -1)
				{
					return true;
				}
			}
			listener = searchHttpListenerFromSpecial(text2, _unhandled);
			if (listener != null)
			{
				return true;
			}
			listener = searchHttpListenerFromSpecial(text2, _all);
			return listener != null;
		}

		public void AddPrefix(HttpListenerPrefix prefix)
		{
			List<HttpListenerPrefix> unhandled;
			List<HttpListenerPrefix> list;
			if (prefix.Host == "*")
			{
				do
				{
					unhandled = _unhandled;
					list = ((unhandled != null) ? new List<HttpListenerPrefix>(unhandled) : new List<HttpListenerPrefix>());
					addSpecial(list, prefix);
				}
				while (Interlocked.CompareExchange(ref _unhandled, list, unhandled) != unhandled);
				return;
			}
			if (prefix.Host == "+")
			{
				do
				{
					unhandled = _all;
					list = ((unhandled != null) ? new List<HttpListenerPrefix>(unhandled) : new List<HttpListenerPrefix>());
					addSpecial(list, prefix);
				}
				while (Interlocked.CompareExchange(ref _all, list, unhandled) != unhandled);
				return;
			}
			do
			{
				unhandled = _prefixes;
				int num = unhandled.IndexOf(prefix);
				if (num > -1)
				{
					if (unhandled[num].Listener != prefix.Listener)
					{
						string message = $"There is another listener for {prefix}.";
						throw new HttpListenerException(87, message);
					}
					break;
				}
				list = new List<HttpListenerPrefix>(unhandled);
				list.Add(prefix);
			}
			while (Interlocked.CompareExchange(ref _prefixes, list, unhandled) != unhandled);
		}

		public void Close()
		{
			_socket.Close();
			clearConnections();
			EndPointManager.RemoveEndPoint(_endpoint);
		}

		public void RemovePrefix(HttpListenerPrefix prefix)
		{
			List<HttpListenerPrefix> unhandled;
			List<HttpListenerPrefix> list;
			if (prefix.Host == "*")
			{
				do
				{
					unhandled = _unhandled;
					if (unhandled == null)
					{
						break;
					}
					list = new List<HttpListenerPrefix>(unhandled);
				}
				while (removeSpecial(list, prefix) && Interlocked.CompareExchange(ref _unhandled, list, unhandled) != unhandled);
				leaveIfNoPrefix();
				return;
			}
			if (prefix.Host == "+")
			{
				do
				{
					unhandled = _all;
					if (unhandled == null)
					{
						break;
					}
					list = new List<HttpListenerPrefix>(unhandled);
				}
				while (removeSpecial(list, prefix) && Interlocked.CompareExchange(ref _all, list, unhandled) != unhandled);
				leaveIfNoPrefix();
				return;
			}
			do
			{
				unhandled = _prefixes;
				if (!unhandled.Contains(prefix))
				{
					break;
				}
				list = new List<HttpListenerPrefix>(unhandled);
				list.Remove(prefix);
			}
			while (Interlocked.CompareExchange(ref _prefixes, list, unhandled) != unhandled);
			leaveIfNoPrefix();
		}
	}
}
#endif