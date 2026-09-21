using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmoothieSmash
{
	public class SmoothieSmashInputObserver : MonoBehaviour
	{
		public Vector2 CurrentSteering;

		public event Action<Vector2, Vector2> SteeringChangedEvent;

		private const float StickDeadZoneSqr = 0.04f;

		private void Update()
		{
			Vector2 keyboardVec = Vector2.zero;
			Vector2 gamepadVec = Vector2.zero;

			Keyboard kb = Keyboard.current;
			if (kb != null)
			{
				float x = 0f;
				float y = 0f;

				if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
				{
					x -= 1f;
				}
				if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
				{
					x += 1f;
				}
				if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
				{
					y += 1f;
				}
				if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
				{
					y -= 1f;
				}

				if (x != 0f || y != 0f)
				{
					keyboardVec = new Vector2(x, y).normalized;
				}
			}

			Gamepad gp = Gamepad.current;
			if (gp != null)
			{
				gamepadVec = gp.leftStick.ReadValue();
				if (gamepadVec.sqrMagnitude < StickDeadZoneSqr)
				{
					gamepadVec = Vector2.zero;
				}
			}

			// Keyboard must work even when a controller is connected.
			Vector2 vector = (keyboardVec != Vector2.zero) ? keyboardVec : gamepadVec;

			if (vector != CurrentSteering && SteeringChangedEvent != null)
			{
				SteeringChangedEvent(CurrentSteering, vector);
			}
			CurrentSteering = vector;
		}

		private void OnDestroy()
		{
			SteeringChangedEvent = null;
		}
	}
}
