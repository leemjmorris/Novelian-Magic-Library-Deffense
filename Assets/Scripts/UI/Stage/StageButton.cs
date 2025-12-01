using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NovelianMagicLibraryDefense.Managers;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Individual stage button controller
    /// Handles locked/unlocked state with overlay effect
    /// CSV 데이터 연동으로 스테이지 정보 관리
    /// </summary>
    public class StageButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image stageImage;
        [SerializeField] private TextMeshProUGUI stageNumberText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private Button button;

        [Header("Settings")]
        [SerializeField] private int stageNumber;
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private bool isLocked = true;
        [SerializeField] private bool startLocked = true; // Inspector에서 초기 잠금 상태 설정

        // CSV에서 로드된 스테이지 데이터
        private StageData cachedStageData;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void Start()
        {
            // 시작할 때 스테이지 번호 표시
            SetStageNumber(stageNumber);

            // JML: StageProgressManager에서 해금 상태 확인
            if (StageProgressManager.Instance != null)
            {
                bool isUnlocked = StageProgressManager.Instance.IsStageUnlocked(stageNumber);
                SetLocked(!isUnlocked);
                Debug.Log($"[StageButton] Stage {stageNumber} - Unlocked: {isUnlocked}");
            }
            else
            {
                // StageProgressManager가 없으면 Inspector 설정값 사용
                SetLocked(startLocked);
                Debug.LogWarning("[StageButton] StageProgressManager not found, using Inspector setting");
            }
        }

        /// <summary>
        /// Set the locked state of this stage button
        /// </summary>
        public void SetLocked(bool locked)
        {
            isLocked = locked;

            // Show/hide lock overlay and icon
            if (lockOverlay != null)
                lockOverlay.SetActive(locked);

            if (lockIcon != null)
                lockIcon.SetActive(locked);

            // Change image color (darker when locked)
            if (stageImage != null)
                stageImage.color = locked ? lockedColor : unlockedColor;

            // Disable button interaction when locked
            if (button != null)
                button.interactable = !locked;
        }

        /// <summary>
        /// Get whether this stage is locked
        /// </summary>
        public bool IsLocked()
        {
            return isLocked;
        }

        /// <summary>
        /// Get the stage number
        /// </summary>
        public int GetStageNumber()
        {
            return stageNumber;
        }

        /// <summary>
        /// Set the stage number and update display
        /// </summary>
        public void SetStageNumber(int number)
        {
            stageNumber = number;
            if (stageNumberText != null)
                stageNumberText.text = number.ToString();
        }

        /// <summary>
        /// 버튼 클릭 시 호출 - CSV에서 스테이지 데이터 로드 후 SelectedStage에 저장
        /// Inspector의 Button OnClick 이벤트에 연결
        /// </summary>
        public void OnStageButtonClicked()
        {
            // CSV에서 stageNumber에 해당하는 스테이지 데이터 조회
            if (CSVLoader.Instance == null)
            {
                Debug.LogError("[StageButton] CSVLoader가 초기화되지 않음");
                return;
            }

            var table = CSVLoader.Instance.GetTable<StageData>();
            if (table == null)
            {
                Debug.LogError("[StageButton] StageTable이 로드되지 않음");
                return;
            }

            // Chapter_Number로 스테이지 찾기
            cachedStageData = table.Find(s => s.Chapter_Number == stageNumber);

            if (cachedStageData == null)
            {
                Debug.LogError($"[StageButton] stageNumber {stageNumber}에 해당하는 스테이지를 찾을 수 없음");
                return;
            }

            // SelectedStage에 저장 (씬 전환 후에도 유지)
            SelectedStage.Data = cachedStageData;

            Debug.Log($"[StageButton] 스테이지 선택: Stage_ID={cachedStageData.Stage_ID}, " +
                      $"Time_Limit={cachedStageData.Time_Limit}, Barrier_HP={cachedStageData.Barrier_HP}");
        }

        /// <summary>
        /// 현재 캐시된 스테이지 데이터 반환
        /// </summary>
        public StageData GetStageData()
        {
            return cachedStageData;
        }
    }
}
