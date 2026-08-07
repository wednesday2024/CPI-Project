using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("OpenCPIsland/Utilities/Lock Position To Zero")]
public class LockPositionToZero : MonoBehaviour
{
    void LateUpdate()
    {
        ApplyLock();
    }

    void ApplyLock()
    {
    
    if (lockPosition)
        transform.localPosition = Vector3.zero;
    
    if (lockRotation)
        transform.localRotation = Quaternion.identity;
    }

    [Header("Lock Configuration")]
    [SerializeField] private bool lockPosition = false;
    [SerializeField] private bool lockRotation = false;
}
