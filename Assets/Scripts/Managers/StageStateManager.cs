using System;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.Managers;
using NovelianMagicLibraryDefense.UI;
using Novelian.Combat;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    public enum StageState
    {
        Playing,    // Stage in progress
        Cleared,    // Stage cleared (all enemies defeated or boss killed)
        Failed      // Stage failed (wall destroyed or time up with enemies remaining)
    }

    /// <summary>
    /// MonoBehaviour 기반 Manager (VContainer 지원)
    /// </summary>
    public class StageStateManager : BaseManager
    {
        [Header("Dependencies")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private StageManager stageManager;
        [SerializeField] private StageClearPanel stageClearPanel; // JML: 클리어 패널 (로비/다음 스테이지 선택)
        [SerializeField] private StageFailedPanel stageFailedPanel; // JML: 실패 패널 (로비/재시작 선택)
        [SerializeField] private StageEvents stageEvents;

        [Header("Wall References (동적 로드 - Tag로 찾음)")]
        [Tooltip("맵 프리팹에서 동적으로 로드됨 - Inspector 할당 불필요")]
        private Wall wall;       // Tag: Wall에서 GetComponent로 찾음
        private Wall wall2;      // Tag: Wall2에서 GetComponent로 찾음 (양방향 방어)
        private WallEvents wallEvents;   // Wall에서 GetComponent로 찾음
        private WallEvents wallEvents2;  // Wall2에서 GetComponent로 찾음

        public StageState CurrentState { get; private set; }

        protected override void OnInitialize()
        {
            CurrentState = StageState.Playing;

            // JML: Tag로 Wall 참조 찾기 (동적 맵 로드 지원)
            FindWallReferences();

            // LMJ: Subscribe to EventChannels instead of static events
            if (stageEvents != null)
            {
                stageEvents.AddAllMonstersDefeatedListener(HandleAllMonstersDefeated);
                stageEvents.AddBossDefeatedListener(HandleBossDefeated);
                stageEvents.AddTimeUpListener(HandleTimeUp);
            }

            if (wallEvents != null)
            {
                wallEvents.AddWallDestroyedListener(HandleWallDestroyed);
            }

            // JML: Wall 2 이벤트도 구독 (양방향 방어 - 하나라도 파괴되면 게임 오버)
            if (wallEvents2 != null)
            {
                wallEvents2.AddWallDestroyedListener(HandleWallDestroyed);
            }
        }

        /// <summary>
        /// JML: Tag로 Wall 참조 찾기 (동적 맵 로드 지원)
        /// StageManager에서 맵이 로드된 후 호출되어야 함
        /// </summary>
        private void FindWallReferences()
        {
            // Wall 1 찾기 (Tag: Wall)
            GameObject wallObj = GameObject.FindWithTag("Wall");
            if (wallObj != null)
            {
                wall = wallObj.GetComponent<Wall>();
                // WallEvents는 ScriptableObject이므로 Wall 컴포넌트에서 가져옴
                wallEvents = wall?.GetWallEvents();
                Debug.Log($"[StageStateManager] Found Wall: {wallObj.name}");
            }
            else
            {
                Debug.LogWarning("[StageStateManager] Wall not found by tag. Will retry when map is loaded.");
            }

            // Wall 2 찾기 (Tag: Wall2 - 양방향 방어)
            GameObject wall2Obj = GameObject.FindWithTag("Wall2");
            if (wall2Obj != null)
            {
                wall2 = wall2Obj.GetComponent<Wall>();
                // WallEvents는 ScriptableObject이므로 Wall 컴포넌트에서 가져옴
                wallEvents2 = wall2?.GetWallEvents();
                Debug.Log($"[StageStateManager] Found Wall2: {wall2Obj.name}");
            }
        }

        /// <summary>
        /// JML: 외부에서 Wall 참조 갱신 요청 시 호출 (맵 로드 완료 후)
        /// </summary>
        public void RefreshWallReferences()
        {
            // 기존 이벤트 구독 해제
            if (wallEvents != null)
            {
                wallEvents.RemoveWallDestroyedListener(HandleWallDestroyed);
            }
            if (wallEvents2 != null)
            {
                wallEvents2.RemoveWallDestroyedListener(HandleWallDestroyed);
            }

            // 새로운 참조 찾기
            FindWallReferences();

            // 새로운 이벤트 구독
            if (wallEvents != null)
            {
                wallEvents.AddWallDestroyedListener(HandleWallDestroyed);
            }
            if (wallEvents2 != null)
            {
                wallEvents2.AddWallDestroyedListener(HandleWallDestroyed);
            }

            Debug.Log("[StageStateManager] Wall references refreshed");
        }

        protected override void OnReset()
        {
            CurrentState = StageState.Playing;
            Time.timeScale = 1f;
        }

        protected override void OnDispose()
        {
            // LMJ: Unsubscribe from EventChannels
            if (stageEvents != null)
            {
                stageEvents.RemoveAllMonstersDefeatedListener(HandleAllMonstersDefeated);
                stageEvents.RemoveBossDefeatedListener(HandleBossDefeated);
                stageEvents.RemoveTimeUpListener(HandleTimeUp);
            }

            if (wallEvents != null)
            {
                wallEvents.RemoveWallDestroyedListener(HandleWallDestroyed);
            }

            // JML: Wall 2 이벤트 구독 해제
            if (wallEvents2 != null)
            {
                wallEvents2.RemoveWallDestroyedListener(HandleWallDestroyed);
            }
        }

        #region Victory Conditions

        private void HandleAllMonstersDefeated()
        {
            if (CurrentState != StageState.Playing) return;
            CheckVictoryCondition();
        }

        private void HandleBossDefeated()
        {
            if (CurrentState != StageState.Playing) return;
            CheckVictoryCondition();
        }
        private void CheckVictoryCondition()
        {
            if (!waveManager.HasRemainingEnemies() && !waveManager.HasBoss())
            {
                SetStageState(StageState.Cleared);
            }
        }
        #endregion

        #region Defeat Conditions

        private void HandleWallDestroyed()
        {
            if (CurrentState != StageState.Playing) return;
            SetStageState(StageState.Failed);
        }

        private void HandleTimeUp()
        {
            if (CurrentState != StageState.Playing) return;

            if (waveManager.HasRemainingEnemies() || waveManager.HasBoss())
            {
                SetStageState(StageState.Failed);
            }
            else
            {
                SetStageState(StageState.Cleared);
            }
        }

        #endregion

        public void SetStageState(StageState newState)
        {
            CurrentState = newState;
            Time.timeScale = 0f;

            // LMJ: Use EventChannel instead of static event
            if (stageEvents != null)
            {
                stageEvents.RaiseStageStateChanged(newState);
            }

            if (newState == StageState.Cleared)
            {
                Debug.Log("[StageStateManager] Stage Cleared!");

                // JML: 모든 캐릭터 승리 애니메이션 재생
                PlayAllCharactersVictoryAnimation();

                // JML: 스테이지 클리어 시 진행도 저장 (다음 스테이지 해금)
                if (SelectedStage.HasSelection)
                {
                    int clearedStageNumber = SelectedStage.Data.Chapter_Number;
                    StageProgressManager.Instance?.OnStageClear(clearedStageNumber);
                }

                // JML: StageClearPanel 사용 (로비/다음 스테이지 선택)
                if (stageClearPanel != null)
                {
                    // 진행 시간, 처치 몬스터 수, Wall HP 비율 전달
                    float progressTime = stageManager != null ? stageManager.GetProgressTime() : 0f;
                    int killCount = waveManager != null ? waveManager.GetKillCount() : 0;
                    // JML: 양방향 방어 시 두 Wall 중 낮은 HP 비율 사용
                    float wallHpRatio = GetLowestWallHpRatio();
                    stageClearPanel.Show(progressTime, killCount, wallHpRatio);
                }
                else
                {
                    Debug.LogError("[StageStateManager] StageClearPanel is null! Inspector에서 할당해주세요.");
                }
            }
            else if (newState == StageState.Failed)
            {
                Debug.Log("[StageStateManager] Stage Failed!");

                // JML: 모든 캐릭터 사망 애니메이션 재생
                PlayAllCharactersDieAnimation();

                // JML: StageFailedPanel 사용 (로비/재시작 선택 가능)
                if (stageFailedPanel != null)
                {
                    // 진행 시간 및 남은 몬스터 수 전달
                    float progressTime = stageManager != null ? stageManager.GetProgressTime() : 0f;
                    int remainingMonsters = waveManager != null ? waveManager.GetRemainderCount() : 0;
                    stageFailedPanel.Show(progressTime, remainingMonsters);
                }
                else
                {
                    Debug.LogError("[StageStateManager] StageFailedPanel is null! Inspector에서 할당해주세요.");
                }
            }
        }

        public StageState GetCurrentState()
        {
            return CurrentState;
        }

        /// <summary>
        /// JML: 가장 낮은 Wall HP 비율 반환 (양방향 방어 지원)
        /// </summary>
        private float GetLowestWallHpRatio()
        {
            float ratio1 = wall != null ? wall.GetHealth() / wall.GetMaxHealth() : 1f;

            // Wall 2가 활성화된 경우 (양방향 방어)
            if (wall2 != null && wall2.gameObject.activeInHierarchy)
            {
                float ratio2 = wall2.GetHealth() / wall2.GetMaxHealth();
                return Mathf.Min(ratio1, ratio2);
            }

            return ratio1;
        }

        #region Character Animation

        /// <summary>
        /// JML: 모든 캐릭터의 승리 애니메이션 재생
        /// </summary>
        private void PlayAllCharactersVictoryAnimation()
        {
            Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
            int count = characters.Length;
            for (int i = 0; i < count; i++)
            {
                characters[i].PlayVictoryAnimation();
            }
            Debug.Log($"[StageStateManager] {count}명의 캐릭터 승리 애니메이션 재생");
        }

        /// <summary>
        /// JML: 모든 캐릭터의 사망 애니메이션 재생
        /// </summary>
        private void PlayAllCharactersDieAnimation()
        {
            Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
            int count = characters.Length;
            for (int i = 0; i < count; i++)
            {
                characters[i].PlayDieAnimation();
            }
            Debug.Log($"[StageStateManager] {count}명의 캐릭터 사망 애니메이션 재생");
        }

        #endregion
    }
}