#if UNITY_WEBGL
#else
namespace Sfs2X.WebSocketSharp.Net
{
	internal class HttpHeaderInfo
	{
		private string _headerName;

		private HttpHeaderType _headerType;

		internal bool IsMultiValueInRequest
		{
			get
			{
				HttpHeaderType httpHeaderType = _headerType & HttpHeaderType.MultiValueInRequest;
				return httpHeaderType == HttpHeaderType.MultiValueInRequest;
			}
		}

		internal bool IsMultiValueInResponse
		{
			get
			{
				HttpHeaderType httpHeaderType = _headerType & HttpHeaderType.MultiValueInResponse;
				return httpHeaderType == HttpHeaderType.MultiValueInResponse;
			}
		}

		public string HeaderName => _headerName;

		public HttpHeaderType HeaderType => _headerType;

		public bool IsRequest
		{
			get
			{
				HttpHeaderType httpHeaderType = _headerType & HttpHeaderType.Request;
				return httpHeaderType == HttpHeaderType.Request;
			}
		}

		public bool IsResponse
		{
			get
			{
				HttpHeaderType httpHeaderType = _headerType & HttpHeaderType.Response;
				return httpHeaderType == HttpHeaderType.Response;
			}
		}

		internal HttpHeaderInfo(string headerName, HttpHeaderType headerType)
		{
			_headerName = headerName;
			_headerType = headerType;
		}

		public bool IsMultiValue(bool response)
		{
			HttpHeaderType httpHeaderType = _headerType & HttpHeaderType.MultiValue;
			if (httpHeaderType != HttpHeaderType.MultiValue)
			{
				return response ? IsMultiValueInResponse : IsMultiValueInRequest;
			}
			return response ? IsResponse : IsRequest;
		}

		public bool IsRestricted(bool response)
		{
			HttpHeaderType httpHeaderType = _headerType & HttpHeaderType.Restricted;
			if (httpHeaderType != HttpHeaderType.Restricted)
			{
				return false;
			}
			return response ? IsResponse : IsRequest;
		}
	}
}
#endif