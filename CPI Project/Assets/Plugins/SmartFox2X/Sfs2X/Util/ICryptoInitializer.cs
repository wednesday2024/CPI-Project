#if UNITY_WEBGL
#else
namespace Sfs2X.Util
{
	public interface ICryptoInitializer
	{
		void Run();
	}
}
#endif