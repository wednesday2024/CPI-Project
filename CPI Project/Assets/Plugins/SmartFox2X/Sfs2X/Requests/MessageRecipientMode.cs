#if UNITY_WEBGL
using System;

namespace Sfs2X.Requests
{
	public class MessageRecipientMode
	{
		private object target;

		private int mode;

		public object Target => target;

		public int Mode => mode;

		public MessageRecipientMode(int mode, object target)
		{
			if (mode < 0 || mode > 3)
			{
				throw new ArgumentException("Illegal recipient mode: " + mode);
			}
			this.mode = mode;
			this.target = target;
		}
	}
}
#else
using System;

namespace Sfs2X.Requests
{
    public class MessageRecipientMode
    {
        private object target;

        private int mode;

        public object Target => target;

        public int Mode => mode;

        public MessageRecipientMode(int mode, object target)
        {
            if (mode < 0 || mode > 3)
            {
                throw new ArgumentException("Illegal recipient mode: " + mode);
            }
            this.mode = mode;
            this.target = target;
        }
    }
}
#endif