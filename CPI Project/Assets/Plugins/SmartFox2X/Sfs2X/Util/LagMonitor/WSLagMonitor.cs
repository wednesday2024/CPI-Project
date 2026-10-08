#if UNITY_WEBGL
using System;
using System.Collections.Generic;
using Sfs2X.Requests;

namespace Sfs2X.Util.LagMonitor
{
	public class WSLagMonitor : ILagMonitor
	{
		private bool isRunning;

		private DateTime lastReqTime;

		private List<int> valueQueue;

		private int interval;

		private int queueSize;

		private SmartFox sfs;

		public bool IsRunning => isRunning;

		private int AveragePingTime
		{
			get
			{
				if (valueQueue.Count == 0)
				{
					return 0;
				}
				int num = 0;
				foreach (int item in valueQueue)
				{
					num += item;
				}
				return num / valueQueue.Count;
			}
		}

		public WSLagMonitor(SmartFox sfs, int interval, int queueSize)
		{
			if (interval < 1)
			{
				interval = 1;
			}
			this.sfs = sfs;
			valueQueue = new List<int>();
			this.interval = interval;
			this.queueSize = queueSize;
		}

		public void Start()
		{
			if (!IsRunning)
			{
				isRunning = true;
			}
		}

		public void Stop()
		{
			if (IsRunning)
			{
				isRunning = false;
				valueQueue = new List<int>();
			}
		}

		public void Destroy()
		{
			Stop();
			sfs = null;
		}

		public void Execute()
		{
			DateTime now = DateTime.Now;
			if (IsRunning && (now - lastReqTime).TotalMilliseconds >= (double)(interval * 1000))
			{
				lastReqTime = now;
				sfs.Send(new PingPongRequest());
			}
		}

		public int OnPingPong()
		{
			int item = Convert.ToInt32((DateTime.Now - lastReqTime).TotalMilliseconds);
			if (valueQueue.Count >= queueSize)
			{
				valueQueue.RemoveAt(0);
			}
			valueQueue.Add(item);
			return AveragePingTime;
		}
	}
}
#else
#endif