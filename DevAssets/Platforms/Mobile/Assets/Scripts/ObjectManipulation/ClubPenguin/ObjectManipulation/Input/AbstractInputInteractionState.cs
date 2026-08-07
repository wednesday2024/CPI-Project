using Disney.Kelowna.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ClubPenguin.ObjectManipulation.Input
{
	public abstract class AbstractInputInteractionState
	{
		public LayerMask TargetLayerMask = -1;

		protected InteractionState state;

		protected Vector3 lastMousePositionWhenDown = Vector3.zero;

		protected float MinTimeToMoveInput;

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
            int num = InputWrapper.touchCount; // Use InputWrapper for touch count

            // Process touch input
            if (num == 1)
            {
                // Assuming InputWrapper.GetTouch(0) returns a TouchEquivalent
                TouchEquivalent touchEq = InputWrapper.GetTouch(0);
                if (touchEq.Phase != UnityEngine.TouchPhase.Canceled) // Check if touch is valid
                {
                    processOneTouch(touchEq);
                }
                return num; // Return early if touch is handled
            }
            else if (num == 2) // Handle two-finger input if needed, similar to DragAreaState
            {
                // Process two-finger input (e.g., pinch/zoom)
                // This would require a new method in AbstractInputInteractionState or a more complex handling
                // For now, we'll focus on single touch/mouse
                return num;
            }

            // Process mouse input if no touch is active
            if (InputWrapper.GetMouseButton(0)) // Check if left mouse button is held down
            {
                if (!EventSystem.current.IsPointerOverGameObject() && !IsScreenPointOverUI(InputWrapper.mousePosition))
                {
                    // Use TouchEquivalent.FromLeftMouseButton with InputWrapper.mousePosition
                    processOneTouch(TouchEquivalent.FromLeftMouseButton(lastMousePositionWhenDown));
                    lastMousePositionWhenDown = InputWrapper.mousePosition;
                }
                num = 1; // Indicate that one input is active (mouse)
            }
            else if (InputWrapper.GetMouseButtonUp(0)) // Check if left mouse button was released
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    processOneTouch(TouchEquivalent.FromLeftMouseButton(lastMousePositionWhenDown));
                    lastMousePositionWhenDown = Vector3.zero;
                }
                num = 0; // No active input after release
            }
            else
            {
                // If no touch and no mouse button is pressed, consider it canceled or no input
                // This might need more nuanced handling depending on desired behavior
                // For example, if a mouse button was previously held and now isn't, it should be 'Ended'
                // The current TouchEquivalent.FromMouse() in DragAreaState handles this by returning Canceled
                // We might need a similar mechanism here or ensure the state machine transitions correctly.
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
