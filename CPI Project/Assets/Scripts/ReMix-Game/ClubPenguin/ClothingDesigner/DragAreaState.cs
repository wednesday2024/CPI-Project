using ClubPenguin.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.TouchPhase;

namespace ClubPenguin.ClothingDesigner
{
    public abstract class DragAreaState
    {
        public struct ITouch
        {
            public TouchPhase phase;
            public Vector2 position;
            public Vector2 deltaPosition;
            public int tapCount;

            private static Vector3 lastPosition;
            private static float lastClick;

            public static ITouch fromTouch(UnityEngine.Touch touch)
            {
                ITouch result = default(ITouch);
                result.phase = touch.phase;
                result.position = touch.position;
                result.deltaPosition = touch.deltaPosition;
                result.tapCount = touch.tapCount;
                return result;
            }

            public static ITouch fromMouse()
            {
                ITouch result = default(ITouch);
                result.tapCount = 0;

                Vector2 mousePosition = Vector2.zero;
                bool mouseDown = false, mouseUp = false, mouseHeld = false;

                if (Mouse.current != null)
                {
                    mousePosition = Mouse.current.position.ReadValue();
                    mouseDown = Mouse.current.leftButton.wasPressedThisFrame;
                    mouseUp = Mouse.current.leftButton.wasReleasedThisFrame;
                    mouseHeld = Mouse.current.leftButton.isPressed;
                }

                if (mouseDown)
                {
                    result.phase = TouchPhase.Began;
                    lastClick = Time.time;
                }
                else if (mouseUp)
                {
                    result.phase = TouchPhase.Ended;
                    if (Time.time - lastClick < 0.1f)
                    {
                        result.tapCount = 1;
                    }
                }
                else if (mouseHeld)
                {
                    if (lastPosition != (Vector3)mousePosition)
                    {
                        result.phase = TouchPhase.Moved;
                    }
                    else
                    {
                        result.phase = TouchPhase.Stationary;
                    }
                }
                else
                {
                    result.phase = TouchPhase.Canceled;
                }
                result.position = mousePosition;
                result.deltaPosition = mousePosition - (Vector2)lastPosition;
                lastPosition = mousePosition;
                return result;
            }
        }

        public const float MAX_DRAG_X = 5f;
        public const float MAX_DRAG_Y = 20f;
        public const float MIN_DRAG_Y = 3f;

        public float DragDeltaDampenX = 3f;

        public abstract void EnterState(CustomizerGestureModel currentGesture);

        public virtual void UpdateState()
        {
            // Touch input (new Input System)
            if (Touchscreen.current != null && Touchscreen.current.touches.Count == 1 && PlatformUtils.GetPlatformType() != PlatformType.Standalone)
            {
                var touchControl = Touchscreen.current.touches[0];
                TouchPhase phase = TouchPhase.Canceled;
                if (touchControl.press.wasPressedThisFrame)
                    phase = TouchPhase.Began;
                else if (touchControl.press.wasReleasedThisFrame)
                    phase = TouchPhase.Ended;
                else if (touchControl.press.isPressed)
                    phase = TouchPhase.Moved;
                else
                    phase = TouchPhase.Canceled;

                ITouch touch = new ITouch
                {
                    phase = phase,
                    position = touchControl.position.ReadValue(),
                    deltaPosition = Vector2.zero,
                    tapCount = 0 // Not tracked in new Input System
                };
                ProcessOneTouch(touch);
                return;
            }
            // Two-finger touch (pinch/zoom)
            if (Touchscreen.current != null && Touchscreen.current.touches.Count == 2)
            {
                ProcessTwoTouchPinchAndZoom();
                return;
            }
            ITouch mouseTouch = ITouch.fromMouse();
            if (mouseTouch.phase != TouchPhase.Canceled)
            {
                ProcessOneTouch(mouseTouch);
            }
        }

        public abstract void ExitState();

        protected virtual void ProcessOneTouch(ITouch touch)
        {
        }

        protected void ProcessTwoTouchPinchAndZoom()
        {
        }

        protected bool checkButtonDrag(Vector2 dragDelta)
        {
            float num = (PlatformUtils.GetPlatformType() == PlatformType.Standalone) ? (dragDelta.x / DragDeltaDampenX) : dragDelta.x;
            return dragDelta.y > 20f || (dragDelta.y > 3f && dragDelta.y >= num && num > -5f && num < 5f);
        }
    }
}