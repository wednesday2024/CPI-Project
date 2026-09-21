using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClubPenguin.Classic.MiniGames
{
    public class MouseInputObserver : MonoBehaviour
    {
        public string HorizontalAxisName = "Mouse X";
        public string VerticalAxisName = "Mouse Y";
        public float horizontalSpeed = 2f;
        public float verticalSpeed = 2f;

        public event Action<Vector3, Vector2> MouseMovedEvent;
        public event Action PrimaryMouseButtonDownEvent;

        private Vector2 lastMousePos;

        private void Awake()
        {
            if (Mouse.current != null)
                lastMousePos = Mouse.current.position.ReadValue();
        }

        private void Update()
        {
            if (Mouse.current == null)
                return;

            Vector2 currentMousePos = Mouse.current.position.ReadValue();

            // Calculate delta and scaled speed (like classic Input.GetAxis)
            Vector2 delta = currentMousePos - lastMousePos;
            float num = horizontalSpeed * delta.x * Time.deltaTime;
            float num2 = verticalSpeed * delta.y * Time.deltaTime;

            if (MouseMovedEvent != null && (num != 0f || num2 != 0f))
            {
                MouseMovedEvent(currentMousePos, new Vector2(num, num2));
            }

            if (Mouse.current.leftButton.wasPressedThisFrame && PrimaryMouseButtonDownEvent != null)
            {
                PrimaryMouseButtonDownEvent();
            }

            lastMousePos = currentMousePos;
        }

        private void OnDestroy()
        {
            MouseMovedEvent = null;
            PrimaryMouseButtonDownEvent = null;
        }
    }
}