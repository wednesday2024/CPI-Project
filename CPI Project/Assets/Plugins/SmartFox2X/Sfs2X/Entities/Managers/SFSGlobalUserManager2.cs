using System.Collections.Generic;

namespace Sfs2X.Entities.Managers
{
	public class SFSGlobalUserManager2 : SFSUserManager
	{
		public override int UserCount => GetUserList().Count;

		public SFSGlobalUserManager2(SmartFox sfs)
			: base(sfs)
		{
		}

		public override bool ContainsUser(User user)
		{
			bool result = false;
			List<Room> joinedRooms = sfs.JoinedRooms;
			if (joinedRooms != null)
			{
				foreach (Room item in joinedRooms)
				{
					if (item.ContainsUser(user))
					{
						result = true;
						break;
					}
				}
			}
			return result;
		}

		public override bool ContainsUserName(string userName)
		{
			bool result = false;
			List<Room> joinedRooms = sfs.JoinedRooms;
			if (joinedRooms != null)
			{
				foreach (Room item in joinedRooms)
				{
					if (item.GetUserByName(userName) != null)
					{
						result = true;
						break;
					}
				}
			}
			return result;
		}

		public override bool ContainsUserId(int userId)
		{
			bool result = false;
			List<Room> joinedRooms = sfs.JoinedRooms;
			if (joinedRooms != null)
			{
				foreach (Room item in joinedRooms)
				{
					if (item.GetUserById(userId) != null)
					{
						result = true;
						break;
					}
				}
			}
			return result;
		}

		public override User GetUserById(int userId)
		{
			if (sfs.MySelf.Id == userId)
			{
				return sfs.MySelf;
			}
			User user = null;
			List<Room> joinedRooms = sfs.JoinedRooms;
			if (joinedRooms != null)
			{
				foreach (Room item in joinedRooms)
				{
					user = item.GetUserById(userId);
					if (user != null)
					{
						break;
					}
				}
			}
			return user;
		}

		public override User GetUserByName(string userName)
		{
			if (sfs.MySelf.Name == userName)
			{
				return sfs.MySelf;
			}
			User user = null;
			List<Room> joinedRooms = sfs.JoinedRooms;
			if (joinedRooms != null)
			{
				foreach (Room item in joinedRooms)
				{
					user = item.GetUserByName(userName);
					if (user != null)
					{
						break;
					}
				}
			}
			return user;
		}

		public override List<User> GetUserList()
		{
			List<Room> joinedRooms = sfs.JoinedRooms;
			if (joinedRooms == null || joinedRooms.Count == 0)
			{
				List<User> list = new List<User>();
				if (sfs.MySelf != null)
				{
					list.Add(sfs.MySelf);
				}
				return list;
			}
			HashSet<User> hashSet = new HashSet<User>();
			foreach (Room item in joinedRooms)
			{
				hashSet.UnionWith(item.UserList);
			}
			return new List<User>(hashSet);
		}

		public override void AddUser(User user)
		{
		}

		public override void RemoveUser(User user)
		{
		}

		public override void RemoveUserById(int id)
		{
		}

		public override void ReplaceAll(List<User> userList)
		{
		}
	}
}
