using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Timers;
using Sfs2X.Bitswarm.BBox;
using Sfs2X.Controllers;
using Sfs2X.Core;
using Sfs2X.Core.Sockets;
using Sfs2X.Exceptions;
using Sfs2X.Logging;
using Sfs2X.Util;

namespace Sfs2X.Bitswarm
{
	public class BitSwarmClient : ISocketClient, IDispatchable
	{
		private readonly double reconnectionDelayMillis = 1000.0;

		private ISocketLayer socket = null;

		private IoHandler ioHandler;

		private Dictionary<int, IController> controllers = new Dictionary<int, IController>();

		private int compressionThreshold = 2000000;

		private int maxMessageSize = 10000;

		private SmartFox sfs;

		private string lastHost;

		private int lastTcpPort;

		private Logger log;

		private int reconnectionSeconds = 0;

		private bool attemptingReconnection = false;

		private DateTime firstReconnAttempt = DateTime.MinValue;

		private int reconnCounter = 1;

		private SystemController sysController;

		private ExtensionController extController;

		private IUDPManager udpManager;

		private bool controllersInited = false;

		private EventDispatcher dispatcher;

		private IBBClient bbClient;

		private volatile bool useBlueBox = false;

		private bool bbConnected = false;

		private string connectionMode;

		private ThreadManager threadManager = new ThreadManager();

		private CryptoKey cryptoKey;

		private Timer retryTimer = null;

		public ThreadManager ThreadManager => threadManager;

		public string ConnectionMode => connectionMode;

		public bool UseBlueBox => useBlueBox;

		public bool Debug
		{
			get
			{
				if (sfs == null)
				{
					return true;
				}
				return sfs.Debug;
			}
		}

		public SmartFox Sfs => sfs;

		public bool Connected
		{
			get
			{
				if (useBlueBox)
				{
					return bbConnected;
				}
				if (socket == null)
				{
					return false;
				}
				return socket.IsConnected;
			}
		}

		public IoHandler IoHandler
		{
			get
			{
				return ioHandler;
			}
			set
			{
				if (ioHandler != null)
				{
					throw new SFSError("IOHandler is already set!");
				}
				ioHandler = value;
			}
		}

		public int CompressionThreshold
		{
			get
			{
				return compressionThreshold;
			}
			set
			{
				if (value > 100)
				{
					compressionThreshold = value;
					return;
				}
				throw new ArgumentException("Compression threshold cannot be < 100 bytes");
			}
		}

		public int MaxMessageSize
		{
			get
			{
				return maxMessageSize;
			}
			set
			{
				maxMessageSize = value;
			}
		}

		public SystemController SysController => sysController;

		public ExtensionController ExtController => extController;

		public ISocketLayer Socket => socket;

		public IBBClient HttpClient => bbClient;

		public bool IsReconnecting
		{
			get
			{
				return attemptingReconnection;
			}
			set
			{
				attemptingReconnection = value;
			}
		}

		public int ReconnectionSeconds
		{
			get
			{
				return reconnectionSeconds;
			}
			set
			{
				if (value < 0)
				{
					reconnectionSeconds = 0;
				}
				else
				{
					reconnectionSeconds = value;
				}
			}
		}

		public bool IsBinProtocol => true;

		public EventDispatcher Dispatcher
		{
			get
			{
				return dispatcher;
			}
			set
			{
				dispatcher = value;
			}
		}

		public Logger Log
		{
			get
			{
				if (sfs == null)
				{
					return new Logger(null);
				}
				return sfs.Log;
			}
		}

		public CryptoKey CryptoKey
		{
			get
			{
				return cryptoKey;
			}
			set
			{
				cryptoKey = value;
			}
		}

		public string ConnectionHost
		{
			get
			{
				if (!Connected)
				{
					return "Not Connected";
				}
				return lastHost;
			}
		}

		public int ConnectionPort
		{
			get
			{
				if (!Connected)
				{
					return -1;
				}
				return lastTcpPort;
			}
		}

		public IUDPManager UdpManager
		{
			get
			{
				return udpManager;
			}
			set
			{
				udpManager = value;
			}
		}

		public BitSwarmClient()
		{
			sfs = null;
			log = null;
		}

		public BitSwarmClient(SmartFox sfs)
		{
			this.sfs = sfs;
			log = sfs.Log;
		}

		public void ForceBlueBox(bool val)
		{
			if (!bbConnected)
			{
				useBlueBox = val;
				return;
			}
			throw new Exception("You can't change the BlueBox mode while the connection is running");
		}

		public void Init()
		{
			if (dispatcher == null)
			{
				dispatcher = new EventDispatcher(this);
			}
			if (!controllersInited)
			{
				InitControllers();
				controllersInited = true;
			}
			if (socket == null)
			{
				socket = new TCPSocketLayer(this);
				ISocketLayer socketLayer = socket;
				socketLayer.OnConnect = (ConnectionDelegate)Delegate.Combine(socketLayer.OnConnect, new ConnectionDelegate(OnSocketConnect));
				ISocketLayer socketLayer2 = socket;
				socketLayer2.OnDisconnect = (DisconnectionDelegate)Delegate.Combine(socketLayer2.OnDisconnect, new DisconnectionDelegate(OnSocketClose));
				ISocketLayer socketLayer3 = socket;
				socketLayer3.OnData = (OnDataDelegate)Delegate.Combine(socketLayer3.OnData, new OnDataDelegate(OnSocketData));
				ISocketLayer socketLayer4 = socket;
				socketLayer4.OnError = (OnErrorDelegate)Delegate.Combine(socketLayer4.OnError, new OnErrorDelegate(OnSocketError));
				bbClient = new BBClientV2(log);
				bbClient.AddEventListener(BBEvent.CONNECT, OnBBConnect);
				bbClient.AddEventListener(BBEvent.DATA, OnBBData);
				bbClient.AddEventListener(BBEvent.DISCONNECT, OnBBDisconnect);
				bbClient.AddEventListener(BBEvent.IO_ERROR, OnBBError);
				bbClient.AddEventListener(BBEvent.SECURITY_ERROR, OnBBError);
			}
		}

		public void Destroy()
		{
			ISocketLayer socketLayer = socket;
			socketLayer.OnConnect = (ConnectionDelegate)Delegate.Remove(socketLayer.OnConnect, new ConnectionDelegate(OnSocketConnect));
			ISocketLayer socketLayer2 = socket;
			socketLayer2.OnDisconnect = (DisconnectionDelegate)Delegate.Remove(socketLayer2.OnDisconnect, new DisconnectionDelegate(OnSocketClose));
			ISocketLayer socketLayer3 = socket;
			socketLayer3.OnData = (OnDataDelegate)Delegate.Remove(socketLayer3.OnData, new OnDataDelegate(OnSocketData));
			ISocketLayer socketLayer4 = socket;
			socketLayer4.OnError = (OnErrorDelegate)Delegate.Remove(socketLayer4.OnError, new OnErrorDelegate(OnSocketError));
			if (socket.IsConnected)
			{
				socket.Disconnect();
			}
			socket = null;
			threadManager.Stop();
		}

		public void EnableBBoxDebug(bool value)
		{
			bbClient.IsDebug = value;
		}

		public IController GetController(int id)
		{
			return controllers[id];
		}

		private void AddController(int id, IController controller)
		{
			if (controller == null)
			{
				throw new ArgumentException("Controller is null, it can't be added.");
			}
			if (controllers.ContainsKey(id))
			{
				throw new ArgumentException("A controller with id: " + id + " already exists! Controller can't be added: " + controller);
			}
			controllers[id] = controller;
		}

		private void AddCustomController(int id, Type controllerType)
		{
			IController controller = Activator.CreateInstance(controllerType) as IController;
			AddController(id, controller);
		}

		public void Connect()
		{
			Connect("127.0.0.1", 9933);
		}

		public void Connect(string host, int port)
		{
			lastHost = host;
			lastTcpPort = port;
			threadManager.Start();
			if (useBlueBox)
			{
				connectionMode = ConnectionModes.BLUEBOX;
				bbClient.Connect(sfs.Config);
			}
			else
			{
				socket.Connect(lastHost, lastTcpPort);
				connectionMode = ConnectionModes.SOCKET;
			}
		}

		public void Send(IMessage message)
		{
			ioHandler.Codec.OnPacketWrite(message);
		}

		public void Disconnect()
		{
			Disconnect(null);
		}

		public void Disconnect(string reason)
		{
			if (useBlueBox)
			{
				bbClient.Close(reason);
			}
			else
			{
				socket.Disconnect(reason);
				if (udpManager != null)
				{
					udpManager.Disconnect();
				}
			}
			ReleaseResources();
		}

		private void InitControllers()
		{
			sysController = new SystemController(this);
			extController = new ExtensionController(this);
			AddController(0, sysController);
			AddController(1, extController);
		}

		private void OnSocketConnect()
		{
			BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.CONNECT);
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["success"] = true;
			dictionary["isReconnection"] = attemptingReconnection;
			bitSwarmEvent.Params = dictionary;
			DispatchEvent(bitSwarmEvent);
		}

		public void StopReconnection()
		{
			attemptingReconnection = false;
			firstReconnAttempt = DateTime.MinValue;
			if (socket.IsConnected)
			{
				socket.Disconnect();
			}
		}

		private void OnSocketClose(string reason = null)
		{
			bool flag = !attemptingReconnection && sfs.GetReconnectionSeconds() == 0;
			bool flag2 = reason == ClientDisconnectionReason.MANUAL;
			if (flag | flag2)
			{
				firstReconnAttempt = DateTime.MinValue;
				ExecuteDisconnection(reason);
				return;
			}
			if (attemptingReconnection)
			{
				Reconnect();
				return;
			}
			attemptingReconnection = true;
			firstReconnAttempt = DateTime.Now;
			reconnCounter = 1;
			DispatchEvent(new BitSwarmEvent(BitSwarmEvent.RECONNECTION_TRY));
			Reconnect();
		}

		private void SetTimeout(ElapsedEventHandler handler, double timeout)
		{
			if (retryTimer == null)
			{
				retryTimer = new Timer(timeout);
				retryTimer.Elapsed += handler;
			}
			retryTimer.AutoReset = false;
			retryTimer.Enabled = true;
			retryTimer.Start();
		}

		private void OnRetryConnectionEvent(object source, ElapsedEventArgs e)
		{
			retryTimer.Enabled = false;
			retryTimer.Stop();
			socket.Connect(lastHost, lastTcpPort);
		}

		private void Reconnect()
		{
			if (attemptingReconnection)
			{
				int seconds = sfs.GetReconnectionSeconds();
				DateTime now = DateTime.Now;
				TimeSpan timeSpan = firstReconnAttempt + new TimeSpan(0, 0, seconds) - now;
				if (timeSpan > TimeSpan.Zero)
				{
					log.Info("Reconnection attempt: " + reconnCounter + " - time left:" + timeSpan.TotalSeconds + " sec.");
					SetTimeout(OnRetryConnectionEvent, reconnectionDelayMillis);
					reconnCounter++;
				}
				else
				{
					ExecuteDisconnection();
				}
			}
		}

		private void ExecuteDisconnection(string reason = null)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["reason"] = ((reason == null) ? ClientDisconnectionReason.UNKNOWN : reason);
			DispatchEvent(new BitSwarmEvent(BitSwarmEvent.DISCONNECT, dictionary));
			ReleaseResources();
		}

		private void ReleaseResources()
		{
			threadManager.Stop();
			if (udpManager != null && udpManager.Inited)
			{
				udpManager.Disconnect();
			}
		}

		private void OnSocketData(byte[] data)
		{
			try
			{
				ByteArray buffer = new ByteArray(data);
				ioHandler.OnDataRead(buffer);
			}
			catch (Exception ex)
			{
				log.Error("## SocketDataError: " + ex.Message);
				BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.DATA_ERROR);
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary["message"] = ex.ToString();
				bitSwarmEvent.Params = dictionary;
				DispatchEvent(bitSwarmEvent);
			}
		}

		private void OnSocketError(string message, SocketError se)
		{
			if (attemptingReconnection)
			{
				Reconnect();
				return;
			}
			BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.IO_ERROR);
			bitSwarmEvent.Params = new Dictionary<string, object>();
			bitSwarmEvent.Params["message"] = message + " ==> " + se;
			DispatchEvent(bitSwarmEvent);
		}

		public void KillConnection()
		{
			if (!useBlueBox)
			{
				socket.Kill();
				OnSocketClose();
			}
		}

		public long NextUdpPacketId()
		{
			return udpManager.NextUdpPacketId;
		}

		public void AddEventListener(string eventType, EventListenerDelegate listener)
		{
			dispatcher.AddEventListener(eventType, listener);
		}

		private void DispatchEvent(BitSwarmEvent evt)
		{
			dispatcher.DispatchEvent(evt);
		}

		private void OnBBConnect(BaseEvent e)
		{
			bbConnected = true;
			BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.CONNECT);
			bitSwarmEvent.Params = new Dictionary<string, object>();
			bitSwarmEvent.Params["success"] = true;
			bitSwarmEvent.Params["isReconnection"] = false;
			DispatchEvent(bitSwarmEvent);
		}

		private void OnBBData(BaseEvent e)
		{
			BBEvent bBEvent = e as BBEvent;
			ByteArray buffer = (ByteArray)bBEvent.Params["data"];
			ioHandler.OnDataRead(buffer);
		}

		private void OnBBDisconnect(BaseEvent e)
		{
			bbConnected = false;
			useBlueBox = false;
			if (e.Params.ContainsKey("reason") && (string)e.Params["reason"] == ClientDisconnectionReason.MANUAL)
			{
				ExecuteDisconnection(ClientDisconnectionReason.MANUAL);
				return;
			}
			object value = (e.Params.ContainsKey("reason") ? e.Params["reason"] : ClientDisconnectionReason.UNKNOWN);
			BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.DISCONNECT);
			bitSwarmEvent.Params = new Dictionary<string, object>();
			bitSwarmEvent.Params["reason"] = value;
			DispatchEvent(bitSwarmEvent);
		}

		private void OnBBError(BaseEvent e)
		{
			BBEvent bBEvent = e as BBEvent;
			log.Error("## BlueBox Error: " + (string)bBEvent.Params["message"]);
			BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.IO_ERROR);
			bitSwarmEvent.Params = new Dictionary<string, object>();
			bitSwarmEvent.Params["message"] = bBEvent.Params["message"];
			DispatchEvent(bitSwarmEvent);
		}
	}
}
