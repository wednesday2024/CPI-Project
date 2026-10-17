#if UNITY_WEBGL
namespace SFSLitJson
{
	internal delegate object ImporterFunc(object input);
	public delegate TValue ImporterFunc<TJson, TValue>(TJson input);
}
#else
namespace SFSLitJson
{
    internal delegate object ImporterFunc(object input);
    public delegate TValue ImporterFunc<TJson, TValue>(TJson input);
}
#endif