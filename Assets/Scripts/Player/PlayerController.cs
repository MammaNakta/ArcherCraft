using UnityEngine;

namespace ArcherCraft.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6.5f;
        [SerializeField] private float dashSpeed = 16f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 1.0f;

        [Header("Melee Kick / Knockback (초근접 대처)")]
        [SerializeField] private float kickRadius = 2.2f;
        [SerializeField] private float kickKnockbackForce = 12f;
        [SerializeField] private float kickDamage = 15f;
        [SerializeField] private float kickCooldown = 1.2f;

        [Header("Layers")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask enemyLayer;

        private CharacterController controller;
        private Camera mainCamera;
        private Vector3 moveInput;
        private Vector3 aimPoint;

        private float speedMultiplier = 1.0f;
        private float dashTimer;
        private float dashCooldownTimer;
        private float kickCooldownTimer;
        private bool isDashing;
        private Vector3 dashDirection;

        public Vector3 AimDirection { get; private set; }
        public Vector3 AimWorldPosition => aimPoint;
        public bool IsDashing => isDashing;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            HandleInput();
            HandleAimRotation();
            HandleMovement();
            HandleMeleeKick();
        }

        private void HandleInput()
        {
            // 이동 입력
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            moveInput = new Vector3(horizontal, 0f, vertical).normalized;

            // 대시 / 회피 입력 (Space)
            dashCooldownTimer -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Space) && dashCooldownTimer <= 0f && !isDashing)
            {
                StartDash();
            }

            // 근접 발차기/밀쳐내기 입력 (F키 또는 마우스 우클릭)
            kickCooldownTimer -= Time.deltaTime;
            if ((Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1)) && kickCooldownTimer <= 0f)
            {
                PerformMeleeKick();
            }
        }

        private void HandleAimRotation()
        {
            if (mainCamera == null) return;

            // 마우스 커서 위치를 바닥 평면(Y = 플레이어 Y)에 투영
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, transform.position);

            if (groundPlane.Raycast(ray, out float distance))
            {
                aimPoint = ray.GetPoint(distance);
                aimPoint.y = transform.position.y;

                Vector3 direction = (aimPoint - transform.position).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    AimDirection = direction;
                    transform.forward = direction;
                }
            }
        }

        private void HandleMovement()
        {
            if (isDashing)
            {
                controller.Move(dashDirection * (dashSpeed * Time.deltaTime));
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0f)
                {
                    isDashing = false;
                }
                return;
            }

            Vector3 velocity = moveInput * (moveSpeed * speedMultiplier);
            velocity.y = -9.81f; // 기본 중력
            controller.Move(velocity * Time.deltaTime);
        }

        private void StartDash()
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput : transform.forward;
        }

        private void HandleMeleeKick()
        {
            // 쿨타임 관리
        }

        private void PerformMeleeKick()
        {
            kickCooldownTimer = kickCooldown;

            // 전방 원뿔/원형 범위 내 적 넉백
            Vector3 kickCenter = transform.position + transform.forward * (kickRadius * 0.5f);
            Collider[] hitEnemies = Physics.OverlapSphere(kickCenter, kickRadius, enemyLayer.value != 0 ? enemyLayer : ~0);

            foreach (var col in hitEnemies)
            {
                if (col.gameObject == gameObject) continue;

                if (col.CompareTag("Enemy") || col.GetComponent<Combat.IDamageable>() != null)
                {
                    // 대미지
                    if (col.TryGetComponent<Combat.IDamageable>(out var damageable))
                    {
                        damageable.TakeDamage(kickDamage, col.transform.position, transform.forward);
                    }

                    // 넉백 (Rigidbody 지원 시)
                    if (col.TryGetComponent<Rigidbody>(out var rb))
                    {
                        Vector3 knockDir = (col.transform.position - transform.position).normalized;
                        knockDir.y = 0.1f;
                        rb.AddForce(knockDir * kickKnockbackForce, ForceMode.Impulse);
                    }
                }
            }
        }

        /// <summary>
        /// 활 차징 등에 따른 이동 속도 배율 제어 (예: 차징 중 0.5f로 감속)
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + transform.forward * (kickRadius * 0.5f), kickRadius);
        }
    }
}
