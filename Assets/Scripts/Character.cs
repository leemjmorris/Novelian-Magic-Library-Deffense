//LMJ : Character with simple projectile-based combat (Issue #265)
namespace Novelian.Combat
{
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;

    public class Character : MonoBehaviour, IPoolable
    {
        [Header("Character Visual")]
        [SerializeField] private GameObject characterObj;

        [Header("스킬 장착 (Skill Equipment)")]
        [SerializeField, Tooltip("기본 공격 스킬 (Basic Attack Skill)")]
        private SkillAssetData basicAttackSkill;

        [SerializeField, Tooltip("액티브 스킬 (Active Skill - 자동 실행)")]
        private SkillAssetData activeSkill;

        [SerializeField, Tooltip("보조 스킬 (Support Skill - Active Skill에 영향)")]
        private SkillAssetData supportSkill;

        [Header("캐릭터 스텟 변형 (%) (Character Stat Modifiers)")]
        [SerializeField, Tooltip("데미지 변형 (%)")]
        private float damageModifier = 0f;

        [SerializeField, Tooltip("공격 속도 변형 (%)")]
        private float attackSpeedModifier = 0f;

        [SerializeField, Tooltip("투사체 속도 변형 (%)")]
        private float projectileSpeedModifier = 0f;

        [SerializeField, Tooltip("사거리 변형 (%)")]
        private float rangeModifier = 0f;

        [Header("Spawn Position")]
        [SerializeField, Tooltip("Projectile spawn offset (Y=1.5 for chest height)")]
        private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Projectile Template")]
        [SerializeField, Tooltip("Generic projectile template (used when skill has no projectile prefab)")]
        private GameObject projectileTemplate;

        [Header("Targeting Strategy")]
        [SerializeField, Tooltip("Use weight-based targeting (default: distance-based)")]
        private bool useWeightTargeting = false;

        // 최종 수치 계산 프로퍼티 (스킬 기본값 × 캐릭터 변형)
        private float FinalDamage => basicAttackSkill != null
            ? basicAttackSkill.baseDamage * (1f + damageModifier / 100f)
            : 0f;

        private float FinalAttackSpeed => basicAttackSkill != null
            ? (1f / basicAttackSkill.cooldown) * (1f + attackSpeedModifier / 100f)
            : 1f;

        private float FinalProjectileSpeed => basicAttackSkill != null
            ? basicAttackSkill.projectileSpeed * (1f + projectileSpeedModifier / 100f)
            : 10f;

        private float FinalRange => basicAttackSkill != null
            ? basicAttackSkill.range * (1f + rangeModifier / 100f)
            : 1000f;

        private float FinalProjectileLifetime => basicAttackSkill != null
            ? basicAttackSkill.projectileLifetime
            : 5f;

        // Active Skill 최종 수치 계산 프로퍼티 (캐릭터 변형 + Support 스킬 변형)
        private float FinalActiveDamage
        {
            get
            {
                if (activeSkill == null) return 0f;
                float damage = activeSkill.baseDamage * (1f + damageModifier / 100f);
                if (supportSkill != null) damage *= (1f + supportSkill.damageModifier / 100f);
                return damage;
            }
        }

        private float FinalActiveAttackSpeed
        {
            get
            {
                if (activeSkill == null) return 1f;
                float attackSpeed = (1f / activeSkill.cooldown) * (1f + attackSpeedModifier / 100f);
                if (supportSkill != null) attackSpeed *= (1f + supportSkill.attackSpeedModifier / 100f);
                return attackSpeed;
            }
        }

        private float FinalActiveProjectileSpeed
        {
            get
            {
                if (activeSkill == null) return 10f;
                float speed = activeSkill.projectileSpeed * (1f + projectileSpeedModifier / 100f);
                if (supportSkill != null) speed *= (1f + supportSkill.projectileSpeedMultiplier / 100f);
                return speed;
            }
        }

        private float FinalActiveRange => activeSkill != null
            ? activeSkill.range * (1f + rangeModifier / 100f)
            : 1000f;

        private float FinalActiveProjectileLifetime
        {
            get
            {
                if (activeSkill == null) return 5f;
                float lifetime = activeSkill.projectileLifetime;
                if (supportSkill != null) lifetime *= (1f + supportSkill.durationMultiplier / 100f);
                return lifetime;
            }
        }

        // Active Skill 발사체 개수 (Support 스킬이 있으면 총 개수로 대체)
        private int FinalActiveProjectileCount
        {
            get
            {
                if (activeSkill == null) return 1;

                // Support 스킬이 있고 additionalProjectiles > 0이면 총 개수로 사용
                if (supportSkill != null && supportSkill.additionalProjectiles > 0)
                {
                    return supportSkill.additionalProjectiles;
                }

                // Support 스킬이 없으면 Active Skill의 기본 개수 사용
                return activeSkill.projectileCount;
            }
        }

        // Attack state
        private CancellationTokenSource attackCts;
        private CancellationTokenSource activeSkillCts;
        private CancellationTokenSource channelingCts;
        private bool isInitialized = false;
        private bool isChanneling = false;

        private void Start()
        {
            InitializeProjectilePool();
            InitializeActiveSkillPool();
            StartAttackLoop();
            StartActiveSkillLoop();
            isInitialized = true;
        }

        //LMJ : Initialize projectile pool (from basic attack skill)
        private void InitializeProjectilePool()
        {
            if (basicAttackSkill == null)
            {
                Debug.LogError("[Character] basicAttackSkill is null!");
                return;
            }

            // Effect만 있는 경우 ProjectileTemplate 사용
            if (basicAttackSkill.projectileEffectPrefab == null)
            {
                Debug.LogWarning($"[Character] {basicAttackSkill.skillName} has no effect prefab. Cannot fire skill.");
                return;
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
                Debug.Log($"[Character] Projectile pool initialized with ProjectileTemplate for skill: {basicAttackSkill.skillName}");
            }
        }

        //LMJ : Initialize active skill projectile pool
        private void InitializeActiveSkillPool()
        {
            if (activeSkill == null)
            {
                Debug.LogWarning("[Character] activeSkill is null. Skipping active skill initialization.");
                return;
            }

            // Effect만 있는 경우 ProjectileTemplate 사용
            if (activeSkill.projectileEffectPrefab == null)
            {
                Debug.LogWarning($"[Character] {activeSkill.skillName} has no effect prefab. Cannot fire skill.");
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
                Debug.Log($"[Character] Active skill pool initialized with ProjectileTemplate for skill: {activeSkill.skillName}");
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
            if (activeSkill == null)
            {
                Debug.LogWarning("[Character] activeSkill is null. Skipping active skill loop.");
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
        //      Priority: Focus Mark > useWeightTargeting ? Weight : Distance
        private void TryAttack()
        {
            if (!isInitialized || basicAttackSkill == null) return;

            // Find target with mark priority, then use weight/distance strategy
            ITargetable target = TargetRegistry.Instance.FindTarget(transform.position, FinalRange, useWeightTargeting);

            if (target == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // Effect가 있으면 ProjectileTemplate 발사
            if (basicAttackSkill.projectileEffectPrefab != null)
            {
                var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;
                Projectile projectile = pool.Spawn<Projectile>(spawnPos);
                projectile.Launch(spawnPos, targetPos, FinalProjectileSpeed, FinalProjectileLifetime, FinalDamage, basicAttackSkill, null);

                Debug.Log($"[Character] Fired projectile {basicAttackSkill.skillName} at {target.GetTransform().name} (Damage: {FinalDamage:F1})");
            }
            // Effect도 없으면 즉발 공격
            else
            {
                // 피격 이펙트만 표시
                if (basicAttackSkill.hitEffectPrefab != null)
                {
                    GameObject hitEffect = Object.Instantiate(basicAttackSkill.hitEffectPrefab, targetPos, Quaternion.identity);
                    Object.Destroy(hitEffect, 2f);
                }

                // 즉시 데미지 적용
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

                Debug.Log($"[Character] Instant attack {basicAttackSkill.skillName} at {target.GetTransform().name} (Instant Damage: {FinalDamage:F1})");
            }
        }

        //LMJ : Attempt to use active skill on target
        //      Priority: Focus Mark > useWeightTargeting ? Weight : Distance
        private void TryUseActiveSkill()
        {
            if (!isInitialized || activeSkill == null) return;

            // Skip if already channeling
            if (isChanneling) return;

            // Find target with mark priority, then use weight/distance strategy
            ITargetable target = TargetRegistry.Instance.FindTarget(transform.position, FinalActiveRange, useWeightTargeting);

            if (target == null) return;

            // Check if skill is Channeling type
            if (activeSkill.skillType == SkillAssetType.Channeling)
            {
                UseChannelingSkillAsync(target).Forget();
                return;
            }

            // Check if skill is AOE type (Meteor, etc.)
            if (activeSkill.skillType == SkillAssetType.AOE)
            {
                UseAOESkillAsync(target).Forget();
                return;
            }

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // Effect가 있으면 ProjectileTemplate으로 다중 발사체 부채꼴 발사
            if (activeSkill.projectileEffectPrefab != null)
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

                    // 발사체 생성 및 발사 (Support 스킬 데이터 전달)
                    Projectile projectile = pool.Spawn<Projectile>(spawnPos);
                    projectile.Launch(spawnPos, spreadTargetPos, FinalActiveProjectileSpeed, FinalActiveProjectileLifetime, FinalActiveDamage, activeSkill, supportSkill);

                    // Debug: Support 스킬 전달 확인
                    if (supportSkill != null)
                    {
                        Debug.Log($"[Character] Support Skill passed to projectile: {supportSkill.skillName}, StatusEffect: {supportSkill.statusEffectType}");
                    }
                }

                Debug.Log($"[Character] Used Active Skill (projectile x{projectileCount}): {activeSkill.skillName} at {target.GetTransform().name} (Damage: {FinalActiveDamage:F1})");
            }
            // Effect도 없으면 즉발 공격
            else
            {
                // 피격 이펙트만 표시
                if (activeSkill.hitEffectPrefab != null)
                {
                    GameObject hitEffect = Object.Instantiate(activeSkill.hitEffectPrefab, targetPos, Quaternion.identity);
                    Object.Destroy(hitEffect, 2f);
                }

                // 즉시 데미지 적용
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

                Debug.Log($"[Character] Active instant attack {activeSkill.skillName} at {target.GetTransform().name} (Instant Damage: {FinalActiveDamage:F1})");
            }
        }

        //LMJ : Use channeling skill (laser/beam style)
        private async UniTaskVoid UseChannelingSkillAsync(ITargetable target)
        {
            if (activeSkill == null || activeSkill.skillType != SkillAssetType.Channeling) return;

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
                Debug.Log($"[Character] Starting channeling skill: {activeSkill.skillName}");

                // 1. Cast Effect (시전 준비)
                if (activeSkill.castTime > 0f && activeSkill.castEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    castEffect = Object.Instantiate(activeSkill.castEffectPrefab, spawnPos, Quaternion.identity);
                    Debug.Log($"[Character] Cast Effect started ({activeSkill.castTime:F1}s)");

                    await UniTask.Delay((int)(activeSkill.castTime * 1000), cancellationToken: ct);

                    if (castEffect != null) Object.Destroy(castEffect);
                }

                // Check if target is still valid after cast time
                if (target == null || !target.IsAlive())
                {
                    Debug.Log("[Character] Channeling cancelled: Target died during cast");
                    return;
                }

                // 2. Start Effect (빔 발사 지점, 선택)
                if (activeSkill.projectileEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    startEffect = Object.Instantiate(activeSkill.projectileEffectPrefab, spawnPos, Quaternion.identity);
                    startEffect.transform.SetParent(transform); // Follow character
                    Debug.Log("[Character] Start Effect spawned");
                }

                // 3. Build chain targets (if Chain support skill is active)
                System.Collections.Generic.List<ITargetable> chainTargets = BuildChainTargets(target);

                // 4. Create beam effects for all targets
                if (activeSkill.areaEffectPrefab != null)
                {
                    for (int i = 0; i < chainTargets.Count; i++)
                    {
                        Vector3 spawnPos = (i == 0) ? transform.position + spawnOffset : chainTargets[i - 1].GetPosition();
                        GameObject beamEffect = Object.Instantiate(activeSkill.areaEffectPrefab, spawnPos, Quaternion.identity);
                        beamEffects.Add(beamEffect);
                    }
                    Debug.Log($"[Character] Created {beamEffects.Count} beam effects for {chainTargets.Count} targets");
                }
                else
                {
                    Debug.LogWarning("[Character] Channeling skill has no Beam Effect (areaEffectPrefab)!");
                }

                // 5. Create hit effects for all targets (follow targets)
                if (activeSkill.hitEffectPrefab != null)
                {
                    for (int i = 0; i < chainTargets.Count; i++)
                    {
                        GameObject hitEffect = Object.Instantiate(activeSkill.hitEffectPrefab, chainTargets[i].GetPosition(), Quaternion.identity);
                        hitEffect.transform.SetParent(chainTargets[i].GetTransform()); // Follow target
                        hitEffects.Add(hitEffect);
                    }
                    Debug.Log($"[Character] Created {hitEffects.Count} hit effects following targets");
                }

                // 6. Channeling loop (channelDuration 동안)
                float elapsed = 0f;
                float nextTickTime = 0f;
                int tickCount = 0;
                bool firstTick = true; // Track first tick for status effects

                while (elapsed < activeSkill.channelDuration)
                {
                    // Update beam effects position/rotation and clean up dead targets
                    for (int i = 0; i < beamEffects.Count && i < chainTargets.Count; i++)
                    {
                        if (chainTargets[i] == null || !chainTargets[i].IsAlive())
                        {
                            // Remove dead target's beam and hit effect
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

                    // Apply damage and effects at tick intervals
                    if (elapsed >= nextTickTime)
                    {
                        float currentDamage = FinalActiveDamage;

                        for (int i = 0; i < chainTargets.Count; i++)
                        {
                            if (chainTargets[i] == null || !chainTargets[i].IsAlive())
                                continue;

                            // Apply chain damage reduction
                            if (i > 0 && supportSkill != null && supportSkill.statusEffectType == StatusEffectType.Chain)
                            {
                                currentDamage *= (1f - supportSkill.chainDamageReduction / 100f);
                            }

                            // Apply status effects (only on first tick)
                            if (firstTick && supportSkill != null && supportSkill.statusEffectType != StatusEffectType.Chain)
                            {
                                ApplyStatusEffect(chainTargets[i]);
                            }

                            // Apply damage
                            chainTargets[i].TakeDamage(currentDamage);

                            Debug.Log($"[Character] Channeling tick {tickCount} target {i}: {currentDamage:F1} damage to {chainTargets[i].GetTransform().name}");
                        }

                        tickCount++;
                        nextTickTime += activeSkill.channelTickInterval;
                        firstTick = false;
                    }

                    // Wait one frame
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
                // Clean up effects
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
                Debug.Log("[Character] Channeling ended, effects cleaned up");
            }
        }

        //LMJ : Use AOE skill (Meteor style - falls from sky, ground collision)
        private async UniTaskVoid UseAOESkillAsync(ITargetable target)
        {
            if (activeSkill == null || activeSkill.skillType != SkillAssetType.AOE) return;

            GameObject castEffect = null;
            GameObject meteorEffect = null;
            GameObject hitEffect = null;

            try
            {
                Debug.Log($"[Character] Starting AOE skill: {activeSkill.skillName}, Damage: {FinalActiveDamage:F1}");

                // 1. Cast Effect (시전 준비) - 캐릭터 위치에서 재생
                if (activeSkill.castTime > 0f && activeSkill.castEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + spawnOffset;
                    castEffect = Object.Instantiate(activeSkill.castEffectPrefab, spawnPos, Quaternion.identity);
                    Debug.Log($"[Character] AOE Cast Effect started ({activeSkill.castTime:F1}s)");

                    await UniTask.Delay((int)(activeSkill.castTime * 1000));

                    if (castEffect != null) Object.Destroy(castEffect);
                }

                // 2. castTime 후 타겟 위치 다시 가져오기 (몬스터가 이동했을 수 있음)
                // 타겟이 죽었으면 마지막 위치 사용
                Vector3 targetPos;
                if (target != null && target.IsAlive())
                {
                    targetPos = target.GetPosition();
                }
                else
                {
                    // 타겟이 죽었으면 새 타겟 찾기
                    ITargetable newTarget = TargetRegistry.Instance.FindTarget(transform.position, FinalActiveRange, useWeightTargeting);
                    if (newTarget != null)
                    {
                        targetPos = newTarget.GetPosition();
                        Debug.Log($"[Character] Original target died, using new target at {targetPos}");
                    }
                    else
                    {
                        Debug.Log("[Character] AOE cancelled: No valid targets");
                        return;
                    }
                }

                // 3. 착탄 위치는 지면 좌표로 변환 (Meteor-style)
                Vector3 impactPos = targetPos;

                // Raycast로 지면 위치 찾기
                Ray groundRay = new Ray(targetPos + Vector3.up * 10f, Vector3.down);
                if (Physics.Raycast(groundRay, out RaycastHit groundHit, 20f, LayerMask.GetMask("Ground")))
                {
                    impactPos = groundHit.point;
                    Debug.Log($"[Character] AOE ground impact: {impactPos} (raycast hit)");
                }
                else
                {
                    // Ground 레이어 못 찾으면 Y=0으로 설정
                    impactPos = new Vector3(targetPos.x, 0f, targetPos.z);
                    Debug.Log($"[Character] AOE ground impact: {impactPos} (fallback Y=0)");
                }

                // 4. Meteor Effect (몬스터 스폰 위치에서 출발 → 착탄 지점으로 이동)
                if (activeSkill.projectileEffectPrefab != null)
                {
                    // 몬스터 스폰 위치 가져오기 (WaveManager에서)
                    Vector3 meteorStartPos = impactPos + Vector3.up * 20f; // 기본값: 착탄점 위 20m
                    if (NovelianMagicLibraryDefense.Managers.GameManager.Instance?.Wave != null)
                    {
                        // WaveManager의 monsterSpawner 위치 사용
                        var waveManager = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Wave;
                        var spawnerField = waveManager.GetType().GetField("monsterSpawner",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (spawnerField != null)
                        {
                            var spawner = spawnerField.GetValue(waveManager) as NovelianMagicLibraryDefense.Spawners.MonsterSpawner;
                            if (spawner != null)
                            {
                                meteorStartPos = spawner.transform.position + Vector3.up * 10f; // 스폰 위치 + 높이
                                Debug.Log($"[Character] Meteor start from MonsterSpawner: {meteorStartPos}");
                            }
                        }
                    }

                    meteorEffect = Object.Instantiate(activeSkill.projectileEffectPrefab, meteorStartPos, Quaternion.identity);
                    Debug.Log($"[Character] Meteor spawned at {meteorStartPos}, moving to {impactPos}");

                    // Meteor 이동 애니메이션 (projectileSpeed 사용)
                    float meteorSpeed = FinalActiveProjectileSpeed > 0 ? FinalActiveProjectileSpeed : 10f;
                    float distance = Vector3.Distance(meteorStartPos, impactPos);
                    float travelTime = distance / meteorSpeed;

                    Debug.Log($"[Character] Meteor travel: distance={distance:F1}m, speed={meteorSpeed:F1}, time={travelTime:F2}s");

                    // Lerp로 이동 애니메이션
                    float elapsed = 0f;
                    while (elapsed < travelTime && meteorEffect != null)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / travelTime);
                        meteorEffect.transform.position = Vector3.Lerp(meteorStartPos, impactPos, t);

                        // Meteor가 착탄 지점을 바라보도록 회전
                        Vector3 direction = (impactPos - meteorStartPos).normalized;
                        if (direction != Vector3.zero)
                        {
                            meteorEffect.transform.rotation = Quaternion.LookRotation(direction);
                        }

                        await UniTask.Yield(PlayerLoopTiming.Update);
                    }

                    // 최종 위치 보정
                    if (meteorEffect != null)
                    {
                        meteorEffect.transform.position = impactPos;
                    }
                }

                // 5. Hit Effect (착탄 폭발) - 착탄 위치에서 재생
                if (activeSkill.hitEffectPrefab != null)
                {
                    hitEffect = Object.Instantiate(activeSkill.hitEffectPrefab, impactPos, Quaternion.identity);
                    Debug.Log($"[Character] AOE Hit Effect spawned at {impactPos}");
                }

                // 6. AOE 범위 데미지 적용 (착탄 위치 기준)
                float aoeRadius = activeSkill.aoeRadius > 0 ? activeSkill.aoeRadius : 3f;
                Collider[] hits = Physics.OverlapSphere(impactPos, aoeRadius);
                int hitCount = 0;
                float damageToApply = FinalActiveDamage;

                Debug.Log($"[Character] AOE checking {hits.Length} colliders in {aoeRadius}m radius");

                foreach (var hit in hits)
                {
                    if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                        continue;

                    ITargetable hitTarget = hit.GetComponent<ITargetable>();
                    if (hitTarget == null || !hitTarget.IsAlive())
                        continue;

                    // 데미지 적용
                    Debug.Log($"[Character] AOE applying {damageToApply:F1} damage to {hit.name}");
                    hitTarget.TakeDamage(damageToApply);
                    hitCount++;

                    // Support 스킬 상태이상 적용
                    if (supportSkill != null && supportSkill.statusEffectType != StatusEffectType.None)
                    {
                        ApplyStatusEffect(hitTarget);
                    }
                }

                Debug.Log($"[Character] AOE skill {activeSkill.skillName} completed: {hitCount} targets hit with {damageToApply:F1} damage each");

                // Meteor 이펙트 정리 (hitEffect보다 먼저 사라지게)
                if (meteorEffect != null)
                {
                    Object.Destroy(meteorEffect, 0.1f);
                }

                // Hit Effect 자동 정리 (2초 후)
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
                // 예외 발생 시 이펙트 정리
                if (castEffect != null) Object.Destroy(castEffect);
            }
        }

        //LMJ : Build chain targets for channeling skill
        private System.Collections.Generic.List<ITargetable> BuildChainTargets(ITargetable firstTarget)
        {
            var targets = new System.Collections.Generic.List<ITargetable> { firstTarget };

            // If no Chain support skill, return single target
            if (supportSkill == null || supportSkill.statusEffectType != StatusEffectType.Chain)
            {
                return targets;
            }

            // Build chain
            int maxChainCount = supportSkill.chainCount;
            var hitTargets = new System.Collections.Generic.HashSet<ITargetable> { firstTarget };
            ITargetable currentTarget = firstTarget;

            for (int i = 0; i < maxChainCount; i++)
            {
                ITargetable nextTarget = FindNextChainTarget(currentTarget.GetPosition(), supportSkill.chainRange, hitTargets);
                if (nextTarget == null) break;

                targets.Add(nextTarget);
                hitTargets.Add(nextTarget);
                currentTarget = nextTarget;

                Debug.Log($"[Character] Chain {i + 1}/{maxChainCount}: {nextTarget.GetTransform().name}");
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

                // Skip already hit targets
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
            if (supportSkill == null || target == null) return;

            switch (supportSkill.statusEffectType)
            {
                case StatusEffectType.CC:
                    if (target.GetTransform().CompareTag(Tag.Monster))
                    {
                        Monster monster = target.GetTransform().GetComponent<Monster>();
                        if (monster != null)
                        {
                            monster.ApplyCC(supportSkill.ccType, supportSkill.ccDuration, supportSkill.ccSlowAmount, supportSkill.ccEffectPrefab);
                            Debug.Log($"[Character] Applied CC to {monster.name}: {supportSkill.ccType}");
                        }
                    }
                    else if (target.GetTransform().CompareTag(Tag.BossMonster))
                    {
                        BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                        if (boss != null)
                        {
                            boss.ApplyCC(supportSkill.ccType, supportSkill.ccDuration, supportSkill.ccSlowAmount, supportSkill.ccEffectPrefab);
                            Debug.Log($"[Character] Applied CC to {boss.name}: {supportSkill.ccType}");
                        }
                    }
                    break;

                case StatusEffectType.DOT:
                    if (target.GetTransform().CompareTag(Tag.Monster))
                    {
                        Monster monster = target.GetTransform().GetComponent<Monster>();
                        if (monster != null)
                        {
                            monster.ApplyDOT(supportSkill.dotType, supportSkill.dotDamagePerTick, supportSkill.dotTickInterval, supportSkill.dotDuration, supportSkill.dotEffectPrefab);
                            Debug.Log($"[Character] Applied DOT to {monster.name}: {supportSkill.dotType}");
                        }
                    }
                    else if (target.GetTransform().CompareTag(Tag.BossMonster))
                    {
                        BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                        if (boss != null)
                        {
                            boss.ApplyDOT(supportSkill.dotType, supportSkill.dotDamagePerTick, supportSkill.dotTickInterval, supportSkill.dotDuration, supportSkill.dotEffectPrefab);
                            Debug.Log($"[Character] Applied DOT to {boss.name}: {supportSkill.dotType}");
                        }
                    }
                    break;

                case StatusEffectType.Mark:
                    if (target.GetTransform().CompareTag(Tag.Monster))
                    {
                        Monster monster = target.GetTransform().GetComponent<Monster>();
                        if (monster != null)
                        {
                            monster.ApplyMark(supportSkill.markType, supportSkill.markDuration, supportSkill.markDamageMultiplier, supportSkill.markEffectPrefab);
                            Debug.Log($"[Character] Applied Mark to {monster.name}: {supportSkill.markType}");
                        }
                    }
                    else if (target.GetTransform().CompareTag(Tag.BossMonster))
                    {
                        BossMonster boss = target.GetTransform().GetComponent<BossMonster>();
                        if (boss != null)
                        {
                            boss.ApplyMark(supportSkill.markType, supportSkill.markDuration, supportSkill.markDamageMultiplier, supportSkill.markEffectPrefab);
                            Debug.Log($"[Character] Applied Mark to {boss.name}: {supportSkill.markType}");
                        }
                    }
                    break;

                case StatusEffectType.Chain:
                    // Chain is handled in BuildChainTargets
                    break;
            }
        }

        //LMJ : Update beam effect to connect character and target
        private void UpdateBeamEffect(GameObject beamEffect, ITargetable target)
        {
            if (beamEffect == null || target == null) return;

            Vector3 startPos = transform.position + spawnOffset;
            Vector3 endPos = target.GetPosition();

            UpdateBeamEffect(beamEffect, startPos, endPos);
        }

        //LMJ : Update beam effect to connect two positions (for chain beams)
        private void UpdateBeamEffect(GameObject beamEffect, Vector3 startPos, Vector3 endPos)
        {
            if (beamEffect == null) return;

            // Try LineRenderer first (most common for beam effects)
            LineRenderer lineRenderer = beamEffect.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);
                return;
            }

            // Fallback: Transform-based (position, scale, rotation)
            Vector3 direction = endPos - startPos;
            float distance = direction.magnitude;

            beamEffect.transform.position = startPos;
            beamEffect.transform.rotation = Quaternion.LookRotation(direction);
            beamEffect.transform.localScale = new Vector3(1f, 1f, distance); // Stretch along Z-axis
        }

        // IPoolable implementation
        public void OnSpawn()
        {
            characterObj.SetActive(true);

            if (!isInitialized)
            {
                Start(); // Initialize if not started yet
            }
            else
            {
                StartAttackLoop(); // Restart attack loop
                StartActiveSkillLoop(); // Restart active skill loop
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
    }
}
