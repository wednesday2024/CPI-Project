using DisneyMobile.CoreUnitySystems;
using MinigameFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace JetpackReboot
{
	public class mg_jr_InputManager : MonoBehaviour
	{
		private bool m_isMouseDown = false;

		private Dictionary<int, bool> m_isWaitingForUp = new Dictionary<int, bool>();

		private void Awake()
		{
			mg_JetpackReboot active = MinigameManager.GetActive<mg_JetpackReboot>();
			active.InputManager = this;
		}

		public void Prepare(Camera _camera)
		{
			InputManager.AddCamera(_camera);
		}

		private void OnDisable()
		{
			foreach (int key in m_isWaitingForUp.Keys)
			{
				if (m_isWaitingForUp[key])
				{
					OnTouchUp(Vector2.zero, key);
				}
			}
		}

		private void Update()
		{
			// Use the new Input System for mouse and touch handling
			Vector3 mousePosition = Vector3.zero;

			// Mouse input
			if (Mouse.current != null)
			{
				mousePosition = Mouse.current.position.ReadValue();

				if (Mouse.current.leftButton.wasPressedThisFrame)
				{
					m_isMouseDown = true;
					OnTouchDown(new Vector2(mousePosition.x, mousePosition.y));
				}
				else if (Mouse.current.leftButton.wasReleasedThisFrame)
				{
					m_isMouseDown = false;
					OnTouchUp(new Vector2(mousePosition.x, mousePosition.y));
				}

				OnTouchDrag(new Vector2(mousePosition.x, mousePosition.y));
			}

			// Touch input
			if (Touchscreen.current != null)
			{
				foreach (var touch in Touchscreen.current.touches)
				{
					// Only process active touches
					if (!touch.press.isPressed && !touch.press.wasPressedThisFrame && !touch.press.wasReleasedThisFrame)
						continue;

					int fingerId = touch.touchId.ReadValue();
					Vector2 position = touch.position.ReadValue();

					switch (touch.phase.ReadValue())
					{
						case UnityEngine.InputSystem.TouchPhase.Began:
							OnTouchDown(position, fingerId);
							break;
						case UnityEngine.InputSystem.TouchPhase.Moved:
							OnTouchDrag(position, fingerId);
							break;
						case UnityEngine.InputSystem.TouchPhase.Ended:
							OnTouchUp(position, fingerId);
							break;
						case UnityEngine.InputSystem.TouchPhase.Canceled:
							OnTouchUp(position, fingerId);
							break;
						case UnityEngine.InputSystem.TouchPhase.Stationary:
							break;
						default:
							DisneyMobile.CoreUnitySystems.Logger.LogWarning(touch.phase.ReadValue(), "Unknown touch event");
							break;
					}
				}
			}
		}

		private bool IsTouchOrMouseClickOverUI(int _touchId)
		{
			// For mouse (_touchId == -1 or 0), and for touch (_touchId >= 0)
			if (EventSystem.current == null)
				return false;

			if (_touchId < 0)
				return EventSystem.current.IsPointerOverGameObject();
			else
				return EventSystem.current.IsPointerOverGameObject(_touchId);
		}

		private void OnTouchDrag(Vector2 _position, int touchId = 0)
		{
			mg_JetpackReboot active = MinigameManager.GetActive<mg_JetpackReboot>();
			if (active != null && active.GameLogic != null && (!IsTouchOrMouseClickOverUI(touchId) || (m_isWaitingForUp.ContainsKey(touchId) && m_isWaitingForUp[touchId])))
			{
				active.GameLogic.OnTouchDrag(touchId, _position);
			}
		}

		private void OnTouchDown(Vector2 _position, int touchId = 0)
		{
			mg_JetpackReboot active = MinigameManager.GetActive<mg_JetpackReboot>();
			if (active != null && active.GameLogic != null)
			{
				if (!m_isWaitingForUp.ContainsKey(touchId))
				{
					m_isWaitingForUp.Add(touchId, false);
				}
				if (!IsTouchOrMouseClickOverUI(touchId))
				{
					m_isWaitingForUp[touchId] = true;
					active.GameLogic.OnTouchPress(true, touchId, _position);
				}
			}
		}

		private void OnTouchUp(Vector2 _position, int touchId = 0)
		{
			mg_JetpackReboot active = MinigameManager.GetActive<mg_JetpackReboot>();
			if (active != null && active.GameLogic != null && (!IsTouchOrMouseClickOverUI(touchId) || (m_isWaitingForUp.ContainsKey(touchId) && m_isWaitingForUp[touchId])))
			{
				active.GameLogic.OnTouchPress(false, touchId, _position);
				m_isWaitingForUp[touchId] = false;
			}
		}
	}
}