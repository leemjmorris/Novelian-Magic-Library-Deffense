// 훈련소 매니저 (Issue #458)
// 훈련소 전체 로직 제어
namespace Novelian.Training
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using Novelian.Combat;
    using NovelianMagicLibraryDefense.Managers;
    using System;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// 훈련소 전체 제어
    /// - 캐릭터 스폰/설정
    /// - 허수아비 스폰/관리
    /// - 시작/정지/리셋 제어
    /// </summary>
    public class TrainingManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Spawn Points")]
        [SerializeField, Tooltip("캐릭터 스폰 위치")]
        private Transform characterSpawnPoint;

        [SerializeField, Tooltip("허수아비 스폰 위치들")]
        private Transform[] dummySpawnPoints;

        [Header("Prefabs")]
        [SerializeField, Tooltip("허수아비 프리팹")]
        private GameObject dummyPrefab;

        [SerializeField, Tooltip("투사체 템플릿 (TrainingScene용)")]
        private GameObject projectileTemplate;

        [Header("References")]
        [SerializeField]
        private DPSCalculator dpsCalculator;

        #endregion

        #region Private Fields

        // 상태
        private bool isRunning = false;
        private Character currentCharacter;
        private List<Character> deckCharacters = new List<Character>();  // 내 덱 모드용
        private List<DummyTarget> activeDummies = new List<DummyTarget>();

        // 설정 값
        private int selectedCharacterId = 0;
        private int selectedGrade = 1;
        private int selectedEnhancement = 0;
        private int selectedMainSkillBookmarkId = 0;
        private int selectedSupportSkillId = 0;  // 보조 스킬 (책갈피 아님)
        private int[] selectedStatBookmarkIds = new int[4];  // 스탯 책갈피 4개 (중복 가능)
        private int dummyCount = 1;

        // 내 덱 사용 모드
        private bool useMyDeck = false;

        // 캐릭터 데이터 캐시
        private List<CharacterData> characterDataList = new List<CharacterData>();

        // Addressables로 로드된 캐릭터 프리팹 캐시 (prefabKey → GameObject)
        private Dictionary<string, GameObject> loadedCharacterPrefabs = new Dictionary<string, GameObject>();

        #endregion

        #region Events

        public event System.Action OnTrainingStarted;
        public event System.Action OnTrainingStopped;
        public event System.Action OnTrainingReset;

        #endregion

        #region Properties

        public bool IsRunning => isRunning;
        public int DummyCount => dummyCount;
        public Character CurrentCharacter => currentCharacter;
        public List<CharacterData> CharacterDataList => characterDataList;
        public int SelectedCharacterId => selectedCharacterId;
        public int SelectedGrade => selectedGrade;
        public int SelectedEnhancement => selectedEnhancement;
        public bool UseMyDeck => useMyDeck;
        public int[] SelectedStatBookmarkIds => selectedStatBookmarkIds;

        #endregion

        #region Lifecycle

        private async void Start()
        {
            // DPSCalculator 자동 찾기 (Inspector에서 연결 안 된 경우)
            if (dpsCalculator == null)
            {
                dpsCalculator = GetComponent<DPSCalculator>();
                if (dpsCalculator == null)
                {
                    Debug.LogWarning("[TrainingManager] DPSCalculator가 없습니다. Inspector에서 연결하거나 같은 오브젝트에 추가하세요.");
                }
            }

            // CSVLoader 초기화 대기
            await WaitForCSVLoaderAsync();

            // 캐릭터 프리팹 미리 로드 (Addressables)
            await PreloadCharacterPrefabs();

            // 캐릭터 데이터 캐시
            LoadCharacterDataCache();
        }

        private void OnDestroy()
        {
            // 정리
            if (currentCharacter != null)
            {
                Destroy(currentCharacter.gameObject);
            }
            DespawnDeckCharacters();
            DespawnDummies();
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
        /// 모든 캐릭터 프리팹을 미리 로드
        /// Editor: AssetDatabase 사용 (Addressables 빌드 불필요)
        /// Runtime: Addressables 사용
        /// CharacterTable → PathTable → "Prefab_" + Addressable_Key
        /// </summary>
        private async UniTask PreloadCharacterPrefabs()
        {
            Debug.Log("[TrainingManager] 캐릭터 프리팹 로드 시작...");

            var characterTable = CSVLoader.Instance?.GetTable<CharacterData>();
            if (characterTable == null || characterTable.Count == 0)
            {
                Debug.LogError("[TrainingManager] CharacterTable 로드 실패");
                return;
            }

            var allCharacters = characterTable.GetAll();
            int loadedCount = 0;
            int failedCount = 0;

            for (int i = 0; i < allCharacters.Count; i++)
            {
                var characterData = allCharacters[i];

                // PathTable에서 Addressable_Key 조회
                var pathData = CSVLoader.Instance?.GetData<PathData>(characterData.Path_ID);
                if (pathData == null)
                {
                    Debug.LogWarning($"[TrainingManager] PathData를 찾을 수 없음: Path_ID={characterData.Path_ID}");
                    failedCount++;
                    continue;
                }

                // "Prefab_" + Addressable_Key 로 프리팹 키 생성
                string prefabKey = $"Prefab_{pathData.Addressable_Key}";

                // 이미 로드된 프리팹은 스킵
                if (loadedCharacterPrefabs.ContainsKey(prefabKey))
                {
                    continue;
                }

                GameObject prefab = null;

#if UNITY_EDITOR
                // Editor 모드: AssetDatabase로 직접 로드 (Addressables 빌드 불필요)
                prefab = LoadPrefabFromAssetDatabase(pathData.Addressable_Key);
                if (prefab != null)
                {
                    loadedCharacterPrefabs[prefabKey] = prefab;
                    loadedCount++;
                    Debug.Log($"[TrainingManager] 프리팹 로드 완료 (AssetDatabase): {prefabKey}");
                }
                else
                {
                    Debug.LogError($"[TrainingManager] 프리팹 로드 실패 (AssetDatabase) '{prefabKey}'");
                    failedCount++;
                }
#else
                // Runtime: Addressables 사용
                try
                {
                    prefab = await Addressables.LoadAssetAsync<GameObject>(prefabKey).Task;
                    loadedCharacterPrefabs[prefabKey] = prefab;
                    loadedCount++;
                    Debug.Log($"[TrainingManager] 프리팹 로드 완료 (Addressables): {prefabKey}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TrainingManager] 프리팹 로드 실패 (Addressables) '{prefabKey}': {e.Message}");
                    failedCount++;
                }
#endif
            }

            Debug.Log($"[TrainingManager] 캐릭터 프리팹 로드 완료. 성공: {loadedCount}, 실패: {failedCount}");
            await UniTask.CompletedTask;
        }

#if UNITY_EDITOR
        // Addressable_Key → 실제 프리팹 파일명 매핑 (불일치하는 경우)
        private static readonly Dictionary<string, string> keyToFileNameMap = new Dictionary<string, string>
        {
            { "Serin", "Serene" },
            { "Ki", "Key" }
        };

        /// <summary>
        /// Editor 모드에서 AssetDatabase를 통해 프리팹 로드
        /// </summary>
        private GameObject LoadPrefabFromAssetDatabase(string addressableKey)
        {
            // 키-파일명 매핑이 있으면 변환
            string fileName = addressableKey;
            if (keyToFileNameMap.TryGetValue(addressableKey, out string mappedName))
            {
                fileName = mappedName;
            }

            // Character Prefabs 폴더에서 검색
            string prefabPath = $"Assets/Character Prefabs/{fileName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null)
            {
                return prefab;
            }

            // 원본 키로도 시도
            if (fileName != addressableKey)
            {
                prefabPath = $"Assets/Character Prefabs/{addressableKey}.prefab";
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    return prefab;
                }
            }

            // 못 찾으면 프로젝트 전체에서 검색
            string[] guids = AssetDatabase.FindAssets($"t:Prefab {fileName}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith($"{fileName}.prefab") || path.EndsWith($"{addressableKey}.prefab"))
                {
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        Debug.Log($"[TrainingManager] 프리팹 발견: {path}");
                        return prefab;
                    }
                }
            }

            return null;
        }
#endif

        /// <summary>
        /// 캐릭터 데이터 캐시 로드
        /// </summary>
        private void LoadCharacterDataCache()
        {
            var characterTable = CSVLoader.Instance?.GetTable<CharacterData>();
            if (characterTable == null)
            {
                Debug.LogError("[TrainingManager] CharacterTable 로드 실패");
                return;
            }

            characterDataList.Clear();

            var allCharacters = characterTable.GetAll();
            for (int i = 0; i < allCharacters.Count; i++)
            {
                characterDataList.Add(allCharacters[i]);
            }

            Debug.Log($"[TrainingManager] 캐릭터 데이터 {characterDataList.Count}개 캐시 완료");

            // 첫 번째 캐릭터를 기본 선택
            if (characterDataList.Count > 0)
            {
                selectedCharacterId = characterDataList[0].Character_ID;
            }
        }

        /// <summary>
        /// Character_ID로 프리팹 키 조회
        /// Character_ID → CharacterData.Path_ID → PathData.Addressable_Key → "Prefab_" + key
        /// </summary>
        private string GetCharacterPrefabKey(int characterId)
        {
            var characterData = CSVLoader.Instance?.GetData<CharacterData>(characterId);
            if (characterData == null)
            {
                Debug.LogError($"[TrainingManager] CharacterData를 찾을 수 없음: {characterId}");
                return null;
            }

            var pathData = CSVLoader.Instance?.GetData<PathData>(characterData.Path_ID);
            if (pathData == null)
            {
                Debug.LogError($"[TrainingManager] PathData를 찾을 수 없음: Path_ID={characterData.Path_ID}");
                return null;
            }

            return $"Prefab_{pathData.Addressable_Key}";
        }

        #endregion

        #region Public API - Settings

        /// <summary>
        /// 캐릭터 선택
        /// </summary>
        public void SetCharacter(int characterId)
        {
            selectedCharacterId = characterId;
            Debug.Log($"[TrainingManager] 캐릭터 설정: {characterId}");
        }

        /// <summary>
        /// 등급(성급) 선택
        /// </summary>
        public void SetGrade(int grade)
        {
            selectedGrade = Mathf.Clamp(grade, 1, 3);
            Debug.Log($"[TrainingManager] 등급 설정: {selectedGrade}성");
        }

        /// <summary>
        /// 강화 단계 선택
        /// </summary>
        public void SetEnhancement(int level)
        {
            selectedEnhancement = Mathf.Clamp(level, 0, 10);
            Debug.Log($"[TrainingManager] 강화 단계 설정: {selectedEnhancement}");
        }

        /// <summary>
        /// 메인 스킬 책갈피 선택 (BookmarkSkillTable의 Skill_ID)
        /// </summary>
        public void SetMainSkillBookmark(int skillId)
        {
            selectedMainSkillBookmarkId = skillId;
            Debug.Log($"[TrainingManager] 메인 스킬 책갈피 설정: {skillId}");
        }

        /// <summary>
        /// 보조 스킬 선택 (SupportSkillTable의 support_id) - 책갈피 아님
        /// </summary>
        public void SetSupportSkill(int supportSkillId)
        {
            selectedSupportSkillId = supportSkillId;
            Debug.Log($"[TrainingManager] 보조 스킬 설정: {supportSkillId}");
        }

        /// <summary>
        /// 스탯 책갈피 선택 (BookmarkOptionTable의 Option_ID)
        /// slotIndex: 0~3 (4개 슬롯)
        /// </summary>
        public void SetStatBookmark(int slotIndex, int optionId)
        {
            if (slotIndex < 0 || slotIndex >= 4)
            {
                Debug.LogWarning($"[TrainingManager] 잘못된 슬롯 인덱스: {slotIndex}");
                return;
            }
            selectedStatBookmarkIds[slotIndex] = optionId;
            Debug.Log($"[TrainingManager] 스탯 책갈피 {slotIndex + 1} 설정: {optionId}");
        }

        /// <summary>
        /// 내 덱 사용 모드 설정
        /// </summary>
        public void SetUseMyDeck(bool use)
        {
            useMyDeck = use;
            Debug.Log($"[TrainingManager] 내 덱 사용 모드: {(use ? "ON" : "OFF")}");
        }

        /// <summary>
        /// 허수아비 수량 설정
        /// </summary>
        public void SetDummyCount(int count)
        {
            dummyCount = Mathf.Clamp(count, 1, 10);
            Debug.Log($"[TrainingManager] 허수아비 수량 설정: {dummyCount}");

            // 실행 중이면 허수아비 재스폰
            if (isRunning)
            {
                SpawnDummies();
            }
        }

        /// <summary>
        /// 허수아비 수량 증가
        /// </summary>
        public void IncreaseDummyCount()
        {
            SetDummyCount(dummyCount + 1);
        }

        /// <summary>
        /// 허수아비 수량 감소
        /// </summary>
        public void DecreaseDummyCount()
        {
            SetDummyCount(dummyCount - 1);
        }

        #endregion

        #region Public API - Control

        /// <summary>
        /// 훈련 시작
        /// </summary>
        public async void StartTraining()
        {
            if (isRunning) return;

            Debug.Log($"[TrainingManager] 훈련 시작 (내 덱 사용: {useMyDeck})");
            isRunning = true;

            if (useMyDeck)
            {
                // 내 덱 모드: 기존 덱 데이터로 캐릭터들 스폰
                await SpawnMyDeckAsync();
            }
            else
            {
                // 단일 캐릭터 모드: 설정된 캐릭터 1명 스폰
                await SpawnCharacterAsync();
            }

            // 허수아비 스폰
            SpawnDummies();

            // DPS 측정 초기화 후 시작
            if (dpsCalculator != null)
            {
                dpsCalculator.Reset();
                dpsCalculator.StartMeasurement();
            }

            OnTrainingStarted?.Invoke();
        }

        /// <summary>
        /// 훈련 정지 (일시정지, 설정 변경 가능)
        /// </summary>
        public void StopTraining()
        {
            if (!isRunning) return;

            Debug.Log("[TrainingManager] 훈련 정지");
            isRunning = false;

            // 단일 캐릭터 비활성화
            if (currentCharacter != null)
            {
                currentCharacter.SetAutoAttackEnabled(false);
                currentCharacter.gameObject.SetActive(false);
            }

            // 덱 캐릭터들 비활성화
            for (int i = 0; i < deckCharacters.Count; i++)
            {
                if (deckCharacters[i] != null)
                {
                    deckCharacters[i].SetAutoAttackEnabled(false);
                    deckCharacters[i].gameObject.SetActive(false);
                }
            }

            // 허수아비 비활성화
            DespawnDummies();

            // DPS 측정 일시정지
            if (dpsCalculator != null)
            {
                dpsCalculator.PauseMeasurement();
            }

            OnTrainingStopped?.Invoke();
        }

        /// <summary>
        /// 훈련 리셋 (모든 데이터 초기화)
        /// </summary>
        public void ResetTraining()
        {
            Debug.Log("[TrainingManager] 훈련 리셋");

            // DPS 측정 리셋
            if (dpsCalculator != null)
            {
                dpsCalculator.Reset();

                // 실행 중이면 타이머 재시작
                if (isRunning)
                {
                    dpsCalculator.StartMeasurement();
                }
            }

            OnTrainingReset?.Invoke();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 캐릭터 스폰 (Addressables로 로드된 프리팹 사용)
        /// </summary>
        private async UniTask SpawnCharacterAsync()
        {
            // 기존 캐릭터 제거
            if (currentCharacter != null)
            {
                Destroy(currentCharacter.gameObject);
                currentCharacter = null;
            }

            if (selectedCharacterId <= 0)
            {
                Debug.LogWarning("[TrainingManager] 캐릭터가 선택되지 않음");
                return;
            }

            // 프리팹 키 조회
            string prefabKey = GetCharacterPrefabKey(selectedCharacterId);
            if (string.IsNullOrEmpty(prefabKey))
            {
                Debug.LogError($"[TrainingManager] 프리팹 키를 찾을 수 없음: characterId={selectedCharacterId}");
                return;
            }

            // 캐시된 프리팹 조회
            if (!loadedCharacterPrefabs.TryGetValue(prefabKey, out GameObject prefab) || prefab == null)
            {
                Debug.LogError($"[TrainingManager] 캐릭터 프리팹이 로드되지 않음: {prefabKey}");
                return;
            }

            // 스폰 위치
            Vector3 spawnPos = characterSpawnPoint != null ? characterSpawnPoint.position : Vector3.zero;
            Quaternion spawnRot = characterSpawnPoint != null ? characterSpawnPoint.rotation : Quaternion.identity;

            // 프리팹 인스턴스화
            GameObject charObj = Instantiate(prefab, spawnPos, spawnRot);
            currentCharacter = charObj.GetComponent<Character>();

            if (currentCharacter == null)
            {
                Debug.LogError($"[TrainingManager] 프리팹에 Character 컴포넌트가 없음");
                Destroy(charObj);
                return;
            }

            // 자동 공격 비활성화 (Initialize 전에)
            currentCharacter.SetAutoAttackEnabled(false);

            // 캐릭터 초기화
            currentCharacter.Initialize(selectedCharacterId);

            // 성급 적용 (1성이 기본, 2/3성으로 업그레이드)
            ApplyStarTier(currentCharacter, selectedGrade);

            // 강화 단계 적용
            ApplyEnhancement(currentCharacter, selectedEnhancement);

            // 메인 스킬 책갈피 적용
            if (selectedMainSkillBookmarkId > 0)
            {
                ApplyMainSkillBookmark(currentCharacter, selectedMainSkillBookmarkId);
            }

            // 보조 스킬 적용 (책갈피 아님)
            if (selectedSupportSkillId > 0)
            {
                currentCharacter.EquipSupportSkill(selectedSupportSkillId);
            }

            // 스탯 책갈피 4개 적용 (중복 가능)
            for (int i = 0; i < selectedStatBookmarkIds.Length; i++)
            {
                if (selectedStatBookmarkIds[i] > 0)
                {
                    ApplyStatBookmark(currentCharacter, selectedStatBookmarkIds[i]);
                }
            }

            // TrainingScene용 투사체 템플릿 설정 (GameManager/Pool 없이 투사체 사용 가능하게)
            if (projectileTemplate != null)
            {
                currentCharacter.SetProjectileTemplate(projectileTemplate);
            }

            // 자동 공격 활성화
            currentCharacter.SetAutoAttackEnabled(true);

            var characterData = CSVLoader.Instance?.GetData<CharacterData>(selectedCharacterId);
            Debug.Log($"[TrainingManager] 캐릭터 스폰 완료: ID={selectedCharacterId}, 성급={selectedGrade}, 강화={selectedEnhancement}");

            // CSVLoader 비동기 대기용 (형식 맞추기)
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 성급 적용 (1성 기본, 2/3성으로 업그레이드)
        /// </summary>
        private void ApplyStarTier(Character character, int targetGrade)
        {
            // 1성이 기본이므로, targetGrade까지 업그레이드
            for (int i = 1; i < targetGrade; i++)
            {
                if (!character.UpgradeStarTier())
                {
                    break;
                }
            }
            Debug.Log($"[TrainingManager] 성급 적용: {targetGrade}성");
        }

        /// <summary>
        /// 강화 단계 적용 (CharacterEnhancementTable 기반)
        /// </summary>
        private void ApplyEnhancement(Character character, int enhancementLevel)
        {
            if (enhancementLevel <= 0) return;

            // CharacterEnhancementTable에서 해당 캐릭터의 강화 데이터 조회
            var enhancementTable = CSVLoader.Instance?.GetTable<CharacterEnhancementData>();
            if (enhancementTable == null)
            {
                Debug.LogWarning("[TrainingManager] CharacterEnhancementTable 로드 실패");
                return;
            }

            // 캐릭터 ID로 강화 데이터 찾기
            CharacterEnhancementData enhancementData = null;
            var allEnhancements = enhancementTable.GetAll();
            for (int i = 0; i < allEnhancements.Count; i++)
            {
                if (allEnhancements[i].Character_ID == selectedCharacterId)
                {
                    enhancementData = allEnhancements[i];
                    break;
                }
            }

            if (enhancementData == null)
            {
                Debug.LogWarning($"[TrainingManager] 캐릭터 {selectedCharacterId}의 강화 데이터를 찾을 수 없음");
                return;
            }

            // 강화 레벨별 스탯 증가 적용
            // Pw_Level1~10은 각 강화 단계의 스탯 증가량(%)을 나타냄
            float totalBonus = 0f;
            for (int i = 1; i <= enhancementLevel; i++)
            {
                float levelBonus = GetEnhancementBonus(enhancementData, i);
                totalBonus += levelBonus;
            }

            // 데미지 버프로 적용 (% 단위)
            if (totalBonus > 0)
            {
                character.ApplyStatBuff(StatType.Damage, totalBonus / 100f);
                Debug.Log($"[TrainingManager] 강화 {enhancementLevel}단계 적용: +{totalBonus}%");
            }
        }

        /// <summary>
        /// 강화 데이터에서 특정 레벨의 보너스 값 추출
        /// </summary>
        private float GetEnhancementBonus(CharacterEnhancementData data, int level)
        {
            return level switch
            {
                1 => data.Pw_Level1,
                2 => data.Pw_Level2,
                3 => data.Pw_Level3,
                4 => data.Pw_Level4,
                5 => data.Pw_Level5,
                6 => data.Pw_Level6,
                7 => data.Pw_Level7,
                8 => data.Pw_Level8,
                9 => data.Pw_Level9,
                10 => data.Pw_Level10,
                _ => 0f
            };
        }

        /// <summary>
        /// 메인 스킬 책갈피 적용 (액티브 스킬로 설정)
        /// </summary>
        private void ApplyMainSkillBookmark(Character character, int skillId)
        {
            // MainSkillTable에서 스킬 확인
            var mainSkillData = CSVLoader.Instance?.GetData<MainSkillData>(skillId);
            if (mainSkillData == null)
            {
                Debug.LogWarning($"[TrainingManager] MainSkillData를 찾을 수 없음: {skillId}");
                return;
            }

            // 액티브 스킬로 설정
            character.SetSkillIds(character.GetBasicAttackSkillId(), skillId, character.GetSupportSkillId());
            Debug.Log($"[TrainingManager] 메인 스킬 책갈피 적용: {mainSkillData.skill_name}");
        }

        /// <summary>
        /// 스탯 책갈피 적용 (BookmarkOptionTable 기반)
        /// </summary>
        private void ApplyStatBookmark(Character character, int optionId)
        {
            var optionData = CSVLoader.Instance?.GetData<BookmarkOptionData>(optionId);
            if (optionData == null)
            {
                Debug.LogWarning($"[TrainingManager] BookmarkOptionData를 찾을 수 없음: {optionId}");
                return;
            }

            // OptionType에 따라 스탯 적용
            float value = optionData.Option_Value;

            switch (optionData.Option_Type)
            {
                case OptionType.AttackPower:
                    character.ApplyStatBuff(StatType.Damage, value);
                    break;
                case OptionType.AttackSpeed:
                    character.ApplyStatBuff(StatType.AttackSpeed, value);
                    break;
                case OptionType.CritMultiplier:
                    character.ApplyStatBuff(StatType.CritMultiplier, value);
                    break;
                case OptionType.CritChance:
                    character.ApplyStatBuff(StatType.CritChance, value);
                    break;
                case OptionType.CritDamage:
                    character.ApplyStatBuff(StatType.CritMultiplier, value);
                    break;
                case OptionType.BossDamage:
                    // 보스 데미지는 별도 처리 필요 (현재는 일반 데미지로 적용)
                    character.ApplyStatBuff(StatType.Damage, value);
                    break;
                default:
                    Debug.Log($"[TrainingManager] 스탯 책갈피 적용: OptionType={optionData.Option_Type}, Value={value}");
                    break;
            }
        }

        /// <summary>
        /// 내 덱 모드: DeckManager에서 덱 데이터를 가져와 캐릭터들 스폰
        /// </summary>
        private async UniTask SpawnMyDeckAsync()
        {
            // 기존 캐릭터들 제거
            DespawnDeckCharacters();
            if (currentCharacter != null)
            {
                Destroy(currentCharacter.gameObject);
                currentCharacter = null;
            }

            // DeckManager에서 현재 덱 정보 가져오기
            if (DeckManager.Instance == null)
            {
                Debug.LogError("[TrainingManager] DeckManager가 없습니다");
                return;
            }

            var deckCharacterIds = DeckManager.Instance.GetValidCharacters();
            if (deckCharacterIds == null || deckCharacterIds.Count == 0)
            {
                Debug.LogWarning("[TrainingManager] 덱에 캐릭터가 없습니다");
                return;
            }

            Debug.Log($"[TrainingManager] 내 덱 캐릭터 {deckCharacterIds.Count}명 스폰 시작");

            int spawnIndex = 0;
            foreach (var characterId in deckCharacterIds)
            {
                if (characterId <= 0) continue;

                // 프리팹 키 조회
                string prefabKey = GetCharacterPrefabKey(characterId);
                if (string.IsNullOrEmpty(prefabKey)) continue;

                // 캐시된 프리팹 조회
                if (!loadedCharacterPrefabs.TryGetValue(prefabKey, out GameObject prefab) || prefab == null)
                {
                    Debug.LogWarning($"[TrainingManager] 프리팹 없음: {prefabKey}");
                    continue;
                }

                // 스폰 위치 (가로로 배치)
                float xOffset = (spawnIndex - (deckCharacterIds.Count - 1) / 2f) * 2f;
                Vector3 spawnPos = (characterSpawnPoint != null ? characterSpawnPoint.position : Vector3.zero)
                                   + new Vector3(xOffset, 0f, 0f);
                Quaternion spawnRot = characterSpawnPoint != null ? characterSpawnPoint.rotation : Quaternion.identity;

                // 프리팹 인스턴스화
                GameObject charObj = Instantiate(prefab, spawnPos, spawnRot);
                Character character = charObj.GetComponent<Character>();

                if (character == null)
                {
                    Destroy(charObj);
                    continue;
                }

                // 자동 공격 비활성화 후 초기화
                character.SetAutoAttackEnabled(false);
                character.Initialize(characterId);

                // 캐릭터 보유 데이터에서 성급/강화 정보 적용
                ApplyOwnedCharacterData(character, characterId);

                // TrainingScene용 투사체 템플릿 설정
                if (projectileTemplate != null)
                {
                    character.SetProjectileTemplate(projectileTemplate);
                }

                // 자동 공격 활성화
                character.SetAutoAttackEnabled(true);

                deckCharacters.Add(character);
                spawnIndex++;
            }

            Debug.Log($"[TrainingManager] 내 덱 캐릭터 {deckCharacters.Count}명 스폰 완료");
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 보유 캐릭터 데이터 적용 (성급, 강화, 장착된 책갈피 등)
        /// TODO: CharacterEnhancementManager/BookmarkEquipManager와 연동
        /// </summary>
        private void ApplyOwnedCharacterData(Character character, int characterId)
        {
            // 강화 레벨 적용 (CharacterEnhancementManager에서 가져오기)
            if (CharacterEnhancementManager.Instance != null)
            {
                int enhancementLevel = CharacterEnhancementManager.Instance.GetEnhancementLevel(characterId);
                if (enhancementLevel > 0)
                {
                    ApplyEnhancement(character, enhancementLevel);
                }
            }

            // TODO: 성급 정보 연동 (현재 시스템에 성급 저장 기능 추가 필요)
            // TODO: 장착된 책갈피 정보 연동 (BookmarkEquipManager 연동)
        }

        /// <summary>
        /// 덱 캐릭터들 제거
        /// </summary>
        private void DespawnDeckCharacters()
        {
            for (int i = 0; i < deckCharacters.Count; i++)
            {
                if (deckCharacters[i] != null)
                {
                    Destroy(deckCharacters[i].gameObject);
                }
            }
            deckCharacters.Clear();
        }

        /// <summary>
        /// 허수아비 스폰
        /// </summary>
        private void SpawnDummies()
        {
            // 기존 허수아비 제거
            DespawnDummies();

            if (dummyPrefab == null)
            {
                Debug.LogWarning("[TrainingManager] 허수아비 프리팹이 설정되지 않음");
                return;
            }

            // 스폰 위치 계산
            for (int i = 0; i < dummyCount; i++)
            {
                Vector3 spawnPos;
                if (dummySpawnPoints != null && i < dummySpawnPoints.Length && dummySpawnPoints[i] != null)
                {
                    spawnPos = dummySpawnPoints[i].position;
                }
                else
                {
                    // 기본 위치: 캐릭터 앞 5m, 가로로 1m 간격
                    float xOffset = (i - (dummyCount - 1) / 2f) * 1f;
                    spawnPos = (characterSpawnPoint != null ? characterSpawnPoint.position : Vector3.zero)
                               + new Vector3(xOffset, 0f, 5f);
                }

                GameObject dummyObj = Instantiate(dummyPrefab, spawnPos, Quaternion.identity);
                DummyTarget dummy = dummyObj.GetComponent<DummyTarget>();
                if (dummy == null)
                {
                    dummy = dummyObj.AddComponent<DummyTarget>();
                }
                activeDummies.Add(dummy);
                Debug.Log($"[TrainingManager] 허수아비 {i + 1} 스폰: position={spawnPos}, layer={dummyObj.layer}");
            }

            Debug.Log($"[TrainingManager] 허수아비 {dummyCount}마리 스폰 완료");
        }

        /// <summary>
        /// 허수아비 제거
        /// </summary>
        private void DespawnDummies()
        {
            for (int i = 0; i < activeDummies.Count; i++)
            {
                if (activeDummies[i] != null)
                {
                    Destroy(activeDummies[i].gameObject);
                }
            }
            activeDummies.Clear();
        }

        #endregion

        #region Getters for UI

        public DPSCalculator GetDPSCalculator() => dpsCalculator;

        #endregion
    }
}
