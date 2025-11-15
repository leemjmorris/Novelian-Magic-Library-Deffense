using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

//JML: Projectile with Rigidbody2D-based movement and target tracking
public class Projectile : MonoBehaviour, IPoolable
{
    [Header("Projectile Attributes")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 5f;
    private float spawnTime;

    private Vector3 direction;

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
        if (direction != Vector3.zero)
        {
            rb.linearVelocity = direction * speed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void SetTarget(Transform target)
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position;

            // Debug.Log($"[Projectile] SetTarget - Projectile Pos: {transform.position}, Target Pos: {targetPosition}");

            // Try to predict target's future position based on velocity
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null && targetRb.linearVelocity.sqrMagnitude > 0.01f)
            {
                Vector3 targetVelocity = targetRb.linearVelocity;

                // Simple prediction: use 50% of estimated time to reduce over-prediction
                float distance = Vector3.Distance(transform.position, targetPosition);
                float timeToReach = (distance / speed) * 0.5f;

                // Predict where target will be after that time
                Vector3 predictedPosition = targetPosition + (targetVelocity * timeToReach);

                // Aim at predicted position for better accuracy
                direction = (predictedPosition - transform.position).normalized;

                // Debug.Log($"[Projectile] Prediction - Velocity: {targetVelocity}, Time: {timeToReach:F2}s, Predicted: {predictedPosition}, Direction: {direction}");
            }
            else
            {
                // Fallback: direct aim if target has no Rigidbody2D or is stationary
                direction = (targetPosition - transform.position).normalized;

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
        direction = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
        spawnTime = 0f;  // Reset spawn time for pool reuse
    }

    //JML: Clean up projectile state on despawn
    public void OnDespawn()
    {
        direction = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
        spawnTime = 0f;  // Reset spawn time
    }
}
