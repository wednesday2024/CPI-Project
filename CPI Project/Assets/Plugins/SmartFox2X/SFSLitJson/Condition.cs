#if UNITY_WEBGL
namespace SFSLitJson
{
	internal enum Condition
	{
		InArray = 0,
		InObject = 1,
		NotAProperty = 2,
		Property = 3,
		Value = 4
	}
}
#else
namespace SFSLitJson
{
    internal enum Condition
    {
        InArray = 0,
        InObject = 1,
        NotAProperty = 2,
        Property = 3,
        Value = 4
    }
}
#endif