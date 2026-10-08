#if UNITY_WEBGL
#else
using System;
using System.Globalization;
using System.Text;

namespace Sfs2X.WebSocketSharp.Net
{
	[Serializable]
	public sealed class Cookie
	{
		private string _comment;

		private Uri _commentUri;

		private bool _discard;

		private string _domain;

		private static readonly int[] _emptyPorts;

		private DateTime _expires;

		private bool _httpOnly;

		private string _name;

		private string _path;

		private string _port;

		private int[] _ports;

		private static readonly char[] _reservedCharsForValue;

		private string _sameSite;

		private bool _secure;

		private DateTime _timeStamp;

		private string _value;

		private int _version;

		internal bool ExactDomain => _domain.Length == 0 || _domain[0] != '.';

		internal int MaxAge
		{
			get
			{
				if (_expires == DateTime.MinValue)
				{
					return 0;
				}
				DateTime dateTime = ((_expires.Kind != DateTimeKind.Local) ? _expires.ToLocalTime() : _expires);
				TimeSpan timeSpan = dateTime - DateTime.Now;
				return (timeSpan > TimeSpan.Zero) ? ((int)timeSpan.TotalSeconds) : 0;
			}
			set
			{
				_expires = ((value > 0) ? DateTime.Now.AddSeconds(value) : DateTime.Now);
			}
		}

		internal int[] Ports => _ports ?? _emptyPorts;

		internal string SameSite
		{
			get
			{
				return _sameSite;
			}
			set
			{
				_sameSite = value;
			}
		}

		public string Comment
		{
			get
			{
				return _comment;
			}
			internal set
			{
				_comment = value;
			}
		}

		public Uri CommentUri
		{
			get
			{
				return _commentUri;
			}
			internal set
			{
				_commentUri = value;
			}
		}

		public bool Discard
		{
			get
			{
				return _discard;
			}
			internal set
			{
				_discard = value;
			}
		}

		public string Domain
		{
			get
			{
				return _domain;
			}
			set
			{
				_domain = value ?? string.Empty;
			}
		}

		public bool Expired
		{
			get
			{
				return _expires != DateTime.MinValue && _expires <= DateTime.Now;
			}
			set
			{
				_expires = (value ? DateTime.Now : DateTime.MinValue);
			}
		}

		public DateTime Expires
		{
			get
			{
				return _expires;
			}
			set
			{
				_expires = value;
			}
		}

		public bool HttpOnly
		{
			get
			{
				return _httpOnly;
			}
			set
			{
				_httpOnly = value;
			}
		}

		public string Name
		{
			get
			{
				return _name;
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
				if (value[0] == '$')
				{
					string message = "It starts with a dollar sign.";
					throw new ArgumentException(message, "value");
				}
				if (!value.IsToken())
				{
					string message2 = "It contains an invalid character.";
					throw new ArgumentException(message2, "value");
				}
				_name = value;
			}
		}

		public string Path
		{
			get
			{
				return _path;
			}
			set
			{
				_path = value ?? string.Empty;
			}
		}

		public string Port
		{
			get
			{
				return _port;
			}
			internal set
			{
				if (tryCreatePorts(value, out var result))
				{
					_port = value;
					_ports = result;
				}
			}
		}

		public bool Secure
		{
			get
			{
				return _secure;
			}
			set
			{
				_secure = value;
			}
		}

		public DateTime TimeStamp => _timeStamp;

		public string Value
		{
			get
			{
				return _value;
			}
			set
			{
				if (value == null)
				{
					value = string.Empty;
				}
				if (value.Contains(_reservedCharsForValue) && !value.IsEnclosedIn('"'))
				{
					string message = "A string not enclosed in double quotes.";
					throw new ArgumentException(message, "value");
				}
				_value = value;
			}
		}

		public int Version
		{
			get
			{
				return _version;
			}
			internal set
			{
				if (value >= 0 && value <= 1)
				{
					_version = value;
				}
			}
		}

		static Cookie()
		{
			_emptyPorts = new int[0];
			_reservedCharsForValue = new char[2] { ';', ',' };
		}

		internal Cookie()
		{
			init(string.Empty, string.Empty, string.Empty, string.Empty);
		}

		public Cookie(string name, string value)
			: this(name, value, string.Empty, string.Empty)
		{
		}

		public Cookie(string name, string value, string path)
			: this(name, value, path, string.Empty)
		{
		}

		public Cookie(string name, string value, string path, string domain)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (name.Length == 0)
			{
				throw new ArgumentException("An empty string.", "name");
			}
			if (name[0] == '$')
			{
				string message = "It starts with a dollar sign.";
				throw new ArgumentException(message, "name");
			}
			if (!name.IsToken())
			{
				string message2 = "It contains an invalid character.";
				throw new ArgumentException(message2, "name");
			}
			if (value == null)
			{
				value = string.Empty;
			}
			if (value.Contains(_reservedCharsForValue) && !value.IsEnclosedIn('"'))
			{
				string message3 = "A string not enclosed in double quotes.";
				throw new ArgumentException(message3, "value");
			}
			init(name, value, path ?? string.Empty, domain ?? string.Empty);
		}

		private static int hash(int i, int j, int k, int l, int m)
		{
			return i ^ ((j << 13) | (j >> 19)) ^ ((k << 26) | (k >> 6)) ^ ((l << 7) | (l >> 25)) ^ ((m << 20) | (m >> 12));
		}

		private void init(string name, string value, string path, string domain)
		{
			_name = name;
			_value = value;
			_path = path;
			_domain = domain;
			_expires = DateTime.MinValue;
			_timeStamp = DateTime.Now;
		}

		private string toResponseStringVersion0()
		{
			StringBuilder stringBuilder = new StringBuilder(64);
			stringBuilder.AppendFormat("{0}={1}", _name, _value);
			if (_expires != DateTime.MinValue)
			{
				stringBuilder.AppendFormat("; Expires={0}", _expires.ToUniversalTime().ToString("ddd, dd'-'MMM'-'yyyy HH':'mm':'ss 'GMT'", CultureInfo.CreateSpecificCulture("en-US")));
			}
			if (!_path.IsNullOrEmpty())
			{
				stringBuilder.AppendFormat("; Path={0}", _path);
			}
			if (!_domain.IsNullOrEmpty())
			{
				stringBuilder.AppendFormat("; Domain={0}", _domain);
			}
			if (!_sameSite.IsNullOrEmpty())
			{
				stringBuilder.AppendFormat("; SameSite={0}", _sameSite);
			}
			if (_secure)
			{
				stringBuilder.Append("; Secure");
			}
			if (_httpOnly)
			{
				stringBuilder.Append("; HttpOnly");
			}
			return stringBuilder.ToString();
		}

		private string toResponseStringVersion1()
		{
			StringBuilder stringBuilder = new StringBuilder(64);
			stringBuilder.AppendFormat("{0}={1}; Version={2}", _name, _value, _version);
			if (_expires != DateTime.MinValue)
			{
				stringBuilder.AppendFormat("; Max-Age={0}", MaxAge);
			}
			if (!_path.IsNullOrEmpty())
			{
				stringBuilder.AppendFormat("; Path={0}", _path);
			}
			if (!_domain.IsNullOrEmpty())
			{
				stringBuilder.AppendFormat("; Domain={0}", _domain);
			}
			if (_port != null)
			{
				if (_port != "\"\"")
				{
					stringBuilder.AppendFormat("; Port={0}", _port);
				}
				else
				{
					stringBuilder.Append("; Port");
				}
			}
			if (_comment != null)
			{
				stringBuilder.AppendFormat("; Comment={0}", HttpUtility.UrlEncode(_comment));
			}
			if (_commentUri != null)
			{
				string originalString = _commentUri.OriginalString;
				stringBuilder.AppendFormat("; CommentURL={0}", (!originalString.IsToken()) ? originalString.Quote() : originalString);
			}
			if (_discard)
			{
				stringBuilder.Append("; Discard");
			}
			if (_secure)
			{
				stringBuilder.Append("; Secure");
			}
			return stringBuilder.ToString();
		}

		private static bool tryCreatePorts(string value, out int[] result)
		{
			result = null;
			string[] array = value.Trim(new char[1] { '"' }).Split(new char[1] { ',' });
			int num = array.Length;
			int[] array2 = new int[num];
			for (int i = 0; i < num; i++)
			{
				string text = array[i].Trim();
				if (text.Length == 0)
				{
					array2[i] = int.MinValue;
				}
				else if (!int.TryParse(text, out array2[i]))
				{
					return false;
				}
			}
			result = array2;
			return true;
		}

		internal bool EqualsWithoutValue(Cookie cookie)
		{
			StringComparison comparisonType = StringComparison.InvariantCulture;
			StringComparison comparisonType2 = StringComparison.InvariantCultureIgnoreCase;
			return _name.Equals(cookie._name, comparisonType2) && _path.Equals(cookie._path, comparisonType) && _domain.Equals(cookie._domain, comparisonType2) && _version == cookie._version;
		}

		internal bool EqualsWithoutValueAndVersion(Cookie cookie)
		{
			StringComparison comparisonType = StringComparison.InvariantCulture;
			StringComparison comparisonType2 = StringComparison.InvariantCultureIgnoreCase;
			return _name.Equals(cookie._name, comparisonType2) && _path.Equals(cookie._path, comparisonType) && _domain.Equals(cookie._domain, comparisonType2);
		}

		internal string ToRequestString(Uri uri)
		{
			if (_name.Length == 0)
			{
				return string.Empty;
			}
			if (_version == 0)
			{
				return $"{_name}={_value}";
			}
			StringBuilder stringBuilder = new StringBuilder(64);
			stringBuilder.AppendFormat("$Version={0}; {1}={2}", _version, _name, _value);
			if (!_path.IsNullOrEmpty())
			{
				stringBuilder.AppendFormat("; $Path={0}", _path);
			}
			else if (uri != null)
			{
				stringBuilder.AppendFormat("; $Path={0}", uri.GetAbsolutePath());
			}
			else
			{
				stringBuilder.Append("; $Path=/");
			}
			if (!_domain.IsNullOrEmpty() && (uri == null || uri.Host != _domain))
			{
				stringBuilder.AppendFormat("; $Domain={0}", _domain);
			}
			if (_port != null)
			{
				if (_port != "\"\"")
				{
					stringBuilder.AppendFormat("; $Port={0}", _port);
				}
				else
				{
					stringBuilder.Append("; $Port");
				}
			}
			return stringBuilder.ToString();
		}

		internal string ToResponseString()
		{
			return (_name.Length == 0) ? string.Empty : ((_version == 0) ? toResponseStringVersion0() : toResponseStringVersion1());
		}

		internal static bool TryCreate(string name, string value, out Cookie result)
		{
			result = null;
			try
			{
				result = new Cookie(name, value);
			}
			catch
			{
				return false;
			}
			return true;
		}

		public override bool Equals(object comparand)
		{
			if (!(comparand is Cookie cookie))
			{
				return false;
			}
			StringComparison comparisonType = StringComparison.InvariantCulture;
			StringComparison comparisonType2 = StringComparison.InvariantCultureIgnoreCase;
			return _name.Equals(cookie._name, comparisonType2) && _value.Equals(cookie._value, comparisonType) && _path.Equals(cookie._path, comparisonType) && _domain.Equals(cookie._domain, comparisonType2) && _version == cookie._version;
		}

		public override int GetHashCode()
		{
			return hash(StringComparer.InvariantCultureIgnoreCase.GetHashCode(_name), _value.GetHashCode(), _path.GetHashCode(), StringComparer.InvariantCultureIgnoreCase.GetHashCode(_domain), _version);
		}

		public override string ToString()
		{
			return ToRequestString(null);
		}
	}
}
#endif