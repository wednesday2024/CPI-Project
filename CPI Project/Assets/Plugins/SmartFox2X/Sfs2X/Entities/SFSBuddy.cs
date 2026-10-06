using System;
using System.Collections.Generic;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;

namespace Sfs2X.Entities
{
	public class SFSBuddy : Buddy
	{
		protected string name;

		protected int id;

		protected bool isBlocked;

		protected Dictionary<string, BuddyVariable> variables;

		protected bool isTemp;

		public int Id
		{
			get
			{
				return id;
			}
			set
			{
				id = value;
			}
		}

		public bool IsBlocked
		{
			get
			{
				return isBlocked;
			}
			set
			{
				isBlocked = value;
			}
		}

		public bool IsTemp => isTemp;

		public string Name => name;

		public bool IsOnline
		{
			get
			{
				BuddyVariable variable = GetVariable(ReservedBuddyVariables.BV_ONLINE);
				return (variable == null || variable.GetBoolValue()) && id > -1;
			}
		}

		public string State => GetVariable(ReservedBuddyVariables.BV_STATE)?.GetStringValue();

		public string NickName => GetVariable(ReservedBuddyVariables.BV_NICKNAME)?.GetStringValue();

		public List<BuddyVariable> Variables
		{
			get
			{
				lock (variables)
				{
					return new List<BuddyVariable>(variables.Values);
				}
			}
		}

		public static Buddy FromSFSArray(ISFSArray arr)
		{
			Buddy buddy = new SFSBuddy(arr.GetInt(0), arr.GetUtfString(1), arr.GetBool(2), arr.Size() > 4 && arr.GetBool(4));
			ISFSArray sFSArray = arr.GetSFSArray(3);
			for (int i = 0; i < sFSArray.Size(); i++)
			{
				BuddyVariable variable = SFSBuddyVariable.FromSFSArray(sFSArray.GetSFSArray(i));
				buddy.SetVariable(variable);
			}
			return buddy;
		}

		public SFSBuddy(int id, string name)
			: this(id, name, isBlocked: false, isTemp: false)
		{
		}

		public SFSBuddy(int id, string name, bool isBlocked)
			: this(id, name, isBlocked, isTemp: false)
		{
		}

		public SFSBuddy(int id, string name, bool isBlocked, bool isTemp)
		{
			this.id = id;
			this.name = name;
			this.isBlocked = isBlocked;
			variables = new Dictionary<string, BuddyVariable>();
			this.isTemp = isTemp;
		}

		public BuddyVariable GetVariable(string varName)
		{
			lock (variables)
			{
				if (variables.ContainsKey(varName))
				{
					return variables[varName];
				}
				return null;
			}
		}

		public List<BuddyVariable> GetOfflineVariables()
		{
			List<BuddyVariable> list = new List<BuddyVariable>();
			lock (variables)
			{
				foreach (BuddyVariable value in variables.Values)
				{
					if (value.Name[0] == Convert.ToChar(SFSBuddyVariable.OFFLINE_PREFIX))
					{
						list.Add(value);
					}
				}
			}
			return list;
		}

		public List<BuddyVariable> GetOnlineVariables()
		{
			List<BuddyVariable> list = new List<BuddyVariable>();
			lock (variables)
			{
				foreach (BuddyVariable value in variables.Values)
				{
					if (value.Name[0] != Convert.ToChar(SFSBuddyVariable.OFFLINE_PREFIX))
					{
						list.Add(value);
					}
				}
			}
			return list;
		}

		public bool ContainsVariable(string varName)
		{
			lock (variables)
			{
				return variables.ContainsKey(varName);
			}
		}

		public void SetVariable(BuddyVariable bVar)
		{
			lock (variables)
			{
				variables[bVar.Name] = bVar;
			}
		}

		public void SetVariables(ICollection<BuddyVariable> variables)
		{
			lock (variables)
			{
				foreach (BuddyVariable variable in variables)
				{
					SetVariable(variable);
				}
			}
		}

		public void RemoveVariable(string varName)
		{
			lock (variables)
			{
				variables.Remove(varName);
			}
		}

		public void ClearVolatileVariables()
		{
			List<string> list = new List<string>();
			lock (variables)
			{
				foreach (BuddyVariable value in variables.Values)
				{
					if (value.Name[0] != Convert.ToChar(SFSBuddyVariable.OFFLINE_PREFIX))
					{
						list.Add(value.Name);
					}
				}
				foreach (string item in list)
				{
					RemoveVariable(item);
				}
			}
		}

		public override string ToString()
		{
			return "[Buddy: " + name + ", id: " + id + "]";
		}
	}
}
