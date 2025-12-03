//LMJ : Character with simple projectile-based combat (Issue #265)
//     Migrated to new CSV-based skill system
namespace Novelian.Combat
{
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;

    public class Character : MonoBehaviour, IPoolable
    {
        [Header("Character Visual")]
        [SerializeField] private GameObject characterObj;

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
                // DamageCalculator 사용
                float baseDamage = DamageCalculator.CalculateSingleDamage(basicAttackData.base_damage, levelMult, 1f);
                // 캐릭터 변형 적용
                return baseDamage * (1f + damageModifier / 100f);
            }
        }

        private float FinalAttackSpeed => basicAttackData != null
            ? (1f / basicAttackData.cooldown) * (1f + attackSpeedModifier / 100f)
            : 1f;

        private float FinalProjectileSpeed => basicAttackData != null
            ? basicAttackData.projectile_speed * (1f + projectileSpeedModifier / 100f)
            : 10f;

        private float FinalRange => basicAttackData != null
            ? basicAttackData.range * (1f + rangeModifier / 100f)
            : 1000f;

        private float FinalProjectileLifetime => basicAttackData != null
            ? basicAttackData.skill_lifetime
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
                float attackSpeed = (1f / activeSkillData.cooldown) * (1f + attackSpeedModifier / 100f);
                if (supportData != null) attackSpeed *= supportData.attack_speed_mult;
                return attackSpeed;
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

        // Active Skill 발사체 개수 (Support 스킬이 있으면 총 개수로 대체)
        private int FinalActiveProjectileCount
        {
            get
            {
                if (activeSkillData == null) return 1;

                // Support 스킬이 있고 add_projectiles > 0이면 총 개수로 사용
                if (supportData != null && supportData.add_projectiles > 0)
                {
                    return supportData.add_projectiles;
                }

                // Support 스킬이 없으면 Active Skill의 기본 개수 사용
                return activeSkillData.projectile_count;
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

        private void Start()
        {
            // Initialize()로 이미 초기화되었으면 스킵
            if (isManuallyInitialized) return;

            // 기존 방식 (프리팹 Inspector 값 사용) - 하위 호환성
            ApplyBookmarksIfAvailable();
            LoadSkillData();
            InitializeProjectilePool();
            InitializeActiveSkillPool();
            StartAttackLoop();
            StartActiveSkillLoop();
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

            // Support Skill
            if (supportSkillId > 0)
            {
                supportData = CSVLoader.Instance.GetData<SupportSkillData>(supportSkillId);
                if (supportData != null)
                {
                    supportPrefabs = prefabDb?.GetSupportSkillEntry(supportSkillId);
                    Debug.Log($"[Character] Loaded support skill: {supportData.support_name} (ID: {supportSkillId})");
                }
                else
                {
                    Debug.LogWarning($"[Character] Support skill ID {supportSkillId} not found in CSV!");
                }
            }
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

            // Find target with mark priority, then use weight/distance strategy
            ITargetable target = TargetRegistry.Instance.FindTarget(transform.position, FinalRange, useWeightTargeting);

            if (target == null)
            {
                // 타겟이 없으면 공격 스킵 (정상적인 상황)
                return;
            }

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // Get projectile prefab from database
            GameObject projectilePrefab = basicAttackPrefabs?.projectilePrefab;
            GameObject hitEffectPrefab = basicAttackPrefabs?.hitEffectPrefab;


            // Launch projectile
            if (projectilePrefab != null || projectileTemplate != null)
            {
                var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;
                Projectile projectile = pool.Spawn<Projectile>(spawnPos);
                projectile.Launch(spawnPos, targetPos, FinalProjectileSpeed, FinalProjectileLifetime, FinalDamage, basicAttackSkillId, 0);

            }
            // Instant attack (no projectile)
            else
            {
                // Hit effect
                if (hitEffectPrefab != null)
                {
                    GameObject hitEffect = Object.Instantiate(hitEffectPrefab, targetPos, Quaternion.identity);
                    Object.Destroy(hitEffect, 2f);
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

        //LMJ : Attempt to use active skill on target
        private void TryUseActiveSkill()
        {
            if (!isInitialized || activeSkillData == null) return;

            // Skip if already channeling
            if (isChanneling) return;

            // Find target with mark priority, then use weight/distance strategy
            ITargetable target = TargetRegistry.Instance.FindTarget(transform.position, FinalActiveRange, useWeightTargeting);

            if (target == null) return;

            // Check if skill is Channeling type
            if (activeSkillData.GetSkillType() == SkillAssetType.Channeling)
            {
                UseChannelingSkillAsync(target).Forget();
                return;
            }

            // Check if skill is AOE type (Meteor, etc.)
            if (activeSkillData.GetSkillType() == SkillAssetType.AOE)
            {
                UseAOESkillAsync(target).Forget();
                return;
            }

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // Get prefabs from database
            GameObject projectilePrefab = activeSkillPrefabs?.projectilePrefab;

            // Launch projectiles (fan pattern)
            if (projectilePrefab != null || projectileTemplate != null)
            {
                var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;
                int projectileCount = FinalActiveProjectileCount;
                float spreadAngle = 15f; // 각 발사체 간 각도 (도 단위)

                for (int i = 0; i < projectileCount; i++)
                {
                    // 부채꼴 각도 계산: 중앙을 0도로 하여 좌우로 퍼짐
                    float angleOffset = (i - (projectileCount - 1) / 2f) * spreadAngle;

                    // 타겟 방향 벡터 계산
                    Vector3 direction = (targetPos - spawnPos).normalized;

                    // Y축 회전을 적용하여 부채꼴 방향 계산
                    Quaternion rotation = Quaternion.Euler(0, angleOffset, 0);
                    Vector3 spreadDirection = rotation * direction;

                    // 새로운 타겟 위치 계산 (멀리 떨어진 지점)
                    Vector3 spreadTargetPos = spawnPos + spreadDirection * 1000f;

                    // 발사체 생성 및 발사 (Support 스킬 ID 전달)
                    Projectile projectile = pool.Spawn<Projectile>(spawnPos);
                    projectile.Launch(spawnPos, spreadTargetPos, FinalActiveProjectileSpeed, FinalActiveProjectileLifetime, FinalActiveDamage, activeSkillId, supportSkillId);
                }

                Debug.Log($"[Character] Used Active Skill (projectile x{projectileCount}): {activeSkillData.skill_name} at {target.GetTransform().name} (Damage: {FinalActiveDamage:F1})");
            }
            // Instant attack
            else
            {
                GameObject hitEffectPrefab = activeSkillPrefabs?.hitEffectPrefab;

                // Hit effect
                if (hitEffectPrefab != null)
                {
                    GameObject hitEffect = Object.Instantiate(hitEffectPrefab, targetPos, Quaternion.identity);
                    Object.Destroy(hitEffect, 2f);
                }

                // Apply damage
                if (target.GetTransform().CompareTag(Tag.Monster))
                {
                    Monster monster = target.GetTransform().GetComponent<Monster>();
                    if (monster != null) monster.TakeDamage(FinalActiveDamage);
                }
                else if (target.GetTransform().CompareTag(Tag.BossMonster))
                {
                    BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                    if (boss != null) boss.TakeDamage(FinalActiveDamage);
                }

                Debug.Log($"[Character] Active instant attack {activeSkillData.skill_name} at {target.GetTransform().name} (Instant Damage: {FinalActiveDamage:F1})");
            }
        }

        //LMJ : Use channeling skill (laser/beam style)
        private async UniTaskVoid UseChannelingSkillAsync(ITargetable target)
        {
            if (activeSkillData == null || activeSkillData.GetSkillType() != SkillAssetType.Channeling) return;

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
                Debug.Log($"[Character] Starting channeling skill: {activeSkillData.skill_name}");

                // Get prefabs
                GameObject castEffectPrefab = activeSkillPrefabs?.castEffectPrefab;
                GameObject projectileEffectPrefab = activeSkillPrefabs?.projectilePrefab;
                GameObject areaEffectPrefab = activeSkillPrefabs?.areaEffectPrefab;
                GameObject hitEffectPrefab = activeSkillPrefabs?.hitEffectPrefab;

                // 1. Cast Effect (시전 준비)
                if (activeSkillData.cast_time > 0f && castEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    castEffect = Object.Instantiate(castEffectPrefab, spawnPos, Quaternion.identity);
                    Debug.Log($"[Character] Cast Effect started ({activeSkillData.cast_time:F1}s)");

                    await UniTask.Delay((int)(activeSkillData.cast_time * 1000), cancellationToken: ct);

                    if (castEffect != null) Object.Destroy(castEffect);
                }

                // Check if target is still valid after cast time
                if (target == null || !target.IsAlive())
                {
                    Debug.Log("[Character] Channeling cancelled: Target died during cast");
                    return;
                }

                // 2. Start Effect (빔 발사 지점)
                if (projectileEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    startEffect = Object.Instantiate(projectileEffectPrefab, spawnPos, Quaternion.identity);
                    startEffect.transform.SetParent(transform);
                    Debug.Log("[Character] Start Effect spawned");
                }

                // 3. Build chain targets (if Chain support skill is active)
                System.Collections.Generic.List<ITargetable> chainTargets = BuildChainTargets(target);

                // 4. Create beam effects for all targets
                if (areaEffectPrefab != null)
                {
                    for (int i = 0; i < chainTargets.Count; i++)
                    {
                        Vector3 spawnPos = (i == 0) ? transform.position + spawnOffset : chainTargets[i - 1].GetPosition();
                        GameObject beamEffect = Object.Instantiate(areaEffectPrefab, spawnPos, Quaternion.identity);
                        beamEffects.Add(beamEffect);
                    }
                    Debug.Log($"[Character] Created {beamEffects.Count} beam effects for {chainTargets.Count} targets");
                }

                // 5. Create hit effects for all targets
                if (hitEffectPrefab != null)
                {
                    for (int i = 0; i < chainTargets.Count; i++)
                    {
                        GameObject hitEffect = Object.Instantiate(hitEffectPrefab, chainTargets[i].GetPosition(), Quaternion.identity);
                        hitEffect.transform.SetParent(chainTargets[i].GetTransform());
                        hitEffects.Add(hitEffect);
                    }
                }

                // 6. Channeling loop
                float elapsed = 0f;
                float nextTickTime = 0f;
                int tickCount = 0;
                bool firstTick = true;

                while (elapsed < activeSkillData.channel_duration)
                {
                    // Update beam effects and clean up dead targets
                    for (int i = 0; i < beamEffects.Count && i < chainTargets.Count; i++)
                    {
                        if (chainTargets[i] == null || !chainTargets[i].IsAlive())
                        {
                            if (beamEffects[i] != null) Object.Destroy(beamEffects[i]);
                            beamEffects[i] = null;

                            if (i < hitEffects.Count && hitEffects[i] != null)
                            {
                                Object.Destroy(hitEffects[i]);
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
                        float currentDamage = FinalActiveDamage;

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
                        }

                        tickCount++;
                        nextTickTime += activeSkillData.channel_tick_interval;
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
                if (castEffect != null) Object.Destroy(castEffect);
                if (startEffect != null) Object.Destroy(startEffect);
                foreach (var beam in beamEffects)
                {
                    if (beam != null) Object.Destroy(beam);
                }
                foreach (var hitEffect in hitEffects)
                {
                    if (hitEffect != null) Object.Destroy(hitEffect);
                }

                isChanneling = false;
            }
        }

        //LMJ : Use AOE skill (Meteor style)
        private async UniTaskVoid UseAOESkillAsync(ITargetable target)
        {
            if (activeSkillData == null || activeSkillData.GetSkillType() != SkillAssetType.AOE) return;

            GameObject castEffect = null;
            GameObject meteorEffect = null;
            GameObject hitEffect = null;

            try
            {
                Debug.Log($"[Character] Starting AOE skill: {activeSkillData.skill_name}");

                // Get prefabs
                GameObject castEffectPrefab = activeSkillPrefabs?.castEffectPrefab;
                GameObject projectileEffectPrefab = activeSkillPrefabs?.projectilePrefab;
                GameObject hitEffectPrefab = activeSkillPrefabs?.hitEffectPrefab;

                // 1. Cast Effect
                if (activeSkillData.cast_time > 0f && castEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    castEffect = Object.Instantiate(castEffectPrefab, spawnPos, Quaternion.identity);

                    await UniTask.Delay((int)(activeSkillData.cast_time * 1000));

                    if (castEffect != null) Object.Destroy(castEffect);
                }

                // 2. Get target position
                Vector3 targetPos;
                if (target != null && target.IsAlive())
                {
                    targetPos = target.GetPosition();
                }
                else
                {
                    ITargetable newTarget = TargetRegistry.Instance.FindTarget(transform.position, FinalActiveRange, useWeightTargeting);
                    if (newTarget != null)
                    {
                        targetPos = newTarget.GetPosition();
                    }
                    else
                    {
                        Debug.Log("[Character] AOE cancelled: No valid targets");
                        return;
                    }
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

                // 4. Meteor Effect
                if (projectileEffectPrefab != null)
                {
                    Vector3 meteorStartPos = impactPos + Vector3.up * 20f;
                    meteorEffect = Object.Instantiate(projectileEffectPrefab, meteorStartPos, Quaternion.identity);

                    float meteorSpeed = FinalActiveProjectileSpeed > 0 ? FinalActiveProjectileSpeed : 10f;
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

                // 5. Hit Effect
                if (hitEffectPrefab != null)
                {
                    hitEffect = Object.Instantiate(hitEffectPrefab, impactPos, Quaternion.identity);
                }

                // 6. AOE damage
                float aoeRadius = activeSkillData.aoe_radius > 0 ? activeSkillData.aoe_radius : 3f;
                Collider[] hits = Physics.OverlapSphere(impactPos, aoeRadius);
                float damageToApply = FinalActiveDamage;

                foreach (var hit in hits)
                {
                    if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                        continue;

                    ITargetable hitTarget = hit.GetComponent<ITargetable>();
                    if (hitTarget == null || !hitTarget.IsAlive())
                        continue;

                    hitTarget.TakeDamage(damageToApply);

                    // Apply status effects
                    if (supportData != null && supportData.GetStatusEffectType() != StatusEffectType.None)
                    {
                        ApplyStatusEffect(hitTarget);
                    }
                }

                // Cleanup
                if (meteorEffect != null)
                {
                    Object.Destroy(meteorEffect, 0.1f);
                }
                if (hitEffect != null)
                {
                    Object.Destroy(hitEffect, 2f);
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[Character] AOE skill cancelled");
            }
            finally
            {
                if (castEffect != null) Object.Destroy(castEffect);
            }
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
                else if (bookmark.Type == BookmarkType.Skill)
                {
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
    }
}
