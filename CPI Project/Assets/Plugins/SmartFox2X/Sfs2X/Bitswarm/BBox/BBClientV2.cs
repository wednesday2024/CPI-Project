#if UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sfs2X.Core;
using Sfs2X.Logging;
using Sfs2X.Util;

namespace Sfs2X.Bitswarm.BBox
{
	public class BBClientV2 : IBBClient, IDispatchable
	{
		public const string BB_SERVLET = "/BlueBox/BlueBox.do";

		private const string BB_NULL = "null";

		private const string CMD_CONNECT = "connect";

		private const string CMD_POLL = "poll";

		private const string CMD_DATA = "data";

		private const string CMD_DISCONNECT = "disconnect";

		private const string ERR_INVALID_SESSION = "err01";

		private const string SFS_HTTP = "sfsHttp";

		private const char SEP = '|';

		private const int MIN_POLL_SPEED = 50;

		private const int MAX_POLL_SPEED = 5000;

		private const int DEFAULT_POLL_SPEED = 300;

		private const int HTTP_CONN_TIMEOUT = 30;

		private bool isConnected = false;

		private string bbUrl;

		private string sessId;

		private int pollSpeed = 300;

		private Timer pollTimer = null;

		private EventDispatcher dispatcher;

		private Logger log;

		private HttpClient reqConnection;

		private HttpClient pollingConnection;

		private ConfigData cfg;

		public bool IsDebug { get; set; } = false;

		public bool IsConnected => sessId != null;

		public string SessionId => sessId;

		public EventDispatcher Dispatcher => dispatcher;

		public BBClientV2(Logger logger)
		{
			log = logger;
			if (dispatcher == null)
			{
				dispatcher = new EventDispatcher(this);
			}
		}

		public void Connect(ConfigData cfg)
		{
			if (isConnected)
			{
				throw new Exception("BlueBox session is already connected");
			}
			this.cfg = cfg;
			ValidatePollingRate();
			bbUrl = GetBBUrl();
			if (IsDebug)
			{
				log.Info("[ BB-Connect ]: " + bbUrl);
			}
			reqConnection = GetConnection();
			reqConnection.Timeout = TimeSpan.FromSeconds(30.0);
			reqConnection.BaseAddress = new Uri(bbUrl);
			pollingConnection = GetConnection();
			pollingConnection.Timeout = TimeSpan.FromSeconds(30.0);
			pollingConnection.BaseAddress = new Uri(bbUrl);
			SendRequest("connect", null);
		}

		public void Send(ByteArray binData)
		{
			if (!isConnected)
			{
				throw new Exception("Can't send data. There is no active BlueBox connection");
			}
			SendRequest("data", binData);
		}

		public void Close(string reason)
		{
			HandleConnectionLost(fireEvent: true, reason);
		}

		private HttpClient GetConnection()
		{
			HttpClient httpClient = null;
			if (cfg.BlueBox.Proxy.Host != null && cfg.BlueBox.Proxy.Port > 0)
			{
				string address = cfg.BlueBox.Proxy.Host + ":" + cfg.BlueBox.Proxy.Port;
				WebProxy proxy = new WebProxy(address, cfg.BlueBox.Proxy.BypassLocal);
				HttpClientHandler httpClientHandler = new HttpClientHandler
				{
					Proxy = proxy,
					UseProxy = true
				};
				if (cfg.BlueBox.Proxy.UserName != null)
				{
					httpClientHandler.Credentials = new NetworkCredential(cfg.BlueBox.Proxy.UserName, cfg.BlueBox.Proxy.Password);
				}
				return new HttpClient(httpClientHandler);
			}
			return new HttpClient();
		}

		private void ValidatePollingRate()
		{
			if (cfg.BlueBox.PollingRate < 50)
			{
				cfg.BlueBox.PollingRate = 50;
			}
			else if (cfg.BlueBox.PollingRate > 5000)
			{
				cfg.BlueBox.PollingRate = 5000;
			}
		}

		private string GetBBUrl()
		{
			string text = (cfg.BlueBox.UseHttps ? "https://" : "http://");
			int num = (cfg.BlueBox.UseHttps ? cfg.HttpsPort : cfg.HttpPort);
			return text + cfg.Host + ":" + num;
		}

		private string EncodeRequest(string cmd)
		{
			return EncodeRequest(cmd, null);
		}

		private string EncodeRequest(string cmd, object data)
		{
			string text = "";
			string text2 = "";
			if (cmd == null)
			{
				cmd = "null";
			}
			if (data == null)
			{
				text2 = "null";
			}
			else if (data is ByteArray)
			{
				text2 = Convert.ToBase64String(((ByteArray)data).Bytes);
			}
			return ((sessId == null) ? "null" : sessId) + Convert.ToString('|') + cmd + Convert.ToString('|') + text2;
		}

		private ByteArray DecodeResponse(string rawData)
		{
			return new ByteArray(Convert.FromBase64String(rawData));
		}

		private void SendRequest(string cmd, object data, HttpClient client = null)
		{
			HttpClient httpClient = ((client == null) ? reqConnection : pollingConnection);
			string text = EncodeRequest(cmd, data);
			if (IsDebug)
			{
				log.Info("[ BB-Send ]: " + text);
			}
			FormUrlEncodedContent content = new FormUrlEncodedContent(new KeyValuePair<string, string>[1]
			{
				new KeyValuePair<string, string>("sfsHttp", text)
			});
			try
			{
				httpClient.PostAsync("/BlueBox/BlueBox.do", content).ContinueWith(OnHttpResponse);
			}
			catch (TaskCanceledException)
			{
				OnHttpError("Connection timeout. Server is not reachable at: " + bbUrl);
			}
			catch (HttpRequestException ex2)
			{
				OnHttpError("Error sending HTTP request to: " + bbUrl + " -- " + ex2.Message);
			}
		}

		private void OnHttpError(string message)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["message"] = message;
			HandleConnectionLost(fireEvent: true);
			DispatchEvent(new BBEvent(BBEvent.IO_ERROR, dictionary));
		}

		private void HandleConnectionLost(bool fireEvent, string reason = null)
		{
			if (!isConnected)
			{
				return;
			}
			isConnected = false;
			sessId = null;
			pollTimer.Dispose();
			if (fireEvent)
			{
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				if (reason != null)
				{
					dictionary["reason"] = reason;
				}
				BBEvent evt = new BBEvent(BBEvent.DISCONNECT, dictionary);
				DispatchEvent(evt);
			}
		}

		private void DispatchEvent(BaseEvent evt)
		{
			dispatcher.DispatchEvent(evt);
		}

		private void OnHttpResponse(Task<HttpResponseMessage> task)
		{
			try
			{
				string result = task.Result.Content.ReadAsStringAsync().Result;
				if (IsDebug && result != "")
				{
					log.Info("[ BB-Receive ]: " + result);
				}
				string[] array = result.Split(new char[1] { '|' });
				if (array.Length < 2)
				{
					return;
				}
				string text = array[0];
				string text2 = array[1];
				switch (text)
				{
				case "connect":
					sessId = text2;
					isConnected = true;
					DispatchEvent(new BBEvent(BBEvent.CONNECT));
					Poll(null);
					break;
				case "poll":
				{
					ByteArray value = null;
					if (text2 != "null")
					{
						value = DecodeResponse(text2);
					}
					if (isConnected)
					{
						pollTimer = new Timer(Poll, null, pollSpeed, -1);
					}
					if (text2 != "null")
					{
						Dictionary<string, object> dictionary = new Dictionary<string, object>();
						dictionary["data"] = value;
						DispatchEvent(new BBEvent(BBEvent.DATA, dictionary));
					}
					break;
				}
				case "err01":
					HandleConnectionLost(fireEvent: true);
					break;
				}
			}
			catch (AggregateException ex)
			{
				if (ex.InnerException is HttpRequestException)
				{
					OnHttpError("Error sending HTTP request to: " + bbUrl + " -- " + ex.InnerException.Message);
				}
				else
				{
					OnHttpError("Unexpected HTTP error: " + ex.InnerException.HelpLink + ", connecting to: " + bbUrl);
				}
			}
		}

		private void Poll(object state)
		{
			if (isConnected)
			{
				SendRequest("poll", null, pollingConnection);
			}
		}

		public void AddEventListener(string eventType, EventListenerDelegate listener)
		{
			dispatcher.AddEventListener(eventType, listener);
		}
	}
}
#else
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sfs2X.Core;
using Sfs2X.Logging;
using Sfs2X.Util;

namespace Sfs2X.Bitswarm.BBox
{
    public class BBClientV2 : IBBClient, IDispatchable
    {
        public const string BB_SERVLET = "/BlueBox/BlueBox.do";

        private const string BB_NULL = "null";

        private const string CMD_CONNECT = "connect";

        private const string CMD_POLL = "poll";

        private const string CMD_DATA = "data";

        private const string CMD_DISCONNECT = "disconnect";

        private const string ERR_INVALID_SESSION = "err01";

        private const string SFS_HTTP = "sfsHttp";

        private const char SEP = '|';

        private const int MIN_POLL_SPEED = 50;

        private const int MAX_POLL_SPEED = 5000;

        private const int DEFAULT_POLL_SPEED = 300;

        private const int HTTP_CONN_TIMEOUT = 30;

        private bool isConnected = false;

        private string bbUrl;

        private string sessId;

        private int pollSpeed = 300;

        private Timer pollTimer = null;

        private EventDispatcher dispatcher;

        private Logger log;

        private HttpClient reqConnection;

        private HttpClient pollingConnection;

        private ConfigData cfg;

        public bool IsDebug { get; set; } = false;

        public bool IsConnected => sessId != null;

        public string SessionId => sessId;

        public EventDispatcher Dispatcher => dispatcher;

        public BBClientV2(Logger logger)
        {
            log = logger;
            if (dispatcher == null)
            {
                dispatcher = new EventDispatcher(this);
            }
        }

        public void Connect(ConfigData cfg)
        {
            if (isConnected)
            {
                throw new Exception("BlueBox session is already connected");
            }
            this.cfg = cfg;
            ValidatePollingRate();
            bbUrl = GetBBUrl();
            if (IsDebug)
            {
                log.Info("[ BB-Connect ]: " + bbUrl);
            }
            reqConnection = GetConnection();
            reqConnection.Timeout = TimeSpan.FromSeconds(30.0);
            reqConnection.BaseAddress = new Uri(bbUrl);
            pollingConnection = GetConnection();
            pollingConnection.Timeout = TimeSpan.FromSeconds(30.0);
            pollingConnection.BaseAddress = new Uri(bbUrl);
            SendRequest("connect", null);
        }

        public void Send(ByteArray binData)
        {
            if (!isConnected)
            {
                throw new Exception("Can't send data. There is no active BlueBox connection");
            }
            SendRequest("data", binData);
        }

        public void Close(string reason)
        {
            HandleConnectionLost(fireEvent: true, reason);
        }

        private HttpClient GetConnection()
        {
            HttpClient httpClient = null;
            if (cfg.BlueBox.Proxy.Host != null && cfg.BlueBox.Proxy.Port > 0)
            {
                string address = cfg.BlueBox.Proxy.Host + ":" + cfg.BlueBox.Proxy.Port;
                WebProxy proxy = new WebProxy(address, cfg.BlueBox.Proxy.BypassLocal);
                HttpClientHandler httpClientHandler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true
                };
                if (cfg.BlueBox.Proxy.UserName != null)
                {
                    httpClientHandler.Credentials = new NetworkCredential(cfg.BlueBox.Proxy.UserName, cfg.BlueBox.Proxy.Password);
                }
                return new HttpClient(httpClientHandler);
            }
            return new HttpClient();
        }

        private void ValidatePollingRate()
        {
            if (cfg.BlueBox.PollingRate < 50)
            {
                cfg.BlueBox.PollingRate = 50;
            }
            else if (cfg.BlueBox.PollingRate > 5000)
            {
                cfg.BlueBox.PollingRate = 5000;
            }
        }

        private string GetBBUrl()
        {
            string text = (cfg.BlueBox.UseHttps ? "https://" : "http://");
            int num = (cfg.BlueBox.UseHttps ? cfg.HttpsPort : cfg.HttpPort);
            return text + cfg.Host + ":" + num;
        }

        private string EncodeRequest(string cmd)
        {
            return EncodeRequest(cmd, null);
        }

        private string EncodeRequest(string cmd, object data)
        {
            string text = "";
            string text2 = "";
            if (cmd == null)
            {
                cmd = "null";
            }
            if (data == null)
            {
                text2 = "null";
            }
            else if (data is ByteArray)
            {
                text2 = Convert.ToBase64String(((ByteArray)data).Bytes);
            }
            return ((sessId == null) ? "null" : sessId) + Convert.ToString('|') + cmd + Convert.ToString('|') + text2;
        }

        private ByteArray DecodeResponse(string rawData)
        {
            return new ByteArray(Convert.FromBase64String(rawData));
        }

        private void SendRequest(string cmd, object data, HttpClient client = null)
        {
            HttpClient httpClient = ((client == null) ? reqConnection : pollingConnection);
            string text = EncodeRequest(cmd, data);
            if (IsDebug)
            {
                log.Info("[ BB-Send ]: " + text);
            }
            FormUrlEncodedContent content = new FormUrlEncodedContent(new KeyValuePair<string, string>[1]
            {
                new KeyValuePair<string, string>("sfsHttp", text)
            });
            try
            {
                httpClient.PostAsync("/BlueBox/BlueBox.do", content).ContinueWith(OnHttpResponse);
            }
            catch (TaskCanceledException)
            {
                OnHttpError("Connection timeout. Server is not reachable at: " + bbUrl);
            }
            catch (HttpRequestException ex2)
            {
                OnHttpError("Error sending HTTP request to: " + bbUrl + " -- " + ex2.Message);
            }
        }

        private void OnHttpError(string message)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["message"] = message;
            HandleConnectionLost(fireEvent: true);
            DispatchEvent(new BBEvent(BBEvent.IO_ERROR, dictionary));
        }

        private void HandleConnectionLost(bool fireEvent, string reason = null)
        {
            if (!isConnected)
            {
                return;
            }
            isConnected = false;
            sessId = null;
            pollTimer.Dispose();
            if (fireEvent)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                if (reason != null)
                {
                    dictionary["reason"] = reason;
                }
                BBEvent evt = new BBEvent(BBEvent.DISCONNECT, dictionary);
                DispatchEvent(evt);
            }
        }

        private void DispatchEvent(BaseEvent evt)
        {
            dispatcher.DispatchEvent(evt);
        }

        private void OnHttpResponse(Task<HttpResponseMessage> task)
        {
            try
            {
                string result = task.Result.Content.ReadAsStringAsync().Result;
                if (IsDebug && result != "")
                {
                    log.Info("[ BB-Receive ]: " + result);
                }
                string[] array = result.Split(new char[1] { '|' });
                if (array.Length < 2)
                {
                    return;
                }
                string text = array[0];
                string text2 = array[1];
                switch (text)
                {
                    case "connect":
                        sessId = text2;
                        isConnected = true;
                        DispatchEvent(new BBEvent(BBEvent.CONNECT));
                        Poll(null);
                        break;
                    case "poll":
                        {
                            ByteArray value = null;
                            if (text2 != "null")
                            {
                                value = DecodeResponse(text2);
                            }
                            if (isConnected)
                            {
                                pollTimer = new Timer(Poll, null, pollSpeed, -1);
                            }
                            if (text2 != "null")
                            {
                                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                                dictionary["data"] = value;
                                DispatchEvent(new BBEvent(BBEvent.DATA, dictionary));
                            }
                            break;
                        }
                    case "err01":
                        HandleConnectionLost(fireEvent: true);
                        break;
                }
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is HttpRequestException)
                {
                    OnHttpError("Error sending HTTP request to: " + bbUrl + " -- " + ex.InnerException.Message);
                }
                else
                {
                    OnHttpError("Unexpected HTTP error: " + ex.InnerException.HelpLink + ", connecting to: " + bbUrl);
                }
            }
        }

        private void Poll(object state)
        {
            if (isConnected)
            {
                SendRequest("poll", null, pollingConnection);
            }
        }

        public void AddEventListener(string eventType, EventListenerDelegate listener)
        {
            dispatcher.AddEventListener(eventType, listener);
        }
    }
}
#endif