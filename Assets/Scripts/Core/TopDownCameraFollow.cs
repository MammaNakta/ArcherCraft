using UnityEngine;

namespace ArcherCraft.Core
{
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Target & Offset")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 16f, -10f); // 약 60도 쿼터뷰 앵글
        [SerializeField] private float smoothSpeed = 8f;

        [Header("Aim Peek (조준 방향 시야 확장)")]
        [Tooltip("마우스 커서 방향으로 카메라를 살짝 이동시키는 배율")]
        [SerializeField] private float aimPeekMultiplier = 0.25f;
        [SerializeField] private float maxPeekDistance = 4.0f;

        private Vector3 currentVelocity;

        private void LateUpdate()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
                return;
            }

            Vector3 desiredPosition = target.position + offset;

            // Aim Peek 계산 (마우스 커서 방향 반영)
            Ray ray = Camera.main != null ? Camera.main.ScreenPointToRay(Input.mousePosition) : default;
            Plane groundPlane = new Plane(Vector3.up, target.position);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 mouseWorldPoint = ray.GetPoint(distance);
                Vector3 aimOffset = (mouseWorldPoint - target.position) * aimPeekMultiplier;
                aimOffset = Vector3.ClampMagnitude(aimOffset, maxPeekDistance);
                desiredPosition += new Vector3(aimOffset.x, 0f, aimOffset.z);
            }

            // 부드러운 카메라 이동
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / smoothSpeed);
            transform.LookAt(target.position + Vector3.up * 1.0f);
        }
    }
}
