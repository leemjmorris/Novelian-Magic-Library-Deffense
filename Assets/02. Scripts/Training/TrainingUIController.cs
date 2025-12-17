// 훈련소 UI 컨트롤러 (Issue #458)
// UGUI 기반 UI 제어
namespace Novelian.Training
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Novelian.Combat;

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

        #region Serialized Fields - Settings Panels

        [Header("Settings Panels (시작 시 숨김)")]
        [SerializeField] private GameObject characterSettingRail;
        [SerializeField] private GameObject bookMarkSettingRail;

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

        // 드롭다운 데이터 캐시
        private List<CharacterData> characterDataList = new List<CharacterData>();
        private List<MainSkillData> mainSkillDataList = new List<MainSkillData>();
        private List<BookmarkOptionData> statOptionDataList = new List<BookmarkOptionData>();

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

        private async void Start()
        {
            // CSVLoader 초기화 대기
            await WaitForCSVLoaderAsync();

            // 드롭다운 데이터 로드
            await LoadDropdownDataAsync();

            // 드롭다운 UI 초기화
            InitializeDropdowns();
            UpdateDummyCountDisplay();
            UpdateDamageInfoDisplay();
        }

        /// <summary>
        /// CSVLoader 초기화 대기
        /// </summary>
        private async UniTask WaitForCSVLoaderAsync()
        {
            while (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                await UniTask.Delay(100);
            }
        }

        /// <summary>
        /// 드롭다운 데이터 로드
        /// </summary>
        private async UniTask LoadDropdownDataAsync()
        {
            // 캐릭터 데이터 로드
            var characterTable = CSVLoader.Instance?.GetTable<CharacterData>();
            if (characterTable != null)
            {
                characterDataList.Clear();
                var allCharacters = characterTable.GetAll();
                for (int i = 0; i < allCharacters.Count; i++)
                {
                    characterDataList.Add(allCharacters[i]);
                }
            }

            // 메인 스킬 데이터 로드 (스킬 책갈피용)
            var mainSkillTable = CSVLoader.Instance?.GetTable<MainSkillData>();
            if (mainSkillTable != null)
            {
                mainSkillDataList.Clear();
                var allSkills = mainSkillTable.GetAll();
                for (int i = 0; i < allSkills.Count; i++)
                {
                    mainSkillDataList.Add(allSkills[i]);
                }
            }

            // 스탯 옵션 데이터 로드
            var optionTable = CSVLoader.Instance?.GetTable<BookmarkOptionData>();
            if (optionTable != null)
            {
                statOptionDataList.Clear();
                var allOptions = optionTable.GetAll();
                for (int i = 0; i < allOptions.Count; i++)
                {
                    statOptionDataList.Add(allOptions[i]);
                }
            }

            await UniTask.CompletedTask;
            Debug.Log($"[TrainingUIController] 드롭다운 데이터 로드 완료: 캐릭터={characterDataList.Count}, 스킬={mainSkillDataList.Count}, 스탯옵션={statOptionDataList.Count}");
        }

        #endregion

        #region Initialization

        private void InitializeDropdowns()
        {
            // 캐릭터 드롭다운
            if (characterDropdown != null)
            {
                characterDropdown.ClearOptions();
                var characterOptions = new List<string>();
                for (int i = 0; i < characterDataList.Count; i++)
                {
                    // Character_Name_ID로 StringTable에서 이름 조회 (간략화: ID 사용)
                    string name = GetCharacterDisplayName(characterDataList[i]);
                    characterOptions.Add(name);
                }
                characterDropdown.AddOptions(characterOptions);
                if (characterOptions.Count > 0)
                {
                    characterDropdown.value = 0;
                    // 첫 번째 캐릭터 선택
                    trainingManager?.SetCharacter(characterDataList[0].Character_ID);
                }
            }

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

            // 메인 스킬 책갈피 드롭다운
            if (mainSkillBookmarkDropdown != null)
            {
                mainSkillBookmarkDropdown.ClearOptions();
                var skillOptions = new List<string> { "없음" };
                for (int i = 0; i < mainSkillDataList.Count; i++)
                {
                    if (!string.IsNullOrEmpty(mainSkillDataList[i].skill_name))
                    {
                        skillOptions.Add(mainSkillDataList[i].skill_name);
                    }
                }
                mainSkillBookmarkDropdown.AddOptions(skillOptions);
                mainSkillBookmarkDropdown.value = 0;
            }

            // 보조 스킬 책갈피 드롭다운 (현재는 "없음"만)
            if (supportSkillBookmarkDropdown != null)
            {
                supportSkillBookmarkDropdown.ClearOptions();
                supportSkillBookmarkDropdown.AddOptions(new List<string> { "없음" });
                supportSkillBookmarkDropdown.value = 0;
            }

            // 스탯 책갈피 드롭다운
            if (statBookmarkDropdown != null)
            {
                statBookmarkDropdown.ClearOptions();
                var statOptions = new List<string> { "없음" };
                for (int i = 0; i < statOptionDataList.Count; i++)
                {
                    string optionName = GetOptionDisplayName(statOptionDataList[i]);
                    statOptions.Add(optionName);
                }
                statBookmarkDropdown.AddOptions(statOptions);
                statBookmarkDropdown.value = 0;
            }

            Debug.Log("[TrainingUIController] 드롭다운 초기화 완료");
        }

        /// <summary>
        /// 캐릭터 표시 이름 생성
        /// </summary>
        private string GetCharacterDisplayName(CharacterData data)
        {
            // StringTable에서 이름 조회 시도
            var stringData = CSVLoader.Instance?.GetData<StringTable>(data.Character_Name_ID);
            if (stringData != null && !string.IsNullOrEmpty(stringData.Text))
            {
                return stringData.Text;
            }
            // 없으면 ID 표시
            return $"캐릭터 {data.Character_ID}";
        }

        /// <summary>
        /// 스탯 옵션 표시 이름 생성
        /// </summary>
        private string GetOptionDisplayName(BookmarkOptionData data)
        {
            string typeName = data.Option_Type switch
            {
                OptionType.AttackPower => "공격력",
                OptionType.AttackSpeed => "공격속도",
                OptionType.CritChance => "치명타 확률",
                OptionType.CritMultiplier => "치명타 배율",
                OptionType.CritDamage => "치명타 피해",
                OptionType.BossDamage => "보스 피해",
                OptionType.CooldownReduction => "쿨타임 감소",
                _ => $"옵션{(int)data.Option_Type}"
            };
            return $"{typeName} +{data.Option_Value * 100f:F0}%";
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
            if (index >= 0 && index < characterDataList.Count)
            {
                trainingManager?.SetCharacter(characterDataList[index].Character_ID);
                UpdateDamageInfoDisplay();
            }
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
            // index 0 = "없음"
            if (index <= 0)
            {
                trainingManager?.SetMainSkillBookmark(0);
            }
            else
            {
                // 실제 스킬 인덱스는 index - 1 (없음 제외)
                int skillIndex = index - 1;
                if (skillIndex >= 0 && skillIndex < mainSkillDataList.Count)
                {
                    trainingManager?.SetMainSkillBookmark(mainSkillDataList[skillIndex].skill_id);
                }
            }
            UpdateDamageInfoDisplay();
        }

        /// <summary>
        /// 보조 스킬 책갈피 드롭다운 변경
        /// </summary>
        public void OnSupportSkillBookmarkDropdownChanged(int index)
        {
            // 현재는 "없음"만 있음
            trainingManager?.SetSupportSkillBookmark(0);
            UpdateDamageInfoDisplay();
        }

        /// <summary>
        /// 스탯 책갈피 드롭다운 변경
        /// </summary>
        public void OnStatBookmarkDropdownChanged(int index)
        {
            // index 0 = "없음"
            if (index <= 0)
            {
                trainingManager?.SetStatBookmark(0);
            }
            else
            {
                int optionIndex = index - 1;
                if (optionIndex >= 0 && optionIndex < statOptionDataList.Count)
                {
                    trainingManager?.SetStatBookmark(statOptionDataList[optionIndex].Option_ID);
                }
            }
            UpdateDamageInfoDisplay();
        }

        #endregion

        #region Event Handlers

        private void OnTrainingStarted()
        {
            isRunning = true;
            UpdateStartStopButtonText();

            // 설정 패널 숨김
            if (characterSettingRail != null) characterSettingRail.SetActive(false);
            if (bookMarkSettingRail != null) bookMarkSettingRail.SetActive(false);
        }

        private void OnTrainingStopped()
        {
            isRunning = false;
            UpdateStartStopButtonText();

            // 설정 패널 표시
            if (characterSettingRail != null) characterSettingRail.SetActive(true);
            if (bookMarkSettingRail != null) bookMarkSettingRail.SetActive(true);
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
            if (trainingManager == null) return;

            // 현재 선택된 캐릭터 데이터 조회
            var characterData = CSVLoader.Instance?.GetData<CharacterData>(trainingManager.SelectedCharacterId);
            if (characterData == null)
            {
                SetDefaultDamageInfo();
                return;
            }

            // 기본 스킬 데이터 조회
            var skillData = CSVLoader.Instance?.GetData<MainSkillData>(characterData.Base_Skill_ID);
            if (skillData == null)
            {
                SetDefaultDamageInfo();
                return;
            }

            // 기본 데미지
            float baseDamage = skillData.base_damage;

            // 성급 보너스 계산 (1성=1.0, 2성=1.2, 3성=1.5)
            float gradeMultiplier = trainingManager.SelectedGrade switch
            {
                1 => 1.0f,
                2 => 1.2f,
                3 => 1.5f,
                _ => 1.0f
            };

            // 강화 보너스 계산 (임시: 강화 레벨당 5%)
            float enhancementBonus = trainingManager.SelectedEnhancement * 0.05f;

            // 총 보너스
            float totalBonus = (gradeMultiplier - 1.0f) + enhancementBonus;

            // 최종 데미지
            float finalDamage = baseDamage * (1f + totalBonus);

            // 공격 속도
            float attackSpeed = skillData.cooldown > 0 ? 1f / skillData.cooldown : 1f;

            // 예상 DPS
            float expectedDPS = finalDamage * attackSpeed;

            // UI 업데이트
            if (baseDamageText != null) baseDamageText.text = FormatNumber(baseDamage);
            if (totalBonusText != null) totalBonusText.text = $"+{totalBonus * 100f:F0}%";
            if (finalDamageText != null) finalDamageText.text = FormatNumber(finalDamage);
            if (mainSkillNameText != null) mainSkillNameText.text = skillData.skill_name ?? "-";
            if (attackSpeedPreviewText != null) attackSpeedPreviewText.text = $"{attackSpeed:F2}";
            if (expectedDPSText != null) expectedDPSText.text = FormatNumber(expectedDPS);
        }

        /// <summary>
        /// 기본 데미지 정보 설정
        /// </summary>
        private void SetDefaultDamageInfo()
        {
            if (baseDamageText != null) baseDamageText.text = "-";
            if (totalBonusText != null) totalBonusText.text = "+0%";
            if (finalDamageText != null) finalDamageText.text = "-";
            if (mainSkillNameText != null) mainSkillNameText.text = "-";
            if (attackSpeedPreviewText != null) attackSpeedPreviewText.text = "-";
            if (expectedDPSText != null) expectedDPSText.text = "-";
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
