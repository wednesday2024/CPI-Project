using System.Collections.Generic;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Managers;
using Sfs2X.Entities.Variables;
using Sfs2X.Exceptions;

namespace Sfs2X.Entities
{
	public class SFSRoom : Room
	{
		protected int id;

		protected string name;

		protected string groupId;

		protected bool isGame;

		protected bool isHidden;

		protected bool isJoined;

		protected bool isPasswordProtected;

		protected bool isManaged;

		protected Dictionary<string, RoomVariable> variables;

		protected Dictionary<object, object> properties;

		protected IUserManager userManager;

		protected int maxUsers;

		protected int maxSpectators;

		protected int userCount;

		protected int specCount;

		protected IRoomManager roomManager;

		public int Id => id;

		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				name = value;
			}
		}

		public string GroupId => groupId;

		public bool IsGame
		{
			get
			{
				return isGame;
			}
			set
			{
				isGame = value;
			}
		}

		public bool IsHidden
		{
			get
			{
				return isHidden;
			}
			set
			{
				isHidden = value;
			}
		}

		public bool IsJoined
		{
			get
			{
				return isJoined;
			}
			set
			{
				isJoined = value;
			}
		}

		public bool IsPasswordProtected
		{
			get
			{
				return isPasswordProtected;
			}
			set
			{
				isPasswordProtected = value;
			}
		}

		public bool IsManaged
		{
			get
			{
				return isManaged;
			}
			set
			{
				isManaged = value;
			}
		}

		public int MaxSpectators
		{
			get
			{
				return maxSpectators;
			}
			set
			{
				maxSpectators = value;
			}
		}

		public Dictionary<object, object> Properties
		{
			get
			{
				return properties;
			}
			set
			{
				properties = value;
			}
		}

		public int UserCount
		{
			get
			{
				if (!isJoined)
				{
					return userCount;
				}
				if (isGame)
				{
					return PlayerList.Count;
				}
				return userManager.UserCount;
			}
			set
			{
				userCount = value;
			}
		}

		public int MaxUsers
		{
			get
			{
				return maxUsers;
			}
			set
			{
				maxUsers = value;
			}
		}

		public int Capacity => maxUsers + maxSpectators;

		public int SpectatorCount
		{
			get
			{
				if (!isGame)
				{
					return 0;
				}
				if (isJoined)
				{
					return SpectatorList.Count;
				}
				return specCount;
			}
			set
			{
				specCount = value;
			}
		}

		public List<User> UserList => userManager.GetUserList();

		public List<User> PlayerList
		{
			get
			{
				List<User> list = new List<User>();
				foreach (User user in userManager.GetUserList())
				{
					if (user.IsPlayerInRoom(this))
					{
						list.Add(user);
					}
				}
				return list;
			}
		}

		public List<User> SpectatorList
		{
			get
			{
				List<User> list = new List<User>();
				foreach (User user in userManager.GetUserList())
				{
					if (user.IsSpectatorInRoom(this))
					{
						list.Add(user);
					}
				}
				return list;
			}
		}

		public IRoomManager RoomManager
		{
			get
			{
				return roomManager;
			}
			set
			{
				if (roomManager != null)
				{
					throw new SFSError("Room manager already assigned. Room: " + this);
				}
				roomManager = value;
			}
		}

		public static Room FromSFSArray(ISFSArray sfsa)
		{
			bool flag = sfsa.Size() == 14;
			Room room = null;
			room = ((!flag) ? new SFSRoom(sfsa.GetInt(0), sfsa.GetUtfString(1), sfsa.GetUtfString(2)) : new MMORoom(sfsa.GetInt(0), sfsa.GetUtfString(1), sfsa.GetUtfString(2)));
			room.IsGame = sfsa.GetBool(3);
			room.IsHidden = sfsa.GetBool(4);
			room.IsPasswordProtected = sfsa.GetBool(5);
			room.UserCount = sfsa.GetShort(6);
			room.MaxUsers = sfsa.GetShort(7);
			ISFSArray sFSArray = sfsa.GetSFSArray(8);
			if (sFSArray.Size() > 0)
			{
				List<RoomVariable> list = new List<RoomVariable>();
				for (int i = 0; i < sFSArray.Size(); i++)
				{
					list.Add(SFSRoomVariable.FromSFSArray(sFSArray.GetSFSArray(i)));
				}
				room.SetVariables(list);
			}
			if (room.IsGame)
			{
				room.SpectatorCount = sfsa.GetShort(9);
				room.MaxSpectators = sfsa.GetShort(10);
			}
			if (flag)
			{
				MMORoom mMORoom = room as MMORoom;
				mMORoom.DefaultAOI = Vec3D.fromArray(sfsa.GetElementAt(11));
				if (!sfsa.IsNull(13))
				{
					mMORoom.LowerMapLimit = Vec3D.fromArray(sfsa.GetElementAt(12));
					mMORoom.HigherMapLimit = Vec3D.fromArray(sfsa.GetElementAt(13));
				}
			}
			return room;
		}

		public SFSRoom(int id, string name)
		{
			Init(id, name, "default");
		}

		public SFSRoom(int id, string name, string groupId)
		{
			Init(id, name, groupId);
		}

		private void Init(int id, string name, string groupId)
		{
			this.id = id;
			this.name = name;
			this.groupId = groupId;
			isJoined = (isGame = (isHidden = false));
			isManaged = true;
			userCount = (specCount = 0);
			variables = new Dictionary<string, RoomVariable>();
			properties = new Dictionary<object, object>();
			userManager = new SFSUserManager(this);
		}

		public List<RoomVariable> GetVariables()
		{
			lock (variables)
			{
				return new List<RoomVariable>(variables.Values);
			}
		}

		public RoomVariable GetVariable(string name)
		{
			lock (variables)
			{
				if (!variables.ContainsKey(name))
				{
					return null;
				}
				return variables[name];
			}
		}

		public User GetUserByName(string name)
		{
			return userManager.GetUserByName(name);
		}

		public User GetUserById(int id)
		{
			return userManager.GetUserById(id);
		}

		public void RemoveUser(User user)
		{
			userManager.RemoveUser(user);
		}

		public void SetVariable(RoomVariable roomVariable)
		{
			lock (variables)
			{
				if (roomVariable.IsNull())
				{
					variables.Remove(roomVariable.Name);
				}
				else
				{
					variables[roomVariable.Name] = roomVariable;
				}
			}
		}

		public void SetVariables(ICollection<RoomVariable> roomVariables)
		{
			lock (variables)
			{
				foreach (RoomVariable roomVariable in roomVariables)
				{
					SetVariable(roomVariable);
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

		public void AddUser(User user)
		{
			userManager.AddUser(user);
		}

		public bool ContainsUser(User user)
		{
			return userManager.ContainsUser(user);
		}

		public override string ToString()
		{
			return "[Room: " + name + ", Id: " + id + ", GroupId: " + groupId + "]";
		}

		public override bool Equals(object other)
		{
			if (other is Room)
			{
				Room room = (Room)other;
				return room.Id == id;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return id.GetHashCode();
		}

		public void Merge(Room anotherRoom)
		{
			if (IsJoined)
			{
				return;
			}
			lock (variables)
			{
				variables.Clear();
				foreach (RoomVariable variable in anotherRoom.GetVariables())
				{
					variables[variable.Name] = variable;
				}
			}
			userManager.ReplaceAll(anotherRoom.UserList);
		}
	}
}
