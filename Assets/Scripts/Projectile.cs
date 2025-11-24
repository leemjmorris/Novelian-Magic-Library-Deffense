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

//LMJ : Straight-line projectile with fixed direction (Issue #265)
namespace Novelian.Combat
{
    using NovelianMagicLibraryDefense.Managers;
    using UnityEngine;
    using Cysharp.Threading.Tasks;
    using System.Threading;

    public class Projectile : MonoBehaviour, IPoolable
    {
        private const float OUT_OF_BOUNDS_DISTANCE = 100f;

        [Header("Components")]
        [SerializeField, Tooltip("Rigidbody for physics movement")]
        private Rigidbody rb;

        [Header("Damage")]
        [SerializeField, Tooltip("Projectile damage")]
        private float damage = 10f;

        // Movement state
        private Vector3 fixedDirection;
        private float speed;
        private float lifetime;
        private Vector3 startPosition;
        private bool isInitialized = false;

        // Lifetime tracking
        private CancellationTokenSource lifetimeCts;

        //LMJ : Initialize and launch projectile in fixed direction
        public void Launch(Vector3 spawnPos, Vector3 targetPos, float projectileSpeed, float projectileLifetime)
        {
            transform.position = spawnPos;
            startPosition = spawnPos;

            // Calculate fixed direction (NO HOMING)
            fixedDirection = (targetPos - spawnPos).normalized;
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            isInitialized = true;

            // Cancel previous lifetime token
            lifetimeCts?.Cancel();
            lifetimeCts = new CancellationTokenSource();

            // Start lifetime countdown
            TrackLifetimeAsync(lifetimeCts.Token).Forget();

            Debug.Log($"[Projectile] Launched from {spawnPos} to {targetPos}, direction: {fixedDirection}");
        }

        //LMJ : Physics-based movement in fixed direction
        private void FixedUpdate()
        {
            // Wait until initialized
            if (!isInitialized) return;

            // Pause support (respect Time.timeScale for card selection UI)
            if (Time.timeScale == 0f)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }

            // Move in fixed direction
            rb.linearVelocity = fixedDirection * speed;

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

        //LMJ : Handle collision with monsters
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(Tag.Monster))
            {
                Monster monster = other.GetComponent<Monster>();
                if (monster != null)
                {
                    monster.TakeDamage(damage);
                }
                ReturnToPool();
            }
            else if (other.CompareTag(Tag.BossMonster))
            {
                BossMonster boss = other.GetComponent<BossMonster>();
                if (boss != null)
                {
                    boss.TakeDamage(damage);
                }
                ReturnToPool();
            }
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
            isInitialized = false;
            fixedDirection = Vector3.zero;
            rb.linearVelocity = Vector3.zero;

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
            rb.linearVelocity = Vector3.zero;
            lifetimeCts?.Cancel();
        }

        private void OnDestroy()
        {
            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
        }
    }
}
