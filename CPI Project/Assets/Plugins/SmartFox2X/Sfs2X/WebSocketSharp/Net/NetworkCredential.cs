#if UNITY_WEBGL
#else
using System;

namespace Sfs2X.WebSocketSharp.Net
{
	public class NetworkCredential
	{
		private string _domain;

		private static readonly string[] _noRoles;

		private string _password;

		private string[] _roles;

		private string _username;

		public string Domain
		{
			get
			{
				return _domain ?? string.Empty;
			}
			internal set
			{
				_domain = value;
			}
		}

		public string Password
		{
			get
			{
				return _password ?? string.Empty;
			}
			internal set
			{
				_password = value;
			}
		}

		public string[] Roles
		{
			get
			{
				return _roles ?? _noRoles;
			}
			internal set
			{
				_roles = value;
			}
		}

		public string Username
		{
			get
			{
				return _username;
			}
			internal set
			{
				_username = value;
			}
		}

		static NetworkCredential()
		{
			_noRoles = new string[0];
		}

		public NetworkCredential(string username, string password)
			: this(username, password, (string)null, (string[])null)
		{
		}

		public NetworkCredential(string username, string password, string domain, params string[] roles)
		{
			if (username == null)
			{
				throw new ArgumentNullException("username");
			}
			if (username.Length == 0)
			{
				throw new ArgumentException("An empty string.", "username");
			}
			_username = username;
			_password = password;
			_domain = domain;
			_roles = roles;
		}
	}
}
#endif