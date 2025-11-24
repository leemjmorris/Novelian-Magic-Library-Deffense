//LMJ : Character with simple projectile-based combat (Issue #265)
namespace Novelian.Combat
{
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;

    public class Character : MonoBehaviour, IPoolable
    {
        private const float DEFAULT_ATTACK_SPEED = 1f;
        private const float DEFAULT_PROJECTILE_SPEED = 10f;
        private const float DEFAULT_PROJECTILE_LIFETIME = 5f;
        private const float DEFAULT_ATTACK_RANGE = 1000f;

        [Header("Character Visual")]
        [SerializeField] private GameObject characterObj;

        [Header("Combat Settings")]
        [SerializeField, Tooltip("Attacks per second"), Range(0.1f, 10f)]
        private float attackSpeed = DEFAULT_ATTACK_SPEED;

        [SerializeField, Tooltip("Projectile movement speed"), Range(1f, 50f)]
        private float projectileSpeed = DEFAULT_PROJECTILE_SPEED;

        [SerializeField, Tooltip("Projectile lifetime in seconds"), Range(0.5f, 20f)]
        private float projectileLifetime = DEFAULT_PROJECTILE_LIFETIME;

        [SerializeField, Tooltip("Attack range"), Range(10f, 2000f)]
        private float attackRange = DEFAULT_ATTACK_RANGE;

        [Header("Spawn Position")]
        [SerializeField, Tooltip("Projectile spawn offset (future: from CharacterPreset)")]
        private Vector3 spawnOffset = Vector3.zero;

        [Header("Targeting Strategy")]
        [SerializeField, Tooltip("Use weight-based targeting (default: distance-based)")]
        private bool useWeightTargeting = false;

        [Header("References (Assign in Inspector)")]
        [SerializeField, Tooltip("Projectile prefab for pooling")]
        private GameObject projectilePrefab;

        // Attack state
        private CancellationTokenSource attackCts;
        private bool isInitialized = false;

        private void Start()
        {
            InitializeProjectilePool();
            StartAttackLoop();
            isInitialized = true;
        }

        //LMJ : Initialize projectile pool
        private void InitializeProjectilePool()
        {
            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;

            if (!pool.HasPool<Projectile>())
            {
                pool.CreatePool<Projectile>(projectilePrefab, defaultCapacity: 20, maxSize: 100);
                pool.WarmUp<Projectile>(20);
                Debug.Log("[Character] Projectile pool initialized");
            }
        }

        //LMJ : Start attack loop
        private void StartAttackLoop()
        {
            attackCts?.Cancel();
            attackCts = new CancellationTokenSource();
            AttackLoopAsync(attackCts.Token).Forget();
        }

        //LMJ : Main attack loop with UniTask
        private async UniTaskVoid AttackLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // Wait for attack interval
                float interval = 1f / attackSpeed;
                await UniTask.Delay((int)(interval * 1000), cancellationToken: ct);

                // Pause support (skip attack when Time.timeScale = 0)
                if (Time.timeScale == 0f) continue;

                TryAttack();
            }
        }

        //LMJ : Attempt to attack nearest or highest weight target
        private void TryAttack()
        {
            if (!isInitialized) return;

            // Find target based on strategy
            ITargetable target = useWeightTargeting
                ? TargetRegistry.Instance.FindSkillTarget(transform.position, attackRange)
                : TargetRegistry.Instance.FindTarget(transform.position, attackRange);

            if (target == null) return;

            // Calculate spawn position (character position + offset)
            Vector3 spawnPos = transform.position + spawnOffset;
            Vector3 targetPos = target.GetPosition();

            // Spawn projectile from pool
            var pool = NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool;
            Projectile projectile = pool.Spawn<Projectile>(spawnPos);

            // Launch projectile in straight line
            projectile.Launch(spawnPos, targetPos, projectileSpeed, projectileLifetime);

            Debug.Log($"[Character] Fired at {target.GetTransform().name} from {spawnPos}");
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
            }

            Debug.Log("[Character] Character spawned and ready");
        }

        public void OnDespawn()
        {
            characterObj.SetActive(false);
            attackCts?.Cancel();
            Debug.Log("[Character] Character despawned");
        }

        private void OnDestroy()
        {
            attackCts?.Cancel();
            attackCts?.Dispose();
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
