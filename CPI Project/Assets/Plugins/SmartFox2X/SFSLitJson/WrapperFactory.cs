#if UNITY_WEBGL
namespace SFSLitJson
{
	public delegate IJsonWrapper WrapperFactory();
}
#else
namespace SFSLitJson
{
    public delegate IJsonWrapper WrapperFactory();
}
#endif