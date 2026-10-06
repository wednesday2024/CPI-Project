using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Sfs2X.WebSocketSharp.Net
{
	public class ClientSslConfiguration
	{
		private bool _checkCertRevocation;

		private LocalCertificateSelectionCallback _clientCertSelectionCallback;

		private X509CertificateCollection _clientCerts;

		private SslProtocols _enabledSslProtocols;

		private RemoteCertificateValidationCallback _serverCertValidationCallback;

		private string _targetHost;

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

		public X509CertificateCollection ClientCertificates
		{
			get
			{
				return _clientCerts;
			}
			set
			{
				_clientCerts = value;
			}
		}

		public LocalCertificateSelectionCallback ClientCertificateSelectionCallback
		{
			get
			{
				if (_clientCertSelectionCallback == null)
				{
					_clientCertSelectionCallback = defaultSelectClientCertificate;
				}
				return _clientCertSelectionCallback;
			}
			set
			{
				_clientCertSelectionCallback = value;
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

		public RemoteCertificateValidationCallback ServerCertificateValidationCallback
		{
			get
			{
				if (_serverCertValidationCallback == null)
				{
					_serverCertValidationCallback = defaultValidateServerCertificate;
				}
				return _serverCertValidationCallback;
			}
			set
			{
				_serverCertValidationCallback = value;
			}
		}

		public string TargetHost
		{
			get
			{
				return _targetHost;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (value.Length == 0)
				{
					throw new ArgumentException("An empty string.", "value");
				}
				_targetHost = value;
			}
		}

		public ClientSslConfiguration(string targetHost)
		{
			if (targetHost == null)
			{
				throw new ArgumentNullException("targetHost");
			}
			if (targetHost.Length == 0)
			{
				throw new ArgumentException("An empty string.", "targetHost");
			}
			_targetHost = targetHost;
			_enabledSslProtocols = SslProtocols.None;
		}

		public ClientSslConfiguration(ClientSslConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}
			_checkCertRevocation = configuration._checkCertRevocation;
			_clientCertSelectionCallback = configuration._clientCertSelectionCallback;
			_clientCerts = configuration._clientCerts;
			_enabledSslProtocols = configuration._enabledSslProtocols;
			_serverCertValidationCallback = configuration._serverCertValidationCallback;
			_targetHost = configuration._targetHost;
		}

		private static X509Certificate defaultSelectClientCertificate(object sender, string targetHost, X509CertificateCollection clientCertificates, X509Certificate serverCertificate, string[] acceptableIssuers)
		{
			return null;
		}

		private static bool defaultValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}
	}
}
