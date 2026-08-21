using ClubPenguin.Kelowna.Common.ImageCache;
using Disney.Kelowna.Common;
using Disney.MobileNetwork;
using Disney.Native;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClubPenguin.UI
{
	public class ClearCachePanelController : MonoBehaviour
	{
		public Button ClearCacheButton;

		[Header("Default state")]
		public GameObject ButtonImage;

		public GameObject ButtonText;

		[Header("Cache Clearing")]
		public GameObject Preloader;

		[Header("Done")]
		public GameObject DoneImage;

		public GameObject DoneText;

		public void OnClearButtonClicked()
		{
			ClearCacheButton.interactable = false;
			setActiveSafe(ButtonImage, false);
			setActiveSafe(ButtonText, false);
			setActiveSafe(Preloader, true);
			setActiveSafe(DoneImage, false);
			setActiveSafe(DoneText, false);
			clearImageCache();
			clearContentCache();
			CoroutineRunner.Start(waitForAnimationPreloader(), this, "waitForAnimationPreloader");
		}

		private IEnumerator waitForAnimationPreloader()
		{
			yield return new WaitForSeconds(2f);
			setActiveSafe(ButtonImage, false);
			setActiveSafe(ButtonText, false);
			setActiveSafe(Preloader, false);
			setActiveSafe(DoneImage, true);
			setActiveSafe(DoneText, true);
			CoroutineRunner.Start(waitForAnimationDone(), this, "waitForAnimationDone");
			if (MonoSingleton<NativeAccessibilityManager>.Instance.IsEnabled)
			{
				Text label = ClearCacheButton.GetComponentInChildren<Text>();
				if (label != null)
				{
					MonoSingleton<NativeAccessibilityManager>.Instance.Native.Speak(label.text);
				}
			}
		}

		private IEnumerator waitForAnimationDone()
		{
			yield return new WaitForSeconds(2f);
			setActiveSafe(ButtonImage, true);
			setActiveSafe(ButtonText, true);
			setActiveSafe(Preloader, false);
			setActiveSafe(DoneImage, false);
			setActiveSafe(DoneText, false);
			ClearCacheButton.interactable = true;
		}

		private static void setActiveSafe(GameObject target, bool active)
		{
			if (target != null)
			{
				target.SetActive(active);
			}
		}

		private void clearImageCache()
		{
			Service.Get<ImageCache>().ClearImageCache();
		}

		private void clearContentCache()
		{
			Caching.ClearCache();
		}
	}
}
