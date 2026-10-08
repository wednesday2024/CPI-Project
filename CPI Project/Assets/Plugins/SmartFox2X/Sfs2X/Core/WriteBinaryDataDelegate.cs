#if UNITY_WEBGL
using Sfs2X.Util;

namespace Sfs2X.Core
{
	public delegate void WriteBinaryDataDelegate(PacketHeader header, ByteArray binData, bool udp);
}
#else
using Sfs2X.Util;

namespace Sfs2X.Core
{
    public delegate void WriteBinaryDataDelegate(PacketHeader header, ByteArray binData, bool udp);
}
#endif