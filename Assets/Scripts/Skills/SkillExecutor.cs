using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Skills
{
    /// <summary>
    /// Skill Execution Engine - Handles automatic skill usage based on SkillConfig
    /// Supports multiple skill types: Instant, Projectile, AOE, Dash, MovingAOE
    /// </summary>
    public class SkillExecutor : MonoBehaviour
    {
        [Header("Skill Configuration")]
        [SerializeField] private SkillConfig skillConfig;

        [Header("Targeting")]
        [SerializeField] private float attackRange = 1000.0f;

        [Header("Animation")]
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private string attackAnimationTrigger = "2_Attack";

        [Header("Spawn Position")]
        [SerializeField] private Transform projectileSpawnPoint; // Optional: custom spawn point
        [SerializeField] private Vector3 spawnOffset = new Vector3(0, 1.5f, 0); // Offset from spawn point (Y=1.5 for height)

        // Internal state
        private ITargetable currentTarget;
        private float cooldownTimer = 0.0f;
        private bool isReady = false;  // Start as not ready to prevent immediate firing
        private bool isInitialized = false;

        private void Start()
        {
            InitializeSkillPool();
            isInitialized = true;

            // Start with cooldown to prevent immediate firing after spawn
            StartCooldown();
        }

        private void Update()
        {
            // Don't execute until properly initialized
            if (!isInitialized)
                return;

            // Update cooldown timer
            if (!isReady)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    isReady = true;
                }
            }

            // Find target if needed (with debug logging like original Character.cs)
            if (currentTarget == null || !currentTarget.IsAlive())
            {
                currentTarget = TargetRegistry.Instance.FindTarget(transform.position, attackRange);
                if (currentTarget != null)
                {
                    Debug.Log($"[SkillExecutor] Found target at position: {currentTarget.GetPosition()}");
                }
                else
                {
                    Debug.Log($"[SkillExecutor] No target found. Character position: {transform.position}, AttackRange: {attackRange}");
                }
            }

            // Execute skill when ready and target exists
            if (isReady && currentTarget != null && skillConfig != null)
            {
                ExecuteSkill();
                StartCooldown();
            }
        }

        /// <summary>
        /// Initialize object pool for skill projectiles/effects
        /// </summary>
        private void InitializeSkillPool()
        {
            if (GameManager.Instance == null || GameManager.Instance.Pool == null)
            {
                return;
            }

            if (skillConfig == null)
            {
                Debug.LogWarning("[SkillExecutor] No SkillConfig assigned!");
                return;
            }

            // Create pool for projectiles
            if (skillConfig.hasProjectile && skillConfig.projectilePrefab != null)
            {
                if (!GameManager.Instance.Pool.HasPool<Projectile>())
                {
                    GameManager.Instance.Pool.CreatePool<Projectile>(
                        skillConfig.projectilePrefab,
                        defaultCapacity: 5,
                        maxSize: 20
                    );
                    GameManager.Instance.Pool.WarmUp<Projectile>(20);
                    Debug.Log($"[SkillExecutor] Created pool for projectile: {skillConfig.skillName}");
                }
            }
        }

        /// <summary>
        /// Execute skill based on SkillConfig settings
        /// </summary>
        private void ExecuteSkill()
        {
            if (skillConfig == null || currentTarget == null)
                return;

            // Play animation
            PlayAttackAnimation();

            // Execute based on cast mode
            switch (skillConfig.castMode)
            {
                case CastMode.Instant:
                    ExecuteInstantSkill();
                    break;

                case CastMode.Projectile:
                    ExecuteProjectileSkill();
                    break;

                case CastMode.Placement:
                    ExecutePlacementSkill();
                    break;

                case CastMode.Dash:
                    ExecuteDashSkill();
                    break;

                case CastMode.MovingAOE:
                    ExecuteMovingAOESkill();
                    break;

                case CastMode.Channeling:
                    ExecuteChannelingSkill();
                    break;

                default:
                    Debug.LogWarning($"[SkillExecutor] Unsupported cast mode: {skillConfig.castMode}");
                    break;
            }

            // Spawn muzzle flash effect if configured
            if (skillConfig.muzzleFlashEffectPrefab != null)
            {
                SpawnMuzzleFlash();
            }
        }

        /// <summary>
        /// Execute instant damage skill (no projectile)
        /// </summary>
        private void ExecuteInstantSkill()
        {
            Debug.Log($"[SkillExecutor] Instant skill: {skillConfig.skillName}");

            // Apply instant damage or effect
            if (skillConfig.aoeType == AreaOfEffectType.None)
            {
                // Single target
                ApplyDamageToTarget(currentTarget);
            }
            else
            {
                // AOE damage
                ExecuteAOEDamage();
            }

            // Spawn hit effect on target
            if (skillConfig.onHitEffectPrefab != null && currentTarget != null)
            {
                SpawnHitEffect(currentTarget.GetPosition());
            }
        }

        /// <summary>
        /// Execute projectile-based skill
        /// </summary>
        private void ExecuteProjectileSkill()
        {
            if (!skillConfig.hasProjectile || skillConfig.projectilePrefab == null)
            {
                Debug.LogWarning("[SkillExecutor] Projectile skill configured but no projectile prefab!");
                return;
            }

            // Calculate spawn position with offset
            Vector3 basePosition = projectileSpawnPoint != null ?
                projectileSpawnPoint.position : transform.position;
            Vector3 spawnPosition = basePosition + spawnOffset;

            // Spawn projectile from pool
            var projectile = GameManager.Instance.Pool.Spawn<Projectile>(spawnPosition);

            Debug.Log($"[SkillExecutor] Spawning projectile at {spawnPosition} (base: {basePosition}, offset: {spawnOffset})");

            // Initialize projectile with skill config settings
            projectile.InitializeAndSetTarget(
                skillConfig.projectileSpeed,
                skillConfig.projectileDuration,
                currentTarget.GetTransform()
            );

            Debug.Log($"[SkillExecutor] Projectile skill: {skillConfig.skillName} at {spawnPosition}");
        }

        /// <summary>
        /// Execute placement skill (place AOE on ground)
        /// </summary>
        private void ExecutePlacementSkill()
        {
            Debug.Log($"[SkillExecutor] Placement skill: {skillConfig.skillName}");

            Vector3 placementPosition = currentTarget.GetPosition();

            // TODO: Spawn placement AOE effect
            if (skillConfig.aoeEffectPrefab != null)
            {
                GameObject aoe = Instantiate(skillConfig.aoeEffectPrefab, placementPosition, Quaternion.identity);
                // TODO: Add AOE damage logic component
            }
        }

        /// <summary>
        /// Execute dash skill (Flicker Strike style)
        /// </summary>
        private void ExecuteDashSkill()
        {
            if (!skillConfig.isDashSkill)
            {
                Debug.LogWarning("[SkillExecutor] Dash skill not properly configured!");
                return;
            }

            Debug.Log($"[SkillExecutor] Dash skill: {skillConfig.skillName}");

            // TODO: Implement dash sequence
            // 1. Find nearby targets within dashRange
            // 2. Dash to each target (maxDashTargets)
            // 3. Apply damage at each dash
            // 4. Return to origin if configured
        }

        /// <summary>
        /// Execute moving AOE skill (moving cloud of damage)
        /// </summary>
        private void ExecuteMovingAOESkill()
        {
            if (!skillConfig.isMovingAOE)
            {
                Debug.LogWarning("[SkillExecutor] Moving AOE skill not properly configured!");
                return;
            }

            Debug.Log($"[SkillExecutor] Moving AOE skill: {skillConfig.skillName}");

            Vector3 spawnPosition = transform.position;

            // TODO: Spawn moving AOE effect
            if (skillConfig.aoeEffectPrefab != null)
            {
                GameObject movingAOE = Instantiate(skillConfig.aoeEffectPrefab, spawnPosition, Quaternion.identity);
                // TODO: Add MovingAOE component with movement logic
            }
        }

        /// <summary>
        /// Execute channeling skill (continuous damage)
        /// </summary>
        private void ExecuteChannelingSkill()
        {
            Debug.Log($"[SkillExecutor] Channeling skill: {skillConfig.skillName}");

            // TODO: Implement channeling logic
            // 1. Start channeling animation
            // 2. Apply damage over time
            // 3. Cancel if target dies or moves out of range
        }

        /// <summary>
        /// Execute AOE damage around target or caster
        /// </summary>
        private void ExecuteAOEDamage()
        {
            Vector3 center = currentTarget != null ? currentTarget.GetPosition() : transform.position;

            // Find all targets in AOE range
            Collider[] hits = Physics.OverlapSphere(center, skillConfig.aoeRadius);

            foreach (var hit in hits)
            {
                ITargetable target = hit.GetComponent<ITargetable>();
                if (target != null && target.IsAlive())
                {
                    ApplyDamageToTarget(target);
                }
            }

            Debug.Log($"[SkillExecutor] AOE damage at {center}, radius {skillConfig.aoeRadius}");
        }

        /// <summary>
        /// Apply damage to a single target
        /// </summary>
        private void ApplyDamageToTarget(ITargetable target)
        {
            // TODO: Get damage from skill config or character stats
            // For now, just log
            Debug.Log($"[SkillExecutor] Applying damage to {target.GetTransform().name}");

            // Spawn hit effect
            if (skillConfig.onHitEffectPrefab != null)
            {
                SpawnHitEffect(target.GetPosition());
            }
        }

        /// <summary>
        /// Spawn muzzle flash effect at skill origin
        /// </summary>
        private void SpawnMuzzleFlash()
        {
            Vector3 spawnPosition = projectileSpawnPoint != null ?
                projectileSpawnPoint.position : transform.position;

            GameObject flash = Instantiate(
                skillConfig.muzzleFlashEffectPrefab,
                spawnPosition,
                Quaternion.identity
            );

            // Auto-destroy after 2 seconds
            Destroy(flash, 2f);
        }

        /// <summary>
        /// Spawn hit effect at target position
        /// </summary>
        private void SpawnHitEffect(Vector3 position)
        {
            GameObject hitEffect = Instantiate(
                skillConfig.onHitEffectPrefab,
                position,
                Quaternion.identity
            );

            // Auto-destroy after 2 seconds
            Destroy(hitEffect, 2f);
        }

        /// <summary>
        /// Play attack animation
        /// </summary>
        private void PlayAttackAnimation()
        {
            if (characterAnimator != null && !string.IsNullOrEmpty(attackAnimationTrigger))
            {
                characterAnimator.SetTrigger(attackAnimationTrigger);
            }
        }

        /// <summary>
        /// Start skill cooldown
        /// </summary>
        private void StartCooldown()
        {
            if (skillConfig == null)
                return;

            cooldownTimer = skillConfig.cooldown;
            isReady = false;

            Debug.Log($"[SkillExecutor] Skill on cooldown for {cooldownTimer}s");
        }

        /// <summary>
        /// Force reset cooldown (for testing or special cases)
        /// </summary>
        public void ResetCooldown()
        {
            cooldownTimer = 0f;
            isReady = true;
        }

        /// <summary>
        /// Set new skill config at runtime
        /// </summary>
        public void SetSkillConfig(SkillConfig newConfig)
        {
            skillConfig = newConfig;
            InitializeSkillPool();
            ResetCooldown();
        }

        /// <summary>
        /// Get current skill config
        /// </summary>
        public SkillConfig GetSkillConfig()
        {
            return skillConfig;
        }

        /// <summary>
        /// Check if skill is ready to use
        /// </summary>
        public bool IsReady()
        {
            return isReady;
        }

        /// <summary>
        /// Get remaining cooldown time
        /// </summary>
        public float GetRemainingCooldown()
        {
            return Mathf.Max(0f, cooldownTimer);
        }
    }
}
