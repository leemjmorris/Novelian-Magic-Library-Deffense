#if false
// LMJ: Old Projectile disabled for combat system refactor (Issue #265)
// Will be replaced with straight-line projectile
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

//JML: Projectile with Rigidbody-based movement and target tracking
public class Projectile_OLD : MonoBehaviour, IPoolable
{
    // Old code preserved for reference
}
#endif

//LMJ : Unified projectile system - supports both physics and effect modes
//      Migrated to new CSV-based skill system
namespace Novelian.Combat
{
    using NovelianMagicLibraryDefense.Managers;
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;

    public enum ProjectileMode
    {
        Physics,    // Rigidbody-based with collision detection
        Effect      // Visual-only with lerp movement
    }

    public class Projectile : MonoBehaviour, IPoolable
    {
        private const float OUT_OF_BOUNDS_DISTANCE = 100f;

        [Header("Components")]
        [SerializeField, Tooltip("Rigidbody for physics movement (Physics mode only)")]
        private Rigidbody rb;

        [Header("Damage")]
        [SerializeField, Tooltip("Projectile damage")]
        private float damage = 10f;

        // Movement mode
        private ProjectileMode mode = ProjectileMode.Physics;
        private System.Action<Vector3> onHitCallback;

        // Skill data (CSV-based)
        private int skillId;
        private MainSkillData skillData;
        private MainSkillPrefabEntry skillPrefabs;

        // Support skill data for status effects (CSV-based)
        private int supportSkillId;
        private SupportSkillData supportSkillData;
        private SupportSkillPrefabEntry supportPrefabs;

        // Chain state tracking
        private int currentChainCount = 0;
        private int maxChainCount = 0;
        private System.Collections.Generic.HashSet<ITargetable> chainHitTargets;
        private float currentChainDamage = 0f;

        // Pierce state tracking (관통 시스템)
        private int currentPierceCount = 0;
        private int maxPierceCount = 0;
        private float baseDamageForPierce = 0f; // 관통 데미지 감소 계산을 위한 기본 데미지

        // Movement state
        private Vector3 fixedDirection;
        private float speed;
        private float lifetime;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float elapsedTime;
        private bool isInitialized = false;

        // Lifetime tracking
        private CancellationTokenSource lifetimeCts;

        //LMJ : Launch projectile in Physics mode - basic version (for backward compatibility)
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime)
        {
            Launch(spawnPos, targetPos, projectileSpeed, projectileLifetime, this.damage, 0, 0);
        }

        //LMJ : Launch projectile in Physics mode - with skill IDs (new CSV-based system)
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, int mainSkillId, int supportId)
        {
            mode = ProjectileMode.Physics;
            transform.position = spawnPos;
            startPosition = spawnPos;
            targetPosition = targetPos;

            // Calculate fixed direction (NO HOMING)
            fixedDirection = (targetPos - spawnPos).normalized;
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            damage = damageAmount;
            elapsedTime = 0f;
            isInitialized = true;

            // Rigidbody 자동 추가 (없으면 Physics 충돌이 작동하지 않음)
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.isKinematic = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    Debug.Log("[Projectile] Rigidbody auto-added for physics movement");
                }
            }

            // Collider 자동 추가 (없으면 충돌 감지가 작동하지 않음)
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
                sphereCol.isTrigger = true;
                sphereCol.radius = 0.5f;
                Debug.Log("[Projectile] SphereCollider auto-added for collision detection");
            }

            // 레이어 설정 (Projectile 레이어)
            int projectileLayer = LayerMask.NameToLayer("Projectile");
            if (projectileLayer >= 0)
            {
                gameObject.layer = projectileLayer;
            }

            // Load skill data from CSV and PrefabDatabase
            LoadSkillData(mainSkillId, supportId);

            // JML: Launch 로그 제거

            // Spawn effect prefab as child if skillData is provided
            if (skillPrefabs != null && skillPrefabs.projectilePrefab != null)
            {
                // Clear any existing child effects
                foreach (Transform child in transform)
                {
                    if (child.gameObject != null)
                    {
                        Object.Destroy(child.gameObject);
                    }
                }

                // Spawn new effect as child
                GameObject effectInstance = Object.Instantiate(skillPrefabs.projectilePrefab, transform);
                effectInstance.transform.localPosition = Vector3.zero;
                effectInstance.transform.localRotation = Quaternion.LookRotation(fixedDirection);
            }

            // Initialize Chain state (only on first launch, not re-launch)
            if (currentChainCount == 0 && supportSkillData != null && supportSkillData.GetStatusEffectType() == StatusEffectType.Chain)
            {
                maxChainCount = supportSkillData.chain_count;
                chainHitTargets = new System.Collections.Generic.HashSet<ITargetable>();
                currentChainDamage = damageAmount;
                // JML: Chain init 로그 제거
            }

            // Initialize Pierce state (관통 시스템 - 메인 스킬 또는 서포트 스킬에서 관통 가능)
            if (currentPierceCount == 0)
            {
                int basePierce = skillData?.pierce_count ?? 0;
                int supportPierce = supportSkillData?.add_pierce ?? 0;

                if (basePierce > 0 || supportPierce > 0)
                {
                    maxPierceCount = basePierce + supportPierce;
                    baseDamageForPierce = damageAmount;
                    // JML: Pierce init 로그 제거
                }
            }

            // Cancel previous lifetime token
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();

            // Start lifetime countdown
            TrackLifetimeAsync(lifetimeCts.Token).Forget();
        }

        //LMJ : Load skill data from CSV and PrefabDatabase
        private void LoadSkillData(int mainSkillId, int supportId)
        {
            skillId = mainSkillId;
            supportSkillId = supportId;
            skillData = null;
            skillPrefabs = null;
            supportSkillData = null;
            supportPrefabs = null;

            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogWarning("[Projectile] CSVLoader not initialized");
                return;
            }

            var prefabDb = SkillPrefabDatabase.Instance;

            // Load main skill data
            if (mainSkillId > 0)
            {
                skillData = CSVLoader.Instance.GetData<MainSkillData>(mainSkillId);
                if (skillData != null)
                {
                    skillPrefabs = prefabDb?.GetMainSkillEntry(mainSkillId);
                }
            }

            // Load support skill data
            if (supportId > 0)
            {
                supportSkillData = CSVLoader.Instance.GetData<SupportSkillData>(supportId);
                if (supportSkillData != null)
                {
                    supportPrefabs = prefabDb?.GetSupportSkillEntry(supportId);
                }
            }
        }

        //LMJ : Launch projectile in Effect mode (for visual-only projectiles without physics)
        public void LaunchEffect(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, System.Action<Vector3> onHit = null, int supportId = 0)
        {
            mode = ProjectileMode.Effect;
            transform.position = spawnPos;
            startPosition = spawnPos;
            targetPosition = targetPos;

            fixedDirection = (targetPos - spawnPos).normalized;
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            damage = damageAmount;
            onHitCallback = onHit;
            elapsedTime = 0f;
            isInitialized = true;

            // Load support skill data
            LoadSkillData(0, supportId);

            transform.rotation = Quaternion.LookRotation(fixedDirection);

            // Set layer to Projectile for proper collision detection
            gameObject.layer = LayerMask.NameToLayer("Projectile");

            // Add Kinematic Rigidbody for collision detection (required for Trigger detection)
            Rigidbody effectRb = gameObject.GetComponent<Rigidbody>();
            if (effectRb == null)
            {
                effectRb = gameObject.AddComponent<Rigidbody>();
            }
            effectRb.isKinematic = true;
            effectRb.useGravity = false;
            effectRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Add SphereCollider for collision detection
            SphereCollider collider = gameObject.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<SphereCollider>();
            }
            collider.isTrigger = true;
            collider.radius = 1.0f;

            // Cancel previous lifetime token
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();

            // Start effect movement
            EffectMovementAsync(lifetimeCts.Token).Forget();
        }

        //LMJ : Physics-based movement in fixed direction (Physics mode only)
        private void FixedUpdate()
        {
            if (mode != ProjectileMode.Physics) return;
            if (!isInitialized)
            {
                // Debug.Log($"[Projectile] FixedUpdate skipped: isInitialized={isInitialized}");
                return;
            }

            if (Time.timeScale == 0f)
            {
                if (rb != null) rb.linearVelocity = Vector3.zero;
                return;
            }

            // Rigidbody가 있으면 velocity로 이동, 없으면 transform 직접 이동
            if (rb != null)
            {
                rb.linearVelocity = fixedDirection * speed;
            }
            else
            {
                // Fallback: Rigidbody 없이 직접 이동
                transform.position += fixedDirection * speed * Time.fixedDeltaTime;
            }

            if (fixedDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(fixedDirection);
            }

            // 이동 거리 체크용 디버그 (매 프레임마다 출력하면 로그가 많아지므로 주석 처리)
            // float distance = Vector3.Distance(startPosition, transform.position);
            // Debug.Log($"[Projectile] Moving: pos={transform.position}, dir={fixedDirection}, speed={speed}, distance={distance:F1}");

            if (Vector3.Distance(startPosition, transform.position) > OUT_OF_BOUNDS_DISTANCE)
            {
                ReturnToPool();
            }
        }

        //LMJ : Effect-based movement with lerp (Effect mode only)
        private async UniTaskVoid EffectMovementAsync(CancellationToken ct)
        {
            try
            {
                while (isInitialized && !ct.IsCancellationRequested)
                {
                    if (Time.timeScale == 0f)
                    {
                        await UniTask.Yield(ct);
                        continue;
                    }

                    elapsedTime += Time.deltaTime;

                    float distance = Vector3.Distance(startPosition, targetPosition);
                    float t = Mathf.Clamp01(elapsedTime * speed / distance);
                    transform.position = Vector3.Lerp(startPosition, targetPosition, t);

                    if (t >= 1f || elapsedTime >= lifetime)
                    {
                        OnReachTarget();
                        break;
                    }

                    await UniTask.Yield(ct);
                }
            }
            catch (System.OperationCanceledException)
            {
                // Expected
            }
        }

        //LMJ : Handle reaching target in Effect mode
        private void OnReachTarget()
        {
            isInitialized = false;
            onHitCallback?.Invoke(targetPosition);
            Destroy(gameObject);
        }

        //LMJ : Track lifetime and auto-despawn
        private async UniTaskVoid TrackLifetimeAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay((int)(lifetime * 1000), cancellationToken: ct);

                if (!ct.IsCancellationRequested)
                {
                    ReturnToPool();
                }
            }
            catch (System.OperationCanceledException)
            {
                // Expected when projectile hits target before lifetime ends
            }
        }

        //LMJ : Handle collision with monsters and obstacles (both Physics and Effect modes)
        private void OnTriggerEnter(Collider other)
        {
            // JML: OnTriggerEnter 로그 제거

            if (!isInitialized) return;

            // Obstacle collision
            if (other.CompareTag(Tag.Obstacle))
            {
                if (mode == ProjectileMode.Physics)
                {
                    ReturnToPool();
                }
                else if (mode == ProjectileMode.Effect)
                {
                    lifetimeCts?.Cancel();
                    Destroy(gameObject);
                }
                return;
            }

            // Ground collision
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                if (mode == ProjectileMode.Physics)
                {
                    ReturnToPool();
                }
                else if (mode == ProjectileMode.Effect)
                {
                    lifetimeCts?.Cancel();
                    Destroy(gameObject);
                }
                return;
            }

            if (other.CompareTag(Tag.Monster))
            {
                Monster monster = other.GetComponent<Monster>();
                if (monster != null)
                {
                    // Apply status effects BEFORE damage
                    if (supportSkillData != null && supportSkillData.GetStatusEffectType() != StatusEffectType.Chain)
                    {
                        ApplyStatusEffect(monster);
                    }

                    // Apply damage (새 데미지 공식 적용)
                    float damageToApply = CalculateDamageToApply();

                    // Issue #362 - 저체력 보너스 데미지 적용 (처형 서포트 등)
                    if (supportSkillData != null && supportSkillData.IsLowHpBonusSupport)
                    {
                        damageToApply = DamageCalculator.CalculateLowHpBonusDamage(
                            damageToApply,
                            monster.GetHealth(),
                            monster.GetMaxHealth(),
                            supportSkillData.low_hp_bonus_damage_mult);
                    }

                    monster.TakeDamage(damageToApply);

                    // Spawn hit effect
                    if (skillPrefabs != null && skillPrefabs.hitEffectPrefab != null)
                    {
                        GameObject hitEffect = Object.Instantiate(skillPrefabs.hitEffectPrefab, other.transform.position, Quaternion.identity);
                        Object.Destroy(hitEffect, 2f);
                    }

                    // Add to hit targets for chain tracking
                    if (maxChainCount > 0)
                    {
                        chainHitTargets.Add(monster);
                    }

                    // Process Chain (체이닝: DamageCalculator 사용)
                    if (supportSkillData != null && supportSkillData.GetStatusEffectType() == StatusEffectType.Chain && currentChainCount < maxChainCount)
                    {
                        // Spawn chain effect
                        if (supportPrefabs?.chainEffectPrefab != null && currentChainCount > 0)
                        {
                            SpawnChainEffect(startPosition, other.transform.position);
                        }

                        // Find next target
                        ITargetable nextTarget = FindNextChainTarget(other.transform.position, chainHitTargets, monster);

                        if (nextTarget != null)
                        {
                            // DamageCalculator로 체이닝 데미지 계산 (n번째 타격)
                            currentChainCount++;
                            float reductionRate = supportSkillData.chain_damage_reduction / 100f;
                            currentChainDamage = DamageCalculator.CalculatePierceChainDamage(baseDamageForPierce > 0 ? baseDamageForPierce : damage, reductionRate, currentChainCount);

                            // JML: Chain 로그 압축
                            Debug.Log($"[Proj] Chain {currentChainCount}/{maxChainCount}");

                            Vector3 directionToNext = (nextTarget.GetPosition() - other.transform.position).normalized;
                            float spawnOffset = 1.0f;
                            Vector3 spawnPos = other.transform.position + directionToNext * spawnOffset;

                            Launch(spawnPos, nextTarget.GetPosition(), speed, lifetime, currentChainDamage, skillId, supportSkillId);
                            return;
                        }
                    }

                    // Process Pierce (관통: DamageCalculator 사용)
                    if (maxPierceCount > 0 && currentPierceCount < maxPierceCount)
                    {
                        currentPierceCount++;
                        // JML: Pierce 로그 제거
                        return; // 관통하여 계속 진행 (풀로 돌아가지 않음)
                    }

                    // Process Fragmentation (파편화 40002: 명중 시 분열)
                    if (supportSkillId == 40002 && supportSkillData != null && supportSkillData.add_projectiles > 0)
                    {
                        int totalFragments = 1 + supportSkillData.add_projectiles;
                        SpawnFragmentProjectilesFan(other.transform.position, totalFragments, fixedDirection, other);
                    }
                }

                // Cleanup
                if (mode == ProjectileMode.Physics)
                {
                    ReturnToPool();
                }
                else if (mode == ProjectileMode.Effect)
                {
                    lifetimeCts?.Cancel();
                    onHitCallback?.Invoke(other.transform.position);
                    Destroy(gameObject);
                }
            }
            else if (other.CompareTag(Tag.BossMonster))
            {
                BossMonster boss = other.GetComponent<BossMonster>();
                if (boss != null)
                {
                    // Apply status effects BEFORE damage
                    if (supportSkillData != null && supportSkillData.GetStatusEffectType() != StatusEffectType.Chain)
                    {
                        ApplyStatusEffectToBoss(boss);
                    }

                    // Apply damage (새 데미지 공식 적용)
                    float damageToApply = CalculateDamageToApply();

                    // Issue #362 - 저체력 보너스 데미지 적용 (처형 서포트 등)
                    if (supportSkillData != null && supportSkillData.IsLowHpBonusSupport)
                    {
                        damageToApply = DamageCalculator.CalculateLowHpBonusDamage(
                            damageToApply,
                            boss.GetHealth(),
                            boss.GetMaxHealth(),
                            supportSkillData.low_hp_bonus_damage_mult);
                    }

                    boss.TakeDamage(damageToApply);

                    // Spawn hit effect
                    if (skillPrefabs != null && skillPrefabs.hitEffectPrefab != null)
                    {
                        GameObject hitEffect = Object.Instantiate(skillPrefabs.hitEffectPrefab, other.transform.position, Quaternion.identity);
                        Object.Destroy(hitEffect, 2f);
                    }

                    // Add to hit targets for chain tracking
                    if (maxChainCount > 0)
                    {
                        chainHitTargets.Add(boss);
                    }

                    // Process Chain (체이닝: DamageCalculator 사용)
                    if (supportSkillData != null && supportSkillData.GetStatusEffectType() == StatusEffectType.Chain && currentChainCount < maxChainCount)
                    {
                        if (supportPrefabs?.chainEffectPrefab != null && currentChainCount > 0)
                        {
                            SpawnChainEffect(startPosition, other.transform.position);
                        }

                        ITargetable nextTarget = FindNextChainTarget(other.transform.position, chainHitTargets, boss);

                        if (nextTarget != null)
                        {
                            // DamageCalculator로 체이닝 데미지 계산 (n번째 타격)
                            currentChainCount++;
                            float reductionRate = supportSkillData.chain_damage_reduction / 100f;
                            currentChainDamage = DamageCalculator.CalculatePierceChainDamage(baseDamageForPierce > 0 ? baseDamageForPierce : damage, reductionRate, currentChainCount);

                            // JML: Chain 로그 압축
                            Debug.Log($"[Proj] Chain {currentChainCount}/{maxChainCount}");

                            Vector3 directionToNext = (nextTarget.GetPosition() - other.transform.position).normalized;
                            float spawnOffset = 1.0f;
                            Vector3 spawnPos = other.transform.position + directionToNext * spawnOffset;

                            Launch(spawnPos, nextTarget.GetPosition(), speed, lifetime, currentChainDamage, skillId, supportSkillId);
                            return;
                        }
                    }

                    // Process Pierce (관통: DamageCalculator 사용)
                    if (maxPierceCount > 0 && currentPierceCount < maxPierceCount)
                    {
                        currentPierceCount++;
                        // JML: Pierce 로그 제거
                        return; // 관통하여 계속 진행 (풀로 돌아가지 않음)
                    }

                    // Process Fragmentation (파편화 40002: 명중 시 분열)
                    if (supportSkillId == 40002 && supportSkillData != null && supportSkillData.add_projectiles > 0)
                    {
                        int totalFragments = 1 + supportSkillData.add_projectiles;
                        SpawnFragmentProjectilesFan(other.transform.position, totalFragments, fixedDirection, other);
                    }
                }

                // Cleanup
                if (mode == ProjectileMode.Physics)
                {
                    ReturnToPool();
                }
                else if (mode == ProjectileMode.Effect)
                {
                    lifetimeCts?.Cancel();
                    onHitCallback?.Invoke(other.transform.position);
                    Destroy(gameObject);
                }
            }
        }

        //LMJ : Calculate damage to apply (새 데미지 공식)
        // 관통/체이닝 감소 공식: n번째 타격 데미지 = (단일 타격 데미지) × (1 - 감소율)^n
        private float CalculateDamageToApply()
        {
            // 1. 체이닝 활성화된 경우
            if (maxChainCount > 0)
            {
                return currentChainDamage;
            }

            // 2. 관통 활성화된 경우 - DamageCalculator 사용
            if (maxPierceCount > 0 && currentPierceCount > 0)
            {
                // 관통 감소율: 서포트 스킬의 chain_damage_reduction 또는 기본값 30%
                float reductionRate = supportSkillData?.chain_damage_reduction / 100f ?? 0.3f;
                float pierceDamage = DamageCalculator.CalculatePierceChainDamage(baseDamageForPierce, reductionRate, currentPierceCount);
                // JML: Pierce damage 로그 제거
                return pierceDamage;
            }

            // 3. 기본 데미지 반환
            return damage;
        }

        //LMJ : Apply status effect to monster (MainSkillData + SupportSkillData)
        private void ApplyStatusEffect(Monster monster)
        {
            if (monster == null) return;

            // Get effect prefabs from database
            GameObject ccEffectPrefab = supportPrefabs?.ccEffectPrefab ?? skillPrefabs?.hitEffectPrefab;
            GameObject dotEffectPrefab = supportPrefabs?.dotEffectPrefab ?? skillPrefabs?.hitEffectPrefab;
            GameObject markEffectPrefab = supportPrefabs?.markEffectPrefab ?? skillPrefabs?.hitEffectPrefab;

            // 1. MainSkillData 자체 효과 적용 (스킬 테이블에 정의된 CC/DOT/표식)
            if (skillData != null)
            {
                // CC 효과 (stun_use=true 또는 cc_duration > 0)
                if (skillData.HasCCEffect)
                {
                    monster.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, ccEffectPrefab);
                }

                // DOT 효과 (dot_duration > 0)
                if (skillData.HasDOTEffect)
                {
                    monster.ApplyDOT(DOTType.Burn, skillData.dot_damage_per_tick, skillData.dot_tick_interval, skillData.dot_duration, dotEffectPrefab);
                }

                // 표식 효과 (mark_duration > 0)
                if (skillData.HasMarkEffect)
                {
                    monster.ApplyMark(skillData.GetElementBasedMarkType(), skillData.mark_duration, skillData.mark_damage_mult / 100f, markEffectPrefab);
                }
            }

            // 2. SupportSkillData 추가 효과 적용
            if (supportSkillData != null)
            {
                switch (supportSkillData.GetStatusEffectType())
                {
                    case StatusEffectType.CC:
                        monster.ApplyCC(supportSkillData.GetCCType(), supportSkillData.cc_duration, supportSkillData.cc_slow_amount, ccEffectPrefab);
                        break;

                    case StatusEffectType.DOT:
                        monster.ApplyDOT(supportSkillData.GetDOTType(), supportSkillData.dot_damage_per_tick, supportSkillData.dot_tick_interval, supportSkillData.dot_duration, dotEffectPrefab);
                        break;

                    case StatusEffectType.Mark:
                        monster.ApplyMark(supportSkillData.GetMarkType(), supportSkillData.mark_duration, supportSkillData.mark_damage_mult, markEffectPrefab);
                        break;

                    case StatusEffectType.Chain:
                        // Chain is handled separately
                        break;
                }
            }
        }

        //LMJ : Apply status effect to boss monster (MainSkillData + SupportSkillData)
        private void ApplyStatusEffectToBoss(BossMonster boss)
        {
            if (boss == null) return;

            GameObject ccEffectPrefab = supportPrefabs?.ccEffectPrefab ?? skillPrefabs?.hitEffectPrefab;
            GameObject dotEffectPrefab = supportPrefabs?.dotEffectPrefab ?? skillPrefabs?.hitEffectPrefab;
            GameObject markEffectPrefab = supportPrefabs?.markEffectPrefab ?? skillPrefabs?.hitEffectPrefab;

            // 1. MainSkillData 자체 효과 적용 (스킬 테이블에 정의된 CC/DOT/표식)
            if (skillData != null)
            {
                // CC 효과 (stun_use=true 또는 cc_duration > 0)
                if (skillData.HasCCEffect)
                {
                    boss.ApplyCC(skillData.GetCCType(), skillData.cc_duration, skillData.cc_slow_amount, ccEffectPrefab);
                }

                // DOT 효과 (dot_duration > 0)
                if (skillData.HasDOTEffect)
                {
                    boss.ApplyDOT(DOTType.Burn, skillData.dot_damage_per_tick, skillData.dot_tick_interval, skillData.dot_duration, dotEffectPrefab);
                }

                // 표식 효과 (mark_duration > 0)
                if (skillData.HasMarkEffect)
                {
                    boss.ApplyMark(skillData.GetElementBasedMarkType(), skillData.mark_duration, skillData.mark_damage_mult / 100f, markEffectPrefab);
                }
            }

            // 2. SupportSkillData 추가 효과 적용
            if (supportSkillData != null)
            {
                switch (supportSkillData.GetStatusEffectType())
                {
                    case StatusEffectType.CC:
                        boss.ApplyCC(supportSkillData.GetCCType(), supportSkillData.cc_duration, supportSkillData.cc_slow_amount, ccEffectPrefab);
                        break;

                    case StatusEffectType.DOT:
                        boss.ApplyDOT(supportSkillData.GetDOTType(), supportSkillData.dot_damage_per_tick, supportSkillData.dot_tick_interval, supportSkillData.dot_duration, dotEffectPrefab);
                        break;

                    case StatusEffectType.Mark:
                        boss.ApplyMark(supportSkillData.GetMarkType(), supportSkillData.mark_duration, supportSkillData.mark_damage_mult, markEffectPrefab);
                        break;

                    case StatusEffectType.Chain:
                        break;
                }
            }
        }

        //LMJ : Spawn chain effect visual between two positions
        private void SpawnChainEffect(Vector3 startPos, Vector3 endPos)
        {
            if (supportPrefabs == null || supportPrefabs.chainEffectPrefab == null) return;

            Vector3 midPos = (startPos + endPos) / 2f;
            GameObject chainEffect = Instantiate(supportPrefabs.chainEffectPrefab, midPos, Quaternion.identity);

            Vector3 direction = (endPos - startPos).normalized;
            if (direction != Vector3.zero)
            {
                chainEffect.transform.rotation = Quaternion.LookRotation(direction);
            }

            float distance = Vector3.Distance(startPos, endPos);
            chainEffect.transform.localScale = new Vector3(1f, 1f, distance);

            Destroy(chainEffect, 1f);
        }

        //LMJ : Find next target for chain effect
        private ITargetable FindNextChainTarget(Vector3 currentPosition, System.Collections.Generic.HashSet<ITargetable> hitTargets, ITargetable excludeTarget = null)
        {
            if (supportSkillData == null) return null;

            Collider[] hits = Physics.OverlapSphere(currentPosition, supportSkillData.chain_range);

            ITargetable closestTarget = null;
            float closestDistance = float.MaxValue;
            const float MIN_CHAIN_DISTANCE = 0.5f;

            foreach (var hit in hits)
            {
                if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable target = hit.GetComponent<ITargetable>();
                if (target == null || !target.IsAlive())
                    continue;

                if (excludeTarget != null && target == excludeTarget)
                    continue;

                if (hitTargets.Contains(target))
                    continue;

                float distance = Vector3.Distance(currentPosition, target.GetPosition());

                if (distance < MIN_CHAIN_DISTANCE)
                    continue;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        //LMJ : Spawn fragment projectiles in fan pattern on hit (파편화 40002)
        // 원본 진행 방향을 기준으로 부채꼴 형태로 발사
        // 몬스터 콜라이더 크기에 따라 동적으로 스폰 위치 계산
        private void SpawnFragmentProjectilesFan(Vector3 hitPosition, int totalCount, Vector3 originalDirection, Collider hitCollider)
        {
            if (totalCount <= 0) return;

            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;
            float spreadAngle = 30f; // 부채꼴 총 각도 (좌우 각각 15도씩)

            // 원본 발사체의 이펙트 프리팹 참조 저장
            GameObject effectPrefab = skillPrefabs?.projectilePrefab;

            // 원본 발사체의 시작 높이 사용 (바닥에서 생성되지 않도록)
            float projectileHeight = startPosition.y;

            // 몬스터 콜라이더 크기에 따라 동적으로 통과 오프셋 계산
            // 콜라이더 바운드의 magnitude + 여유 공간으로 몬스터 뒤쪽에서 생성
            float passOffset = 2.5f; // 기본값 (fallback)
            if (hitCollider != null)
            {
                passOffset = hitCollider.bounds.extents.magnitude + 1.0f;
            }
            float spreadOffset = 0.5f; // 부채꼴 방향으로의 추가 오프셋

            // JML: Fragment spawn 로그 제거

            for (int i = 0; i < totalCount; i++)
            {
                // 부채꼴 각도 계산: 중앙을 0도로 하여 좌우로 퍼짐
                // 예: 5발이면 -60도, -30도, 0도, +30도, +60도
                float angleOffset = 0f;
                if (totalCount > 1)
                {
                    angleOffset = spreadAngle * (i - (totalCount - 1) / 2f);
                }

                // Y축 회전을 적용하여 부채꼴 방향 계산
                Vector3 fragmentDirection = Quaternion.Euler(0, angleOffset, 0) * originalDirection;

                // 히트 위치의 XZ + 원본 발사체의 Y 높이 사용
                Vector3 adjustedHitPos = new Vector3(hitPosition.x, projectileHeight, hitPosition.z);

                // 몬스터 뒤쪽(원본 방향으로 통과한 지점)에서 생성
                Vector3 behindMonster = adjustedHitPos + originalDirection * passOffset;
                Vector3 spawnPos = behindMonster + fragmentDirection * spreadOffset;
                Vector3 targetPos = spawnPos + fragmentDirection * 50f;

                Projectile fragment = pool.Spawn<Projectile>(spawnPos);
                // 분열 발사체는 supportSkillId = 0으로 설정하여 재분열 방지
                fragment.Launch(spawnPos, targetPos, speed, lifetime, damage, skillId, 0);

                // 파편에 원본 이펙트 복사
                if (effectPrefab != null)
                {
                    // 기존 자식 이펙트 제거
                    foreach (Transform child in fragment.transform)
                    {
                        if (child.gameObject != null)
                        {
                            Object.Destroy(child.gameObject);
                        }
                    }
                    // 새 이펙트 생성
                    GameObject fragmentEffect = Object.Instantiate(effectPrefab, fragment.transform);
                    fragmentEffect.transform.localPosition = Vector3.zero;
                    fragmentEffect.transform.localRotation = Quaternion.LookRotation(fragmentDirection);
                }

                // JML: Fragment 개별 로그 제거
            }

            Debug.Log($"[Proj] Frag {totalCount}발");
        }

        //LMJ : Return projectile to pool
        private void ReturnToPool()
        {
            lifetimeCts?.Cancel();
            GameManager.Instance.Pool.Despawn(this);
        }

        // IPoolable implementation
        public void OnSpawn()
        {
            mode = ProjectileMode.Physics;
            isInitialized = false;
            fixedDirection = Vector3.zero;
            elapsedTime = 0f;
            onHitCallback = null;

            if (rb != null) rb.linearVelocity = Vector3.zero;

            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }

        public void OnDespawn()
        {
            isInitialized = false;
            fixedDirection = Vector3.zero;
            elapsedTime = 0f;
            onHitCallback = null;

            // Clear skill references
            skillId = 0;
            skillData = null;
            skillPrefabs = null;
            supportSkillId = 0;
            supportSkillData = null;
            supportPrefabs = null;

            // Reset chain state
            currentChainCount = 0;
            maxChainCount = 0;
            chainHitTargets = null;
            currentChainDamage = 0f;

            // Reset pierce state
            currentPierceCount = 0;
            maxPierceCount = 0;
            baseDamageForPierce = 0f;

            if (rb != null) rb.linearVelocity = Vector3.zero;
            lifetimeCts?.Cancel();
        }

        private void OnDestroy()
        {
            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
        }
    }
}
