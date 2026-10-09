using System.Collections;
using UnityEngine;
using ArcherCraft.Combat;

namespace ArcherCraft.Enemy
{
    public class EnemyBulletEmitter : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;

        private void Awake()
        {
            if (firePoint == null) firePoint = transform;
        }

        /// <summary>
        /// 플레이어를 직접 조준하여 단발 또는 점사 발사
        /// </summary>
        public void FireAimShot(Vector3 targetPosition, float bulletSpeed = 12f)
        {
            Vector3 dir = (targetPosition - firePoint.position).normalized;
            dir.y = 0;
            SpawnBullet(firePoint.position, dir, bulletSpeed);
        }

        /// <summary>
        /// 부채꼴 형태의 탄막 확산 발사 (Fan Spread)
        /// </summary>
        public void FireSpread(Vector3 targetPosition, int count = 5, float spreadAngle = 45f, float bulletSpeed = 11f)
        {
            Vector3 baseDir = (targetPosition - firePoint.position).normalized;
            baseDir.y = 0;

            float startAngle = -spreadAngle * 0.5f;
            float step = count > 1 ? spreadAngle / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float currentAngle = startAngle + (step * i);
                Quaternion rot = Quaternion.Euler(0, currentAngle, 0);
                Vector3 shootDir = rot * baseDir;
                SpawnBullet(firePoint.position, shootDir, bulletSpeed);
            }
        }

        /// <summary>
        /// 360도 전방위 원형 링 탄막 발사 (Ring Burst) - 카운터샷 테스트에 최적!
        /// </summary>
        public void FireRing(int bulletCount = 12, float bulletSpeed = 9f)
        {
            float stepAngle = 360f / bulletCount;
            for (int i = 0; i < bulletCount; i++)
            {
                float angle = stepAngle * i;
                Quaternion rot = Quaternion.Euler(0, angle, 0);
                Vector3 dir = rot * Vector3.forward;
                SpawnBullet(firePoint.position, dir, bulletSpeed);
            }
        }

        private void SpawnBullet(Vector3 position, Vector3 direction, float speed)
        {
            if (bulletPrefab == null) return;

            GameObject bulletObj = Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction));
            if (bulletObj.TryGetComponent<EnemyBullet>(out var bullet))
            {
                bullet.Initialize(direction, speed);
            }
        }
    }
}
