// 훈련소 UI 컨트롤러 (Issue #458)
// UGUI 기반 UI 제어
namespace Novelian.Training
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using System.Collections.Generic;

    /// <summary>
    /// 훈련소 UI 컨트롤러 (UGUI)
    /// - 설정 드롭다운 제어
    /// - 버튼 OnClick 이벤트 처리
    /// - 실시간 DPS/데미지 표시
    /// </summary>
    public class TrainingUIController : MonoBehaviour
    {
        #region Serialized Fields - References

        [Header("Manager Reference")]
        [SerializeField] private TrainingManager trainingManager;

        #endregion

        #region Serialized Fields - Dropdowns

        [Header("Dropdowns")]
        [SerializeField] private TMP_Dropdown characterDropdown;
        [SerializeField] private TMP_Dropdown gradeDropdown;
        [SerializeField] private TMP_Dropdown enhancementDropdown;
        [SerializeField] private TMP_Dropdown mainSkillBookmarkDropdown;
        [SerializeField] private TMP_Dropdown supportSkillBookmarkDropdown;
        [SerializeField] private TMP_Dropdown statBookmarkDropdown;

        #endregion

        #region Serialized Fields - Info Display (Damage Info)

        [Header("Damage Info Display")]
        [SerializeField] private TMP_Text baseDamageText;
        [SerializeField] private TMP_Text totalBonusText;
        [SerializeField] private TMP_Text finalDamageText;
        [SerializeField] private TMP_Text mainSkillNameText;
        [SerializeField] private TMP_Text attackSpeedPreviewText;
        [SerializeField] private TMP_Text expectedDPSText;

        #endregion

        #region Serialized Fields - Info Display (Realtime)

        [Header("Realtime Display")]
        [SerializeField] private TMP_Text realtimeDPSText;
        [SerializeField] private TMP_Text totalDamageText;
        [SerializeField] private TMP_Text attackSpeedText;
        [SerializeField] private TMP_Text elapsedTimeText;

        #endregion

        #region Serialized Fields - Controls

        [Header("Dummy Control")]
        [SerializeField] private TMP_Text dummyCountText;

        [Header("Buttons")]
        [SerializeField] private Button startStopButton;
        [SerializeField] private TMP_Text startStopButtonText;

        #endregion

        #region Private Fields

        private DPSCalculator dpsCalculator;
        private bool isRunning = false;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (trainingManager != null)
            {
                dpsCalculator = trainingManager.GetDPSCalculator();
                if (dpsCalculator != null)
                {
                    dpsCalculator.OnStatsUpdated += UpdateRealtimeDisplay;
                }

                trainingManager.OnTrainingStarted += OnTrainingStarted;
                trainingManager.OnTrainingStopped += OnTrainingStopped;
                trainingManager.OnTrainingReset += OnTrainingReset;
            }
        }

        private void OnDisable()
        {
            if (dpsCalculator != null)
            {
                dpsCalculator.OnStatsUpdated -= UpdateRealtimeDisplay;
            }

            if (trainingManager != null)
            {
                trainingManager.OnTrainingStarted -= OnTrainingStarted;
                trainingManager.OnTrainingStopped -= OnTrainingStopped;
                trainingManager.OnTrainingReset -= OnTrainingReset;
            }
        }

        private void Start()
        {
            InitializeDropdowns();
            UpdateDummyCountDisplay();
            UpdateDamageInfoDisplay();
        }

        #endregion

        #region Initialization

        private void InitializeDropdowns()
        {
            // 등급 드롭다운 (1성~3성)
            if (gradeDropdown != null)
            {
                gradeDropdown.ClearOptions();
                gradeDropdown.AddOptions(new List<string> { "1성", "2성", "3성" });
                gradeDropdown.value = 0;
            }

            // 강화 드롭다운 (0~5단계)
            if (enhancementDropdown != null)
            {
                enhancementDropdown.ClearOptions();
                var enhancementOptions = new List<string>();
                for (int i = 0; i <= 5; i++)
                {
                    enhancementOptions.Add($"{i}단계");
                }
                enhancementDropdown.AddOptions(enhancementOptions);
                enhancementDropdown.value = 0;
            }

            // 캐릭터, 책갈피 드롭다운은 외부에서 설정
            Debug.Log("[TrainingUIController] 드롭다운 초기화 완료");
        }

        #endregion

        #region OnClick Methods (Inspector에서 연결)

        /// <summary>
        /// 허수아비 증가 버튼 OnClick
        /// </summary>
        public void OnDummyIncrease()
        {
            trainingManager?.IncreaseDummyCount();
            UpdateDummyCountDisplay();
        }

        /// <summary>
        /// 허수아비 감소 버튼 OnClick
        /// </summary>
        public void OnDummyDecrease()
        {
            trainingManager?.DecreaseDummyCount();
            UpdateDummyCountDisplay();
        }

        /// <summary>
        /// 시작/종료 버튼 OnClick
        /// </summary>
        public void OnStartStopButton()
        {
            if (trainingManager == null) return;

            if (isRunning)
            {
                trainingManager.StopTraining();
            }
            else
            {
                trainingManager.StartTraining();
            }
        }

        /// <summary>
        /// 리셋 버튼 OnClick
        /// </summary>
        public void OnResetButton()
        {
            trainingManager?.ResetTraining();
        }

        #endregion

        #region Dropdown OnValueChanged (Inspector에서 연결)

        /// <summary>
        /// 캐릭터 드롭다운 변경
        /// </summary>
        public void OnCharacterDropdownChanged(int index)
        {
            // TODO: 실제 캐릭터 ID 매핑
            trainingManager?.SetCharacter(index + 1);
            UpdateDamageInfoDisplay();
        }

        /// <summary>
        /// 등급 드롭다운 변경
        /// </summary>
        public void OnGradeDropdownChanged(int index)
        {
            trainingManager?.SetGrade(index + 1);
            UpdateDamageInfoDisplay();
        }

        /// <summary>
        /// 강화 드롭다운 변경
        /// </summary>
        public void OnEnhancementDropdownChanged(int index)
        {
            trainingManager?.SetEnhancement(index);
            UpdateDamageInfoDisplay();
        }

        /// <summary>
        /// 메인 스킬 책갈피 드롭다운 변경
        /// </summary>
        public void OnMainSkillBookmarkDropdownChanged(int index)
        {
            trainingManager?.SetMainSkillBookmark(index);
            UpdateDamageInfoDisplay();
        }

        /// <summary>
        /// 보조 스킬 책갈피 드롭다운 변경
        /// </summary>
        public void OnSupportSkillBookmarkDropdownChanged(int index)
        {
            trainingManager?.SetSupportSkillBookmark(index);
            UpdateDamageInfoDisplay();
        }

        /// <summary>
        /// 스탯 책갈피 드롭다운 변경
        /// </summary>
        public void OnStatBookmarkDropdownChanged(int index)
        {
            trainingManager?.SetStatBookmark(index);
            UpdateDamageInfoDisplay();
        }

        #endregion

        #region Event Handlers

        private void OnTrainingStarted()
        {
            isRunning = true;
            UpdateStartStopButtonText();
        }

        private void OnTrainingStopped()
        {
            isRunning = false;
            UpdateStartStopButtonText();
        }

        private void OnTrainingReset()
        {
            UpdateRealtimeDisplay(0f, 0f, 0f);
        }

        #endregion

        #region Display Updates

        private void UpdateStartStopButtonText()
        {
            if (startStopButtonText != null)
            {
                startStopButtonText.text = isRunning ? "종료" : "시작";
            }
        }

        private void UpdateDummyCountDisplay()
        {
            if (dummyCountText != null && trainingManager != null)
            {
                dummyCountText.text = trainingManager.DummyCount.ToString();
            }
        }

        /// <summary>
        /// 데미지 정보 패널 업데이트 (설정 변경 시)
        /// </summary>
        private void UpdateDamageInfoDisplay()
        {
            // TODO: TrainingManager에서 계산된 값 가져오기
            // 현재는 임시 값 표시

            if (baseDamageText != null) baseDamageText.text = "50";
            if (totalBonusText != null) totalBonusText.text = "+0%";
            if (finalDamageText != null) finalDamageText.text = "50";
            if (mainSkillNameText != null) mainSkillNameText.text = "-";
            if (attackSpeedPreviewText != null) attackSpeedPreviewText.text = "1.0";
            if (expectedDPSText != null) expectedDPSText.text = "50";
        }

        /// <summary>
        /// 실시간 측정 패널 업데이트 (DPSCalculator 이벤트)
        /// </summary>
        private void UpdateRealtimeDisplay(float dps, float totalDamage, float elapsedTime)
        {
            if (realtimeDPSText != null)
            {
                realtimeDPSText.text = FormatNumber(dps);
            }

            if (totalDamageText != null)
            {
                totalDamageText.text = FormatNumber(totalDamage);
            }

            if (attackSpeedText != null)
            {
                // TODO: 실제 공격속도 값
                attackSpeedText.text = "1.0";
            }

            if (elapsedTimeText != null)
            {
                elapsedTimeText.text = elapsedTime.ToString("F1");
            }
        }

        /// <summary>
        /// 숫자 포맷팅 (천, 만, 억 단위)
        /// </summary>
        private string FormatNumber(float value)
        {
            if (value >= 100000000f)
            {
                return $"{value / 100000000f:F1}억";
            }
            else if (value >= 10000f)
            {
                return $"{value / 10000f:F1}만";
            }
            else if (value >= 1000f)
            {
                return $"{value / 1000f:F1}천";
            }
            else
            {
                return $"{value:F0}";
            }
        }

        #endregion

        #region Public API (외부에서 드롭다운 옵션 설정)

        /// <summary>
        /// 캐릭터 드롭다운 옵션 설정
        /// </summary>
        public void SetCharacterOptions(List<string> characterNames)
        {
            if (characterDropdown != null)
            {
                characterDropdown.ClearOptions();
                characterDropdown.AddOptions(characterNames);
                if (characterNames.Count > 0)
                {
                    characterDropdown.value = 0;
                }
            }
        }

        /// <summary>
        /// 메인 스킬 책갈피 드롭다운 옵션 설정
        /// </summary>
        public void SetMainSkillBookmarkOptions(List<string> bookmarkNames)
        {
            if (mainSkillBookmarkDropdown != null)
            {
                mainSkillBookmarkDropdown.ClearOptions();
                mainSkillBookmarkDropdown.AddOptions(bookmarkNames);
                if (bookmarkNames.Count > 0)
                {
                    mainSkillBookmarkDropdown.value = 0;
                }
            }
        }

        /// <summary>
        /// 보조 스킬 책갈피 드롭다운 옵션 설정
        /// </summary>
        public void SetSupportSkillBookmarkOptions(List<string> bookmarkNames)
        {
            if (supportSkillBookmarkDropdown != null)
            {
                supportSkillBookmarkDropdown.ClearOptions();
                supportSkillBookmarkDropdown.AddOptions(bookmarkNames);
                if (bookmarkNames.Count > 0)
                {
                    supportSkillBookmarkDropdown.value = 0;
                }
            }
        }

        /// <summary>
        /// 스탯 책갈피 드롭다운 옵션 설정
        /// </summary>
        public void SetStatBookmarkOptions(List<string> bookmarkNames)
        {
            if (statBookmarkDropdown != null)
            {
                statBookmarkDropdown.ClearOptions();
                statBookmarkDropdown.AddOptions(bookmarkNames);
                if (bookmarkNames.Count > 0)
                {
                    statBookmarkDropdown.value = 0;
                }
            }
        }

        #endregion
    }
}
