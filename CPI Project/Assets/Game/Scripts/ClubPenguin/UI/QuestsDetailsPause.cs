using ClubPenguin.Adventure;
using DevonLocalization.Core;
using Disney.LaunchPadFramework;
using Disney.MobileNetwork;
using UnityEngine;
using TMPro;

namespace ClubPenguin.UI
{
	public class QuestsDetailsPause : MonoBehaviour
	{
		public TMP_Text AdventureNameText;

		private Quest questData;

		public void Start()
		{
			Quest activeQuest = Service.Get<QuestService>().ActiveQuest;
			LoadQuestData(activeQuest);
		}

		public void LoadQuestData(Quest questData)
		{
			this.questData = questData;
			AdventureNameText.text = Service.Get<Localizer>().GetTokenTranslation(this.questData.Definition.Title);
		}

		public void PauseButtonPressed()
		{
			Service.Get<EventDispatcher>().DispatchEvent(new QuestEvents.SuspendQuest(Service.Get<QuestService>().ActiveQuest));
			Service.Get<EventDispatcher>().DispatchEvent(new TrayEvents.SelectTrayScreen("ControlsScreen"));
		}
	}
}
