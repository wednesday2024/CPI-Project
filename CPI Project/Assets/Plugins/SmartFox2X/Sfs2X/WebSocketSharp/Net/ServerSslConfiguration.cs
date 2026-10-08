#if UNITY_WEBGL
#else
using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Sfs2X.WebSocketSharp.Net
{
	public class ServerSslConfiguration
	{
		private bool _checkCertRevocation;

		private bool _clientCertRequired;

		private RemoteCertificateValidationCallback _clientCertValidationCallback;

		private SslProtocols _enabledSslProtocols;

		private X509Certificate2 _serverCert;

		public bool CheckCertificateRevocation
		{
			get
			{
				return _checkCertRevocation;
			}
			set
			{
				_checkCertRevocation = value;
			}
		}

		public bool ClientCertificateRequired
		{
			get
			{
				return _clientCertRequired;
			}
			set
			{
				_clientCertRequired = value;
			}
		}

		public RemoteCertificateValidationCallback ClientCertificateValidationCallback
		{
			get
			{
				if (_clientCertValidationCallback == null)
				{
					_clientCertValidationCallback = defaultValidateClientCertificate;
				}
				return _clientCertValidationCallback;
			}
			set
			{
				_clientCertValidationCallback = value;
			}
		}

		public SslProtocols EnabledSslProtocols
		{
			get
			{
				return _enabledSslProtocols;
			}
			set
			{
				_enabledSslProtocols = value;
			}
		}

		public X509Certificate2 ServerCertificate
		{
			get
			{
				return _serverCert;
			}
			set
			{
				_serverCert = value;
			}
		}

		public ServerSslConfiguration()
		{
			_enabledSslProtocols = SslProtocols.None;
		}

		public ServerSslConfiguration(ServerSslConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}
			_checkCertRevocation = configuration._checkCertRevocation;
			_clientCertRequired = configuration._clientCertRequired;
			_clientCertValidationCallback = configuration._clientCertValidationCallback;
			_enabledSslProtocols = configuration._enabledSslProtocols;
			_serverCert = configuration._serverCert;
		}

		private static bool defaultValidateClientCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}
	}
}
#endif