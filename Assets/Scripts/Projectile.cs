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
    private Transform target;
    private bool hasLostTarget = false;
    //JML: Update target direction in Update
    private void Update()
    {
        if (target != null && !hasLostTarget)
        {
            if (!target.gameObject.activeInHierarchy)
            {
                hasLostTarget = true;
            }
            else
            {
                direction = (target.position - transform.position).normalized;
            }
        }

        spawnTime += Time.deltaTime;
        if (spawnTime >= lifetime)
        {
            GameManager.Instance.Pool.Despawn(this);
            spawnTime = 0f;
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
        if (!hasLostTarget)
        {
            this.target = target;
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
                Debug.LogWarning("[Projectile] Monster component not found!"); // LCB: Debug warning
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
        hasLostTarget = false;
        target = null;
        direction = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
    }

    //JML: Clean up projectile state on despawn
    public void OnDespawn()
    {
        hasLostTarget = false;
        target = null;
        direction = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
    }
}
