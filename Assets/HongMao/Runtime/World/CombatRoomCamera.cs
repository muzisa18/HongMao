using UnityEngine;

namespace HongMao
{
    // Follow on a parent rig so hit shake remains local to the camera.
    public sealed class CombatRoomCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Camera sceneCamera;
        [SerializeField] Vector2 horizontalBounds = new(-32f, 32f);
        [SerializeField] Vector2 verticalBounds = new(-5f, 15f);
        [SerializeField] float followSpeed = 7f;

        public void Configure(Transform actor, Camera camera)
        {
            target = actor;
            sceneCamera = camera;
        }

        void LateUpdate()
        {
            if (target == null || sceneCamera == null) return;
            float halfHeight = sceneCamera.orthographicSize;
            float halfWidth = halfHeight * sceneCamera.aspect;
            Vector3 desired = new(
                ClampCenter(target.position.x, horizontalBounds, halfWidth),
                ClampCenter(target.position.y + 1.5f, verticalBounds, halfHeight), 0f);
            transform.position = Vector3.Lerp(transform.position, desired,
                1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime));
        }

        static float ClampCenter(float value, Vector2 bounds, float extent)
        {
            return extent * 2f >= bounds.y - bounds.x ? (bounds.x + bounds.y) * .5f
                : Mathf.Clamp(value, bounds.x + extent, bounds.y - extent);
        }
    }
}
