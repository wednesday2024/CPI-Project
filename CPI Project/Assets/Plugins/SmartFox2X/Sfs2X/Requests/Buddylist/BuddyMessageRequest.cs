using Sfs2X.Entities;
using Sfs2X.Entities.Data;

namespace Sfs2X.Requests.Buddylist
{
	public class BuddyMessageRequest : GenericMessageRequest
	{
		public BuddyMessageRequest(string message, Buddy targetBuddy, ISFSObject parameters)
		{
			type = 5;
			base.message = message;
			recipient = targetBuddy?.Id ?? (-1);
			base.parameters = parameters;
		}

		public BuddyMessageRequest(string message, Buddy targetBuddy)
			: this(message, targetBuddy, null)
		{
		}
	}
}
