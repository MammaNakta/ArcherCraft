using UnityEngine;
using UnityEngine.UI;
using ArcherCraft.Player;

namespace ArcherCraft.UI
{
    public class AimReticleUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerBow playerBow;
        [SerializeField] private RectTransform crosshairRect;
        [SerializeField] private Image chargeFillImage;
        [SerializeField] private LineRenderer rangeRingLine;

        [Header("Settings")]
        [SerializeField] private float optimalRangeRadius = 8f;
        [SerializeField] private int ringSegments = 48;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color fullChargeColor = new Color(0.2f, 1f, 0.4f, 0.9f);

        private void Start()
        {
            if (playerBow == null)
            {
                playerBow = FindFirstObjectByType<PlayerBow>();
            }

            if (playerBow != null)
            {
                playerBow.OnChargeUpdated += UpdateChargeDisplay;
            }

            SetupRangeRing();
        }

        private void OnDestroy()
        {
            if (playerBow != null)
            {
                playerBow.OnChargeUpdated -= UpdateChargeDisplay;
            }
        }

        private void Update()
        {
            // 마우스 커서 위치에 크로스헤어 UI 배치
            if (crosshairRect != null)
            {
                crosshairRect.position = Input.mousePosition;
            }

            // 플레이어 발밑 사거리 링 위치 갱신
            if (rangeRingLine != null && playerBow != null)
            {
                rangeRingLine.transform.position = new Vector3(playerBow.transform.position.x, 0.05f, playerBow.transform.position.z);
            }
        }

        private void UpdateChargeDisplay(float chargeRatio)
        {
            if (chargeFillImage != null)
            {
                chargeFillImage.fillAmount = chargeRatio;
                chargeFillImage.color = chargeRatio >= 0.95f ? fullChargeColor : normalColor;
            }
        }

        private void SetupRangeRing()
        {
            if (rangeRingLine == null) return;

            rangeRingLine.positionCount = ringSegments + 1;
            rangeRingLine.useWorldSpace = false;
            rangeRingLine.loop = true;

            float angleStep = 360f / ringSegments;
            for (int i = 0; i <= ringSegments; i++)
            {
                float rad = Mathf.Deg2Rad * (angleStep * i);
                float x = Mathf.Sin(rad) * optimalRangeRadius;
                float z = Mathf.Cos(rad) * optimalRangeRadius;
                rangeRingLine.SetPosition(i, new Vector3(x, 0f, z));
            }
        }
    }
}
