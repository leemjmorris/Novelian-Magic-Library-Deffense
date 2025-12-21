//LMJ : Character with simple projectile-based combat (Issue #265)
//     Migrated to new CSV-based skill system
//     Refactored to partial classes for better maintainability
namespace Novelian.Combat
{
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;
    using NovelianMagicLibraryDefense.Managers;

    /// <summary>
    /// 캐릭터 메인 클래스 (코어)
    /// - MonoBehaviour 필드 및 생명주기 관리
    /// - 공격 루프 및 스킬 실행 조율
    /// - IPoolable 구현
    ///
    /// Partial Classes:
    /// - Character.Stats.cs: 스탯/버프/성급/책갈피
    /// - Character.SkillData.cs: 스킬 데이터 로딩/계산
    /// - Character.SkillExecutor.cs: 스킬 실행 (투사체/AOE/채널링/버프/트랩)
    /// - Character.Targeting.cs: 타겟 선정 로직
    /// - Character.StatusEffects.cs: 상태이상 적용
    /// - Character.Visuals.cs: 비주얼 파츠 관리
    /// - Character.Animation.cs: 애니메이션 제어
    /// </summary>
    public partial class Character : MonoBehaviour, IPoolable
    {
        #region Serialized Fields

        [Header("Character Visual")]
        [SerializeField] private GameObject characterObj;

        [Header("Character Animator")]
        [SerializeField] private Animator characterAnimator;

        [Header("스킬 장착 (Skill Equipment) - CSV ID 기반")]
        [SerializeField, Tooltip("기본 공격 스킬 ID (MainSkillTable)")]
        private int basicAttackSkillId = 39001;

        [SerializeField, Tooltip("액티브 스킬 ID (MainSkillTable)")]
        private int activeSkillId = 0;

        [SerializeField, Tooltip("보조 스킬 ID (SupportSkillTable)")]
        private int supportSkillId = 0;

        [Header("캐릭터 스텟 변형 (%) (Character Stat Modifiers)")]
        [SerializeField, Tooltip("데미지 변형 (%)")]
        private float damageModifier = 0f;

        [SerializeField, Tooltip("공격 속도 변형 (%)")]
        private float attackSpeedModifier = 0f;

        [SerializeField, Tooltip("투사체 속도 변형 (%)")]
        private float projectileSpeedModifier = 0f;

        [SerializeField, Tooltip("사거리 변형 (%)")]
        private float rangeModifier = 0f;

        [SerializeField, Tooltip("치명타 확률 변형 (%)")]
        private float critChanceModifier = 0f;

        [SerializeField, Tooltip("치명타 배율 변형 (%)")]
        private float critMultiplierModifier = 0f;

        [SerializeField, Tooltip("추가 데미지 변형 (%)")]
        private float bonusDamageModifier = 0f;

        [SerializeField, Tooltip("체력 회복 변형 (%)")]
        private float healthRegenModifier = 0f;

        [SerializeField, Tooltip("쿨타임 감소 (%)")]
        private float cooldownModifier = 0f;

        [SerializeField, Tooltip("시전 속도 감소 (%)")]
        private float castTimeModifier = 0f;

        [SerializeField, Tooltip("보스 데미지 증가 (%)")]
        private float bossDamageModifier = 0f;

        [Header("Spawn Position")]
        [SerializeField, Tooltip("Projectile spawn offset (Y=1.5 for chest height)")]
        private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Projectile Template")]
        [SerializeField, Tooltip("Generic projectile template (used when skill has no projectile prefab)")]
        private GameObject projectileTemplate;

        [Header("Targeting Strategy")]
        [SerializeField, Tooltip("Use weight-based targeting (default: distance-based)")]
        private bool useWeightTargeting = false;

        #endregion

        #region Debug / Gizmo Settings

        [Header("Debug - AOE Gizmo (런타임 범위 표시)")]
        [SerializeField, Tooltip("AOE 스킬 범위를 Gizmo로 표시")]
        private bool showAOEGizmo = true;

        [SerializeField, Tooltip("AOE Gizmo 색상")]
        private Color aoeGizmoColor = new Color(1f, 0.5f, 0f, 0.3f); // 주황색 반투명

        [SerializeField, Tooltip("AOE Gizmo 와이어 색상")]
        private Color aoeGizmoWireColor = new Color(1f, 0.3f, 0f, 1f); // 주황색 불투명

        // 런타임 AOE 정보 (UseAOESkillAsync에서 업데이트)
        private Vector3 lastAOETargetPosition;
        private float lastAOERadius;
        private float aoeGizmoDisplayTime;
        private const float AOE_GIZMO_DURATION = 2f; // Gizmo 표시 지속 시간

        #endregion

        #region Private Fields

        // 캐싱된 스킬 데이터
        private MainSkillData basicAttackData;
        private MainSkillData activeSkillData;
        private SupportSkillData supportData;
        private MainSkillPrefabEntry basicAttackPrefabs;
        private MainSkillPrefabEntry activeSkillPrefabs;

        // 스킬 레벨 데이터 (현재는 레벨 1 고정, 추후 레벨 시스템 추가 시 확장)
#pragma warning disable CS0414 // 추후 레벨 시스템 구현 시 사용 예정
        private int currentSkillLevel = 1;
#pragma warning restore CS0414

        // Attack state
        private CancellationTokenSource attackCts;
        private CancellationTokenSource activeSkillCts;
        private CancellationTokenSource channelingCts;
        private bool isInitialized = false;
        private bool isChanneling = false;

        // Issue #476: 도전던전 스턴 상태
        private bool isStunnedByBossDungeon = false;

        // JML: 책갈피 시스템 (Issue #320)
        private int characterId = -1;
        private bool isManuallyInitialized = false;  // Initialize()로 초기화되었는지 여부
        private bool autoAttackEnabled = true;  // 자동 공격 활성화 여부 (테스트 씬에서 false로 설정)

        // JML: 비주얼 파츠 캐시 (Issue #356)
        private Dictionary<string, Transform> cachedTransforms = new Dictionary<string, Transform>();
        private Transform weaponRightSlot;
        private Transform weaponLeftSlot;

        // 속성(장르) 강화 modifier (GenreType별 누적)
        private Dictionary<GenreType, float> genreModifiers = new Dictionary<GenreType, float>();

        #endregion

        #region Lifecycle

        private void Start()
        {
            // Initialize()로 이미 초기화되었으면 스킵
            if (isManuallyInitialized) return;

            // 기존 방식 (프리팹 Inspector 값 사용) - 하위 호환성
            ApplyBookmarksIfAvailable();
            LoadSkillData();
            InitializeProjectilePool();
            InitializeActiveSkillPool();

            // 자동 공격이 활성화된 경우에만 공격 루프 시작
            if (autoAttackEnabled)
            {
                StartAttackLoop();
                StartActiveSkillLoop();
            }

            isInitialized = true;
        }

        /// <summary>
        /// JML: CSV 데이터 기반 초기화 (Issue #320)
        /// CharacterPlacementManager에서 호출
        /// </summary>
        public void Initialize(int csvCharacterId)
        {
            characterId = csvCharacterId;
            isManuallyInitialized = true;

            Debug.Log($"[Character] Initialize 시작 (CharacterID: {csvCharacterId})");

            // 0. 비주얼 파츠 적용 (Issue #356)
            ApplyVisualConfig(csvCharacterId);

            // 1. CSV에서 캐릭터 데이터 로드
            var characterData = CSVLoader.Instance?.GetData<CharacterData>(csvCharacterId);
            if (characterData != null)
            {
                // Base_Skill_ID를 기본 공격 스킬로 설정
                basicAttackSkillId = characterData.Base_Skill_ID;
                Debug.Log($"[Character] CSV에서 Base_Skill_ID 로드: {basicAttackSkillId}");
            }
            else
            {
                Debug.LogWarning($"[Character] CharacterData를 찾을 수 없음 (ID: {csvCharacterId}). 기본값 사용.");
            }

            // 2. 책갈피 적용 (스탯 + 스킬)
            ApplyBookmarksIfAvailable();

            // 3. 스킬 데이터 로드 및 초기화
            LoadSkillData();
            InitializeProjectilePool();
            InitializeActiveSkillPool();
            StartAttackLoop();
            StartActiveSkillLoop();

            isInitialized = true;
            Debug.Log($"[Character] Initialize 완료 (CharacterID: {csvCharacterId}, BasicSkill: {basicAttackSkillId})");
        }

        private void OnDestroy()
        {
            attackCts?.Cancel();
            attackCts?.Dispose();
            activeSkillCts?.Cancel();
            activeSkillCts?.Dispose();
            channelingCts?.Cancel();
            channelingCts?.Dispose();
        }

        #endregion

        #region Attack Loop

        //LMJ : Start attack loop
        private void StartAttackLoop()
        {
            attackCts?.Cancel();
            attackCts?.Dispose();
            attackCts = new CancellationTokenSource();
            AttackLoopAsync(attackCts.Token).Forget();
        }

        //LMJ : Main attack loop with UniTask (using skill-based attack speed)
        private async UniTaskVoid AttackLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // Wait for attack interval (using final attack speed from skill + character modifier)
                float interval = 1f / FinalAttackSpeed;
                await UniTask.Delay((int)(interval * 1000), cancellationToken: ct);

                // Pause support (skip attack when Time.timeScale = 0)
                if (Time.timeScale == 0f) continue;

                TryAttack();
            }
        }

        //LMJ : Start active skill loop
        private void StartActiveSkillLoop()
        {
            // activeSkillId가 0이면 액티브 스킬이 없는 캐릭터 (정상 케이스)
            if (activeSkillData == null)
            {
                // 액티브 스킬이 없는 캐릭터는 경고 없이 스킵
                return;
            }

            activeSkillCts?.Cancel();
            activeSkillCts?.Dispose();
            activeSkillCts = new CancellationTokenSource();
            ActiveSkillLoopAsync(activeSkillCts.Token).Forget();
        }

        //LMJ : Active skill loop with UniTask (independent cooldown)
        private async UniTaskVoid ActiveSkillLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // Wait for active skill interval (using final attack speed from active skill + character modifier)
                float interval = 1f / FinalActiveAttackSpeed;
                await UniTask.Delay((int)(interval * 1000), cancellationToken: ct);

                // Pause support (skip attack when Time.timeScale = 0)
                if (Time.timeScale == 0f) continue;

                TryUseActiveSkill();
            }
        }

        //LMJ : Attempt to attack nearest or highest weight target (skill-based)
        //      Now supports all skill types (same as ForceAttack)
        private void TryAttack()
        {
            // Issue #476: 도전던전 스턴 상태면 공격 안함
            if (isStunnedByBossDungeon) return;

            // 디버그: 초기화 상태 확인
            if (!isInitialized)
            {
                Debug.LogWarning("[Character] TryAttack skipped: not initialized");
                return;
            }
            if (basicAttackData == null)
            {
                Debug.LogWarning("[Character] TryAttack skipped: basicAttackData is null");
                return;
            }

            // Check skill type and call appropriate method
            var skillType = basicAttackData.GetSkillType();

            // 버프 스킬은 타겟이 필요 없음 (자기/아군 대상)
            if (skillType == SkillAssetType.Buff)
            {
                UseBuffSkillAsync(basicAttackData, basicAttackPrefabs).Forget();
                return;
            }

            // 타겟 탐색 범위 결정: range가 0이면 aoe_radius 사용 (관중의야유 등 전역 디버프)
            float searchRange = FinalRange;
            if (searchRange <= 0 && basicAttackData.aoe_radius > 0)
            {
                searchRange = basicAttackData.aoe_radius;
            }
            // 그래도 0이면 기본값 사용
            if (searchRange <= 0) searchRange = 100f;

            // Find target with mark priority, then use weight/distance strategy
            ITargetable target = TargetRegistry.Instance.FindTarget(transform.position, searchRange, useWeightTargeting);

            if (target == null)
            {
                // 타겟이 없으면 스포너 방향을 바라봄
                LookAtNearestSpawner();
                return;
            }

            // JML: 타겟 방향을 바라봄
            LookAtTarget(target);

            // JML: 공격 애니메이션 재생
            PlayAttackAnimation();

            // 스킬 타입별 분기 처리
            ExecuteSkillByType(skillType, target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed, FinalProjectileLifetime, isActiveSkill: false);
        }

        /// <summary>
        /// JML: 타겟 방향으로 캐릭터 회전
        /// </summary>
        private void LookAtTarget(ITargetable target)
        {
            if (target == null) return;

            Vector3 targetPos = target.GetPosition();
            Vector3 direction = targetPos - transform.position;
            direction.y = 0; // Y축 무시 (수평 회전만)

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        /// <summary>
        /// JML: 가장 가까운 스포너 방향으로 캐릭터 회전
        /// </summary>
        private void LookAtNearestSpawner()
        {
            GameObject spawner1 = GameObject.FindWithTag("SpawnArea1");
            GameObject spawner2 = GameObject.FindWithTag("SpawnArea2");

            Vector3? targetDir = null;

            if (spawner1 != null && spawner2 != null)
            {
                float dist1 = Vector3.Distance(transform.position, spawner1.transform.position);
                float dist2 = Vector3.Distance(transform.position, spawner2.transform.position);

                targetDir = dist1 <= dist2
                    ? spawner1.transform.position - transform.position
                    : spawner2.transform.position - transform.position;
            }
            else if (spawner1 != null)
            {
                targetDir = spawner1.transform.position - transform.position;
            }
            else if (spawner2 != null)
            {
                targetDir = spawner2.transform.position - transform.position;
            }

            if (targetDir.HasValue && targetDir.Value.sqrMagnitude > 0.01f)
            {
                Vector3 dir = targetDir.Value;
                dir.y = 0;
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        //LMJ : Attempt to use active skill on target
        private void TryUseActiveSkill()
        {
            // Issue #476: 도전던전 스턴 상태면 공격 안함
            if (isStunnedByBossDungeon) return;

            if (!isInitialized || activeSkillData == null) return;

            // Skip if already channeling
            if (isChanneling) return;

            // Check skill type
            var skillType = activeSkillData.GetSkillType();

            // 버프 스킬은 타겟이 필요 없음 (자기/아군 대상)
            if (skillType == SkillAssetType.Buff)
            {
                UseBuffSkillAsync(activeSkillData, activeSkillPrefabs).Forget();
                return;
            }

            // 타겟 탐색 범위 결정: range가 0이면 aoe_radius 사용
            float searchRange = FinalActiveRange;
            if (searchRange <= 0 && activeSkillData.aoe_radius > 0)
            {
                searchRange = activeSkillData.aoe_radius;
            }
            if (searchRange <= 0) searchRange = 100f;

            // Find target with mark priority, then use weight/distance strategy
            ITargetable target = TargetRegistry.Instance.FindTarget(transform.position, searchRange, useWeightTargeting);

            if (target == null) return;

            // JML: 타겟 방향을 바라봄
            LookAtTarget(target);

            // JML: 공격 애니메이션 재생 (액티브 스킬도 동일)
            PlayAttackAnimation();

            // 스킬 타입별 분기 처리
            ExecuteSkillByType(skillType, target, activeSkillData, activeSkillPrefabs, FinalActiveDamage, FinalActiveRange, FinalActiveProjectileSpeed, FinalActiveProjectileLifetime, isActiveSkill: true);
        }

        /// <summary>
        /// 스킬 타입별 실행 분기 (TryAttack, TryUseActiveSkill, ForceAttack 공통)
        /// </summary>
        private void ExecuteSkillByType(SkillAssetType skillType, ITargetable target, MainSkillData skillData, MainSkillPrefabEntry prefabs, float damage, float range, float projectileSpeed, float lifetime, bool isActiveSkill)
        {
            switch (skillType)
            {
                // 투사체 스킬 - 투사체 발사
                case SkillAssetType.Projectile:
                    if (isActiveSkill)
                    {
                        // 액티브 스킬 특수 처리 (다이너마이트, 전설의 지팡이 등)
                        if (skillData.IsDynamiteSkill)
                            LaunchActiveProjectile(target, isDynamite: true);
                        else if (skillData.IsLegendaryStaffSkill)
                            LaunchActiveProjectile(target, isLegendaryStaff: true);
                        else if (skillData.IsTimeBombSkill)
                            LaunchActiveProjectile(target, isTimeBomb: true);
                        else if (skillData.IsBoomerangSkill)
                            LaunchActiveProjectile(target, isBoomerang: true);
                        else
                            LaunchActiveProjectile(target);
                    }
                    else
                    {
                        LaunchProjectile(target);
                    }
                    break;

                // 단일 즉발 스킬 - 타겟에게 즉시 데미지/효과
                case SkillAssetType.InstantSingle:
                    // 심장마비: 체력 10% 이하 적 즉사 (보스 제외)
                    if (skillData.IsInstantKillSkill)
                    {
                        if (isActiveSkill)
                            UseActiveInstantKillSkill(target);
                        else
                            UseInstantKillSkill(target);
                    }
                    else
                    {
                        UseAOESkillAsync(target, skillData, prefabs, damage, range, projectileSpeed).Forget();
                    }
                    break;

                // 범위 스킬 - 타겟 위치에 AOE 효과
                case SkillAssetType.AOE:
                    // 다이너마이트: 투사체를 던져서 N초 후 폭발 (특수 처리)
                    if (skillData.IsDynamiteSkill)
                    {
                        if (isActiveSkill)
                            LaunchActiveProjectile(target, isDynamite: true);
                        else
                            LaunchDynamiteProjectile(target);
                    }
                    // 전설의 지팡이: 투사체가 일직선으로 날아가며 경로상 AOE 데미지 (특수 처리)
                    else if (skillData.IsLegendaryStaffSkill)
                    {
                        if (isActiveSkill)
                            LaunchActiveProjectile(target, isLegendaryStaff: true);
                        else
                            LaunchLegendaryStaffProjectile(target);
                    }
                    else
                    {
                        UseAOESkillAsync(target, skillData, prefabs, damage, range, projectileSpeed).Forget();
                    }
                    break;

                // DOT 스킬 - 범위 내 적에게 지속 데미지 (AOE 방식)
                case SkillAssetType.DOT:
                    UseAOESkillAsync(target, skillData, prefabs, damage, range, projectileSpeed).Forget();
                    break;

                // 디버프 스킬 - 범위 내 적에게 디버프 적용 (AOE 방식)
                case SkillAssetType.Debuff:
                    UseAOESkillAsync(target, skillData, prefabs, damage, range, projectileSpeed).Forget();
                    break;

                // 채널링 스킬 - 지속 시전
                case SkillAssetType.Channeling:
                    UseChannelingSkillAsync(target, skillData, prefabs, damage).Forget();
                    break;

                // 트랩 스킬 - 필드에 트랩 오브젝트 설치
                case SkillAssetType.Trap:
                    PlaceTrapObject(target, skillData, prefabs, damage);
                    break;

                // 지뢰 스킬 - 필드에 지뢰 오브젝트 설치
                case SkillAssetType.Mine:
                    PlaceMineObject(target, skillData, prefabs, damage);
                    break;

                default:
                    Debug.LogWarning($"[Character] Unknown skill type: {skillType}, falling back to projectile");
                    if (isActiveSkill)
                        LaunchActiveProjectile(target);
                    else
                        LaunchProjectile(target);
                    break;
            }
        }

        #endregion

        #region IPoolable Implementation

        public void OnSpawn()
        {
            characterObj.SetActive(true);

            if (!isInitialized)
            {
                Start();
            }
            else
            {
                StartAttackLoop();
                StartActiveSkillLoop();
            }

            Debug.Log("[Character] Character spawned and ready");
        }

        public void OnDespawn()
        {
            characterObj.SetActive(false);
            attackCts?.Cancel();
            attackCts?.Dispose();
            attackCts = null;
            activeSkillCts?.Cancel();
            activeSkillCts?.Dispose();
            activeSkillCts = null;
            channelingCts?.Cancel();
            channelingCts?.Dispose();
            channelingCts = null;
            Debug.Log("[Character] Character despawned");
        }

        #endregion

        #region Public API (Test Methods)

        /// <summary>
        /// 테스트용: 수동으로 공격 발사 (SkillTestManager에서 호출)
        /// 스킬 타입에 따라 적절한 메서드를 호출
        /// </summary>
        public void ForceAttack()
        {
            if (!isInitialized || basicAttackData == null)
            {
                Debug.LogWarning("[Character] ForceAttack skipped: not initialized or no skill data");
                return;
            }

            // Check skill type and call appropriate method
            var skillType = basicAttackData.GetSkillType();
            Debug.Log($"[Character] ForceAttack: {basicAttackData.skill_name} (Type: {skillType})");

            // 버프 스킬은 타겟이 필요 없음 (자기/아군 대상)
            if (skillType == SkillAssetType.Buff)
            {
                UseBuffSkillAsync(basicAttackData, basicAttackPrefabs).Forget();
                return;
            }

            // 타겟 탐색 범위 결정: range가 0이면 aoe_radius 사용 (관중의야유 등 전역 디버프)
            float searchRange = FinalRange;
            if (searchRange <= 0 && basicAttackData.aoe_radius > 0)
            {
                searchRange = basicAttackData.aoe_radius;
            }
            // 그래도 0이면 기본값 사용
            if (searchRange <= 0) searchRange = 100f;

            // Find target (버프 외 스킬은 타겟 필요)
            ITargetable target = TargetRegistry.Instance.FindTarget(transform.position, searchRange, useWeightTargeting);
            if (target == null)
            {
                Debug.LogWarning($"[Character] ForceAttack skipped: no target found (searchRange={searchRange})");
                return;
            }

            ExecuteSkillByType(skillType, target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed, FinalProjectileLifetime, isActiveSkill: false);
        }

        /// <summary>
        /// 테스트용: 자동 공격 루프 활성화/비활성화 (SkillTestManager에서 사용)
        /// Start() 전에 호출하면 자동 공격 시작을 방지, 후에 호출하면 루프 중지/재시작
        /// </summary>
        public void SetAutoAttackEnabled(bool enabled)
        {
            autoAttackEnabled = enabled;

            if (!enabled)
            {
                // 자동 공격 루프 중지 (이미 시작된 경우)
                attackCts?.Cancel();
                attackCts?.Dispose();
                attackCts = null;

                activeSkillCts?.Cancel();
                activeSkillCts?.Dispose();
                activeSkillCts = null;

                Debug.Log("[Character] 자동 공격 비활성화");
            }
            else if (isInitialized)
            {
                // 이미 초기화된 후에 활성화하면 루프 재시작
                StartAttackLoop();
                StartActiveSkillLoop();
                Debug.Log("[Character] 자동 공격 활성화");
            }
        }

        //LMJ : Set spawn offset from CharacterPreset (future feature)
        public void SetSpawnOffsetFromPreset(Vector3 offset)
        {
            spawnOffset = offset;
        }

        //LMJ : Set targeting strategy at runtime
        public void SetTargetingStrategy(bool useWeight)
        {
            useWeightTargeting = useWeight;
        }

        //LMJ : Set skill IDs at runtime
        public void SetSkillIds(int basicAttackId, int activeId = 0, int supportId = 0)
        {
            basicAttackSkillId = basicAttackId;
            activeSkillId = activeId;
            supportSkillId = supportId;
            LoadSkillData();
        }

        /// <summary>
        /// JML: 서포트 스킬 장착 (Issue #424)
        /// 호환성 검증 후 장착, 성공 여부 반환
        /// </summary>
        /// <param name="supportId">SupportSkillTable의 support_id</param>
        /// <returns>장착 성공 여부</returns>
        public bool EquipSupportSkill(int supportId)
        {
            if (supportId <= 0)
            {
                Debug.LogWarning("[Character] Invalid support skill ID");
                return false;
            }

            // 호환성 검증
            var newSupportData = CSVLoader.Instance?.GetData<SupportSkillData>(supportId);
            if (newSupportData == null)
            {
                Debug.LogWarning($"[Character] SupportSkillData not found for ID: {supportId}");
                return false;
            }

            var compatibilityTable = CSVLoader.Instance?.GetTable<SupportCompatibilityData>();
            if (compatibilityTable != null)
            {
                var compatibility = compatibilityTable.GetId(supportId);
                if (compatibility != null && basicAttackData != null)
                {
                    if (!compatibility.IsCompatibleWith(basicAttackData.GetSkillType()))
                    {
                        Debug.LogWarning($"[Character] 서포트 스킬 '{newSupportData.support_name}'은(는) 메인 스킬 '{basicAttackData.skill_name}'과 호환되지 않습니다!");
                        return false;
                    }
                }
            }

            // 장착
            supportSkillId = supportId;
            supportData = newSupportData;
            // Support prefabs are no longer needed - effect modifiers come from CSV data

            Debug.Log($"[Character] 서포트 스킬 장착 완료: {newSupportData.support_name} (ID: {supportId})");
            return true;
        }

        /// <summary>
        /// JML: 현재 기본 공격 스킬 ID 반환 (Issue #424)
        /// </summary>
        public int GetBasicAttackSkillId() => basicAttackSkillId;

        /// <summary>
        /// JML: 현재 서포트 스킬 ID 반환 (Issue #424)
        /// </summary>
        public int GetSupportSkillId() => supportSkillId;

        /// <summary>
        /// JML: 서포트 스킬 장착 여부 (Issue #424)
        /// </summary>
        public bool HasSupportSkill() => supportSkillId > 0 && supportData != null;

        /// <summary>
        /// 투사체 템플릿 설정 (TrainingScene 등 Pool 없는 환경용)
        /// </summary>
        public void SetProjectileTemplate(GameObject template)
        {
            projectileTemplate = template;
            Debug.Log($"[Character] ProjectileTemplate 설정됨: {(template != null ? template.name : "null")}");
        }

        #endregion

        #region AOE Gizmo (Runtime Debug)

        /// <summary>
        /// AOE 스킬 사용 시 Gizmo 정보 업데이트 (UseAOESkillAsync에서 호출)
        /// </summary>
        public void UpdateAOEGizmoInfo(Vector3 targetPosition, float radius)
        {
            lastAOETargetPosition = targetPosition;
            lastAOERadius = radius;
            aoeGizmoDisplayTime = AOE_GIZMO_DURATION;
        }

        private void Update()
        {
            // AOE Gizmo 표시 시간 감소
            if (aoeGizmoDisplayTime > 0)
            {
                aoeGizmoDisplayTime -= Time.deltaTime;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // AOE Gizmo 표시가 꺼져있거나 표시 시간이 지났으면 스킵
            if (!showAOEGizmo || aoeGizmoDisplayTime <= 0 || lastAOERadius <= 0)
                return;

            // 페이드 아웃 효과
            float alpha = Mathf.Clamp01(aoeGizmoDisplayTime / AOE_GIZMO_DURATION);

            // 바닥에 원형 범위 표시
            Vector3 gizmoPos = lastAOETargetPosition;
            gizmoPos.y = 0.1f; // 바닥에서 살짝 위

            // 채워진 원 (반투명)
            Color fillColor = aoeGizmoColor;
            fillColor.a *= alpha;
            Gizmos.color = fillColor;

            // UnityEditor.Handles를 사용하여 원 그리기
            UnityEditor.Handles.color = fillColor;
            UnityEditor.Handles.DrawSolidDisc(gizmoPos, Vector3.up, lastAOERadius);

            // 와이어 원 (불투명)
            Color wireColor = aoeGizmoWireColor;
            wireColor.a *= alpha;
            UnityEditor.Handles.color = wireColor;
            UnityEditor.Handles.DrawWireDisc(gizmoPos, Vector3.up, lastAOERadius);

            // 중앙 십자 표시
            float crossSize = lastAOERadius * 0.1f;
            Gizmos.color = wireColor;
            Gizmos.DrawLine(gizmoPos + Vector3.left * crossSize, gizmoPos + Vector3.right * crossSize);
            Gizmos.DrawLine(gizmoPos + Vector3.forward * crossSize, gizmoPos + Vector3.back * crossSize);

            // 라벨 표시 (반경)
            UnityEditor.Handles.Label(gizmoPos + Vector3.up * 0.5f, $"AOE: {lastAOERadius:F1}");
        }
#endif

        #endregion

        #region Issue #476: 도전던전 스턴 시스템

        /// <summary>
        /// 도전던전에서 결계 스턴 게이지 100 도달 시 호출
        /// 캐릭터 공격 중지
        /// </summary>
        public void ApplyStunFromBossDungeon(float duration)
        {
            if (isStunnedByBossDungeon) return;

            isStunnedByBossDungeon = true;
            Debug.Log($"[Character] 도전던전 스턴 적용: {duration}초");
        }

        /// <summary>
        /// 도전던전 스턴 해제
        /// </summary>
        public void ReleaseStunFromBossDungeon()
        {
            if (!isStunnedByBossDungeon) return;

            isStunnedByBossDungeon = false;
            Debug.Log("[Character] 도전던전 스턴 해제");
        }

        /// <summary>
        /// 캐릭터가 살아있는지 확인 (IPoolable과 별개로 간단 체크)
        /// </summary>
        public bool IsAlive()
        {
            return gameObject.activeInHierarchy;
        }

        #endregion
    }
}
