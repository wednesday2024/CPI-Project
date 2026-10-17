#if UNITY_WEBGL
namespace Sfs2X.Entities.Variables
{
	public interface IMMOItemVariable : Variable
	{
	}
}
#else
namespace Sfs2X.Entities.Variables
{
    public interface IMMOItemVariable : Variable
    {
    }
}
#endif