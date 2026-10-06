using System;
using System.IO;

namespace Sfs2X.WebSocketSharp.Net
{
	internal class ChunkedRequestStream : RequestStream
	{
		private static readonly int _bufferLength;

		private HttpListenerContext _context;

		private ChunkStream _decoder;

		private bool _disposed;

		private bool _noMoreData;

		internal bool HasRemainingBuffer => _decoder.Count + base.Count > 0;

		internal byte[] RemainingBuffer
		{
			get
			{
				using MemoryStream memoryStream = new MemoryStream();
				int count = _decoder.Count;
				if (count > 0)
				{
					memoryStream.Write(_decoder.EndBuffer, _decoder.Offset, count);
				}
				count = base.Count;
				if (count > 0)
				{
					memoryStream.Write(base.InitialBuffer, base.Offset, count);
				}
				memoryStream.Close();
				return memoryStream.ToArray();
			}
		}

		static ChunkedRequestStream()
		{
			_bufferLength = 8192;
		}

		internal ChunkedRequestStream(Stream innerStream, byte[] initialBuffer, int offset, int count, HttpListenerContext context)
			: base(innerStream, initialBuffer, offset, count, -1L)
		{
			_context = context;
			_decoder = new ChunkStream((WebHeaderCollection)context.Request.Headers);
		}

		private void onRead(IAsyncResult asyncResult)
		{
			ReadBufferState readBufferState = (ReadBufferState)asyncResult.AsyncState;
			HttpStreamAsyncResult asyncResult2 = readBufferState.AsyncResult;
			try
			{
				int count = base.EndRead(asyncResult);
				_decoder.Write(asyncResult2.Buffer, asyncResult2.Offset, count);
				count = _decoder.Read(readBufferState.Buffer, readBufferState.Offset, readBufferState.Count);
				readBufferState.Offset += count;
				readBufferState.Count -= count;
				if (readBufferState.Count == 0 || !_decoder.WantsMore || count == 0)
				{
					_noMoreData = !_decoder.WantsMore && count == 0;
					asyncResult2.Count = readBufferState.InitialCount - readBufferState.Count;
					asyncResult2.Complete();
				}
				else
				{
					base.BeginRead(asyncResult2.Buffer, asyncResult2.Offset, asyncResult2.Count, (AsyncCallback)onRead, (object)readBufferState);
				}
			}
			catch (Exception exception)
			{
				_context.ErrorMessage = "I/O operation aborted";
				_context.SendError();
				asyncResult2.Complete(exception);
			}
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			if (_disposed)
			{
				string objectName = GetType().ToString();
				throw new ObjectDisposedException(objectName);
			}
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (offset < 0)
			{
				string message = "A negative value.";
				throw new ArgumentOutOfRangeException("offset", message);
			}
			if (count < 0)
			{
				string message2 = "A negative value.";
				throw new ArgumentOutOfRangeException("count", message2);
			}
			int num = buffer.Length;
			if (offset + count > num)
			{
				string message3 = "The sum of 'offset' and 'count' is greater than the length of 'buffer'.";
				throw new ArgumentException(message3);
			}
			HttpStreamAsyncResult httpStreamAsyncResult = new HttpStreamAsyncResult(callback, state);
			if (_noMoreData)
			{
				httpStreamAsyncResult.Complete();
				return httpStreamAsyncResult;
			}
			int num2 = _decoder.Read(buffer, offset, count);
			offset += num2;
			count -= num2;
			if (count == 0)
			{
				httpStreamAsyncResult.Count = num2;
				httpStreamAsyncResult.Complete();
				return httpStreamAsyncResult;
			}
			if (!_decoder.WantsMore)
			{
				_noMoreData = num2 == 0;
				httpStreamAsyncResult.Count = num2;
				httpStreamAsyncResult.Complete();
				return httpStreamAsyncResult;
			}
			httpStreamAsyncResult.Buffer = new byte[_bufferLength];
			httpStreamAsyncResult.Offset = 0;
			httpStreamAsyncResult.Count = _bufferLength;
			ReadBufferState readBufferState = new ReadBufferState(buffer, offset, count, httpStreamAsyncResult);
			readBufferState.InitialCount += num2;
			base.BeginRead(httpStreamAsyncResult.Buffer, httpStreamAsyncResult.Offset, httpStreamAsyncResult.Count, (AsyncCallback)onRead, (object)readBufferState);
			return httpStreamAsyncResult;
		}

		public override void Close()
		{
			if (!_disposed)
			{
				base.Close();
				_disposed = true;
			}
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			if (_disposed)
			{
				string objectName = GetType().ToString();
				throw new ObjectDisposedException(objectName);
			}
			if (asyncResult == null)
			{
				throw new ArgumentNullException("asyncResult");
			}
			if (!(asyncResult is HttpStreamAsyncResult httpStreamAsyncResult))
			{
				string message = "A wrong IAsyncResult instance.";
				throw new ArgumentException(message, "asyncResult");
			}
			if (!httpStreamAsyncResult.IsCompleted)
			{
				httpStreamAsyncResult.AsyncWaitHandle.WaitOne();
			}
			if (httpStreamAsyncResult.HasException)
			{
				string message2 = "The I/O operation has been aborted.";
				throw new HttpListenerException(995, message2);
			}
			return httpStreamAsyncResult.Count;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			IAsyncResult asyncResult = BeginRead(buffer, offset, count, null, null);
			return EndRead(asyncResult);
		}
	}
}
