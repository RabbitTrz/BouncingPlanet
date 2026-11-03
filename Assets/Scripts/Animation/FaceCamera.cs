using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>让 Sprite 子物体在旋转相机下保持屏幕正立。</summary>
    [DisallowMultipleComponent]
    public sealed class FaceCamera : MonoBehaviour
    {
        public bool copyCameraRotation = true;
        public Vector3 localEulerOffset;

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            if (copyCameraRotation)
                transform.rotation = cam.transform.rotation * Quaternion.Euler(localEulerOffset);
        }
    }
}
