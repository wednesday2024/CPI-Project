#if UNITY_WEBGL
#else
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Sfs2X.WebSocketSharp.Net
{
	[Serializable]
	public class HttpListenerException : Win32Exception
	{
		public override int ErrorCode => base.NativeErrorCode;

		protected HttpListenerException(SerializationInfo serializationInfo, StreamingContext streamingContext)
			: base(serializationInfo, streamingContext)
		{
		}

		public HttpListenerException()
		{
		}

		public HttpListenerException(int errorCode)
			: base(errorCode)
		{
		}

		public HttpListenerException(int errorCode, string message)
			: base(errorCode, message)
		{
		}
	}
}
#endif