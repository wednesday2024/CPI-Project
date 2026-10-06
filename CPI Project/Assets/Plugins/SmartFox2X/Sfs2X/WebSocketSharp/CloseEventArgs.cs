using System;

namespace Sfs2X.WebSocketSharp
{
	public class CloseEventArgs : EventArgs
	{
		private bool _clean;

		private PayloadData _payloadData;

		public ushort Code => _payloadData.Code;

		public string Reason => _payloadData.Reason;

		public bool WasClean => _clean;

		internal CloseEventArgs(PayloadData payloadData, bool clean)
		{
			_payloadData = payloadData;
			_clean = clean;
		}
	}
}
