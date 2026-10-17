#if UNITY_WEBGL
namespace Sfs2X.Logging
{
	public enum LogLevel
	{
		DEBUG = 100,
		INFO = 200,
		WARN = 300,
		ERROR = 400
	}
}
#else
namespace Sfs2X.Logging
{
    public enum LogLevel
    {
        DEBUG = 100,
        INFO = 200,
        WARN = 300,
        ERROR = 400
    }
}
#endif