#if UNITY_WEBGL
#else
using Sfs2X.Util;

namespace Sfs2X.Core
{
	public interface IPacketEncrypter
	{
		void Encrypt(ByteArray data);

		void Decrypt(ByteArray data);
	}
}
#endif