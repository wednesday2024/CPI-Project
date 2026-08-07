using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Pizzatron
{
    public class mg_pt_InputManager : MonoBehaviour
    {
        private Camera m_camera;
        private mg_pt_ToppingBar m_toppingBar;
        public bool IsActive { get; set; } = true;

        private void Awake()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDestroy()
        {
            EnhancedTouchSupport.Disable();
        }

        public void Initialize(Camera p_camera, mg_pt_ToppingBar p_toppingBar)
        {
            m_camera = p_camera;
            m_toppingBar = p_toppingBar;
        }

        private void Update()
        {
            if (!IsActive || m_toppingBar == null || m_camera == null)
                return;

            foreach (var touch in Touch.activeTouches)
            {
                Vector3 worldPos = m_camera.ScreenToWorldPoint(new Vector3(touch.screenPosition.x, touch.screenPosition.y, m_camera.nearClipPlane));
                int fingerIndex = touch.finger.index;

                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        m_toppingBar.OnTouchStart(worldPos, fingerIndex);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                        m_toppingBar.OnTouchMove(worldPos, fingerIndex);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        m_toppingBar.OnTouchEnd(worldPos, fingerIndex);
                        break;
                }
            }
        }
    }
}
