using System.Security.Principal;

namespace Sfs2X.WebSocketSharp.Net
{
	public class HttpBasicIdentity : GenericIdentity
	{
		private string _password;

		public virtual string Password => _password;

		internal HttpBasicIdentity(string username, string password)
			: base(username, "Basic")
		{
			_password = password;
		}
	}
}
