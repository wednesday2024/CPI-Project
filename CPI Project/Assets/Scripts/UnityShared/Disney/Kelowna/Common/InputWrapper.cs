using Disney.MobileNetwork;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

namespace Disney.Kelowna.Common
{
    public class InputWrapper : MonoBehaviour
    {
        private TouchEquivalent? fakeTouch;
        private bool? fakeLeftMouseButtonDown;
        private bool fakeMouseButtonChangedStateThisFrame;
        private Vector3? fakeMousePosition;

        private static InputWrapper instance
        {
            get { return Service.Get<InputWrapper>(); }
        }

        public static int touchCount
        {
            get
            {
                if (instance.fakeTouch.HasValue) return 1;
                // Use EnhancedTouch if enabled, otherwise fallback (should always use new system)
                return EnhancedTouch.EnhancedTouchSupport.enabled ? EnhancedTouch.Touch.activeTouches.Count : 0;
            }
        }

        public static Vector3 mousePosition
        {
            get
            {
                if (instance.fakeMousePosition.HasValue)
                    return instance.fakeMousePosition.Value;

                // Use Mouse.current from new Input System
                return Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
            }
        }

        public static TouchEquivalent GetTouch(int index)
        {
            if (instance.fakeTouch.HasValue)
                return instance.fakeTouch.Value;

            if (!EnhancedTouch.EnhancedTouchSupport.enabled)
                throw new System.InvalidOperationException("EnhancedTouch is not enabled!");

            if (index < 0 || index >= EnhancedTouch.Touch.activeTouches.Count)
                throw new System.IndexOutOfRangeException("No touch at this index.");

            var touches = EnhancedTouch.Touch.activeTouches;
            var touch = touches[index];

            // You may want to track previous time for deltaTime, here we just pass 0
            return TouchEquivalent.FromEnhancedTouch(touch, 0);
        }

        public static void SetTouch(int index, TouchEquivalent? touch)
        {
            instance.fakeTouch = touch;
        }

        public static bool GetMouseButtonDown(int button)
        {
            if (instance.fakeLeftMouseButtonDown.HasValue)
                return instance.fakeLeftMouseButtonDown.Value && instance.fakeMouseButtonChangedStateThisFrame;

            if (Mouse.current == null) return false;
            switch (button)
            {
                case 0: return Mouse.current.leftButton.wasPressedThisFrame;
                case 1: return Mouse.current.rightButton.wasPressedThisFrame;
                case 2: return Mouse.current.middleButton.wasPressedThisFrame;
                default: return false;
            }
        }

        public static bool GetMouseButtonUp(int button)
        {
            if (instance.fakeLeftMouseButtonDown.HasValue)
                return !instance.fakeLeftMouseButtonDown.Value && instance.fakeMouseButtonChangedStateThisFrame;

            if (Mouse.current == null) return false;
            switch (button)
            {
                case 0: return Mouse.current.leftButton.wasReleasedThisFrame;
                case 1: return Mouse.current.rightButton.wasReleasedThisFrame;
                case 2: return Mouse.current.middleButton.wasReleasedThisFrame;
                default: return false;
            }
        }

        public static bool GetMouseButton(int index)
        {
            if (instance.fakeLeftMouseButtonDown.HasValue)
                return instance.fakeLeftMouseButtonDown.Value;

            if (Mouse.current == null) return false;
            switch (index)
            {
                case 0: return Mouse.current.leftButton.isPressed;
                case 1: return Mouse.current.rightButton.isPressed;
                case 2: return Mouse.current.middleButton.isPressed;
                default: return false;
            }
        }

        public static void SetMouseButton(int index, bool? isPressed, Vector3? position)
        {
            if (isPressed.HasValue && isPressed != instance.fakeLeftMouseButtonDown)
                CoroutineRunner.StartPersistent(fakeMouseButtonStateChange(), instance, "mouseButtonChange");
            instance.fakeLeftMouseButtonDown = isPressed;
            instance.fakeMousePosition = position;
        }

        private static IEnumerator fakeMouseButtonStateChange()
        {
            instance.fakeMouseButtonChangedStateThisFrame = true;
            yield return null;
            instance.fakeMouseButtonChangedStateThisFrame = false;
        }
    }
}