using NUnit.Framework;
using UnityEngine;

namespace JetpackReboot
{
	public class mg_jr_PersistGoalsInPlayerPrefs : mg_jr_IGoalPersistenceStrategy
	{
		private const string KEY_PREFIX = "mg_jr_";

		private const string LEVEL_SUFIX = "_level";

		private const string PROGRESS_SUFIX = "_progress";

		public void SaveGoalProgressFor(mg_jr_Goal _goal)
		{
			Assert.NotNull(_goal, "Can't save a null object");
			PlayerPrefs.SetInt(GetPlatformKey("mg_jr_" + _goal.GoalName + "_level"), _goal.CurrentLevel);
			PlayerPrefs.SetFloat(GetPlatformKey("mg_jr_" + _goal.GoalName + "_progress"), _goal.Progress);
		}

		public int LoadLevelFor(mg_jr_Goal _goal)
		{
			Assert.NotNull(_goal, "Can't load data for a null object");
			return PlayerPrefs.GetInt(GetPlatformKey("mg_jr_" + _goal.GoalName + "_level"));
		}

		public float LoadProgresFor(mg_jr_Goal _goal)
		{
			Assert.NotNull(_goal, "Can't save a null object");
			return PlayerPrefs.GetFloat(GetPlatformKey("mg_jr_" + _goal.GoalName + "_progress"));
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
