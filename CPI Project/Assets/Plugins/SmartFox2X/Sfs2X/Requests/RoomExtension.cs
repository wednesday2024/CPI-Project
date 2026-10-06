namespace Sfs2X.Requests
{
	public class RoomExtension
	{
		private string id;

		private string className;

		private string propertiesFile;

		public string Id => id;

		public string ClassName => className;

		public string PropertiesFile
		{
			get
			{
				return propertiesFile;
			}
			set
			{
				propertiesFile = value;
			}
		}

		public RoomExtension(string id, string className)
		{
			this.id = id;
			this.className = className;
			propertiesFile = "";
		}
	}
}
