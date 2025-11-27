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
        private const float SPAWN_GRACE_PERIOD = 0.1f; // Ignore ground collision for first 0.1s

        [Header("Components")]
        [SerializeField, Tooltip("Rigidbody for physics movement (Physics mode only)")]
        private Rigidbody rb;

        [Header("Damage")]
        [SerializeField, Tooltip("Projectile damage")]
        private float damage = 10f;

        // Movement mode
        private ProjectileMode mode = ProjectileMode.Physics;
        private System.Action<Vector3> onHitCallback;

        // Skill data (for effect prefab)
        private SkillAssetData skillData;

        // Support skill data for status effects
        private SkillAssetData supportSkill;

        // Chain state tracking
        private int currentChainCount = 0;
        private int maxChainCount = 0;
        private System.Collections.Generic.HashSet<ITargetable> chainHitTargets;
        private float currentChainDamage = 0f;

        // Movement state
        private Vector3 fixedDirection;
        private float speed;
        private float lifetime;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float elapsedTime;
        private float spawnTime; // Time.time when spawned (for grace period)
        private bool isInitialized = false;

        // Lifetime tracking
        private CancellationTokenSource lifetimeCts;

        //LMJ : Launch projectile in Physics mode - basic version (for backward compatibility)
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime)
        {
            Launch(spawnPos, targetPos, projectileSpeed, projectileLifetime, this.damage, null);
        }

        //LMJ : Launch projectile in Physics mode - with support skill (for backward compatibility)
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, SkillAssetData supportSkillData)
        {
            Launch(spawnPos, targetPos, projectileSpeed, projectileLifetime, this.damage, supportSkillData);
        }

        //LMJ : Launch projectile in Physics mode - full version with damage and support skill
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, SkillAssetData supportSkillData)
        {
            Launch(spawnPos, targetPos, projectileSpeed, projectileLifetime, damageAmount, null, supportSkillData);
        }

        //LMJ : Launch projectile in Physics mode - with skill data for effect prefab
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, SkillAssetData skillDataParam, SkillAssetData supportSkillData)
        {
            mode = ProjectileMode.Physics;
            transform.position = spawnPos;
            startPosition = spawnPos;
            targetPosition = targetPos;

            // Calculate fixed direction (NO HOMING)
            fixedDirection = (targetPos - spawnPos).normalized;
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            damage = damageAmount; // Set damage from parameter
            elapsedTime = 0f;
            spawnTime = Time.time; // Record spawn time for grace period
            isInitialized = true;

            // Store skill data and support skill data
            if (skillDataParam != null)
            {
                skillData = skillDataParam;
            }
            supportSkill = supportSkillData;

            // Spawn effect prefab as child if skillData is provided
            if (skillData != null && skillData.projectileEffectPrefab != null)
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
                GameObject effectInstance = Object.Instantiate(skillData.projectileEffectPrefab, transform);
                effectInstance.transform.localPosition = Vector3.zero;
                effectInstance.transform.localRotation = Quaternion.LookRotation(fixedDirection);
                Debug.Log($"[Projectile] Effect prefab spawned as child: {skillData.projectileEffectPrefab.name}");
            }

            // Initialize Chain state (only on first launch, not re-launch)
            if (currentChainCount == 0 && supportSkill != null && supportSkill.statusEffectType == StatusEffectType.Chain)
            {
                maxChainCount = supportSkill.chainCount;
                chainHitTargets = new System.Collections.Generic.HashSet<ITargetable>();
                currentChainDamage = damageAmount; // Use parameter damage, not field default
                Debug.Log($"[Projectile] Chain initialized: maxChainCount={maxChainCount}, initialDamage={currentChainDamage:F1}");
            }

            // Cancel previous lifetime token
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();

            // Start lifetime countdown
            TrackLifetimeAsync(lifetimeCts.Token).Forget();

            Debug.Log($"[Projectile] Physics mode launched from {spawnPos} to {targetPos}, damage={damage:F1}, chainCount={currentChainCount}/{maxChainCount}");
        }

        //LMJ : Launch projectile in Effect mode (for visual-only projectiles without physics)
        public void LaunchEffect(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, System.Action<Vector3> onHit = null, SkillAssetData supportSkillData = null)
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
            spawnTime = Time.time; // Record spawn time for grace period
            isInitialized = true;

            // Store support skill data for status effects
            supportSkill = supportSkillData;

            transform.rotation = Quaternion.LookRotation(fixedDirection);

            // Set layer to Projectile for proper collision detection
            gameObject.layer = LayerMask.NameToLayer("Projectile");

            // Add Kinematic Rigidbody for collision detection (required for Trigger detection)
            Rigidbody effectRb = gameObject.GetComponent<Rigidbody>();
            if (effectRb == null)
            {
                effectRb = gameObject.AddComponent<Rigidbody>();
            }
            effectRb.isKinematic = true; // No physics simulation, only collision detection
            effectRb.useGravity = false;
            effectRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Better collision detection

            // Add SphereCollider for collision detection
            SphereCollider collider = gameObject.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<SphereCollider>();
            }
            collider.isTrigger = true;
            collider.radius = 1.0f; // Increased collision radius for better detection

            // Cancel previous lifetime token
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();

            // Start effect movement
            EffectMovementAsync(lifetimeCts.Token).Forget();

            Debug.Log($"[Projectile] Effect mode launched from {spawnPos} to {targetPos}");
        }

        //LMJ : Physics-based movement in fixed direction (Physics mode only)
        private void FixedUpdate()
        {
            // Only update in Physics mode
            if (mode != ProjectileMode.Physics) return;

            // Wait until initialized
            if (!isInitialized) return;

            // Pause support (respect Time.timeScale for card selection UI)
            if (Time.timeScale == 0f)
            {
                if (rb != null) rb.linearVelocity = Vector3.zero;
                return;
            }

            // Move in fixed direction
            if (rb != null) rb.linearVelocity = fixedDirection * speed;

            // Rotate to face movement direction
            if (fixedDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(fixedDirection);
            }

            // Out of bounds check
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
                    // Pause support
                    if (Time.timeScale == 0f)
                    {
                        await UniTask.Yield(ct);
                        continue;
                    }

                    elapsedTime += Time.deltaTime;

                    // Lerp movement from start to target
                    float distance = Vector3.Distance(startPosition, targetPosition);
                    float t = Mathf.Clamp01(elapsedTime * speed / distance);
                    transform.position = Vector3.Lerp(startPosition, targetPosition, t);

                    // Check if reached target or lifetime expired
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

            // Invoke callback (damage, effects, etc.)
            onHitCallback?.Invoke(targetPosition);

            // Destroy self (Effect mode doesn't use pooling)
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
            if (!isInitialized) return;

            // LMJ: Obstacle collision (rocks, trees, terrain objects) - destroy projectile without damage
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

            // LMJ: Ground/Terrain collision - destroy projectile (layer-based check)
            // Skip during spawn grace period to prevent immediate destruction
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                if (Time.time - spawnTime < SPAWN_GRACE_PERIOD)
                {
                    return; // Ignore ground collision during grace period
                }

                Debug.Log("[Projectile] Hit ground, destroying");
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
                    // Apply status effects BEFORE damage (so monster is still alive)
                    if (supportSkill != null && supportSkill.statusEffectType != StatusEffectType.Chain)
                    {
                        ApplyStatusEffect(monster);
                    }

                    // Apply damage (use chain damage if chaining)
                    float damageToApply = (maxChainCount > 0) ? currentChainDamage : damage;
                    monster.TakeDamage(damageToApply);

                    // Spawn hit effect
                    if (skillData != null && skillData.hitEffectPrefab != null)
                    {
                        GameObject hitEffect = Object.Instantiate(skillData.hitEffectPrefab, other.transform.position, Quaternion.identity);
                        Object.Destroy(hitEffect, 2f);
                    }

                    // Add to hit targets for chain tracking
                    if (maxChainCount > 0)
                    {
                        chainHitTargets.Add(monster);
                    }

                    // Process Chain: find next target and re-launch projectile
                    if (supportSkill != null && supportSkill.statusEffectType == StatusEffectType.Chain && currentChainCount < maxChainCount)
                    {
                        // Spawn chain effect from previous position to current
                        if (supportSkill.chainEffectPrefab != null && currentChainCount > 0)
                        {
                            SpawnChainEffect(startPosition, other.transform.position);
                        }

                        // Find next target (exclude current target)
                        ITargetable nextTarget = FindNextChainTarget(other.transform.position, chainHitTargets, monster);

                        if (nextTarget != null)
                        {
                            // Calculate reduced damage for next chain
                            currentChainDamage *= (1f - supportSkill.chainDamageReduction / 100f);
                            currentChainCount++;

                            Debug.Log($"[Projectile] Chain {currentChainCount}/{maxChainCount}: Bouncing to {nextTarget.GetTransform().name}, damage={currentChainDamage:F1}");

                            // Calculate spawn position offset (outside collider to prevent re-collision)
                            Vector3 directionToNext = (nextTarget.GetPosition() - other.transform.position).normalized;
                            float spawnOffset = 1.0f; // 1m offset to clear collider
                            Vector3 spawnPos = other.transform.position + directionToNext * spawnOffset;

                            // Re-launch projectile to next target (bounce like billiard ball!)
                            Launch(spawnPos, nextTarget.GetPosition(), speed, lifetime, currentChainDamage, skillData, supportSkill);
                            return; // Don't despawn - projectile continues!
                        }
                        else
                        {
                            Debug.Log($"[Projectile] Chain ended: No more targets found");
                        }
                    }
                }

                // Mode-specific cleanup (only if not chaining to next target)
                if (mode == ProjectileMode.Physics)
                {
                    ReturnToPool();
                }
                else if (mode == ProjectileMode.Effect)
                {
                    // Cancel movement async task
                    lifetimeCts?.Cancel();

                    // Invoke callback at collision position
                    onHitCallback?.Invoke(other.transform.position);

                    // Destroy self
                    Destroy(gameObject);
                }
            }
            else if (other.CompareTag(Tag.BossMonster))
            {
                BossMonster boss = other.GetComponent<BossMonster>();
                if (boss != null)
                {
                    // Apply status effects BEFORE damage (so boss is still alive)
                    if (supportSkill != null && supportSkill.statusEffectType != StatusEffectType.Chain)
                    {
                        ApplyStatusEffectToBoss(boss);
                    }

                    // Apply damage (use chain damage if chaining)
                    float damageToApply = (maxChainCount > 0) ? currentChainDamage : damage;
                    boss.TakeDamage(damageToApply);

                    // Spawn hit effect
                    if (skillData != null && skillData.hitEffectPrefab != null)
                    {
                        GameObject hitEffect = Object.Instantiate(skillData.hitEffectPrefab, other.transform.position, Quaternion.identity);
                        Object.Destroy(hitEffect, 2f);
                    }

                    // Add to hit targets for chain tracking
                    if (maxChainCount > 0)
                    {
                        chainHitTargets.Add(boss);
                    }

                    // Process Chain: find next target and re-launch projectile
                    if (supportSkill != null && supportSkill.statusEffectType == StatusEffectType.Chain && currentChainCount < maxChainCount)
                    {
                        // Spawn chain effect from previous position to current
                        if (supportSkill.chainEffectPrefab != null && currentChainCount > 0)
                        {
                            SpawnChainEffect(startPosition, other.transform.position);
                        }

                        // Find next target (exclude current target)
                        ITargetable nextTarget = FindNextChainTarget(other.transform.position, chainHitTargets, boss);

                        if (nextTarget != null)
                        {
                            // Calculate reduced damage for next chain
                            currentChainDamage *= (1f - supportSkill.chainDamageReduction / 100f);
                            currentChainCount++;

                            Debug.Log($"[Projectile] Chain {currentChainCount}/{maxChainCount}: Bouncing to {nextTarget.GetTransform().name}, damage={currentChainDamage:F1}");

                            // Calculate spawn position offset (outside collider to prevent re-collision)
                            Vector3 directionToNext = (nextTarget.GetPosition() - other.transform.position).normalized;
                            float spawnOffset = 1.0f; // 1m offset to clear collider
                            Vector3 spawnPos = other.transform.position + directionToNext * spawnOffset;

                            // Re-launch projectile to next target (bounce like billiard ball!)
                            Launch(spawnPos, nextTarget.GetPosition(), speed, lifetime, currentChainDamage, skillData, supportSkill);
                            return; // Don't despawn - projectile continues!
                        }
                        else
                        {
                            Debug.Log($"[Projectile] Chain ended: No more targets found");
                        }
                    }
                }

                // Mode-specific cleanup (only if not chaining to next target)
                if (mode == ProjectileMode.Physics)
                {
                    ReturnToPool();
                }
                else if (mode == ProjectileMode.Effect)
                {
                    // Cancel movement async task
                    lifetimeCts?.Cancel();

                    // Invoke callback at collision position
                    onHitCallback?.Invoke(other.transform.position);

                    // Destroy self
                    Destroy(gameObject);
                }
            }
        }

        //LMJ : Apply status effect to monster
        private void ApplyStatusEffect(Monster monster)
        {
            if (supportSkill == null)
            {
                Debug.Log("[Projectile] ApplyStatusEffect called but supportSkill is null");
                return;
            }

            if (monster == null)
            {
                Debug.Log("[Projectile] ApplyStatusEffect called but monster is null");
                return;
            }

            Debug.Log($"[Projectile] Applying status effect: {supportSkill.statusEffectType}");

            switch (supportSkill.statusEffectType)
            {
                case StatusEffectType.CC:
                    Debug.Log($"[Projectile] Applying CC: {supportSkill.ccType}, Duration: {supportSkill.ccDuration}");
                    monster.ApplyCC(supportSkill.ccType, supportSkill.ccDuration, supportSkill.ccSlowAmount, supportSkill.ccEffectPrefab);
                    break;

                case StatusEffectType.DOT:
                    Debug.Log($"[Projectile] Applying DOT: {supportSkill.dotType}");
                    monster.ApplyDOT(supportSkill.dotType, supportSkill.dotDamagePerTick, supportSkill.dotTickInterval, supportSkill.dotDuration, supportSkill.dotEffectPrefab);
                    break;

                case StatusEffectType.Mark:
                    Debug.Log($"[Projectile] Applying Mark: {supportSkill.markType}");
                    monster.ApplyMark(supportSkill.markType, supportSkill.markDuration, supportSkill.markDamageMultiplier, supportSkill.markEffectPrefab);
                    break;

                case StatusEffectType.Chain:
                    // Chain은 첫 타격 후 처리되므로 여기서는 로그만
                    Debug.Log("[Projectile] Chain effect will be processed after hit");
                    break;

                case StatusEffectType.None:
                    Debug.Log("[Projectile] StatusEffectType is None");
                    break;
            }
        }

        //LMJ : Apply status effect to boss monster
        private void ApplyStatusEffectToBoss(BossMonster boss)
        {
            if (supportSkill == null || boss == null) return;

            switch (supportSkill.statusEffectType)
            {
                case StatusEffectType.CC:
                    boss.ApplyCC(supportSkill.ccType, supportSkill.ccDuration, supportSkill.ccSlowAmount, supportSkill.ccEffectPrefab);
                    break;

                case StatusEffectType.DOT:
                    boss.ApplyDOT(supportSkill.dotType, supportSkill.dotDamagePerTick, supportSkill.dotTickInterval, supportSkill.dotDuration, supportSkill.dotEffectPrefab);
                    break;

                case StatusEffectType.Mark:
                    boss.ApplyMark(supportSkill.markType, supportSkill.markDuration, supportSkill.markDamageMultiplier, supportSkill.markEffectPrefab);
                    break;

                case StatusEffectType.Chain:
                    // Chain은 첫 타격 후 처리되므로 여기서는 로그만
                    break;
            }
        }

        //LMJ : Spawn chain effect visual between two positions
        private void SpawnChainEffect(Vector3 startPos, Vector3 endPos)
        {
            if (supportSkill == null || supportSkill.chainEffectPrefab == null) return;

            Vector3 midPos = (startPos + endPos) / 2f;
            GameObject chainEffect = Instantiate(supportSkill.chainEffectPrefab, midPos, Quaternion.identity);

            // Orient the effect to point from start to end
            Vector3 direction = (endPos - startPos).normalized;
            if (direction != Vector3.zero)
            {
                chainEffect.transform.rotation = Quaternion.LookRotation(direction);
            }

            // Scale the effect based on distance (optional)
            float distance = Vector3.Distance(startPos, endPos);
            chainEffect.transform.localScale = new Vector3(1f, 1f, distance);

            // Auto-destroy after short duration
            Destroy(chainEffect, 1f);

            Debug.Log($"[Projectile] Chain effect spawned from {startPos} to {endPos}");
        }

        //LMJ : Find next target for chain effect (no re-hit allowed)
        private ITargetable FindNextChainTarget(Vector3 currentPosition, System.Collections.Generic.HashSet<ITargetable> hitTargets, ITargetable excludeTarget = null)
        {
            if (supportSkill == null) return null;

            // Get all targets within chain range
            Collider[] hits = Physics.OverlapSphere(currentPosition, supportSkill.chainRange);

            ITargetable closestTarget = null;
            float closestDistance = float.MaxValue;

            const float MIN_CHAIN_DISTANCE = 0.5f; // Minimum distance to prevent immediate re-hit

            foreach (var hit in hits)
            {
                // Check if it's a valid target (Monster or BossMonster)
                if (!hit.CompareTag(Tag.Monster) && !hit.CompareTag(Tag.BossMonster))
                    continue;

                ITargetable target = hit.GetComponent<ITargetable>();
                if (target == null || !target.IsAlive())
                    continue;

                // Skip the target we just hit (prevent immediate re-collision)
                if (excludeTarget != null && target == excludeTarget)
                    continue;

                // Skip already hit targets (no re-hit allowed)
                if (hitTargets.Contains(target))
                    continue;

                float distance = Vector3.Distance(currentPosition, target.GetPosition());

                // Skip targets that are too close (prevent re-collision with overlapping colliders)
                if (distance < MIN_CHAIN_DISTANCE)
                    continue;

                // Find closest unhit target
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            if (closestTarget != null)
            {
                Debug.Log($"[Projectile] Chain: Found target at {closestDistance:F1}m");
            }
            else
            {
                Debug.Log("[Projectile] Chain: No valid targets found (all targets already hit or out of range)");
            }

            return closestTarget;
        }

        //LMJ : Return projectile to pool
        private void ReturnToPool()
        {
            lifetimeCts?.Cancel();
            GameManager.Instance.Pool.Despawn(this);
        }

        // IPoolable implementation (Physics mode only)
        public void OnSpawn()
        {
            mode = ProjectileMode.Physics;
            isInitialized = false;
            fixedDirection = Vector3.zero;
            elapsedTime = 0f;
            onHitCallback = null;

            if (rb != null) rb.linearVelocity = Vector3.zero;

            // Ensure particle systems follow projectile
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
            supportSkill = null; // Clear support skill reference

            // Reset chain state
            currentChainCount = 0;
            maxChainCount = 0;
            chainHitTargets = null;
            currentChainDamage = 0f;

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
