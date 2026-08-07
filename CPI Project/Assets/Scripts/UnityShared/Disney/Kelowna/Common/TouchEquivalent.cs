using UnityEngine;
using UnityEngine.InputSystem;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityTouchPhase = UnityEngine.TouchPhase;

namespace Disney.Kelowna.Common
{
    public struct TouchEquivalent
    {
        public const int MOUSE_POINTER_ID = 909;

        public Vector2 deltaPosition { get; set; }
        public float deltaTime { get; set; }
        public int fingerId { get; set; }
        public UnityTouchPhase phase { get; set; }
        public Vector2 position { get; set; }
        public Vector2 rawPosition { get; set; }
        public int tapCount { get; set; }

        private static UnityTouchPhase ConvertPhase(InputTouchPhase phase)
        {
            switch (phase)
            {
                case InputTouchPhase.Began: return UnityTouchPhase.Began;
                case InputTouchPhase.Moved: return UnityTouchPhase.Moved;
                case InputTouchPhase.Stationary: return UnityTouchPhase.Stationary;
                case InputTouchPhase.Ended: return UnityTouchPhase.Ended;
                case InputTouchPhase.Canceled: return UnityTouchPhase.Canceled;
                default: return UnityTouchPhase.Canceled;
            }
        }

        // This is for UnityEngine.InputSystem.EnhancedTouch.Touch
        public static TouchEquivalent FromEnhancedTouch(EnhancedTouch.Touch touch, float prevTime = 0)
        {
            TouchEquivalent result = default(TouchEquivalent);
            result.fingerId = touch.finger.index;
            result.position = touch.screenPosition;
            result.rawPosition = touch.screenPosition;
            // deltaTime workaround: use difference between current and previous touch time
            result.deltaTime = prevTime > 0 ? (float)(touch.time - prevTime) : 0f;
            result.deltaPosition = touch.delta;
            result.phase = ConvertPhase(touch.phase);
            result.tapCount = touch.tapCount;
            return result;
        }

        // This is for mouse input
        public static TouchEquivalent FromMouseButton(int buttonIndex, Vector3 lastMousePosition)
        {
            TouchEquivalent result = default(TouchEquivalent);

            if (Mouse.current == null)
                return result;

            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (lastMousePosition == Vector3.zero)
                result.deltaPosition = Vector2.zero;
            else
                result.deltaPosition = (Vector2)mousePos - (Vector2)lastMousePosition;

            bool isDown = false, isPressed = false, isUp = false;
            switch (buttonIndex)
            {
                case 0:
                    isDown = Mouse.current.leftButton.wasPressedThisFrame;
                    isPressed = Mouse.current.leftButton.isPressed;
                    isUp = Mouse.current.leftButton.wasReleasedThisFrame;
                    break;
                case 1:
                    isDown = Mouse.current.rightButton.wasPressedThisFrame;
                    isPressed = Mouse.current.rightButton.isPressed;
                    isUp = Mouse.current.rightButton.wasReleasedThisFrame;
                    break;
                case 2:
                    isDown = Mouse.current.middleButton.wasPressedThisFrame;
                    isPressed = Mouse.current.middleButton.isPressed;
                    isUp = Mouse.current.middleButton.wasReleasedThisFrame;
                    break;
            }

            if (isPressed || isUp)
            {
                result.fingerId = MOUSE_POINTER_ID;
                result.position = mousePos;
                result.rawPosition = mousePos;
                result.deltaTime = Time.deltaTime;
                if (isUp)
                {
                    result.phase = UnityTouchPhase.Ended;
                }
                else if (isDown)
                {
                    result.phase = UnityTouchPhase.Began;
                }
                else
                {
                    result.phase = (result.deltaPosition.sqrMagnitude > 1f) ? UnityTouchPhase.Moved : UnityTouchPhase.Stationary;
                }
                result.tapCount = 1;
            }

            return result;
        }

        public static TouchEquivalent FromLeftMouseButton(Vector3 lastMousePosition)
        {
            return FromMouseButton(0, lastMousePosition);
        }
    }
}