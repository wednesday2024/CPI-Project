#if UNITY_WEBGL
#else
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;

namespace Sfs2X.WebSocketSharp.Net
{
	public sealed class HttpListener : IDisposable
	{
		private AuthenticationSchemes _authSchemes;

		private Func<HttpListenerRequest, AuthenticationSchemes> _authSchemeSelector;

		private string _certFolderPath;

		private Queue<HttpListenerContext> _contextQueue;

		private LinkedList<HttpListenerContext> _contextRegistry;

		private object _contextRegistrySync;

		private static readonly string _defaultRealm;

		private bool _disposed;

		private bool _ignoreWriteExceptions;

		private volatile bool _listening;

		private Logger _log;

		private string _objectName;

		private HttpListenerPrefixCollection _prefixes;

		private string _realm;

		private bool _reuseAddress;

		private ServerSslConfiguration _sslConfig;

		private object _sync;

		private Func<IIdentity, NetworkCredential> _userCredFinder;

		private Queue<HttpListenerAsyncResult> _waitQueue;

		internal bool ReuseAddress
		{
			get
			{
				return _reuseAddress;
			}
			set
			{
				_reuseAddress = value;
			}
		}

		public AuthenticationSchemes AuthenticationSchemes
		{
			get
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				return _authSchemes;
			}
			set
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				_authSchemes = value;
			}
		}

		public Func<HttpListenerRequest, AuthenticationSchemes> AuthenticationSchemeSelector
		{
			get
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				return _authSchemeSelector;
			}
			set
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				_authSchemeSelector = value;
			}
		}

		public string CertificateFolderPath
		{
			get
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				return _certFolderPath;
			}
			set
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				_certFolderPath = value;
			}
		}

		public bool IgnoreWriteExceptions
		{
			get
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				return _ignoreWriteExceptions;
			}
			set
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				_ignoreWriteExceptions = value;
			}
		}

		public bool IsListening => _listening;

		public static bool IsSupported => true;

		public Logger Log => _log;

		public HttpListenerPrefixCollection Prefixes
		{
			get
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				return _prefixes;
			}
		}

		public string Realm
		{
			get
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				return _realm;
			}
			set
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				_realm = value;
			}
		}

		public ServerSslConfiguration SslConfiguration
		{
			get
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				if (_sslConfig == null)
				{
					_sslConfig = new ServerSslConfiguration();
				}
				return _sslConfig;
			}
		}

		public bool UnsafeConnectionNtlmAuthentication
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public Func<IIdentity, NetworkCredential> UserCredentialsFinder
		{
			get
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				return _userCredFinder;
			}
			set
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				_userCredFinder = value;
			}
		}

		static HttpListener()
		{
			_defaultRealm = "SECRET AREA";
		}

		public HttpListener()
		{
			_authSchemes = AuthenticationSchemes.Anonymous;
			_contextQueue = new Queue<HttpListenerContext>();
			_contextRegistry = new LinkedList<HttpListenerContext>();
			_contextRegistrySync = ((ICollection)_contextRegistry).SyncRoot;
			_log = new Logger();
			_objectName = GetType().ToString();
			_prefixes = new HttpListenerPrefixCollection(this);
			_sync = new object();
			_waitQueue = new Queue<HttpListenerAsyncResult>();
		}

		private bool authenticateClient(HttpListenerContext context)
		{
			AuthenticationSchemes authenticationSchemes = selectAuthenticationScheme(context.Request);
			switch (authenticationSchemes)
			{
			case AuthenticationSchemes.Anonymous:
				return true;
			case AuthenticationSchemes.None:
			{
				string message = "Authentication not allowed";
				context.SendError(403, message);
				return false;
			}
			default:
			{
				string realm = getRealm();
				if (!context.SetUser(authenticationSchemes, realm, _userCredFinder))
				{
					context.SendAuthenticationChallenge(authenticationSchemes, realm);
					return false;
				}
				return true;
			}
			}
		}

		private HttpListenerAsyncResult beginGetContext(AsyncCallback callback, object state)
		{
			lock (_contextRegistrySync)
			{
				if (!_listening)
				{
					string message = "The method is canceled.";
					throw new HttpListenerException(995, message);
				}
				HttpListenerAsyncResult httpListenerAsyncResult = new HttpListenerAsyncResult(callback, state);
				if (_contextQueue.Count == 0)
				{
					_waitQueue.Enqueue(httpListenerAsyncResult);
					return httpListenerAsyncResult;
				}
				HttpListenerContext context = _contextQueue.Dequeue();
				httpListenerAsyncResult.Complete(context, completedSynchronously: true);
				return httpListenerAsyncResult;
			}
		}

		private void cleanupContextQueue(bool force)
		{
			if (_contextQueue.Count == 0)
			{
				return;
			}
			if (force)
			{
				_contextQueue.Clear();
				return;
			}
			HttpListenerContext[] array = _contextQueue.ToArray();
			_contextQueue.Clear();
			HttpListenerContext[] array2 = array;
			foreach (HttpListenerContext httpListenerContext in array2)
			{
				httpListenerContext.SendError(503);
			}
		}

		private void cleanupContextRegistry()
		{
			int count = _contextRegistry.Count;
			if (count != 0)
			{
				HttpListenerContext[] array = new HttpListenerContext[count];
				lock (_contextRegistrySync)
				{
					_contextRegistry.CopyTo(array, 0);
					_contextRegistry.Clear();
				}
				HttpListenerContext[] array2 = array;
				foreach (HttpListenerContext httpListenerContext in array2)
				{
					httpListenerContext.Connection.Close(force: true);
				}
			}
		}

		private void cleanupWaitQueue(string message)
		{
			if (_waitQueue.Count != 0)
			{
				HttpListenerAsyncResult[] array = _waitQueue.ToArray();
				_waitQueue.Clear();
				HttpListenerAsyncResult[] array2 = array;
				foreach (HttpListenerAsyncResult httpListenerAsyncResult in array2)
				{
					HttpListenerException exception = new HttpListenerException(995, message);
					httpListenerAsyncResult.Complete(exception);
				}
			}
		}

		private void close(bool force)
		{
			lock (_sync)
			{
				if (_disposed)
				{
					return;
				}
				lock (_contextRegistrySync)
				{
					if (!_listening)
					{
						_disposed = true;
						return;
					}
					_listening = false;
				}
				cleanupContextQueue(force);
				cleanupContextRegistry();
				string message = "The listener is closed.";
				cleanupWaitQueue(message);
				EndPointManager.RemoveListener(this);
				_disposed = true;
			}
		}

		private string getRealm()
		{
			string realm = _realm;
			return (realm != null && realm.Length > 0) ? realm : _defaultRealm;
		}

		private bool registerContext(HttpListenerContext context)
		{
			if (!_listening)
			{
				return false;
			}
			lock (_contextRegistrySync)
			{
				if (!_listening)
				{
					return false;
				}
				context.Listener = this;
				_contextRegistry.AddLast(context);
				if (_waitQueue.Count == 0)
				{
					_contextQueue.Enqueue(context);
					return true;
				}
				HttpListenerAsyncResult httpListenerAsyncResult = _waitQueue.Dequeue();
				httpListenerAsyncResult.Complete(context, completedSynchronously: false);
				return true;
			}
		}

		private AuthenticationSchemes selectAuthenticationScheme(HttpListenerRequest request)
		{
			Func<HttpListenerRequest, AuthenticationSchemes> authSchemeSelector = _authSchemeSelector;
			if (authSchemeSelector == null)
			{
				return _authSchemes;
			}
			try
			{
				return authSchemeSelector(request);
			}
			catch
			{
				return AuthenticationSchemes.None;
			}
		}

		internal void CheckDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(_objectName);
			}
		}

		internal bool RegisterContext(HttpListenerContext context)
		{
			if (!authenticateClient(context))
			{
				return false;
			}
			if (!registerContext(context))
			{
				context.SendError(503);
				return false;
			}
			return true;
		}

		internal void UnregisterContext(HttpListenerContext context)
		{
			lock (_contextRegistrySync)
			{
				_contextRegistry.Remove(context);
			}
		}

		public void Abort()
		{
			if (!_disposed)
			{
				close(force: true);
			}
		}

		public IAsyncResult BeginGetContext(AsyncCallback callback, object state)
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(_objectName);
			}
			if (!_listening)
			{
				string message = "The listener has not been started.";
				throw new InvalidOperationException(message);
			}
			if (_prefixes.Count == 0)
			{
				string message2 = "The listener has no URI prefix on which listens.";
				throw new InvalidOperationException(message2);
			}
			return beginGetContext(callback, state);
		}

		public void Close()
		{
			if (!_disposed)
			{
				close(force: false);
			}
		}

		public HttpListenerContext EndGetContext(IAsyncResult asyncResult)
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(_objectName);
			}
			if (!_listening)
			{
				string message = "The listener has not been started.";
				throw new InvalidOperationException(message);
			}
			if (asyncResult == null)
			{
				throw new ArgumentNullException("asyncResult");
			}
			if (!(asyncResult is HttpListenerAsyncResult { SyncRoot: var syncRoot } httpListenerAsyncResult))
			{
				string message2 = "A wrong IAsyncResult instance.";
				throw new ArgumentException(message2, "asyncResult");
			}
			bool lockTaken = false;
			try
			{
				Monitor.Enter(syncRoot, ref lockTaken);
				if (httpListenerAsyncResult.EndCalled)
				{
					string message3 = "This IAsyncResult instance cannot be reused.";
					throw new InvalidOperationException(message3);
				}
				httpListenerAsyncResult.EndCalled = true;
			}
			finally
			{
				if (lockTaken)
				{
					Monitor.Exit(syncRoot);
				}
			}
			if (!httpListenerAsyncResult.IsCompleted)
			{
				httpListenerAsyncResult.AsyncWaitHandle.WaitOne();
			}
			return httpListenerAsyncResult.Context;
		}

		public HttpListenerContext GetContext()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(_objectName);
			}
			if (!_listening)
			{
				string message = "The listener has not been started.";
				throw new InvalidOperationException(message);
			}
			if (_prefixes.Count == 0)
			{
				string message2 = "The listener has no URI prefix on which listens.";
				throw new InvalidOperationException(message2);
			}
			HttpListenerAsyncResult httpListenerAsyncResult = beginGetContext(null, null);
			httpListenerAsyncResult.EndCalled = true;
			if (!httpListenerAsyncResult.IsCompleted)
			{
				httpListenerAsyncResult.AsyncWaitHandle.WaitOne();
			}
			return httpListenerAsyncResult.Context;
		}

		public void Start()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(_objectName);
			}
			lock (_sync)
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				lock (_contextRegistrySync)
				{
					if (!_listening)
					{
						EndPointManager.AddListener(this);
						_listening = true;
					}
				}
			}
		}

		public void Stop()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(_objectName);
			}
			lock (_sync)
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(_objectName);
				}
				lock (_contextRegistrySync)
				{
					if (!_listening)
					{
						return;
					}
					_listening = false;
				}
				cleanupContextQueue(force: false);
				cleanupContextRegistry();
				string message = "The listener is stopped.";
				cleanupWaitQueue(message);
				EndPointManager.RemoveListener(this);
			}
		}

		void IDisposable.Dispose()
		{
			if (!_disposed)
			{
				close(force: true);
			}
		}
	}
}
#endif