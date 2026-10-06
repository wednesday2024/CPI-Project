using Sfs2X.Entities.Data;

namespace Sfs2X.Entities.Variables
{
	public class MMOItemVariable : BaseVariable, IMMOItemVariable, Variable
	{
		public static IMMOItemVariable FromSFSArray(ISFSArray sfsa)
		{
			return new MMOItemVariable(sfsa.GetUtfString(0), sfsa.GetElementAt(2), sfsa.GetByte(1));
		}

		public MMOItemVariable(string name, object val, int type)
			: base(name, val, type)
		{
		}

		public MMOItemVariable(string name, object val)
			: base(name, val)
		{
		}

		public override string ToString()
		{
			return "[MMOItemVar: " + name + ", type: " + type.ToString() + ", value: " + val?.ToString() + "]";
		}
	}
}
