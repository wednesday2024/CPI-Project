namespace Sfs2X.Util
{
	public class BlueBoxCfg
	{
		public bool IsActive { get; set; } = true;

		public bool UseHttps { get; set; } = false;

		public int PollingRate { get; set; } = 750;

		public ProxyCfg Proxy { get; set; } = new ProxyCfg();
	}
}
