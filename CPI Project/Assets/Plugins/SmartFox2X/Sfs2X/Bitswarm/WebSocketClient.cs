#if UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Sfs2X.Bitswarm.BBox;
using Sfs2X.Controllers;
using Sfs2X.Core;
using Sfs2X.Core.Sockets;
using Sfs2X.Exceptions;
using Sfs2X.Logging;
using Sfs2X.Util;

namespace Sfs2X.Bitswarm
{
	public class WebSocketClient : ISocketClient, IDispatchable
	{
		private ISocketLayer socket = null;

		private IoHandler ioHandler;

		private Dictionary<int, IController> controllers = new Dictionary<int, IController>();

		private int compressionThreshold = 2000000;

		private int maxMessageSize = 10000;

		private SmartFox sfs;

		private string lastHost;

		private int lastWsPort;

		private bool useWSBinary;

		private bool useWSSecure;

		private Logger log;

		private SystemController sysController;

		private ExtensionController extController;

		private bool controllersInited = false;

		private EventDispatcher dispatcher;

		public bool UseBlueBox => false;

		public string ConnectionMode
		{
			get
			{
				if (useWSSecure)
				{
					if (useWSBinary)
					{
						return ConnectionModes.WEBSOCKET_SECURE_BIN;
					}
					return ConnectionModes.WEBSOCKET_SECURE_TEXT;
				}
				if (useWSBinary)
				{
					return ConnectionModes.WEBSOCKET_BIN;
				}
				return ConnectionModes.WEBSOCKET_TEXT;
			}
		}

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

		public IBBClient HttpClient => null;

		public bool IsReconnecting
		{
			get
			{
				return false;
			}
			set
			{
				logUnsupportedFeature("HRC system", "IsReconnecting setter");
			}
		}

		public int ReconnectionSeconds
		{
			get
			{
				return 0;
			}
			set
			{
				if (value > 0)
				{
					logUnsupportedFeature("HRC system", "ReconnectionSeconds setter");
				}
			}
		}

		public bool IsBinProtocol => useWSBinary;

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
				return lastWsPort;
			}
		}

		public CryptoKey CryptoKey
		{
			get
			{
				return null;
			}
			set
			{
				logUnsupportedFeature("Encryption", "CryptoKey setter");
			}
		}

		public IUDPManager UdpManager
		{
			get
			{
				return null;
			}
			set
			{
				logUnsupportedFeature("UDP protocol", "UdpManager setter");
			}
		}

		public void EnableBBoxDebug(bool value)
		{
			logUnsupportedFeature("BlueBox", "EnableBBoxDebug method");
		}

		public WebSocketClient()
		{
			sfs = null;
			log = null;
			useWSBinary = false;
			useWSSecure = false;
		}

		public WebSocketClient(SmartFox sfs, bool useWSBinary, bool useWSSecure)
		{
			this.sfs = sfs;
			log = sfs.Log;
			this.useWSBinary = useWSBinary;
			this.useWSSecure = useWSSecure;
		}

		public void ForceBlueBox(bool val)
		{
			logUnsupportedFeature("BlueBox", "ForceBlueBox method");
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
				socket = new WebSocketLayer(this, useWSSecure);
				ISocketLayer socketLayer = socket;
				socketLayer.OnConnect = (ConnectionDelegate)Delegate.Combine(socketLayer.OnConnect, new ConnectionDelegate(OnSocketConnect));
				ISocketLayer socketLayer2 = socket;
				socketLayer2.OnDisconnect = (DisconnectionDelegate)Delegate.Combine(socketLayer2.OnDisconnect, new DisconnectionDelegate(OnSocketClose));
				ISocketLayer socketLayer3 = socket;
				socketLayer3.OnData = (OnDataDelegate)Delegate.Combine(socketLayer3.OnData, new OnDataDelegate(OnSocketData));
				ISocketLayer socketLayer4 = socket;
				socketLayer4.OnStringData = (OnStringDataDelegate)Delegate.Combine(socketLayer4.OnStringData, new OnStringDataDelegate(OnSocketData));
				ISocketLayer socketLayer5 = socket;
				socketLayer5.OnError = (OnErrorDelegate)Delegate.Combine(socketLayer5.OnError, new OnErrorDelegate(OnSocketError));
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
			socketLayer4.OnStringData = (OnStringDataDelegate)Delegate.Remove(socketLayer4.OnStringData, new OnStringDataDelegate(OnSocketData));
			ISocketLayer socketLayer5 = socket;
			socketLayer5.OnError = (OnErrorDelegate)Delegate.Remove(socketLayer5.OnError, new OnErrorDelegate(OnSocketError));
			if (socket.IsConnected)
			{
				socket.Disconnect();
			}
			socket = null;
		}

		public IController GetController(int id)
		{
			return controllers[id];
		}

		public void Connect()
		{
			Connect("127.0.0.1", 8888);
		}

		public void Connect(string host, int port)
		{
			lastHost = host;
			lastWsPort = port;
			socket.Connect(lastHost, lastWsPort);
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
			socket.Disconnect(reason);
			ExecuteDisconnection(reason);
		}

		public void StopReconnection()
		{
			logUnsupportedFeature("HRC system", "StopReconnection method");
		}

		public void KillConnection()
		{
			logUnsupportedFeature("HRC system", "KillConnection method");
		}

		public long NextUdpPacketId()
		{
			return 0L;
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

		private void InitControllers()
		{
			sysController = new SystemController(this);
			extController = new ExtensionController(this);
			AddController(0, sysController);
			AddController(1, extController);
		}

		private void ExecuteDisconnection(string reason)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["reason"] = ((reason == null) ? ClientDisconnectionReason.UNKNOWN : reason);
			DispatchEvent(new BitSwarmEvent(BitSwarmEvent.DISCONNECT, dictionary));
		}

		private void logUnsupportedFeature(string feature, string method)
		{
			log.Debug(feature + " not supported by " + ConnectionMode + " connection mode; " + method + " call ignored");
		}

		private void OnSocketConnect()
		{
			BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.CONNECT);
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["success"] = true;
			dictionary["isReconnection"] = false;
			bitSwarmEvent.Params = dictionary;
			DispatchEvent(bitSwarmEvent);
		}

		private void OnSocketClose(string reason = null)
		{
			ExecuteDisconnection(reason);
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
				log.Error("## WebSocketDataError: " + ex.Message);
				BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.DATA_ERROR);
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary["message"] = ex.ToString();
				bitSwarmEvent.Params = dictionary;
				DispatchEvent(bitSwarmEvent);
			}
		}

		private void OnSocketData(string data)
		{
			ioHandler.OnDataRead(data);
		}

		private void OnSocketError(string message, SocketError se)
		{
			BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.IO_ERROR);
			bitSwarmEvent.Params = new Dictionary<string, object>();
			bitSwarmEvent.Params["message"] = message + " ==> " + se;
			DispatchEvent(bitSwarmEvent);
		}

		public void AddEventListener(string eventType, EventListenerDelegate listener)
		{
			dispatcher.AddEventListener(eventType, listener);
		}

		private void DispatchEvent(BitSwarmEvent evt)
		{
			dispatcher.DispatchEvent(evt);
		}
	}
}
#else
using Sfs2X.Bitswarm.BBox;
using Sfs2X.Controllers;
using Sfs2X.Core;
using Sfs2X.Core.Sockets;
using Sfs2X.Exceptions;
using Sfs2X.Logging;
using Sfs2X.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Logger = Sfs2X.Logging.Logger;

namespace Sfs2X.Bitswarm
{
    public class WebSocketClient : ISocketClient, IDispatchable
    {
        private ISocketLayer socket = null;

        private IoHandler ioHandler;

        private Dictionary<int, IController> controllers = new Dictionary<int, IController>();

        private int compressionThreshold = 2000000;

        private int maxMessageSize = 10000;

        private SmartFox sfs;

        private string lastHost;

        private int lastWsPort;

        private bool useWSBinary;

        private bool useWSSecure;

        private Logger log;

        private SystemController sysController;

        private ExtensionController extController;

        private bool controllersInited = false;

        private EventDispatcher dispatcher;

        public bool UseBlueBox => false;

        public string ConnectionMode
        {
            get
            {
                if (useWSSecure)
                {
                    if (useWSBinary)
                    {
                        return ConnectionModes.WEBSOCKET_SECURE_BIN;
                    }
                    return ConnectionModes.WEBSOCKET_SECURE_TEXT;
                }
                if (useWSBinary)
                {
                    return ConnectionModes.WEBSOCKET_BIN;
                }
                return ConnectionModes.WEBSOCKET_TEXT;
            }
        }

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

        public IBBClient HttpClient => null;

        public bool IsReconnecting
        {
            get
            {
                return false;
            }
            set
            {
                logUnsupportedFeature("HRC system", "IsReconnecting setter");
            }
        }

        public int ReconnectionSeconds
        {
            get
            {
                return 0;
            }
            set
            {
                if (value > 0)
                {
                    logUnsupportedFeature("HRC system", "ReconnectionSeconds setter");
                }
            }
        }

        public bool IsBinProtocol => useWSBinary;

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
                return lastWsPort;
            }
        }

        public CryptoKey CryptoKey
        {
            get
            {
                return null;
            }
            set
            {
                logUnsupportedFeature("Encryption", "CryptoKey setter");
            }
        }

        public IUDPManager UdpManager
        {
            get
            {
                return null;
            }
            set
            {
                logUnsupportedFeature("UDP protocol", "UdpManager setter");
            }
        }

        public void EnableBBoxDebug(bool value)
        {
            logUnsupportedFeature("BlueBox", "EnableBBoxDebug method");
        }

        public WebSocketClient()
        {
            sfs = null;
            log = null;
            useWSBinary = false;
            useWSSecure = false;
        }

        public WebSocketClient(SmartFox sfs, bool useWSBinary, bool useWSSecure)
        {
            this.sfs = sfs;
            log = sfs.Log;
            this.useWSBinary = useWSBinary;
            this.useWSSecure = useWSSecure;
        }

        public void ForceBlueBox(bool val)
        {
            logUnsupportedFeature("BlueBox", "ForceBlueBox method");
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
                socket = new WebSocketLayer(this, useWSSecure);
                ISocketLayer socketLayer = socket;
                socketLayer.OnConnect = (ConnectionDelegate)Delegate.Combine(socketLayer.OnConnect, new ConnectionDelegate(OnSocketConnect));
                ISocketLayer socketLayer2 = socket;
                socketLayer2.OnDisconnect = (DisconnectionDelegate)Delegate.Combine(socketLayer2.OnDisconnect, new DisconnectionDelegate(OnSocketClose));
                ISocketLayer socketLayer3 = socket;
                socketLayer3.OnData = (OnDataDelegate)Delegate.Combine(socketLayer3.OnData, new OnDataDelegate(OnSocketData));
                ISocketLayer socketLayer4 = socket;
                socketLayer4.OnStringData = (OnStringDataDelegate)Delegate.Combine(socketLayer4.OnStringData, new OnStringDataDelegate(OnSocketData));
                ISocketLayer socketLayer5 = socket;
                socketLayer5.OnError = (OnErrorDelegate)Delegate.Combine(socketLayer5.OnError, new OnErrorDelegate(OnSocketError));
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
            socketLayer4.OnStringData = (OnStringDataDelegate)Delegate.Remove(socketLayer4.OnStringData, new OnStringDataDelegate(OnSocketData));
            ISocketLayer socketLayer5 = socket;
            socketLayer5.OnError = (OnErrorDelegate)Delegate.Remove(socketLayer5.OnError, new OnErrorDelegate(OnSocketError));
            if (socket.IsConnected)
            {
                socket.Disconnect();
            }
            socket = null;
        }

        public IController GetController(int id)
        {
            return controllers[id];
        }

        public void Connect()
        {
            Connect("127.0.0.1", 8888);
        }

        public void Connect(string host, int port)
        {
            lastHost = host;
            lastWsPort = port;
            socket.Connect(lastHost, lastWsPort);
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
            socket.Disconnect(reason);
            ExecuteDisconnection(reason);
        }

        public void StopReconnection()
        {
            logUnsupportedFeature("HRC system", "StopReconnection method");
        }

        public void KillConnection()
        {
            logUnsupportedFeature("HRC system", "KillConnection method");
        }

        public long NextUdpPacketId()
        {
            return 0L;
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

        private void InitControllers()
        {
            sysController = new SystemController(this);
            extController = new ExtensionController(this);
            AddController(0, sysController);
            AddController(1, extController);
        }

        private void ExecuteDisconnection(string reason)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["reason"] = ((reason == null) ? ClientDisconnectionReason.UNKNOWN : reason);
            DispatchEvent(new BitSwarmEvent(BitSwarmEvent.DISCONNECT, dictionary));
        }

        private void logUnsupportedFeature(string feature, string method)
        {
            log.Debug(feature + " not supported by " + ConnectionMode + " connection mode; " + method + " call ignored");
        }

        private void OnSocketConnect()
        {
            BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.CONNECT);
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["success"] = true;
            dictionary["isReconnection"] = false;
            bitSwarmEvent.Params = dictionary;
            DispatchEvent(bitSwarmEvent);
        }

        private void OnSocketClose(string reason = null)
        {
            ExecuteDisconnection(reason);
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
                log.Error("## WebSocketDataError: " + ex.Message);
                BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.DATA_ERROR);
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary["message"] = ex.ToString();
                bitSwarmEvent.Params = dictionary;
                DispatchEvent(bitSwarmEvent);
            }
        }

        private void OnSocketData(string data)
        {
            ioHandler.OnDataRead(data);
        }

        private void OnSocketError(string message, SocketError se)
        {
            BitSwarmEvent bitSwarmEvent = new BitSwarmEvent(BitSwarmEvent.IO_ERROR);
            bitSwarmEvent.Params = new Dictionary<string, object>();
            bitSwarmEvent.Params["message"] = message + " ==> " + se;
            DispatchEvent(bitSwarmEvent);
        }

        public void AddEventListener(string eventType, EventListenerDelegate listener)
        {
            dispatcher.AddEventListener(eventType, listener);
        }

        private void DispatchEvent(BitSwarmEvent evt)
        {
            dispatcher.DispatchEvent(evt);
        }
    }
}
#endif