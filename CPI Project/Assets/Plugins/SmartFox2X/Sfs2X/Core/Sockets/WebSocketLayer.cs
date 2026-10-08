#if UNITY_WEBGL
using System;
using System.Net.Sockets;
using Sfs2X.Bitswarm;

namespace Sfs2X.Core.Sockets
{
	public class WebSocketLayer : BaseSocketLayer, ISocketLayer
	{
		private WebSocketHelper wsh;

		private bool useWSSecure;

		private ConnectionDelegate onConnect;

		private DisconnectionDelegate onDisconnect;

		private OnDataDelegate onData = null;

		private OnStringDataDelegate onStringData = null;

		private OnErrorDelegate onError = null;

		public bool IsConnected => base.State == States.Connected;

		public bool RequiresConnection => true;

		public ConnectionDelegate OnConnect
		{
			get
			{
				return onConnect;
			}
			set
			{
				onConnect = value;
			}
		}

		public DisconnectionDelegate OnDisconnect
		{
			get
			{
				return onDisconnect;
			}
			set
			{
				onDisconnect = value;
			}
		}

		public OnDataDelegate OnData
		{
			get
			{
				return onData;
			}
			set
			{
				onData = value;
			}
		}

		public OnStringDataDelegate OnStringData
		{
			get
			{
				return onStringData;
			}
			set
			{
				onStringData = value;
			}
		}

		public OnErrorDelegate OnError
		{
			get
			{
				return onError;
			}
			set
			{
				onError = value;
			}
		}

		public WebSocketLayer(WebSocketClient wsc, bool useWSSecure)
		{
			socketClient = wsc;
			log = wsc.Log;
			this.useWSSecure = useWSSecure;
			InitStates();
		}

		private void LogWarn(string msg)
		{
			if (log != null)
			{
				log.Warn("[WebSocketLayer] " + msg);
			}
		}

		private void LogError(string msg)
		{
			if (log != null)
			{
				log.Error("[WebSocketLayer] " + msg);
			}
		}

		public void Connect(string host, int port)
		{
			if (base.State != States.Disconnected)
			{
				LogWarn("Call to Connect method ignored, as the websocket is already connected");
				return;
			}
			string uriString = "ws" + (useWSSecure ? "s" : "") + "://" + host + ":" + port + "/BlueBox/websocket";
			wsh = new WebSocketHelper(new Uri(uriString), log);
			fsm.ApplyTransition(Transitions.StartConnect);
			wsh.Connect();
		}

		public void Disconnect()
		{
			Disconnect(null);
		}

		public void Disconnect(string reason)
		{
			if (base.State != States.Connected)
			{
				LogWarn("Calling disconnect when the socket is not connected");
				return;
			}
			isDisconnecting = true;
			wsh.Close();
			HandleDisconnection(reason);
			isDisconnecting = false;
		}

		public void Write(byte[] data)
		{
			if (base.State != States.Connected)
			{
				LogError("Trying to write to disconnected websocket");
			}
			else
			{
				wsh.Send(data);
			}
		}

		public void Write(string data)
		{
			if (base.State != States.Connected)
			{
				LogError("Trying to write to disconnected websocket");
			}
			else
			{
				wsh.Send(data);
			}
		}

		public void Kill()
		{
		}

		public void ProcessState()
		{
			if (base.State == States.Connecting)
			{
				if (wsh.Error != null)
				{
					string err = "Connection error: " + wsh.Error.Message + ((wsh.Error.Exception != null) ? (" " + wsh.Error.Exception.StackTrace) : null);
					HandleError(err, wsh.Error.Exception);
				}
				else if (wsh.IsConnected)
				{
					fsm.ApplyTransition(Transitions.ConnectionSuccess);
					CallOnConnect();
				}
			}
			else
			{
				if (base.State != States.Connected)
				{
					return;
				}
				if (wsh.Error != null)
				{
					string err2 = "Communication error: " + wsh.Error.Message + ((wsh.Error.Exception != null) ? (" " + wsh.Error.Exception.StackTrace) : null);
					HandleError(err2, wsh.Error.Exception);
				}
				else if (socketClient.IsBinProtocol)
				{
					byte[] data;
					while ((data = wsh.ReceiveByteArray()) != null)
					{
						CallOnData(data);
					}
				}
				else
				{
					string data2;
					while ((data2 = wsh.ReceiveString()) != null)
					{
						CallOnStringData(data2);
					}
				}
			}
		}

		private void HandleError(string err, Exception e)
		{
			SocketError se = SocketError.SocketError;
			if (e != null && e is SocketException)
			{
				se = (e as SocketException).SocketErrorCode;
			}
			fsm.ApplyTransition(Transitions.ConnectionFailure);
			if (!isDisconnecting)
			{
				LogError(err);
				onError(err, se);
			}
			HandleDisconnection();
		}

		private void HandleDisconnection()
		{
			HandleDisconnection(null);
		}

		private void HandleDisconnection(string reason)
		{
			if (base.State != States.Disconnected)
			{
				fsm.ApplyTransition(Transitions.Disconnect);
				if (reason == null)
				{
					CallOnDisconnect();
				}
			}
		}

		private void CallOnConnect()
		{
			if (onConnect != null)
			{
				onConnect();
			}
		}

		private void CallOnDisconnect()
		{
			if (onDisconnect != null)
			{
				onDisconnect();
			}
		}

		private void CallOnData(byte[] data)
		{
			if (onData != null)
			{
				onData(data);
			}
		}

		private void CallOnStringData(string data)
		{
			if (onStringData != null)
			{
				onStringData(data);
			}
		}

		private void CallOnError(string msg, SocketError se)
		{
			if (onError != null)
			{
				onError(msg, se);
			}
		}
	}
}
#else
using Sfs2X.Bitswarm;
using System;
using System.Net.Sockets;
using static UnityEditor.VersionControl.Asset;
using static UnityEngine.Windows.Speech.PhraseRecognitionSystem;

namespace Sfs2X.Core.Sockets
{
    public class WebSocketLayer : BaseSocketLayer, ISocketLayer
    {
        private WebSocketHelper wsh;

        private bool useWSSecure;

        private ConnectionDelegate onConnect;

        private DisconnectionDelegate onDisconnect;

        private OnDataDelegate onData = null;

        private OnStringDataDelegate onStringData = null;

        private OnErrorDelegate onError = null;

        public bool IsConnected => base.State == States.Connected;

        public bool RequiresConnection => true;

        public ConnectionDelegate OnConnect
        {
            get
            {
                return onConnect;
            }
            set
            {
                onConnect = value;
            }
        }

        public DisconnectionDelegate OnDisconnect
        {
            get
            {
                return onDisconnect;
            }
            set
            {
                onDisconnect = value;
            }
        }

        public OnDataDelegate OnData
        {
            get
            {
                return onData;
            }
            set
            {
                onData = value;
            }
        }

        public OnStringDataDelegate OnStringData
        {
            get
            {
                return onStringData;
            }
            set
            {
                onStringData = value;
            }
        }

        public OnErrorDelegate OnError
        {
            get
            {
                return onError;
            }
            set
            {
                onError = value;
            }
        }

        public WebSocketLayer(WebSocketClient wsc, bool useWSSecure)
        {
            socketClient = wsc;
            log = wsc.Log;
            this.useWSSecure = useWSSecure;
            InitStates();
        }

        private void LogWarn(string msg)
        {
            if (log != null)
            {
                log.Warn("[WebSocketLayer] " + msg);
            }
        }

        private void LogError(string msg)
        {
            if (log != null)
            {
                log.Error("[WebSocketLayer] " + msg);
            }
        }

        public void Connect(string host, int port)
        {
            if (base.State != States.Disconnected)
            {
                LogWarn("Call to Connect method ignored, as the websocket is already connected");
                return;
            }
            string uriString = "ws" + (useWSSecure ? "s" : "") + "://" + host + ":" + port + "/BlueBox/websocket";
            wsh = new WebSocketHelper(new Uri(uriString), log);
            fsm.ApplyTransition(Transitions.StartConnect);
            wsh.Connect();
        }

        public void Disconnect()
        {
            Disconnect(null);
        }

        public void Disconnect(string reason)
        {
            if (base.State != States.Connected)
            {
                LogWarn("Calling disconnect when the socket is not connected");
                return;
            }
            isDisconnecting = true;
            wsh.Close();
            HandleDisconnection(reason);
            isDisconnecting = false;
        }

        public void Write(byte[] data)
        {
            if (base.State != States.Connected)
            {
                LogError("Trying to write to disconnected websocket");
            }
            else
            {
                wsh.Send(data);
            }
        }

        public void Write(string data)
        {
            if (base.State != States.Connected)
            {
                LogError("Trying to write to disconnected websocket");
            }
            else
            {
                wsh.Send(data);
            }
        }

        public void Kill()
        {
        }

        public void ProcessState()
        {
            if (base.State == States.Connecting)
            {
                if (wsh.Error != null)
                {
                    string err = "Connection error: " + wsh.Error.Message + ((wsh.Error.Exception != null) ? (" " + wsh.Error.Exception.StackTrace) : null);
                    HandleError(err, wsh.Error.Exception);
                }
                else if (wsh.IsConnected)
                {
                    fsm.ApplyTransition(Transitions.ConnectionSuccess);
                    CallOnConnect();
                }
            }
            else
            {
                if (base.State != States.Connected)
                {
                    return;
                }
                if (wsh.Error != null)
                {
                    string err2 = "Communication error: " + wsh.Error.Message + ((wsh.Error.Exception != null) ? (" " + wsh.Error.Exception.StackTrace) : null);
                    HandleError(err2, wsh.Error.Exception);
                }
                else if (socketClient.IsBinProtocol)
                {
                    byte[] data;
                    while ((data = wsh.ReceiveByteArray()) != null)
                    {
                        CallOnData(data);
                    }
                }
                else
                {
                    string data2;
                    while ((data2 = wsh.ReceiveString()) != null)
                    {
                        CallOnStringData(data2);
                    }
                }
            }
        }

        private void HandleError(string err, Exception e)
        {
            SocketError se = SocketError.SocketError;
            if (e != null && e is SocketException)
            {
                se = (e as SocketException).SocketErrorCode;
            }
            fsm.ApplyTransition(Transitions.ConnectionFailure);
            if (!isDisconnecting)
            {
                LogError(err);
                onError(err, se);
            }
            HandleDisconnection();
        }

        private void HandleDisconnection()
        {
            HandleDisconnection(null);
        }

        private void HandleDisconnection(string reason)
        {
            if (base.State != States.Disconnected)
            {
                fsm.ApplyTransition(Transitions.Disconnect);
                if (reason == null)
                {
                    CallOnDisconnect();
                }
            }
        }

        private void CallOnConnect()
        {
            if (onConnect != null)
            {
                onConnect();
            }
        }

        private void CallOnDisconnect()
        {
            if (onDisconnect != null)
            {
                onDisconnect();
            }
        }

        private void CallOnData(byte[] data)
        {
            if (onData != null)
            {
                onData(data);
            }
        }

        private void CallOnStringData(string data)
        {
            if (onStringData != null)
            {
                onStringData(data);
            }
        }

        private void CallOnError(string msg, SocketError se)
        {
            if (onError != null)
            {
                onError(msg, se);
            }
        }
    }
}
#endif