using ClubPenguin.Classic.MiniGames;
using MinigameFramework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IceFishing
{
    public class mg_if_InputManager : MonoBehaviour
    {
        private int m_fingerIndex = -1;
        private mg_if_GameLogic m_logic;
        private Camera m_camera;

        public void Initialize(Camera camera, mg_if_GameLogic logic)
        {
            m_camera = camera;
            m_logic = logic;
        }

        private void Update()
        {
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                for (int i = 0; i < Touchscreen.current.touches.Count; i++)
                {
                    var touch = Touchscreen.current.touches[i].ReadValue();
                    int fingerIndex = i;
                    Vector2 position = touch.position;
                    Vector2 startPosition = touch.startPosition;
                    var phase = touch.phase;

                    switch (phase)
                    {
                        case UnityEngine.InputSystem.TouchPhase.Began:
                            OnTouchStart(fingerIndex, position, startPosition);
                            break;
                        case UnityEngine.InputSystem.TouchPhase.Moved:
                        case UnityEngine.InputSystem.TouchPhase.Stationary:
                            OnTouchDown(fingerIndex, position);
                            break;
                        case UnityEngine.InputSystem.TouchPhase.Ended:
                        case UnityEngine.InputSystem.TouchPhase.Canceled:
                            OnTouchUp(fingerIndex, position, startPosition);
                            break;
                    }
                }
            }
        }

        private void OnTouchStart(int fingerIndex, Vector2 position, Vector2 startPosition)
        {
            if (m_fingerIndex < 0)
            {
                m_fingerIndex = fingerIndex;
            }
        }

        private void OnTouchUp(int fingerIndex, Vector2 position, Vector2 startPosition)
        {
            if (fingerIndex == m_fingerIndex)
            {
                float num = Mathf.Abs(startPosition.y - position.y);
                if (num <= 10f)
                {
                    m_logic.OnSimpleTap(position);
                }
                m_fingerIndex = -1;
            }
        }

        private void OnTouchDown(int fingerIndex, Vector2 position)
        {
            if (m_fingerIndex == fingerIndex)
            {
                m_logic.OnTouchDown(position);
            }
        }
    }
}