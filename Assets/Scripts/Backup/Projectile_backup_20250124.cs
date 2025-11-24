using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

//JML: Projectile with Rigidbody-based movement and target tracking
public class Projectile : MonoBehaviour, IPoolable
{
    [Header("Projectile Attributes")]
    [SerializeField] private Rigidbody rb;

    // JML: Damage is still set via Inspector (not in CSV yet)
    [SerializeField] private float damage = 10f;

    // JML: Speed and lifetime are now set dynamically from SkillConfig
    private float speed = 10f;
    private float lifetime = 5f;
    private float spawnTime;

    private Vector3 direction;
    private bool isInitialized = false;  // JML: Flag to prevent FixedUpdate before initialization

    // Homing projectile support
    private Transform homingTarget;
    private bool isHoming = false;
    [SerializeField] private float homingStrength = 5f; // How aggressively it tracks target

    //JML: Update for lifetime management only
    private void Update()
    {
        spawnTime += Time.deltaTime;
        if (spawnTime >= lifetime)
        {
            GameManager.Instance.Pool.Despawn(this);
        }
    }

    //JML: Physics-based movement in FixedUpdate
    private void FixedUpdate()
    {
        // JML: Defense code - wait until initialization is complete
        if (!isInitialized || direction == Vector3.zero)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // Homing behavior: Update direction towards target
        if (isHoming && homingTarget != null)
        {
            Vector3 targetDirection = (homingTarget.position - transform.position).normalized;
            direction = Vector3.Lerp(direction, targetDirection, homingStrength * Time.fixedDeltaTime).normalized;
        }

        rb.linearVelocity = direction * speed;

        // JML: Rotate projectile to face movement direction
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// JML: Initialize projectile with speed and lifetime from SkillConfig
    /// </summary>
    public void Initialize(float projectileSpeed, float projectileLifetime)
    {
        speed = projectileSpeed;
        lifetime = projectileLifetime;
    }

    /// <summary>
    /// JML: Initialize and set target atomically (before first FixedUpdate)
    /// This prevents the race condition where FixedUpdate runs before SetTarget
    /// </summary>
    public void InitializeAndSetTarget(float projectileSpeed, float projectileLifetime, Transform target, bool enableHoming = true)
    {
        // 1. Set speed and lifetime
        speed = projectileSpeed;
        lifetime = projectileLifetime;

        // 2. Set homing target
        homingTarget = target;
        isHoming = enableHoming;

        // 3. Calculate initial direction: Simply aim at target (LookAt behavior)
        if (target != null)
        {
            Vector3 targetPosition = target.position;
            Vector3 projectilePosition = transform.position;

            // Simple direct aim at target's current position
            direction = (targetPosition - projectilePosition).normalized;

            Debug.Log($"[Projectile] Aiming at target: {targetPosition}, from: {projectilePosition}, homing: {isHoming}");
        }

        // 4. Mark as initialized (enables FixedUpdate movement)
        isInitialized = true;
    }

    public void SetTarget(Transform target)
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position;
            Vector3 projectilePosition = transform.position;

            // Simple direct aim at target's current position
            direction = (targetPosition - projectilePosition).normalized;
        }
    }

    //JML: Handle collision with monsters
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag(Tag.Monster))
        {
            var monster = collision.GetComponent<Monster>();
            if (monster != null)
            {
                monster.TakeDamage(damage);
            }
            else
            {
                // Debug.LogWarning("[Projectile] Monster component not found!"); // LCB: Debug warning
            }
            // LMJ: Changed from ObjectPoolManager.Instance to GameManager.Instance.Pool
            GameManager.Instance.Pool.Despawn(this);
        }
        if (collision.CompareTag(Tag.BossMonster))
        {
            var bossMonster = collision.GetComponent<BossMonster>();
            if (bossMonster != null)
            {
                bossMonster.TakeDamage(damage);
            }
            GameManager.Instance.Pool.Despawn(this);
        }
    }

    //JML: Reset projectile state on spawn
    public void OnSpawn()
    {
        direction = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        spawnTime = 0f;  // Reset spawn time for pool reuse
        isInitialized = false;  // JML: Reset initialization flag

        // Reset homing
        homingTarget = null;
        isHoming = false;

        // JML: Ensure all particle systems follow the projectile
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }
    }

    //JML: Clean up projectile state on despawn
    public void OnDespawn()
    {
        direction = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        spawnTime = 0f;  // Reset spawn time

        // Reset homing
        homingTarget = null;
        isHoming = false;
    }
}
