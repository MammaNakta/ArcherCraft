using UnityEngine;

namespace ArcherCraft.Combat
{
    [RequireComponent(typeof(SphereCollider))]
    public class ArrowProjectile : MonoBehaviour
    {
        [Header("Flight Settings")]
        [SerializeField] private float speed = 25f;
        [SerializeField] private float damage = 30f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private int pierceCount = 1; // 관통 가능한 탄막/적 수

        [Header("Counter-shot / Parry (핵심 기믹)")]
        [Tooltip("적 탄막을 쉽게 상쇄시키기 위한 관대한 히트박스 반경")]
        [SerializeField] private float magneticParryRadius = 0.8f;
        [SerializeField] private LayerMask enemyBulletLayer;

        [Header("VFX Prefabs")]
        [SerializeField] private GameObject parrySparkPrefab;
        [SerializeField] private GameObject hitFleshPrefab;
        [SerializeField] private GameObject hitObstaclePrefab;

        private Vector3 moveDirection;
        private float lifeTimer;
        private int remainingPierce;
        private bool isDisposed;

        public void Initialize(Vector3 direction, float chargeRatio, float baseSpeed = 25f, float baseDamage = 30f, int basePierce = 1)
        {
            moveDirection = direction.normalized;
            moveDirection.y = 0; // 쿼터뷰 수평면 유지

            // 차징 비율(0.0 ~ 1.0)에 따른 스펙 보정
            speed = Mathf.Lerp(baseSpeed * 0.7f, baseSpeed * 1.5f, chargeRatio);
            damage = Mathf.Lerp(baseDamage * 0.5f, baseDamage * 2.2f, chargeRatio);
            remainingPierce = basePierce + (chargeRatio >= 0.85f ? 2 : 0); // 풀차지 시 관통력 보너스

            transform.forward = moveDirection;
        }

        private void Update()
        {
            if (isDisposed) return;

            // 1. 전진 이동
            Vector3 step = moveDirection * (speed * Time.deltaTime);
            transform.position += step;

            // 2. 탄막 요격 (Magnetic Overlap Detection)
            CheckParryEnemyBullets();

            // 3. 수명 타이머
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifetime)
            {
                DestroySelf();
            }
        }

        /// <summary>
        /// 화살 주변의 적 탄막을 탐색하여 요격(상쇄) 처리
        /// </summary>
        private void CheckParryEnemyBullets()
        {
            Collider[] hitBullets;
            if (enemyBulletLayer.value != 0)
            {
                hitBullets = Physics.OverlapSphere(transform.position, magneticParryRadius, enemyBulletLayer);
            }
            else
            {
                // 레이어가 명시되지 않은 경우 태그 기반 검출
                hitBullets = Physics.OverlapSphere(transform.position, magneticParryRadius);
            }

            foreach (var col in hitBullets)
            {
                if (col.TryGetComponent<EnemyBullet>(out var bullet))
                {
                    if (bullet.CanBeParried)
                    {
                        // 탄막 상쇄 성공!
                        bullet.OnParried(col.transform.position);
                        SpawnEffect(parrySparkPrefab, col.transform.position);
                        Core.GameManager.Instance?.AddParryCount();

                        remainingPierce--;
                        if (remainingPierce <= 0)
                        {
                            DestroySelf();
                            return;
                        }
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDisposed) return;

            // 플레이어 자신의 콜라이더는 무시
            if (other.CompareTag("Player")) return;

            // 적 충돌
            if (other.CompareTag("Enemy") || other.GetComponent<IDamageable>() != null)
            {
                if (other.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage, transform.position, moveDirection);
                }

                SpawnEffect(hitFleshPrefab, transform.position);

                remainingPierce--;
                if (remainingPierce <= 0)
                {
                    DestroySelf();
                }
            }
            // 벽 / 장애물 충돌
            else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle") || other.CompareTag("Obstacle"))
            {
                SpawnEffect(hitObstaclePrefab, transform.position);
                DestroySelf();
            }
        }

        private void SpawnEffect(GameObject prefab, Vector3 position)
        {
            if (prefab != null)
            {
                Instantiate(prefab, position, Quaternion.identity);
            }
        }

        private void DestroySelf()
        {
            isDisposed = true;
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, magneticParryRadius);
        }
    }
}
