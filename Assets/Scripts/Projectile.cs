using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

//JML: Projectile with Rigidbody2D-based movement and target tracking
public class Projectile : MonoBehaviour, IPoolable
{
    [Header("Projectile Attributes")]
    [SerializeField] private Rigidbody2D rb;

    // JML: Damage is still set via Inspector (not in CSV yet)
    [SerializeField] private float damage = 10f;

    // JML: Speed and lifetime are now set dynamically from SkillConfig
    private float speed = 10f;
    private float lifetime = 5f;
    private float spawnTime;

    private Vector2 direction;
    private bool isInitialized = false;  // JML: Flag to prevent FixedUpdate before initialization

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
        if (!isInitialized || direction == Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = direction * speed;

        // JML: Rotate projectile to face movement direction
        // Calculate angle from direction vector (in degrees)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
    public void InitializeAndSetTarget(float projectileSpeed, float projectileLifetime, Transform target)
    {
        // 1. Set speed and lifetime
        speed = projectileSpeed;
        lifetime = projectileLifetime;

        // 2. Calculate and set direction immediately
        if (target != null)
        {
            Vector2 targetPosition = new Vector2(target.position.x, target.position.y);
            Vector2 projectilePosition = new Vector2(transform.position.x, transform.position.y);

            // Try to predict target's future position based on velocity
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null && targetRb.linearVelocity.sqrMagnitude > 0.01f)
            {
                Vector2 targetVelocity = targetRb.linearVelocity;

                // Simple prediction: use 50% of estimated time to reduce over-prediction
                float distance = Vector2.Distance(projectilePosition, targetPosition);
                float timeToReach = (distance / speed) * 0.5f;

                // Predict where target will be after that time
                Vector2 predictedPosition = targetPosition + (targetVelocity * timeToReach);

                // Aim at predicted position for better accuracy
                direction = (predictedPosition - projectilePosition).normalized;
            }
            else
            {
                // Fallback: direct aim if target has no Rigidbody2D or is stationary
                direction = (targetPosition - projectilePosition).normalized;
            }
        }

        // 3. Mark as initialized (enables FixedUpdate movement)
        isInitialized = true;
    }

    public void SetTarget(Transform target)
    {
        if (target != null)
        {
            Vector2 targetPosition = new Vector2(target.position.x, target.position.y);
            Vector2 projectilePosition = new Vector2(transform.position.x, transform.position.y);

            // Debug.Log($"[Projectile] SetTarget - Projectile Pos: {projectilePosition}, Target Pos: {targetPosition}");

            // Try to predict target's future position based on velocity
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null && targetRb.linearVelocity.sqrMagnitude > 0.01f)
            {
                Vector2 targetVelocity = targetRb.linearVelocity;

                // Simple prediction: use 50% of estimated time to reduce over-prediction
                float distance = Vector2.Distance(projectilePosition, targetPosition);
                float timeToReach = (distance / speed) * 0.5f;

                // Predict where target will be after that time
                Vector2 predictedPosition = targetPosition + (targetVelocity * timeToReach);

                // Aim at predicted position for better accuracy
                direction = (predictedPosition - projectilePosition).normalized;

                // Debug.Log($"[Projectile] Prediction - Velocity: {targetVelocity}, Time: {timeToReach:F2}s, Predicted: {predictedPosition}, Direction: {direction}");
            }
            else
            {
                // Fallback: direct aim if target has no Rigidbody2D or is stationary
                direction = (targetPosition - projectilePosition).normalized;

                // Debug.Log($"[Projectile] Direct aim - Direction: {direction}");
            }
        }
    }
    
    //JML: Handle collision with monsters
    private void OnTriggerEnter2D(Collider2D collision)
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
        direction = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        spawnTime = 0f;  // Reset spawn time for pool reuse
        isInitialized = false;  // JML: Reset initialization flag

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
        direction = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        spawnTime = 0f;  // Reset spawn time
    }
}
