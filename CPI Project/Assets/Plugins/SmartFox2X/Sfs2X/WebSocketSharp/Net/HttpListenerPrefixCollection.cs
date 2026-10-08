#if UNITY_WEBGL
#else
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sfs2X.WebSocketSharp.Net
{
	public class HttpListenerPrefixCollection : ICollection<string>, IEnumerable<string>, IEnumerable
	{
		private HttpListener _listener;

		private List<string> _prefixes;

		public int Count => _prefixes.Count;

		public bool IsReadOnly => false;

		public bool IsSynchronized => false;

		internal HttpListenerPrefixCollection(HttpListener listener)
		{
			_listener = listener;
			_prefixes = new List<string>();
		}

		public void Add(string uriPrefix)
		{
			_listener.CheckDisposed();
			HttpListenerPrefix.CheckPrefix(uriPrefix);
			if (!_prefixes.Contains(uriPrefix))
			{
				if (_listener.IsListening)
				{
					EndPointManager.AddPrefix(uriPrefix, _listener);
				}
				_prefixes.Add(uriPrefix);
			}
		}

		public void Clear()
		{
			_listener.CheckDisposed();
			if (_listener.IsListening)
			{
				EndPointManager.RemoveListener(_listener);
			}
			_prefixes.Clear();
		}

		public bool Contains(string uriPrefix)
		{
			_listener.CheckDisposed();
			if (uriPrefix == null)
			{
				throw new ArgumentNullException("uriPrefix");
			}
			return _prefixes.Contains(uriPrefix);
		}

		public void CopyTo(string[] array, int offset)
		{
			_listener.CheckDisposed();
			_prefixes.CopyTo(array, offset);
		}

		public IEnumerator<string> GetEnumerator()
		{
			return _prefixes.GetEnumerator();
		}

		public bool Remove(string uriPrefix)
		{
			_listener.CheckDisposed();
			if (uriPrefix == null)
			{
				throw new ArgumentNullException("uriPrefix");
			}
			if (!_prefixes.Contains(uriPrefix))
			{
				return false;
			}
			if (_listener.IsListening)
			{
				EndPointManager.RemovePrefix(uriPrefix, _listener);
			}
			return _prefixes.Remove(uriPrefix);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _prefixes.GetEnumerator();
		}
	}
}
#endif