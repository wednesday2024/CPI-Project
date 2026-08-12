using NUnit.Framework;
using UnityEngine;

namespace JetpackReboot
{
	public class mg_jr_PersistStatsInPlayerPrefs : mg_jr_IStatPersistenceStrategy
	{
		private const string DISTANCE_KEY = "mg_jr_BestDistance";

		private const string COINS_KEY = "mg_jr_MostCoins";

		public void Save(mg_jr_PlayerStats _toSave)
		{
			Assert.NotNull(_toSave, "Can't save a null object");
			PlayerPrefs.SetInt(GetPlatformKey("mg_jr_BestDistance"), _toSave.BestDistance);
			PlayerPrefs.SetInt(GetPlatformKey("mg_jr_MostCoins"), _toSave.MostCoins);
		}

		public mg_jr_PlayerStats LoadOrCreate()
		{
			int @int = PlayerPrefs.GetInt(GetPlatformKey("mg_jr_BestDistance"));
			int int2 = PlayerPrefs.GetInt(GetPlatformKey("mg_jr_MostCoins"));
			return new mg_jr_PlayerStats(@int, int2, this);
		}

		private static string GetPlatformKey(string key)
		{
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
			if (UnityEngine.Application.isEditor)
			{
				return "Editor_" + key;
			}
#endif
			return key;
		}
	}
}
