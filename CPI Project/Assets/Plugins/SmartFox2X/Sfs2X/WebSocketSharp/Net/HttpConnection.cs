using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Sfs2X.WebSocketSharp.Net
{
	internal sealed class HttpConnection
	{
		private int _attempts;

		private byte[] _buffer;

		private static readonly int _bufferLength;

		private HttpListenerContext _context;

		private StringBuilder _currentLine;

		private EndPointListener _endPointListener;

		private InputState _inputState;

		private RequestStream _inputStream;

		private LineState _lineState;

		private EndPoint _localEndPoint;

		private static readonly int _maxInputLength;

		private ResponseStream _outputStream;

		private int _position;

		private EndPoint _remoteEndPoint;

		private MemoryStream _requestBuffer;

		private int _reuses;

		private bool _secure;

		private Socket _socket;

		private Stream _stream;

		private object _sync;

		private int _timeout;

		private Dictionary<int, bool> _timeoutCanceled;

		private Timer _timer;

		public bool IsClosed => _socket == null;

		public bool IsLocal => ((IPEndPoint)_remoteEndPoint).Address.IsLocal();

		public bool IsSecure => _secure;

		public IPEndPoint LocalEndPoint => (IPEndPoint)_localEndPoint;

		public IPEndPoint RemoteEndPoint => (IPEndPoint)_remoteEndPoint;

		public int Reuses => _reuses;

		public Stream Stream => _stream;

		static HttpConnection()
		{
			_bufferLength = 8192;
			_maxInputLength = 32768;
		}

		internal HttpConnection(Socket socket, EndPointListener listener)
		{
			_socket = socket;
			_endPointListener = listener;
			NetworkStream networkStream = new NetworkStream(socket, ownsSocket: false);
			if (listener.IsSecure)
			{
				ServerSslConfiguration sslConfiguration = listener.SslConfiguration;
				SslStream sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false, sslConfiguration.ClientCertificateValidationCallback);
				sslStream.AuthenticateAsServer(sslConfiguration.ServerCertificate, sslConfiguration.ClientCertificateRequired, sslConfiguration.EnabledSslProtocols, sslConfiguration.CheckCertificateRevocation);
				_secure = true;
				_stream = sslStream;
			}
			else
			{
				_stream = networkStream;
			}
			_buffer = new byte[_bufferLength];
			_localEndPoint = socket.LocalEndPoint;
			_remoteEndPoint = socket.RemoteEndPoint;
			_sync = new object();
			_timeoutCanceled = new Dictionary<int, bool>();
			_timer = new Timer(onTimeout, this, -1, -1);
			init(new MemoryStream(), 90000);
		}

		private void close()
		{
			lock (_sync)
			{
				if (_socket == null)
				{
					return;
				}
				disposeTimer();
				disposeRequestBuffer();
				disposeStream();
				closeSocket();
			}
			_context.Unregister();
			_endPointListener.RemoveConnection(this);
		}

		private void closeSocket()
		{
			try
			{
				_socket.Shutdown(SocketShutdown.Both);
			}
			catch
			{
			}
			_socket.Close();
			_socket = null;
		}

		private static MemoryStream createRequestBuffer(RequestStream inputStream)
		{
			MemoryStream memoryStream = new MemoryStream();
			if (inputStream is ChunkedRequestStream)
			{
				ChunkedRequestStream chunkedRequestStream = (ChunkedRequestStream)inputStream;
				if (chunkedRequestStream.HasRemainingBuffer)
				{
					byte[] remainingBuffer = chunkedRequestStream.RemainingBuffer;
					memoryStream.Write(remainingBuffer, 0, remainingBuffer.Length);
				}
				return memoryStream;
			}
			int count = inputStream.Count;
			if (count > 0)
			{
				memoryStream.Write(inputStream.InitialBuffer, inputStream.Offset, count);
			}
			return memoryStream;
		}

		private void disposeRequestBuffer()
		{
			if (_requestBuffer != null)
			{
				_requestBuffer.Dispose();
				_requestBuffer = null;
			}
		}

		private void disposeStream()
		{
			if (_stream != null)
			{
				_stream.Dispose();
				_stream = null;
			}
		}

		private void disposeTimer()
		{
			if (_timer != null)
			{
				try
				{
					_timer.Change(-1, -1);
				}
				catch
				{
				}
				_timer.Dispose();
				_timer = null;
			}
		}

		private void init(MemoryStream requestBuffer, int timeout)
		{
			_requestBuffer = requestBuffer;
			_timeout = timeout;
			_context = new HttpListenerContext(this);
			_currentLine = new StringBuilder(64);
			_inputState = InputState.RequestLine;
			_inputStream = null;
			_lineState = LineState.None;
			_outputStream = null;
			_position = 0;
		}

		private static void onRead(IAsyncResult asyncResult)
		{
			HttpConnection httpConnection = (HttpConnection)asyncResult.AsyncState;
			int attempts = httpConnection._attempts;
			if (httpConnection._socket == null)
			{
				return;
			}
			lock (httpConnection._sync)
			{
				if (httpConnection._socket == null)
				{
					return;
				}
				httpConnection._timer.Change(-1, -1);
				httpConnection._timeoutCanceled[attempts] = true;
				int num = 0;
				try
				{
					num = httpConnection._stream.EndRead(asyncResult);
				}
				catch (Exception)
				{
					httpConnection.close();
					return;
				}
				if (num <= 0)
				{
					httpConnection.close();
					return;
				}
				httpConnection._requestBuffer.Write(httpConnection._buffer, 0, num);
				if (!httpConnection.processRequestBuffer())
				{
					httpConnection.BeginReadRequest();
				}
			}
		}

		private static void onTimeout(object state)
		{
			HttpConnection httpConnection = (HttpConnection)state;
			int attempts = httpConnection._attempts;
			if (httpConnection._socket == null)
			{
				return;
			}
			lock (httpConnection._sync)
			{
				if (httpConnection._socket != null && !httpConnection._timeoutCanceled[attempts])
				{
					httpConnection._context.SendError(408);
				}
			}
		}

		private bool processInput(byte[] data, int length)
		{
			HttpListenerRequest request = _context.Request;
			try
			{
				while (true)
				{
					string text = readLineFrom(data, _position, length, out var nread);
					_position += nread;
					if (text == null)
					{
						break;
					}
					if (text.Length == 0)
					{
						if (_inputState == InputState.RequestLine)
						{
							continue;
						}
						if (_position > _maxInputLength)
						{
							_context.ErrorMessage = "Headers too long";
						}
						return true;
					}
					if (_inputState == InputState.RequestLine)
					{
						request.SetRequestLine(text);
						_inputState = InputState.Headers;
					}
					else
					{
						request.AddHeader(text);
					}
					if (!_context.HasErrorMessage)
					{
						continue;
					}
					return true;
				}
			}
			catch (Exception)
			{
				_context.ErrorMessage = "Processing failure";
				return true;
			}
			if (_position >= _maxInputLength)
			{
				_context.ErrorMessage = "Headers too long";
				return true;
			}
			return false;
		}

		private bool processRequestBuffer()
		{
			byte[] buffer = _requestBuffer.GetBuffer();
			int length = (int)_requestBuffer.Length;
			if (!processInput(buffer, length))
			{
				return false;
			}
			HttpListenerRequest request = _context.Request;
			if (!_context.HasErrorMessage)
			{
				request.FinishInitialization();
			}
			if (_context.HasErrorMessage)
			{
				_context.SendError();
				return true;
			}
			Uri url = request.Url;
			if (!_endPointListener.TrySearchHttpListener(url, out var listener))
			{
				_context.SendError(404);
				return true;
			}
			listener.RegisterContext(_context);
			return true;
		}

		private string readLineFrom(byte[] buffer, int offset, int length, out int nread)
		{
			nread = 0;
			for (int i = offset; i < length; i++)
			{
				nread++;
				byte b = buffer[i];
				switch (b)
				{
				case 13:
					_lineState = LineState.Cr;
					continue;
				case 10:
					break;
				default:
					_currentLine.Append((char)b);
					continue;
				}
				_lineState = LineState.Lf;
				break;
			}
			if (_lineState != LineState.Lf)
			{
				return null;
			}
			string result = _currentLine.ToString();
			_currentLine.Length = 0;
			_lineState = LineState.None;
			return result;
		}

		private MemoryStream takeOverRequestBuffer()
		{
			if (_inputStream != null)
			{
				return createRequestBuffer(_inputStream);
			}
			MemoryStream memoryStream = new MemoryStream();
			byte[] buffer = _requestBuffer.GetBuffer();
			int num = (int)_requestBuffer.Length;
			int num2 = num - _position;
			if (num2 > 0)
			{
				memoryStream.Write(buffer, _position, num2);
			}
			disposeRequestBuffer();
			return memoryStream;
		}

		internal void BeginReadRequest()
		{
			_attempts++;
			_timeoutCanceled.Add(_attempts, value: false);
			_timer.Change(_timeout, -1);
			try
			{
				_stream.BeginRead(_buffer, 0, _bufferLength, onRead, this);
			}
			catch (Exception)
			{
				close();
			}
		}

		internal void Close(bool force)
		{
			if (_socket == null)
			{
				return;
			}
			lock (_sync)
			{
				if (_socket == null)
				{
					return;
				}
				if (force)
				{
					if (_outputStream != null)
					{
						_outputStream.Close(force: true);
					}
					close();
					return;
				}
				GetResponseStream().Close(force: false);
				if (_context.Response.CloseConnection)
				{
					close();
					return;
				}
				if (!_context.Request.FlushInput())
				{
					close();
					return;
				}
				_context.Unregister();
				_reuses++;
				MemoryStream memoryStream = takeOverRequestBuffer();
				long length = memoryStream.Length;
				init(memoryStream, 15000);
				if (length <= 0 || !processRequestBuffer())
				{
					BeginReadRequest();
				}
			}
		}

		public void Close()
		{
			Close(force: false);
		}

		public RequestStream GetRequestStream(long contentLength, bool chunked)
		{
			lock (_sync)
			{
				if (_socket == null)
				{
					return null;
				}
				if (_inputStream != null)
				{
					return _inputStream;
				}
				byte[] buffer = _requestBuffer.GetBuffer();
				int num = (int)_requestBuffer.Length;
				int count = num - _position;
				_inputStream = (chunked ? new ChunkedRequestStream(_stream, buffer, _position, count, _context) : new RequestStream(_stream, buffer, _position, count, contentLength));
				disposeRequestBuffer();
				return _inputStream;
			}
		}

		public ResponseStream GetResponseStream()
		{
			lock (_sync)
			{
				if (_socket == null)
				{
					return null;
				}
				if (_outputStream != null)
				{
					return _outputStream;
				}
				bool ignoreWriteExceptions = _context.Listener?.IgnoreWriteExceptions ?? true;
				_outputStream = new ResponseStream(_stream, _context.Response, ignoreWriteExceptions);
				return _outputStream;
			}
		}
	}
}
