using Disney.Kelowna.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace ClubPenguin.ObjectManipulation.Input
{
    public abstract class AbstractInputInteractionState
    {
        public LayerMask TargetLayerMask = -1;

        protected InteractionState state;

        protected Vector3 lastMousePositionWhenDown = Vector3.zero;

        protected float MinTimeToMoveInput;

        private static bool enhancedTouchInitialized = false;

        public InteractionState State
        {
            get
            {
                return state;
            }
            private set
            {
            }
        }

        public virtual void EnterState(LayerMask targetLayerMask, float minTimeToMoveInput)
        {
            MinTimeToMoveInput = minTimeToMoveInput;
            TargetLayerMask = targetLayerMask;
        }

        public virtual void ExitState()
        {
        }

        public virtual int Update()
        {
            // Ensure EnhancedTouch is enabled once
            if (!enhancedTouchInitialized)
            {
                if (!EnhancedTouchSupport.enabled)
                {
                    EnhancedTouchSupport.Enable();
                }
                enhancedTouchInitialized = true;
            }

            int num = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count;
            if (num == 1)
            {
                // If you need specific logic for single touch, implement here
            }

            // Mouse input using new Input System
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                num = 1;
                Vector2 mousePos = Mouse.current.position.ReadValue();
                if (!EventSystem.current.IsPointerOverGameObject() && !IsScreenPointOverUI(mousePos))
                {
                    processOneTouch(TouchEquivalent.FromLeftMouseButton(lastMousePositionWhenDown));
                    lastMousePositionWhenDown = mousePos;
                }
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame && !EventSystem.current.IsPointerOverGameObject())
            {
                processOneTouch(TouchEquivalent.FromLeftMouseButton(lastMousePositionWhenDown));
                lastMousePositionWhenDown = Vector3.zero;
            }

            return num;
        }

        private static bool IsScreenPointOverUI(Vector2 position)
        {
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = position;
            List<RaycastResult> list = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, list);
            return list.Count > 0;
        }

        protected abstract void processOneTouch(TouchEquivalent touch);

        protected GameObject raycastScreenPointToObject(Vector2 screenPosition, LayerMask mask)
        {
            GameObject result = null;
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, float.PositiveInfinity, mask, QueryTriggerInteraction.Collide))
            {
                result = hitInfo.transform.gameObject;
            }
            return result;
        }
    }
}
