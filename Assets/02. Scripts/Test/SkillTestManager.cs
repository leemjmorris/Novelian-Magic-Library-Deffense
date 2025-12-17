// SkillTestManager.cs - 스킬 테스트 씬 전용 매니저 (Issue #351)
// 기존 매니저들의 핵심 기능만 재사용하고, 테스트 전용 로직은 별도 관리
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Managers;
using Novelian.Combat;

namespace NovelianMagicLibraryDefense.Test
{
    /// <summary>
    /// 스킬 테스트 씬 매니저
    /// - 몬스터 관리 (추가/제거/리셋, 무적 토글)
    /// - 스킬 선택 (메인/서포트)
    /// - 패턴 배치 (일렬/원형/무작위)
    /// - 수치 표시
    /// - 수동 발사
    /// </summary>
    public class SkillTestManager : MonoBehaviour
    {
        #region Inspector References

        [Header("UI - 게임 뷰")]
        [SerializeField] private Button invincibleButton;
        [SerializeField] private TMP_Text invincibleButtonText;
        [SerializeField] private TMP_Text monsterCountText;

        [Header("UI - 스킬 선택")]
        [SerializeField] private TMP_Dropdown mainSkillDropdown;
        [SerializeField] private TMP_Dropdown supportSkillDropdown;

        [Header("UI - 몬스터 컨트롤")]
        [SerializeField] private Button addMonsterButton;
        [SerializeField] private Button removeMonsterButton;
        [SerializeField] private Button resetButton;

        [Header("UI - 패턴 선택")]
        [SerializeField] private Button linePatternButton;
        [SerializeField] private Button circlePatternButton;
        [SerializeField] private Button randomPatternButton;

        [Header("UI - 수치 표시")]
        [SerializeField] private TMP_Text statsText;

        [Header("UI - 발사")]
        [SerializeField] private Button fireButton;

        [Header("씬 참조")]
        [SerializeField] private Transform monsterSpawnArea;
        [SerializeField] private Transform characterSpawnPoint;
        [SerializeField] private GameObject monsterPrefab;
        [SerializeField] private GameObject characterPrefab;

        [Header("설정")]
        [SerializeField] private int maxMonsters = 8;
        [SerializeField] private float monsterSpacing = 2f;

        #endregion

        #region Private Fields

        // 상태
        private bool isInvincible = true;
        private int currentMonsterCount = 3;
        private MonsterPattern currentPattern = MonsterPattern.Line;

        // 스킬 데이터
        private int selectedMainSkillId = 0;
        private int selectedSupportSkillId = 0;
        private MainSkillData[] mainSkillList;
        private SupportSkillData[] supportSkillList;
        private SupportCompatibilityData[] compatibilityList;
        private SupportSkillData[] filteredSupportSkillList; // 현재 메인 스킬과 호환되는 서포트 스킬

        // 런타임 오브젝트
        private Monster[] spawnedMonsters;
        private Character spawnedCharacter;

        #endregion

        #region Enums

        public enum MonsterPattern
        {
            Line,
            Circle,
            Random
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            // CSV 로드 대기
            await UniTask.WaitUntil(() => CSVLoader.Instance != null && CSVLoader.Instance.IsInit);

            // 스킬 데이터 로드
            LoadSkillData();

            // UI 초기화
            SetupUI();

            // 초기 상태 설정
            UpdateInvincibleUI();
            UpdateMonsterCountUI();
            UpdateStatsUI();

            // 초기 몬스터 스폰
            SpawnMonsters();

            // 캐릭터 스폰
            SpawnCharacter();

            Debug.Log("[SkillTestManager] 초기화 완료");
        }

        #endregion

        #region Skill Data Loading

        private void LoadSkillData()
        {
            // MainSkillData 로드
            var mainSkillTable = CSVLoader.Instance.GetTable<MainSkillData>();
            if (mainSkillTable != null)
            {
                var dataList = mainSkillTable.GetAll();
                if (dataList != null)
                {
                    mainSkillList = dataList.ToArray();
                    Debug.Log($"[SkillTestManager] MainSkillData {mainSkillList.Length}개 로드");
                }
            }

            // SupportSkillData 로드
            var supportSkillTable = CSVLoader.Instance.GetTable<SupportSkillData>();
            if (supportSkillTable != null)
            {
                var dataList = supportSkillTable.GetAll();
                if (dataList != null)
                {
                    supportSkillList = dataList.ToArray();
                    Debug.Log($"[SkillTestManager] SupportSkillData {supportSkillList.Length}개 로드");
                }
            }

            // SupportCompatibilityData 로드
            var compatibilityTable = CSVLoader.Instance.GetTable<SupportCompatibilityData>();
            if (compatibilityTable != null)
            {
                var dataList = compatibilityTable.GetAll();
                if (dataList != null)
                {
                    compatibilityList = dataList.ToArray();
                    Debug.Log($"[SkillTestManager] SupportCompatibilityData {compatibilityList.Length}개 로드");
                }
            }
        }

        #endregion

        #region UI Setup

        private void SetupUI()
        {
            // 무적 버튼
            if (invincibleButton != null)
                invincibleButton.onClick.AddListener(ToggleInvincible);

            // 몬스터 컨트롤 버튼
            if (addMonsterButton != null)
                addMonsterButton.onClick.AddListener(AddMonster);
            if (removeMonsterButton != null)
                removeMonsterButton.onClick.AddListener(RemoveMonster);
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetMonsters);

            // 패턴 버튼
            if (linePatternButton != null)
                linePatternButton.onClick.AddListener(() => SetPattern(MonsterPattern.Line));
            if (circlePatternButton != null)
                circlePatternButton.onClick.AddListener(() => SetPattern(MonsterPattern.Circle));
            if (randomPatternButton != null)
                randomPatternButton.onClick.AddListener(() => SetPattern(MonsterPattern.Random));

            // 발사 버튼
            if (fireButton != null)
                fireButton.onClick.AddListener(FireSkill);

            // 드롭다운 설정
            SetupMainSkillDropdown();
            SetupSupportSkillDropdown();
        }

        private void SetupMainSkillDropdown()
        {
            if (mainSkillDropdown == null || mainSkillList == null) return;

            mainSkillDropdown.ClearOptions();

            var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            for (int i = 0; i < mainSkillList.Length; i++)
            {
                var skill = mainSkillList[i];
                options.Add(new TMP_Dropdown.OptionData($"{skill.skill_name} (ID:{skill.skill_id})"));
            }
            mainSkillDropdown.AddOptions(options);

            mainSkillDropdown.onValueChanged.AddListener(OnMainSkillChanged);

            // 첫 번째 스킬 선택
            if (mainSkillList.Length > 0)
            {
                selectedMainSkillId = mainSkillList[0].skill_id;
                mainSkillDropdown.value = 0;
            }
        }

        private void SetupSupportSkillDropdown()
        {
            if (supportSkillDropdown == null) return;

            supportSkillDropdown.onValueChanged.AddListener(OnSupportSkillChanged);

            // 초기 필터링 (첫 번째 메인 스킬 기준)
            RefreshSupportSkillDropdown();
        }

        /// <summary>
        /// 현재 선택된 메인 스킬 타입에 따라 호환되는 서포트 스킬만 드롭다운에 표시
        /// </summary>
        private void RefreshSupportSkillDropdown()
        {
            if (supportSkillDropdown == null || supportSkillList == null || compatibilityList == null) return;

            // 현재 선택된 메인 스킬의 타입 가져오기
            MainSkillData currentMainSkill = null;
            if (mainSkillList != null)
            {
                for (int i = 0; i < mainSkillList.Length; i++)
                {
                    if (mainSkillList[i].skill_id == selectedMainSkillId)
                    {
                        currentMainSkill = mainSkillList[i];
                        break;
                    }
                }
            }

            SkillAssetType skillType = currentMainSkill?.GetSkillType() ?? SkillAssetType.Projectile;

            // 호환되는 서포트 스킬 필터링
            var compatibleList = new System.Collections.Generic.List<SupportSkillData>();
            for (int i = 0; i < supportSkillList.Length; i++)
            {
                var support = supportSkillList[i];

                // 호환성 테이블에서 해당 서포트 스킬 찾기
                SupportCompatibilityData compatibility = null;
                for (int j = 0; j < compatibilityList.Length; j++)
                {
                    if (compatibilityList[j].support_id == support.support_id)
                    {
                        compatibility = compatibilityList[j];
                        break;
                    }
                }

                // 호환성 체크 (테이블에 없으면 기본 허용)
                if (compatibility == null || compatibility.IsCompatibleWith(skillType))
                {
                    compatibleList.Add(support);
                }
            }

            filteredSupportSkillList = compatibleList.ToArray();

            // 드롭다운 갱신
            supportSkillDropdown.ClearOptions();

            var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            options.Add(new TMP_Dropdown.OptionData("없음"));

            for (int i = 0; i < filteredSupportSkillList.Length; i++)
            {
                var skill = filteredSupportSkillList[i];
                options.Add(new TMP_Dropdown.OptionData($"{skill.support_name} (ID:{skill.support_id})"));
            }
            supportSkillDropdown.AddOptions(options);

            // 서포트 스킬 초기화
            selectedSupportSkillId = 0;
            supportSkillDropdown.value = 0;

            Debug.Log($"[SkillTestManager] 서포트 드롭다운 갱신: {filteredSupportSkillList.Length}개 호환 (스킬타입: {skillType})");
        }

        #endregion

        #region UI Event Handlers

        private void OnMainSkillChanged(int index)
        {
            if (mainSkillList == null || index < 0 || index >= mainSkillList.Length) return;

            selectedMainSkillId = mainSkillList[index].skill_id;

            // 메인 스킬 변경 시 서포트 드롭다운 갱신 (호환되는 서포트만 표시)
            RefreshSupportSkillDropdown();

            UpdateStatsUI();
            UpdateCharacterSkill();

            Debug.Log($"[SkillTestManager] 메인 스킬 변경: {mainSkillList[index].skill_name}");
        }

        private void OnSupportSkillChanged(int index)
        {
            if (index == 0)
            {
                selectedSupportSkillId = 0;
            }
            else if (filteredSupportSkillList != null && index - 1 < filteredSupportSkillList.Length)
            {
                selectedSupportSkillId = filteredSupportSkillList[index - 1].support_id;
            }

            UpdateStatsUI();
            UpdateCharacterSkill();

            Debug.Log($"[SkillTestManager] 서포트 스킬 변경: {(selectedSupportSkillId == 0 ? "없음" : selectedSupportSkillId.ToString())}");
        }

        #endregion

        #region Monster Management

        public void AddMonster()
        {
            if (currentMonsterCount >= maxMonsters) return;

            currentMonsterCount++;
            UpdateMonsterCountUI();
            SpawnMonsters();

            Debug.Log($"[SkillTestManager] 몬스터 추가: {currentMonsterCount}");
        }

        public void RemoveMonster()
        {
            if (currentMonsterCount <= 1) return;

            currentMonsterCount--;
            UpdateMonsterCountUI();
            SpawnMonsters();

            Debug.Log($"[SkillTestManager] 몬스터 제거: {currentMonsterCount}");
        }

        public void ResetMonsters()
        {
            currentMonsterCount = 1;
            UpdateMonsterCountUI();
            SpawnMonsters();

            Debug.Log("[SkillTestManager] 몬스터 리셋");
        }

        public void ToggleInvincible()
        {
            isInvincible = !isInvincible;
            UpdateInvincibleUI();
            ApplyInvincibleToMonsters();

            Debug.Log($"[SkillTestManager] 무적 토글: {isInvincible}");
        }

        private void SpawnMonsters()
        {
            // 기존 몬스터 제거
            ClearMonsters();

            if (monsterPrefab == null || monsterSpawnArea == null)
            {
                Debug.LogWarning("[SkillTestManager] monsterPrefab 또는 monsterSpawnArea가 설정되지 않음");
                return;
            }

            spawnedMonsters = new Monster[currentMonsterCount];

            for (int i = 0; i < currentMonsterCount; i++)
            {
                Vector3 spawnPos = GetMonsterPosition(i, currentMonsterCount);
                GameObject monsterObj = Instantiate(monsterPrefab, spawnPos, Quaternion.identity, monsterSpawnArea);
                Monster monster = monsterObj.GetComponent<Monster>();

                if (monster != null)
                {
                    spawnedMonsters[i] = monster;

                    // OnSpawn 호출 (TargetRegistry 등록 + 초기화)
                    monster.OnSpawn();

                    // 무적 설정
                    if (isInvincible)
                    {
                        monster.SetInvincible(true);
                    }
                }
            }

            Debug.Log($"[SkillTestManager] 몬스터 {currentMonsterCount}마리 스폰 완료");
        }

        private void ClearMonsters()
        {
            if (spawnedMonsters == null) return;

            for (int i = 0; i < spawnedMonsters.Length; i++)
            {
                if (spawnedMonsters[i] != null)
                {
                    Destroy(spawnedMonsters[i].gameObject);
                }
            }
            spawnedMonsters = null;
        }

        private void ApplyInvincibleToMonsters()
        {
            if (spawnedMonsters == null) return;

            for (int i = 0; i < spawnedMonsters.Length; i++)
            {
                if (spawnedMonsters[i] != null)
                {
                    spawnedMonsters[i].SetInvincible(isInvincible);
                }
            }
        }

        private Vector3 GetMonsterPosition(int index, int total)
        {
            Vector3 basePos = monsterSpawnArea != null ? monsterSpawnArea.position : Vector3.zero;

            switch (currentPattern)
            {
                case MonsterPattern.Line:
                    float lineOffset = (index - (total - 1) / 2f) * monsterSpacing;
                    return basePos + new Vector3(lineOffset, 0, 0);

                case MonsterPattern.Circle:
                    float angle = (360f / total) * index * Mathf.Deg2Rad;
                    float radius = monsterSpacing * total / (2f * Mathf.PI);
                    radius = Mathf.Max(radius, 2f);
                    return basePos + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

                case MonsterPattern.Random:
                    float randomX = Random.Range(-monsterSpacing * 2, monsterSpacing * 2);
                    float randomZ = Random.Range(-monsterSpacing, monsterSpacing);
                    return basePos + new Vector3(randomX, 0, randomZ);

                default:
                    return basePos;
            }
        }

        #endregion

        #region Pattern Management

        public void SetPattern(MonsterPattern pattern)
        {
            currentPattern = pattern;
            UpdatePatternButtonsUI();
            SpawnMonsters();

            Debug.Log($"[SkillTestManager] 패턴 변경: {pattern}");
        }

        private void UpdatePatternButtonsUI()
        {
            // 패턴 버튼 하이라이트 (선택된 패턴만 강조)
            SetButtonHighlight(linePatternButton, currentPattern == MonsterPattern.Line);
            SetButtonHighlight(circlePatternButton, currentPattern == MonsterPattern.Circle);
            SetButtonHighlight(randomPatternButton, currentPattern == MonsterPattern.Random);
        }

        private void SetButtonHighlight(Button button, bool isSelected)
        {
            if (button == null) return;

            var colors = button.colors;
            colors.normalColor = isSelected ? new Color(0.9f, 0.3f, 0.4f) : new Color(0.2f, 0.2f, 0.2f);
            button.colors = colors;
        }

        #endregion

        #region Character Management

        private void SpawnCharacter()
        {
            if (characterPrefab == null || characterSpawnPoint == null)
            {
                Debug.LogWarning("[SkillTestManager] characterPrefab 또는 characterSpawnPoint가 설정되지 않음");
                return;
            }

            GameObject charObj = Instantiate(characterPrefab, characterSpawnPoint.position, Quaternion.identity);
            spawnedCharacter = charObj.GetComponent<Character>();

            if (spawnedCharacter != null)
            {
                // 테스트 씬에서는 자동 공격 비활성화 (발사 버튼으로만 공격)
                spawnedCharacter.SetAutoAttackEnabled(false);

                if (selectedMainSkillId > 0)
                {
                    spawnedCharacter.SetSkillIds(selectedMainSkillId, selectedMainSkillId, selectedSupportSkillId);
                }
            }

            Debug.Log("[SkillTestManager] 캐릭터 스폰 완료 (자동 공격 비활성화)");
        }

        private void UpdateCharacterSkill()
        {
            if (spawnedCharacter == null) return;

            spawnedCharacter.SetSkillIds(selectedMainSkillId, selectedMainSkillId, selectedSupportSkillId);
            Debug.Log($"[SkillTestManager] 캐릭터 스킬 업데이트: Main={selectedMainSkillId}, Support={selectedSupportSkillId}");
        }

        #endregion

        #region Fire Skill

        public void FireSkill()
        {
            if (spawnedCharacter == null)
            {
                Debug.LogWarning("[SkillTestManager] 캐릭터가 없어서 발사 불가");
                return;
            }

            spawnedCharacter.ForceAttack();
            Debug.Log("[SkillTestManager] 발사!");
        }

        #endregion

        #region UI Update

        private void UpdateInvincibleUI()
        {
            if (invincibleButtonText != null)
            {
                invincibleButtonText.text = isInvincible ? "무적 ON" : "무적 OFF";
            }

            if (invincibleButton != null)
            {
                var colors = invincibleButton.colors;
                colors.normalColor = isInvincible ? new Color(0, 0.82f, 0.42f) : new Color(1f, 0.28f, 0.34f);
                invincibleButton.colors = colors;
            }
        }

        private void UpdateMonsterCountUI()
        {
            if (monsterCountText != null)
            {
                monsterCountText.text = $"몬스터: {currentMonsterCount}";
            }
        }

        private void UpdateStatsUI()
        {
            // 메인 스킬 데이터 가져오기
            MainSkillData mainSkill = null;
            if (mainSkillList != null)
            {
                for (int i = 0; i < mainSkillList.Length; i++)
                {
                    if (mainSkillList[i].skill_id == selectedMainSkillId)
                    {
                        mainSkill = mainSkillList[i];
                        break;
                    }
                }
            }

            // 서포트 스킬 데이터 가져오기
            SupportSkillData supportSkill = null;
            if (supportSkillList != null && selectedSupportSkillId > 0)
            {
                for (int i = 0; i < supportSkillList.Length; i++)
                {
                    if (supportSkillList[i].support_id == selectedSupportSkillId)
                    {
                        supportSkill = supportSkillList[i];
                        break;
                    }
                }
            }

            // 데미지 계산 (서포트 배율 적용)
            float baseDamage = mainSkill != null ? mainSkill.base_damage : 0;
            float damageMult = supportSkill != null ? supportSkill.damage_mult : 1f;
            float finalDamage = baseDamage * damageMult;

            // 쿨다운 계산 (서포트 배율 적용)
            float baseCooldown = mainSkill != null ? mainSkill.cooldown : 1f;
            float cooldownMult = supportSkill != null ? supportSkill.cooldown_mult : 1f;
            float cooldown = baseCooldown * cooldownMult;
            cooldown = Mathf.Max(cooldown, 0.1f); // 최소 쿨다운 보장

            // 공격 속도 계산 (서포트 배율 적용)
            float baseAttackSpeed = 1f / cooldown;
            float attackSpeedMult = supportSkill != null ? supportSkill.attack_speed_mult : 1f;
            float finalAttackSpeed = baseAttackSpeed * attackSpeedMult;

            // CC 정보
            string ccInfo = "없음";
            if (supportSkill != null)
            {
                var effectType = supportSkill.GetStatusEffectType();
                if (effectType == StatusEffectType.CC)
                {
                    var ccType = supportSkill.GetCCType();
                    ccInfo = $"{ccType} {supportSkill.cc_slow_amount}% / {supportSkill.cc_duration}초";
                }
                else if (effectType == StatusEffectType.DOT)
                {
                    var dotType = supportSkill.GetDOTType();
                    ccInfo = $"{dotType} {supportSkill.dot_damage_per_tick}/틱 / {supportSkill.dot_duration}초";
                }
                else if (effectType == StatusEffectType.Mark)
                {
                    var markType = supportSkill.GetMarkType();
                    ccInfo = $"{markType} {supportSkill.mark_damage_mult}x / {supportSkill.mark_duration}초";
                }
            }

            // UI 업데이트 (하나의 TMP_Text에 줄바꿈으로 표시)
            if (statsText != null)
            {
                string damageChange = damageMult != 1f ? $" ({(damageMult - 1f) * 100:+0;-0}%)" : "";
                string attackSpeedChange = attackSpeedMult != 1f ? $" ({(attackSpeedMult - 1f) * 100:+0;-0}%)" : "";
                statsText.text = $"데미지: {finalDamage:F0}{damageChange}\n공격속도: {finalAttackSpeed:F2}/s{attackSpeedChange}\n쿨다운: {cooldown:F2}초\nCC: {ccInfo}";
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            ClearMonsters();

            if (spawnedCharacter != null)
            {
                Destroy(spawnedCharacter.gameObject);
            }
        }

        #endregion
    }
}
