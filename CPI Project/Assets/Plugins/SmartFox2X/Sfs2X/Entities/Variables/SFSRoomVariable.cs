using Sfs2X.Entities.Data;

namespace Sfs2X.Entities.Variables
{
	public class SFSRoomVariable : BaseVariable, RoomVariable, Variable
	{
		private bool isPrivate;

		private bool isPersistent;

		public bool IsPrivate
		{
			get
			{
				return isPrivate;
			}
			set
			{
				isPrivate = value;
			}
		}

		public bool IsPersistent
		{
			get
			{
				return isPersistent;
			}
			set
			{
				isPersistent = value;
			}
		}

		public static RoomVariable FromSFSArray(ISFSArray sfsa)
		{
			RoomVariable roomVariable = new SFSRoomVariable(sfsa.GetUtfString(0), sfsa.GetElementAt(2), sfsa.GetByte(1));
			roomVariable.IsPrivate = sfsa.GetBool(3);
			roomVariable.IsPersistent = sfsa.GetBool(4);
			return roomVariable;
		}

		public SFSRoomVariable(string name, object val, int type)
			: base(name, val, type)
		{
			isPrivate = false;
			isPersistent = false;
		}

		public SFSRoomVariable(string name, object val)
			: base(name, val)
		{
			isPrivate = false;
			isPersistent = false;
		}

		public override ISFSArray ToSFSArray()
		{
			ISFSArray iSFSArray = base.ToSFSArray();
			iSFSArray.AddBool(isPrivate);
			iSFSArray.AddBool(isPersistent);
			return iSFSArray;
		}

		public override string ToString()
		{
			return "[RoomVar: " + name + ", type: " + type.ToString() + ", value: " + val?.ToString() + ", private: " + isPrivate + "]";
		}
	}
}
