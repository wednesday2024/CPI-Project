#if UNITY_WEBGL
namespace SFSLitJson
{
	internal class FsmContext
	{
		public bool Return;

		public int NextState;

		public Lexer L;

		public int StateStack;
	}
}
#else
namespace SFSLitJson
{
    internal class FsmContext
    {
        public bool Return;

        public int NextState;

        public Lexer L;

        public int StateStack;
    }
}
#endif