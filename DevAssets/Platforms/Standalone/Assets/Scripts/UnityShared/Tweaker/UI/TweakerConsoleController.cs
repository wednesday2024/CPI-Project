using Disney.Kelowna.Common;
using System.Collections;
using System.Linq;
using Tweaker.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tweaker.UI
{
	public class TweakerConsoleController : MonoBehaviour, ITweakerConsoleController
	{
		public InspectorView InspectorViewPrefab;

		public HexGridController GridController;

		private IInspectorController inspector;

		private ITweakerLogger logger = LogManager.GetCurrentClassLogger();

		private bool isLandscape;

		private KeyBindingManager keyBindingManager;

		public Tweaker Tweaker
		{
			get;
			private set;
		}

		public TweakerTree Tree
		{
			get;
			private set;
		}

		public ITweakerSerializer Serializer
		{
			get;
			private set;
		}

		public BaseNode CurrentInspectorNode
		{
			get;
			private set;
		}

		public void OnEnable()
		{
			if (Tree != null)
			{
				Refresh();
			}
		}

		public static bool IsLandscape()
		{
			return Screen.height < Screen.width;
		}

		public static bool IsPortrait()
		{
			return !IsLandscape();
		}

		public void Update()
		{
			if (isLandscape != IsLandscape())
			{
				isLandscape = IsLandscape();
				if (GridController != null)
				{
					GridController.Resize();
				}
				if (inspector != null)
				{
					inspector.Resize();
				}
			}
		}

		public void Init(Tweaker tweaker, ITweakerSerializer serializer)
		{
			logger.Info("Init: " + tweaker);
			Tweaker = tweaker;
			Serializer = serializer;
			Tweaker.Scanner.ScanInstance(GridController);
			GridController.Init();
			Refresh();
			keyBindingManager = base.gameObject.transform.parent.gameObject.AddComponent<KeyBindingManager>();
			keyBindingManager.Init(Tweaker.Invokables.GetInvokables().Values.ToArray());
			CoroutineRunner.StartPersistent(checkShouldActivate(), this, "checkShouldActivate");
		}

		[Invokable("Tweaker.UI.Refresh", Description = "Repopulate the tweaker tree and refresh the hex grid.")]
		public void Refresh()
		{
			Tree = new TweakerTree(Tweaker);
			Tree.BuildTree();
			isLandscape = IsLandscape();
			GridController.Refresh();
		}

		public void ShowInspector(BaseNode nodeToInspect)
		{
			if (inspector != null && inspector.NodeType != nodeToInspect.Type)
			{
				inspector.Destroy();
				inspector = null;
			}
			if (inspector == null)
			{
				CreateInspector(nodeToInspect.Type);
			}
			CurrentInspectorNode = nodeToInspect;
			inspector.InspectNode(nodeToInspect);
		}

		private void CreateInspector(BaseNode.NodeType type)
		{
			InspectorView inspectorView = Object.Instantiate(InspectorViewPrefab);
			inspectorView.GetComponent<RectTransform>().SetParent(GetComponent<RectTransform>(), false);
			inspector = InspectorControllerFactory.MakeController(inspectorView, GridController, type);
			inspector.Closed += InspectorClosed;
		}

		private void InspectorClosed()
		{
			inspector = null;
			CurrentInspectorNode = null;
		}

		public void DestroyObject(GameObject go)
		{
			Object.Destroy(go);
		}

		public void HideConsole()
		{
			base.gameObject.SetActive(false);
		}

		public void ShowConsole()
		{
			base.gameObject.SetActive(true);
		}

		private IEnumerator checkShouldActivate()
		{
			while (true)
			{
				float timer = 0f;
				while (timer < 1f)
				{
					if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
					{
						base.gameObject.SetActive(!base.gameObject.activeSelf);
					}
					timer += Time.deltaTime;
					yield return null;
				}
				if (IsScreenTouched())
				{
					ShowConsole();
				}
			}
		}

		private bool IsScreenTouched()
		{
			bool flag = false;
			bool flag2 = false;
			var touchscreen = Touchscreen.current;
			if (touchscreen != null && touchscreen.touches.Count >= 2)
			{
				for (int i = 0; i < touchscreen.touches.Count; i++)
				{
					var touch = touchscreen.touches[i].ReadValue();
					float num = touch.position.y / (float)Screen.height;
					if (num < 0.2f)
					{
						flag = true;
					}
					else if (num > 0.8f)
					{
						flag2 = true;
					}
				}
			}
			return flag && flag2;
		}
	}
}