#if UNITY_WEBGL
using System.Collections.Generic;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;

namespace Sfs2X.Entities
{
	public class MMOItem : IMMOItem
	{
		private int id;

		private Vec3D aoiEntryPoint;

		private Dictionary<string, IMMOItemVariable> variables = new Dictionary<string, IMMOItemVariable>();

		public int Id => id;

		public Vec3D AOIEntryPoint
		{
			get
			{
				return aoiEntryPoint;
			}
			set
			{
				aoiEntryPoint = value;
			}
		}

		public static IMMOItem FromSFSArray(ISFSArray encodedItem)
		{
			IMMOItem iMMOItem = new MMOItem(encodedItem.GetInt(0));
			ISFSArray sFSArray = encodedItem.GetSFSArray(1);
			for (int i = 0; i < sFSArray.Size(); i++)
			{
				iMMOItem.SetVariable(MMOItemVariable.FromSFSArray(sFSArray.GetSFSArray(i)));
			}
			return iMMOItem;
		}

		public MMOItem(int id)
		{
			this.id = id;
		}

		public List<IMMOItemVariable> GetVariables()
		{
			lock (variables)
			{
				return new List<IMMOItemVariable>(variables.Values);
			}
		}

		public IMMOItemVariable GetVariable(string name)
		{
			lock (variables)
			{
				return variables[name];
			}
		}

		public void SetVariable(IMMOItemVariable variable)
		{
			lock (variables)
			{
				if (variable.IsNull())
				{
					variables.Remove(variable.Name);
				}
				else
				{
					variables[variable.Name] = variable;
				}
			}
		}

		public void SetVariables(List<IMMOItemVariable> variables)
		{
			lock (variables)
			{
				foreach (IMMOItemVariable variable in variables)
				{
					SetVariable(variable);
				}
			}
		}

		public bool ContainsVariable(string name)
		{
			lock (variables)
			{
				return variables.ContainsKey(name);
			}
		}
	}
}
#else
using System.Collections.Generic;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;

namespace Sfs2X.Entities
{
    public class MMOItem : IMMOItem
    {
        private int id;

        private Vec3D aoiEntryPoint;

        private Dictionary<string, IMMOItemVariable> variables = new Dictionary<string, IMMOItemVariable>();

        public int Id => id;

        public Vec3D AOIEntryPoint
        {
            get
            {
                return aoiEntryPoint;
            }
            set
            {
                aoiEntryPoint = value;
            }
        }

        public static IMMOItem FromSFSArray(ISFSArray encodedItem)
        {
            IMMOItem iMMOItem = new MMOItem(encodedItem.GetInt(0));
            ISFSArray sFSArray = encodedItem.GetSFSArray(1);
            for (int i = 0; i < sFSArray.Size(); i++)
            {
                iMMOItem.SetVariable(MMOItemVariable.FromSFSArray(sFSArray.GetSFSArray(i)));
            }
            return iMMOItem;
        }

        public MMOItem(int id)
        {
            this.id = id;
        }

        public List<IMMOItemVariable> GetVariables()
        {
            lock (variables)
            {
                return new List<IMMOItemVariable>(variables.Values);
            }
        }

        public IMMOItemVariable GetVariable(string name)
        {
            lock (variables)
            {
                return variables[name];
            }
        }

        public void SetVariable(IMMOItemVariable variable)
        {
            lock (variables)
            {
                if (variable.IsNull())
                {
                    variables.Remove(variable.Name);
                }
                else
                {
                    variables[variable.Name] = variable;
                }
            }
        }

        public void SetVariables(List<IMMOItemVariable> variables)
        {
            lock (variables)
            {
                foreach (IMMOItemVariable variable in variables)
                {
                    SetVariable(variable);
                }
            }
        }

        public bool ContainsVariable(string name)
        {
            lock (variables)
            {
                return variables.ContainsKey(name);
            }
        }
    }
}
#endif