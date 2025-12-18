using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Issue #476 - 도전던전 전용 UI
    /// 타이머, 스턴 게이지, 시간 경고 효과
    /// </summary>
    public class BossDungeonUI : MonoBehaviour
    {
        [Header("Timer UI")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private GameObject timeWarningEffect;
        [SerializeField] private Color normalTimeColor = Color.white;
        [SerializeField] private Color warningTimeColor = Color.yellow;
        [SerializeField] private Color criticalTimeColor = Color.red;

        [Header("Stun Gauge UI")]
        [SerializeField] private GameObject stunGaugePanel;
        [SerializeField] private Slider stunGaugeSlider;  // Slider 사용
        [SerializeField] private TextMeshProUGUI stunGaugeText;
        [SerializeField] private GameObject characterStunEffect;  // 캐릭터들 스턴 효과

        [Header("Attack Countdown UI")]
        [SerializeField] private TextMeshProUGUI attackCountdownText;  // 보스 공격까지 남은 시간

        [Header("Floor Info")]
        [SerializeField] private TextMeshProUGUI floorText;

        [Header("Result Panels")]
        [SerializeField] private BossDungeonClearPanel clearPanel;   // 승리 패널
        [SerializeField] private BossDungeonFailedPanel failedPanel; // 패배 패널

        // 초기 시간 (타이머 Fill용)
        private float initialTime;

        private void Awake()
        {
            // Issue #476: 결과 패널 비활성화
            if (clearPanel != null)
                clearPanel.gameObject.SetActive(false);

            if (failedPanel != null)
                failedPanel.gameObject.SetActive(false);

            // 초기 상태
            if (timeWarningEffect != null)
                timeWarningEffect.SetActive(false);

            if (characterStunEffect != null)
                characterStunEffect.SetActive(false);

            // 공격 카운트다운 초기화
            if (attackCountdownText != null)
                attackCountdownText.text = "";

            // 스턴 게이지 초기화 (0에서 시작)
            if (stunGaugeSlider != null)
            {
                stunGaugeSlider.minValue = 0f;
                stunGaugeSlider.maxValue = 1f;
                stunGaugeSlider.value = 0f;
            }
        }

        /// <summary>
        /// 던전 정보로 UI 초기화
        /// </summary>
        public void Initialize(BossDungeonData dungeonData)
        {
            if (dungeonData == null) return;

            initialTime = dungeonData.Time_Limit;

            // 스테이지 번호 표시
            if (floorText != null)
            {
                floorText.text = $"{dungeonData.Floor_Index}층";
            }

            // 타이머 초기화
            UpdateTimer(dungeonData.Time_Limit);

            // 스턴 게이지 초기화
            UpdateStunGauge(0f, dungeonData.Stun_Gauge);
        }

        /// <summary>
        /// 타이머 UI 업데이트
        /// </summary>
        public void UpdateTimer(float remainingTime)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";

                // 시간에 따른 색상 변경
                if (remainingTime <= 10f)
                {
                    timerText.color = criticalTimeColor;
                }
                else if (remainingTime <= 30f)
                {
                    timerText.color = warningTimeColor;
                }
                else
                {
                    timerText.color = normalTimeColor;
                }
            }

            // Fill 이미지 업데이트
            if (timerFillImage != null && initialTime > 0)
            {
                timerFillImage.fillAmount = remainingTime / initialTime;
            }
        }

        /// <summary>
        /// 시간 경고 효과 표시
        /// </summary>
        public void ShowTimeWarning(bool isCritical)
        {
            if (timeWarningEffect != null)
            {
                timeWarningEffect.SetActive(true);

                // 경고 효과 애니메이션 (깜빡임)
                PlayWarningEffectAsync(isCritical).Forget();
            }

            Debug.Log($"[BossDungeonUI] 시간 경고! Critical: {isCritical}");
        }

        /// <summary>
        /// 경고 효과 애니메이션
        /// </summary>
        private async UniTaskVoid PlayWarningEffectAsync(bool isCritical)
        {
            if (timeWarningEffect == null) return;

            float duration = isCritical ? 3f : 2f;
            float elapsed = 0f;
            float blinkInterval = isCritical ? 0.15f : 0.3f;

            while (elapsed < duration)
            {
                timeWarningEffect.SetActive(!timeWarningEffect.activeSelf);
                await UniTask.Delay((int)(blinkInterval * 1000));
                elapsed += blinkInterval;
            }

            timeWarningEffect.SetActive(false);
        }

        /// <summary>
        /// 스턴 게이지 UI 업데이트
        /// </summary>
        public void UpdateStunGauge(float current, float max)
        {
            if (stunGaugeSlider != null && max > 0)
            {
                stunGaugeSlider.value = current / max;
            }

            if (stunGaugeText != null)
            {
                int percent = Mathf.RoundToInt((current / max) * 100f);
                stunGaugeText.text = $"{percent}%";
            }
        }

        /// <summary>
        /// 캐릭터 스턴 효과 표시 (결계 스턴 게이지 100 도달 시)
        /// </summary>
        public void ShowCharacterStunEffect(float duration)
        {
            if (characterStunEffect != null)
            {
                characterStunEffect.SetActive(true);
                HideCharacterStunEffectAfterDelayAsync(duration).Forget();
            }

            Debug.Log($"[BossDungeonUI] 캐릭터들 스턴! 지속시간: {duration}초");
        }

        /// <summary>
        /// 캐릭터 스턴 효과 숨기기
        /// </summary>
        private async UniTaskVoid HideCharacterStunEffectAfterDelayAsync(float delay)
        {
            await UniTask.Delay((int)(delay * 1000));

            if (characterStunEffect != null)
            {
                characterStunEffect.SetActive(false);
            }
        }

        /// <summary>
        /// 공격 카운트다운 업데이트 (보스가 공격하기까지 남은 시간)
        /// </summary>
        public void UpdateAttackCountdown(float remainingSeconds)
        {
            if (attackCountdownText == null)
            {
                Debug.LogWarning("[BossDungeonUI] attackCountdownText가 null입니다! Inspector에서 연결해주세요.");
                return;
            }

            if (remainingSeconds <= 0f)
            {
                attackCountdownText.text = "";
            }
            else
            {
                int seconds = Mathf.CeilToInt(remainingSeconds);
                attackCountdownText.text = $"다음 공격까지 {seconds}초";

                // 3초 이하면 빨간색
                if (remainingSeconds <= 3f)
                {
                    attackCountdownText.color = criticalTimeColor;
                }
                else
                {
                    attackCountdownText.color = normalTimeColor;
                }
            }
        }

        /// <summary>
        /// 클리어 결과 표시
        /// </summary>
        public void ShowClearResult(float remainingTime)
        {
            if (clearPanel != null)
            {
                clearPanel.Show(remainingTime);
            }
        }

        /// <summary>
        /// 실패 결과 표시
        /// </summary>
        public void ShowFailResult(string reason)
        {
            if (failedPanel != null)
            {
                failedPanel.Show(reason);
            }
        }
    }
}
