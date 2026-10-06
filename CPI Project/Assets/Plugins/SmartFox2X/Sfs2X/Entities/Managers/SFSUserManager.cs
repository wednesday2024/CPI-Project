using System.Collections.Generic;

namespace Sfs2X.Entities.Managers
{
	public class SFSUserManager : IUserManager
	{
		private Dictionary<string, User> usersByName;

		private Dictionary<int, User> usersById;

		private readonly object listLock = new object();

		protected Room room;

		protected SmartFox sfs;

		public virtual int UserCount
		{
			get
			{
				lock (listLock)
				{
					return usersById.Count;
				}
			}
		}

		public SmartFox SmartFoxClient => sfs;

		protected void LogWarn(string msg)
		{
			if (sfs != null)
			{
				sfs.Log.Warn(msg);
			}
			else if (room != null && room.RoomManager != null)
			{
				room.RoomManager.SmartFoxClient.Log.Warn(msg);
			}
		}

		public SFSUserManager(SmartFox sfs)
		{
			this.sfs = sfs;
			usersByName = new Dictionary<string, User>();
			usersById = new Dictionary<int, User>();
		}

		public SFSUserManager(Room room)
		{
			this.room = room;
			usersByName = new Dictionary<string, User>();
			usersById = new Dictionary<int, User>();
		}

		public virtual bool ContainsUserName(string userName)
		{
			lock (listLock)
			{
				return usersByName.ContainsKey(userName);
			}
		}

		public virtual bool ContainsUserId(int userId)
		{
			lock (listLock)
			{
				return usersById.ContainsKey(userId);
			}
		}

		public virtual bool ContainsUser(User user)
		{
			lock (listLock)
			{
				return usersByName.ContainsValue(user);
			}
		}

		public virtual User GetUserByName(string userName)
		{
			lock (listLock)
			{
				User value = null;
				usersByName.TryGetValue(userName, out value);
				return value;
			}
		}

		public virtual User GetUserById(int userId)
		{
			lock (listLock)
			{
				User value = null;
				usersById.TryGetValue(userId, out value);
				return value;
			}
		}

		public virtual void AddUser(User user)
		{
			lock (listLock)
			{
				usersByName[user.Name] = user;
				usersById[user.Id] = user;
			}
		}

		public virtual void RemoveUser(User user)
		{
			lock (listLock)
			{
				usersByName.Remove(user.Name);
				usersById.Remove(user.Id);
			}
		}

		public virtual void RemoveUserById(int id)
		{
			lock (listLock)
			{
				if (ContainsUserId(id))
				{
					User user = usersById[id];
					RemoveUser(user);
				}
			}
		}

		public virtual List<User> GetUserList()
		{
			lock (listLock)
			{
				return new List<User>(usersById.Values);
			}
		}

		public virtual void ReplaceAll(List<User> newUserList)
		{
			lock (listLock)
			{
				usersByName.Clear();
				usersById.Clear();
				foreach (User newUser in newUserList)
				{
					AddUser(newUser);
				}
			}
		}
	}
}
