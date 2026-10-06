using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;

namespace Sfs2X.WebSocketSharp.Server
{
	public class WebSocketSessionManager
	{
		private object _forSweep;

		private volatile bool _keepClean;

		private Logger _log;

		private static readonly byte[] _rawEmptyPingFrame;

		private Dictionary<string, IWebSocketSession> _sessions;

		private volatile ServerState _state;

		private volatile bool _sweeping;

		private System.Timers.Timer _sweepTimer;

		private object _sync;

		private TimeSpan _waitTime;

		internal ServerState State => _state;

		public IEnumerable<string> ActiveIDs
		{
			get
			{
				foreach (KeyValuePair<string, bool> res in broadping(_rawEmptyPingFrame))
				{
					if (res.Value)
					{
						yield return res.Key;
					}
				}
			}
		}

		public int Count
		{
			get
			{
				lock (_sync)
				{
					return _sessions.Count;
				}
			}
		}

		public IEnumerable<string> IDs
		{
			get
			{
				if (_state != ServerState.Start)
				{
					return Enumerable.Empty<string>();
				}
				lock (_sync)
				{
					if (_state != ServerState.Start)
					{
						return Enumerable.Empty<string>();
					}
					return _sessions.Keys.ToList();
				}
			}
		}

		public IEnumerable<string> InactiveIDs
		{
			get
			{
				foreach (KeyValuePair<string, bool> res in broadping(_rawEmptyPingFrame))
				{
					if (!res.Value)
					{
						yield return res.Key;
					}
				}
			}
		}

		public IWebSocketSession this[string id]
		{
			get
			{
				if (id == null)
				{
					throw new ArgumentNullException("id");
				}
				if (id.Length == 0)
				{
					throw new ArgumentException("An empty string.", "id");
				}
				tryGetSession(id, out var session);
				return session;
			}
		}

		public bool KeepClean
		{
			get
			{
				return _keepClean;
			}
			set
			{
				lock (_sync)
				{
					if (canSet())
					{
						_keepClean = value;
					}
				}
			}
		}

		public IEnumerable<IWebSocketSession> Sessions
		{
			get
			{
				if (_state != ServerState.Start)
				{
					return Enumerable.Empty<IWebSocketSession>();
				}
				lock (_sync)
				{
					if (_state != ServerState.Start)
					{
						return Enumerable.Empty<IWebSocketSession>();
					}
					return _sessions.Values.ToList();
				}
			}
		}

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
					string message = "Zero or less.";
					throw new ArgumentOutOfRangeException("value", message);
				}
				lock (_sync)
				{
					if (canSet())
					{
						_waitTime = value;
					}
				}
			}
		}

		static WebSocketSessionManager()
		{
			_rawEmptyPingFrame = WebSocketFrame.CreatePingFrame(mask: false).ToArray();
		}

		internal WebSocketSessionManager(Logger log)
		{
			_log = log;
			_forSweep = new object();
			_keepClean = true;
			_sessions = new Dictionary<string, IWebSocketSession>();
			_state = ServerState.Ready;
			_sync = ((ICollection)_sessions).SyncRoot;
			_waitTime = TimeSpan.FromSeconds(1.0);
			setSweepTimer(60000.0);
		}

		private void broadcast(Opcode opcode, byte[] data, Action completed)
		{
			Dictionary<CompressionMethod, byte[]> dictionary = new Dictionary<CompressionMethod, byte[]>();
			try
			{
				foreach (IWebSocketSession session in Sessions)
				{
					if (_state != ServerState.Start)
					{
						_log.Error("The send is cancelled.");
						break;
					}
					session.WebSocket.Send(opcode, data, dictionary);
				}
				completed?.Invoke();
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
				_log.Debug(ex.ToString());
			}
			finally
			{
				dictionary.Clear();
			}
		}

		private void broadcast(Opcode opcode, Stream sourceStream, Action completed)
		{
			Dictionary<CompressionMethod, Stream> dictionary = new Dictionary<CompressionMethod, Stream>();
			try
			{
				foreach (IWebSocketSession session in Sessions)
				{
					if (_state != ServerState.Start)
					{
						_log.Error("The send is cancelled.");
						break;
					}
					session.WebSocket.Send(opcode, sourceStream, dictionary);
				}
				completed?.Invoke();
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
				_log.Debug(ex.ToString());
			}
			finally
			{
				foreach (Stream value in dictionary.Values)
				{
					value.Dispose();
				}
				dictionary.Clear();
			}
		}

		private void broadcastAsync(Opcode opcode, byte[] data, Action completed)
		{
			ThreadPool.QueueUserWorkItem((object state) =>
			{
				broadcast(opcode, data, completed);
			});
		}

		private void broadcastAsync(Opcode opcode, Stream sourceStream, Action completed)
		{
			ThreadPool.QueueUserWorkItem((object state) =>
			{
				broadcast(opcode, sourceStream, completed);
			});
		}

		private Dictionary<string, bool> broadping(byte[] rawFrame)
		{
			Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
			foreach (IWebSocketSession session in Sessions)
			{
				if (_state != ServerState.Start)
				{
					dictionary.Clear();
					break;
				}
				bool value = session.WebSocket.Ping(rawFrame);
				dictionary.Add(session.ID, value);
			}
			return dictionary;
		}

		private bool canSet()
		{
			return _state == ServerState.Ready || _state == ServerState.Stop;
		}

		private static string createID()
		{
			return Guid.NewGuid().ToString("N");
		}

		private void setSweepTimer(double interval)
		{
			_sweepTimer = new System.Timers.Timer(interval);
			_sweepTimer.Elapsed += (object sender, ElapsedEventArgs e) =>
			{
				Sweep();
			};
		}

		private void stop(PayloadData payloadData, bool send)
		{
			byte[] rawFrame = (send ? WebSocketFrame.CreateCloseFrame(payloadData, mask: false).ToArray() : null);
			lock (_sync)
			{
				_state = ServerState.ShuttingDown;
				_sweepTimer.Enabled = false;
				foreach (IWebSocketSession item in _sessions.Values.ToList())
				{
					item.WebSocket.Close(payloadData, rawFrame);
				}
				_state = ServerState.Stop;
			}
		}

		private bool tryGetSession(string id, out IWebSocketSession session)
		{
			session = null;
			if (_state != ServerState.Start)
			{
				return false;
			}
			lock (_sync)
			{
				if (_state != ServerState.Start)
				{
					return false;
				}
				return _sessions.TryGetValue(id, out session);
			}
		}

		internal string Add(IWebSocketSession session)
		{
			lock (_sync)
			{
				if (_state != ServerState.Start)
				{
					return null;
				}
				string text = createID();
				_sessions.Add(text, session);
				return text;
			}
		}

		internal bool Remove(string id)
		{
			lock (_sync)
			{
				return _sessions.Remove(id);
			}
		}

		internal void Start()
		{
			lock (_sync)
			{
				_sweepTimer.Enabled = _keepClean;
				_state = ServerState.Start;
			}
		}

		internal void Stop(ushort code, string reason)
		{
			if (code == 1005)
			{
				stop(PayloadData.Empty, send: true);
				return;
			}
			PayloadData payloadData = new PayloadData(code, reason);
			bool send = !code.IsReservedStatusCode();
			stop(payloadData, send);
		}

		public void Broadcast(byte[] data)
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the service is not Start.";
				throw new InvalidOperationException(message);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (data.LongLength <= WebSocket.FragmentLength)
			{
				broadcast(Opcode.Binary, data, null);
			}
			else
			{
				broadcast(Opcode.Binary, new MemoryStream(data), null);
			}
		}

		public void Broadcast(string data)
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the service is not Start.";
				throw new InvalidOperationException(message);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (!data.TryGetUTF8EncodedBytes(out var bytes))
			{
				string message2 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(message2, "data");
			}
			if (bytes.LongLength <= WebSocket.FragmentLength)
			{
				broadcast(Opcode.Text, bytes, null);
			}
			else
			{
				broadcast(Opcode.Text, new MemoryStream(bytes), null);
			}
		}

		public void Broadcast(Stream stream, int length)
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the service is not Start.";
				throw new InvalidOperationException(message);
			}
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (!stream.CanRead)
			{
				string message2 = "It cannot be read.";
				throw new ArgumentException(message2, "stream");
			}
			if (length < 1)
			{
				string message3 = "Less than 1.";
				throw new ArgumentException(message3, "length");
			}
			byte[] array = stream.ReadBytes(length);
			int num = array.Length;
			if (num == 0)
			{
				string message4 = "No data could be read from it.";
				throw new ArgumentException(message4, "stream");
			}
			if (num < length)
			{
				string format = "Only {0} byte(s) of data could be read from the stream.";
				string message5 = string.Format(format, num);
				_log.Warn(message5);
			}
			if (num <= WebSocket.FragmentLength)
			{
				broadcast(Opcode.Binary, array, null);
			}
			else
			{
				broadcast(Opcode.Binary, new MemoryStream(array), null);
			}
		}

		public void BroadcastAsync(byte[] data, Action completed)
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the service is not Start.";
				throw new InvalidOperationException(message);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (data.LongLength <= WebSocket.FragmentLength)
			{
				broadcastAsync(Opcode.Binary, data, completed);
			}
			else
			{
				broadcastAsync(Opcode.Binary, new MemoryStream(data), completed);
			}
		}

		public void BroadcastAsync(string data, Action completed)
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the service is not Start.";
				throw new InvalidOperationException(message);
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (!data.TryGetUTF8EncodedBytes(out var bytes))
			{
				string message2 = "It could not be UTF-8-encoded.";
				throw new ArgumentException(message2, "data");
			}
			if (bytes.LongLength <= WebSocket.FragmentLength)
			{
				broadcastAsync(Opcode.Text, bytes, completed);
			}
			else
			{
				broadcastAsync(Opcode.Text, new MemoryStream(bytes), completed);
			}
		}

		public void BroadcastAsync(Stream stream, int length, Action completed)
		{
			if (_state != ServerState.Start)
			{
				string message = "The current state of the service is not Start.";
				throw new InvalidOperationException(message);
			}
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (!stream.CanRead)
			{
				string message2 = "It cannot be read.";
				throw new ArgumentException(message2, "stream");
			}
			if (length < 1)
			{
				string message3 = "Less than 1.";
				throw new ArgumentException(message3, "length");
			}
			byte[] array = stream.ReadBytes(length);
			int num = array.Length;
			if (num == 0)
			{
				string message4 = "No data could be read from it.";
				throw new ArgumentException(message4, "stream");
			}
			if (num < length)
			{
				string format = "Only {0} byte(s) of data could be read from the stream.";
				string message5 = string.Format(format, num);
				_log.Warn(message5);
			}
			if (num <= WebSocket.FragmentLength)
			{
				broadcastAsync(Opcode.Binary, array, completed);
			}
			else
			{
				broadcastAsync(Opcode.Binary, new MemoryStream(array), completed);
			}
		}

		public void CloseSession(string id)
		{
			if (!TryGetSession(id, out var session))
			{
				string message = "The session could not be found.";
				throw new InvalidOperationException(message);
			}
			session.WebSocket.Close();
		}

		public void CloseSession(string id, ushort code, string reason)
		{
			if (!TryGetSession(id, out var session))
			{
				string message = "The session could not be found.";
				throw new InvalidOperationException(message);
			}
			session.WebSocket.Close(code, reason);
		}

		public void CloseSession(string id, CloseStatusCode code, string reason)
		{
			if (!TryGetSession(id, out var session))
			{
				string message = "The session could not be found.";
				throw new InvalidOperationException(message);
			}
			session.WebSocket.Close(code, reason);
		}

		public bool PingTo(string id)
		{
			if (!TryGetSession(id, out var session))
			{
				string message = "The session could not be found.";
				throw new InvalidOperationException(message);
			}
			return session.WebSocket.Ping();
		}

		public bool PingTo(string message, string id)
		{
			if (!TryGetSession(id, out var session))
			{
				string message2 = "The session could not be found.";
				throw new InvalidOperationException(message2);
			}
			return session.WebSocket.Ping(message);
		}

		public void SendTo(byte[] data, string id)
		{
			if (!TryGetSession(id, out var session))
			{
				string message = "The session could not be found.";
				throw new InvalidOperationException(message);
			}
			session.WebSocket.Send(data);
		}

		public void SendTo(string data, string id)
		{
			if (!TryGetSession(id, out var session))
			{
				string message = "The session could not be found.";
				throw new InvalidOperationException(message);
			}
			session.WebSocket.Send(data);
		}

		public void SendTo(Stream stream, int length, string id)
		{
			if (!TryGetSession(id, out var session))
			{
				string message = "The session could not be found.";
				throw new InvalidOperationException(message);
			}
			session.WebSocket.Send(stream, length);
		}

		public void SendToAsync(byte[] data, string id, Action<bool> completed)
		{
			if (!TryGetSession(id, out var session))
			{
				string message = "The session could not be found.";
				throw new InvalidOperationException(message);
			}
			session.WebSocket.SendAsync(data, completed);
		}

		public void SendToAsync(string data, string id, Action<bool> completed)
		{
			if (!TryGetSession(id, out var session))
			{
				string message = "The session could not be found.";
				throw new InvalidOperationException(message);
			}
			session.WebSocket.SendAsync(data, completed);
		}

		public void SendToAsync(Stream stream, int length, string id, Action<bool> completed)
		{
			if (!TryGetSession(id, out var session))
			{
				string message = "The session could not be found.";
				throw new InvalidOperationException(message);
			}
			session.WebSocket.SendAsync(stream, length, completed);
		}

		public void Sweep()
		{
			if (_sweeping)
			{
				_log.Trace("The sweep process is already in progress.");
				return;
			}
			lock (_forSweep)
			{
				if (_sweeping)
				{
					_log.Trace("The sweep process is already in progress.");
					return;
				}
				_sweeping = true;
			}
			foreach (string inactiveID in InactiveIDs)
			{
				if (_state != ServerState.Start)
				{
					break;
				}
				lock (_sync)
				{
					if (_state != ServerState.Start)
					{
						break;
					}
					if (_sessions.TryGetValue(inactiveID, out var value))
					{
						switch (value.WebSocket.ReadyState)
						{
						case WebSocketState.Open:
							value.WebSocket.Close(CloseStatusCode.Abnormal);
							break;
						default:
							_sessions.Remove(inactiveID);
							break;
						case WebSocketState.Closing:
							break;
						}
					}
					continue;
				}
			}
			_sweeping = false;
		}

		public bool TryGetSession(string id, out IWebSocketSession session)
		{
			if (id == null)
			{
				throw new ArgumentNullException("id");
			}
			if (id.Length == 0)
			{
				throw new ArgumentException("An empty string.", "id");
			}
			return tryGetSession(id, out session);
		}
	}
}
