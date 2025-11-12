using UnityEngine;
using UnityEngine.Pool;
public class Monster : MonoBehaviour, IPoolable, ITargetable
{
    public static event System.Action<Monster> OnMonsterDied;
    [SerializeField] new Collider2D collider2D;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackInterval = 0.7f;
    [SerializeField] private float maxHealth = 100f;
    public int Exp { get; private set; } = 11; // JML: Example exp amount

    private float attackTimer = 0f;
    private Wall wall;
    private float currentHealth;
    private bool isWallHit = false;

    // JML: ITargetable implementation
    public Transform GetTransform() => transform;
    public Vector3 GetPosition() => transform.position;
    public bool IsAlive() => gameObject.activeInHierarchy && currentHealth > 0;
    public float Weight { get; private set; } = 1f; // Example weight value
    //--------------------------------
    private void OnEnable()
    {
        collider2D.enabled = true;
        Debug.Log("Collider Enabled");
    }
    private void OnDisable()
    {
        collider2D.enabled = false;
        Debug.Log("Collider Disabled");
    }

    void Start()
    {
        float randomX = Random.Range(-0.4f, 0.4f);
        transform.position = new Vector3(randomX, 2, -7.5f);
        
    }

    private void Update()
    {
        Move();

        if (isWallHit)
        {
            attackTimer += Time.deltaTime;
            if (attackInterval <= attackTimer)
            {
                wall.TakeDamage(damage);
                attackTimer = 0f;
            }
        }
        Weight += 1f;
    }

    private void Move()
    {
        if (!isWallHit)
        {
            rb.linearVelocity = Vector2.down * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"Monster took {damage} damage. current Health: {currentHealth}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnMonsterDied?.Invoke(this);
        // LMJ: Changed from ObjectPoolManager.Instance to GameManager.Instance.Pool
        NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool.Despawn(this);
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(Tag.Wall))
        {
            Debug.Log("Monster hit the wall.");
            wall = collision.GetComponent<Wall>();
            
            isWallHit = true;
        }
    }

    public void OnSpawn()
    {
        
        currentHealth = maxHealth;
        isWallHit = false;
        wall = null;
        attackTimer = 0f;
        Weight = 1f;
        
        TargetRegistry.Instance.RegisterTarget(this);
        Debug.Log("Monster spawned");
    }

    public void OnDespawn()
    {
        isWallHit = false;
        wall = null;
        attackTimer = 0f;
        Weight = 1f;
        TargetRegistry.Instance.UnregisterTarget(this);
        Debug.Log("Monster despawned");
    }
}
