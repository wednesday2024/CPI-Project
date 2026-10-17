#if UNITY_WEBGL
#else
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Text;

namespace Sfs2X.WebSocketSharp.Net
{
	internal static class HttpUtility
	{
		private static Dictionary<string, char> _entities;

		private static char[] _hexChars;

		private static object _sync;

		static HttpUtility()
		{
			_hexChars = "0123456789ABCDEF".ToCharArray();
			_sync = new object();
		}

		private static Dictionary<string, char> getEntities()
		{
			lock (_sync)
			{
				if (_entities == null)
				{
					initEntities();
				}
				return _entities;
			}
		}

		private static int getNumber(char c)
		{
			return (c >= '0' && c <= '9') ? (c - 48) : ((c >= 'A' && c <= 'F') ? (c - 65 + 10) : ((c >= 'a' && c <= 'f') ? (c - 97 + 10) : (-1)));
		}

		private static int getNumber(byte[] bytes, int offset, int count)
		{
			int num = 0;
			int num2 = offset + count - 1;
			for (int i = offset; i <= num2; i++)
			{
				int number = getNumber((char)bytes[i]);
				if (number == -1)
				{
					return -1;
				}
				num = (num << 4) + number;
			}
			return num;
		}

		private static int getNumber(string s, int offset, int count)
		{
			int num = 0;
			int num2 = offset + count - 1;
			for (int i = offset; i <= num2; i++)
			{
				int number = getNumber(s[i]);
				if (number == -1)
				{
					return -1;
				}
				num = (num << 4) + number;
			}
			return num;
		}

		private static string htmlDecode(string s)
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num = 0;
			StringBuilder stringBuilder2 = new StringBuilder();
			int num2 = 0;
			foreach (char c in s)
			{
				if (num == 0)
				{
					if (c == '&')
					{
						stringBuilder2.Append('&');
						num = 1;
					}
					else
					{
						stringBuilder.Append(c);
					}
					continue;
				}
				if (c == '&')
				{
					stringBuilder.Append(stringBuilder2.ToString());
					stringBuilder2.Length = 0;
					stringBuilder2.Append('&');
					num = 1;
					continue;
				}
				stringBuilder2.Append(c);
				switch (num)
				{
				case 1:
					if (c == ';')
					{
						stringBuilder.Append(stringBuilder2.ToString());
						stringBuilder2.Length = 0;
						num = 0;
					}
					else
					{
						num2 = 0;
						num = ((c == '#') ? 3 : 2);
					}
					break;
				case 2:
					if (c == ';')
					{
						string text = stringBuilder2.ToString();
						string key = text.Substring(1, text.Length - 2);
						Dictionary<string, char> entities = getEntities();
						if (entities.ContainsKey(key))
						{
							stringBuilder.Append(entities[key]);
						}
						else
						{
							stringBuilder.Append(text);
						}
						stringBuilder2.Length = 0;
						num = 0;
					}
					break;
				case 3:
					switch (c)
					{
					case ';':
						if (stringBuilder2.Length > 3 && num2 < 65536)
						{
							stringBuilder.Append((char)num2);
						}
						else
						{
							stringBuilder.Append(stringBuilder2.ToString());
						}
						stringBuilder2.Length = 0;
						num = 0;
						break;
					case 'x':
						num = ((stringBuilder2.Length == 3) ? 4 : 2);
						break;
					default:
						if (!isNumeric(c))
						{
							num = 2;
						}
						else
						{
							num2 = num2 * 10 + (c - 48);
						}
						break;
					}
					break;
				case 4:
					if (c == ';')
					{
						if (stringBuilder2.Length > 4 && num2 < 65536)
						{
							stringBuilder.Append((char)num2);
						}
						else
						{
							stringBuilder.Append(stringBuilder2.ToString());
						}
						stringBuilder2.Length = 0;
						num = 0;
					}
					else
					{
						int number = getNumber(c);
						if (number == -1)
						{
							num = 2;
						}
						else
						{
							num2 = (num2 << 4) + number;
						}
					}
					break;
				}
			}
			if (stringBuilder2.Length > 0)
			{
				stringBuilder.Append(stringBuilder2.ToString());
			}
			return stringBuilder.ToString();
		}

		private static string htmlEncode(string s, bool minimal)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				stringBuilder.Append(c switch
				{
					'>' => "&gt;", 
					'<' => "&lt;", 
					'&' => "&amp;", 
					'"' => "&quot;", 
					_ => (!minimal && c > '\u009f') ? $"&#{(int)c};" : c.ToString(), 
				});
			}
			return stringBuilder.ToString();
		}

		private static void initEntities()
		{
			_entities = new Dictionary<string, char>();
			_entities.Add("nbsp", '\u00a0');
			_entities.Add("iexcl", '¡');
			_entities.Add("cent", '¢');
			_entities.Add("pound", '£');
			_entities.Add("curren", '¤');
			_entities.Add("yen", '¥');
			_entities.Add("brvbar", '¦');
			_entities.Add("sect", '§');
			_entities.Add("uml", '\u00a8');
			_entities.Add("copy", '©');
			_entities.Add("ordf", 'ª');
			_entities.Add("laquo", '«');
			_entities.Add("not", '¬');
			_entities.Add("shy", '\u00ad');
			_entities.Add("reg", '®');
			_entities.Add("macr", '\u00af');
			_entities.Add("deg", '°');
			_entities.Add("plusmn", '±');
			_entities.Add("sup2", '²');
			_entities.Add("sup3", '³');
			_entities.Add("acute", '\u00b4');
			_entities.Add("micro", 'µ');
			_entities.Add("para", '¶');
			_entities.Add("middot", '·');
			_entities.Add("cedil", '\u00b8');
			_entities.Add("sup1", '¹');
			_entities.Add("ordm", 'º');
			_entities.Add("raquo", '»');
			_entities.Add("frac14", '¼');
			_entities.Add("frac12", '½');
			_entities.Add("frac34", '¾');
			_entities.Add("iquest", '¿');
			_entities.Add("Agrave", 'À');
			_entities.Add("Aacute", 'Á');
			_entities.Add("Acirc", 'Â');
			_entities.Add("Atilde", 'Ã');
			_entities.Add("Auml", 'Ä');
			_entities.Add("Aring", 'Å');
			_entities.Add("AElig", 'Æ');
			_entities.Add("Ccedil", 'Ç');
			_entities.Add("Egrave", 'È');
			_entities.Add("Eacute", 'É');
			_entities.Add("Ecirc", 'Ê');
			_entities.Add("Euml", 'Ë');
			_entities.Add("Igrave", 'Ì');
			_entities.Add("Iacute", 'Í');
			_entities.Add("Icirc", 'Î');
			_entities.Add("Iuml", 'Ï');
			_entities.Add("ETH", 'Ð');
			_entities.Add("Ntilde", 'Ñ');
			_entities.Add("Ograve", 'Ò');
			_entities.Add("Oacute", 'Ó');
			_entities.Add("Ocirc", 'Ô');
			_entities.Add("Otilde", 'Õ');
			_entities.Add("Ouml", 'Ö');
			_entities.Add("times", '×');
			_entities.Add("Oslash", 'Ø');
			_entities.Add("Ugrave", 'Ù');
			_entities.Add("Uacute", 'Ú');
			_entities.Add("Ucirc", 'Û');
			_entities.Add("Uuml", 'Ü');
			_entities.Add("Yacute", 'Ý');
			_entities.Add("THORN", 'Þ');
			_entities.Add("szlig", 'ß');
			_entities.Add("agrave", 'à');
			_entities.Add("aacute", 'á');
			_entities.Add("acirc", 'â');
			_entities.Add("atilde", 'ã');
			_entities.Add("auml", 'ä');
			_entities.Add("aring", 'å');
			_entities.Add("aelig", 'æ');
			_entities.Add("ccedil", 'ç');
			_entities.Add("egrave", 'è');
			_entities.Add("eacute", 'é');
			_entities.Add("ecirc", 'ê');
			_entities.Add("euml", 'ë');
			_entities.Add("igrave", 'ì');
			_entities.Add("iacute", 'í');
			_entities.Add("icirc", 'î');
			_entities.Add("iuml", 'ï');
			_entities.Add("eth", 'ð');
			_entities.Add("ntilde", 'ñ');
			_entities.Add("ograve", 'ò');
			_entities.Add("oacute", 'ó');
			_entities.Add("ocirc", 'ô');
			_entities.Add("otilde", 'õ');
			_entities.Add("ouml", 'ö');
			_entities.Add("divide", '÷');
			_entities.Add("oslash", 'ø');
			_entities.Add("ugrave", 'ù');
			_entities.Add("uacute", 'ú');
			_entities.Add("ucirc", 'û');
			_entities.Add("uuml", 'ü');
			_entities.Add("yacute", 'ý');
			_entities.Add("thorn", 'þ');
			_entities.Add("yuml", 'ÿ');
			_entities.Add("fnof", 'ƒ');
			_entities.Add("Alpha", 'Α');
			_entities.Add("Beta", 'Β');
			_entities.Add("Gamma", 'Γ');
			_entities.Add("Delta", 'Δ');
			_entities.Add("Epsilon", 'Ε');
			_entities.Add("Zeta", 'Ζ');
			_entities.Add("Eta", 'Η');
			_entities.Add("Theta", 'Θ');
			_entities.Add("Iota", 'Ι');
			_entities.Add("Kappa", 'Κ');
			_entities.Add("Lambda", 'Λ');
			_entities.Add("Mu", 'Μ');
			_entities.Add("Nu", 'Ν');
			_entities.Add("Xi", 'Ξ');
			_entities.Add("Omicron", 'Ο');
			_entities.Add("Pi", 'Π');
			_entities.Add("Rho", 'Ρ');
			_entities.Add("Sigma", 'Σ');
			_entities.Add("Tau", 'Τ');
			_entities.Add("Upsilon", 'Υ');
			_entities.Add("Phi", 'Φ');
			_entities.Add("Chi", 'Χ');
			_entities.Add("Psi", 'Ψ');
			_entities.Add("Omega", 'Ω');
			_entities.Add("alpha", 'α');
			_entities.Add("beta", 'β');
			_entities.Add("gamma", 'γ');
			_entities.Add("delta", 'δ');
			_entities.Add("epsilon", 'ε');
			_entities.Add("zeta", 'ζ');
			_entities.Add("eta", 'η');
			_entities.Add("theta", 'θ');
			_entities.Add("iota", 'ι');
			_entities.Add("kappa", 'κ');
			_entities.Add("lambda", 'λ');
			_entities.Add("mu", 'μ');
			_entities.Add("nu", 'ν');
			_entities.Add("xi", 'ξ');
			_entities.Add("omicron", 'ο');
			_entities.Add("pi", 'π');
			_entities.Add("rho", 'ρ');
			_entities.Add("sigmaf", 'ς');
			_entities.Add("sigma", 'σ');
			_entities.Add("tau", 'τ');
			_entities.Add("upsilon", 'υ');
			_entities.Add("phi", 'φ');
			_entities.Add("chi", 'χ');
			_entities.Add("psi", 'ψ');
			_entities.Add("omega", 'ω');
			_entities.Add("thetasym", 'ϑ');
			_entities.Add("upsih", 'ϒ');
			_entities.Add("piv", 'ϖ');
			_entities.Add("bull", '•');
			_entities.Add("hellip", '…');
			_entities.Add("prime", '′');
			_entities.Add("Prime", '″');
			_entities.Add("oline", '‾');
			_entities.Add("frasl", '⁄');
			_entities.Add("weierp", '℘');
			_entities.Add("image", 'ℑ');
			_entities.Add("real", 'ℜ');
			_entities.Add("trade", '™');
			_entities.Add("alefsym", 'ℵ');
			_entities.Add("larr", '←');
			_entities.Add("uarr", '↑');
			_entities.Add("rarr", '→');
			_entities.Add("darr", '↓');
			_entities.Add("harr", '↔');
			_entities.Add("crarr", '↵');
			_entities.Add("lArr", '⇐');
			_entities.Add("uArr", '⇑');
			_entities.Add("rArr", '⇒');
			_entities.Add("dArr", '⇓');
			_entities.Add("hArr", '⇔');
			_entities.Add("forall", '∀');
			_entities.Add("part", '∂');
			_entities.Add("exist", '∃');
			_entities.Add("empty", '∅');
			_entities.Add("nabla", '∇');
			_entities.Add("isin", '∈');
			_entities.Add("notin", '∉');
			_entities.Add("ni", '∋');
			_entities.Add("prod", '∏');
			_entities.Add("sum", '∑');
			_entities.Add("minus", '−');
			_entities.Add("lowast", '∗');
			_entities.Add("radic", '√');
			_entities.Add("prop", '∝');
			_entities.Add("infin", '∞');
			_entities.Add("ang", '∠');
			_entities.Add("and", '∧');
			_entities.Add("or", '∨');
			_entities.Add("cap", '∩');
			_entities.Add("cup", '∪');
			_entities.Add("int", '∫');
			_entities.Add("there4", '∴');
			_entities.Add("sim", '∼');
			_entities.Add("cong", '≅');
			_entities.Add("asymp", '≈');
			_entities.Add("ne", '≠');
			_entities.Add("equiv", '≡');
			_entities.Add("le", '≤');
			_entities.Add("ge", '≥');
			_entities.Add("sub", '⊂');
			_entities.Add("sup", '⊃');
			_entities.Add("nsub", '⊄');
			_entities.Add("sube", '⊆');
			_entities.Add("supe", '⊇');
			_entities.Add("oplus", '⊕');
			_entities.Add("otimes", '⊗');
			_entities.Add("perp", '⊥');
			_entities.Add("sdot", '⋅');
			_entities.Add("lceil", '⌈');
			_entities.Add("rceil", '⌉');
			_entities.Add("lfloor", '⌊');
			_entities.Add("rfloor", '⌋');
			_entities.Add("lang", '〈');
			_entities.Add("rang", '〉');
			_entities.Add("loz", '◊');
			_entities.Add("spades", '♠');
			_entities.Add("clubs", '♣');
			_entities.Add("hearts", '♥');
			_entities.Add("diams", '♦');
			_entities.Add("quot", '"');
			_entities.Add("amp", '&');
			_entities.Add("lt", '<');
			_entities.Add("gt", '>');
			_entities.Add("OElig", 'Œ');
			_entities.Add("oelig", 'œ');
			_entities.Add("Scaron", 'Š');
			_entities.Add("scaron", 'š');
			_entities.Add("Yuml", 'Ÿ');
			_entities.Add("circ", 'ˆ');
			_entities.Add("tilde", '\u02dc');
			_entities.Add("ensp", '\u2002');
			_entities.Add("emsp", '\u2003');
			_entities.Add("thinsp", '\u2009');
			_entities.Add("zwnj", '\u200c');
			_entities.Add("zwj", '\u200d');
			_entities.Add("lrm", '\u200e');
			_entities.Add("rlm", '\u200f');
			_entities.Add("ndash", '–');
			_entities.Add("mdash", '—');
			_entities.Add("lsquo", '‘');
			_entities.Add("rsquo", '’');
			_entities.Add("sbquo", '‚');
			_entities.Add("ldquo", '“');
			_entities.Add("rdquo", '”');
			_entities.Add("bdquo", '„');
			_entities.Add("dagger", '†');
			_entities.Add("Dagger", '‡');
			_entities.Add("permil", '‰');
			_entities.Add("lsaquo", '‹');
			_entities.Add("rsaquo", '›');
			_entities.Add("euro", '€');
		}

		private static bool isAlphabet(char c)
		{
			return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
		}

		private static bool isNumeric(char c)
		{
			return c >= '0' && c <= '9';
		}

		private static bool isUnreserved(char c)
		{
			return c == '*' || c == '-' || c == '.' || c == '_';
		}

		private static bool isUnreservedInRfc2396(char c)
		{
			return c == '!' || c == '\'' || c == '(' || c == ')' || c == '*' || c == '-' || c == '.' || c == '_' || c == '~';
		}

		private static bool isUnreservedInRfc3986(char c)
		{
			return c == '-' || c == '.' || c == '_' || c == '~';
		}

		private static byte[] urlDecodeToBytes(byte[] bytes, int offset, int count)
		{
			using MemoryStream memoryStream = new MemoryStream();
			int num = offset + count - 1;
			for (int i = offset; i <= num; i++)
			{
				byte b = bytes[i];
				switch ((char)b)
				{
				case '%':
					if (i <= num - 2)
					{
						int number = getNumber(bytes, i + 1, 2);
						if (number != -1)
						{
							memoryStream.WriteByte((byte)number);
							i += 2;
							continue;
						}
					}
					break;
				case '+':
					memoryStream.WriteByte(32);
					continue;
				default:
					memoryStream.WriteByte(b);
					continue;
				}
				break;
			}
			memoryStream.Close();
			return memoryStream.ToArray();
		}

		private static void urlEncode(byte b, Stream output)
		{
			if (b > 31 && b < 127)
			{
				char c = (char)b;
				if (c == ' ')
				{
					output.WriteByte(43);
					return;
				}
				if (isNumeric(c))
				{
					output.WriteByte(b);
					return;
				}
				if (isAlphabet(c))
				{
					output.WriteByte(b);
					return;
				}
				if (isUnreserved(c))
				{
					output.WriteByte(b);
					return;
				}
			}
			byte[] buffer = new byte[3]
			{
				37,
				(byte)_hexChars[b >> 4],
				(byte)_hexChars[b & 0xF]
			};
			output.Write(buffer, 0, 3);
		}

		private static byte[] urlEncodeToBytes(byte[] bytes, int offset, int count)
		{
			using MemoryStream memoryStream = new MemoryStream();
			int num = offset + count - 1;
			for (int i = offset; i <= num; i++)
			{
				urlEncode(bytes[i], memoryStream);
			}
			memoryStream.Close();
			return memoryStream.ToArray();
		}

		internal static Uri CreateRequestUrl(string requestUri, string host, bool websocketRequest, bool secure)
		{
			if (requestUri == null || requestUri.Length == 0)
			{
				return null;
			}
			if (host == null || host.Length == 0)
			{
				return null;
			}
			string text = null;
			string arg = null;
			Uri result;
			bool num;
			if (requestUri.IndexOf('/') == 0)
			{
				arg = requestUri;
			}
			else
			{
				if (requestUri.MaybeUri())
				{
					if (!Uri.TryCreate(requestUri, UriKind.Absolute, out result))
					{
						return null;
					}
					text = result.Scheme;
					if (!websocketRequest)
					{
						if (!(text == "http"))
						{
							num = text == "https";
							goto IL_00c6;
						}
					}
					else if (!(text == "ws"))
					{
						num = text == "wss";
						goto IL_00c6;
					}
					goto IL_00db;
				}
				if (!(requestUri == "*"))
				{
					host = requestUri;
				}
			}
			goto IL_0109;
			IL_0109:
			if (text == null)
			{
				text = ((!websocketRequest) ? (secure ? "https" : "http") : (secure ? "wss" : "ws"));
			}
			if (host.IndexOf(':') == -1)
			{
				host = $"{host}:{(secure ? 443 : 80)}";
			}
			string uriString = $"{text}://{host}{arg}";
			Uri result2;
			return Uri.TryCreate(uriString, UriKind.Absolute, out result2) ? result2 : null;
			IL_00c6:
			if (!num)
			{
				return null;
			}
			goto IL_00db;
			IL_00db:
			host = result.Authority;
			arg = result.PathAndQuery;
			goto IL_0109;
		}

		internal static IPrincipal CreateUser(string response, AuthenticationSchemes scheme, string realm, string method, Func<IIdentity, NetworkCredential> credentialsFinder)
		{
			if (response == null || response.Length == 0)
			{
				return null;
			}
			switch (scheme)
			{
			case AuthenticationSchemes.Digest:
				if (realm == null || realm.Length == 0)
				{
					return null;
				}
				if (method == null || method.Length == 0)
				{
					return null;
				}
				break;
			default:
				return null;
			case AuthenticationSchemes.Basic:
				break;
			}
			if (credentialsFinder == null)
			{
				return null;
			}
			StringComparison comparisonType = StringComparison.OrdinalIgnoreCase;
			if (response.IndexOf(scheme.ToString(), comparisonType) != 0)
			{
				return null;
			}
			AuthenticationResponse authenticationResponse = AuthenticationResponse.Parse(response);
			if (authenticationResponse == null)
			{
				return null;
			}
			IIdentity identity = authenticationResponse.ToIdentity();
			if (identity == null)
			{
				return null;
			}
			NetworkCredential networkCredential = null;
			try
			{
				networkCredential = credentialsFinder(identity);
			}
			catch
			{
			}
			if (networkCredential == null)
			{
				return null;
			}
			if (scheme == AuthenticationSchemes.Basic)
			{
				HttpBasicIdentity httpBasicIdentity = (HttpBasicIdentity)identity;
				return (httpBasicIdentity.Password == networkCredential.Password) ? new GenericPrincipal(identity, networkCredential.Roles) : null;
			}
			HttpDigestIdentity httpDigestIdentity = (HttpDigestIdentity)identity;
			return httpDigestIdentity.IsValid(networkCredential.Password, realm, method, null) ? new GenericPrincipal(identity, networkCredential.Roles) : null;
		}

		internal static Encoding GetEncoding(string contentType)
		{
			string value = "charset=";
			StringComparison comparisonType = StringComparison.OrdinalIgnoreCase;
			foreach (string item in contentType.SplitHeaderValue(';'))
			{
				string text = item.Trim();
				if (!text.StartsWith(value, comparisonType))
				{
					continue;
				}
				string value2 = text.GetValue('=', unquote: true);
				if (value2 == null || value2.Length == 0)
				{
					return null;
				}
				return Encoding.GetEncoding(value2);
			}
			return null;
		}

		internal static bool TryGetEncoding(string contentType, out Encoding result)
		{
			result = null;
			try
			{
				result = GetEncoding(contentType);
			}
			catch
			{
				return false;
			}
			return result != null;
		}

		public static string HtmlAttributeEncode(string s)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			return (s.Length > 0) ? htmlEncode(s, minimal: true) : s;
		}

		public static void HtmlAttributeEncode(string s, TextWriter output)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (output == null)
			{
				throw new ArgumentNullException("output");
			}
			if (s.Length != 0)
			{
				output.Write(htmlEncode(s, minimal: true));
			}
		}

		public static string HtmlDecode(string s)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			return (s.Length > 0) ? htmlDecode(s) : s;
		}

		public static void HtmlDecode(string s, TextWriter output)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (output == null)
			{
				throw new ArgumentNullException("output");
			}
			if (s.Length != 0)
			{
				output.Write(htmlDecode(s));
			}
		}

		public static string HtmlEncode(string s)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			return (s.Length > 0) ? htmlEncode(s, minimal: false) : s;
		}

		public static void HtmlEncode(string s, TextWriter output)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (output == null)
			{
				throw new ArgumentNullException("output");
			}
			if (s.Length != 0)
			{
				output.Write(htmlEncode(s, minimal: false));
			}
		}

		public static string UrlDecode(string s)
		{
			return UrlDecode(s, Encoding.UTF8);
		}

		public static string UrlDecode(byte[] bytes, Encoding encoding)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			int num = bytes.Length;
			return (num > 0) ? (encoding ?? Encoding.UTF8).GetString(urlDecodeToBytes(bytes, 0, num)) : string.Empty;
		}

		public static string UrlDecode(string s, Encoding encoding)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (s.Length == 0)
			{
				return s;
			}
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			return (encoding ?? Encoding.UTF8).GetString(urlDecodeToBytes(bytes, 0, bytes.Length));
		}

		public static string UrlDecode(byte[] bytes, int offset, int count, Encoding encoding)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			int num = bytes.Length;
			if (num == 0)
			{
				if (offset != 0)
				{
					throw new ArgumentOutOfRangeException("offset");
				}
				if (count != 0)
				{
					throw new ArgumentOutOfRangeException("count");
				}
				return string.Empty;
			}
			if (offset < 0 || offset >= num)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || count > num - offset)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			return (count > 0) ? (encoding ?? Encoding.UTF8).GetString(urlDecodeToBytes(bytes, offset, count)) : string.Empty;
		}

		public static byte[] UrlDecodeToBytes(byte[] bytes)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			int num = bytes.Length;
			return (num > 0) ? urlDecodeToBytes(bytes, 0, num) : bytes;
		}

		public static byte[] UrlDecodeToBytes(string s)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (s.Length == 0)
			{
				return new byte[0];
			}
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			return urlDecodeToBytes(bytes, 0, bytes.Length);
		}

		public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			int num = bytes.Length;
			if (num == 0)
			{
				if (offset != 0)
				{
					throw new ArgumentOutOfRangeException("offset");
				}
				if (count != 0)
				{
					throw new ArgumentOutOfRangeException("count");
				}
				return bytes;
			}
			if (offset < 0 || offset >= num)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || count > num - offset)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			return (count > 0) ? urlDecodeToBytes(bytes, offset, count) : new byte[0];
		}

		public static string UrlEncode(byte[] bytes)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			int num = bytes.Length;
			return (num > 0) ? Encoding.ASCII.GetString(urlEncodeToBytes(bytes, 0, num)) : string.Empty;
		}

		public static string UrlEncode(string s)
		{
			return UrlEncode(s, Encoding.UTF8);
		}

		public static string UrlEncode(string s, Encoding encoding)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			int length = s.Length;
			if (length == 0)
			{
				return s;
			}
			if (encoding == null)
			{
				encoding = Encoding.UTF8;
			}
			byte[] bytes = new byte[encoding.GetMaxByteCount(length)];
			int bytes2 = encoding.GetBytes(s, 0, length, bytes, 0);
			return Encoding.ASCII.GetString(urlEncodeToBytes(bytes, 0, bytes2));
		}

		public static string UrlEncode(byte[] bytes, int offset, int count)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			int num = bytes.Length;
			if (num == 0)
			{
				if (offset != 0)
				{
					throw new ArgumentOutOfRangeException("offset");
				}
				if (count != 0)
				{
					throw new ArgumentOutOfRangeException("count");
				}
				return string.Empty;
			}
			if (offset < 0 || offset >= num)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || count > num - offset)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			return (count > 0) ? Encoding.ASCII.GetString(urlEncodeToBytes(bytes, offset, count)) : string.Empty;
		}

		public static byte[] UrlEncodeToBytes(byte[] bytes)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			int num = bytes.Length;
			return (num > 0) ? urlEncodeToBytes(bytes, 0, num) : bytes;
		}

		public static byte[] UrlEncodeToBytes(string s)
		{
			return UrlEncodeToBytes(s, Encoding.UTF8);
		}

		public static byte[] UrlEncodeToBytes(string s, Encoding encoding)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (s.Length == 0)
			{
				return new byte[0];
			}
			byte[] bytes = (encoding ?? Encoding.UTF8).GetBytes(s);
			return urlEncodeToBytes(bytes, 0, bytes.Length);
		}

		public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes");
			}
			int num = bytes.Length;
			if (num == 0)
			{
				if (offset != 0)
				{
					throw new ArgumentOutOfRangeException("offset");
				}
				if (count != 0)
				{
					throw new ArgumentOutOfRangeException("count");
				}
				return bytes;
			}
			if (offset < 0 || offset >= num)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || count > num - offset)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			return (count > 0) ? urlEncodeToBytes(bytes, offset, count) : new byte[0];
		}
	}
}
#endif