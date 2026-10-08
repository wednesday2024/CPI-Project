#if UNITY_WEBGL
#else
using System.Collections.Specialized;
using System.Text;

namespace Sfs2X.WebSocketSharp.Net
{
	internal sealed class QueryStringCollection : NameValueCollection
	{
		public QueryStringCollection()
		{
		}

		public QueryStringCollection(int capacity)
			: base(capacity)
		{
		}

		public static QueryStringCollection Parse(string query)
		{
			return Parse(query, Encoding.UTF8);
		}

		public static QueryStringCollection Parse(string query, Encoding encoding)
		{
			if (query == null)
			{
				return new QueryStringCollection(1);
			}
			if (query.Length == 0)
			{
				return new QueryStringCollection(1);
			}
			if (query == "?")
			{
				return new QueryStringCollection(1);
			}
			if (query[0] == '?')
			{
				query = query.Substring(1);
			}
			if (encoding == null)
			{
				encoding = Encoding.UTF8;
			}
			QueryStringCollection queryStringCollection = new QueryStringCollection();
			string[] array = query.Split(new char[1] { '&' });
			foreach (string text in array)
			{
				int length = text.Length;
				if (length != 0 && !(text == "="))
				{
					string name = null;
					string text2 = null;
					int num = text.IndexOf('=');
					if (num < 0)
					{
						text2 = text.UrlDecode(encoding);
					}
					else if (num == 0)
					{
						text2 = text.Substring(1).UrlDecode(encoding);
					}
					else
					{
						name = text.Substring(0, num).UrlDecode(encoding);
						int num2 = num + 1;
						text2 = ((num2 < length) ? text.Substring(num2).UrlDecode(encoding) : string.Empty);
					}
					queryStringCollection.Add(name, text2);
				}
			}
			return queryStringCollection;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string[] allKeys = AllKeys;
			foreach (string text in allKeys)
			{
				stringBuilder.AppendFormat("{0}={1}&", text, base[text]);
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Length--;
			}
			return stringBuilder.ToString();
		}
	}
}
#endif