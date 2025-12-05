//LMJ : Character with simple projectile-based combat (Issue #265)
//     Migrated to new CSV-based skill system
namespace Novelian.Combat
{
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using NovelianMagicLibraryDefense.Managers;

    public class Character : MonoBehaviour, IPoolable
    {
        [Header("Character Visual")]
        [SerializeField] private GameObject characterObj;

        [Header("Character Animator")]
        [SerializeField] private Animator characterAnimator;

        // JML: Animation trigger hash (성능 최적화)
        private static readonly int ANIM_ATTACK = Animator.StringToHash("Attack");
        private static readonly int ANIM_DIE = Animator.StringToHash("Die");
        private static readonly int ANIM_VICTORY = Animator.StringToHash("Victory");

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

        [Header("Spawn Position")]
        [SerializeField, Tooltip("Projectile spawn offset (Y=1.5 for chest height)")]
        private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Projectile Template")]
        [SerializeField, Tooltip("Generic projectile template (used when skill has no projectile prefab)")]
        private GameObject projectileTemplate;

        [Header("Targeting Strategy")]
        [SerializeField, Tooltip("Use weight-based targeting (default: distance-based)")]
        private bool useWeightTargeting = false;

        // 캐싱된 스킬 데이터
        private MainSkillData basicAttackData;
        private MainSkillData activeSkillData;
        private SupportSkillData supportData;
        private MainSkillPrefabEntry basicAttackPrefabs;
        private MainSkillPrefabEntry activeSkillPrefabs;
        private SupportSkillPrefabEntry supportPrefabs;

        // 스킬 레벨 데이터 (현재는 레벨 1 고정, 추후 레벨 시스템 추가 시 확장)
#pragma warning disable CS0414 // 추후 레벨 시스템 구현 시 사용 예정
        private int currentSkillLevel = 1;
#pragma warning restore CS0414

        // 최종 수치 계산 프로퍼티 (새 데미지 공식 적용)
        // 공식: (기본 데미지) × (레벨 배율) × (보조 스킬 배율) × (캐릭터 변형)
        private float FinalDamage
        {
            get
            {
                if (basicAttackData == null) return 0f;
                // 레벨 데이터 조회 (없으면 배율 1)
                float levelMult = 1f;
                // 보조 스킬 배율
                float supportMult = supportData?.damage_mult ?? 1f;
                // DamageCalculator 사용
                float baseDamage = DamageCalculator.CalculateSingleDamage(basicAttackData.base_damage, levelMult, supportMult);
                // 캐릭터 변형 적용
                return baseDamage * (1f + damageModifier / 100f);
            }
        }

        private float FinalAttackSpeed
        {
            get
            {
                if (basicAttackData == null) return 1f;
                // 쿨다운에 서포트 배율 적용
                float cooldown = basicAttackData.cooldown;
                if (supportData != null) cooldown *= supportData.cooldown_mult;
                cooldown = Mathf.Max(cooldown, 0.1f); // 최소 쿨다운 보장
                // 공격 속도 계산
                float attackSpeed = (1f / cooldown) * (1f + attackSpeedModifier / 100f);
                if (supportData != null) attackSpeed *= supportData.attack_speed_mult;
                return Mathf.Max(attackSpeed, 0.1f); // 음수 방지
            }
        }

        private float FinalProjectileSpeed
        {
            get
            {
                if (basicAttackData == null) return 10f;
                // projectile_speed가 0이면 기본값 15 사용 (의문의 예고장 등)
                float baseSpeed = basicAttackData.projectile_speed > 0 ? basicAttackData.projectile_speed : 15f;
                float speed = baseSpeed * (1f + projectileSpeedModifier / 100f);
                if (supportData != null) speed *= supportData.speed_mult;
                return speed;
            }
        }

        private float FinalRange => basicAttackData != null
            ? basicAttackData.range * (1f + rangeModifier / 100f)
            : 1000f;

        private float FinalProjectileLifetime => basicAttackData != null
            ? (basicAttackData.skill_lifetime > 0 ? basicAttackData.skill_lifetime : 5f)
            : 5f;

        // Active Skill 최종 수치 계산 프로퍼티 (새 데미지 공식 적용)
        // 공식: (기본 데미지) × (레벨 배율) × (보조 스킬 배율) × (캐릭터 변형)
        private float FinalActiveDamage
        {
            get
            {
                if (activeSkillData == null) return 0f;
                // 레벨 배율 (현재 레벨 1 고정)
                float levelMult = 1f;
                // 보조 스킬 배율
                float supportMult = supportData?.damage_mult ?? 1f;
                // DamageCalculator로 단일 데미지 계산
                float baseDamage = DamageCalculator.CalculateSingleDamage(activeSkillData.base_damage, levelMult, supportMult);
                // 캐릭터 변형 적용
                return baseDamage * (1f + damageModifier / 100f);
            }
        }

        private float FinalActiveAttackSpeed
        {
            get
            {
                if (activeSkillData == null) return 1f;
                // 쿨다운에 서포트 배율 적용
                float cooldown = activeSkillData.cooldown;
                if (supportData != null) cooldown *= supportData.cooldown_mult;
                cooldown = Mathf.Max(cooldown, 0.1f); // 최소 쿨다운 보장
                // 공격 속도 계산
                float attackSpeed = (1f / cooldown) * (1f + attackSpeedModifier / 100f);
                if (supportData != null) attackSpeed *= supportData.attack_speed_mult;
                return Mathf.Max(attackSpeed, 0.1f); // 음수 방지
            }
        }

        private float FinalActiveProjectileSpeed
        {
            get
            {
                if (activeSkillData == null) return 10f;
                float speed = activeSkillData.projectile_speed * (1f + projectileSpeedModifier / 100f);
                if (supportData != null) speed *= supportData.speed_mult;
                return speed;
            }
        }

        private float FinalActiveRange => activeSkillData != null
            ? activeSkillData.range * (1f + rangeModifier / 100f)
            : 1000f;

        private float FinalActiveProjectileLifetime
        {
            get
            {
                if (activeSkillData == null) return 5f;
                float lifetime = activeSkillData.skill_lifetime;
                // Support skill duration multiplier (currently not in CSV, future feature)
                return lifetime;
            }
        }

        // Active Skill 발사체 개수 (기본 개수 + 서포트 추가 개수)
        private int FinalActiveProjectileCount
        {
            get
            {
                if (activeSkillData == null) return 1;

                // 기본 발사체 개수 + 서포트 스킬 추가 개수
                int baseCount = activeSkillData.projectile_count;
                int additionalCount = supportData?.add_projectiles ?? 0;
                return Mathf.Max(1, baseCount + additionalCount);
            }
        }

        // Attack state
        private CancellationTokenSource attackCts;
        private CancellationTokenSource activeSkillCts;
        private CancellationTokenSource channelingCts;
        private bool isInitialized = false;
        private bool isChanneling = false;

        // JML: 책갈피 시스템 (Issue #320)
        private int characterId = -1;
        private bool isManuallyInitialized = false;  // Initialize()로 초기화되었는지 여부
        private bool autoAttackEnabled = true;  // 자동 공격 활성화 여부 (테스트 씬에서 false로 설정)

        // JML: 비주얼 파츠 캐시 (Issue #356)
        private Dictionary<string, Transform> cachedTransforms = new Dictionary<string, Transform>();
        private Transform weaponRightSlot;
        private Transform weaponLeftSlot;

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

        //LMJ : Load skill data from CSV and PrefabDatabase
        private void LoadSkillData()
        {
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogError("[Character] CSVLoader not initialized!");
                return;
            }

            var prefabDb = SkillPrefabDatabase.Instance;

            // Basic Attack Skill
            if (basicAttackSkillId > 0)
            {
                basicAttackData = CSVLoader.Instance.GetData<MainSkillData>(basicAttackSkillId);
                if (basicAttackData != null)
                {
                    basicAttackPrefabs = prefabDb?.GetMainSkillEntry(basicAttackSkillId);
                    Debug.Log($"[Character] Loaded basic attack: {basicAttackData.skill_name} (ID: {basicAttackSkillId})");
                }
                else
                {
                    Debug.LogError($"[Character] Basic attack skill ID {basicAttackSkillId} not found in CSV!");
                }
            }

            // Active Skill
            if (activeSkillId > 0)
            {
                activeSkillData = CSVLoader.Instance.GetData<MainSkillData>(activeSkillId);
                if (activeSkillData != null)
                {
                    activeSkillPrefabs = prefabDb?.GetMainSkillEntry(activeSkillId);
                    Debug.Log($"[Character] Loaded active skill: {activeSkillData.skill_name} (ID: {activeSkillId})");
                }
                else
                {
                    Debug.LogWarning($"[Character] Active skill ID {activeSkillId} not found in CSV!");
                }
            }

            // Support Skill + Compatibility 검증
            if (supportSkillId > 0)
            {
                supportData = CSVLoader.Instance.GetData<SupportSkillData>(supportSkillId);
                if (supportData != null)
                {
                    // CompatibilityTable 검증
                    bool isCompatible = ValidateSupportCompatibility(basicAttackData, supportData);
                    if (!isCompatible)
                    {
                        Debug.LogWarning($"[Character] 서포트 스킬 '{supportData.support_name}'은(는) 메인 스킬 '{basicAttackData?.skill_name}'과 호환되지 않습니다! 서포트 효과가 제한됩니다.");
                    }

                    supportPrefabs = prefabDb?.GetSupportSkillEntry(supportSkillId);
                    Debug.Log($"[Character] Loaded support skill: {supportData.support_name} (ID: {supportSkillId}, speed_mult: {supportData.speed_mult}, damage_mult: {supportData.damage_mult}, compatible: {isCompatible})");
                }
                else
                {
                    Debug.LogWarning($"[Character] Support skill ID {supportSkillId} not found in CSV!");
                }
            }
            else
            {
                // 서포트 스킬이 없을 때 초기화
                supportData = null;
                supportPrefabs = null;
                Debug.Log("[Character] No support skill selected (supportData = null)");
            }
        }

        /// <summary>
        /// 서포트 스킬과 메인 스킬의 호환성 검증
        /// SupportCompatibilityTable 기반
        /// </summary>
        private bool ValidateSupportCompatibility(MainSkillData mainSkill, SupportSkillData support)
        {
            if (mainSkill == null || support == null) return false;

            var compatibilityTable = CSVLoader.Instance?.GetTable<SupportCompatibilityData>();
            if (compatibilityTable == null)
            {
                Debug.LogWarning("[Character] SupportCompatibilityTable not loaded. Skipping compatibility check.");
                return true; // 테이블 없으면 통과
            }

            var compatibility = compatibilityTable.GetId(support.support_id);
            if (compatibility == null)
            {
                Debug.LogWarning($"[Character] Compatibility data not found for support {support.support_id}");
                return true; // 데이터 없으면 통과
            }

            return compatibility.IsCompatibleWith(mainSkill.GetSkillType());
        }

        /// <summary>
        /// 현재 서포트 스킬이 메인 스킬과 호환되는지 확인
        /// </summary>
        private bool IsSupportCompatible(MainSkillData mainSkill)
        {
            if (supportData == null) return true; // 서포트 없으면 항상 true
            return ValidateSupportCompatibility(mainSkill, supportData);
        }

        //LMJ : Initialize projectile pool (from basic attack skill)
        private void InitializeProjectilePool()
        {
            if (basicAttackData == null)
            {
                Debug.LogError("[Character] basicAttackData is null!");
                return;
            }

            // Check for projectile prefab
            GameObject projectilePrefab = basicAttackPrefabs?.projectilePrefab;
            if (projectilePrefab == null && projectileTemplate == null)
            {
                Debug.LogWarning($"[Character] No projectile prefab for skill {basicAttackData.skill_name}. Using template.");
            }

            if (projectileTemplate == null)
            {
                Debug.LogError("[Character] ProjectileTemplate is not assigned! Please assign it in the Inspector.");
                return;
            }

            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;

            if (!pool.HasPool<Projectile>())
            {
                pool.CreatePool<Projectile>(projectileTemplate, defaultCapacity: 20, maxSize: 100);
                pool.WarmUp<Projectile>(20);
                Debug.Log($"[Character] Projectile pool initialized for skill: {basicAttackData.skill_name}");
            }
        }

        //LMJ : Initialize active skill projectile pool
        private void InitializeActiveSkillPool()
        {
            if (activeSkillData == null)
            {
                Debug.LogWarning("[Character] activeSkillData is null. Skipping active skill initialization.");
                return;
            }

            if (projectileTemplate == null)
            {
                Debug.LogError("[Character] ProjectileTemplate is not assigned! Please assign it in the Inspector.");
                return;
            }

            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;

            // ProjectileTemplate은 모든 스킬이 공유하므로 이미 생성되어 있을 수 있음
            if (!pool.HasPool<Projectile>())
            {
                pool.CreatePool<Projectile>(projectileTemplate, defaultCapacity: 10, maxSize: 50);
                pool.WarmUp<Projectile>(10);
                Debug.Log($"[Character] Active skill pool initialized for skill: {activeSkillData.skill_name}");
            }
        }

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
            if (activeSkillData == null)
            {
                Debug.LogWarning("[Character] activeSkillData is null. Skipping active skill loop.");
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
                // 타겟이 없으면 공격 스킵 (정상적인 상황)
                return;
            }

            // JML: 공격 애니메이션 재생
            PlayAttackAnimation();

            // 스킬 타입별 분기 처리
            switch (skillType)
            {
                // 투사체 스킬 - 투사체 발사
                case SkillAssetType.Projectile:
                    LaunchProjectile(target);
                    break;

                // 단일 즉발 스킬 - 타겟에게 즉시 데미지/효과
                case SkillAssetType.InstantSingle:
                    // 심장마비: 체력 10% 이하 적 즉사 (보스 제외)
                    if (basicAttackData.IsInstantKillSkill)
                    {
                        UseInstantKillSkill(target);
                    }
                    else
                    {
                        UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
                    }
                    break;

                // 범위 스킬 - 타겟 위치에 AOE 효과
                case SkillAssetType.AOE:
                    // 다이너마이트: 투사체를 던져서 N초 후 폭발 (특수 처리)
                    if (basicAttackData.IsDynamiteSkill)
                    {
                        LaunchDynamiteProjectile(target);
                    }
                    // 전설의 지팡이: 투사체가 일직선으로 날아가며 경로상 AOE 데미지 (특수 처리)
                    else if (basicAttackData.IsLegendaryStaffSkill)
                    {
                        LaunchLegendaryStaffProjectile(target);
                    }
                    else
                    {
                        UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
                    }
                    break;

                // DOT 스킬 - 범위 내 적에게 지속 데미지 (AOE 방식)
                case SkillAssetType.DOT:
                    UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
                    break;

                // 디버프 스킬 - 범위 내 적에게 디버프 적용 (AOE 방식)
                case SkillAssetType.Debuff:
                    UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
                    break;

                // 채널링 스킬 - 지속 시전
                case SkillAssetType.Channeling:
                    UseChannelingSkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage).Forget();
                    break;

                // 트랩 스킬 - 필드에 트랩 오브젝트 설치
                case SkillAssetType.Trap:
                    PlaceTrapObject(target, basicAttackData, basicAttackPrefabs, FinalDamage);
                    break;

                // 지뢰 스킬 - 필드에 지뢰 오브젝트 설치
                case SkillAssetType.Mine:
                    PlaceMineObject(target, basicAttackData, basicAttackPrefabs, FinalDamage);
                    break;

                default:
                    Debug.LogWarning($"[Character] Unknown skill type: {skillType}, falling back to projectile");
                    LaunchProjectile(target);
                    break;
            }
        }

        //LMJ : TryAttack ends here

        //LMJ : Launch projectile(s) - extracted from old TryAttack for reuse
        private void LaunchProjectile(ITargetable target)
        {
            if (target == null || basicAttackData == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // Flatten target position to horizontal plane (수평 발사)
            targetPos.y = spawnPos.y;

            // Get projectile prefab from database
            GameObject projectilePrefab = basicAttackPrefabs?.projectilePrefab;
            GameObject hitEffectPrefab = basicAttackPrefabs?.hitEffectPrefab;

            // Launch projectile(s) - 다중 발사 지원 (add_projectiles)
            if (projectilePrefab != null || projectileTemplate != null)
            {
                var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;

                // 발사체 개수 계산 (CSV의 projectile_count + 서포트 추가)
                // 파편화(40002)는 명중 시 분열이므로 발사 시에는 서포트 추가분 제외
                int projectileCount = basicAttackData.projectile_count;
                if (projectileCount <= 0) projectileCount = 1; // 최소 1발 보장
                if (supportData != null && supportData.support_id != 40002)
                {
                    projectileCount += supportData.add_projectiles;
                }

                // 연속 발사 (기관총처럼 순차 발사)
                if (projectileCount > 1)
                {
                    FireBurstProjectilesAsync(pool, spawnPos, targetPos, projectileCount).Forget();
                }
                else
                {
                    // 1발만 발사하는 경우 즉시 발사
                    Projectile projectile = pool.Spawn<Projectile>(spawnPos);
                    projectile.Launch(spawnPos, targetPos, FinalProjectileSpeed, FinalProjectileLifetime, FinalDamage, basicAttackSkillId, supportSkillId);
                    Debug.Log($"[Character] Fired 1 projectile {basicAttackData.skill_name} (Damage: {FinalDamage:F1}, Speed: {FinalProjectileSpeed:F1})");
                }
            }
            // Instant attack (no projectile)
            else
            {
                // Hit effect at target collider center
                Collider targetCol = target.GetTransform().GetComponent<Collider>();
                Vector3 hitPos = targetCol != null ? targetCol.bounds.center : target.GetPosition();

                if (hitEffectPrefab != null)
                {
                    GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                    UnityEngine.Object.Destroy(hitEffect, 2f);
                }

                // Apply damage
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    Monster monster = target.GetTransform().GetComponent<Monster>();
                    if (monster != null) monster.TakeDamage(FinalDamage);
                }
                else if (target.GetTransform().CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                    if (boss != null) boss.TakeDamage(FinalDamage);
                }
            }
        }

        //LMJ : Launch dynamite projectile (던져서 N초 후 폭발)
        private void LaunchDynamiteProjectile(ITargetable target)
        {
            if (target == null || basicAttackData == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // 다이너마이트는 타겟 위치로 던지기 (Y축 유지하지 않음 - 포물선 이동)
            // targetPos.y = spawnPos.y; // 수평 발사 대신 포물선 이동을 위해 주석처리

            // Get projectile prefab from database
            GameObject projectilePrefab = basicAttackPrefabs?.projectilePrefab;

            if (projectilePrefab != null || projectileTemplate != null)
            {
                var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;

                // Spawn projectile
                Projectile projectile = pool.Spawn<Projectile>(spawnPos);

                // Launch with dynamite parameters
                // skill_lifetime을 폭발 딜레이로 사용, Projectile에서 다이너마이트 처리
                float projectileSpeed = basicAttackData.projectile_speed > 0 ? basicAttackData.projectile_speed : 10f;
                float lifetime = basicAttackData.skill_lifetime > 0 ? basicAttackData.skill_lifetime + 1f : 6f; // 퓨즈 시간 + 여유

                projectile.Launch(spawnPos, targetPos, projectileSpeed, lifetime, FinalDamage, basicAttackSkillId, supportSkillId);
                Debug.Log($"[Character] Launched Dynamite projectile: speed={projectileSpeed}, fuseTime={basicAttackData.skill_lifetime}, damage={FinalDamage:F1}");
            }
            else
            {
                Debug.LogWarning("[Character] LaunchDynamiteProjectile: No projectile prefab found, falling back to instant AOE");
                UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
            }
        }

        //LMJ : Launch legendary staff projectile (일직선 이동하며 경로상 AOE 데미지)
        private void LaunchLegendaryStaffProjectile(ITargetable target)
        {
            if (target == null || basicAttackData == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // 수평 발사 (Y축 동일하게)
            targetPos.y = spawnPos.y;

            // Get projectile prefab from database
            GameObject projectilePrefab = basicAttackPrefabs?.projectilePrefab;

            if (projectilePrefab != null || projectileTemplate != null)
            {
                var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;

                // Spawn projectile
                Projectile projectile = pool.Spawn<Projectile>(spawnPos);

                // Launch with legendary staff parameters
                // range를 투사체 속도로 사용 (CSV에 projectile_speed가 0)
                float projectileSpeed = basicAttackData.projectile_speed > 0 ? basicAttackData.projectile_speed : 20f;
                float lifetime = basicAttackData.range / projectileSpeed + 1f; // 사거리까지 이동 시간 + 여유

                projectile.Launch(spawnPos, targetPos, projectileSpeed, lifetime, FinalDamage, basicAttackSkillId, supportSkillId);
                Debug.Log($"[Character] Launched LegendaryStaff projectile: speed={projectileSpeed}, range={basicAttackData.range}, aoeRadius={basicAttackData.aoe_radius}, damage={FinalDamage:F1}");
            }
            else
            {
                Debug.LogWarning("[Character] LaunchLegendaryStaffProjectile: No projectile prefab found, falling back to instant AOE");
                UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
            }
        }

        //LMJ : Attempt to use active skill on target
        private void TryUseActiveSkill()
        {
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

            // JML: 공격 애니메이션 재생 (액티브 스킬도 동일)
            PlayAttackAnimation();

            // 스킬 타입별 분기 처리 (TryAttack과 동일한 구조)
            switch (skillType)
            {
                // 투사체 스킬 - 투사체 발사
                case SkillAssetType.Projectile:
                    // 다이너마이트: 투사체를 던져서 N초 후 폭발
                    if (activeSkillData.IsDynamiteSkill)
                    {
                        LaunchActiveProjectile(target, isDynamite: true);
                    }
                    // 전설의 지팡이: 일직선 AOE
                    else if (activeSkillData.IsLegendaryStaffSkill)
                    {
                        LaunchActiveProjectile(target, isLegendaryStaff: true);
                    }
                    // 의문의 예고장: 시한폭탄
                    else if (activeSkillData.IsTimeBombSkill)
                    {
                        LaunchActiveProjectile(target, isTimeBomb: true);
                    }
                    // 부메랑
                    else if (activeSkillData.IsBoomerangSkill)
                    {
                        LaunchActiveProjectile(target, isBoomerang: true);
                    }
                    else
                    {
                        LaunchActiveProjectile(target);
                    }
                    break;

                // 단일 즉발 스킬 - 타겟에게 즉시 데미지/효과
                case SkillAssetType.InstantSingle:
                    // 심장마비: 체력 10% 이하 적 즉사 (보스 제외)
                    if (activeSkillData.IsInstantKillSkill)
                    {
                        UseActiveInstantKillSkill(target);
                    }
                    else
                    {
                        UseAOESkillAsync(target, activeSkillData, activeSkillPrefabs, FinalActiveDamage, FinalActiveRange, FinalActiveProjectileSpeed).Forget();
                    }
                    break;

                // 범위 스킬 - 타겟 위치에 AOE 효과
                case SkillAssetType.AOE:
                    if (activeSkillData.IsDynamiteSkill)
                    {
                        LaunchActiveProjectile(target, isDynamite: true);
                    }
                    else if (activeSkillData.IsLegendaryStaffSkill)
                    {
                        LaunchActiveProjectile(target, isLegendaryStaff: true);
                    }
                    else
                    {
                        UseAOESkillAsync(target, activeSkillData, activeSkillPrefabs, FinalActiveDamage, FinalActiveRange, FinalActiveProjectileSpeed).Forget();
                    }
                    break;

                // DOT 스킬 - 범위 내 적에게 지속 데미지
                case SkillAssetType.DOT:
                    UseAOESkillAsync(target, activeSkillData, activeSkillPrefabs, FinalActiveDamage, FinalActiveRange, FinalActiveProjectileSpeed).Forget();
                    break;

                // 디버프 스킬 - 범위 내 적에게 디버프 적용
                case SkillAssetType.Debuff:
                    UseAOESkillAsync(target, activeSkillData, activeSkillPrefabs, FinalActiveDamage, FinalActiveRange, FinalActiveProjectileSpeed).Forget();
                    break;

                // 채널링 스킬 - 지속 시전
                case SkillAssetType.Channeling:
                    UseChannelingSkillAsync(target, activeSkillData, activeSkillPrefabs, FinalActiveDamage).Forget();
                    break;

                // 트랩 스킬 - 필드에 트랩 오브젝트 설치
                case SkillAssetType.Trap:
                    PlaceTrapObject(target, activeSkillData, activeSkillPrefabs, FinalActiveDamage);
                    break;

                // 지뢰 스킬 - 필드에 지뢰 오브젝트 설치
                case SkillAssetType.Mine:
                    PlaceMineObject(target, activeSkillData, activeSkillPrefabs, FinalActiveDamage);
                    break;

                default:
                    Debug.LogWarning($"[Character] Unknown active skill type: {skillType}, falling back to projectile");
                    LaunchActiveProjectile(target);
                    break;
            }
        }

        //LMJ : Launch active skill projectile(s)
        private void LaunchActiveProjectile(ITargetable target, bool isDynamite = false, bool isLegendaryStaff = false, bool isTimeBomb = false, bool isBoomerang = false)
        {
            if (target == null || activeSkillData == null) return;

            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;
            int projectileCount = FinalActiveProjectileCount;
            float spreadAngle = 15f;

            for (int i = 0; i < projectileCount; i++)
            {
                float angleOffset = (i - (projectileCount - 1) / 2f) * spreadAngle;
                Vector3 direction = (targetPos - spawnPos).normalized;
                Quaternion rotation = Quaternion.Euler(0, angleOffset, 0);
                Vector3 spreadDirection = rotation * direction;
                Vector3 spreadTargetPos = spawnPos + spreadDirection * 1000f;

                Projectile projectile = pool.Spawn<Projectile>(spawnPos);
                projectile.Launch(spawnPos, spreadTargetPos, FinalActiveProjectileSpeed, FinalActiveProjectileLifetime, FinalActiveDamage, activeSkillId, supportSkillId);
            }

            Debug.Log($"[Character] Active Projectile: {activeSkillData.skill_name} x{projectileCount} (Damage: {FinalActiveDamage:F1})");
        }

        //LMJ : Use active instant kill skill (심장마비)
        private void UseActiveInstantKillSkill(ITargetable target)
        {
            if (activeSkillData == null || target == null) return;

            Debug.Log($"[Character] Active InstantKill: {activeSkillData.skill_name}");

            // 범위 내 체력 10% 이하인 적 즉사 (보스 제외)
            float aoeRadius = activeSkillData.aoe_radius > 0 ? activeSkillData.aoe_radius : 100f;
            Collider[] hits = Physics.OverlapSphere(target.GetPosition(), aoeRadius);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag(Tag.Monster))
                {
                    Monster monster = hits[i].GetComponent<Monster>();
                    if (monster != null && monster.IsAlive())
                    {
                        float hpPercent = monster.GetHealth() / monster.GetMaxHealth();
                        if (hpPercent <= 0.1f)
                        {
                            Debug.Log($"[Character] InstantKill 즉사: {monster.name} (HP: {hpPercent * 100:F1}%)");
                            monster.Die();
                        }
                        else
                        {
                            // 10% 초과면 일반 데미지
                            monster.TakeDamage(FinalActiveDamage);
                        }
                    }
                }
                // 보스는 즉사 불가, 일반 데미지만
                else if (hits[i].CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = hits[i].GetComponent<BossMonster>();
                    if (boss != null && boss.IsAlive())
                    {
                        boss.TakeDamage(FinalActiveDamage);
                    }
                }
            }
        }

        //LMJ : Use channeling skill (laser/beam style)
        //LMJ : 통합된 채널링 스킬 메서드 - 기본공격/액티브 모두 사용
        private async UniTaskVoid UseChannelingSkillAsync(ITargetable target, MainSkillData skillData, MainSkillPrefabEntry prefabs, float damage)
        {
            if (skillData == null || skillData.GetSkillType() != SkillAssetType.Channeling) return;

            isChanneling = true;
            channelingCts?.Cancel();
            channelingCts?.Dispose();
            channelingCts = new CancellationTokenSource();
            var ct = channelingCts.Token;

            GameObject castEffect = null;
            GameObject startEffect = null;
            System.Collections.Generic.List<GameObject> beamEffects = new System.Collections.Generic.List<GameObject>();
            System.Collections.Generic.List<GameObject> hitEffects = new System.Collections.Generic.List<GameObject>();

            try
            {
                Debug.Log($"[Character] Starting channeling skill: {skillData.skill_name}");

                // Get prefabs
                GameObject castEffectPrefab = prefabs?.castEffectPrefab;
                GameObject projectileEffectPrefab = prefabs?.projectilePrefab;
                GameObject areaEffectPrefab = prefabs?.areaEffectPrefab;
                GameObject hitEffectPrefab = prefabs?.hitEffectPrefab;

                // 1. Cast Effect (시전 준비) - cast_time_mult 적용
                float castTime = skillData.cast_time;
                if (supportData != null) castTime *= supportData.cast_time_mult;

                if (castTime > 0f && castEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    castEffect = UnityEngine.Object.Instantiate(castEffectPrefab, spawnPos, Quaternion.identity);
                    Debug.Log($"[Character] Cast Effect started ({castTime:F1}s)");

                    await UniTask.Delay((int)(castTime * 1000), cancellationToken: ct);

                    if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
                }

                // Check if target is still valid after cast time
                if (target == null || !target.IsAlive())
                {
                    Debug.Log("[Character] Channeling cancelled: Target died during cast");
                    return;
                }

                // 모든 Channeling 이펙트가 Wall을 통과하도록 레이어 설정용
                int projectileLayer = LayerMask.NameToLayer("Projectile");

                // 2. Start Effect (빔 발사 지점)
                if (projectileEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    startEffect = UnityEngine.Object.Instantiate(projectileEffectPrefab, spawnPos, Quaternion.identity);
                    startEffect.transform.SetParent(transform);

                    // Wall 통과를 위해 Projectile 레이어 설정 및 Collider 비활성화
                    if (projectileLayer != -1)
                    {
                        SetLayerRecursively(startEffect, projectileLayer);
                    }
                    DisableCollidersRecursively(startEffect);

                    // 렌더링 순서 조정 (Wall 앞에 렌더링되도록)
                    SetBeamRenderingOrder(startEffect, 100);

                    Debug.Log("[Character] Start Effect spawned (Layer: Projectile, Colliders disabled, RenderQueue: 3100)");
                }

                // 3. Build chain targets (if Chain support skill is active)
                System.Collections.Generic.List<ITargetable> chainTargets = BuildChainTargets(target);

                // 4. Create beam effects for all targets
                // 빔 이펙트가 Wall을 통과하도록 레이어 설정 및 Collider 비활성화
                if (areaEffectPrefab != null)
                {
                    for (int i = 0; i < chainTargets.Count; i++)
                    {
                        Vector3 spawnPos = (i == 0) ? transform.position + spawnOffset : chainTargets[i - 1].GetPosition();
                        GameObject beamEffect = UnityEngine.Object.Instantiate(areaEffectPrefab, spawnPos, Quaternion.identity);

                        // Wall 통과를 위해 Projectile 레이어 설정
                        if (projectileLayer != -1)
                        {
                            SetLayerRecursively(beamEffect, projectileLayer);
                        }

                        // Collider 비활성화 (빔은 시각적 효과만, 물리 충돌 불필요)
                        DisableCollidersRecursively(beamEffect);

                        // 렌더링 순서 조정 (Wall 앞에 렌더링되도록)
                        SetBeamRenderingOrder(beamEffect, 100);

                        // RetroBeamStatic의 beamCollides 비활성화 (Wall Raycast 충돌 방지)
                        DisableBeamCollision(beamEffect);

                        beamEffects.Add(beamEffect);
                    }
                    Debug.Log($"[Character] Created {beamEffects.Count} beam effects for {chainTargets.Count} targets (beamCollides disabled)");
                }

                // 5. Create hit effects for all targets
                // 히트 이펙트도 Wall 통과 처리
                if (hitEffectPrefab != null)
                {
                    for (int i = 0; i < chainTargets.Count; i++)
                    {
                        GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, chainTargets[i].GetPosition(), Quaternion.identity);
                        hitEffect.transform.SetParent(chainTargets[i].GetTransform());

                        // Wall 통과를 위해 Projectile 레이어 설정 및 Collider 비활성화
                        if (projectileLayer != -1)
                        {
                            SetLayerRecursively(hitEffect, projectileLayer);
                        }
                        DisableCollidersRecursively(hitEffect);

                        // 렌더링 순서 조정 (Wall 앞에 렌더링되도록)
                        SetBeamRenderingOrder(hitEffect, 100);

                        hitEffects.Add(hitEffect);
                    }
                }

                // 6. Channeling loop
                float elapsed = 0f;
                float nextTickTime = 0f;
                int tickCount = 0;
                bool firstTick = true;

                // Issue #362 - 채널링 지속시간 배율 적용
                float finalChannelDuration = skillData.channel_duration;
                if (supportData != null && supportData.channel_duration_mult > 0)
                {
                    finalChannelDuration = DamageCalculator.CalculateChannelDuration(
                        skillData.channel_duration, supportData.channel_duration_mult);
                }

                while (elapsed < finalChannelDuration)
                {
                    // Update beam effects and clean up dead targets
                    for (int i = 0; i < beamEffects.Count && i < chainTargets.Count; i++)
                    {
                        if (chainTargets[i] == null || !chainTargets[i].IsAlive())
                        {
                            if (beamEffects[i] != null) UnityEngine.Object.Destroy(beamEffects[i]);
                            beamEffects[i] = null;

                            if (i < hitEffects.Count && hitEffects[i] != null)
                            {
                                UnityEngine.Object.Destroy(hitEffects[i]);
                                hitEffects[i] = null;
                            }
                            continue;
                        }

                        Vector3 startPos = (i == 0) ? transform.position + spawnOffset : chainTargets[i - 1].GetPosition();
                        Vector3 endPos = chainTargets[i].GetPosition();
                        UpdateBeamEffect(beamEffects[i], startPos, endPos);
                    }

                    // Apply damage at tick intervals
                    if (elapsed >= nextTickTime)
                    {
                        float currentDamage = damage;

                        for (int i = 0; i < chainTargets.Count; i++)
                        {
                            if (chainTargets[i] == null || !chainTargets[i].IsAlive())
                                continue;

                            // Apply chain damage reduction
                            if (i > 0 && supportData != null && supportData.GetStatusEffectType() == StatusEffectType.Chain)
                            {
                                currentDamage *= (1f - supportData.chain_damage_reduction / 100f);
                            }

                            // Apply status effects (only on first tick)
                            if (firstTick && supportData != null && supportData.GetStatusEffectType() != StatusEffectType.Chain)
                            {
                                ApplyStatusEffect(chainTargets[i]);
                            }

                            // Apply damage
                            chainTargets[i].TakeDamage(currentDamage);

                            // 틱마다 히트 이펙트 재생 (타겟 위치에 새로 생성)
                            // Wall 통과 처리 포함
                            if (hitEffectPrefab != null)
                            {
                                GameObject tickHitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, chainTargets[i].GetPosition(), Quaternion.identity);

                                // Wall 통과를 위해 Projectile 레이어 설정 및 Collider 비활성화
                                if (projectileLayer != -1)
                                {
                                    SetLayerRecursively(tickHitEffect, projectileLayer);
                                }
                                DisableCollidersRecursively(tickHitEffect);

                                // 렌더링 순서 조정 (Wall 앞에 렌더링되도록)
                                SetBeamRenderingOrder(tickHitEffect, 100);

                                UnityEngine.Object.Destroy(tickHitEffect, 0.5f); // 짧은 시간 후 자동 삭제
                            }
                        }

                        tickCount++;
                        nextTickTime += skillData.channel_tick_interval;
                        firstTick = false;
                    }

                    await UniTask.Yield(ct);
                    elapsed += Time.deltaTime;
                }

                Debug.Log($"[Character] Channeling completed: {tickCount} ticks, {chainTargets.Count} targets");
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[Character] Channeling cancelled");
            }
            finally
            {
                if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
                if (startEffect != null) UnityEngine.Object.Destroy(startEffect);
                foreach (var beam in beamEffects)
                {
                    if (beam != null) UnityEngine.Object.Destroy(beam);
                }
                foreach (var hitEffect in hitEffects)
                {
                    if (hitEffect != null) UnityEngine.Object.Destroy(hitEffect);
                }

                isChanneling = false;
            }
        }

        //LMJ : 통합된 AOE 스킬 메서드 - 기본공격/액티브 모두 사용
        //      AOE, DOT, Debuff, Trap, Mine, InstantSingle 스킬 처리
        private async UniTaskVoid UseAOESkillAsync(ITargetable target, MainSkillData skillData, MainSkillPrefabEntry prefabs, float damage, float range, float projectileSpeed)
        {
            if (skillData == null) return;

            // Allow skill types that use AOE-style effects (범위 이펙트가 필요한 스킬 타입들)
            var skillType = skillData.GetSkillType();
            bool isValidType = skillType == SkillAssetType.AOE
                            || skillType == SkillAssetType.DOT
                            || skillType == SkillAssetType.Debuff
                            || skillType == SkillAssetType.Trap
                            || skillType == SkillAssetType.Mine
                            || skillType == SkillAssetType.InstantSingle;
            if (!isValidType) return;

            GameObject castEffect = null;
            GameObject meteorEffect = null;
            GameObject hitEffect = null;

            try
            {
                Debug.Log($"[Character] Starting AOE skill: {skillData.skill_name}");

                // Get prefabs
                GameObject castEffectPrefab = prefabs?.castEffectPrefab;
                GameObject projectileEffectPrefab = prefabs?.projectilePrefab;
                GameObject hitEffectPrefab = prefabs?.hitEffectPrefab;

                // 1. Cast Effect - cast_time_mult 적용
                float castTime = skillData.cast_time;
                if (supportData != null) castTime *= supportData.cast_time_mult;

                if (castTime > 0f && castEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    castEffect = UnityEngine.Object.Instantiate(castEffectPrefab, spawnPos, Quaternion.identity);

                    await UniTask.Delay((int)(castTime * 1000));

                    if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
                }

                // 2. Get target position - 밀집 지역 기반 타겟팅
                // AOE 스킬은 몬스터 origin이 아닌 가장 밀집된 Area를 타겟으로 함
                float aoeRadius = skillData.aoe_radius > 0 ? skillData.aoe_radius : 3f;
                if (supportData != null) aoeRadius *= supportData.aoe_mult;

                Vector3 targetPos = FindBestAOETargetPosition(range, aoeRadius);
                if (targetPos == Vector3.zero)
                {
                    Debug.Log("[Character] AOE cancelled: No valid targets in range");
                    return;
                }

                // 3. Ground impact position
                Vector3 impactPos = targetPos;
                Ray groundRay = new Ray(targetPos + Vector3.up * 10f, Vector3.down);
                if (Physics.Raycast(groundRay, out RaycastHit groundHit, 20f, LayerMask.GetMask("Ground")))
                {
                    impactPos = groundHit.point;
                }
                else
                {
                    impactPos = new Vector3(targetPos.x, 0f, targetPos.z);
                }

                // 4. Meteor Effect (only if projectile_speed > 0, otherwise instant AOE)
                if (skillData.projectile_speed > 0 && projectileEffectPrefab != null)
                {
                    Vector3 meteorStartPos = impactPos + Vector3.up * 20f;
                    meteorEffect = UnityEngine.Object.Instantiate(projectileEffectPrefab, meteorStartPos, Quaternion.identity);

                    float meteorSpeed = projectileSpeed > 0 ? projectileSpeed : 10f;
                    float distance = Vector3.Distance(meteorStartPos, impactPos);
                    float travelTime = distance / meteorSpeed;

                    float elapsed = 0f;
                    while (elapsed < travelTime && meteorEffect != null)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / travelTime);
                        meteorEffect.transform.position = Vector3.Lerp(meteorStartPos, impactPos, t);

                        Vector3 direction = (impactPos - meteorStartPos).normalized;
                        if (direction != Vector3.zero)
                        {
                            meteorEffect.transform.rotation = Quaternion.LookRotation(direction);
                        }

                        await UniTask.Yield(PlayerLoopTiming.Update);
                    }

                    if (meteorEffect != null)
                    {
                        meteorEffect.transform.position = impactPos;
                    }
                }

                // 5. Hit Effect - AOE 범위에 맞춰 스케일 조절
                // (표식/CC 스킬은 각 타겟에 개별 적용하므로 중앙 이펙트 생략)
                bool skipCentralEffect = skillData.HasMarkEffect || skillData.HasCCEffect;
                if (hitEffectPrefab != null && !skipCentralEffect)
                {
                    hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, impactPos, Quaternion.identity);

                    // 기본 이펙트 크기를 25 단위로 가정하고 aoeRadius에 비례하여 스케일 조절
                    // aoe_radius 150 → 스케일 6, aoe_radius 400 → 스케일 16
                    float baseEffectSize = 25f;
                    float scaleFactor = aoeRadius / baseEffectSize;
                    hitEffect.transform.localScale = Vector3.one * scaleFactor;
                }

                // 6. AOE damage - aoeRadius는 위에서 이미 계산됨
                Collider[] hits = Physics.OverlapSphere(impactPos, aoeRadius);
                float damageToApply = damage;

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider hit = hits[i];
                    if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                        continue;

                    ITargetable hitTarget = hit.GetComponent<ITargetable>();
                    if (hitTarget == null || !hitTarget.IsAlive())
                        continue;

                    // 각 대상에게 히트 이펙트 생성 (표식/CC 스킬은 ApplyMark/ApplyCC에서 처리하므로 제외)
                    if (hitEffectPrefab != null && !skipCentralEffect)
                    {
                        Vector3 hitTargetPos = hitTarget.GetPosition();
                        GameObject targetHitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitTargetPos + Vector3.up, Quaternion.identity);
                        UnityEngine.Object.Destroy(targetHitEffect, 1f);
                    }

                    // 데미지 적용 (디버프 스킬은 데미지 0일 수 있음)
                    if (damageToApply > 0)
                    {
                        hitTarget.TakeDamage(damageToApply);
                    }

                    // MainSkillData 자체 효과 적용 (CC/DOT/표식/디버프)
                    ApplyMainSkillEffectsToTarget(hitTarget, skillData);

                    // Support 효과 적용
                    if (supportData != null && supportData.GetStatusEffectType() != StatusEffectType.None)
                    {
                        ApplyStatusEffect(hitTarget);
                    }
                }

                // Cleanup
                if (meteorEffect != null)
                {
                    UnityEngine.Object.Destroy(meteorEffect, 0.1f);
                }
                if (hitEffect != null)
                {
                    UnityEngine.Object.Destroy(hitEffect, 2f);
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[Character] AOE skill cancelled");
            }
            finally
            {
                if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
            }
        }

        //LMJ : Use Buff skill - apply buff to self or allies
        //LMJ : 통합된 버프 스킬 메서드 - 기본공격/액티브 모두 사용
        private async UniTaskVoid UseBuffSkillAsync(MainSkillData skillData, MainSkillPrefabEntry prefabs)
        {
            if (skillData == null) return;

            var skillType = skillData.GetSkillType();
            if (skillType != SkillAssetType.Buff) return;

            GameObject castEffect = null;

            try
            {
                Debug.Log($"[Character] Starting Buff skill: {skillData.skill_name}");

                // Get prefabs
                GameObject castEffectPrefab = prefabs?.castEffectPrefab;
                GameObject hitEffectPrefab = prefabs?.hitEffectPrefab;

                // 1. Cast Effect
                float castTime = skillData.cast_time;
                if (supportData != null) castTime *= supportData.cast_time_mult;

                if (castTime > 0f && castEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    castEffect = UnityEngine.Object.Instantiate(castEffectPrefab, spawnPos, Quaternion.identity);
                    await UniTask.Delay((int)(castTime * 1000));
                    if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
                }

                // 2. Apply buff effect to allies in range
                float buffValue = skillData.base_buff_value;
                if (supportData != null) buffValue *= supportData.buff_value_mult;

                float buffDuration = skillData.skill_lifetime;
                if (supportData != null) buffDuration *= supportData.buff_value_mult; // 지속시간도 배율 적용

                BuffType buffType = skillData.GetBuffType();
                float buffRadius = skillData.aoe_radius > 0 ? skillData.aoe_radius : 400f; // 기본 범위

                // 범위 내 아군 캐릭터에게 버프 적용
                ApplyBuffToAlliesInRange(buffType, buffValue, buffDuration, buffRadius);

                Debug.Log($"[Character] Buff skill applied: {skillData.skill_name} (Type: {buffType}, Value: {buffValue}%, Duration: {buffDuration}s, Radius: {buffRadius})");

                // 3. 버프 지속시간 동안 이펙트 반복 재생
                if (hitEffectPrefab != null && buffDuration > 0)
                {
                    float effectTickInterval = 1f; // 1초마다 이펙트 재생
                    float elapsed = 0f;

                    while (elapsed < buffDuration)
                    {
                        Vector3 effectPos = transform.position;
                        GameObject tickEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, effectPos, Quaternion.identity);
                        UnityEngine.Object.Destroy(tickEffect, 0.8f); // 이펙트 0.8초 후 삭제

                        await UniTask.Delay((int)(effectTickInterval * 1000));
                        elapsed += effectTickInterval;
                    }
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[Character] Buff skill cancelled");
            }
            finally
            {
                if (castEffect != null) UnityEngine.Object.Destroy(castEffect);
            }
        }

        /// <summary>
        /// 범위 내 아군에게 버프 적용
        /// includeSelf: true면 자기 자신도 포함 (DevScene 테스트용)
        /// </summary>
        private void ApplyBuffToAlliesInRange(BuffType buffType, float buffValue, float duration, float radius, bool includeSelf = false)
        {
            // 범위 내 모든 캐릭터 찾기
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);

            // 아군이 없는 경우 (DevScene 테스트 등) 자기 자신에게 버프 적용
            bool hasAllies = false;
            foreach (var hit in hits)
            {
                if (hit.CompareTag(Tag.Character))
                {
                    Character ally = hit.GetComponent<Character>();
                    if (ally != null && ally != this)
                    {
                        hasAllies = true;
                        break;
                    }
                }
            }

            // 아군이 없으면 자동으로 자기 자신 포함
            if (!hasAllies)
            {
                includeSelf = true;
                Debug.Log("[Character] 범위 내 아군 없음 - 자기 자신에게 버프 적용");
            }

            foreach (var hit in hits)
            {
                if (!hit.CompareTag(Tag.Character)) continue;

                Character ally = hit.GetComponent<Character>();
                if (ally == null) continue;

                // 자신 제외 (세레나데 등) - includeSelf가 true면 자기 자신도 포함
                if (ally == this && !includeSelf) continue;

                // 버프 타입에 따라 스탯 적용
                float percentValue = buffValue / 100f; // % → 소수
                switch (buffType)
                {
                    case BuffType.ATK_Damage_UP:
                        ally.ApplyTemporaryBuff(StatType.Damage, percentValue, duration);
                        break;
                    case BuffType.ATK_Speed_UP:
                        ally.ApplyTemporaryBuff(StatType.AttackSpeed, percentValue, duration);
                        break;
                    case BuffType.ATK_Range_UP:
                        ally.ApplyTemporaryBuff(StatType.Range, percentValue, duration);
                        break;
                    case BuffType.Critical_Damage_UP:
                        ally.ApplyTemporaryBuff(StatType.CritMultiplier, percentValue, duration);
                        break;
                    case BuffType.Battle_Exp_UP:
                        // 경험치 버프는 별도 시스템 필요
                        Debug.Log($"[Character] EXP buff applied to {ally.name}: +{buffValue}%");
                        break;
                }
            }
        }

        /// <summary>
        /// 일시적 버프 적용 (지속시간 후 해제)
        /// </summary>
        public void ApplyTemporaryBuff(StatType statType, float value, float duration)
        {
            // 버프 적용
            ApplyStatBuff(statType, value);
            Debug.Log($"[Character] Temporary buff applied: {statType} +{value * 100}% for {duration}s");

            // 지속시간 후 해제
            RemoveBuffAfterDurationAsync(statType, value, duration).Forget();
        }

        private async UniTaskVoid RemoveBuffAfterDurationAsync(StatType statType, float value, float duration)
        {
            await UniTask.Delay((int)(duration * 1000));
            ApplyStatBuff(statType, -value); // 버프 해제 (음수로 제거)
            Debug.Log($"[Character] Temporary buff expired: {statType} -{value * 100}%");
        }

        //LMJ : Build chain targets for channeling skill
        private System.Collections.Generic.List<ITargetable> BuildChainTargets(ITargetable firstTarget)
        {
            var targets = new System.Collections.Generic.List<ITargetable> { firstTarget };

            // If no Chain support skill, return single target
            if (supportData == null || supportData.GetStatusEffectType() != StatusEffectType.Chain)
            {
                return targets;
            }

            // Build chain
            int maxChainCount = supportData.chain_count;
            var hitTargets = new System.Collections.Generic.HashSet<ITargetable> { firstTarget };
            ITargetable currentTarget = firstTarget;

            for (int i = 0; i < maxChainCount; i++)
            {
                ITargetable nextTarget = FindNextChainTarget(currentTarget.GetPosition(), supportData.chain_range, hitTargets);
                if (nextTarget == null) break;

                targets.Add(nextTarget);
                hitTargets.Add(nextTarget);
                currentTarget = nextTarget;
            }

            return targets;
        }

        //LMJ : Find next target for chain effect
        private ITargetable FindNextChainTarget(Vector3 currentPosition, float chainRange, System.Collections.Generic.HashSet<ITargetable> hitTargets)
        {
            Collider[] hits = Physics.OverlapSphere(currentPosition, chainRange);

            ITargetable closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable target = hit.GetComponent<ITargetable>();
                if (target == null || !target.IsAlive())
                    continue;

                if (hitTargets.Contains(target))
                    continue;

                float distance = Vector3.Distance(currentPosition, target.GetPosition());

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        //LMJ : Apply status effects to target (CC, DOT, Mark)
        private void ApplyStatusEffect(ITargetable target)
        {
            if (supportData == null || target == null) return;

            // Get effect prefabs
            GameObject ccEffectPrefab = supportPrefabs?.ccEffectPrefab;
            GameObject dotEffectPrefab = supportPrefabs?.dotEffectPrefab;
            GameObject markEffectPrefab = supportPrefabs?.markEffectPrefab;

            switch (supportData.GetStatusEffectType())
            {
                case StatusEffectType.CC:
                    if (target.GetTransform().CompareTag(Tag.Monster))
                    {
                        Monster monster = target.GetTransform().GetComponent<Monster>();
                        if (monster != null)
                        {
                            monster.ApplyCC(supportData.GetCCType(), supportData.cc_duration, supportData.cc_slow_amount, ccEffectPrefab);
                        }
                    }
                    else if (target.GetTransform().CompareTag(Tag.BossMonster))
                    {
                        BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                        if (boss != null)
                        {
                            boss.ApplyCC(supportData.GetCCType(), supportData.cc_duration, supportData.cc_slow_amount, ccEffectPrefab);
                        }
                    }
                    break;

                case StatusEffectType.DOT:
                    if (target.GetTransform().CompareTag(Tag.Monster))
                    {
                        Monster monster = target.GetTransform().GetComponent<Monster>();
                        if (monster != null)
                        {
                            monster.ApplyDOT(supportData.GetDOTType(), supportData.dot_damage_per_tick, supportData.dot_tick_interval, supportData.dot_duration, dotEffectPrefab);
                        }
                    }
                    else if (target.GetTransform().CompareTag(Tag.BossMonster))
                    {
                        BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                        if (boss != null)
                        {
                            boss.ApplyDOT(supportData.GetDOTType(), supportData.dot_damage_per_tick, supportData.dot_tick_interval, supportData.dot_duration, dotEffectPrefab);
                        }
                    }
                    break;

                case StatusEffectType.Mark:
                    if (target.GetTransform().CompareTag(Tag.Monster))
                    {
                        Monster monster = target.GetTransform().GetComponent<Monster>();
                        if (monster != null)
                        {
                            monster.ApplyMark(supportData.GetMarkType(), supportData.mark_duration, supportData.mark_damage_mult, markEffectPrefab);
                        }
                    }
                    else if (target.GetTransform().CompareTag(Tag.BossMonster))
                    {
                        BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                        if (boss != null)
                        {
                            boss.ApplyMark(supportData.GetMarkType(), supportData.mark_duration, supportData.mark_damage_mult, markEffectPrefab);
                        }
                    }
                    break;

                case StatusEffectType.Chain:
                    // Chain is handled in BuildChainTargets
                    break;
            }
        }

        /// <summary>
        /// MainSkillData 자체 효과를 타겟에게 적용 (CC/DOT/표식/디버프)
        /// </summary>
        private void ApplyMainSkillEffectsToTarget(ITargetable target, MainSkillData skillData)
        {
            if (target == null || skillData == null) return;

            GameObject hitEffectPrefab = activeSkillPrefabs?.hitEffectPrefab;

            // CC 효과
            if (skillData.HasCCEffect)
            {
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    Monster monster = target.GetTransform().GetComponent<Monster>();
                    monster?.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, hitEffectPrefab);
                }
                else if (target.GetTransform().CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                    boss?.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, hitEffectPrefab);
                }
            }

            // DOT 효과
            if (skillData.HasDOTEffect)
            {
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    Monster monster = target.GetTransform().GetComponent<Monster>();
                    monster?.ApplyDOT(DOTType.Burn, skillData.dot_damage_per_tick, skillData.dot_tick_interval, skillData.dot_duration, hitEffectPrefab);
                }
                else if (target.GetTransform().CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                    boss?.ApplyDOT(DOTType.Burn, skillData.dot_damage_per_tick, skillData.dot_tick_interval, skillData.dot_duration, hitEffectPrefab);
                }
            }

            // 표식 효과
            if (skillData.HasMarkEffect)
            {
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    Monster monster = target.GetTransform().GetComponent<Monster>();
                    monster?.ApplyMark(skillData.GetElementBasedMarkType(), skillData.mark_duration, skillData.mark_damage_mult / 100f, hitEffectPrefab);
                }
                else if (target.GetTransform().CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                    boss?.ApplyMark(skillData.GetElementBasedMarkType(), skillData.mark_duration, skillData.mark_damage_mult / 100f, hitEffectPrefab);
                }
            }

            // 디버프 효과
            if (skillData.HasDebuffEffect)
            {
                DeBuffType debuffType = skillData.GetDeBuffType();
                float debuffValue = skillData.base_debuff_value;
                if (supportData != null) debuffValue *= supportData.debuff_value_mult;

                float debuffDuration = skillData.skill_lifetime > 0 ? skillData.skill_lifetime : 10f;

                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    Monster monster = target.GetTransform().GetComponent<Monster>();
                    monster?.ApplyDebuff(debuffType, debuffValue, debuffDuration, hitEffectPrefab);
                }
                else if (target.GetTransform().CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                    boss?.ApplyDebuff(debuffType, debuffValue, debuffDuration, hitEffectPrefab);
                }

                Debug.Log($"[Character] Debuff applied: {debuffType} -{debuffValue}% for {debuffDuration}s");
            }
        }

        //LMJ : Update beam effect to connect two positions
        private void UpdateBeamEffect(GameObject beamEffect, Vector3 startPos, Vector3 endPos)
        {
            if (beamEffect == null) return;

            LineRenderer lineRenderer = beamEffect.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);
                return;
            }

            // Fallback: Transform-based
            Vector3 direction = endPos - startPos;
            float distance = direction.magnitude;

            beamEffect.transform.position = startPos;
            beamEffect.transform.rotation = Quaternion.LookRotation(direction);
            beamEffect.transform.localScale = new Vector3(1f, 1f, distance);
        }

        /// <summary>
        /// LMJ: 빔 이펙트의 렌더러가 Wall 앞에 렌더링되도록 설정
        /// LineRenderer/ParticleSystem의 sortingOrder를 높여서 Depth 문제 해결
        /// </summary>
        private void SetBeamRenderingOrder(GameObject beamEffect, int sortingOrder = 100)
        {
            if (beamEffect == null) return;

            // LineRenderer 설정
            LineRenderer lineRenderer = beamEffect.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.sortingOrder = sortingOrder;
                // Material의 renderQueue도 높여서 Wall 앞에 렌더링
                if (lineRenderer.material != null)
                {
                    lineRenderer.material.renderQueue = 3100; // Transparent 이후 렌더링
                }
            }

            // ParticleSystemRenderer 설정 (파티클 이펙트가 있는 경우)
            ParticleSystemRenderer[] particleRenderers = beamEffect.GetComponentsInChildren<ParticleSystemRenderer>();
            int particleCount = particleRenderers.Length;
            for (int i = 0; i < particleCount; i++)
            {
                particleRenderers[i].sortingOrder = sortingOrder;
                if (particleRenderers[i].material != null)
                {
                    particleRenderers[i].material.renderQueue = 3100;
                }
            }

            // 일반 MeshRenderer 설정
            MeshRenderer[] meshRenderers = beamEffect.GetComponentsInChildren<MeshRenderer>();
            int meshCount = meshRenderers.Length;
            for (int i = 0; i < meshCount; i++)
            {
                if (meshRenderers[i].material != null)
                {
                    meshRenderers[i].material.renderQueue = 3100;
                }
            }
        }

        /// <summary>
        /// LMJ: RetroArsenal 빔 이펙트의 Raycast 충돌 비활성화
        /// RetroBeamStatic 컴포넌트의 beamCollides를 false로 설정하여 Wall을 통과하도록 함
        /// </summary>
        private void DisableBeamCollision(GameObject beamEffect)
        {
            if (beamEffect == null) return;

            // RetroBeamStatic 컴포넌트 찾기 (자식 포함)
            var beamStatic = beamEffect.GetComponent<RetroArsenal.RetroBeamStatic>();
            if (beamStatic != null)
            {
                beamStatic.beamCollides = false;
                Debug.Log("[Character] RetroBeamStatic.beamCollides disabled");
            }

            // 자식에서도 찾기
            var childBeamStatics = beamEffect.GetComponentsInChildren<RetroArsenal.RetroBeamStatic>();
            int count = childBeamStatics.Length;
            for (int i = 0; i < count; i++)
            {
                childBeamStatics[i].beamCollides = false;
            }
        }

        // IPoolable implementation
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

        private void OnDestroy()
        {
            attackCts?.Cancel();
            attackCts?.Dispose();
            activeSkillCts?.Cancel();
            activeSkillCts?.Dispose();
            channelingCts?.Cancel();
            channelingCts?.Dispose();
        }

        /// <summary>
        /// 연속 발사 (기관총처럼 순차 발사)
        /// projectile_count가 2 이상인 스킬에서 사용
        /// </summary>
        private async UniTaskVoid FireBurstProjectilesAsync(ObjectPoolManager pool, Vector3 spawnPos, Vector3 targetPos, int projectileCount)
        {
            const float BURST_INTERVAL = 0.1f; // 연속 발사 간격 (100ms)

            Debug.Log($"[Character] Burst fire started: {projectileCount} projectiles ({basicAttackData.skill_name})");

            for (int i = 0; i < projectileCount; i++)
            {
                // 발사 시점에 타겟 방향 재계산 (동일한 방향으로 연속 발사)
                Projectile projectile = pool.Spawn<Projectile>(spawnPos);
                projectile.Launch(spawnPos, targetPos, FinalProjectileSpeed, FinalProjectileLifetime, FinalDamage, basicAttackSkillId, supportSkillId);

                // 마지막 발사가 아니면 대기
                if (i < projectileCount - 1)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(BURST_INTERVAL));
                }
            }

            Debug.Log($"[Character] Burst fire complete: {projectileCount} projectiles fired (Damage: {FinalDamage:F1}, Speed: {FinalProjectileSpeed:F1})");
        }

        // [REMOVED] ApplySkillEffects - 미사용 메서드 (ApplyMainSkillEffectsToTarget으로 통합됨)

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

        //LMJ : 통합된 트랩 배치 메서드 - 기본공격/액티브 모두 사용
        private void PlaceTrapObject(ITargetable target, MainSkillData skillData, MainSkillPrefabEntry prefabs, float damage)
        {
            if (skillData == null || target == null) return;

            // Get placement position
            Vector3 placementPos = target.GetPosition();

            // Raycast to ground for proper placement
            Ray groundRay = new Ray(placementPos + Vector3.up * 10f, Vector3.down);
            if (Physics.Raycast(groundRay, out RaycastHit groundHit, 20f, LayerMask.GetMask("Ground")))
            {
                placementPos = groundHit.point;
            }
            else
            {
                placementPos.y = 0f;
            }

            // Create trap object
            GameObject trapObj = new GameObject($"Trap_{skillData.skill_name}");
            TrapObject trap = trapObj.AddComponent<TrapObject>();
            trap.Initialize(skillData, prefabs, supportData, damage, placementPos);

            Debug.Log($"[Character] Placed Trap: {skillData.skill_name} at {placementPos}");
        }

        //LMJ : 통합된 지뢰 배치 메서드 - 기본공격/액티브 모두 사용
        private void PlaceMineObject(ITargetable target, MainSkillData skillData, MainSkillPrefabEntry prefabs, float damage)
        {
            if (skillData == null || target == null) return;

            // Get placement position
            Vector3 placementPos = target.GetPosition();

            // Raycast to ground for proper placement
            Ray groundRay = new Ray(placementPos + Vector3.up * 10f, Vector3.down);
            if (Physics.Raycast(groundRay, out RaycastHit groundHit, 20f, LayerMask.GetMask("Ground")))
            {
                placementPos = groundHit.point;
            }
            else
            {
                placementPos.y = 0f;
            }

            // Create mine object
            GameObject mineObj = new GameObject($"Mine_{skillData.skill_name}");
            MineObject mine = mineObj.AddComponent<MineObject>();
            mine.Initialize(skillData, prefabs, supportData, damage, placementPos);

            Debug.Log($"[Character] Placed Mine: {skillData.skill_name} at {placementPos}");
        }

        //LMJ : Use instant kill skill (심장마비 - 체력 10% 이하 적 즉사, 보스 제외)
        private void UseInstantKillSkill(ITargetable target)
        {
            if (basicAttackData == null || target == null) return;

            // Get hit effect prefab
            GameObject hitEffectPrefab = basicAttackPrefabs?.hitEffectPrefab;

            // Get target's collider for proper effect positioning
            Collider targetCol = target.GetTransform().GetComponent<Collider>();
            Vector3 hitPos = targetCol != null ? targetCol.bounds.center : target.GetPosition();

            // Check if target is boss (cannot be instant killed)
            if (target.GetTransform().CompareTag(Tag.BossMonster))
            {
                // Boss: apply normal damage instead
                BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                if (boss != null)
                {
                    boss.TakeDamage(FinalDamage);

                    // Spawn hit effect at boss center
                    if (hitEffectPrefab != null)
                    {
                        GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                        UnityEngine.Object.Destroy(hitEffect, 2f);
                    }

                    Debug.Log($"[Character] InstantKill on Boss: Normal damage {FinalDamage} (bosses cannot be instant killed)");
                }
                return;
            }

            // Regular monster: check HP threshold (10%)
            if (target.GetTransform().CompareTag(Tag.Monster))
            {
                Monster monster = target.GetTransform().GetComponent<Monster>();
                if (monster != null)
                {
                    float hpRatio = monster.GetHealth() / monster.GetMaxHealth();
                    float instantKillThreshold = 0.1f; // 10%

                    if (hpRatio <= instantKillThreshold)
                    {
                        // Instant kill!
                        monster.Die();

                        // Spawn hit effect at monster center
                        if (hitEffectPrefab != null)
                        {
                            GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                            UnityEngine.Object.Destroy(hitEffect, 2f);
                        }

                        Debug.Log($"[Character] InstantKill SUCCESS: {monster.name} (HP: {hpRatio * 100:F1}% <= {instantKillThreshold * 100}%)");
                    }
                    else
                    {
                        // HP too high: apply normal damage
                        monster.TakeDamage(FinalDamage);

                        // Spawn hit effect at monster center
                        if (hitEffectPrefab != null)
                        {
                            GameObject hitEffect = UnityEngine.Object.Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
                            UnityEngine.Object.Destroy(hitEffect, 2f);
                        }

                        Debug.Log($"[Character] InstantKill FAILED: {monster.name} HP {hpRatio * 100:F1}% > {instantKillThreshold * 100}%, applied {FinalDamage} damage");
                    }
                }
            }
        }

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

            switch (skillType)
            {
                // 투사체 스킬 - 투사체 발사
                case SkillAssetType.Projectile:
                    LaunchProjectile(target);
                    break;

                // 단일 즉발 스킬 - 타겟에게 즉시 데미지/효과
                case SkillAssetType.InstantSingle:
                    // 심장마비: 체력 10% 이하 적 즉사 (보스 제외)
                    if (basicAttackData.IsInstantKillSkill)
                    {
                        UseInstantKillSkill(target);
                    }
                    else
                    {
                        UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
                    }
                    break;

                // 범위 스킬 - 타겟 위치에 AOE 효과
                case SkillAssetType.AOE:
                    // 다이너마이트: 투사체를 던져서 N초 후 폭발 (특수 처리)
                    if (basicAttackData.IsDynamiteSkill)
                    {
                        LaunchDynamiteProjectile(target);
                    }
                    // 전설의 지팡이: 투사체가 일직선으로 날아가며 경로상 AOE 데미지 (특수 처리)
                    else if (basicAttackData.IsLegendaryStaffSkill)
                    {
                        LaunchLegendaryStaffProjectile(target);
                    }
                    else
                    {
                        UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
                    }
                    break;

                // DOT 스킬 - 범위 내 적에게 지속 데미지 (AOE 방식)
                case SkillAssetType.DOT:
                    UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
                    break;

                // 디버프 스킬 - 범위 내 적에게 디버프 적용 (AOE 방식)
                case SkillAssetType.Debuff:
                    UseAOESkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage, FinalRange, FinalProjectileSpeed).Forget();
                    break;

                // 채널링 스킬 - 지속 시전
                case SkillAssetType.Channeling:
                    UseChannelingSkillAsync(target, basicAttackData, basicAttackPrefabs, FinalDamage).Forget();
                    break;

                // 트랩 스킬 - 필드에 트랩 오브젝트 설치
                case SkillAssetType.Trap:
                    PlaceTrapObject(target, basicAttackData, basicAttackPrefabs, FinalDamage);
                    break;

                // 지뢰 스킬 - 필드에 지뢰 오브젝트 설치
                case SkillAssetType.Mine:
                    PlaceMineObject(target, basicAttackData, basicAttackPrefabs, FinalDamage);
                    break;

                default:
                    Debug.LogWarning($"[Character] Unknown skill type: {skillType}, falling back to TryAttack()");
                    TryAttack();
                    break;
            }
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

        #region 책갈피 시스템 (Issue #320)

        /// <summary>
        /// JML: CharacterPlacementManager에서 호출하여 characterId 설정
        /// </summary>
        public void SetCharacterId(int id)
        {
            characterId = id;
        }

        /// <summary>
        /// JML: characterId 가져오기 (설정되지 않으면 프리팹 이름에서 파싱)
        /// Issue #349: 외부 접근용으로 public 변경
        /// </summary>
        public int GetCharacterId()
        {
            if (characterId > 0) return characterId;

            // Fallback: 프리팹 이름에서 파싱 "Character_01_Slot0" → 1
            string objName = gameObject.name;
            if (objName.Contains("Character_"))
            {
                string idPart = objName.Replace("Character_", "").Split('_')[0];
                if (int.TryParse(idPart, out int parsed))
                {
                    return parsed;
                }
            }
            return -1;
        }

        /// <summary>
        /// JML: 장착된 책갈피 적용 (스탯 + 스킬)
        /// 책갈피가 없으면 프리팹 기본값 사용 (하위 호환성)
        /// </summary>
        private void ApplyBookmarksIfAvailable()
        {
            int charId = GetCharacterId();
            if (charId <= 0) return;

            if (BookMarkManager.Instance == null) return;

            var bookmarks = BookMarkManager.Instance.GetEquippedBookmarksForCharacter(charId);
            if (bookmarks == null || bookmarks.Count == 0) return;

            Debug.Log($"[Character] 책갈피 {bookmarks.Count}개 적용 시작 (CharacterID: {charId})");

            foreach (var bookmark in bookmarks)
            {
                if (bookmark.Type == BookmarkType.Stat)
                {
                    ApplyStatBookmark(bookmark);
                }
                else if (bookmark.Type == BookmarkType.Skill || bookmark.Type == BookmarkType.SubSkill)
                {
                    // JML: Skill(메인스킬)과 SubSkill(서포트스킬) 모두 ApplySkillBookmark로 처리
                    ApplySkillBookmark(bookmark);
                }
            }
        }

        /// <summary>
        /// JML: 스탯 책갈피 적용 (OptionType에 따라 스탯 변형)
        /// </summary>
        private void ApplyStatBookmark(BookMark bookmark)
        {
            switch (bookmark.OptionType)
            {
                case 1: // AttackPower → damageModifier
                    damageModifier += bookmark.OptionValue;
                    Debug.Log($"[Character] 스탯 책갈피: 데미지 +{bookmark.OptionValue}% (총 {damageModifier}%)");
                    break;
                case 2: // AttackSpeed
                    attackSpeedModifier += bookmark.OptionValue;
                    Debug.Log($"[Character] 스탯 책갈피: 공격속도 +{bookmark.OptionValue}% (총 {attackSpeedModifier}%)");
                    break;
                case 3: // ProjectileSpeed
                    projectileSpeedModifier += bookmark.OptionValue;
                    Debug.Log($"[Character] 스탯 책갈피: 투사체속도 +{bookmark.OptionValue}% (총 {projectileSpeedModifier}%)");
                    break;
                case 4: // Range
                    rangeModifier += bookmark.OptionValue;
                    Debug.Log($"[Character] 스탯 책갈피: 사거리 +{bookmark.OptionValue}% (총 {rangeModifier}%)");
                    break;
                default:
                    Debug.LogWarning($"[Character] 알 수 없는 OptionType: {bookmark.OptionType}");
                    break;
            }
        }

        /// <summary>
        /// JML: 스킬 책갈피 적용 (MainSkill → activeSkillId, SupportSkill → supportSkillId)
        /// </summary>
        private void ApplySkillBookmark(BookMark bookmark)
        {
            if (bookmark.SkillID <= 0) return;

            // MainSkillData 확인
            var mainSkill = CSVLoader.Instance?.GetData<MainSkillData>(bookmark.SkillID);
            if (mainSkill != null)
            {
                if (activeSkillId == 0) // 비어있을 때만 설정
                {
                    activeSkillId = bookmark.SkillID;
                    Debug.Log($"[Character] 스킬 책갈피: activeSkillId = {bookmark.SkillID} ({mainSkill.skill_name})");
                }
                return;
            }

            // SupportSkillData 확인
            var supportSkill = CSVLoader.Instance?.GetData<SupportSkillData>(bookmark.SkillID);
            if (supportSkill != null)
            {
                if (supportSkillId == 0) // 비어있을 때만 설정
                {
                    supportSkillId = bookmark.SkillID;
                    Debug.Log($"[Character] 서포트 책갈피: supportSkillId = {bookmark.SkillID} ({supportSkill.support_name})");
                }
            }
        }

        #endregion

        #region 성급 시스템 (Issue #349)

        /// <summary>
        /// JML: 캐릭터 성급 (1~3성, 최대 3성)
        /// 중복 캐릭터 카드 선택 시 성급 업그레이드
        /// </summary>
        private int starTier = 1;
        public const int MAX_STAR_TIER = 3;

        /// <summary>
        /// JML: 현재 성급 반환
        /// </summary>
        public int GetStarTier() => starTier;

        /// <summary>
        /// JML: 최종 성급인지 확인
        /// </summary>
        public bool IsFinalStarTier() => starTier >= MAX_STAR_TIER;

        /// <summary>
        /// JML: 성급 업그레이드 (중복 캐릭터 카드 선택 시)
        /// CardLevelTable.csv의 value_change 값 적용 (배수 방식)
        /// </summary>
        /// <returns>업그레이드 성공 여부</returns>
        public bool UpgradeStarTier()
        {
            if (starTier >= MAX_STAR_TIER)
            {
                Debug.LogWarning($"[Character] 이미 최종 성급입니다! (현재: {starTier}성)");
                return false;
            }

            int oldStarTier = starTier;
            starTier++;

            // CSV에서 성급 배율 가져오기
            float newTierMultiplier = GetStarTierMultiplierFromCSV(starTier);
            float oldTierMultiplier = GetStarTierMultiplierFromCSV(oldStarTier);

            // 증가분 계산 (새 배율 - 이전 배율)
            float upgradeBonus = newTierMultiplier - oldTierMultiplier;

            // 모든 스탯에 버프 적용
            ApplyStatBuff(StatType.Damage, upgradeBonus);
            ApplyStatBuff(StatType.AttackSpeed, upgradeBonus);

            Debug.Log($"[Character] 성급 업그레이드! {oldStarTier}성 → {starTier}성 (배율: {oldTierMultiplier} → {newTierMultiplier}, 증가분: +{upgradeBonus * 100:F0}%)");
            return true;
        }

        /// <summary>
        /// JML: CardLevelTable.csv에서 캐릭터별 성급 배율 가져오기
        /// 캐릭터 ID를 기반으로 CardLevelTable의 해당 행 조회
        /// </summary>
        /// <param name="tier">성급 (1~3)</param>
        /// <returns>배율 값 (1.0, 1.2, 1.5 등)</returns>
        private float GetStarTierMultiplierFromCSV(int tier)
        {
            // CardLevelTable ID 계산
            // 캐릭터 순번 = (characterId % 100) - 1 (021001 → 0, 021002 → 1, ...)
            // 기본 ID = 25025 (그림 Tier1)
            // 각 캐릭터당 3행씩 (Tier 1~3)
            int characterIndex = GetCharacterIndexFromId(characterId);
            int cardLevelId = 25025 + (characterIndex * 3) + (tier - 1);

            var cardLevelTable = CSVLoader.Instance?.GetTable<CardLevelData>();
            if (cardLevelTable == null)
            {
                Debug.LogWarning("[Character] CardLevelTable이 로드되지 않았습니다. 기본값 사용.");
                return GetDefaultTierMultiplier(tier);
            }

            var cardLevelData = cardLevelTable.GetId(cardLevelId);
            if (cardLevelData == null)
            {
                Debug.LogWarning($"[Character] CardLevelTable에서 ID {cardLevelId}를 찾을 수 없습니다. 기본값 사용.");
                return GetDefaultTierMultiplier(tier);
            }

            Debug.Log($"[Character] CSV 성급 배율 조회: CharacterId={characterId}, Tier={tier}, CardLevelId={cardLevelId}, value_change={cardLevelData.value_change}");
            return cardLevelData.value_change;
        }

        /// <summary>
        /// JML: 캐릭터 ID에서 순번 추출 (CardLevelTable 매핑용)
        /// 021001 → 0, 021002 → 1, ... 025020 → 19
        /// </summary>
        private int GetCharacterIndexFromId(int charId)
        {
            // CharacterTable 순서 기준 (0번부터)
            // Horror: 021001~021004 (인덱스 0~3)
            // Romance: 022005~022008 (인덱스 4~7)
            // Adventure: 023009~023012 (인덱스 8~11)
            // Comedy: 024013~024016 (인덱스 12~15)
            // Mystery: 025017~025020 (인덱스 16~19)
            int lastTwoDigits = charId % 100;
            return lastTwoDigits - 1; // 01 → 0, 02 → 1, ...
        }

        /// <summary>
        /// JML: CSV 로드 실패 시 기본 성급 배율
        /// </summary>
        private float GetDefaultTierMultiplier(int tier)
        {
            return tier switch
            {
                1 => 1.0f,
                2 => 1.2f,
                3 => 1.5f,
                _ => 1.0f
            };
        }

        #endregion

        #region 전역 스텟 버프 시스템 (Issue #349)

        /// <summary>
        /// JML: 외부에서 스텟 버프 적용 (StageManager에서 호출)
        /// 전역 스텟 카드 선택 시 모든 캐릭터에 적용
        /// </summary>
        /// <param name="statType">스텟 타입 (StatType enum)</param>
        /// <param name="value">증가 값 (% 단위, 예: 0.1 = 10%)</param>
        public void ApplyStatBuff(StatType statType, float value)
        {
            // value는 비율(0.1 = 10%), modifier는 % 단위로 저장
            float percentValue = value * 100f;

            switch (statType)
            {
                case StatType.Damage:
                case StatType.TotalDamage:
                    damageModifier += percentValue;
                    Debug.Log($"[Character] 데미지 버프 +{percentValue}% (총 {damageModifier}%)");
                    break;

                case StatType.AttackSpeed:
                    attackSpeedModifier += percentValue;
                    Debug.Log($"[Character] 공격속도 버프 +{percentValue}% (총 {attackSpeedModifier}%)");
                    break;

                case StatType.ProjectileSpeed:
                    projectileSpeedModifier += percentValue;
                    Debug.Log($"[Character] 투사체속도 버프 +{percentValue}% (총 {projectileSpeedModifier}%)");
                    break;

                case StatType.Range:
                    rangeModifier += percentValue;
                    Debug.Log($"[Character] 사거리 버프 +{percentValue}% (총 {rangeModifier}%)");
                    break;

                case StatType.CritChance:
                    critChanceModifier += percentValue;
                    Debug.Log($"[Character] 치명타 확률 버프 +{percentValue}% (총 {critChanceModifier}%)");
                    break;

                case StatType.CritMultiplier:
                    critMultiplierModifier += percentValue;
                    Debug.Log($"[Character] 치명타 배율 버프 +{percentValue}% (총 {critMultiplierModifier}%)");
                    break;

                case StatType.BonusDamage:
                    bonusDamageModifier += percentValue;
                    Debug.Log($"[Character] 추가 데미지 버프 +{percentValue}% (총 {bonusDamageModifier}%)");
                    break;

                case StatType.HealthRegen:
                    healthRegenModifier += percentValue;
                    Debug.Log($"[Character] 체력 회복 버프 +{percentValue}% (총 {healthRegenModifier}%)");
                    break;

                default:
                    Debug.LogWarning($"[Character] 알 수 없는 StatType: {statType}");
                    break;
            }
        }

        #endregion

        #region 비주얼 파츠 시스템 (Issue #356)

        /// <summary>
        /// JML: 캐릭터 ID에 따라 비주얼 파츠 활성화/비활성화
        /// </summary>
        private void ApplyVisualConfig(int charId)
        {
            var visualData = CharacterVisualConfig.GetVisualData(charId);
            if (visualData == null)
            {
                Debug.LogWarning($"[Character] 비주얼 데이터 없음 (CharacterID: {charId})");
                return;
            }

            // 캐시 초기화
            CacheTransforms();

            // 1. 모든 Body 비활성화 후 해당 Body만 활성화
            for (int i = 0; i < CharacterVisualConfig.AllBodyParts.Length; i++)
            {
                string partName = CharacterVisualConfig.AllBodyParts[i];
                SetPartActive(partName, partName == visualData.bodyPart);
            }

            // 2. 모든 Hair 비활성화 후 해당 Hair만 활성화
            for (int i = 0; i < CharacterVisualConfig.AllHairParts.Length; i++)
            {
                string partName = CharacterVisualConfig.AllHairParts[i];
                SetPartActive(partName, partName == visualData.hairPart);
            }

            // 3. 모든 Cloak 비활성화 후 해당 Cloak만 활성화
            for (int i = 0; i < CharacterVisualConfig.AllCloakParts.Length; i++)
            {
                string partName = CharacterVisualConfig.AllCloakParts[i];
                bool shouldActivate = !string.IsNullOrEmpty(visualData.cloakPart) && partName == visualData.cloakPart;
                SetPartActive(partName, shouldActivate);
            }

            // 4. weapon_r 자식들 비활성화 후 해당 무기만 활성화
            if (weaponRightSlot != null)
            {
                for (int i = 0; i < CharacterVisualConfig.AllWeaponRightParts.Length; i++)
                {
                    string partName = CharacterVisualConfig.AllWeaponRightParts[i];
                    bool shouldActivate = !string.IsNullOrEmpty(visualData.weaponRight) && partName == visualData.weaponRight;
                    SetWeaponPartActive(weaponRightSlot, partName, shouldActivate);
                }
            }

            // 5. weapon_l 자식들 비활성화 후 해당 방패만 활성화
            if (weaponLeftSlot != null)
            {
                for (int i = 0; i < CharacterVisualConfig.AllWeaponLeftParts.Length; i++)
                {
                    string partName = CharacterVisualConfig.AllWeaponLeftParts[i];
                    bool shouldActivate = !string.IsNullOrEmpty(visualData.weaponLeft) && partName == visualData.weaponLeft;
                    SetWeaponPartActive(weaponLeftSlot, partName, shouldActivate);
                }
            }

            Debug.Log($"[Character] 비주얼 적용 완료 - Body: {visualData.bodyPart}, Hair: {visualData.hairPart}, Cloak: {visualData.cloakPart ?? "없음"}, WeaponR: {visualData.weaponRight}, WeaponL: {visualData.weaponLeft ?? "없음"}");
        }

        /// <summary>
        /// JML: 자식 Transform 캐싱 (성능 최적화)
        /// </summary>
        private void CacheTransforms()
        {
            if (cachedTransforms.Count > 0) return; // 이미 캐싱됨

            // 항상 자기 자신(프리팹 루트)부터 캐싱 - characterObj 무시
            Transform root = transform;
            Debug.Log($"[Character] CacheTransforms - root: {root.name}, childCount: {root.childCount}");
            CacheChildrenRecursive(root);

            Debug.Log($"[Character] 캐싱된 Transform 개수: {cachedTransforms.Count}");

            // Body01 있는지 확인
            if (cachedTransforms.ContainsKey("Body01"))
            {
                Debug.Log("[Character] Body01 찾음!");
            }
            else
            {
                Debug.LogWarning("[Character] Body01 없음! 캐싱된 이름들:");
                int count = 0;
                foreach (var kvp in cachedTransforms)
                {
                    if (count < 30) Debug.Log($"  - {kvp.Key}");
                    count++;
                }
            }

            // weapon_r, weapon_l 슬롯 찾기
            if (cachedTransforms.TryGetValue("weapon_r", out var wr))
            {
                weaponRightSlot = wr;
                Debug.Log("[Character] weapon_r 찾음!");
            }
            if (cachedTransforms.TryGetValue("weapon_l", out var wl))
            {
                weaponLeftSlot = wl;
                Debug.Log("[Character] weapon_l 찾음!");
            }
        }

        /// <summary>
        /// JML: 재귀적으로 모든 자식 Transform 캐싱
        /// </summary>
        private void CacheChildrenRecursive(Transform parent)
        {
            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = parent.GetChild(i);
                string childName = child.name;

                // 중복 이름은 첫 번째만 저장
                if (!cachedTransforms.ContainsKey(childName))
                {
                    cachedTransforms[childName] = child;
                }

                // 재귀 호출
                CacheChildrenRecursive(child);
            }
        }

        /// <summary>
        /// JML: 캐시된 Transform 활성화/비활성화
        /// </summary>
        private void SetPartActive(string partName, bool active)
        {
            if (cachedTransforms.TryGetValue(partName, out var tr))
            {
                tr.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// JML: 무기 슬롯 자식 활성화/비활성화
        /// </summary>
        private void SetWeaponPartActive(Transform weaponSlot, string partName, bool active)
        {
            int childCount = weaponSlot.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = weaponSlot.GetChild(i);
                if (child.name == partName)
                {
                    child.gameObject.SetActive(active);
                    return;
                }
            }
        }

        /// <summary>
        /// LMJ: AOE 스킬의 최적 타겟 위치 계산 (기하학적 중심점 기반)
        /// 사거리 내에서 가장 많은 적이 AOE 범위에 포함되는 위치를 반환
        /// 적 위치뿐만 아니라 기하학적 중심점(centroid)도 후보로 포함
        /// </summary>
        /// <param name="attackRange">캐릭터의 공격 사거리</param>
        /// <param name="aoeRadius">AOE 스킬의 범위 반경</param>
        /// <returns>최적의 AOE 타겟 위치 (적이 없으면 Vector3.zero)</returns>
        private Vector3 FindBestAOETargetPosition(float attackRange, float aoeRadius)
        {
            // 1. 사거리 내 모든 적 찾기
            Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, attackRange);
            List<ITargetable> validTargets = new List<ITargetable>();

            int count = enemiesInRange.Length;
            for (int i = 0; i < count; i++)
            {
                Collider col = enemiesInRange[i];
                if (!col.CompareTag(Tag.Monster) && !col.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable target = col.GetComponent<ITargetable>();
                if (target != null && target.IsAlive())
                {
                    validTargets.Add(target);
                }
            }

            if (validTargets.Count == 0)
            {
                return Vector3.zero;
            }

            // 2. 적이 1마리면 그 위치 반환
            if (validTargets.Count == 1)
            {
                return validTargets[0].GetPosition();
            }

            int targetCount = validTargets.Count;

            // 3. 기하학적 중심점(centroid) 계산
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < targetCount; i++)
            {
                centroid += validTargets[i].GetPosition();
            }
            centroid /= targetCount;

            // 4. 후보 위치 목록 생성 (적 위치들 + 기하학적 중심점)
            List<Vector3> candidatePositions = new List<Vector3>(targetCount + 1);
            for (int i = 0; i < targetCount; i++)
            {
                candidatePositions.Add(validTargets[i].GetPosition());
            }
            candidatePositions.Add(centroid); // 중심점도 후보에 추가

            // 5. 각 후보 위치에서 AOE 범위 내 적 수 계산
            Vector3 bestPosition = Vector3.zero;
            int maxEnemiesInAOE = 0;

            int candidateCount = candidatePositions.Count;
            for (int i = 0; i < candidateCount; i++)
            {
                Vector3 candidatePos = candidatePositions[i];
                int enemiesInAOE = 0;

                // 해당 위치에 AOE를 쏘면 몇 마리가 맞는지 계산
                for (int j = 0; j < targetCount; j++)
                {
                    Vector3 enemyPos = validTargets[j].GetPosition();
                    float distance = Vector3.Distance(candidatePos, enemyPos);
                    if (distance <= aoeRadius)
                    {
                        enemiesInAOE++;
                    }
                }

                // 더 많은 적을 맞출 수 있는 위치 선택
                if (enemiesInAOE > maxEnemiesInAOE)
                {
                    maxEnemiesInAOE = enemiesInAOE;
                    bestPosition = candidatePos;
                }
            }

            bool usedCentroid = (bestPosition == centroid);
            Debug.Log($"[Character] AOE 밀집 지역 타겟팅: {targetCount}마리 중 {maxEnemiesInAOE}마리 적중 예상 (중심점 사용: {usedCentroid}) 위치 = {bestPosition}");
            return bestPosition;
        }

        #endregion

        #region 애니메이션 시스템 (Animation System)

        /// <summary>
        /// JML: 공격 애니메이션 재생
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger(ANIM_ATTACK);
        #region 이펙트 레이어/콜라이더 유틸리티 (Channeling Wall 통과용)

        /// <summary>
        /// LMJ: GameObject와 모든 자식의 레이어를 재귀적으로 설정
        /// Channeling 빔 이펙트가 Wall을 통과하도록 Projectile 레이어로 설정
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;

            obj.layer = layer;

            int childCount = obj.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
            }
        }

        /// <summary>
        /// JML: 사망 애니메이션 재생 (게임 패배 시)
        /// </summary>
        public void PlayDieAnimation()
        {
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger(ANIM_DIE);
            }
        }

        /// <summary>
        /// JML: 승리 애니메이션 재생 (스테이지 클리어 시)
        /// </summary>
        public void PlayVictoryAnimation()
        {
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger(ANIM_VICTORY);
        /// LMJ: GameObject와 모든 자식의 Collider를 재귀적으로 비활성화
        /// Channeling 빔은 시각적 효과만 필요하므로 물리 충돌 제거
        /// </summary>
        private void DisableCollidersRecursively(GameObject obj)
        {
            if (obj == null) return;

            // 현재 오브젝트의 모든 Collider 비활성화
            Collider[] colliders = obj.GetComponents<Collider>();
            int colCount = colliders.Length;
            for (int i = 0; i < colCount; i++)
            {
                colliders[i].enabled = false;
            }

            // 자식들도 재귀적으로 처리
            int childCount = obj.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                DisableCollidersRecursively(obj.transform.GetChild(i).gameObject);
            }
        }

        #endregion
    }
}
