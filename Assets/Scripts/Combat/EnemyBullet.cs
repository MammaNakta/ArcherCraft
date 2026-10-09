using UnityEngine;

namespace ArcherCraft.Combat
{
    [RequireComponent(typeof(SphereCollider))]
    public class EnemyBullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float speed = 12f;
        [SerializeField] private float damage = 15f;
        [SerializeField] private float lifetime = 6f;
        [SerializeField] private bool canBeParried = true;

        [Header("Effects")]
        [SerializeField] private GameObject parrySparkEffectPrefab;
        [SerializeField] private GameObject hitEffectPrefab;

        private Vector3 moveDirection;
        private float lifeTimer;
        private bool isDestroyed;

        public bool CanBeParried => canBeParried;
        public float Damage => damage;

        public void Initialize(Vector3 direction, float customSpeed = -1f, float customDamage = -1f)
        {
            moveDirection = direction.normalized;
            moveDirection.y = 0; // 쿼터뷰 수평면 유지

            if (customSpeed > 0f) speed = customSpeed;
            if (customDamage > 0f) damage = customDamage;

            transform.forward = moveDirection;
        }

        private void Update()
        {
            if (isDestroyed) return;

            transform.position += moveDirection * (speed * Time.deltaTime);

            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDestroyed) return;

            // 플레이어 피격 판정
            if (other.CompareTag("Player"))
            {
                if (other.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage, transform.position, -moveDirection);
                }

                SpawnEffect(hitEffectPrefab);
                DestroyBullet();
            }
            // 벽 / 장애물 충돌
            else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle") || other.CompareTag("Obstacle"))
            {
                SpawnEffect(hitEffectPrefab);
                DestroyBullet();
            }
        }

        /// <summary>
        /// 화살과 충돌하여 상쇄(패링)되었을 때 호출됨
        /// </summary>
        public void OnParried(Vector3 parryPoint)
        {
            if (isDestroyed || !canBeParried) return;

            SpawnEffect(parrySparkEffectPrefab, parryPoint);
            DestroyBullet();
        }

        private void SpawnEffect(GameObject effectPrefab, Vector3? position = null)
        {
            if (effectPrefab != null)
            {
                Vector3 spawnPos = position ?? transform.position;
                Instantiate(effectPrefab, spawnPos, Quaternion.identity);
            }
        }

        private void DestroyBullet()
        {
            isDestroyed = true;
            Destroy(gameObject);
        }
    }
}
