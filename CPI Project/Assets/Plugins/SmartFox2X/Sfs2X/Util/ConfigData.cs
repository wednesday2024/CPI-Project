namespace Sfs2X.Util
{
	public class ConfigData
	{
		public string Host { get; set; } = "127.0.0.1";

		public int Port { get; set; } = 9933;

		public string UdpHost { get; set; } = "127.0.0.1";

		public int UdpPort { get; set; } = 9933;

		public string Zone { get; set; }

		public bool Debug { get; set; } = false;

		public int HttpPort { get; set; } = 8080;

		public int HttpsPort { get; set; } = 8443;

		public bool TcpNoDelay { get; set; } = false;

		public BlueBoxCfg BlueBox { get; } = new BlueBoxCfg();
	}
}
