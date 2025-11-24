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
        [SerializeField, Tooltip("Projectile spawn offset")]
        private Vector3 spawnOffset = Vector3.zero;

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

        // Active Skill 최종 수치 계산 프로퍼티
        private float FinalActiveDamage => activeSkill != null
            ? activeSkill.baseDamage * (1f + damageModifier / 100f)
            : 0f;

        private float FinalActiveAttackSpeed => activeSkill != null
            ? (1f / activeSkill.cooldown) * (1f + attackSpeedModifier / 100f)
            : 1f;

        private float FinalActiveProjectileSpeed => activeSkill != null
            ? activeSkill.projectileSpeed * (1f + projectileSpeedModifier / 100f)
            : 10f;

        private float FinalActiveRange => activeSkill != null
            ? activeSkill.range * (1f + rangeModifier / 100f)
            : 1000f;

        private float FinalActiveProjectileLifetime => activeSkill != null
            ? activeSkill.projectileLifetime
            : 5f;

        // Attack state
        private CancellationTokenSource attackCts;
        private CancellationTokenSource activeSkillCts;
        private bool isInitialized = false;

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
            if (basicAttackSkill == null || basicAttackSkill.projectilePrefab == null)
            {
                Debug.LogError("[Character] basicAttackSkill or projectilePrefab is null!");
                return;
            }

            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;

            if (!pool.HasPool<Projectile>())
            {
                pool.CreatePool<Projectile>(basicAttackSkill.projectilePrefab, defaultCapacity: 20, maxSize: 100);
                pool.WarmUp<Projectile>(20);
                Debug.Log($"[Character] Projectile pool initialized with skill: {basicAttackSkill.skillName}");
            }
        }

        //LMJ : Initialize active skill projectile pool
        private void InitializeActiveSkillPool()
        {
            if (activeSkill == null || activeSkill.projectilePrefab == null)
            {
                Debug.LogWarning("[Character] activeSkill or projectilePrefab is null. Skipping active skill initialization.");
                return;
            }

            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;

            // Active skill이 basic attack과 다른 프리팹을 사용하는 경우에만 새 풀 생성
            if (!pool.HasPool<Projectile>())
            {
                pool.CreatePool<Projectile>(activeSkill.projectilePrefab, defaultCapacity: 10, maxSize: 50);
                pool.WarmUp<Projectile>(10);
                Debug.Log($"[Character] Active skill pool initialized with skill: {activeSkill.skillName}");
            }
        }

        //LMJ : Start attack loop
        private void StartAttackLoop()
        {
            attackCts?.Cancel();
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
            if (!isInitialized || basicAttackSkill == null) return;

            // Find target based on strategy (using final range from skill + character modifier)
            ITargetable target = useWeightTargeting
                ? TargetRegistry.Instance.FindSkillTarget(transform.position, FinalRange)
                : TargetRegistry.Instance.FindTarget(transform.position, FinalRange);

            if (target == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // Spawn projectile from pool (using skill's projectile prefab)
            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;
            Projectile projectile = pool.Spawn<Projectile>(spawnPos);

            // Launch projectile in straight line (using final projectile speed from skill + character modifier)
            projectile.Launch(spawnPos, targetPos, FinalProjectileSpeed, FinalProjectileLifetime);

            Debug.Log($"[Character] Fired {basicAttackSkill.skillName} at {target.GetTransform().name} (Damage: {FinalDamage:F1}, Speed: {FinalProjectileSpeed:F1})");
        }

        //LMJ : Attempt to use active skill on target
        private void TryUseActiveSkill()
        {
            if (!isInitialized || activeSkill == null) return;

            // Find target based on strategy (using final range from active skill + character modifier)
            ITargetable target = useWeightTargeting
                ? TargetRegistry.Instance.FindSkillTarget(transform.position, FinalActiveRange)
                : TargetRegistry.Instance.FindTarget(transform.position, FinalActiveRange);

            if (target == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // Spawn projectile from pool (using active skill's projectile prefab)
            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;
            Projectile projectile = pool.Spawn<Projectile>(spawnPos);

            // Launch projectile in straight line (using final projectile speed from active skill + character modifier)
            projectile.Launch(spawnPos, targetPos, FinalActiveProjectileSpeed, FinalActiveProjectileLifetime);

            Debug.Log($"[Character] Used Active Skill: {activeSkill.skillName} at {target.GetTransform().name} (Damage: {FinalActiveDamage:F1}, Speed: {FinalActiveProjectileSpeed:F1})");
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
            activeSkillCts?.Cancel();
            Debug.Log("[Character] Character despawned");
        }

        private void OnDestroy()
        {
            attackCts?.Cancel();
            attackCts?.Dispose();
            activeSkillCts?.Cancel();
            activeSkillCts?.Dispose();
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
