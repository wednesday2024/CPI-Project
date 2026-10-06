using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Sfs2X.WebSocketSharp.Net
{
	internal sealed class EndPointManager
	{
		private static readonly Dictionary<IPEndPoint, EndPointListener> _endpoints;

		static EndPointManager()
		{
			_endpoints = new Dictionary<IPEndPoint, EndPointListener>();
		}

		private EndPointManager()
		{
		}

		private static void addPrefix(string uriPrefix, HttpListener listener)
		{
			HttpListenerPrefix httpListenerPrefix = new HttpListenerPrefix(uriPrefix, listener);
			IPAddress iPAddress = convertToIPAddress(httpListenerPrefix.Host);
			if (iPAddress == null)
			{
				string message = "The URI prefix includes an invalid host.";
				throw new HttpListenerException(87, message);
			}
			if (!iPAddress.IsLocal())
			{
				string message2 = "The URI prefix includes an invalid host.";
				throw new HttpListenerException(87, message2);
			}
			if (!int.TryParse(httpListenerPrefix.Port, out var result))
			{
				string message3 = "The URI prefix includes an invalid port.";
				throw new HttpListenerException(87, message3);
			}
			if (!result.IsPortNumber())
			{
				string message4 = "The URI prefix includes an invalid port.";
				throw new HttpListenerException(87, message4);
			}
			string path = httpListenerPrefix.Path;
			if (path.IndexOf('%') != -1)
			{
				string message5 = "The URI prefix includes an invalid path.";
				throw new HttpListenerException(87, message5);
			}
			if (path.IndexOf("//", StringComparison.Ordinal) != -1)
			{
				string message6 = "The URI prefix includes an invalid path.";
				throw new HttpListenerException(87, message6);
			}
			IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, result);
			if (_endpoints.TryGetValue(iPEndPoint, out var value))
			{
				if (value.IsSecure ^ httpListenerPrefix.IsSecure)
				{
					string message7 = "The URI prefix includes an invalid scheme.";
					throw new HttpListenerException(87, message7);
				}
			}
			else
			{
				value = new EndPointListener(iPEndPoint, httpListenerPrefix.IsSecure, listener.CertificateFolderPath, listener.SslConfiguration, listener.ReuseAddress);
				_endpoints.Add(iPEndPoint, value);
			}
			value.AddPrefix(httpListenerPrefix);
		}

		private static IPAddress convertToIPAddress(string hostname)
		{
			if (hostname == "*")
			{
				return IPAddress.Any;
			}
			if (hostname == "+")
			{
				return IPAddress.Any;
			}
			return hostname.ToIPAddress();
		}

		private static void removePrefix(string uriPrefix, HttpListener listener)
		{
			HttpListenerPrefix httpListenerPrefix = new HttpListenerPrefix(uriPrefix, listener);
			IPAddress iPAddress = convertToIPAddress(httpListenerPrefix.Host);
			if (iPAddress == null || !iPAddress.IsLocal() || !int.TryParse(httpListenerPrefix.Port, out var result) || !result.IsPortNumber())
			{
				return;
			}
			string path = httpListenerPrefix.Path;
			if (path.IndexOf('%') == -1 && path.IndexOf("//", StringComparison.Ordinal) == -1)
			{
				IPEndPoint key = new IPEndPoint(iPAddress, result);
				if (_endpoints.TryGetValue(key, out var value) && !(value.IsSecure ^ httpListenerPrefix.IsSecure))
				{
					value.RemovePrefix(httpListenerPrefix);
				}
			}
		}

		internal static bool RemoveEndPoint(IPEndPoint endpoint)
		{
			lock (((ICollection)_endpoints).SyncRoot)
			{
				return _endpoints.Remove(endpoint);
			}
		}

		public static void AddListener(HttpListener listener)
		{
			List<string> list = new List<string>();
			lock (((ICollection)_endpoints).SyncRoot)
			{
				try
				{
					foreach (string prefix in listener.Prefixes)
					{
						addPrefix(prefix, listener);
						list.Add(prefix);
					}
				}
				catch
				{
					foreach (string item in list)
					{
						removePrefix(item, listener);
					}
					throw;
				}
			}
		}

		public static void AddPrefix(string uriPrefix, HttpListener listener)
		{
			lock (((ICollection)_endpoints).SyncRoot)
			{
				addPrefix(uriPrefix, listener);
			}
		}

		public static void RemoveListener(HttpListener listener)
		{
			lock (((ICollection)_endpoints).SyncRoot)
			{
				foreach (string prefix in listener.Prefixes)
				{
					removePrefix(prefix, listener);
				}
			}
		}

		public static void RemovePrefix(string uriPrefix, HttpListener listener)
		{
			lock (((ICollection)_endpoints).SyncRoot)
			{
				removePrefix(uriPrefix, listener);
			}
		}
	}
}
