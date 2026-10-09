using UnityEngine;
using ArcherCraft.Combat;

namespace ArcherCraft.Enemy
{
    [RequireComponent(typeof(EnemyBulletEmitter))]
    public class EnemyAI : MonoBehaviour
    {
        public enum AttackPattern
        {
            AimShot,
            SpreadShot,
            RingBurst
        }

        [Header("Detection & Movement")]
        [SerializeField] private float detectionRadius = 16f;
        [SerializeField] private float attackRadius = 12f;
        [SerializeField] private float keepDistance = 6f; // 적정 카이팅 거리
        [SerializeField] private float moveSpeed = 3.5f;

        [Header("Combat Patterns")]
        [SerializeField] private float attackInterval = 2.0f;
        [SerializeField] private AttackPattern defaultPattern = AttackPattern.SpreadShot;

        private Transform playerTarget;
        private EnemyBulletEmitter emitter;
        private float attackTimer;

        private void Awake()
        {
            emitter = GetComponent<EnemyBulletEmitter>();
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        private void Update()
        {
            if (playerTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTarget = player.transform;
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTarget.position);

            if (dist <= detectionRadius)
            {
                LookAtPlayer();

                // 적정 거리 유지 이동
                if (dist > keepDistance + 1f)
                {
                    // 전진
                    MoveTowards(playerTarget.position, moveSpeed);
                }
                else if (dist < keepDistance - 1f)
                {
                    // 후퇴
                    MoveTowards(transform.position + (transform.position - playerTarget.position), moveSpeed * 0.7f);
                }

                // 공격
                if (dist <= attackRadius)
                {
                    attackTimer -= Time.deltaTime;
                    if (attackTimer <= 0f)
                    {
                        ExecuteAttack();
                        attackTimer = attackInterval;
                    }
                }
            }
        }

        private void LookAtPlayer()
        {
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.forward = Vector3.Slerp(transform.forward, dir, Time.deltaTime * 8f);
            }
        }

        private void MoveTowards(Vector3 targetPos, float speed)
        {
            Vector3 dir = (targetPos - transform.position).normalized;
            dir.y = 0;
            transform.position += dir * (speed * Time.deltaTime);
        }

        private void ExecuteAttack()
        {
            switch (defaultPattern)
            {
                case AttackPattern.AimShot:
                    emitter.FireAimShot(playerTarget.position);
                    break;
                case AttackPattern.SpreadShot:
                    emitter.FireSpread(playerTarget.position, count: 5, spreadAngle: 40f);
                    break;
                case AttackPattern.RingBurst:
                    emitter.FireRing(bulletCount: 14);
                    break;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRadius);
        }
    }
}
