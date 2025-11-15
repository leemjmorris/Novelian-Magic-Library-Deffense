using NovelianMagicLibraryDefense.Events;
using UnityEngine;

//JML: Monster entity with movement and wall attack behavior
public class Monster : BaseEntity, ITargetable, IMovable
{
    [Header("Event Channels")]
    [SerializeField] private MonsterEvents monsterEvents;


    [Header("References")]
    [SerializeField] private MonsterMove monsterMove;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] new Collider2D collider2D;
    private Wall wall;
    
    [Header("Stats")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackInterval = 0.7f;

    public int Exp { get; private set; } = 11; // JML: Example exp amount

    private float attackTimer = 0f;
    private bool isWallHit = false;
    public bool IsWallHit => isWallHit;

    public float Weight { get; private set; } = 1f; // Example weight value


    private void OnEnable()
    {
        collider2D.enabled = true;
    }
    private void OnDisable()
    {
        collider2D.enabled = false;
    }

    void Start()
    {
        float randomX = Random.Range(-0.4f, 0.4f);
        transform.position = new Vector3(randomX, 2, -7.5f);
        
    }

    //JML: Physics-based movement in FixedUpdate
    private void FixedUpdate()
    {
        monsterMove.Move(this, moveSpeed);
    }

    //JML: Game logic in Update
    private void Update()
    {
        if (isWallHit && wall != null)
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

    public override void TakeDamage(float damage)
    {
        Debug.Log($"[Monster] TakeDamage({damage}) - HP: {currentHealth}/{maxHealth}"); // LCB: Debug damage
        base.TakeDamage(damage);
        Debug.Log($"[Monster] After damage - HP: {currentHealth}/{maxHealth}"); // LCB: Debug HP after damage
    }

    public override void Die()
    {
        Debug.Log($"[Monster] Die() called! Exp={Exp}"); // LCB: Debug monster death

        // JML: Unregister BEFORE despawning to prevent accessing destroyed object
        TargetRegistry.Instance.UnregisterTarget(this);

        // LMJ: Use EventChannel instead of static event
        if (monsterEvents != null)
        {
            monsterEvents.RaiseMonsterDied(this);
        }

        // LMJ: Changed from ObjectPoolManager.Instance to GameManager.Instance.Pool
        NovelianMagicLibraryDefense.Managers.GameManager.Instance.Pool.Despawn(this);
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(Tag.Wall))
        {
            wall = collision.GetComponent<Wall>();
            isWallHit = true;
        }
    }

    public override void OnSpawn()
    {
        base.OnSpawn(); // Initialize health

        isWallHit = false;
        wall = null;
        attackTimer = 0f;
        Weight = 1f;

        TargetRegistry.Instance.RegisterTarget(this);
    }

    public override void OnDespawn()
    {
        isWallHit = false;
        wall = null;
        attackTimer = 0f;
        Weight = 1f;

        // JML: Redundant safety check - should already be unregistered in Die()
        // But kept as failsafe for edge cases
        TargetRegistry.Instance.UnregisterTarget(this);
    }
}
