using System;
using UnityEngine;
using ArcherCraft.Combat;

namespace ArcherCraft.Player
{
    public class PlayerBow : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Transform firePoint;

        [Header("Charging Settings (차징 & 속사)")]
        [Tooltip("완충까지 걸리는 시간(초)")]
        [SerializeField] private float maxChargeTime = 1.0f;
        [Tooltip("이 시간 이하로 클릭 후 떼면 즉발 퀵샷 판정")]
        [SerializeField] private float quickShotThreshold = 0.2f;
        [Tooltip("차징 중 이동속도 배율")]
        [SerializeField] private float chargingMoveSpeedMultiplier = 0.55f;

        [Header("Fire Rate & Cooldown")]
        [SerializeField] private float fireCooldown = 0.35f;

        [Header("Arrow Specs")]
        [SerializeField] private float baseSpeed = 26f;
        [SerializeField] private float baseDamage = 35f;

        private PlayerController playerController;
        private float currentCharge; // 0.0 ~ 1.0
        private float chargeTimer;
        private float cooldownTimer;
        private bool isCharging;

        public event Action<float> OnChargeUpdated; // 0.0 ~ 1.0
        public event Action OnFired;

        public float CurrentChargeRatio => currentCharge;
        public bool IsCharging => isCharging;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            if (firePoint == null) firePoint = transform;
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;

            HandleBowInput();
        }

        private void HandleBowInput()
        {
            // 좌클릭 누르기 시작
            if (Input.GetMouseButtonDown(0) && cooldownTimer <= 0f)
            {
                isCharging = true;
                chargeTimer = 0f;
                currentCharge = 0f;
                playerController.SetSpeedMultiplier(chargingMoveSpeedMultiplier);
            }

            // 좌클릭 유지 중 (차징)
            if (isCharging && Input.GetMouseButton(0))
            {
                chargeTimer += Time.deltaTime;
                currentCharge = Mathf.Clamp01(chargeTimer / maxChargeTime);
                OnChargeUpdated?.Invoke(currentCharge);
            }

            // 좌클릭 릴리즈 (발사)
            if (isCharging && Input.GetMouseButtonUp(0))
            {
                FireBow();
            }
        }

        private void FireBow()
        {
            isCharging = false;
            playerController.SetSpeedMultiplier(1.0f);
            cooldownTimer = fireCooldown;

            Vector3 shootDirection = playerController.AimDirection;
            if (shootDirection.sqrMagnitude < 0.001f)
            {
                shootDirection = transform.forward;
            }

            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position + shootDirection * 0.5f;

            if (arrowPrefab != null)
            {
                GameObject arrowObj = Instantiate(arrowPrefab, spawnPosition, Quaternion.LookRotation(shootDirection));
                if (arrowObj.TryGetComponent<ArrowProjectile>(out var arrow))
                {
                    // 차징 비율에 따라 성능 전달 (0.0은 퀵샷, 1.0은 풀차지 에임샷)
                    arrow.Initialize(shootDirection, currentCharge, baseSpeed, baseDamage);
                }
            }

            OnFired?.Invoke();
            currentCharge = 0f;
            OnChargeUpdated?.Invoke(0f);
        }

        /// <summary>
        /// 외부 피격 또는 캔슬 시 차징 취소
        /// </summary>
        public void CancelCharge()
        {
            if (isCharging)
            {
                isCharging = false;
                currentCharge = 0f;
                playerController.SetSpeedMultiplier(1.0f);
                OnChargeUpdated?.Invoke(0f);
            }
        }
    }
}
