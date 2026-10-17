#if UNITY_WEBGL
namespace Sfs2X.Entities.Variables
{
	public enum VariableType
	{
		NULL = 0,
		BOOL = 1,
		INT = 2,
		DOUBLE = 3,
		STRING = 4,
		OBJECT = 5,
		ARRAY = 6
	}
}
#else
namespace Sfs2X.Entities.Variables
{
    public enum VariableType
    {
        NULL = 0,
        BOOL = 1,
        INT = 2,
        DOUBLE = 3,
        STRING = 4,
        OBJECT = 5,
        ARRAY = 6
    }
}
#endif