#if UNITY_WEBGL
namespace Sfs2X.Entities.Match
{
	public class LogicOperator
	{
		public static readonly LogicOperator AND = new LogicOperator("AND");

		public static readonly LogicOperator OR = new LogicOperator("OR");

		private string id;

		public string Id => id;

		public LogicOperator(string id)
		{
			this.id = id;
		}
	}
}
#else
namespace Sfs2X.Entities.Match
{
    public class LogicOperator
    {
        public static readonly LogicOperator AND = new LogicOperator("AND");

        public static readonly LogicOperator OR = new LogicOperator("OR");

        private string id;

        public string Id => id;

        public LogicOperator(string id)
        {
            this.id = id;
        }
    }
}
#endif