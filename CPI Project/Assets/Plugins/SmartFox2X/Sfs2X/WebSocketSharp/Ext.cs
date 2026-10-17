#if UNITY_WEBGL
#else
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Sfs2X.WebSocketSharp.Net;

namespace Sfs2X.WebSocketSharp
{
	public static class Ext
	{
		private static readonly byte[] _last = new byte[1];

		private static readonly int _maxRetry = 5;

		private const string _tspecials = "()<>@,;:\\\"/[]?={} \t";

		private static byte[] compress(this byte[] data)
		{
			if (data.LongLength == 0)
			{
				return data;
			}
			using MemoryStream stream = new MemoryStream(data);
			return stream.compressToArray();
		}

		private static MemoryStream compress(this Stream stream)
		{
			MemoryStream memoryStream = new MemoryStream();
			if (stream.Length == 0)
			{
				return memoryStream;
			}
			stream.Position = 0L;
			CompressionMode mode = CompressionMode.Compress;
			using DeflateStream deflateStream = new DeflateStream(memoryStream, mode, leaveOpen: true);
			stream.CopyTo(deflateStream, 1024);
			deflateStream.Close();
			memoryStream.Write(_last, 0, 1);
			memoryStream.Position = 0L;
			return memoryStream;
		}

		private static byte[] compressToArray(this Stream stream)
		{
			using MemoryStream memoryStream = stream.compress();
			memoryStream.Close();
			return memoryStream.ToArray();
		}

		private static byte[] decompress(this byte[] data)
		{
			if (data.LongLength == 0)
			{
				return data;
			}
			using MemoryStream stream = new MemoryStream(data);
			return stream.decompressToArray();
		}

		private static MemoryStream decompress(this Stream stream)
		{
			MemoryStream memoryStream = new MemoryStream();
			if (stream.Length == 0)
			{
				return memoryStream;
			}
			stream.Position = 0L;
			CompressionMode mode = CompressionMode.Decompress;
			using DeflateStream deflateStream = new DeflateStream(stream, mode, leaveOpen: true);
			deflateStream.CopyTo(memoryStream, 1024);
			memoryStream.Position = 0L;
			return memoryStream;
		}

		private static byte[] decompressToArray(this Stream stream)
		{
			using MemoryStream memoryStream = stream.decompress();
			memoryStream.Close();
			return memoryStream.ToArray();
		}

		private static bool isPredefinedScheme(this string value)
		{
			switch (value[0])
			{
			case 'h':
				return value == "http" || value == "https";
			case 'w':
				return value == "ws" || value == "wss";
			case 'f':
				return value == "file" || value == "ftp";
			case 'g':
				return value == "gopher";
			case 'm':
				return value == "mailto";
			case 'n':
			{
				char c = value[1];
				return (c != 'e') ? (value == "nntp") : (value == "news" || value == "net.pipe" || value == "net.tcp");
			}
			default:
				return false;
			}
		}

		internal static byte[] Append(this ushort code, string reason)
		{
			byte[] array = code.ToByteArray(ByteOrder.Big);
			if (reason == null || reason.Length == 0)
			{
				return array;
			}
			List<byte> list = new List<byte>(array);
			byte[] bytes = Encoding.UTF8.GetBytes(reason);
			list.AddRange(bytes);
			return list.ToArray();
		}

		internal static byte[] Compress(this byte[] data, CompressionMethod method)
		{
			return (method == CompressionMethod.Deflate) ? data.compress() : data;
		}

		internal static Stream Compress(this Stream stream, CompressionMethod method)
		{
			return (method == CompressionMethod.Deflate) ? stream.compress() : stream;
		}

		internal static bool Contains(this string value, params char[] anyOf)
		{
			return anyOf != null && anyOf.Length != 0 && value.IndexOfAny(anyOf) > -1;
		}

		internal static bool Contains(this NameValueCollection collection, string name)
		{
			return collection[name] != null;
		}

		internal static bool Contains(this NameValueCollection collection, string name, string value, StringComparison comparisonTypeForValue)
		{
			string text = collection[name];
			if (text == null)
			{
				return false;
			}
			string[] array = text.Split(new char[1] { ',' });
			foreach (string text2 in array)
			{
				if (text2.Trim().Equals(value, comparisonTypeForValue))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool Contains<T>(this IEnumerable<T> source, Func<T, bool> condition)
		{
			foreach (T item in source)
			{
				if (condition(item))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool ContainsTwice(this string[] values)
		{
			int len = values.Length;
			int end = len - 1;
			Func<int, bool> seek = null;
			seek = (int idx) =>
			{
				if (idx == end)
				{
					return false;
				}
				string text = values[idx];
				for (int i = idx + 1; i < len; i++)
				{
					if (values[i] == text)
					{
						return true;
					}
				}
				return seek(++idx);
			};
			return seek(0);
		}

		internal static T[] Copy<T>(this T[] sourceArray, int length)
		{
			T[] array = new T[length];
			Array.Copy(sourceArray, 0, array, 0, length);
			return array;
		}

		internal static T[] Copy<T>(this T[] sourceArray, long length)
		{
			T[] array = new T[length];
			Array.Copy(sourceArray, 0L, array, 0L, length);
			return array;
		}

		internal static void CopyTo(this Stream sourceStream, Stream destinationStream, int bufferLength)
		{
			byte[] buffer = new byte[bufferLength];
			while (true)
			{
				int num = sourceStream.Read(buffer, 0, bufferLength);
				if (num <= 0)
				{
					break;
				}
				destinationStream.Write(buffer, 0, num);
			}
		}

		internal static void CopyToAsync(this Stream sourceStream, Stream destinationStream, int bufferLength, Action completed, Action<Exception> error)
		{
			byte[] buff = new byte[bufferLength];
			AsyncCallback callback = null;
			callback = (IAsyncResult ar) =>
			{
				try
				{
					int num = sourceStream.EndRead(ar);
					if (num <= 0)
					{
						if (completed != null)
						{
							completed();
						}
					}
					else
					{
						destinationStream.Write(buff, 0, num);
						sourceStream.BeginRead(buff, 0, bufferLength, callback, null);
					}
				}
				catch (Exception obj2)
				{
					if (error != null)
					{
						error(obj2);
					}
				}
			};
			try
			{
				sourceStream.BeginRead(buff, 0, bufferLength, callback, null);
			}
			catch (Exception obj)
			{
				if (error != null)
				{
					error(obj);
				}
			}
		}

		internal static byte[] Decompress(this byte[] data, CompressionMethod method)
		{
			return (method == CompressionMethod.Deflate) ? data.decompress() : data;
		}

		internal static Stream Decompress(this Stream stream, CompressionMethod method)
		{
			return (method == CompressionMethod.Deflate) ? stream.decompress() : stream;
		}

		internal static byte[] DecompressToArray(this Stream stream, CompressionMethod method)
		{
			return (method == CompressionMethod.Deflate) ? stream.decompressToArray() : stream.ToByteArray();
		}

		internal static void Emit(this EventHandler eventHandler, object sender, EventArgs e)
		{
			eventHandler?.Invoke(sender, e);
		}

		internal static void Emit<TEventArgs>(this EventHandler<TEventArgs> eventHandler, object sender, TEventArgs e) where TEventArgs : EventArgs
		{
			eventHandler?.Invoke(sender, e);
		}

		internal static string GetAbsolutePath(this Uri uri)
		{
			if (uri.IsAbsoluteUri)
			{
				return uri.AbsolutePath;
			}
			string originalString = uri.OriginalString;
			if (originalString[0] != '/')
			{
				return null;
			}
			int num = originalString.IndexOfAny(new char[2] { '?', '#' });
			return (num > 0) ? originalString.Substring(0, num) : originalString;
		}

		internal static Sfs2X.WebSocketSharp.Net.CookieCollection GetCookies(this NameValueCollection headers, bool response)
		{
			string text = headers[response ? "Set-Cookie" : "Cookie"];
			return (text != null) ? Sfs2X.WebSocketSharp.Net.CookieCollection.Parse(text, response) : new Sfs2X.WebSocketSharp.Net.CookieCollection();
		}

		internal static string GetDnsSafeHost(this Uri uri, bool bracketIPv6)
		{
			return (bracketIPv6 && uri.HostNameType == UriHostNameType.IPv6) ? uri.Host : uri.DnsSafeHost;
		}

		internal static string GetErrorMessage(this ushort code)
		{
			return code switch
			{
				1002 => "A protocol error has occurred.", 
				1003 => "Unsupported data has been received.", 
				1006 => "An abnormal error has occurred.", 
				1007 => "Invalid data has been received.", 
				1008 => "A policy violation has occurred.", 
				1009 => "A too big message has been received.", 
				1010 => "The client did not receive expected extension(s).", 
				1011 => "The server got an internal error.", 
				1015 => "An error has occurred during a TLS handshake.", 
				_ => string.Empty, 
			};
		}

		internal static string GetErrorMessage(this CloseStatusCode code)
		{
			return ((ushort)code).GetErrorMessage();
		}

		internal static string GetName(this string nameAndValue, char separator)
		{
			int num = nameAndValue.IndexOf(separator);
			return (num > 0) ? nameAndValue.Substring(0, num).Trim() : null;
		}

		internal static string GetUTF8DecodedString(this byte[] bytes)
		{
			try
			{
				return Encoding.UTF8.GetString(bytes);
			}
			catch
			{
				return null;
			}
		}

		internal static byte[] GetUTF8EncodedBytes(this string s)
		{
			try
			{
				return Encoding.UTF8.GetBytes(s);
			}
			catch
			{
				return null;
			}
		}

		internal static string GetValue(this string nameAndValue, char separator)
		{
			return nameAndValue.GetValue(separator, unquote: false);
		}

		internal static string GetValue(this string nameAndValue, char separator, bool unquote)
		{
			int num = nameAndValue.IndexOf(separator);
			if (num < 0 || num == nameAndValue.Length - 1)
			{
				return null;
			}
			string text = nameAndValue.Substring(num + 1).Trim();
			return unquote ? text.Unquote() : text;
		}

		internal static bool IsCompressionExtension(this string value, CompressionMethod method)
		{
			string value2 = method.ToExtensionString();
			StringComparison comparisonType = StringComparison.Ordinal;
			return value.StartsWith(value2, comparisonType);
		}

		internal static bool IsEqualTo(this int value, char c, Action<int> beforeComparing)
		{
			beforeComparing(value);
			return value == c;
		}

		internal static bool IsHttpMethod(this string value)
		{
			int result;
			switch (value)
			{
			default:
				result = ((value == "TRACE") ? 1 : 0);
				break;
			case "GET":
			case "HEAD":
			case "POST":
			case "PUT":
			case "DELETE":
			case "CONNECT":
			case "OPTIONS":
				result = 1;
				break;
			}
			return (byte)result != 0;
		}

		internal static bool IsPortNumber(this int value)
		{
			return value > 0 && value < 65536;
		}

		internal static bool IsReserved(this CloseStatusCode code)
		{
			return ((ushort)code).IsReservedStatusCode();
		}

		internal static bool IsReservedStatusCode(this ushort code)
		{
			return code == 1004 || code == 1005 || code == 1006 || code == 1015;
		}

		internal static bool IsSupportedOpcode(this byte opcode)
		{
			return Enum.IsDefined(typeof(Opcode), opcode);
		}

		internal static bool IsText(this string value)
		{
			int length = value.Length;
			for (int i = 0; i < length; i++)
			{
				char c = value[i];
				if (c < ' ')
				{
					if ("\r\n\t".IndexOf(c) == -1)
					{
						return false;
					}
					if (c == '\n')
					{
						i++;
						if (i == length)
						{
							break;
						}
						c = value[i];
						if (" \t".IndexOf(c) == -1)
						{
							return false;
						}
					}
				}
				else if (c == '\u007f')
				{
					return false;
				}
			}
			return true;
		}

		internal static bool IsToken(this string value)
		{
			foreach (char c in value)
			{
				if (c < ' ')
				{
					return false;
				}
				if (c > '~')
				{
					return false;
				}
				if ("()<>@,;:\\\"/[]?={} \t".IndexOf(c) > -1)
				{
					return false;
				}
			}
			return true;
		}

		internal static bool KeepsAlive(this NameValueCollection headers, Version version)
		{
			StringComparison comparisonTypeForValue = StringComparison.OrdinalIgnoreCase;
			return (version < Sfs2X.WebSocketSharp.Net.HttpVersion.Version11) ? headers.Contains("Connection", "keep-alive", comparisonTypeForValue) : (!headers.Contains("Connection", "close", comparisonTypeForValue));
		}

		internal static bool MaybeUri(this string value)
		{
			int num = value.IndexOf(':');
			if (num < 2 || num > 9)
			{
				return false;
			}
			string value2 = value.Substring(0, num);
			return value2.isPredefinedScheme();
		}

		internal static string Quote(this string value)
		{
			string format = "\"{0}\"";
			string arg = value.Replace("\"", "\\\"");
			return string.Format(format, arg);
		}

		internal static byte[] ReadBytes(this Stream stream, int length)
		{
			byte[] array = new byte[length];
			int num = 0;
			int num2 = 0;
			while (length > 0)
			{
				int num3 = stream.Read(array, num, length);
				if (num3 <= 0)
				{
					if (num2 >= _maxRetry)
					{
						return array.SubArray(0, num);
					}
					num2++;
				}
				else
				{
					num2 = 0;
					num += num3;
					length -= num3;
				}
			}
			return array;
		}

		internal static byte[] ReadBytes(this Stream stream, long length, int bufferLength)
		{
			using MemoryStream memoryStream = new MemoryStream();
			byte[] buffer = new byte[bufferLength];
			int num = 0;
			while (length > 0)
			{
				if (length < bufferLength)
				{
					bufferLength = (int)length;
				}
				int num2 = stream.Read(buffer, 0, bufferLength);
				if (num2 <= 0)
				{
					if (num >= _maxRetry)
					{
						break;
					}
					num++;
				}
				else
				{
					num = 0;
					memoryStream.Write(buffer, 0, num2);
					length -= num2;
				}
			}
			memoryStream.Close();
			return memoryStream.ToArray();
		}

		internal static void ReadBytesAsync(this Stream stream, int length, Action<byte[]> completed, Action<Exception> error)
		{
			byte[] ret = new byte[length];
			int offset = 0;
			int retry = 0;
			AsyncCallback callback = null;
			callback = (IAsyncResult ar) =>
			{
				try
				{
					int num = stream.EndRead(ar);
					if (num <= 0)
					{
						if (retry < _maxRetry)
						{
							retry++;
							stream.BeginRead(ret, offset, length, callback, null);
						}
						else if (completed != null)
						{
							completed(ret.SubArray(0, offset));
						}
					}
					else if (num == length)
					{
						if (completed != null)
						{
							completed(ret);
						}
					}
					else
					{
						retry = 0;
						offset += num;
						length -= num;
						stream.BeginRead(ret, offset, length, callback, null);
					}
				}
				catch (Exception obj2)
				{
					if (error != null)
					{
						error(obj2);
					}
				}
			};
			try
			{
				stream.BeginRead(ret, offset, length, callback, null);
			}
			catch (Exception obj)
			{
				if (error != null)
				{
					error(obj);
				}
			}
		}

		internal static void ReadBytesAsync(this Stream stream, long length, int bufferLength, Action<byte[]> completed, Action<Exception> error)
		{
			MemoryStream dest = new MemoryStream();
			byte[] buff = new byte[bufferLength];
			int retry = 0;
			Action<long> read = null;
			read = (long len) =>
			{
				if (len < bufferLength)
				{
					bufferLength = (int)len;
				}
				stream.BeginRead(buff, 0, bufferLength, (IAsyncResult ar) =>
				{
					try
					{
						int num = stream.EndRead(ar);
						if (num <= 0)
						{
							if (retry < _maxRetry)
							{
								int num2 = retry;
								retry = num2 + 1;
								read(len);
							}
							else
							{
								if (completed != null)
								{
									dest.Close();
									byte[] obj2 = dest.ToArray();
									completed(obj2);
								}
								dest.Dispose();
							}
						}
						else
						{
							dest.Write(buff, 0, num);
							if (num == len)
							{
								if (completed != null)
								{
									dest.Close();
									byte[] obj3 = dest.ToArray();
									completed(obj3);
								}
								dest.Dispose();
							}
							else
							{
								retry = 0;
								read(len - num);
							}
						}
					}
					catch (Exception obj4)
					{
						dest.Dispose();
						if (error != null)
						{
							error(obj4);
						}
					}
				}, null);
			};
			try
			{
				read(length);
			}
			catch (Exception obj)
			{
				dest.Dispose();
				if (error != null)
				{
					error(obj);
				}
			}
		}

		internal static T[] Reverse<T>(this T[] array)
		{
			long num = array.LongLength;
			T[] array2 = new T[num];
			long num2 = num - 1;
			for (long num3 = 0L; num3 <= num2; num3++)
			{
				array2[num3] = array[num2 - num3];
			}
			return array2;
		}

		internal static IEnumerable<string> SplitHeaderValue(this string value, params char[] separators)
		{
			int len = value.Length;
			int end = len - 1;
			StringBuilder buff = new StringBuilder(32);
			bool escaped = false;
			bool quoted = false;
			for (int i = 0; i <= end; i++)
			{
				char c = value[i];
				buff.Append(c);
				switch (c)
				{
				case '"':
					if (escaped)
					{
						escaped = false;
					}
					else
					{
						quoted = !quoted;
					}
					continue;
				case '\\':
					if (i == end)
					{
						break;
					}
					if (value[i + 1] == '"')
					{
						escaped = true;
					}
					continue;
				default:
					if (Array.IndexOf(separators, c) > -1 && !quoted)
					{
						buff.Length--;
						yield return buff.ToString();
						buff.Length = 0;
					}
					continue;
				}
				break;
			}
			yield return buff.ToString();
		}

		internal static byte[] ToByteArray(this Stream stream)
		{
			stream.Position = 0L;
			using MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream, 1024);
			memoryStream.Close();
			return memoryStream.ToArray();
		}

		internal static byte[] ToByteArray(this ushort value, ByteOrder order)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (!order.IsHostOrder())
			{
				Array.Reverse((Array)bytes);
			}
			return bytes;
		}

		internal static byte[] ToByteArray(this ulong value, ByteOrder order)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (!order.IsHostOrder())
			{
				Array.Reverse((Array)bytes);
			}
			return bytes;
		}

		internal static CompressionMethod ToCompressionMethod(this string value)
		{
			Array values = Enum.GetValues(typeof(CompressionMethod));
			foreach (CompressionMethod item in values)
			{
				if (item.ToExtensionString() == value)
				{
					return item;
				}
			}
			return CompressionMethod.None;
		}

		internal static string ToExtensionString(this CompressionMethod method, params string[] parameters)
		{
			if (method == CompressionMethod.None)
			{
				return string.Empty;
			}
			string arg = method.ToString().ToLower();
			string text = $"permessage-{arg}";
			if (parameters == null || parameters.Length == 0)
			{
				return text;
			}
			string arg2 = parameters.ToString("; ");
			return $"{text}; {arg2}";
		}

		internal static int ToInt32(this string numericString)
		{
			return int.Parse(numericString);
		}

		internal static IPAddress ToIPAddress(this string value)
		{
			if (value == null || value.Length == 0)
			{
				return null;
			}
			if (IPAddress.TryParse(value, out var address))
			{
				return address;
			}
			try
			{
				IPAddress[] hostAddresses = Dns.GetHostAddresses(value);
				return hostAddresses[0];
			}
			catch
			{
				return null;
			}
		}

		internal static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
		{
			return new List<TSource>(source);
		}

		internal static string ToString(this IPAddress address, bool bracketIPv6)
		{
			return (bracketIPv6 && address.AddressFamily == AddressFamily.InterNetworkV6) ? $"[{address}]" : address.ToString();
		}

		internal static ushort ToUInt16(this byte[] source, ByteOrder sourceOrder)
		{
			byte[] value = source.ToHostOrder(sourceOrder);
			return BitConverter.ToUInt16(value, 0);
		}

		internal static ulong ToUInt64(this byte[] source, ByteOrder sourceOrder)
		{
			byte[] value = source.ToHostOrder(sourceOrder);
			return BitConverter.ToUInt64(value, 0);
		}

		internal static Version ToVersion(this string versionString)
		{
			return new Version(versionString);
		}

		internal static IEnumerable<string> TrimEach(this IEnumerable<string> source)
		{
			foreach (string elm in source)
			{
				yield return elm.Trim();
			}
		}

		internal static string TrimSlashFromEnd(this string value)
		{
			string text = value.TrimEnd(new char[1] { '/' });
			return (text.Length > 0) ? text : "/";
		}

		internal static string TrimSlashOrBackslashFromEnd(this string value)
		{
			string text = value.TrimEnd('/', '\\');
			return (text.Length > 0) ? text : value[0].ToString();
		}

		internal static bool TryCreateVersion(this string versionString, out Version result)
		{
			result = null;
			try
			{
				result = new Version(versionString);
			}
			catch
			{
				return false;
			}
			return true;
		}

		internal static bool TryCreateWebSocketUri(this string uriString, out Uri result, out string message)
		{
			result = null;
			message = null;
			Uri uri = uriString.ToUri();
			if (uri == null)
			{
				message = "An invalid URI string.";
				return false;
			}
			if (!uri.IsAbsoluteUri)
			{
				message = "A relative URI.";
				return false;
			}
			string scheme = uri.Scheme;
			if (!(scheme == "ws") && !(scheme == "wss"))
			{
				message = "The scheme part is not 'ws' or 'wss'.";
				return false;
			}
			int port = uri.Port;
			if (port == 0)
			{
				message = "The port part is zero.";
				return false;
			}
			if (uri.Fragment.Length > 0)
			{
				message = "It includes the fragment component.";
				return false;
			}
			if (port == -1)
			{
				port = ((scheme == "ws") ? 80 : 443);
				uriString = $"{scheme}://{uri.Host}:{port}{uri.PathAndQuery}";
				result = new Uri(uriString);
			}
			else
			{
				result = uri;
			}
			return true;
		}

		internal static bool TryGetUTF8DecodedString(this byte[] bytes, out string s)
		{
			s = null;
			try
			{
				s = Encoding.UTF8.GetString(bytes);
			}
			catch
			{
				return false;
			}
			return true;
		}

		internal static bool TryGetUTF8EncodedBytes(this string s, out byte[] bytes)
		{
			bytes = null;
			try
			{
				bytes = Encoding.UTF8.GetBytes(s);
			}
			catch
			{
				return false;
			}
			return true;
		}

		internal static bool TryOpenRead(this FileInfo fileInfo, out FileStream fileStream)
		{
			fileStream = null;
			try
			{
				fileStream = fileInfo.OpenRead();
			}
			catch
			{
				return false;
			}
			return true;
		}

		internal static string Unquote(this string value)
		{
			int num = value.IndexOf('"');
			if (num == -1)
			{
				return value;
			}
			int num2 = value.LastIndexOf('"');
			if (num2 == num)
			{
				return value;
			}
			int num3 = num2 - num - 1;
			return (num3 > 0) ? value.Substring(num + 1, num3).Replace("\\\"", "\"") : string.Empty;
		}

		internal static bool Upgrades(this NameValueCollection headers, string protocol)
		{
			StringComparison comparisonTypeForValue = StringComparison.OrdinalIgnoreCase;
			return headers.Contains("Upgrade", protocol, comparisonTypeForValue) && headers.Contains("Connection", "Upgrade", comparisonTypeForValue);
		}

		internal static string UrlDecode(this string value, Encoding encoding)
		{
			return (value.IndexOfAny(new char[2] { '%', '+' }) > -1) ? HttpUtility.UrlDecode(value, encoding) : value;
		}

		internal static string UrlEncode(this string value, Encoding encoding)
		{
			return HttpUtility.UrlEncode(value, encoding);
		}

		internal static void WriteBytes(this Stream stream, byte[] bytes, int bufferLength)
		{
			using MemoryStream memoryStream = new MemoryStream(bytes);
			memoryStream.CopyTo(stream, bufferLength);
		}

		internal static void WriteBytesAsync(this Stream stream, byte[] bytes, int bufferLength, Action completed, Action<Exception> error)
		{
			MemoryStream src = new MemoryStream(bytes);
			src.CopyToAsync(stream, bufferLength, () =>
			{
				if (completed != null)
				{
					completed();
				}
				src.Dispose();
			}, (Exception ex) =>
			{
				src.Dispose();
				if (error != null)
				{
					error(ex);
				}
			});
		}

		public static string GetDescription(this Sfs2X.WebSocketSharp.Net.HttpStatusCode code)
		{
			return ((int)code).GetStatusDescription();
		}

		public static string GetStatusDescription(this int code)
		{
			return code switch
			{
				100 => "Continue", 
				101 => "Switching Protocols", 
				102 => "Processing", 
				200 => "OK", 
				201 => "Created", 
				202 => "Accepted", 
				203 => "Non-Authoritative Information", 
				204 => "No Content", 
				205 => "Reset Content", 
				206 => "Partial Content", 
				207 => "Multi-Status", 
				300 => "Multiple Choices", 
				301 => "Moved Permanently", 
				302 => "Found", 
				303 => "See Other", 
				304 => "Not Modified", 
				305 => "Use Proxy", 
				307 => "Temporary Redirect", 
				400 => "Bad Request", 
				401 => "Unauthorized", 
				402 => "Payment Required", 
				403 => "Forbidden", 
				404 => "Not Found", 
				405 => "Method Not Allowed", 
				406 => "Not Acceptable", 
				407 => "Proxy Authentication Required", 
				408 => "Request Timeout", 
				409 => "Conflict", 
				410 => "Gone", 
				411 => "Length Required", 
				412 => "Precondition Failed", 
				413 => "Request Entity Too Large", 
				414 => "Request-Uri Too Long", 
				415 => "Unsupported Media Type", 
				416 => "Requested Range Not Satisfiable", 
				417 => "Expectation Failed", 
				422 => "Unprocessable Entity", 
				423 => "Locked", 
				424 => "Failed Dependency", 
				500 => "Internal Server Error", 
				501 => "Not Implemented", 
				502 => "Bad Gateway", 
				503 => "Service Unavailable", 
				504 => "Gateway Timeout", 
				505 => "Http Version Not Supported", 
				507 => "Insufficient Storage", 
				_ => string.Empty, 
			};
		}

		public static bool IsCloseStatusCode(this ushort value)
		{
			return value > 999 && value < 5000;
		}

		public static bool IsEnclosedIn(this string value, char c)
		{
			if (value == null)
			{
				return false;
			}
			int length = value.Length;
			return length > 1 && value[0] == c && value[length - 1] == c;
		}

		public static bool IsHostOrder(this ByteOrder order)
		{
			return BitConverter.IsLittleEndian == (order == ByteOrder.Little);
		}

		public static bool IsLocal(this IPAddress address)
		{
			if (address == null)
			{
				throw new ArgumentNullException("address");
			}
			if (address.Equals(IPAddress.Any))
			{
				return true;
			}
			if (address.Equals(IPAddress.Loopback))
			{
				return true;
			}
			if (Socket.OSSupportsIPv6)
			{
				if (address.Equals(IPAddress.IPv6Any))
				{
					return true;
				}
				if (address.Equals(IPAddress.IPv6Loopback))
				{
					return true;
				}
			}
			string hostName = Dns.GetHostName();
			IPAddress[] hostAddresses = Dns.GetHostAddresses(hostName);
			IPAddress[] array = hostAddresses;
			foreach (IPAddress obj in array)
			{
				if (address.Equals(obj))
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsNullOrEmpty(this string value)
		{
			return value == null || value.Length == 0;
		}

		public static T[] SubArray<T>(this T[] array, int startIndex, int length)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			int num = array.Length;
			if (num == 0)
			{
				if (startIndex != 0)
				{
					throw new ArgumentOutOfRangeException("startIndex");
				}
				if (length != 0)
				{
					throw new ArgumentOutOfRangeException("length");
				}
				return array;
			}
			if (startIndex < 0 || startIndex >= num)
			{
				throw new ArgumentOutOfRangeException("startIndex");
			}
			if (length < 0 || length > num - startIndex)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			if (length == 0)
			{
				return new T[0];
			}
			if (length == num)
			{
				return array;
			}
			T[] array2 = new T[length];
			Array.Copy(array, startIndex, array2, 0, length);
			return array2;
		}

		public static T[] SubArray<T>(this T[] array, long startIndex, long length)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			long num = array.LongLength;
			if (num == 0)
			{
				if (startIndex != 0)
				{
					throw new ArgumentOutOfRangeException("startIndex");
				}
				if (length != 0)
				{
					throw new ArgumentOutOfRangeException("length");
				}
				return array;
			}
			if (startIndex < 0 || startIndex >= num)
			{
				throw new ArgumentOutOfRangeException("startIndex");
			}
			if (length < 0 || length > num - startIndex)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			if (length == 0)
			{
				return new T[0];
			}
			if (length == num)
			{
				return array;
			}
			T[] array2 = new T[length];
			Array.Copy(array, startIndex, array2, 0L, length);
			return array2;
		}

		public static void Times(this int n, Action<int> action)
		{
			if (n > 0 && action != null)
			{
				for (int i = 0; i < n; i++)
				{
					action(i);
				}
			}
		}

		public static void Times(this long n, Action<long> action)
		{
			if (n > 0 && action != null)
			{
				for (long num = 0L; num < n; num++)
				{
					action(num);
				}
			}
		}

		public static byte[] ToHostOrder(this byte[] source, ByteOrder sourceOrder)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (source.Length < 2)
			{
				return source;
			}
			if (sourceOrder.IsHostOrder())
			{
				return source;
			}
			return source.Reverse();
		}

		public static string ToString<T>(this T[] array, string separator)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			int num = array.Length;
			if (num == 0)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder(64);
			int num2 = num - 1;
			for (int i = 0; i < num2; i++)
			{
				stringBuilder.AppendFormat("{0}{1}", array[i], separator);
			}
			stringBuilder.AppendFormat("{0}", array[num2]);
			return stringBuilder.ToString();
		}

		public static Uri ToUri(this string value)
		{
			if (value == null || value.Length == 0)
			{
				return null;
			}
			UriKind uriKind = (value.MaybeUri() ? UriKind.Absolute : UriKind.Relative);
			Uri.TryCreate(value, uriKind, out var result);
			return result;
		}
	}
}
#endif