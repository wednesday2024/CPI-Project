#if UNITY_WEBGL
namespace SFSLitJson
{
	internal class WriterContext
	{
		public int Count;

		public bool InArray;

		public bool InObject;

		public bool ExpectingValue;

		public int Padding;
	}
}
#else
namespace SFSLitJson
{
    internal class WriterContext
    {
        public int Count;

        public bool InArray;

        public bool InObject;

        public bool ExpectingValue;

        public int Padding;
    }
}
#endif