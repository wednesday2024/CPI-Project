#if UNITY_WEBGL
using System.IO;

namespace ComponentAce.Compression.Libs.zlib
{
	public class ZStreamException : IOException
	{
		public ZStreamException()
		{
		}

		public ZStreamException(string s)
			: base(s)
		{
		}
	}
}
#else
using System.IO;

namespace ComponentAce.Compression.Libs.zlib
{
    public class ZStreamException : IOException
    {
        public ZStreamException()
        {
        }

        public ZStreamException(string s)
            : base(s)
        {
        }
    }
}
#endif