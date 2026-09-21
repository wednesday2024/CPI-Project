using DisneyMobile.CoreUnitySystems;
using MinigameFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace JetpackReboot
{
    public class mg_jr_InputManager : MonoBehaviour
    {
        private Dictionary<int, bool> m_isWaitingForUp = new Dictionary<int, bool>();

        private void Awake()
        {
            mg_JetpackReboot active = MinigameManager.GetActive<mg_JetpackReboot>();
            active.InputManager = this;
            EnhancedTouchSupport.Enable();
        }

        private void OnDestroy()
        {
            EnhancedTouchSupport.Disable();
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
            // Enhanced Touch input
            foreach (var touch in Touch.activeTouches)
            {
                int fingerId = touch.finger.index;
                Vector2 position = touch.screenPosition;

                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        OnTouchDown(position, fingerId);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                    case UnityEngine.InputSystem.TouchPhase.Stationary:
                        OnTouchDrag(position, fingerId);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        OnTouchUp(position, fingerId);
                        break;
                }
            }

            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    OnTouchDown(mouse.position.ReadValue(), 0);
                }
                if (mouse.leftButton.isPressed)
                {
                    OnTouchDrag(mouse.position.ReadValue(), 0);
                }
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    OnTouchUp(mouse.position.ReadValue(), 0);
                }
            }
        }

        private void OnTouchDrag(Vector2 _position, int touchId = 0)
        {
            mg_JetpackReboot active = MinigameManager.GetActive<mg_JetpackReboot>();
            if (active != null && active.GameLogic != null)
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
                m_isWaitingForUp[touchId] = true;
                active.GameLogic.OnTouchPress(true, touchId, _position);
            }
        }

        private void OnTouchUp(Vector2 _position, int touchId = 0)
        {
            mg_JetpackReboot active = MinigameManager.GetActive<mg_JetpackReboot>();
            if (active != null && active.GameLogic != null)
            {
                active.GameLogic.OnTouchPress(false, touchId, _position);
                m_isWaitingForUp[touchId] = false;
            }
        }
    }
}