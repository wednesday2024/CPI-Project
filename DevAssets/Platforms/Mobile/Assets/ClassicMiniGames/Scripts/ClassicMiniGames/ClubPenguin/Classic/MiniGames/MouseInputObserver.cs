using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace ClubPenguin.Classic.MiniGames
{
    public class MouseInputObserver : MonoBehaviour
    {
        public float horizontalSpeed = 2f;
        public float verticalSpeed = 2f;

        public event Action<Vector3, Vector2> MouseMovedEvent;
        public event Action PrimaryMouseButtonDownEvent;

        private Vector2 lastTouchPos;
        private bool hasLastTouch = false;

        private void Awake()
        {
            EnhancedTouchSupport.Enable();
        }

        private void Update()
        {
            if (Touch.activeTouches.Count == 0)
            {
                hasLastTouch = false;
                return;
            }

            var touch = Touch.activeTouches[0];
            Vector2 currentTouchPos = touch.screenPosition;

            if (!hasLastTouch)
            {
                lastTouchPos = currentTouchPos;
                hasLastTouch = true;
            }

            Vector2 delta = currentTouchPos - lastTouchPos;
            float num = horizontalSpeed * delta.x * Time.deltaTime;
            float num2 = verticalSpeed * delta.y * Time.deltaTime;

            if (MouseMovedEvent != null && (num != 0f || num2 != 0f))
            {
                MouseMovedEvent(currentTouchPos, new Vector2(num, num2));
            }

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began && PrimaryMouseButtonDownEvent != null)
            {
                PrimaryMouseButtonDownEvent();
            }

            lastTouchPos = currentTouchPos;
        }

        private void OnDestroy()
        {
            EnhancedTouchSupport.Disable();
            MouseMovedEvent = null;
            PrimaryMouseButtonDownEvent = null;
        }
    }
}