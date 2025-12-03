using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novelian.Combat;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.Settings;
using NovelianMagicLibraryDefense.UI;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Manages stage progression, player leveling, and stage timer
    /// MonoBehaviour 기반 Manager
    /// </summary>
    public class StageManager : BaseManager
    {
        [Header("Dependencies")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private MonsterEvents monsterEvents;
        [SerializeField] private StageEvents stageEvents;
        [SerializeField] private Wall wallComponent; // JML: Wall 참조 (Barrier HP 설정용)
        [SerializeField] private CharacterPlacementManager characterPlacementManager; // JML: Inspector에서 직접 참조 (Issue #349)

        [Header("Settings")]
        [SerializeField] private StageSettings stageSettings;

        public int CurrentStageId { get; private set; }
        public string StageName { get; private set; }

        #region PlayerStageLevel
        private const int MAX_LEVEL = 50;
        private int currentExp = 0;
        private int level = 0;

        /// <summary>
        /// 다음 레벨업에 필요한 경험치를 CSV에서 가져오기
        /// </summary>
        private int GetRequiredExpForNextLevel()
        {
            int targetLevel = level + 1;

            // 최대 레벨 체크
            if (targetLevel > MAX_LEVEL)
            {
                return int.MaxValue; // 레벨업 불가
            }

            // Level_ID 계산: 0701, 0702, ... 0750
            int levelId = 700 + targetLevel;

            // CSV에서 PlayerLevelData 조회
            if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
            {
                var levelData = CSVLoader.Instance.GetData<PlayerLevelData>(levelId);
                if (levelData != null)
                {
                    return (int)levelData.Req_EXP;
                }
            }

            // fallback: stageSettings 사용
            return stageSettings != null ? stageSettings.expPerLevel : 100;
        }

        /// <summary>
        /// 최대 레벨 도달 여부 확인
        /// </summary>
        private bool IsMaxLevel()
        {
            return level >= MAX_LEVEL;
        }

        // LCB: Debug - Press 'L' key to instantly level up (add 100 exp)
        #endregion

        #region Timer
        private float RemainingTime { get; set; }
        private bool isStageCleared = false;
        private CancellationTokenSource timerCts;
        #endregion

        #region GlobalStatBuffs (Issue #349)
        /// <summary>
        /// JML: 전역 스텟 버프 저장소
        /// 스텟 카드 선택 시 누적되며, 필드의 모든 캐릭터에 적용됨
        /// 새로 소환되는 캐릭터에도 자동 적용
        /// </summary>
        private Dictionary<StatType, float> globalStatBuffs = new Dictionary<StatType, float>();
        #endregion

        /// <summary>
        /// LMJ: Set stage duration (useful for CSV data loading in the future)
        /// </summary>
        public void SetStageDuration(float duration)
        {
            if (stageSettings != null)
            {
                stageSettings.stageDuration = duration;
            }
            // Debug.Log($"[StageManager] Stage duration set to {duration} seconds");
        }

        protected override void OnInitialize()
        {
            // Debug.Log("[StageManager] Initializing stage");

            // JML: CSV 데이터 기반 스테이지 초기화
            InitializeFromCSV();

            // LMJ: Subscribe to monster death event for exp via EventChannel
            if (monsterEvents != null)
            {
                monsterEvents.AddMonsterDiedListener(AddExp);
            }

            // LMJ: Start stage timer using UniTask (no MonoBehaviour required!)
            timerCts = new CancellationTokenSource();
            StageTimer(timerCts.Token).Forget();

            // LCB: Show start card selection before starting the game
            ShowStartCardSelection().Forget();

            // Debug.Log("[StageManager] Initialized");
        }

        /// <summary>
        /// JML: CSV 데이터 기반으로 스테이지 초기화
        /// SelectedStage.Data에서 Time_Limit, Wave ID, Barrier_HP 등을 가져옴
        /// </summary>
        private void InitializeFromCSV()
        {
            if (!SelectedStage.HasSelection)
            {
                Debug.LogWarning("[StageManager] No stage selected, using default stage (010101)");
                StageName = "Stage 1-1";

                // JML: 기본 스테이지 데이터 로드 (스테이지 선택 없이 GameScene 직접 실행 시)
                StageData defaultStage = CSVLoader.Instance.GetTable<StageData>().GetId(010101);
                if (defaultStage != null)
                {
                    SelectedStage.Data = defaultStage;
                }
                else
                {
                    Debug.LogError("[StageManager] Default stage (010101) not found in CSV!");
                    return;
                }
            }

            StageData stageData = SelectedStage.Data;
            CurrentStageId = stageData.Stage_ID;
            StageName = $"Stage {stageData.Chapter_Number}";

            // 1. Time Limit 설정
            SetStageDuration(stageData.Time_Limit);
            Debug.Log($"[StageManager] Time Limit set to {stageData.Time_Limit} seconds");

            // 2. Wall(Barrier) HP 설정
            if (wallComponent != null)
            {
                wallComponent.SetMaxHealth(stageData.Barrier_HP);
                Debug.Log($"[StageManager] Wall HP set to {stageData.Barrier_HP}");
            }
            else
            {
                Debug.LogError("[StageManager] wallComponent is null! Cannot set Barrier HP.");
            }

            // 3. Wave 데이터 수집 및 WaveManager 초기화
            List<WaveData> waveDataList = CollectWaveData(stageData);
            if (waveDataList.Count > 0)
            {
                waveManager.InitializeWithWaveData(waveDataList);
                waveManager.WaveLoop().Forget();
                Debug.Log($"[StageManager] Initialized with {waveDataList.Count} waves from CSV");
            }
            else
            {
                Debug.LogError("[StageManager] No wave data found! Check Wave IDs in StageTable.csv");
            }
        }

        /// <summary>
        /// JML: StageData에서 Wave ID들을 수집하여 WaveData 리스트로 반환
        /// Wave_1_ID ~ Wave_4_ID 중 0이 아닌 것만 가져옴
        /// </summary>
        private List<WaveData> CollectWaveData(StageData stageData)
        {
            List<WaveData> waveDataList = new List<WaveData>();
            int[] waveIds = new int[]
            {
                stageData.Wave_1_ID,
                stageData.Wave_2_ID,
                stageData.Wave_3_ID,
                stageData.Wave_4_ID
            };

            foreach (int waveId in waveIds)
            {
                if (waveId == 0) continue;

                WaveData waveData = CSVLoader.Instance.GetTable<WaveData>().GetId(waveId);
                if (waveData != null)
                {
                    waveDataList.Add(waveData);
                    Debug.Log($"[StageManager] Added Wave {waveId}: Monster_ID={waveData.Monster_ID}, " +
                              $"Count={waveData.Monster_Count}, Spawn_Time={waveData.Spawn_Time}s");
                }
                else
                {
                    Debug.LogWarning($"[StageManager] Wave data not found for ID: {waveId}");
                }
            }

            return waveDataList;
        }

        /// <summary>
        /// LMJ: Show start card selection (2 character cards only)
        /// Game does NOT pause for start selection
        /// </summary>
        private async UniTaskVoid ShowStartCardSelection()
        {
            Debug.Log("[StageManager] Opening start card selection (2 character cards)");

            if (uiManager != null)
            {
                uiManager.OpenCardSelectForGameStart(); // Opens 2 character cards, no pause

                // Wait until card panel is closed
                while (uiManager != null && uiManager.IsCardSelectOpen())
                {
                    await UniTask.Yield();
                }

                Debug.Log("[StageManager] Start card selection completed");
            }
            else
            {
                Debug.LogError("[StageManager] UIManager is null! Cannot show start card selection.");
            }
        }

        protected override void OnReset()
        {
            // Debug.Log("[StageManager] Resetting stage");

            // LMJ: Cancel and dispose timer
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;

            // LMJ: Unsubscribe events via EventChannel
            if (monsterEvents != null)
            {
                monsterEvents.RemoveMonsterDiedListener(AddExp);
            }

            // LMJ: Reset stage data
            currentExp = 0;
            level = 0;
            RemainingTime = stageSettings != null ? stageSettings.stageDuration : 600f;
            isStageCleared = false;
            CurrentStageId = 0;
            Time.timeScale = 1f;

            // JML: Reset global stat buffs (Issue #349)
            globalStatBuffs.Clear();
        }

        protected override void OnDispose()
        {
            // Debug.Log("[StageManager] Disposing stage");

            // LMJ: Cancel timer
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;

            // LMJ: Unsubscribe events via EventChannel
            if (monsterEvents != null)
            {
                monsterEvents.RemoveMonsterDiedListener(AddExp);
            }

            // JML: Ensure time scale reset
            Time.timeScale = 1f;
        }

        /// <summary>
        /// LMJ: Stage timer using UniTask (works without MonoBehaviour!)
        /// Uses CancellationToken for proper cleanup
        /// </summary>
        private async UniTaskVoid StageTimer(CancellationToken ct)
        {
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            RemainingTime = duration;
            float startTime = Time.time;
            float endTime = startTime + duration;

            try
            {
                while (Time.time < endTime && !isStageCleared)
                {
                    RemainingTime = endTime - Time.time;

                    // LMJ: Update UI timer display
                    if (uiManager != null)
                    {
                        uiManager.UpdateWaveTimer(RemainingTime);
                    }

                    if ((int)RemainingTime <= 0)
                    {
                        RemainingTime = 0;
                        HandleTimeUp();
                        break;
                    }

                    // LMJ: Wait 1 second (1000ms)
                    await UniTask.Delay(1000, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Debug.Log("[StageManager] Timer cancelled");
            }
        }

        public void HandleTimeUp()
        {
            // Debug.Log("[StageManager] Time's Up! Stage Failed");
            waveManager.WaveClear();

            // LMJ: Use EventChannel instead of static event
            if (stageEvents != null)
            {
                stageEvents.RaiseTimeUp();
            }
        }

        /// <summary>
        /// LMJ: Add experience when monster dies
        /// </summary>
        private void AddExp(Monster monster)
        {
            // 최대 레벨이면 경험치 획득 무시
            if (IsMaxLevel())
            {
                return;
            }

            int maxExp = GetRequiredExpForNextLevel();
            currentExp += monster.Exp;
            // Debug.Log($"[StageManager] Exp +{monster.Exp} -> {currentExp}/{maxExp}"); // LCB: Debug exp gain
            uiManager.UpdateExperience(currentExp, maxExp);
            if (currentExp >= maxExp)
            {
                // Debug.Log($"[StageManager] Level up triggered! currentExp={currentExp}, maxExp={maxExp}"); // LCB: Debug level up trigger
                LevelUp().Forget();
            }
        }

        /// <summary>
        /// LMJ: Handle level up with UniTask
        /// LCB: Integrate level-up card system (Issue 139)
        /// </summary>
        private async UniTaskVoid LevelUp()
        {
            var previousTimeScale = Time.timeScale;
            // Debug.Log($"타임 스케일 {previousTimeScale}"); // LCB: Debug previous time scale
            int maxExp = GetRequiredExpForNextLevel();
            while (currentExp >= maxExp && !IsMaxLevel())
            {
                currentExp -= maxExp;
                level++;

                // 다음 레벨의 필요 경험치로 갱신
                maxExp = GetRequiredExpForNextLevel();
                uiManager.UpdateExperience(currentExp, maxExp);

                // LMJ: Open card selection for level up
                Debug.Log($"[StageManager] Level up to {level}! (Next: {maxExp} exp)");

                if (uiManager != null)
                {
                    uiManager.OpenCardSelectForLevelUp(); // Opens with 2 random cards (character + ability mix)

                    // Wait until card panel is closed
                    while (uiManager != null && GameManager.Instance?.UI?.IsCardSelectOpen() == true)
                    {
                        await UniTask.Yield();
                    }
                }
                else
                {
                    Debug.LogError("[StageManager] UIManager is null!");
                }
            }
            Time.timeScale = previousTimeScale; // Resume game
            // Debug.Log($"타임 스케일{Time.timeScale}"); // LCB: Debug resumed time scale
        }

        /// <summary>
        /// LMJ: Get current stage progress (useful for UI)
        /// </summary>
        public float GetStageProgress()
        {
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            return 1f - (RemainingTime / duration);
        }

        /// <summary>
        /// LMJ: Get remaining time (useful for UI)
        /// </summary>
        public float GetRemainingTime()
        {
            return RemainingTime;
        }

        /// <summary>
        /// LMJ: Get current level (useful for UI)
        /// </summary>
        public int GetCurrentLevel()
        {
            return level;
        }

        /// <summary>
        /// LMJ: Get current exp progress (useful for UI)
        /// </summary>
        public float GetExpProgress()
        {
            if (IsMaxLevel())
            {
                return 1f; // 최대 레벨이면 100%로 표시
            }
            int maxExp = GetRequiredExpForNextLevel();
            return (float)currentExp / maxExp;
        }

        /// <summary>
        /// JML: Get remaining time
        /// </summary>
        public float GetRemainderTime()
        {
            return RemainingTime;
        }

        /// <summary>
        /// JML: Get Progress time
        /// </summary>
        public float GetProgressTime()
        {
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            return duration - RemainingTime;
        }

        public int GetReward()
        {
            // JML: Example reward calculation based on level and time
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            int baseReward = 100;
            int timeBonus = (int)(duration - RemainingTime) / 10;
            return baseReward + (level * 50) + timeBonus;
        }

        #region GlobalStatBuff Methods (Issue #349)

        /// <summary>
        /// JML: 전역 스텟 버프 적용
        /// 스텟 카드 선택 시 호출됨
        /// 기존 필드 캐릭터 + 새로 소환되는 캐릭터 모두에 적용
        /// </summary>
        /// <param name="statType">스텟 타입 (StatType enum)</param>
        /// <param name="value">증가 값 (% 단위, 예: 0.1 = 10%)</param>
        public void ApplyGlobalStatBuff(StatType statType, float value)
        {
            // 1. 전역 버프 저장소에 누적
            if (globalStatBuffs.ContainsKey(statType))
            {
                globalStatBuffs[statType] += value;
            }
            else
            {
                globalStatBuffs[statType] = value;
            }

            Debug.Log($"[StageManager] Global Stat Buff Applied: {statType} +{value * 100f}% (Total: {globalStatBuffs[statType] * 100f}%)");

            // 2. 현재 필드의 모든 캐릭터에 버프 적용
            ApplyBuffToAllCharacters(statType, value);
        }

        /// <summary>
        /// JML: 특정 스텟의 전역 버프 총합 조회
        /// 새로 소환되는 캐릭터에 적용할 때 사용
        /// </summary>
        public float GetGlobalStatBuff(StatType statType)
        {
            return globalStatBuffs.TryGetValue(statType, out float value) ? value : 0f;
        }

        /// <summary>
        /// JML: 모든 전역 스텟 버프 조회
        /// 새로 소환되는 캐릭터에 모든 버프 적용 시 사용
        /// </summary>
        public Dictionary<StatType, float> GetAllGlobalStatBuffs()
        {
            return new Dictionary<StatType, float>(globalStatBuffs);
        }

        /// <summary>
        /// JML: 현재 필드의 모든 캐릭터에 버프 적용
        /// CharacterPlacementManager의 GetAllCharacters() 사용
        /// Inspector에서 직접 참조 설정 필요 (Tag 기반 lookup 제거)
        /// </summary>
        private void ApplyBuffToAllCharacters(StatType statType, float value)
        {
            // JML: Inspector에서 설정한 CharacterPlacementManager 참조 사용
            if (characterPlacementManager == null)
            {
                Debug.LogWarning("[StageManager] CharacterPlacementManager 참조가 없습니다! Inspector에서 설정해주세요.");
                return;
            }

            var characters = characterPlacementManager.GetAllCharacters();
            if (characters == null || characters.Count == 0)
            {
                Debug.Log("[StageManager] No characters in field to apply buff.");
                return;
            }

            foreach (var character in characters)
            {
                if (character != null)
                {
                    character.ApplyStatBuff(statType, value);
                }
            }

            Debug.Log($"[StageManager] Buff applied to {characters.Count} characters");
        }

        #endregion
    }
}
