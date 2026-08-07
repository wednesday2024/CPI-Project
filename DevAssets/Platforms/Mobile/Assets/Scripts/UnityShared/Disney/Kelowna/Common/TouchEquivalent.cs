using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    // Fallback for IsExternalInit to support init-only properties in older .NET versions
    internal static class IsExternalInit { }
}
#endif


namespace Disney.Kelowna.Common
{
    /// <summary>
    /// A struct that adapts new Input System touch and mouse input to legacy TouchPhase format.
    /// </summary>
    public struct TouchEquivalent
    {
        public const int MOUSE_POINTER_ID = 909;

        public Vector2 DeltaPosition { get; init; }
        public float DeltaTime { get; init; }
        public int FingerId { get; init; }
        public UnityEngine.TouchPhase Phase { get; init; }
        public Vector2 Position { get; init; }
        public Vector2 RawPosition { get; init; }
        public int TapCount { get; init; }

        private static bool DebugLoggingEnabled => false; // Set to true for debugging

        /// <summary>
        /// Initializes EnhancedTouchSupport for touch input.
        /// </summary>
        public static void Initialize()
        {
            if (!EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Enable();
                if (DebugLoggingEnabled)
                    Debug.Log("TouchEquivalent: EnhancedTouchSupport enabled");
            }
        }

        /// <summary>
        /// Converts an EnhancedTouch.Touch to a TouchEquivalent.
        /// </summary>
        public static TouchEquivalent FromEnhancedTouch(EnhancedTouch.Touch touch, float prevTime = 0)
        {
            if (touch.finger == null)
            {
                if (DebugLoggingEnabled)
                    Debug.LogWarning("TouchEquivalent.FromEnhancedTouch: Touch has no finger");
                return default;
            }

            var result = new TouchEquivalent
            {
                FingerId = touch.finger.index,
                Position = touch.screenPosition,
                RawPosition = touch.screenPosition,
                DeltaTime = prevTime > 0 ? (float)(touch.time - prevTime) : Time.deltaTime,
                DeltaPosition = touch.delta,
                Phase = ConvertPhase(touch.phase),
                TapCount = touch.tapCount
            };

            if (DebugLoggingEnabled)
                Debug.Log($"TouchEquivalent.FromEnhancedTouch: fingerId={result.FingerId}, phase={result.Phase}, position={result.Position}, delta={result.DeltaPosition}, deltaTime={result.DeltaTime}, tapCount={result.TapCount}");
            return result;
        }

        /// <summary>
        /// Converts mouse input to a TouchEquivalent for the specified button.
        /// </summary>
        public static TouchEquivalent FromMouseButton(int buttonIndex, Vector3 lastMousePosition)
        {
            if (Mouse.current == null)
            {
                if (DebugLoggingEnabled)
                    Debug.LogWarning("TouchEquivalent.FromMouseButton: Mouse.current is null");
                return default;
            }

            var mouse = Mouse.current;
            Vector2 mousePos = mouse.position.ReadValue();
            bool isDown, isPressed, isUp;

            switch (buttonIndex)
            {
                case 0:
                    isDown = mouse.leftButton.wasPressedThisFrame;
                    isPressed = mouse.leftButton.isPressed;
                    isUp = mouse.leftButton.wasReleasedThisFrame;
                    break;
                case 1:
                    isDown = mouse.rightButton.wasPressedThisFrame;
                    isPressed = mouse.rightButton.isPressed;
                    isUp = mouse.rightButton.wasReleasedThisFrame;
                    break;
                case 2:
                    isDown = mouse.middleButton.wasPressedThisFrame;
                    isPressed = mouse.middleButton.isPressed;
                    isUp = mouse.middleButton.wasReleasedThisFrame;
                    break;
                default:
                    if (DebugLoggingEnabled)
                        Debug.LogWarning($"TouchEquivalent.FromMouseButton: Invalid button index {buttonIndex}");
                    return default;
            }

            if (!isPressed && !isUp)
                return default;

            var result = new TouchEquivalent
            {
                FingerId = MOUSE_POINTER_ID,
                Position = mousePos,
                RawPosition = mousePos,
                DeltaPosition = lastMousePosition == Vector3.zero ? Vector2.zero : mousePos - (Vector2)lastMousePosition,
                DeltaTime = Time.deltaTime,
                Phase = isUp ? UnityEngine.TouchPhase.Ended :
                        isDown ? UnityEngine.TouchPhase.Began :
                        mousePos != (Vector2)lastMousePosition ? UnityEngine.TouchPhase.Moved : UnityEngine.TouchPhase.Stationary,
                TapCount = isDown ? 1 : 0
            };

            if (DebugLoggingEnabled)
                Debug.Log($"TouchEquivalent.FromMouseButton: fingerId={result.FingerId}, phase={result.Phase}, position={result.Position}, delta={result.DeltaPosition}, deltaTime={result.DeltaTime}, buttonIndex={buttonIndex}");
            return result;
        }

        public static TouchEquivalent FromLeftMouseButton(Vector3 lastMousePosition)
        {
            return FromMouseButton(0, lastMousePosition);
        }

        private static UnityEngine.TouchPhase ConvertPhase(UnityEngine.InputSystem.TouchPhase phase)
        {
            var result = phase switch
            {
                UnityEngine.InputSystem.TouchPhase.Began => UnityEngine.TouchPhase.Began,
                UnityEngine.InputSystem.TouchPhase.Moved => UnityEngine.TouchPhase.Moved,
                UnityEngine.InputSystem.TouchPhase.Stationary => UnityEngine.TouchPhase.Stationary,
                UnityEngine.InputSystem.TouchPhase.Ended => UnityEngine.TouchPhase.Ended,
                UnityEngine.InputSystem.TouchPhase.Canceled => UnityEngine.TouchPhase.Canceled,
                _ => UnityEngine.TouchPhase.Canceled
            };
            if (DebugLoggingEnabled)
                Debug.Log($"TouchEquivalent.ConvertPhase: Input={phase}, Output={result}");
            return result;
        }
    }
}