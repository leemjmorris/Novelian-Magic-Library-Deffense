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

        [Header("Components")]
        [SerializeField, Tooltip("Rigidbody for physics movement (Physics mode only)")]
        private Rigidbody rb;

        [Header("Damage")]
        [SerializeField, Tooltip("Projectile damage")]
        private float damage = 10f;

        // Movement mode
        private ProjectileMode mode = ProjectileMode.Physics;
        private System.Action<Vector3> onHitCallback;

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

        //LMJ : Launch projectile in Physics mode (for pooled projectiles with Rigidbody)
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime)
        {
            mode = ProjectileMode.Physics;
            transform.position = spawnPos;
            startPosition = spawnPos;
            targetPosition = targetPos;

            // Calculate fixed direction (NO HOMING)
            fixedDirection = (targetPos - spawnPos).normalized;
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            elapsedTime = 0f;
            isInitialized = true;

            // Cancel previous lifetime token
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();

            // Start lifetime countdown
            TrackLifetimeAsync(lifetimeCts.Token).Forget();

            Debug.Log($"[Projectile] Physics mode launched from {spawnPos} to {targetPos}");
        }

        //LMJ : Launch projectile in Effect mode (for visual-only projectiles without physics)
        public void LaunchEffect(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime, float damageAmount, System.Action<Vector3> onHit = null)
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

            if (other.CompareTag(Tag.Monster))
            {
                Monster monster = other.GetComponent<Monster>();
                if (monster != null)
                {
                    monster.TakeDamage(damage);
                }

                // Mode-specific cleanup
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
                    boss.TakeDamage(damage);
                }

                // Mode-specific cleanup
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
