#if UNITY_WEBGL
namespace Sfs2X.Bitswarm
{
	public interface IController
	{
		int Id { get; set; }

		void HandleMessage(IMessage message);
	}
}
#else
namespace Sfs2X.Bitswarm
{
    public interface IController
    {
        int Id { get; set; }

        void HandleMessage(IMessage message);
    }
}
#endif