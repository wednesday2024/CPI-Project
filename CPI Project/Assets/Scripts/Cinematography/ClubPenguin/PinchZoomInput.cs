using ClubPenguin.Core;
using Disney.LaunchPadFramework;
using Disney.MobileNetwork;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClubPenguin
{
	[DisallowMultipleComponent]
	internal class PinchZoomInput : MonoBehaviour
	{
		public float MouseSensitivity = 1f;

		public float TouchSensitivity = 2f;

		public float PreviousZoom;

		private EventDispatcher dispatcher;

		private void Start()
		{
			dispatcher = Service.Get<EventDispatcher>();
		}

		private void Update()
		{
			float num = PreviousZoom;

			// Use new Input System for touch and mouse input
			if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
			{
				var touch0 = Touchscreen.current.touches[0];
				var touch1 = Touchscreen.current.touches[1];

				var phase0 = touch0.phase.ReadValue();
				var phase1 = touch1.phase.ReadValue();

				if (phase0 == UnityEngine.InputSystem.TouchPhase.Moved && phase1 == UnityEngine.InputSystem.TouchPhase.Moved)
				{
					Vector2 position0 = touch0.position.ReadValue();
					Vector2 deltaPosition0 = touch0.delta.ReadValue();
					Vector2 position1 = touch1.position.ReadValue();
					Vector2 deltaPosition1 = touch1.delta.ReadValue();

					Rect pixelRect = Camera.main.pixelRect;
					if (pixelRect.Contains(position0) && pixelRect.Contains(position1))
					{
						float prevDist = (position0 - deltaPosition0 - (position1 - deltaPosition1)).magnitude;
						float currDist = (position0 - position1).magnitude;
						float delta = currDist - prevDist;
						num += delta * TouchSensitivity / (float)Screen.width;
					}
				}
			}
			else if (Mouse.current != null)
			{
				float mouseScroll = Mouse.current.scroll.ReadValue().y;
				num += mouseScroll * MouseSensitivity * 0.01f; // Mouse scroll is usually in 120 steps, scale it down
			}

			num = Mathf.Clamp(num, 0f, 1f);
			if (dispatcher != null && !Mathf.Approximately(num, PreviousZoom))
			{
				dispatcher.DispatchEvent(new InputEvents.ZoomEvent(num));
				PreviousZoom = num;
			}
		}
	}
}