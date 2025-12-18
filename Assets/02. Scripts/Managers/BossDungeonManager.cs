using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.Spawners;
using NovelianMagicLibraryDefense.UI;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// Issue #476 - 도전던전 전용 매니저
    /// 보스 1마리 스폰, 제한시간 관리, 스턴 게이지 시스템
    /// </summary>
    public class BossDungeonManager : BaseManager
    {
        [Header("Dependencies")]
        [SerializeField] private ObjectPoolManager poolManager;
        [SerializeField] private MonsterEvents monsterEvents;
        [SerializeField] private StageEvents stageEvents;

        [Header("Spawner")]
        [SerializeField] private MonsterSpawner bossSpawner;

        [Header("Target")]
        [SerializeField] private Transform wallTarget;
        [SerializeField] private Wall wallComponent;
        [SerializeField] private Collider wallCollider;

        [Header("UI References")]
        [SerializeField] private BossDungeonUI dungeonUI;

        // 던전 데이터 (SelectedBossDungeon에서 가져옴)
        private BossDungeonData dungeonData;

        // 보스 관련
        private BossMonster currentBoss;
        private string bossAddressableKey;
        private bool isBossSpawned = false;

        // 타이머 관련
        private float remainingTime;
        private bool isTimerRunning = false;
        private System.Threading.CancellationTokenSource timerCts;

        // 스턴 게이지 관련
        private float currentStunGauge;
        private float maxStunGauge;
        private bool isBossStunned = false;

        // 결과
        public bool IsCleared { get; private set; } = false;
        public bool IsFailed { get; private set; } = false;

        protected override void OnInitialize()
        {
            // 이벤트 구독
            if (monsterEvents != null)
            {
                monsterEvents.AddBossDiedListener(HandleBossDied);
            }

            // SelectedBossDungeon에서 던전 데이터 가져오기
            if (SelectedBossDungeon.HasSelection)
            {
                dungeonData = SelectedBossDungeon.Data;
                InitializeDungeonData();
            }
            else
            {
                Debug.LogError("[BossDungeonManager] 선택된 던전 데이터가 없습니다!");
            }
        }

        /// <summary>
        /// 던전 데이터로 초기화
        /// </summary>
        private void InitializeDungeonData()
        {
            if (dungeonData == null) return;

            // 타이머 초기화
            remainingTime = dungeonData.Time_Limit;

            // 스턴 게이지 초기화
            maxStunGauge = dungeonData.Stun_Gauge;
            currentStunGauge = 0f;

            Debug.Log($"[BossDungeonManager] 던전 초기화: Floor={dungeonData.Floor_Index}, " +
                      $"Boss_ID={dungeonData.Boss_ID}, Time_Limit={dungeonData.Time_Limit}초, " +
                      $"Stun_Gauge={dungeonData.Stun_Gauge}");
        }

        protected override void OnReset()
        {
            StopTimer();
            isBossSpawned = false;
            IsCleared = false;
            IsFailed = false;
            currentStunGauge = 0f;
            isBossStunned = false;
        }

        protected override void OnDispose()
        {
            if (monsterEvents != null)
            {
                monsterEvents.RemoveBossDiedListener(HandleBossDied);
            }

            StopTimer();
            SelectedBossDungeon.Clear();
        }

        /// <summary>
        /// 던전 시작 (카드 선택 완료 후 호출)
        /// </summary>
        public async UniTask StartDungeonAsync()
        {
            if (dungeonData == null)
            {
                Debug.LogError("[BossDungeonManager] 던전 데이터가 없어서 시작할 수 없습니다!");
                return;
            }

            // 보스 스폰
            await SpawnBossAsync();

            // 타이머 시작
            StartTimer();

            Debug.Log("[BossDungeonManager] 도전던전 시작!");
        }

        /// <summary>
        /// 보스 스폰
        /// </summary>
        private async UniTask SpawnBossAsync()
        {
            if (isBossSpawned) return;

            // Boss_ID로 MonsterData 조회 → Addressable Key 가져오기
            bossAddressableKey = AddressableKey.GetMonsterAddressableKey(dungeonData.Boss_ID);
            if (string.IsNullOrEmpty(bossAddressableKey))
            {
                Debug.LogError($"[BossDungeonManager] Boss_ID {dungeonData.Boss_ID}의 Addressable Key를 찾을 수 없습니다!");
                return;
            }

            // 풀 생성 (없으면)
            if (!poolManager.HasPoolByKey(bossAddressableKey))
            {
                bool success = await poolManager.CreatePoolByKeyAsync<BossMonster>(bossAddressableKey, defaultCapacity: 1, maxSize: 3);
                if (!success)
                {
                    Debug.LogError($"[BossDungeonManager] 보스 풀 생성 실패: {bossAddressableKey}");
                    return;
                }
            }

            // 스폰 위치
            Vector3 spawnPos = bossSpawner != null
                ? bossSpawner.GetRandomSpawnPosition()
                : Vector3.zero;

            // 보스 스폰
            currentBoss = poolManager.SpawnByKey<BossMonster>(bossAddressableKey, spawnPos);

            if (currentBoss != null)
            {
                // MonsterLevelData 가져와서 초기화
                MonsterLevelData levelData = CSVLoader.Instance.GetData<MonsterLevelData>(dungeonData.Boss_Level_ID);
                currentBoss.Initialize(levelData, dungeonData.Boss_ID, monsterEvents);

                // 목적지 설정
                if (wallTarget != null)
                {
                    currentBoss.SetDestination(wallTarget.position);
                }

                // 도전던전 전용 보스 설정
                SetupBossForDungeon(currentBoss);

                isBossSpawned = true;
                Debug.Log($"[BossDungeonManager] 보스 스폰 완료: {bossAddressableKey}");
            }
        }

        /// <summary>
        /// 도전던전 전용 보스 설정 (스턴 게이지, 공격 패턴 등)
        /// </summary>
        private void SetupBossForDungeon(BossMonster boss)
        {
            // TODO: 보스에 도전던전 전용 설정 적용
            // - Attack_Count: 보스가 벽 공격 시 카운트 (0이면 즉시 실패)
            // - Attack_Period: 공격 주기
            // - Stun_Damage: 캐릭터 공격 시 스턴 게이지 증가량
            // - Stun_Duration: 스턴 지속시간
        }

        #region Timer

        /// <summary>
        /// 타이머 시작
        /// </summary>
        private void StartTimer()
        {
            StopTimer();

            timerCts = new System.Threading.CancellationTokenSource();
            isTimerRunning = true;
            TimerLoopAsync(timerCts.Token).Forget();
        }

        /// <summary>
        /// 타이머 중지
        /// </summary>
        private void StopTimer()
        {
            isTimerRunning = false;
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;
        }

        /// <summary>
        /// 타이머 루프
        /// </summary>
        private async UniTaskVoid TimerLoopAsync(System.Threading.CancellationToken token)
        {
            while (isTimerRunning && remainingTime > 0 && !token.IsCancellationRequested)
            {
                await UniTask.Yield(token);

                remainingTime -= Time.deltaTime;

                // UI 업데이트
                if (dungeonUI != null)
                {
                    dungeonUI.UpdateTimer(remainingTime);
                }

                // 시간 경고 (30초, 10초)
                CheckTimeWarning();
            }

            // 시간 초과 → 실패
            if (remainingTime <= 0 && !IsCleared && !IsFailed)
            {
                OnDungeonFailed("시간 초과!");
            }
        }

        /// <summary>
        /// 시간 경고 체크
        /// </summary>
        private void CheckTimeWarning()
        {
            if (dungeonUI == null) return;

            if (remainingTime <= 10f && remainingTime > 9.9f)
            {
                dungeonUI.ShowTimeWarning(true); // 긴급 경고
            }
            else if (remainingTime <= 30f && remainingTime > 29.9f)
            {
                dungeonUI.ShowTimeWarning(false); // 일반 경고
            }
        }

        #endregion

        #region Stun Gauge

        /// <summary>
        /// 스턴 게이지 증가 (캐릭터가 보스 공격 시 호출)
        /// </summary>
        public void AddStunGauge(float amount)
        {
            if (isBossStunned || currentBoss == null) return;

            currentStunGauge += amount;

            // UI 업데이트
            if (dungeonUI != null)
            {
                dungeonUI.UpdateStunGauge(currentStunGauge, maxStunGauge);
            }

            // 스턴 게이지 꽉 참 → 보스 스턴
            if (currentStunGauge >= maxStunGauge)
            {
                ApplyBossStun();
            }
        }

        /// <summary>
        /// 보스 스턴 적용
        /// </summary>
        private void ApplyBossStun()
        {
            if (currentBoss == null || isBossStunned) return;

            isBossStunned = true;
            currentStunGauge = 0f;

            // TODO: 보스에 스턴 상태 적용
            // currentBoss.ApplyStun(dungeonData.Stun_Duration);

            // UI 업데이트
            if (dungeonUI != null)
            {
                dungeonUI.ShowBossStunEffect(dungeonData.Stun_Duration);
                dungeonUI.UpdateStunGauge(0f, maxStunGauge);
            }

            // 스턴 해제 예약
            ReleaseStunAfterDelayAsync(dungeonData.Stun_Duration).Forget();

            Debug.Log($"[BossDungeonManager] 보스 스턴! 지속시간: {dungeonData.Stun_Duration}초");
        }

        /// <summary>
        /// 스턴 해제
        /// </summary>
        private async UniTaskVoid ReleaseStunAfterDelayAsync(float duration)
        {
            await UniTask.Delay((int)(duration * 1000));

            isBossStunned = false;

            // TODO: 보스 스턴 해제
            // currentBoss?.ReleaseStun();

            Debug.Log("[BossDungeonManager] 보스 스턴 해제");
        }

        #endregion

        #region Result

        /// <summary>
        /// 보스 처치 시 호출
        /// </summary>
        private void HandleBossDied(BossMonster boss)
        {
            if (boss != currentBoss) return;

            OnDungeonCleared();
        }

        /// <summary>
        /// 던전 클리어
        /// </summary>
        private void OnDungeonCleared()
        {
            if (IsCleared || IsFailed) return;

            IsCleared = true;
            StopTimer();

            Debug.Log($"[BossDungeonManager] 던전 클리어! 남은 시간: {remainingTime:F1}초");

            // UI에 클리어 결과 표시
            if (dungeonUI != null)
            {
                dungeonUI.ShowClearResult(remainingTime);
            }

            // 스테이지 이벤트 발생
            if (stageEvents != null)
            {
                stageEvents.RaiseBossDefeated();
            }
        }

        /// <summary>
        /// 던전 실패
        /// </summary>
        public void OnDungeonFailed(string reason)
        {
            if (IsCleared || IsFailed) return;

            IsFailed = true;
            StopTimer();

            Debug.Log($"[BossDungeonManager] 던전 실패: {reason}");

            // UI에 실패 결과 표시
            if (dungeonUI != null)
            {
                dungeonUI.ShowFailResult(reason);
            }
        }

        /// <summary>
        /// 벽이 파괴되었을 때 호출 (Attack_Count 체크)
        /// </summary>
        public void OnWallAttacked()
        {
            // Attack_Count가 0이면 즉시 실패
            if (dungeonData != null && dungeonData.Attack_Count == 0)
            {
                OnDungeonFailed("벽이 공격당했습니다!");
            }
            // TODO: Attack_Count > 0이면 카운트 감소 로직
        }

        #endregion

        #region Public Getters

        public float GetRemainingTime() => remainingTime;
        public float GetStunGaugePercent() => maxStunGauge > 0 ? currentStunGauge / maxStunGauge : 0f;
        public bool IsBossStunned() => isBossStunned;
        public BossDungeonData GetDungeonData() => dungeonData;

        #endregion

        protected override void OnDestroy()
        {
            StopTimer();
            base.OnDestroy();
        }
    }
}
