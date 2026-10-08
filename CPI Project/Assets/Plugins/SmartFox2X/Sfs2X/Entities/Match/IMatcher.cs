#if UNITY_WEBGL
namespace Sfs2X.Entities.Match
{
	public interface IMatcher
	{
		string Symbol { get; }

		int Type { get; }
	}
}
#else
namespace Sfs2X.Entities.Match
{
    public interface IMatcher
    {
        string Symbol { get; }

        int Type { get; }
    }
}
#endif